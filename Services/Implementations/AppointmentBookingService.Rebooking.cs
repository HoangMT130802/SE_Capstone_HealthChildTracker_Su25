using AutoMapper;
using Contracts.DTOs.Appointment;
using Contracts.DTOs.Child;
using Contracts.DTOs.ChildVaccineProfile;
using Contracts.DTOs.Disease;
using Contracts.DTOs.Order;
using Contracts.DTOs.Vaccine;
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
            var applicableOrder = await orderRepo.GetAsync(
                o => o.MemberId == member.MemberId 
                  && o.Status == "Paid" 
                  && o.OrderDetails.Any(od => od.FacilityVaccine.VaccineId == profile.VaccineId 
                                            && od.DiseaseId == profile.DiseaseId 
                                            && od.RemainingQuantity > 0),
                includeProperties: "OrderDetails,OrderDetails.FacilityVaccine,OrderDetails.Disease"
            );

            bool hasApplicableOrder = applicableOrder != null;
            int availableQuantity = 0;
            decimal estimatedCost = 0;

            if (hasApplicableOrder)
            {
                var relevantOrderDetail = applicableOrder.OrderDetails
                    .FirstOrDefault(od => od.FacilityVaccine.VaccineId == profile.VaccineId 
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
                var cheapestFacilityVaccine = await facilityVaccineRepo.FindAsync(
                    fv => fv.Vaccine.VaccineId == profile.VaccineId 
                       && fv.Status == "Available" 
                       && fv.AvailableQuantity > 0
                );
                
                if (cheapestFacilityVaccine.Any())
                {
                    estimatedCost = cheapestFacilityVaccine.Min(fv => fv.Price);
                }
                else
                {
                    return new AppointmentRebookingValidationDTO
                    {
                        CanRebook = false,
                        ReasonCannotRebook = "Hiện tại không có cơ sở nào cung cấp vaccine này",
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

        public async Task<AppointmentRebookingResponseDTO> RebookAppointmentAsync(AppointmentRebookingRequestDTO request, int accountId)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                _logger.LogInformation("Processing rebooking request for ChildVaccineProfile {ProfileId} by Account {AccountId}", 
                    request.ChildVaccineProfileId, accountId);

                // 1. Validate request trước
                var validation = await ValidateRebookingRequestAsync(request.ChildVaccineProfileId, accountId);
                if (!validation.CanRebook)
                {
                    throw new InvalidOperationException(validation.ReasonCannotRebook);
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
                    throw new ArgumentException("Không tìm thấy lịch trống");
                }

                // 3. Kiểm tra lịch trống còn slot không
                if (schedule.BookedCount >= schedule.Slot.MaxCapacity)
                {
                    throw new InvalidOperationException("Lịch này đã hết chỗ");
                }

                // 4. Xử lý Order (nếu có gói áp dụng)
                Order usedOrder = null;
                int? remainingVaccines = null;
                
                if (validation.HasApplicableOrder && validation.ApplicableOrder != null)
                {
                    var orderRepo = _unitOfWork.GetRepository<Order>();
                    usedOrder = await orderRepo.GetAsync(
                        o => o.OrderId == validation.ApplicableOrder.OrderId,
                        includeProperties: "OrderDetails,OrderDetails.FacilityVaccine"
                    );

                    if (usedOrder != null)
                    {
                        var relevantOrderDetail = usedOrder.OrderDetails
                            .FirstOrDefault(od => od.FacilityVaccine.VaccineId == profile.VaccineId 
                                               && od.DiseaseId == profile.DiseaseId);
                        
                        if (relevantOrderDetail != null && relevantOrderDetail.RemainingQuantity > 0)
                        {
                            // Trừ 1 vaccine từ gói
                            relevantOrderDetail.RemainingQuantity -= 1;
                            remainingVaccines = relevantOrderDetail.RemainingQuantity;
                            
                            var orderDetailRepo = _unitOfWork.GetRepository<OrderDetail>();
                            orderDetailRepo.Update(relevantOrderDetail);
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
                    Note = request.Note,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await appointmentRepo.AddAsync(newAppointment);
                await _unitOfWork.SaveChangesAsync();

                // 6. Cập nhật ChildVaccineProfile với AppointmentId
                profile.AppointmentId = newAppointment.AppointmentId;
                profileRepo.Update(profile);

                // 7. Cập nhật BookedCount của Schedule
                schedule.BookedCount = (schedule.BookedCount ?? 0) + 1;
                scheduleRepo.Update(schedule);

                // 8. Tạo VaccinationAppointmentDetail nếu không dùng gói
                if (usedOrder == null)
                {
                    // Tìm FacilityVaccine tại cơ sở này
                    var facilityVaccineRepo = _unitOfWork.GetRepository<FacilityVaccine>();
                    var facilityVaccine = await facilityVaccineRepo.GetAsync(
                        fv => fv.FacilityId == schedule.FacilityId 
                           && fv.Vaccine.VaccineId == profile.VaccineId
                           && fv.Status == "Available"
                           && fv.AvailableQuantity > 0
                    );

                    if (facilityVaccine != null)
                    {
                        var appointmentDetailRepo = _unitOfWork.GetRepository<VaccinationAppointmentDetail>();
                        var appointmentDetail = new VaccinationAppointmentDetail
                        {
                            AppointmentId = newAppointment.AppointmentId,
                            VaccineId = profile.VaccineId,
                            DoseNumber = profile.DoseNum.ToString(),
                            VaccinationDate = DateOnly.FromDateTime(DateTime.UtcNow), // Ngày tiêm dự kiến
                            Notes = request.Note,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        await appointmentDetailRepo.AddAsync(appointmentDetail);
                    }
                }

                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                // 9. Tạo response
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
                    EstimatedCost = validation.EstimatedCost,
                    UsedExistingOrder = usedOrder != null,
                    UsedOrder = usedOrder != null ? _mapper.Map<OrderDTO>(usedOrder) : null,
                    RemainingVaccinesInOrder = remainingVaccines,
                    Message = usedOrder != null 
                        ? $"Đặt lịch thành công sử dụng gói đã mua. Còn lại {remainingVaccines} vaccine trong gói."
                        : "Đặt lịch thành công. Vui lòng thanh toán tại cơ sở."
                };

                _logger.LogInformation("Rebooking successful for ChildVaccineProfile {ProfileId}. AppointmentId: {AppointmentId}", 
                    request.ChildVaccineProfileId, newAppointment.AppointmentId);

                return response;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi khi rebooking cho ChildVaccineProfile {ProfileId}", request.ChildVaccineProfileId);
                throw;
            }
        }
    }
}