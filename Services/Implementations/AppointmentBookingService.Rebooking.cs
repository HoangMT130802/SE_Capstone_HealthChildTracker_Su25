using AutoMapper;
using Contracts.DTOs.Appointment;
using Contracts.DTOs.Child;
using Contracts.DTOs.ChildVaccineProfile;
using Contracts.DTOs.Disease;
using Contracts.DTOs.Order;
using Contracts.DTOs.Vaccine;
using Contracts.DTOs.Models;
using Microsoft.Extensions.Logging;
using Repositories.Entities;
using Repositories.Interfaces;

namespace Services.Implementations
{
    public partial class AppointmentBookingService
    {
        private async Task<AppointmentRebookingValidationDTO> ValidateOrderAndCostAsync(ChildVaccineProfile profile, int accountId)
        {
            // 5. Tìm gói Order có thể áp dụng
            var memberRepo = _unitOfWork.GetRepository<Member>();
            var member = await memberRepo.GetAsync(m => m.AccountId == accountId);

            var orderRepo = _unitOfWork.GetRepository<Order>();
            var applicableOrder = member != null ? await orderRepo.GetAsync(
                o => o.MemberId == member.MemberId 
                  && o.Status == "Paid" 
                  && o.OrderDetails.Any(od => od.FacilityVaccine != null && od.FacilityVaccine.VaccineId == profile.VaccineId 
                                            && od.DiseaseId == profile.DiseaseId 
                                            && od.RemainingQuantity > 0),
                includeProperties: "OrderDetails,OrderDetails.FacilityVaccine,OrderDetails.Disease"
            ) : null;

            bool hasApplicableOrder = applicableOrder != null;
            int availableQuantity = 0;
            decimal estimatedCost = 0;

            if (hasApplicableOrder && applicableOrder != null)
            {
                var relevantOrderDetail = applicableOrder.OrderDetails?
                    .FirstOrDefault(od => od.FacilityVaccine != null && od.FacilityVaccine.VaccineId == profile.VaccineId 
                                       && od.DiseaseId == profile.DiseaseId);
                
                if (relevantOrderDetail != null)
                {
                    availableQuantity = relevantOrderDetail.RemainingQuantity;
                    estimatedCost = 0; // Không tính phí nếu dùng gói đã mua
                }
            }
            else
            {
                // Nếu không có gói, cần tính chi phí mua lẻ
                var facilityVaccineRepo = _unitOfWork.GetRepository<FacilityVaccine>();
                var availableFacilityVaccines = await facilityVaccineRepo.FindAsync(
                    fv => fv.Vaccine.VaccineId == profile.VaccineId 
                       && fv.Status == "active" 
                       && fv.AvailableQuantity > 0,
                    includeProperties: "Facility,Vaccine"
                );
                
                if (availableFacilityVaccines.Any())
                {
                    estimatedCost = availableFacilityVaccines.Min(fv => fv.Price);
                    
                    // Log thông tin các cơ sở có vaccine này
                    var facilityNames = string.Join(", ", availableFacilityVaccines.Select(fv => fv.Facility.FacilityName));
                    _logger.LogInformation("Tìm thấy vaccine {VaccineId} ({VaccineName}) tại các cơ sở: {FacilityNames}", 
                        profile.VaccineId, profile.Vaccine.Name, facilityNames);
                }
                else
                {
                    return new AppointmentRebookingValidationDTO
                    {
                        CanRebook = false,
                        ReasonCannotRebook = $"Hiện tại không có cơ sở nào cung cấp vaccine {profile.Vaccine.Name} cho bệnh {profile.Disease.Name}",
                        HasApplicableOrder = false,
                        RequiresPayment = true,
                        EstimatedCost = 0
                    };
                }
            }

            return new AppointmentRebookingValidationDTO
            {
                CanRebook = true,
                ReasonCannotRebook = null,
                HasApplicableOrder = hasApplicableOrder,
                ApplicableOrder = hasApplicableOrder ? _mapper.Map<OrderDTO>(applicableOrder) : null,
                AvailableVaccineQuantity = availableQuantity,
                EstimatedCost = estimatedCost,
                RequiresPayment = !hasApplicableOrder,
                VaccineProfile = _mapper.Map<ChildVaccineProfileDTO>(profile),
                Vaccine = _mapper.Map<VaccineDTO>(profile.Vaccine),
                Disease = _mapper.Map<DiseaseDTO>(profile.Disease)
            };
        }

        public async Task<ResponseDataModel<AppointmentRebookingResponseDTO>> RebookAppointmentAsync(AppointmentRebookingRequestDTO request, int accountId)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                _logger.LogInformation("Processing rebooking request for ChildVaccineProfile {ProfileId} by Account {AccountId}", 
                    request.ChildVaccineProfileId, accountId);

                // 1. Validate request trước
                var validationResult = await ValidateRebookingRequestAsync(request.ChildVaccineProfileId, accountId);
                if (!validationResult.Status || !validationResult.Data.CanRebook)
                {
                    return CreateErrorResponse<AppointmentRebookingResponseDTO>(validationResult.Data.ReasonCannotRebook ?? validationResult.Message);
                }
                var validation = validationResult.Data;

                // ===============
                // Re-validate bổ sung: chặn double-book & overbook order tại thời điểm rebook
                // ===============
                try
                {
                    var profileRepoCheck = _unitOfWork.GetRepository<ChildVaccineProfile>();
                    var profileForCheck = await profileRepoCheck.GetAsync(p => p.VaccineProfileId == request.ChildVaccineProfileId,
                        includeProperties: "Child,Vaccine,Disease");
                    if (profileForCheck == null)
                    {
                        return CreateErrorResponse<AppointmentRebookingResponseDTO>("Không tìm thấy vaccine profile để rebook");
                    }

                    // Chặn nếu đã có appointment active khác cho cùng child+disease+dose (ngoài profile này)
                    var profileDiseaseId = profileForCheck.DiseaseId;
                    var appointmentRepoCheck = _unitOfWork.GetRepository<VaccinationAppointment>();
                    var childIdCheck = profileForCheck.ChildId;
                    var existingProfiles = await profileRepoCheck.FindAsync(p => p.ChildId == childIdCheck && p.DiseaseId == profileDiseaseId && p.AppointmentId != null);
                    if (existingProfiles.Any(p => p.VaccineProfileId != request.ChildVaccineProfileId))
                    {
                        // Load appointments active
                        var relatedAppointmentIds = existingProfiles.Where(p => p.AppointmentId.HasValue).Select(p => p.AppointmentId!.Value).Distinct().ToList();
                        var relatedAppointments = relatedAppointmentIds.Any()
                            ? await appointmentRepoCheck.FindAsync(a => relatedAppointmentIds.Contains(a.AppointmentId), includeProperties: "Schedule,Schedule.Slot")
                            : new List<VaccinationAppointment>();

                        var now = DateTime.Now;
                        var hasActive = relatedAppointments.Any(a =>
                        {
                            if (!(a.Status == "Pending" || a.Status == "Approval")) return false;
                            var start = a.Schedule?.Slot?.StartTime;
                            if (start.HasValue) return a.Schedule!.Date.ToDateTime(start.Value) > now;
                            return a.Schedule != null && a.Schedule.Date >= DateOnly.FromDateTime(DateTime.Today);
                        });

                        if (hasActive)
                        {
                            return CreateErrorResponse<AppointmentRebookingResponseDTO>("Đã có lịch đang hoạt động cho bệnh này. Không thể đặt lại.");
                        }
                    }

                    // Nếu dùng Order: kiểm tra overbook theo reserved logic tương tự validate booking
                    if (request.OrderId.HasValue)
                    {
                        var orderRepo = _unitOfWork.GetRepository<Order>();
                        var order = await orderRepo.GetAsync(o => o.OrderId == request.OrderId.Value, includeProperties: "OrderDetails");
                        if (order != null)
                        {
                            var totalRemaining = order.OrderDetails?.Where(od => od.DiseaseId == profileDiseaseId).Sum(od => od.RemainingQuantity) ?? 0;

                            var pendingProfilesForChild = await profileRepoCheck.FindAsync(p => p.ChildId == childIdCheck && p.DiseaseId == profileDiseaseId && (p.Status == "Pending" || p.Status == "Scheduled") && p.AppointmentId != null);
                            var relatedIds = pendingProfilesForChild.Where(p => p.AppointmentId.HasValue).Select(p => p.AppointmentId!.Value).Distinct().ToList();
                            var related = relatedIds.Any()
                                ? await appointmentRepoCheck.FindAsync(a => relatedIds.Contains(a.AppointmentId) && a.OrderId == request.OrderId.Value, includeProperties: "Schedule,Schedule.Slot")
                                : new List<VaccinationAppointment>();

                            var now2 = DateTime.Now;
                            var reservedCount = related.Count(a =>
                            {
                                if (!(a.Status == "Pending" || a.Status == "Approval")) return false;
                                var start = a.Schedule?.Slot?.StartTime;
                                if (start.HasValue) return a.Schedule!.Date.ToDateTime(start.Value) > now2;
                                return a.Schedule != null && a.Schedule.Date >= DateOnly.FromDateTime(DateTime.Today);
                            });

                            if (reservedCount >= totalRemaining)
                            {
                                return CreateErrorResponse<AppointmentRebookingResponseDTO>("Gói đã hết số lượng khả dụng cho bệnh này do đã được giữ bởi các lịch đang chờ.");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi re-validate chặn double-book/overbook trước khi rebook");
                    return CreateErrorResponse<AppointmentRebookingResponseDTO>($"Có lỗi xảy ra khi kiểm tra: {ex.Message}");
                }

                // 2. Lấy thông tin ChildVaccineProfile và Schedule
                var profileRepo = _unitOfWork.GetRepository<ChildVaccineProfile>();
                var profile = await profileRepo.GetAsync(
                    p => p.VaccineProfileId == request.ChildVaccineProfileId,
                    includeProperties: "Child,Child.Member,Vaccine,Disease"
                );

                var scheduleRepo = _unitOfWork.GetRepository<AppointmentSchedule>();
                var schedule = await scheduleRepo.GetAsync(
                    s => s.ScheduleId == request.ScheduleId,
                    includeProperties: "Facility,Slot"
                );

                if (schedule == null)
                {
                    return CreateErrorResponse<AppointmentRebookingResponseDTO>("Không tìm thấy lịch trống");
                }

                // 3. Kiểm tra lịch trống còn slot không
                if (schedule.BookedCount >= schedule.Slot.MaxCapacity)
                {
                    return CreateErrorResponse<AppointmentRebookingResponseDTO>("Lịch này đã hết chỗ");
                }

                // 3.5. Kiểm tra cơ sở có vaccine phù hợp không (nếu không dùng order)
                if (!request.OrderId.HasValue)
                {
                    var facilityVaccineRepo = _unitOfWork.GetRepository<FacilityVaccine>();
                    var facilityVaccine = await facilityVaccineRepo.GetAsync(
                        fv => fv.FacilityId == schedule.FacilityId 
                           && fv.Vaccine != null && fv.Vaccine.VaccineId == profile.VaccineId
                           && fv.Status == "active"
                           && fv.AvailableQuantity > 0
                    );

                    if (facilityVaccine == null)
                    {
                        // Tìm tất cả cơ sở có vaccine này để gợi ý
                        var allFacilityVaccines = await facilityVaccineRepo.FindAsync(
                            fv => fv.Vaccine != null && fv.Vaccine.VaccineId == profile.VaccineId
                               && fv.Status == "active"
                               && fv.AvailableQuantity > 0,
                            includeProperties: "Facility"
                        );

                        if (allFacilityVaccines.Any())
                        {
                            var availableFacilities = string.Join(", ", allFacilityVaccines.Select(fv => fv.Facility?.FacilityName ?? "Unknown"));
                            return CreateErrorResponse<AppointmentRebookingResponseDTO>($"Cơ sở {schedule.Facility?.FacilityName} không có vaccine {profile.Vaccine?.Name} cho bệnh {profile.Disease?.Name}. Các cơ sở có vaccine này: {availableFacilities}");
                        }
                        else
                        {
                            return CreateErrorResponse<AppointmentRebookingResponseDTO>($"Hiện tại không có cơ sở nào cung cấp vaccine {profile.Vaccine?.Name} cho bệnh {profile.Disease?.Name}");
                        }
                    }
                }

                // 4. Xử lý Order với support chọn vaccine khác (ưu tiên OrderDetailId, sau đó OrderId, cuối cùng validation)
                Order? usedOrder = null;
                OrderDetail? selectedOrderDetail = null;
                int? remainingVaccines = null;
                int selectedVaccineId = profile.VaccineId; // Default là vaccine hiện tại
                int selectedDiseaseId = profile.DiseaseId; // Default là disease hiện tại
                
                // ✅ PRIORITY 1: OrderDetailId từ request (cho phép chọn vaccine khác)
                if (request.OrderDetailId.HasValue)
                {
                    var orderDetailRepo = _unitOfWork.GetRepository<OrderDetail>();
                    selectedOrderDetail = await orderDetailRepo.GetAsync(
                        od => od.OrderDetailId == request.OrderDetailId.Value && od.RemainingQuantity > 0,
                        includeProperties: "Order,FacilityVaccine,FacilityVaccine.Vaccine,Disease"
                    );

                    if (selectedOrderDetail != null)
                    {
                        usedOrder = selectedOrderDetail.Order;
                        remainingVaccines = selectedOrderDetail.RemainingQuantity;
                        selectedVaccineId = selectedOrderDetail.FacilityVaccine?.VaccineId ?? profile.VaccineId;
                        selectedDiseaseId = selectedOrderDetail.DiseaseId;
                        
                        _logger.LogInformation("✅ Chọn vaccine từ OrderDetailId {OrderDetailId}: VaccineId {VaccineId} → {NewVaccineId}, DiseaseId {DiseaseId} → {NewDiseaseId}", 
                            request.OrderDetailId.Value, profile.VaccineId, selectedVaccineId, profile.DiseaseId, selectedDiseaseId);
                    }
                    else
                    {
                        _logger.LogWarning("OrderDetailId {OrderDetailId} không tồn tại hoặc đã hết", request.OrderDetailId.Value);
                    }
                }
                // ✅ PRIORITY 2: OrderId từ request với vaccine hiện tại
                else if (request.OrderId.HasValue)
                {
                    var orderRepo = _unitOfWork.GetRepository<Order>();
                    usedOrder = await orderRepo.GetAsync(
                        o => o.OrderId == request.OrderId.Value,
                        includeProperties: "OrderDetails,OrderDetails.FacilityVaccine,OrderDetails.Disease"
                    );

                    if (usedOrder != null)
                    {
                        selectedOrderDetail = usedOrder.OrderDetails?
                            .FirstOrDefault(od => od.FacilityVaccine != null && od.FacilityVaccine.VaccineId == profile.VaccineId 
                                               && od.DiseaseId == profile.DiseaseId && od.RemainingQuantity > 0);
                        
                        if (selectedOrderDetail != null)
                        {
                            remainingVaccines = selectedOrderDetail.RemainingQuantity;
                            _logger.LogInformation("Sử dụng OrderId từ request: {OrderId}, RemainingQuantity: {RemainingQuantity}", 
                                request.OrderId.Value, remainingVaccines);
                        }
                        else
                        {
                            _logger.LogWarning("OrderId {OrderId} không có vaccine phù hợp hoặc đã hết", request.OrderId.Value);
                            usedOrder = null; // Reset nếu không có vaccine phù hợp
                        }
                    }
                }
                // ✅ PRIORITY 3: Fallback sử dụng validation nếu không có OrderId từ request
                else if (validation.HasApplicableOrder && validation.ApplicableOrder != null)
                {
                    var orderRepo = _unitOfWork.GetRepository<Order>();
                    usedOrder = await orderRepo.GetAsync(
                        o => o.OrderId == validation.ApplicableOrder.OrderId,
                        includeProperties: "OrderDetails,OrderDetails.FacilityVaccine,OrderDetails.Disease"
                    );

                    if (usedOrder != null)
                    {
                        selectedOrderDetail = usedOrder.OrderDetails?
                            .FirstOrDefault(od => od.FacilityVaccine != null && od.FacilityVaccine.VaccineId == profile.VaccineId 
                                               && od.DiseaseId == profile.DiseaseId && od.RemainingQuantity > 0);
                        
                        if (selectedOrderDetail != null)
                        {
                            remainingVaccines = selectedOrderDetail.RemainingQuantity;
                            _logger.LogInformation("Sử dụng OrderId từ validation: {OrderId}, RemainingQuantity: {RemainingQuantity}", 
                                validation.ApplicableOrder.OrderId, remainingVaccines);
                        }
                    }
                }

                // 5. Tạo VaccinationAppointment mới
                var appointmentRepo = _unitOfWork.GetRepository<VaccinationAppointment>();
                var newAppointment = new VaccinationAppointment
                {
                    ChildId = profile.ChildId,
                    OrderId = usedOrder?.OrderId,
                    ScheduleId = request.ScheduleId,
                    Status = "Pending",
                    Note = request.Note ?? string.Empty,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await appointmentRepo.AddAsync(newAppointment);
                await _unitOfWork.SaveChangesAsync();

                // 6. Cập nhật ChildVaccineProfile với vaccine/disease mới (nếu có thay đổi)
                var hasVaccineChange = selectedVaccineId != profile.VaccineId;
                var hasDiseaseChange = selectedDiseaseId != profile.DiseaseId;
                
                if (hasVaccineChange || hasDiseaseChange)
                {
                    _logger.LogInformation("🔄 Rebook với thay đổi: VaccineId {OldVaccine} → {NewVaccine}, DiseaseId {OldDisease} → {NewDisease}", 
                        profile.VaccineId, selectedVaccineId, profile.DiseaseId, selectedDiseaseId);
                        
                    // Validate vaccine mới có thể chữa disease mới không
                    var vaccineRepo = _unitOfWork.GetRepository<Vaccine>();
                    var newVaccine = await vaccineRepo.GetAsync(v => v.VaccineId == selectedVaccineId, "VaccineDiseases");
                    
                    if (newVaccine?.VaccineDiseases != null)
                    {
                        var canTreatNewDisease = newVaccine.VaccineDiseases.Any(vd => vd.DiseaseId == selectedDiseaseId);
                        if (!canTreatNewDisease)
                        {
                            return CreateErrorResponse<AppointmentRebookingResponseDTO>($"Vaccine được chọn không thể chữa bệnh được chọn. Vui lòng kiểm tra lại.");
                        }
                    }
                    
                    // Update VaccineId và DiseaseId
                    profile.VaccineId = selectedVaccineId;
                    profile.DiseaseId = selectedDiseaseId;
                }
                
                profile.AppointmentId = newAppointment.AppointmentId;
                profile.Status = "Pending"; // Cập nhật status từ "Scheduled" thành "Pending" khi có appointmentId
                profileRepo.Update(profile);

                // 7. Cập nhật BookedCount của Schedule
                schedule.BookedCount = (schedule.BookedCount ?? 0) + 1;
                scheduleRepo.Update(schedule);

                // 8. Tạo VaccinationAppointmentDetail cho TẤT CẢ trường hợp (có Order hoặc không)
                await CreateVaccinationAppointmentDetailForRebookingAsync(newAppointment, profile, schedule, usedOrder, request.Note);

                // 8.1. Decrease RemainingQuantity từ OrderDetail nếu sử dụng order
                if (selectedOrderDetail != null && usedOrder != null)
                {
                    selectedOrderDetail.RemainingQuantity -= 1;
                    var orderDetailRepo = _unitOfWork.GetRepository<OrderDetail>();
                    orderDetailRepo.Update(selectedOrderDetail);
                    
                    _logger.LogInformation("✅ Giảm 1 vaccine từ OrderDetail {OrderDetailId} cho rebook. RemainingQuantity: {Remaining}", 
                        selectedOrderDetail.OrderDetailId, selectedOrderDetail.RemainingQuantity);
                }

                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                // 9. Tính EstimatedCost từ giá đã snapshot + kiểm tra Order status
                decimal estimatedCostForResponse = 0;
                if (usedOrder != null)
                {
                    // Trường hợp có Order - kiểm tra status
                    if (usedOrder.Status == "Paid")
                    {
                        estimatedCostForResponse = 0; // Đã thanh toán
                        _logger.LogInformation("💰 Rebook EstimatedCost = 0 (Order {OrderId} đã Paid)", usedOrder.OrderId);
                    }
                    else
                    {
                        estimatedCostForResponse = usedOrder.TotalAmount; // Chưa thanh toán
                        _logger.LogInformation("💰 Rebook EstimatedCost from Order: {OrderId} Status={Status} = {TotalAmount}", 
                            usedOrder.OrderId, usedOrder.Status, estimatedCostForResponse);
                    }
                }
                else
                {
                    // Lấy giá đã snapshot từ VaccinationAppointmentDetail vừa tạo
                    var detailRepo = _unitOfWork.GetRepository<VaccinationAppointmentDetail>();
                    var appointmentDetail = await detailRepo.GetAsync(d => d.AppointmentId == newAppointment.AppointmentId);
                    
                    if (appointmentDetail?.FacilityVaccinePrice.HasValue == true)
                    {
                        estimatedCostForResponse = appointmentDetail.FacilityVaccinePrice.Value;
                        _logger.LogInformation("💰 Rebook EstimatedCost from snapshot: {Price}", estimatedCostForResponse);
                    }
                    else
                    {
                        // Fallback nếu không có snapshot
                        estimatedCostForResponse = validation.EstimatedCost;
                        _logger.LogWarning("⚠️ Rebook fallback to validation EstimatedCost: {Price}", estimatedCostForResponse);
                    }
                }
                
                // 10. Tạo response
                var response = new AppointmentRebookingResponseDTO
                {
                    AppointmentId = newAppointment.AppointmentId,
                    Status = newAppointment.Status,
                    CreatedAt = newAppointment.CreatedAt,
                    Note = newAppointment.Note,
                    Child = _mapper.Map<ChildDTO>(profile.Child),
                    Disease = _mapper.Map<DiseaseDTO>(profile.Disease),
                    Vaccine = _mapper.Map<VaccineDTO>(profile.Vaccine),
                    DoseNumber = profile.DoseNum,
                    Schedule = _mapper.Map<AppointmentScheduleDTO>(schedule),
                    EstimatedCost = estimatedCostForResponse, // ✅ Sử dụng giá đã snapshot
                    UsedExistingOrder = usedOrder != null,
                    UsedOrder = usedOrder != null ? _mapper.Map<OrderDTO>(usedOrder) : null,
                    RemainingVaccinesInOrder = remainingVaccines,
                    Message = usedOrder != null 
                        ? $"Đặt lịch thành công sử dụng gói đã mua. Còn lại {remainingVaccines} vaccine trong gói."
                        : "Đặt lịch thành công. Vui lòng thanh toán tại cơ sở."
                };

                _logger.LogInformation("Rebooking successful for ChildVaccineProfile {ProfileId}. AppointmentId: {AppointmentId}", 
                    request.ChildVaccineProfileId, newAppointment.AppointmentId);

                return CreateSuccessResponse(response, "Đặt lại lịch tiêm thành công");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi rebooking cho ChildVaccineProfile {ProfileId}", request.ChildVaccineProfileId);
                return CreateErrorResponse<AppointmentRebookingResponseDTO>($"Có lỗi xảy ra khi đặt lại lịch: {ex.Message}");
            }
        }

        /// <summary>
        /// Tạo VaccinationAppointmentDetail cho rebooking - hỗ trợ TẤT CẢ trường hợp (có Order hoặc không)
        /// </summary>
        private async Task CreateVaccinationAppointmentDetailForRebookingAsync(
            VaccinationAppointment newAppointment, 
            ChildVaccineProfile profile, 
            AppointmentSchedule schedule, 
            Order? usedOrder, 
            string? note)
        {
            try
            {
                _logger.LogInformation("🎯 Tạo VaccinationAppointmentDetail cho rebooking - Appointment {AppointmentId}, Vaccine {VaccineId}", 
                    newAppointment.AppointmentId, profile.VaccineId);

                var appointmentDetailRepo = _unitOfWork.GetRepository<VaccinationAppointmentDetail>();

                // 🎯 Lấy giá hiện tại của FacilityVaccine để snapshot
                decimal? facilityVaccinePrice = null;
                var facilityVaccineRepo = _unitOfWork.GetRepository<FacilityVaccine>();
                try
                {
                    var facilityVaccine = await facilityVaccineRepo.GetAsync(
                        fv => fv.FacilityId == schedule.FacilityId && fv.VaccineId == profile.VaccineId,
                        includeProperties: "Vaccine");
                    
                    if (facilityVaccine != null)
                    {
                        facilityVaccinePrice = facilityVaccine.Price;
                        _logger.LogInformation("💰 Rebook - Snapshot giá cho VaccineId {VaccineId}: {Price}", 
                            profile.VaccineId, facilityVaccinePrice);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Lỗi khi lấy giá FacilityVaccine cho rebook VaccineId {VaccineId}", profile.VaccineId);
                }
                
                // Luôn tạo VaccinationAppointmentDetail cho vaccine từ ChildVaccineProfile
                var appointmentDetail = new VaccinationAppointmentDetail
                {
                    AppointmentId = newAppointment.AppointmentId,
                    VaccineId = profile.VaccineId,
                    DoseNumber = profile.DoseNum.ToString(),
                    VaccinationDate = schedule.Date, // Ngày tiêm dự kiến từ schedule
                    FacilityVaccinePrice = facilityVaccinePrice, // 🎯 LƯU GIÁ TẠI THỜI ĐIỂM REBOOK
                    Notes = note ?? "Rebooked appointment",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await appointmentDetailRepo.AddAsync(appointmentDetail);
                
                _logger.LogInformation("✅ Đã tạo VaccinationAppointmentDetail - AppointmentId {AppointmentId}, VaccineId {VaccineId}, DoseNumber {DoseNumber}, UsedOrder: {HasOrder}", 
                    newAppointment.AppointmentId, profile.VaccineId, profile.DoseNum, usedOrder != null);

                // Nếu không dùng Order, cần kiểm tra cơ sở có vaccine này không
                if (usedOrder == null)
                {
                    // Reuse facilityVaccineRepo đã khai báo ở trên
                    var facilityVaccine = await facilityVaccineRepo.GetAsync(
                        fv => fv.FacilityId == schedule.FacilityId 
                           && fv.Vaccine.VaccineId == profile.VaccineId
                           && fv.Status == "active"
                           && fv.AvailableQuantity > 0,
                        includeProperties: "Vaccine"
                    );

                    if (facilityVaccine == null)
                    {
                        _logger.LogWarning("⚠️ Cơ sở {FacilityId} không có vaccine {VaccineId} khả dụng cho rebooking", 
                            schedule.FacilityId, profile.VaccineId);
                        
                        // Tìm tất cả cơ sở có vaccine này để log thông tin
                        var allFacilityVaccines = await facilityVaccineRepo.FindAsync(
                            fv => fv.Vaccine.VaccineId == profile.VaccineId
                               && fv.Status == "active"
                               && fv.AvailableQuantity > 0,
                            includeProperties: "Facility,Vaccine"
                        );

                        if (allFacilityVaccines.Any())
                        {
                            var availableFacilities = string.Join(", ", allFacilityVaccines.Select(fv => fv.Facility.FacilityName));
                            _logger.LogInformation("📍 Các cơ sở có vaccine {VaccineId}: {Facilities}", 
                                profile.VaccineId, availableFacilities);
                        }
                    }
                    else
                    {
                        _logger.LogInformation("✅ Cơ sở {FacilityId} có vaccine {VaccineId} khả dụng (Quantity: {Quantity})", 
                            schedule.FacilityId, profile.VaccineId, facilityVaccine.AvailableQuantity);
                    }
                }
                else
                {
                    _logger.LogInformation("📋 Sử dụng Order {OrderId} cho vaccine {VaccineId}", 
                        usedOrder.OrderId, profile.VaccineId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi khi tạo VaccinationAppointmentDetail cho rebooking - Appointment {AppointmentId}, Vaccine {VaccineId}", 
                    newAppointment.AppointmentId, profile.VaccineId);
                throw;
            }
        }
    }
}