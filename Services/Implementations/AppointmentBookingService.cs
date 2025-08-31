using AutoMapper;
using Contracts.DTOs.Appointment;
using Contracts.DTOs.Child;
using Contracts.DTOs.VaccinePackage;
using Contracts.DTOs.VaccinationFacility;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repositories.Entities;
using Repositories.Interfaces;
using Services.Interfaces;
using Contracts.DTOs.Order;
using Contracts.DTOs.FacilityVaccine;
using Contracts.DTOs.Dashboard;
using Contracts.DTOs.Models;

namespace Services.Implementations
{
    public partial class AppointmentBookingService : IAppointmentBookingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<AppointmentBookingService> _logger;

        public AppointmentBookingService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<AppointmentBookingService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        #region Search & Filter Methods

        public async Task<FacilitySearchByDiseaseDTO> SearchFacilitiesByDiseaseAsync(int diseaseId, AppointmentSearchFiltersDTO? filters = null)
        {
            try
            {
                _logger.LogInformation("Tìm kiếm cơ sở theo bệnh {DiseaseId}", diseaseId);

                // Lấy thông tin disease
                var diseaseRepo = _unitOfWork.GetRepository<Disease>();
                var disease = await diseaseRepo.GetByIdAsync(diseaseId);
                if (disease == null)
                {
                    throw new ArgumentException($"Không tìm thấy bệnh với ID {diseaseId}");
                }

                // Query cơ sở có vaccine cho bệnh này
                var facilityRepo = _unitOfWork.GetRepository<VaccinationFacility>();
                var allFacilities = await facilityRepo.GetAllAsync("");

                var facilitiesWithVaccines = new List<VaccinationFacilityWithVaccinesDTO>();

                foreach (var facility in allFacilities)
                {
                    // Đếm vaccine có thể điều trị bệnh này tại cơ sở
                    var facilityVaccineRepo = _unitOfWork.GetRepository<FacilityVaccine>();
                    var facilityVaccines = await facilityVaccineRepo.GetAllAsync("");

                    var vaccineCount = facilityVaccines
                        .Where(fv => fv.FacilityId == facility.FacilityId &&
                                   fv.Vaccine.VaccineDiseases.Any(vd => vd.DiseaseId == diseaseId) &&
                                   fv.Status == "active" && fv.AvailableQuantity > 0)
                        .Count();

                    if (vaccineCount > 0)
                    {
                        var prices = facilityVaccines
                            .Where(fv => fv.FacilityId == facility.FacilityId &&
                                       fv.Vaccine.VaccineDiseases.Any(vd => vd.DiseaseId == diseaseId) &&
                                       fv.Status == "active")
                            .Select(fv => fv.Price);

                        // Kiểm tra có gói vaccine không
                        var packageRepo = _unitOfWork.GetRepository<VaccinePackage>();
                        var packages = await packageRepo.GetAllAsync("");
                        var hasPackages = packages.Any(p => p.FacilityId == facility.FacilityId &&
                                                          p.Status == "active" &&
                                                          p.PackageVaccines.Any(pv => pv.DiseaseId == diseaseId));

                        var facilityWithVaccines = _mapper.Map<VaccinationFacilityWithVaccinesDTO>(facility);
                        facilityWithVaccines.AvailableVaccineCount = vaccineCount;
                        facilityWithVaccines.MinPrice = prices.Any() ? prices.Min() : 0;
                        facilityWithVaccines.MaxPrice = prices.Any() ? prices.Max() : 0;
                        facilityWithVaccines.HasPackages = hasPackages;

                        facilitiesWithVaccines.Add(facilityWithVaccines);
                    }
                }

                // Apply filters if provided
                if (filters != null)
                {
                    if (filters.MinPrice.HasValue)
                        facilitiesWithVaccines = facilitiesWithVaccines.Where(f => f.MaxPrice >= filters.MinPrice.Value).ToList();

                    if (filters.MaxPrice.HasValue)
                        facilitiesWithVaccines = facilitiesWithVaccines.Where(f => f.MinPrice <= filters.MaxPrice.Value).ToList();

                    if (filters.HasPackagesOnly == true)
                        facilitiesWithVaccines = facilitiesWithVaccines.Where(f => f.HasPackages).ToList();
                }

                // Sort facilities
                var sortBy = filters?.SortBy ?? FacilitySortBy.Name;
                facilitiesWithVaccines = sortBy switch
                {
                    FacilitySortBy.Price => facilitiesWithVaccines.OrderBy(f => f.MinPrice).ToList(),
                    FacilitySortBy.Name => facilitiesWithVaccines.OrderBy(f => f.FacilityName).ToList(),
                    _ => facilitiesWithVaccines.OrderBy(f => f.FacilityName).ToList()
                };

                return new FacilitySearchByDiseaseDTO
                {
                    DiseaseId = diseaseId,
                    DiseaseName = disease.Name,
                    Facilities = facilitiesWithVaccines
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tìm kiếm cơ sở theo bệnh {DiseaseId}", diseaseId);
                throw;
            }
        }

        public async Task<FacilityVaccinesByDiseaseDTO> GetFacilityVaccinesByDiseaseAsync(int facilityId, int diseaseId)
        {
            try
            {
                _logger.LogInformation("Lấy vaccine của cơ sở {FacilityId} cho bệnh {DiseaseId}", facilityId, diseaseId);

                // Lấy thông tin facility
                var facilityRepo = _unitOfWork.GetRepository<VaccinationFacility>();
                var facility = await facilityRepo.GetByIdAsync(facilityId);
                if (facility == null)
                {
                    throw new ArgumentException($"Không tìm thấy cơ sở với ID {facilityId}");
                }

                // Lấy thông tin disease
                var diseaseRepo = _unitOfWork.GetRepository<Disease>();
                var disease = await diseaseRepo.GetByIdAsync(diseaseId);
                if (disease == null)
                {
                    throw new ArgumentException($"Không tìm thấy bệnh với ID {diseaseId}");
                }

                // Lấy facility vaccines cho bệnh này
                var facilityVaccineRepo = _unitOfWork.GetRepository<FacilityVaccine>();
                var facilityVaccines = await facilityVaccineRepo.GetAllAsync("");

                var relevantVaccines = facilityVaccines
                    .Where(fv => fv.FacilityId == facilityId &&
                               fv.Vaccine.VaccineDiseases.Any(vd => vd.DiseaseId == diseaseId))
                    .ToList();

                var individualVaccines = _mapper.Map<List<FacilityVaccineForBookingDTO>>(relevantVaccines);

                // Lấy vaccine packages
                var packageRepo = _unitOfWork.GetRepository<VaccinePackage>();
                var packages = await packageRepo.GetAllAsync("");

                var relevantPackages = packages
                    .Where(p => p.FacilityId == facilityId &&
                              p.PackageVaccines.Any(pv => pv.DiseaseId == diseaseId))
                    .ToList();

                var vaccinePackages = _mapper.Map<List<VaccinePackageForBookingDTO>>(relevantPackages);

                // Set RelevantVaccineCount for packages
                foreach (var package in vaccinePackages)
                {
                    var relevantCount = relevantPackages
                        .FirstOrDefault(p => p.PackageId == package.PackageId)?
                        .PackageVaccines?.Count(pv => pv.DiseaseId == diseaseId) ?? 0;
                    package.RelevantVaccineCount = relevantCount;
                }

                return new FacilityVaccinesByDiseaseDTO
                {
                    FacilityId = facilityId,
                    FacilityName = facility.FacilityName,
                    DiseaseId = diseaseId,
                    DiseaseName = disease.Name,
                    IndividualVaccines = individualVaccines,
                    VaccinePackages = vaccinePackages
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy vaccine của cơ sở {FacilityId} cho bệnh {DiseaseId}", facilityId, diseaseId);
                throw;
            }
        }

        public async Task<AvailableSchedulesDTO> GetAvailableSchedulesAsync(int facilityId, DateOnly fromDate, DateOnly toDate, List<string>? preferredTimeSlots = null)
        {
            try
            {
                _logger.LogInformation("Lấy lịch trống của cơ sở {FacilityId} từ {FromDate} đến {ToDate}", facilityId, fromDate, toDate);

                // Lấy thông tin facility
                var facilityRepo = _unitOfWork.GetRepository<VaccinationFacility>();
                var facility = await facilityRepo.GetByIdAsync(facilityId);
                if (facility == null)
                {
                    throw new ArgumentException($"Không tìm thấy cơ sở với ID {facilityId}");
                }

                // Lấy appointment schedules trong khoảng thời gian
                var scheduleRepo = _unitOfWork.GetRepository<AppointmentSchedule>();
                var schedules = await scheduleRepo.GetAllAsync("Slot,Facility");

                _logger.LogInformation("Lấy được {Count} schedules từ database", schedules.Count());

                // Reset BookedCount về 0 cho tất cả schedules để tránh giá trị corrupt từ database
                foreach (var schedule in schedules)
                {
                    schedule.BookedCount = 0;
                }

                // Tính BookedCount tự động từ appointments thực tế
                var schedulesWithBookedCount = await CalculateBookedCountForSchedules(schedules.ToList());

                _logger.LogInformation("Tính BookedCount xong cho {Count} schedules", schedulesWithBookedCount.Count);

                _logger.LogInformation("Filtering schedules: facilityId={FacilityId}, fromDate={FromDate}, toDate={ToDate}",
                    facilityId, fromDate, toDate);

                var availableSchedules = schedulesWithBookedCount
                    .Where(s => s.FacilityId == facilityId &&
                              s.Date >= fromDate && s.Date <= toDate &&
                              s.Status == "Available" &&
                              s.Slot != null && // Đảm bảo Slot không null
                              (s.Slot.MaxCapacity - (s.BookedCount ?? 0)) > 0 &&
                              IsSlotBookable(s)) // Kiểm tra thời gian booking
                    .OrderBy(s => s.Date)
                    .ThenBy(s => s.Slot.StartTime)
                    .ToList();

                _logger.LogInformation("Lọc được {Count} available schedules", availableSchedules.Count);

                // Log từng schedule để debug
                foreach (var schedule in schedulesWithBookedCount.Where(s => s.FacilityId == facilityId))
                {
                    _logger.LogDebug("Schedule {ScheduleId}: Date={Date}, Status={Status}, BookedCount={BookedCount}, MaxCapacity={MaxCapacity}, Slot={SlotStatus}",
                        schedule.ScheduleId, schedule.Date, schedule.Status, schedule.BookedCount,
                        schedule.Slot?.MaxCapacity ?? 0, schedule.Slot != null ? "OK" : "NULL");
                }

                // Group by date
                var dailySchedules = new List<DailyScheduleDTO>();
                for (var date = fromDate; date <= toDate; date = date.AddDays(1))
                {
                    var daySchedules = availableSchedules.Where(s => s.Date == date).ToList();

                    var availableSlots = new List<AvailableSlotDTO>();
                    foreach (var schedule in daySchedules)
                    {
                        // Filter by preferred time slots if provided
                        if (preferredTimeSlots != null && preferredTimeSlots.Any())
                        {
                            if (!preferredTimeSlots.Contains(schedule.Slot.SlotTime))
                                continue;
                        }

                        availableSlots.Add(new AvailableSlotDTO
                        {
                            ScheduleId = schedule.ScheduleId,
                            SlotId = schedule.SlotId,
                            SlotTime = schedule.Slot.SlotTime,
                            MaxCapacity = schedule.Slot.MaxCapacity,
                            BookedCount = schedule.BookedCount ?? 0,
                            AvailableCapacity = schedule.Slot.MaxCapacity - (schedule.BookedCount ?? 0),
                            Status = schedule.Status
                        });
                    }

                    dailySchedules.Add(new DailyScheduleDTO
                    {
                        Date = date,
                        DayOfWeek = date.ToString("dddd"),
                        IsAvailable = availableSlots.Any(),
                        AvailableSlots = availableSlots
                    });
                }

                return new AvailableSchedulesDTO
                {
                    FacilityId = facilityId,
                    FacilityName = facility.FacilityName,
                    FromDate = fromDate,
                    ToDate = toDate,
                    DailySchedules = dailySchedules
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy lịch trống của cơ sở {FacilityId}", facilityId);
                throw;
            }
        }

        #endregion

        #region Validation Methods

        public async Task<AppointmentValidationDTO> ValidateBookingRequestAsync(AppointmentBookingRequestDTO request)
        {
            try
            {
                _logger.LogInformation("Validation đặt lịch cho trẻ {ChildId}", request.ChildId);

                var validation = new AppointmentValidationDTO { CanBook = true };

                // ✅ Validate disease selection - chỉ hỗ trợ 1 bệnh
                if (!request.DiseaseId.HasValue)
                {
                    validation.CanBook = false;
                    validation.Errors.Add(new ValidationErrorDTO
                    {
                        Code = "NO_DISEASE_SELECTED",
                        Message = "Phải chọn 1 bệnh",
                        Field = "DiseaseId",
                        Severity = ValidationSeverity.Error
                    });
                    return validation;
                }
                var diseaseId = request.DiseaseId.Value;

                // Validate child exists
                var childRepo = _unitOfWork.GetRepository<Child>();
                var child = await childRepo.GetByIdAsync(request.ChildId);
                if (child == null)
                {
                    validation.CanBook = false;
                    validation.Errors.Add(new ValidationErrorDTO
                    {
                        Code = "CHILD_NOT_FOUND",
                        Message = "Không tìm thấy thông tin trẻ",
                        Field = "ChildId",
                        Severity = ValidationSeverity.Error
                    });
                }

                // Validate schedule exists and available
                var scheduleRepo = _unitOfWork.GetRepository<AppointmentSchedule>();
                var schedule = await scheduleRepo.GetAsync(s => s.ScheduleId == request.ScheduleId, "Slot");
                if (schedule == null)
                {
                    validation.CanBook = false;
                    validation.Errors.Add(new ValidationErrorDTO
                    {
                        Code = "SCHEDULE_NOT_FOUND",
                        Message = "Không tìm thấy lịch hẹn",
                        Field = "ScheduleId",
                        Severity = ValidationSeverity.Error
                    });
                }
                else if (schedule.Status != "Available")
                {
                    validation.CanBook = false;
                    validation.Errors.Add(new ValidationErrorDTO
                    {
                        Code = "SCHEDULE_NOT_AVAILABLE",
                        Message = "Lịch hẹn không khả dụng",
                        Field = "ScheduleId",
                        Severity = ValidationSeverity.Error
                    });
                }
                else
                {
                    // ✅ Kiểm tra BookedCount có vượt quá MaxCapacity không
                    var currentBookedCount = schedule.BookedCount ?? 0;
                    var maxCapacity = schedule.Slot?.MaxCapacity ?? 0;

                    if (currentBookedCount >= maxCapacity)
                    {
                        validation.CanBook = false;
                        validation.Errors.Add(new ValidationErrorDTO
                        {
                            Code = "SCHEDULE_FULL",
                            Message = $"Lịch hẹn đã hết chỗ (Đã đặt: {currentBookedCount}/{maxCapacity})",
                            Field = "ScheduleId",
                            Severity = ValidationSeverity.Error
                        });
                    }
                    else
                    {
                        // Validate booking time - không được book trong quá khứ
                        if (schedule.Slot.StartTime.HasValue)
                        {
                            var slotDateTime = schedule.Date.ToDateTime(schedule.Slot.StartTime.Value);
                            var now = DateTime.Now;

                            // Chỉ kiểm tra không được book trong quá khứ (khi slot đã bắt đầu)
                            if (slotDateTime < now)
                            {
                                validation.CanBook = false;
                                validation.Errors.Add(new ValidationErrorDTO
                                {
                                    Code = "BOOKING_IN_PAST",
                                    Message = "Không thể đặt lịch khi slot đã bắt đầu hoặc đã qua",
                                    Field = "ScheduleId",
                                    Severity = ValidationSeverity.Error
                                });
                            }
                        }
                    }
                }

                // Validate vaccine selection - chỉ được chọn 1 trong 3 options
                var hasOrderId = request.OrderId.HasValue && request.OrderId.Value > 0;
                var hasPackageId = request.PackageId.HasValue && request.PackageId.Value > 0;
                var hasFacilityVaccineIds = request.FacilityVaccineIds != null && request.FacilityVaccineIds.Any();

                var selectionCount = (hasOrderId ? 1 : 0) + (hasPackageId ? 1 : 0) + (hasFacilityVaccineIds ? 1 : 0);

                if (selectionCount == 0)
                {
                    validation.CanBook = false;
                    validation.Errors.Add(new ValidationErrorDTO
                    {
                        Code = "NO_VACCINE_SELECTED",
                        Message = "Phải chọn 1 trong 3: Order đã mua, gói vaccine mới, hoặc vaccine lẻ",
                        Field = "OrderId,PackageId,FacilityVaccineIds",
                        Severity = ValidationSeverity.Error
                    });
                }
                else if (selectionCount > 1)
                {
                    validation.CanBook = false;
                    validation.Errors.Add(new ValidationErrorDTO
                    {
                        Code = "MULTIPLE_VACCINE_SELECTION",
                        Message = "Chỉ được chọn 1 trong 3: Order đã mua, gói vaccine mới, hoặc vaccine lẻ",
                        Field = "OrderId,PackageId,FacilityVaccineIds",
                        Severity = ValidationSeverity.Error
                    });
                }

                // ✅ Validate vaccine lẻ với disease nếu được chọn
                if (hasFacilityVaccineIds)
                {
                    var facilityVaccineRepo = _unitOfWork.GetRepository<FacilityVaccine>();
                    var invalidVaccines = new List<string>();

                    foreach (var facilityVaccineId in request.FacilityVaccineIds)
                    {
                        var facilityVaccine = await facilityVaccineRepo.GetAsync(
                            fv => fv.FacilityVaccineId == facilityVaccineId,
                            includeProperties: "Vaccine,Vaccine.VaccineDiseases");

                        if (facilityVaccine == null)
                        {
                            validation.CanBook = false;
                            validation.Errors.Add(new ValidationErrorDTO
                            {
                                Code = "FACILITY_VACCINE_NOT_FOUND",
                                Message = $"Không tìm thấy vaccine với ID {facilityVaccineId}",
                                Field = "FacilityVaccineIds",
                                Severity = ValidationSeverity.Error
                            });
                            break;
                        }

                        // Kiểm tra vaccine có điều trị được disease không (single disease)
                        var canTreatDisease = facilityVaccine.Vaccine?.VaccineDiseases?.Any(vd => vd.DiseaseId == diseaseId) ?? false;
                        if (!canTreatDisease)
                        {
                            invalidVaccines.Add(facilityVaccine.Vaccine?.Name ?? $"Vaccine {facilityVaccineId}");
                        }
                    }

                    if (invalidVaccines.Any())
                    {
                        validation.CanBook = false;
                        validation.Errors.Add(new ValidationErrorDTO
                        {
                            Code = "VACCINE_DISEASE_MISMATCH",
                            Message = $"Các vaccine sau không điều trị được bệnh đã chọn: {string.Join(", ", invalidVaccines)}",
                            Field = "FacilityVaccineIds",
                            Severity = ValidationSeverity.Error
                        });
                    }
                }

                // Validate Order nếu được chọn
                if (hasOrderId)
                {
                    var orderRepo = _unitOfWork.GetRepository<Order>();
                    var order = await orderRepo.GetAsync(
                        o => o.OrderId == request.OrderId.Value,
                        "Member,OrderDetails,OrderDetails.FacilityVaccine,OrderDetails.FacilityVaccine.Facility");

                    if (order == null)
                    {
                        validation.CanBook = false;
                        validation.Errors.Add(new ValidationErrorDTO
                        {
                            Code = "ORDER_NOT_FOUND",
                            Message = "Không tìm thấy Order",
                            Field = "OrderId",
                            Severity = ValidationSeverity.Error
                        });
                    }

                    else
                    {
                        // Kiểm tra Order có thuộc về Member của Child không
                        var memberRepo = _unitOfWork.GetRepository<Member>();
                        var member = await memberRepo.GetAsync(m => m.MemberId == child.MemberId);
                        if (member != null && order.MemberId != member.MemberId)
                        {
                            validation.CanBook = false;
                            validation.Errors.Add(new ValidationErrorDTO
                            {
                                Code = "ORDER_NOT_OWNED",
                                Message = "Order không thuộc về Member này",
                                Field = "OrderId",
                                Severity = ValidationSeverity.Error
                            });
                        }

                        // ✅ Xác thực OrderDetails chỉ cho 1 disease và facility đang đặt
                        if (validation.CanBook)
                        {
                            var facilityId = request.FacilityId;
                            
                            // Debug logging
                            _logger.LogInformation("Kiểm tra Order {OrderId} cho DiseaseId: {DiseaseId}, FacilityId: {FacilityId}", 
                                request.OrderId.Value, diseaseId, facilityId);
                            _logger.LogInformation("Order có {OrderDetailsCount} OrderDetails", order.OrderDetails?.Count ?? 0);
                            
                            if (order.OrderDetails != null)
                            {
                                foreach (var od in order.OrderDetails)
                                {
                                    _logger.LogInformation("OrderDetail - DiseaseId: {DiseaseId}, FacilityVaccineId: {FacilityVaccineId}, FacilityVaccine: {FacilityVaccine}, FacilityVaccine.FacilityId: {FacilityId}", 
                                        od.DiseaseId, od.FacilityVaccineId, od.FacilityVaccine != null ? "NotNull" : "NULL", od.FacilityVaccine?.FacilityId);
                                }
                            }
                            else
                            {
                                _logger.LogWarning("Order.OrderDetails is NULL!");
                            }
                            
                            // Thử cách khác: query trực tiếp từ database để tránh vấn đề navigation property
                            var orderDetailRepo = _unitOfWork.GetRepository<OrderDetail>();
                            var dbMatchedDetails = await orderDetailRepo.FindAsync(
                                od => od.OrderId == request.OrderId.Value 
                                   && od.DiseaseId == diseaseId
                                   && od.FacilityVaccine.FacilityId == facilityId,
                                "FacilityVaccine");

                            _logger.LogInformation("Query trực tiếp từ DB tìm thấy {Count} OrderDetail phù hợp", dbMatchedDetails.Count());

                            var matchedDetails = order.OrderDetails?
                                .Where(od => od.DiseaseId == diseaseId
                                          && od.FacilityVaccine != null
                                          && od.FacilityVaccine.FacilityId == facilityId)
                                .ToList() ?? new List<OrderDetail>();

                            _logger.LogInformation("Include navigation property tìm thấy {Count} OrderDetail phù hợp", matchedDetails.Count);
                            
                            // Sử dụng kết quả từ DB query trực tiếp nếu include không work
                            if (!matchedDetails.Any() && dbMatchedDetails.Any())
                            {
                                _logger.LogWarning("Include navigation property failed, using direct DB query result");
                                // Validation pass vì DB query tìm thấy matching details
                            }
                            else if (!matchedDetails.Any())
                            {
                                validation.CanBook = false;
                                validation.Errors.Add(new ValidationErrorDTO
                                {
                                    Code = "ORDER_DETAILS_NOT_MATCH_FACILITY_OR_DISEASE",
                                    Message = $"Order không có vaccine phù hợp cho DiseaseId: {diseaseId} tại cơ sở này",
                                    Field = "OrderId",
                                    Severity = ValidationSeverity.Error
                                });
                            }
                            else if (!matchedDetails.Any(od => od.RemainingQuantity > 0) && !dbMatchedDetails.Any(od => od.RemainingQuantity > 0))
                            {
                                validation.CanBook = false;
                                validation.Errors.Add(new ValidationErrorDTO
                                {
                                    Code = "ORDER_DETAILS_NO_REMAINING",
                                    Message = $"Order đã hết số mũi còn lại cho DiseaseId: {diseaseId}",
                                    Field = "OrderId",
                                    Severity = ValidationSeverity.Error
                                });
                            }
                        }
                    }
                }

                // ======================
                // Chặn double-book & spam & giữ số lượng gói (reservation logic kiểm tra)
                // ======================
                try
                {
                    // 1) Double-book theo CVP: nếu đã có CVP Pending/Scheduled và đã gắn AppointmentId cho cùng bệnh
                    // 🔧 COMMENTED FOR TESTING - Tạm thời tắt để test được đặt nhiều lịch cùng bệnh
                    /*
                    var childVaccineProfileRepo = _unitOfWork.GetRepository<ChildVaccineProfile>();
                    var existingPendingProfiles = await childVaccineProfileRepo.FindAsync(
                        p => p.ChildId == request.ChildId
                             && p.DiseaseId == diseaseId
                             && (p.Status == "Pending" || p.Status == "Scheduled")
                             && p.AppointmentId != null);

                    if (existingPendingProfiles.Any())
                    {
                        validation.CanBook = false;
                        validation.Errors.Add(new ValidationErrorDTO
                        {
                            Code = "ALREADY_BOOKED_SAME_DISEASE",
                            Message = "Đã có lịch đang chờ/đã lên lịch cho bệnh đã chọn. Không thể đặt trùng.",
                            Field = "DiseaseId",
                            Severity = ValidationSeverity.Error
                        });
                    }
                    */
                    
                    var childVaccineProfileRepo = _unitOfWork.GetRepository<ChildVaccineProfile>();

                    // 2) Anti-spam: nếu vừa tạo CVP Pending/Scheduled cho cùng bệnh trong 5 phút gần đây
                    // 🔧 COMMENTED FOR TESTING - Tạm thời tắt để test liên tục
                    /*
                    if (validation.CanBook)
                    {
                        var nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                        var windowSec = 5 * 60; // 5 phút
                        var recentPending = await childVaccineProfileRepo.FindAsync(
                            p => p.ChildId == request.ChildId
                                 && p.DiseaseId == diseaseId
                                 && (p.Status == "Pending" || p.Status == "Scheduled")
                                 && p.CreatedAt >= (nowUnix - windowSec));
                        if (recentPending.Any())
                        {
                            validation.CanBook = false;
                            validation.Errors.Add(new ValidationErrorDTO
                            {
                                Code = "BOOKING_RATE_LIMIT",
                                Message = "Bạn vừa đặt lịch cho bệnh này gần đây. Vui lòng thử lại sau ít phút.",
                                Field = "DiseaseId",
                                Severity = ValidationSeverity.Error
                            });
                        }
                    }
                    */

                    // 3) Giữ số lượng gói theo kiểm tra reserved (không trừ thật) khi dùng OrderId
                    if (validation.CanBook && hasOrderId)
                    {
                        var orderRepoForReserve = _unitOfWork.GetRepository<Order>();
                        var orderForReserve = await orderRepoForReserve.GetAsync(
                            o => o.OrderId == request.OrderId.Value,
                            includeProperties: "OrderDetails");

                        if (orderForReserve != null)
                        {
                            // Lấy các CVP đang Pending/Scheduled của Child và có Appointment sử dụng OrderId này
                            var pendingProfilesForChild = await childVaccineProfileRepo.FindAsync(
                                p => p.ChildId == request.ChildId
                                     && (p.Status == "Pending" || p.Status == "Scheduled")
                                     && p.AppointmentId != null
                                     && p.DiseaseId == diseaseId);

                            var appointmentRepo = _unitOfWork.GetRepository<VaccinationAppointment>();
                            var appointmentIds = pendingProfilesForChild
                                .Where(p => p.AppointmentId.HasValue)
                                .Select(p => p.AppointmentId!.Value)
                                .Distinct()
                                .ToList();

                            var relatedAppointments = appointmentIds.Any()
                                ? await appointmentRepo.FindAsync(a => appointmentIds.Contains(a.AppointmentId) && a.OrderId == request.OrderId.Value,
                                    includeProperties: "Schedule,Schedule.Slot")
                                : new List<VaccinationAppointment>();

                            // Đếm reserved theo bệnh (chỉ tính những appointment còn hiệu lực trong tương lai)
                            var now = DateTime.Now;
                            var activeAppointmentIds = new HashSet<int>(relatedAppointments
                                .Where(a => a.Status == "Pending" || a.Status == "Approval")
                                .Where(a =>
                                {
                                    var slotStart = a.Schedule?.Slot?.StartTime;
                                    if (slotStart.HasValue)
                                    {
                                        var slotDateTime = a.Schedule!.Date.ToDateTime(slotStart.Value);
                                        return slotDateTime > now;
                                    }
                                    // Nếu thiếu Slot, xét theo ngày
                                    return a.Schedule != null && a.Schedule.Date >= DateOnly.FromDateTime(DateTime.Today);
                                })
                                .Select(a => a.AppointmentId));

                            var totalRemaining = orderForReserve.OrderDetails
                                ?.Where(od => od.DiseaseId == diseaseId)
                                .Sum(od => od.RemainingQuantity) ?? 0;

                            var reservedCount = pendingProfilesForChild
                                .Where(p => p.DiseaseId == diseaseId && p.AppointmentId.HasValue && activeAppointmentIds.Contains(p.AppointmentId.Value))
                                .Count();

                            if (reservedCount >= totalRemaining)
                            {
                                validation.CanBook = false;
                                validation.Errors.Add(new ValidationErrorDTO
                                {
                                    Code = "ORDER_NO_AVAILABLE_REMAINING",
                                    Message = $"Gói đã hết số lượng khả dụng cho bệnh {diseaseId} do đã được giữ bởi các lịch đang chờ.",
                                    Field = "OrderId",
                                    Severity = ValidationSeverity.Error
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi kiểm tra double-book/spam/giữ số lượng gói trong validate booking");
                    // Không throw để không chặn các lỗi khác; chỉ log
                }

                return validation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi validation đặt lịch cho trẻ {ChildId}", request.ChildId);
                throw;
            }
        }

        public async Task<ChildVaccinationHistoryDTO> GetChildVaccinationHistoryAsync(int childId, int diseaseId)
        {
            try
            {
                _logger.LogInformation("Lấy lịch sử tiêm của trẻ {ChildId} cho bệnh {DiseaseId}", childId, diseaseId);

                // Get child info
                var childRepo = _unitOfWork.GetRepository<Child>();
                var child = await childRepo.GetByIdAsync(childId);
                if (child == null)
                {
                    throw new ArgumentException($"Không tìm thấy trẻ với ID {childId}");
                }

                // Get vaccination history
                var appointmentRepo = _unitOfWork.GetRepository<VaccinationAppointment>();
                var appointments = await appointmentRepo.GetAllAsync("");

                var childAppointments = appointments
                    .Where(a => a.ChildId == childId && a.Status == "Paid")
                    .ToList();

                var relatedVaccines = new List<string>();
                var lastVaccinationDate = (DateTime?)null;

                foreach (var appointment in childAppointments)
                {
                    var details = appointment.VaccinationAppointmentDetails
                        .Where(d => d.Vaccine.VaccineDiseases.Any(vd => vd.DiseaseId == diseaseId));

                    foreach (var detail in details)
                    {
                        relatedVaccines.Add($"{detail.Vaccine.Name} - {detail.DoseNumber}");
                        if (lastVaccinationDate == null || detail.VaccinationDate.ToDateTime(TimeOnly.MinValue) > lastVaccinationDate)
                        {
                            lastVaccinationDate = detail.VaccinationDate.ToDateTime(TimeOnly.MinValue);
                        }
                    }
                }

                return new ChildVaccinationHistoryDTO
                {
                    ChildId = childId,
                    ChildName = child.FullName,
                    LastVaccinationDate = lastVaccinationDate,
                    RelatedVaccinesReceived = relatedVaccines,
                    HasVaccineAllergies = false, // TODO: Implement allergy tracking
                    Allergies = new List<string>(),
                    RequiresDoctorConsultation = false
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy lịch sử tiêm của trẻ {ChildId}", childId);
                throw;
            }
        }

        #endregion

        #region Cost Calculation Methods

        public async Task<CostBreakdownDTO> CalculateEstimatedCostAsync(int facilityId, int? orderId = null, int? packageId = null, List<int>? facilityVaccineIds = null)
        {
            try
            {
                // ===== LOGIC ĐƠN GIẢN: CHỈ LẤY GIÁ GỐC, KHÔNG TÍNH THÊM GÌ =====
                _logger.LogInformation("Tính chi phí cho cơ sở {FacilityId} - Chỉ lấy giá gốc", facilityId);

                decimal totalCost = 0;
                var items = new List<CostItemDTO>();

                if (orderId.HasValue)
                {
                    // Existing Order → kiểm tra status để quyết định giá
                    var orderRepo = _unitOfWork.GetRepository<Order>();
                    var order = await orderRepo.GetAsync(o => o.OrderId == orderId.Value, "Package");
                    if (order != null)
                    {
                        if (order.Status == "Paid")
                        {
                            totalCost = 0; // Đã thanh toán
                            _logger.LogInformation("💰 CalculateEstimatedCost = 0 (Order {OrderId} đã Paid)", order.OrderId);
                        }
                        else
                        {
                            totalCost = order.TotalAmount; // Chưa thanh toán
                            _logger.LogInformation("💰 CalculateEstimatedCost from Order: {OrderId} Status={Status} = {TotalAmount}", 
                                order.OrderId, order.Status, totalCost);
                        }
                        
                        items.Add(new CostItemDTO
                        {
                            Name = order.Package?.Name ?? "Order Package",
                            Type = order.Status == "Paid" ? "Paid Order" : "Pending Order",
                            Quantity = 1,
                            UnitPrice = totalCost,
                            TotalPrice = totalCost
                        });
                    }
                }
                else if (packageId.HasValue)
                {
                    // Package → lấy đúng giá package
                    var packageRepo = _unitOfWork.GetRepository<VaccinePackage>();
                    var package = await packageRepo.GetAsync(p => p.PackageId == packageId.Value);
                    if (package != null)
                    {
                        totalCost = package.Price;
                        items.Add(new CostItemDTO
                        {
                            Name = package.Name,
                            Type = "Package",
                            Quantity = 1,
                            UnitPrice = package.Price,
                            TotalPrice = package.Price
                        });
                    }
                }
                else if (facilityVaccineIds != null && facilityVaccineIds.Any())
                {
                    // Vaccine lẻ → lấy đúng giá vaccine
                    var facilityVaccineRepo = _unitOfWork.GetRepository<FacilityVaccine>();
                    foreach (var vaccineId in facilityVaccineIds)
                    {
                        var facilityVaccine = await facilityVaccineRepo.GetAsync(
                            fv => fv.FacilityVaccineId == vaccineId,
                            includeProperties: "Vaccine");
                        if (facilityVaccine != null)
                        {
                            totalCost += facilityVaccine.Price;
                            items.Add(new CostItemDTO
                            {
                                Name = facilityVaccine.Vaccine?.Name ?? "Unknown Vaccine",
                                Type = "Individual Vaccine",
                                Quantity = 1,
                                UnitPrice = facilityVaccine.Price,
                                TotalPrice = facilityVaccine.Price
                            });
                        }
                    }
                }

                // ✅ CHỈ TRẢ VỀ GIÁ GỐC, KHÔNG TÍNH THÊM PHÍ GÌ CẢ
                return new CostBreakdownDTO
                {
                    VaccineCost = totalCost,
                    ServiceFee = 0,
                    BookingFee = 0,
                    Tax = 0,
                    Discount = 0,
                    TotalCost = totalCost,  // ✅ Tổng = giá gốc
                    Items = items
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tính chi phí cho cơ sở {FacilityId}", facilityId);
                throw;
            }
        }

        #endregion

        #region Helper Methods for Response

        /// <summary>
        /// Tạo success response
        /// </summary>
        private ResponseDataModel<T> CreateSuccessResponse<T>(T data, string message = "Thành công")
        {
            return new ResponseDataModel<T>
            {
                Status = true,
                Message = message,
                Data = data
            };
        }

        /// <summary>
        /// Tạo error response
        /// </summary>
        private ResponseDataModel<T> CreateErrorResponse<T>(string message, T data = default(T))
        {
            return new ResponseDataModel<T>
            {
                Status = false,
                Message = message,
                Data = data
            };
        }

        #endregion

        #region Booking Methods

        public async Task<ResponseDataModel<AppointmentBookingResponseDTO>> BookAppointmentAsync(AppointmentBookingRequestDTO request)
        {
            Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction = null;
            try
            {
                // Bắt đầu giao dịch để đảm bảo tính toàn vẹn dữ liệu xuyên suốt quá trình đặt lịch
                transaction = await _unitOfWork.BeginTransactionAsync();
                _logger.LogInformation("Đặt lịch cho trẻ {ChildId}, OrderId: {OrderId}, PackageId: {PackageId}, FacilityVaccineIds: {FacilityVaccineIds}",
                    request.ChildId, request.OrderId, request.PackageId, request.FacilityVaccineIds != null ? string.Join(",", request.FacilityVaccineIds) : "null");

                // Validate first
                var validation = await ValidateBookingRequestAsync(request);
                if (!validation.CanBook)
                {
                    var errors = string.Join(", ", validation.Errors.Select(e => e.Message));
                    _logger.LogWarning("Validation failed: {Errors}", errors);
                    return CreateErrorResponse<AppointmentBookingResponseDTO>(errors);
                }

                // Get child info (needed for both package and response)
                var childRepo = _unitOfWork.GetRepository<Child>();
                var child = await childRepo.GetAsync(c => c.ChildId == request.ChildId);
                if (child == null)
                {
                    return CreateErrorResponse<AppointmentBookingResponseDTO>($"Không tìm thấy trẻ với ID {request.ChildId}");
                }

                // Kiểm tra capacity của Schedule trước khi tạo appointment
                var scheduleRepoForCap = _unitOfWork.GetRepository<AppointmentSchedule>();
                var scheduleForCap = await scheduleRepoForCap.GetAsync(s => s.ScheduleId == request.ScheduleId, "Slot");
                if (scheduleForCap == null)
                {
                    return CreateErrorResponse<AppointmentBookingResponseDTO>($"Không tìm thấy schedule với ID {request.ScheduleId}");
                }
                if (scheduleForCap.Slot != null)
                {
                    var currentBooked = scheduleForCap.BookedCount ?? 0;
                    if (currentBooked >= scheduleForCap.Slot.MaxCapacity)
                    {
                        return CreateErrorResponse<AppointmentBookingResponseDTO>("Lịch này đã hết chỗ");
                    }
                }

                // Create VaccinationAppointment
                var appointment = new VaccinationAppointment
                {
                    ChildId = request.ChildId,
                    ScheduleId = request.ScheduleId,
                    Status = "Pending", // Đổi thành Pending để staff có thể duyệt
                    Note = request.Note ?? "",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // LUỒNG 1: Sử dụng Order đã mua
                if (request.OrderId.HasValue && request.OrderId.Value > 0)
                {
                    _logger.LogInformation("Xử lý đặt lịch với Order đã mua {OrderId}", request.OrderId.Value);

                    // Validate Order exists và đã paid
                    var orderRepo = _unitOfWork.GetRepository<Order>();
                    var existingOrder = await orderRepo.GetAsync(o => o.OrderId == request.OrderId.Value, "OrderDetails");
                    if (existingOrder == null)
                    {
                        return CreateErrorResponse<AppointmentBookingResponseDTO>($"Không tìm thấy Order với ID {request.OrderId.Value}");
                    }

                    // Thống nhất quy tắc giữ chỗ theo RemainingQuantity: chặn overbook theo bệnh được yêu cầu
                    var requestDiseaseIdsForOrder = new List<int>();
                    if (request.DiseaseId.HasValue)
                    {
                        requestDiseaseIdsForOrder.Add(request.DiseaseId.Value);
                    }
                    if (request.DiseaseIds != null && request.DiseaseIds.Any())
                    {
                        requestDiseaseIdsForOrder.AddRange(request.DiseaseIds);
                    }
                    // Nếu người dùng chưa truyền bệnh, cố gắng suy ra từ OrderDetails
                    if (!requestDiseaseIdsForOrder.Any())
                    {
                        requestDiseaseIdsForOrder = existingOrder.OrderDetails?.Select(od => od.DiseaseId).Distinct().ToList() ?? new List<int>();
                    }

                    if (requestDiseaseIdsForOrder.Any())
                    {
                        // Tổng remaining theo các bệnh sẽ sử dụng
                        var totalRemainingByDiseases = existingOrder.OrderDetails?
                            .Where(od => requestDiseaseIdsForOrder.Contains(od.DiseaseId))
                            .Sum(od => od.RemainingQuantity) ?? 0;

                        // Đếm số lịch đang giữ chỗ (Pending/Approval và còn ở tương lai) cho cùng child + order + các bệnh đó
                        var profileRepoCheck = _unitOfWork.GetRepository<ChildVaccineProfile>();
                        var appointmentRepoCheck = _unitOfWork.GetRepository<VaccinationAppointment>();
                        var childIdCheck = child.ChildId;

                        var pendingProfilesForChild = await profileRepoCheck.FindAsync(p =>
                            p.ChildId == childIdCheck &&
                            requestDiseaseIdsForOrder.Contains(p.DiseaseId) &&
                            p.AppointmentId != null);

                        var relatedIds = pendingProfilesForChild
                            .Where(p => p.AppointmentId.HasValue)
                            .Select(p => p.AppointmentId!.Value)
                            .Distinct()
                            .ToList();

                        var related = relatedIds.Any()
                            ? await appointmentRepoCheck.FindAsync(a => relatedIds.Contains(a.AppointmentId) && a.OrderId == existingOrder.OrderId, includeProperties: "Schedule,Schedule.Slot")
                            : new List<VaccinationAppointment>();

                        var now2 = DateTime.Now;
                        var reservedCount = related.Count(a =>
                        {
                            if (!(a.Status == "Pending" || a.Status == "Approval")) return false;
                            var start = a.Schedule?.Slot?.StartTime;
                            if (start.HasValue) return a.Schedule!.Date.ToDateTime(start.Value) > now2;
                            return a.Schedule != null && a.Schedule.Date >= DateOnly.FromDateTime(DateTime.Today);
                        });

                        if (reservedCount >= totalRemainingByDiseases)
                        {
                            return CreateErrorResponse<AppointmentBookingResponseDTO>("Gói đã hết số lượng khả dụng cho bệnh đã chọn do đã được giữ bởi các lịch đang chờ.");
                        }
                    }

                    appointment.OrderId = request.OrderId.Value;
                    _logger.LogInformation("Sử dụng Order đã có {OrderId} thành công", request.OrderId.Value);
                }
                // LUỒNG 2: Tạo Order mới nếu chọn gói vaccine
                else if (request.PackageId.HasValue && request.PackageId.Value > 0)
                {
                    _logger.LogInformation("Xử lý đặt lịch với gói vaccine mới {PackageId}", request.PackageId.Value);

                    var memberRepo = _unitOfWork.GetRepository<Member>();
                    var member = await memberRepo.GetAsync(m => m.MemberId == child.MemberId);
                    if (member == null)
                    {
                        return CreateErrorResponse<AppointmentBookingResponseDTO>($"Không tìm thấy member với ID {child.MemberId}");
                    }

                    var order = new Order
                    {
                        MemberId = member.MemberId,
                        PackageId = request.PackageId.Value,
                        OrderDate = DateTime.UtcNow,
                        TotalAmount = (await CalculateEstimatedCostAsync(request.FacilityId, null, request.PackageId, null)).TotalCost,
                        Status = "Pending",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    var orderRepo = _unitOfWork.GetRepository<Order>();
                    await orderRepo.AddAsync(order);
                    await _unitOfWork.SaveChangesAsync();
                    appointment.OrderId = order.OrderId;

                    _logger.LogInformation("Tạo Order mới thành công với ID {OrderId}", order.OrderId);
                }
                // LUỒNG 3: Vaccine lẻ
                else
                {
                    _logger.LogInformation("Xử lý đặt lịch với vaccine lẻ");
                    appointment.OrderId = null; // Không có Order cho vaccine lẻ
                }

                // Save appointment
                var appointmentRepo = _unitOfWork.GetRepository<VaccinationAppointment>();
                await appointmentRepo.AddAsync(appointment);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Tạo Appointment thành công với ID {AppointmentId}", appointment.AppointmentId);

                // Get schedule for VaccinationDate
                _logger.LogInformation("🔍 Lấy schedule để tạo VaccinationAppointmentDetail - ScheduleId: {ScheduleId}", request.ScheduleId);
                var scheduleRepo = _unitOfWork.GetRepository<AppointmentSchedule>();
                var schedule = await scheduleRepo.GetByIdAsync(request.ScheduleId);
                if (schedule == null)
                {
                    _logger.LogError("❌ KHÔNG TÌM THẤY SCHEDULE với ID {ScheduleId}", request.ScheduleId);
                    return CreateErrorResponse<AppointmentBookingResponseDTO>($"Không tìm thấy schedule với ID {request.ScheduleId}");
                }
                _logger.LogInformation("✅ Tìm thấy schedule {ScheduleId} - Date: {Date}", schedule.ScheduleId, schedule.Date);

                // LUỒNG 4: Tạo VaccinationAppointmentDetails cho TẤT CẢ các trường hợp
                _logger.LogInformation("🔥 BẮT ĐẦU gọi CreateVaccinationAppointmentDetailsAsync cho appointment {AppointmentId}", appointment.AppointmentId);
                try
                {
                    await CreateVaccinationAppointmentDetailsAsync(appointment, request, schedule.Date);
                    _logger.LogInformation("🔥 HOÀN THÀNH gọi CreateVaccinationAppointmentDetailsAsync cho appointment {AppointmentId}", appointment.AppointmentId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "💥 LỖI khi gọi CreateVaccinationAppointmentDetailsAsync cho appointment {AppointmentId}", appointment.AppointmentId);
                    // Tạo VaccinationAppointmentDetail fallback đơn giản
                    await CreateFallbackVaccinationAppointmentDetailAsync(appointment, request, schedule.Date);
                }

                // ✅ LUỒNG 5: Tạo ChildVaccineProfile với status "Pending" cho tất cả vaccines được book
                _logger.LogInformation("🔥 BẮT ĐẦU gọi CreateChildVaccineProfilesForAppointmentAsync cho appointment {AppointmentId}", appointment.AppointmentId);
                await CreateChildVaccineProfilesForAppointmentAsync(appointment, request, schedule.Date);
                _logger.LogInformation("🔥 HOÀN THÀNH gọi CreateChildVaccineProfilesForAppointmentAsync cho appointment {AppointmentId}", appointment.AppointmentId);

                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("🔥 HOÀN THÀNH SaveChangesAsync sau khi tạo ChildVaccineProfile và VaccinationAppointmentDetail");

                // Calculate estimated cost
                var estimatedCost = await CalculateEstimatedCostAsync(request.FacilityId, request.OrderId, request.PackageId, request.FacilityVaccineIds);

                // Load related data for response
                var diseaseRepo = _unitOfWork.GetRepository<Disease>();
                // Lấy diseaseIds từ request
                var requestDiseaseIds = new List<int>();
                if (request.DiseaseId.HasValue)
                {
                    requestDiseaseIds.Add(request.DiseaseId.Value);
                }
                if (request.DiseaseIds != null && request.DiseaseIds.Any())
                {
                    requestDiseaseIds.AddRange(request.DiseaseIds);
                }
                var disease = await diseaseRepo.GetByIdAsync(requestDiseaseIds.First()); // Lấy bệnh đầu tiên cho response

                var scheduleRepo2 = _unitOfWork.GetRepository<AppointmentSchedule>();
                var scheduleWithDetails = await scheduleRepo2.GetAsync(s => s.ScheduleId == request.ScheduleId, "Slot,Facility");

                // Load package data if exists
                VaccinePackage? package = null;
                if (request.PackageId.HasValue && request.PackageId.Value > 0)
                {
                    var packageRepo = _unitOfWork.GetRepository<VaccinePackage>();
                    package = await packageRepo.GetByIdAsync(request.PackageId.Value);
                }

                // ✅ Load order data if exists
                Order? orderForResponse = null;
                if (request.OrderId.HasValue && request.OrderId.Value > 0)
                {
                    var orderRepo = _unitOfWork.GetRepository<Order>();
                    orderForResponse = await orderRepo.GetAsync(o => o.OrderId == request.OrderId.Value, "Member,Package,OrderDetails");
                }

                // Load selected vaccines data if exists
                List<AppointmentSelectedVaccineDTO> selectedVaccines = new List<AppointmentSelectedVaccineDTO>();
                if (request.FacilityVaccineIds != null && request.FacilityVaccineIds.Any())
                {
                    var facilityVaccineRepo = _unitOfWork.GetRepository<FacilityVaccine>();
                    foreach (var vaccineId in request.FacilityVaccineIds)
                    {
                        var facilityVaccine = await facilityVaccineRepo.GetAsync(
                            fv => fv.FacilityVaccineId == vaccineId,
                            includeProperties: "Vaccine");
                        if (facilityVaccine != null)
                        {
                            selectedVaccines.Add(new AppointmentSelectedVaccineDTO
                            {
                                FacilityVaccineId = facilityVaccine.FacilityVaccineId,
                                VaccineId = facilityVaccine.VaccineId,
                                VaccineName = facilityVaccine.Vaccine?.Name ?? "Unknown Vaccine",
                                Manufacturer = facilityVaccine.Vaccine?.Manufacturer ?? "Unknown Manufacturer",
                                Price = facilityVaccine.Price,
                                Quantity = 1,
                                Description = facilityVaccine.Vaccine?.Description ?? ""
                            });
                        }
                    }
                }

                // Prepare response với data đầy đủ
                var response = new AppointmentBookingResponseDTO
                {
                    AppointmentId = appointment.AppointmentId,
                    Status = appointment.Status,
                    CreatedAt = appointment.CreatedAt,
                    Note = appointment.Note,
                    EstimatedCost = estimatedCost.TotalCost,

                    // Map child data
                    Child = child != null ? new Contracts.DTOs.Child.ChildDTO
                    {
                        ChildId = child.ChildId,
                        FullName = child.FullName,
                        BirthDate = child.BirthDate,
                        Gender = child.Gender,
                        BloodType = child.BloodType
                    } : null,

                    // Map disease data
                    Disease = disease != null ? new Contracts.DTOs.Disease.DiseaseDTO
                    {
                        DiseaseId = disease.DiseaseId,
                        Name = disease.Name,
                        Description = disease.Description
                    } : null,

                    // Map facility data
                    Facility = scheduleWithDetails?.Facility != null ? _mapper.Map<Contracts.DTOs.VaccinationFacility.VaccinationFacilityDTO>(scheduleWithDetails.Facility) : null,

                    // Map package data
                    Package = package != null ? new Contracts.DTOs.VaccinePackage.VaccinePackageDTO
                    {
                        PackageId = package.PackageId,
                        Name = package.Name,
                        Description = package.Description,
                        Price = package.Price
                    } : null,

                    // ✅ Map order data
                    Order = orderForResponse != null ? _mapper.Map<Contracts.DTOs.Order.OrderDTO>(orderForResponse) : null,

                    // Map selected vaccines data
                    SelectedVaccines = selectedVaccines,

                    // Map schedule data
                    Schedule = scheduleWithDetails != null ? new Contracts.DTOs.Appointment.AppointmentScheduleDTO
                    {
                        ScheduleId = scheduleWithDetails.ScheduleId,
                        FacilityId = scheduleWithDetails.FacilityId,
                        SlotId = scheduleWithDetails.SlotId,
                        Date = scheduleWithDetails.Date,
                        Status = scheduleWithDetails.Status,
                        Facility = scheduleWithDetails.Facility != null ? _mapper.Map<Contracts.DTOs.VaccinationFacility.VaccinationFacilityDTO>(scheduleWithDetails.Facility) : null,
                        Slot = scheduleWithDetails.Slot != null ? new Contracts.DTOs.FacilitySchedule.ScheduleSlotDTO
                        {
                            SlotId = scheduleWithDetails.Slot.SlotId,
                            SlotTime = scheduleWithDetails.Slot.SlotTime,
                            StartTime = scheduleWithDetails.Slot.StartTime ?? TimeOnly.MinValue,
                            EndTime = scheduleWithDetails.Slot.EndTime ?? TimeOnly.MinValue,
                            MaxCapacity = scheduleWithDetails.Slot.MaxCapacity
                        } : null
                    } : null
                };

                // ✅ Tăng BookedCount trong AppointmentSchedule
                var scheduleRepoForUpdate = _unitOfWork.GetRepository<AppointmentSchedule>();
                var scheduleToUpdate = await scheduleRepoForUpdate.GetByIdAsync(request.ScheduleId);
                if (scheduleToUpdate != null)
                {
                    // Tăng BookedCount lên 1 (mỗi appointment = 1 người)
                    var oldBookedCount = scheduleToUpdate.BookedCount ?? 0;
                    scheduleToUpdate.BookedCount = oldBookedCount + 1;
                    scheduleToUpdate.UpdatedAt = DateTime.UtcNow;
                    scheduleRepoForUpdate.Update(scheduleToUpdate);
                    await _unitOfWork.SaveChangesAsync();

                    _logger.LogInformation("Đã tăng BookedCount cho Schedule {ScheduleId} từ {OldCount} lên {NewCount}",
                        request.ScheduleId, oldBookedCount, scheduleToUpdate.BookedCount);
                }

                _logger.LogInformation("Đặt lịch thành công cho trẻ {ChildId}, AppointmentId: {AppointmentId}, Luồng: {Flow}",
                    request.ChildId, appointment.AppointmentId,
                    request.OrderId.HasValue ? $"Order existing {request.OrderId}" :
                    request.PackageId.HasValue ? $"Package new {request.PackageId}" :
                    "Individual vaccines");

                // Commit transaction khi mọi thứ thành công
                await transaction.CommitAsync();
                return CreateSuccessResponse(response, "Đặt lịch tiêm chủng thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đặt lịch cho trẻ {ChildId}, PackageId: {PackageId}, FacilityVaccineIds: {FacilityVaccineIds}",
                    request.ChildId, request.PackageId, request.FacilityVaccineIds != null ? string.Join(",", request.FacilityVaccineIds) : "null");
                // Rollback nếu đang trong transaction
                try { if (transaction != null) await transaction.RollbackAsync(); } catch { /* ignore */ }
                return CreateErrorResponse<AppointmentBookingResponseDTO>($"Có lỗi xảy ra khi đặt lịch: {ex.Message}");
            }
        }

        public async Task<ResponseDataModel<AppointmentQuickBookingResponseDTO>> QuickBookAppointmentAsync(AppointmentQuickBookingDTO request)
        {
            try
            {
                _logger.LogInformation("Đặt lịch nhanh cho trẻ {ChildId}", request.ChildId);

                // Find best facility and schedule for the disease
                var facilities = await SearchFacilitiesByDiseaseAsync(request.DiseaseId);

                if (!facilities.Facilities.Any())
                {
                    var failureResponse = new AppointmentQuickBookingResponseDTO
                    {
                        IsSuccess = false,
                        FailureReason = "Không tìm thấy cơ sở có vaccine phù hợp",
                        Suggestions = new List<AppointmentSuggestionDTO>()
                    };
                    return CreateErrorResponse<AppointmentQuickBookingResponseDTO>("Không tìm thấy cơ sở có vaccine phù hợp", failureResponse);
                }

                // Get the first available facility (sorted by best match)
                var bestFacility = facilities.Facilities.First();

                // Get available schedules for this facility
                var fromDate = request.PreferredDate ?? DateOnly.FromDateTime(DateTime.Now.AddDays(1));
                var toDate = fromDate.AddDays(30);

                var availableSchedules = await GetAvailableSchedulesAsync(
                    bestFacility.FacilityId,
                    fromDate,
                    toDate,
                    request.PreferredTimeSlots);

                var firstAvailableSlot = availableSchedules.DailySchedules
                    .Where(d => d.IsAvailable)
                    .SelectMany(d => d.AvailableSlots)
                    .FirstOrDefault();

                if (firstAvailableSlot == null)
                {
                    var failureResponse = new AppointmentQuickBookingResponseDTO
                    {
                        IsSuccess = false,
                        FailureReason = "Không có lịch trống phù hợp",
                        Suggestions = await GenerateAppointmentSuggestionsAsync(request)
                    };
                    return CreateErrorResponse<AppointmentQuickBookingResponseDTO>("Không có lịch trống phù hợp", failureResponse);
                }

                // Get vaccines for this facility and disease
                var vaccines = await GetFacilityVaccinesByDiseaseAsync(bestFacility.FacilityId, request.DiseaseId);

                // Prefer packages if requested, otherwise use first individual vaccine
                var packageId = (int?)null;
                var facilityVaccineIds = (List<int>?)null;

                if (request.PreferPackages && vaccines.VaccinePackages.Any())
                {
                    packageId = vaccines.VaccinePackages.First().PackageId;
                }
                else if (vaccines.IndividualVaccines.Any())
                {
                    facilityVaccineIds = new List<int> { vaccines.IndividualVaccines.First().FacilityVaccineId };
                }

                // Book the appointment
                var bookingRequest = new AppointmentBookingRequestDTO
                {
                    ChildId = request.ChildId,
                    DiseaseId = request.DiseaseId,
                    FacilityId = bestFacility.FacilityId,
                    PackageId = packageId,
                    FacilityVaccineIds = facilityVaccineIds,
                    ScheduleId = firstAvailableSlot.ScheduleId,
                    Note = request.Note
                };

                var bookingResult = await BookAppointmentAsync(bookingRequest);

                if (!bookingResult.Status)
                {
                    var failureResponse = new AppointmentQuickBookingResponseDTO
                    {
                        IsSuccess = false,
                        FailureReason = bookingResult.Message,
                        Suggestions = new List<AppointmentSuggestionDTO>()
                    };
                    return CreateErrorResponse<AppointmentQuickBookingResponseDTO>(bookingResult.Message, failureResponse);
                }

                var successResponse = new AppointmentQuickBookingResponseDTO
                {
                    IsSuccess = true,
                    Appointment = bookingResult.Data,
                    Suggestions = new List<AppointmentSuggestionDTO>()
                };
                return CreateSuccessResponse(successResponse, "Đặt lịch nhanh thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đặt lịch nhanh cho trẻ {ChildId}", request.ChildId);
                var errorResponse = new AppointmentQuickBookingResponseDTO
                {
                    IsSuccess = false,
                    FailureReason = ex.Message,
                    Suggestions = new List<AppointmentSuggestionDTO>()
                };
                return CreateErrorResponse<AppointmentQuickBookingResponseDTO>($"Có lỗi xảy ra khi đặt lịch nhanh: {ex.Message}", errorResponse);
            }
        }

        public async Task<ResponseDataModel<CancelAppointmentResponseDTO>> CancelAppointmentAsync(int appointmentId, string reason)
        {
            try
            {
                _logger.LogInformation("Xóa lịch hẹn {AppointmentId}", appointmentId);

                var appointmentRepo = _unitOfWork.GetRepository<VaccinationAppointment>();
                var appointment = await appointmentRepo.GetByIdAsync(appointmentId);

                if (appointment == null)
                {
                    var errorResponse = new CancelAppointmentResponseDTO
                    {
                        IsSuccess = false,
                        Message = $"Không tìm thấy lịch hẹn với ID {appointmentId}"
                    };
                    return CreateErrorResponse($"Không tìm thấy lịch hẹn với ID {appointmentId}", errorResponse);
                }

                // ✅ Lưu ScheduleId trước khi xóa appointment
                var scheduleId = appointment.ScheduleId;

                // ✅ QUAN TRỌNG: Xử lý ChildVaccineProfile trước khi xóa appointment
                var childVaccineProfileRepo = _unitOfWork.GetRepository<ChildVaccineProfile>();
                var childVaccineProfiles = await childVaccineProfileRepo.FindAsync(p => p.AppointmentId == appointmentId);
                if (childVaccineProfiles.Any())
                {
                    foreach (var profile in childVaccineProfiles)
                    {
                        // Set AppointmentId về null và đặt lại status về "Pending" 
                        profile.AppointmentId = null;
                        profile.Status = "Pending";
                        profile.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        childVaccineProfileRepo.Update(profile);
                    }
                    _logger.LogInformation("Đã cập nhật {Count} ChildVaccineProfiles, đặt AppointmentId về null cho appointment {AppointmentId}",
                        childVaccineProfiles.Count, appointmentId);
                }

                // Xóa các VaccinationAppointmentDetails nếu có (individual vaccines)
                var appointmentDetailRepo = _unitOfWork.GetRepository<VaccinationAppointmentDetail>();
                var appointmentDetails = await appointmentDetailRepo.FindAsync(d => d.AppointmentId == appointmentId);
                if (appointmentDetails.Any())
                {
                    appointmentDetailRepo.HardDeleteRange(appointmentDetails.ToList());
                    _logger.LogInformation("Xóa {Count} VaccinationAppointmentDetails cho appointment {AppointmentId}",
                        appointmentDetails.Count, appointmentId);
                }

                // Xóa appointment
                appointmentRepo.HardDelete(appointment);

                await _unitOfWork.SaveChangesAsync();

                // ✅ Giảm BookedCount trong AppointmentSchedule
                var scheduleRepo = _unitOfWork.GetRepository<AppointmentSchedule>();
                var scheduleToUpdate = await scheduleRepo.GetByIdAsync(scheduleId);
                if (scheduleToUpdate != null && scheduleToUpdate.BookedCount > 0)
                {
                    var oldBookedCount = scheduleToUpdate.BookedCount ?? 0;
                    scheduleToUpdate.BookedCount = Math.Max(0, oldBookedCount - 1); // Không để âm
                    scheduleToUpdate.UpdatedAt = DateTime.UtcNow;
                    scheduleRepo.Update(scheduleToUpdate);
                    await _unitOfWork.SaveChangesAsync();

                    _logger.LogInformation("Đã giảm BookedCount cho Schedule {ScheduleId} từ {OldCount} xuống {NewCount}",
                        scheduleId, oldBookedCount, scheduleToUpdate.BookedCount);
                }

                var response = new CancelAppointmentResponseDTO
                {
                    IsSuccess = true,
                    Message = "Hủy lịch hẹn thành công"
                };

                _logger.LogInformation("Xóa lịch hẹn {AppointmentId} thành công", appointmentId);
                return CreateSuccessResponse(response, "Hủy lịch hẹn thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa lịch hẹn {AppointmentId}", appointmentId);
                var errorResponse = new CancelAppointmentResponseDTO
                {
                    IsSuccess = false,
                    Message = $"Có lỗi xảy ra khi hủy lịch hẹn: {ex.Message}"
                };
                return CreateErrorResponse($"Có lỗi xảy ra khi hủy lịch hẹn: {ex.Message}", errorResponse);
            }
        }

        // Vaccine Update Methods 
        public async Task<ResponseDataModel<UpdateVaccineResponseDTO>> UpdateAppointmentVaccineAsync(UpdateVaccineRequestDTO request, int facilityId, int staffAccountId)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                _logger.LogInformation("Staff {StaffAccountId} đang thay đổi vaccine cho AppointmentDetail {DetailId} tại facility {FacilityId}", 
                    staffAccountId, request.AppointmentDetailId, facilityId);

                // 1. Validate staff permissions
                var staffRepo = _unitOfWork.GetRepository<FacilityStaff>();
                var staff = await staffRepo.GetAsync(s => s.AccountId == staffAccountId);
                if (staff == null || staff.FacilityId != facilityId)
                {
                    return CreateErrorResponse<UpdateVaccineResponseDTO>("Bạn không có quyền thực hiện thao tác này tại cơ sở này");
                }

                // 2. Lấy thông tin AppointmentDetail và related data
                var appointmentDetailRepo = _unitOfWork.GetRepository<VaccinationAppointmentDetail>();
                var appointmentDetail = await appointmentDetailRepo.GetAsync(
                    d => d.DetailId == request.AppointmentDetailId,
                    includeProperties: "Appointment,Appointment.Schedule,Appointment.Schedule.Facility,Appointment.Child,Vaccine"
                );

                if (appointmentDetail == null)
                {
                    return CreateErrorResponse<UpdateVaccineResponseDTO>($"Không tìm thấy VaccinationAppointmentDetail với ID {request.AppointmentDetailId}");
                }

                // 3. Validate appointment thuộc facility của staff
                if (appointmentDetail.Appointment?.Schedule?.FacilityId != facilityId)
                {
                    return CreateErrorResponse<UpdateVaccineResponseDTO>("AppointmentDetail này không thuộc cơ sở của bạn");
                }

                // 4. Validate appointment status (chỉ cho phép thay đổi khi Approval)
                if (appointmentDetail.Appointment.Status != "Approval")
                {
                    return CreateErrorResponse<UpdateVaccineResponseDTO>($"Chỉ có thể thay đổi vaccine khi appointment đang ở trạng thái 'Approval'. Trạng thái hiện tại: {appointmentDetail.Appointment.Status}");
                }

                // 5. Validate new vaccine exists
                var vaccineRepo = _unitOfWork.GetRepository<Vaccine>();
                var newVaccine = await vaccineRepo.GetAsync(v => v.VaccineId == request.NewVaccineId, includeProperties: "VaccineDiseases,VaccineDiseases.Disease");
                if (newVaccine == null)
                {
                    return CreateErrorResponse<UpdateVaccineResponseDTO>($"Không tìm thấy vaccine với ID {request.NewVaccineId}");
                }

                // 6. Check facility has new vaccine and sufficient quantity
                var facilityVaccineRepo = _unitOfWork.GetRepository<FacilityVaccine>();
                var facilityVaccine = await facilityVaccineRepo.GetAsync(
                    fv => fv.VaccineId == request.NewVaccineId && fv.FacilityId == facilityId && fv.Status == "active"
                );

                if (facilityVaccine == null)
                {
                    return CreateErrorResponse<UpdateVaccineResponseDTO>($"Cơ sở này không có vaccine {newVaccine.Name} hoặc vaccine không còn hoạt động");
                }

                if (facilityVaccine.AvailableQuantity <= 0)
                {
                    return CreateErrorResponse<UpdateVaccineResponseDTO>($"Vaccine {newVaccine.Name} đã hết hàng tại cơ sở này (Số lượng: {facilityVaccine.AvailableQuantity})");
                }

                // 7. Check vaccine compatibility with booked diseases
                var bookedDiseaseIds = await GetBookedDiseaseIdsAsync(appointmentDetail.AppointmentId);
                var canTreatBookedDiseases = newVaccine.VaccineDiseases?.Any(vd => bookedDiseaseIds.Contains(vd.DiseaseId)) ?? false;
                
                if (!canTreatBookedDiseases)
                {
                    var diseaseNames = await GetDiseaseNamesAsync(bookedDiseaseIds);
                    return CreateErrorResponse<UpdateVaccineResponseDTO>($"Vaccine {newVaccine.Name} không thể điều trị các bệnh đã đặt lịch: {string.Join(", ", diseaseNames)}");
                }

                // 8. Handle old vaccine quantity update
                await HandleOldVaccineQuantityAsync(appointmentDetail.VaccineId, facilityId, request.SourceType, request.OrderDetailId);

                // 9. Handle new vaccine quantity update  
                await HandleNewVaccineQuantityAsync(request.NewVaccineId, facilityId, request.SourceType, request.OrderDetailId);

                // 10. Update VaccinationAppointmentDetail
                var oldVaccineId = appointmentDetail.VaccineId;
                appointmentDetail.VaccineId = request.NewVaccineId;
                appointmentDetail.Notes = $"{appointmentDetail.Notes ?? ""}\n[{DateTime.Now:yyyy-MM-dd HH:mm}] Thay đổi từ VaccineId {oldVaccineId} sang {request.NewVaccineId}. Lý do: {request.Reason}. Ghi chú: {request.Notes}".Trim();
                
                appointmentDetailRepo.Update(appointmentDetail);

                // ✅ 11. CRITICAL FIX: Update ChildVaccineProfile với logic tương thích bệnh
                var childVaccineProfileRepo = _unitOfWork.GetRepository<ChildVaccineProfile>();
                var childVaccineProfiles = await childVaccineProfileRepo.FindAsync(
                    p => p.AppointmentId == appointmentDetail.AppointmentId && p.VaccineId == oldVaccineId);

                // Lấy danh sách bệnh mà vaccine mới có thể chữa
                var newVaccineDiseaseIds = newVaccine.VaccineDiseases?.Select(vd => vd.DiseaseId).ToList() ?? new List<int>();
                
                // Khởi tạo thông tin compatibility
                var compatibleProfiles = new List<ChildVaccineProfile>();
                var incompatibleProfiles = new List<ChildVaccineProfile>();

                if (childVaccineProfiles.Any())
                {
                    _logger.LogInformation("Xử lý {Count} ChildVaccineProfile khi thay đổi từ VaccineId {OldVaccineId} sang {NewVaccineId}", 
                        childVaccineProfiles.Count(), oldVaccineId, request.NewVaccineId);
                    
                    foreach (var profile in childVaccineProfiles)
                    {
                        // Kiểm tra vaccine mới có chữa được bệnh này không
                        if (newVaccineDiseaseIds.Contains(profile.DiseaseId))
                        {
                            // ✅ Vaccine mới chữa được bệnh này → Update VaccineId
                            profile.VaccineId = request.NewVaccineId;
                            profile.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                            childVaccineProfileRepo.Update(profile);
                            compatibleProfiles.Add(profile);
                            
                            _logger.LogInformation("✅ Cập nhật ChildVaccineProfile {ProfileId} (Disease {DiseaseId}) với VaccineId mới {NewVaccineId}", 
                                profile.VaccineProfileId, profile.DiseaseId, request.NewVaccineId);
                        }
                        else
                        {
                            // ❌ Vaccine mới KHÔNG chữa được bệnh này → Xóa AppointmentId (trả về trạng thái Scheduled)
                            profile.AppointmentId = null;
                            profile.Status = "Scheduled"; // Trả về trạng thái chờ đặt lịch
                            profile.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                            childVaccineProfileRepo.Update(profile);
                            incompatibleProfiles.Add(profile);
                            
                            _logger.LogWarning("⚠️ Vaccine mới {NewVaccineId} không chữa được Disease {DiseaseId}. Xóa AppointmentId khỏi ChildVaccineProfile {ProfileId}", 
                                request.NewVaccineId, profile.DiseaseId, profile.VaccineProfileId);
                        }
                    }

                    _logger.LogInformation("📊 Kết quả thay đổi vaccine: {CompatibleCount} CVP tương thích (updated), {IncompatibleCount} CVP không tương thích (unlinked)", 
                        compatibleProfiles.Count, incompatibleProfiles.Count);
                }
                else
                {
                    _logger.LogWarning("⚠️ Không tìm thấy ChildVaccineProfile nào cho AppointmentId {AppointmentId} với VaccineId {OldVaccineId}", 
                        appointmentDetail.AppointmentId, oldVaccineId);
                }

                await _unitOfWork.SaveChangesAsync();

                // 12. Build response với thông tin disease compatibility
                var oldVaccine = await vaccineRepo.GetAsync(v => v.VaccineId == oldVaccineId, "VaccineDiseases,VaccineDiseases.Disease");
                var oldFacilityVaccine = await facilityVaccineRepo.GetAsync(fv => fv.VaccineId == oldVaccineId && fv.FacilityId == facilityId);
                
                // Lấy tên bệnh để hiển thị trong response
                var diseaseRepo = _unitOfWork.GetRepository<Disease>();
                var compatibleDiseaseNames = new List<string>();
                var incompatibleDiseaseNames = new List<string>();

                if (compatibleProfiles.Any())
                {
                    var compatibleDiseaseIds = compatibleProfiles.Select(p => p.DiseaseId).Distinct().ToList();
                    var compatibleDiseases = await diseaseRepo.FindAsync(d => compatibleDiseaseIds.Contains(d.DiseaseId));
                    compatibleDiseaseNames = compatibleDiseases.Select(d => d.Name).ToList();
                }

                if (incompatibleProfiles.Any())
                {
                    var incompatibleDiseaseIds = incompatibleProfiles.Select(p => p.DiseaseId).Distinct().ToList();
                    var incompatibleDiseases = await diseaseRepo.FindAsync(d => incompatibleDiseaseIds.Contains(d.DiseaseId));
                    incompatibleDiseaseNames = incompatibleDiseases.Select(d => d.Name).ToList();
                }

                // Tạo message phù hợp
                var message = "Thay đổi vaccine thành công";
                if (incompatibleProfiles.Any())
                {
                    message += $". Lưu ý: {incompatibleProfiles.Count} bệnh không còn được chữa bởi vaccine mới và cần đặt lịch lại: {string.Join(", ", incompatibleDiseaseNames)}";
                }

                var response = new UpdateVaccineResponseDTO
                {
                    IsSuccess = true,
                    Message = message,
                    OldVaccine = new VaccineChangeInfo
                    {
                        VaccineId = oldVaccineId,
                        VaccineName = oldVaccine?.Name ?? "Unknown",
                        Manufacturer = oldVaccine?.Manufacturer ?? "",
                        Description = oldVaccine?.Description ?? "",
                        AvailableQuantity = oldFacilityVaccine?.AvailableQuantity ?? 0,
                        Price = oldFacilityVaccine?.Price ?? 0,
                        TreatedDiseases = oldVaccine?.VaccineDiseases?.Select(vd => vd.Disease.Name).ToList() ?? new List<string>()
                    },
                    NewVaccine = new VaccineChangeInfo
                    {
                        VaccineId = request.NewVaccineId,
                        VaccineName = newVaccine.Name,
                        Manufacturer = newVaccine.Manufacturer,
                        Description = newVaccine.Description,
                        AvailableQuantity = facilityVaccine.AvailableQuantity,
                        Price = facilityVaccine.Price,
                        TreatedDiseases = newVaccine.VaccineDiseases?.Select(vd => vd.Disease.Name).ToList() ?? new List<string>()
                    },
                    UpdatedDetail = new UpdatedAppointmentDetailInfo
                    {
                        AppointmentDetailId = appointmentDetail.DetailId,
                        AppointmentId = appointmentDetail.AppointmentId,
                        OldVaccineId = oldVaccineId,
                        NewVaccineId = request.NewVaccineId,
                        Reason = request.Reason,
                        Notes = request.Notes,
                        UpdatedAt = DateTime.UtcNow,
                        UpdatedBy = staff.FullName ?? "Staff",
                        SourceType = request.SourceType,
                        OrderDetailId = request.OrderDetailId,
                        PackageName = request.OrderDetailId.HasValue ? await GetPackageNameByOrderDetailIdAsync(request.OrderDetailId.Value) : null,
                        AdditionalCost = request.SourceType == "Order" ? 0 : facilityVaccine.Price
                    },
                    DiseaseCompatibility = new DiseaseCompatibilityInfo
                    {
                        CompatibleDiseaseCount = compatibleProfiles.Select(p => p.DiseaseId).Distinct().Count(),
                        IncompatibleDiseaseCount = incompatibleProfiles.Select(p => p.DiseaseId).Distinct().Count(),
                        CompatibleDiseases = compatibleDiseaseNames,
                        IncompatibleDiseases = incompatibleDiseaseNames
                    }
                };

                await transaction.CommitAsync();

                _logger.LogInformation("Đã thay đổi vaccine thành công: AppointmentDetail {DetailId}, {OldVaccine} -> {NewVaccine}", 
                    request.AppointmentDetailId, oldVaccine?.Name, newVaccine.Name);

                return CreateSuccessResponse(response, "Thay đổi vaccine thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thay đổi vaccine cho AppointmentDetail {DetailId}", request.AppointmentDetailId);
                
                var errorResponse = new UpdateVaccineResponseDTO
                {
                    IsSuccess = false,
                    Message = $"Có lỗi xảy ra: {ex.Message}"
                };
                return CreateErrorResponse($"Có lỗi xảy ra: {ex.Message}", errorResponse);
            }
        }

        public async Task<ResponseDataModel<AvailableVaccinesResponseDTO>> GetAvailableVaccinesForReplacementAsync(int appointmentDetailId, int facilityId)
        {
            try
            {
                _logger.LogInformation("Lấy danh sách vaccine thay thế cho AppointmentDetail {DetailId} tại facility {FacilityId}", 
                    appointmentDetailId, facilityId);

                // 1. Lấy thông tin AppointmentDetail hiện tại
                var appointmentDetailRepo = _unitOfWork.GetRepository<VaccinationAppointmentDetail>();
                var appointmentDetail = await appointmentDetailRepo.GetAsync(
                    d => d.DetailId == appointmentDetailId,
                    includeProperties: "Appointment,Appointment.Schedule,Appointment.Schedule.Facility,Vaccine"
                );

                if (appointmentDetail == null)
                {
                    return CreateErrorResponse<AvailableVaccinesResponseDTO>($"Không tìm thấy VaccinationAppointmentDetail với ID {appointmentDetailId}");
                }

                // 2. Validate facility
                if (appointmentDetail.Appointment?.Schedule?.FacilityId != facilityId)
                {
                    return CreateErrorResponse<AvailableVaccinesResponseDTO>("AppointmentDetail này không thuộc cơ sở được chỉ định");
                }

                // 3. Lấy danh sách diseases được book
                var bookedDiseaseIds = await GetBookedDiseaseIdsAsync(appointmentDetail.AppointmentId);
                var bookedDiseaseNames = await GetDiseaseNamesAsync(bookedDiseaseIds);

                // 4. Lấy member orders tại facility này
                var member = appointmentDetail.Appointment?.Child?.Member;
                var memberOrders = member != null ? await GetMemberOrdersAtFacilityAsync(member.MemberId, facilityId) : new List<Order>();

                // 5. Lấy tất cả vaccines có thể thay thế
                var facilityVaccineRepo = _unitOfWork.GetRepository<FacilityVaccine>();
                var vaccineRepo = _unitOfWork.GetRepository<Vaccine>();
                
                var availableVaccines = await facilityVaccineRepo.FindAsync(
                    fv => fv.FacilityId == facilityId && 
                          fv.Status == "active" && 
                          fv.VaccineId != appointmentDetail.VaccineId, // Exclude current vaccine
                    includeProperties: "Vaccine,Vaccine.VaccineDiseases,Vaccine.VaccineDiseases.Disease"
                );

                var availableVaccineDTOs = new List<AvailableVaccineDTO>();

                foreach (var facilityVaccine in availableVaccines)
                {
                    var vaccine = facilityVaccine.Vaccine;
                    if (vaccine == null) continue;

                    // Check compatibility with booked diseases
                    var canTreat = vaccine.VaccineDiseases?.Any(vd => bookedDiseaseIds.Contains(vd.DiseaseId)) ?? false;
                    var treatedDiseases = vaccine.VaccineDiseases?.Where(vd => bookedDiseaseIds.Contains(vd.DiseaseId))
                                                                 .Select(vd => vd.Disease.Name).ToList() ?? new List<string>();

                    if (!canTreat) continue; // Skip vaccines that can't treat booked diseases

                    // Get vaccine sources (orders + individual)
                    var vaccineSources = await GetVaccineSourcesAsync(vaccine.VaccineId, facilityId, memberOrders);

                    var availableVaccineDTO = new AvailableVaccineDTO
                    {
                        VaccineId = vaccine.VaccineId,
                        VaccineName = vaccine.Name,
                        Manufacturer = vaccine.Manufacturer,
                        Description = vaccine.Description,
                        AvailableQuantity = facilityVaccine.AvailableQuantity,
                        Price = facilityVaccine.Price,
                        TreatedDiseases = vaccine.VaccineDiseases?.Select(vd => vd.Disease.Name).ToList() ?? new List<string>(),
                        CanTreatBookedDiseases = canTreat,
                        BookedDiseasesItCanTreat = treatedDiseases,
                        AvailableSources = vaccineSources,
                        HasFreeSource = vaccineSources.Any(vs => vs.IsPaid && vs.SourceType == "Order"),
                        RecommendedSource = vaccineSources.FirstOrDefault(vs => vs.IsPriority) ?? vaccineSources.FirstOrDefault()
                    };

                    availableVaccineDTOs.Add(availableVaccineDTO);
                }

                var response = new AvailableVaccinesResponseDTO
                {
                    AppointmentId = appointmentDetail.AppointmentId,
                    AppointmentDetailId = appointmentDetailId,
                    CurrentVaccineName = appointmentDetail.Vaccine?.Name ?? "Unknown",
                    BookedDiseases = bookedDiseaseNames,
                    AvailableVaccines = availableVaccineDTOs.OrderByDescending(v => v.HasFreeSource)
                                                          .ThenBy(v => v.Price)
                                                          .ToList(),
                    TotalAvailable = availableVaccineDTOs.Count
                };

                return CreateSuccessResponse(response, "Lấy danh sách vaccine thay thế thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách vaccine thay thế cho AppointmentDetail {DetailId}", appointmentDetailId);
                
                var errorResponse = new AvailableVaccinesResponseDTO
                {
                    AppointmentDetailId = appointmentDetailId,
                    TotalAvailable = 0
                };
                return CreateErrorResponse($"Có lỗi xảy ra: {ex.Message}", errorResponse);
            }
        }

        #endregion

        #region History Methods

        public async Task<AppointmentHistoryResponseDTO> GetAppointmentHistoryAsync(int memberId, int? childId = null)
        {
            try
            {
                _logger.LogInformation("Lấy lịch sử đặt lịch cho member {MemberId}", memberId);

                // Get appointments của member qua Child
                var appointmentRepo = _unitOfWork.GetRepository<VaccinationAppointment>();
                var childRepo = _unitOfWork.GetRepository<Child>();

                // Lấy danh sách childIds của member
                var memberChildren = await childRepo.FindAsync(c => c.MemberId == memberId);
                var childIds = memberChildren.Select(c => c.ChildId).ToList();

                if (!childIds.Any())
                {
                    return new AppointmentHistoryResponseDTO
                    {
                        TotalCount = 0,
                        TotalPages = 0,
                        CurrentPage = 1,
                        PageSize = 100,
                        Appointments = new List<AppointmentHistoryDTO>()
                    };
                }

                // Build query đơn giản
                var query = appointmentRepo.GetAllQueryable("Schedule.Slot,Schedule.Facility,Child,Order.OrderDetails");

                // Filter theo childIds của member
                query = query.Where(a => childIds.Contains(a.ChildId));

                // Filter theo childId nếu có
                if (childId.HasValue)
                {
                    query = query.Where(a => a.ChildId == childId.Value);
                }

                // Sort theo ngày appointment (mới nhất trước)
                query = query.OrderByDescending(a => a.Schedule.Date).ThenByDescending(a => a.Schedule.Slot.StartTime);

                // Lấy tất cả (không pagination)
                var appointments = query.ToList();

                // Map to DTO
                var appointmentDTOs = new List<AppointmentHistoryDTO>();
                foreach (var appointment in appointments)
                {
                    var dto = await MapToAppointmentHistoryDTO(appointment);
                    appointmentDTOs.Add(dto);
                }

                // Calculate statistics
                var stats = CalculateAppointmentStatistics(appointments);

                return new AppointmentHistoryResponseDTO
                {
                    TotalCount = appointments.Count,
                    TotalPages = 1, // Không pagination
                    CurrentPage = 1,
                    PageSize = appointments.Count,
                    Appointments = appointmentDTOs,
                    UpcomingCount = stats.UpcomingCount,
                    CompletedCount = stats.CompletedCount,
                    CancelledCount = stats.CancelledCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy lịch sử đặt lịch cho member {MemberId}", memberId);
                throw;
            }
        }

        #endregion

        #region Facility Staff Methods

        public async Task<FacilityAppointmentResponseDTO> GetAllFacilityAppointmentsAsync(int facilityId, int pageIndex = 1, int pageSize = 50, string? childName = null)
        {
            try
            {
                _logger.LogInformation("Lấy tất cả lịch đặt cho facility {FacilityId} - Page {PageIndex}, Size {PageSize}, ChildName: {ChildName}",
                    facilityId, pageIndex, pageSize, childName ?? "null");

                // Get appointments của facility với phân trang
                var appointmentRepo = _unitOfWork.GetRepository<VaccinationAppointment>();

                // ✅ Tạo filter động dựa trên childName
                System.Linq.Expressions.Expression<Func<VaccinationAppointment, bool>> filter;
                if (!string.IsNullOrWhiteSpace(childName))
                {
                    // Filter theo facility và tên trẻ (case-insensitive, contains)
                    filter = a => a.Schedule.FacilityId == facilityId && 
                                  a.Child.FullName.ToLower().Contains(childName.ToLower());
                    _logger.LogInformation("Áp dụng filter theo tên trẻ: {ChildName}", childName);
                }
                else
                {
                    // Chỉ filter theo facility
                    filter = a => a.Schedule.FacilityId == facilityId;
                }

                // ✅ Sử dụng phân trang từ repository
                var result = await appointmentRepo.GetAllAsync(
                    filter: filter,
                    orderBy: q => q.OrderByDescending(a => a.Schedule.Date).ThenByDescending(a => a.Schedule.Slot.StartTime),
                    include: "Schedule.Slot,Schedule.Facility,Child.Member.Account,Order.OrderDetails.FacilityVaccine.Vaccine,Order.OrderDetails.Disease,Order.Member",
                    pageIndex: pageIndex,
                    pageSize: pageSize
                );

                var appointmentDTOs = new List<FacilityAppointmentDTO>();
                foreach (var appointment in result.Data)
                {
                    var dto = await MapToFacilityAppointmentDTO(appointment);
                    appointmentDTOs.Add(dto);
                }

                // ✅ Calculate statistics từ tất cả appointments (không phân trang) - áp dụng cùng filter
                var allAppointments = await appointmentRepo.FindAsync(
                    filter,
                    "Schedule.Slot,Schedule.Facility");
                var stats = CalculateFacilityAppointmentStatistics(allAppointments);

                // ✅ Tính total pages
                var totalPages = (int)Math.Ceiling((double)result.TotalCount / pageSize);

                return new FacilityAppointmentResponseDTO
                {
                    // Phân trang
                    TotalCount = result.TotalCount,
                    TotalPages = totalPages,
                    CurrentPage = pageIndex,
                    PageSize = pageSize,

                    Appointments = appointmentDTOs,

                    // Thống kê
                    PendingCount = stats.PendingCount,
                    ConfirmedCount = stats.ConfirmedCount,
                    CompletedCount = stats.CompletedCount,
                    CancelledCount = stats.CancelledCount,
                    RefundingCount = stats.RefundingCount,
                    RefundedCount = stats.RefundedCount,
                    TodayCount = stats.TodayCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy tất cả lịch đặt cho facility {FacilityId}", facilityId);
                throw;
            }
        }

        public async Task<FacilityAppointmentResponseDTO> GetFacilityAppointmentsByDateAsync(int facilityId, DateTime date, int pageIndex = 1, int pageSize = 50)
        {
            try
            {
                _logger.LogInformation("Lấy lịch đặt theo ngày {Date} cho facility {FacilityId} - Page {PageIndex}, Size {PageSize}",
                    date.Date, facilityId, pageIndex, pageSize);

                var dateOnly = DateOnly.FromDateTime(date.Date);
                var appointmentRepo = _unitOfWork.GetRepository<VaccinationAppointment>();

                // ✅ Sử dụng phân trang
                var result = await appointmentRepo.GetAllAsync(
                    filter: a => a.Schedule.FacilityId == facilityId && a.Schedule.Date == dateOnly,
                    orderBy: q => q.OrderBy(a => a.Schedule.Slot.StartTime),
                    include: "Schedule.Slot,Schedule.Facility,Child.Member.Account,Order.OrderDetails.FacilityVaccine.Vaccine,Order.OrderDetails.Disease,Order.Member",
                    pageIndex: pageIndex,
                    pageSize: pageSize
                );

                var appointmentDTOs = new List<FacilityAppointmentDTO>();
                foreach (var appointment in result.Data)
                {
                    var dto = await MapToFacilityAppointmentDTO(appointment);
                    appointmentDTOs.Add(dto);
                }

                // ✅ Calculate statistics từ tất cả appointments trong ngày
                var allAppointments = await appointmentRepo.FindAsync(
                    a => a.Schedule.FacilityId == facilityId && a.Schedule.Date == dateOnly,
                    "Schedule.Slot,Schedule.Facility");
                var stats = CalculateFacilityAppointmentStatistics(allAppointments);

                // ✅ Tính total pages
                var totalPages = (int)Math.Ceiling((double)result.TotalCount / pageSize);

                return new FacilityAppointmentResponseDTO
                {
                    // Phân trang
                    TotalCount = result.TotalCount,
                    TotalPages = totalPages,
                    CurrentPage = pageIndex,
                    PageSize = pageSize,

                    Appointments = appointmentDTOs,

                    // Thống kê
                    PendingCount = stats.PendingCount,
                    ConfirmedCount = stats.ConfirmedCount,
                    CompletedCount = stats.CompletedCount,
                    CancelledCount = stats.CancelledCount,
                    RefundingCount = stats.RefundingCount,
                    RefundedCount = stats.RefundedCount,
                    TodayCount = stats.TodayCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy lịch đặt theo ngày cho facility {FacilityId}", facilityId);
                throw;
            }
        }

        public async Task<FacilityAppointmentResponseDTO> GetFacilityAppointmentsByWeekAsync(int facilityId, DateTime startOfWeek, int pageIndex = 1, int pageSize = 50)
        {
            try
            {
                _logger.LogInformation("Lấy lịch đặt theo tuần {Week} cho facility {FacilityId} - Page {PageIndex}, Size {PageSize}",
                    startOfWeek.Date, facilityId, pageIndex, pageSize);

                var startDate = DateOnly.FromDateTime(startOfWeek.Date);
                var endDate = startDate.AddDays(6);

                var appointmentRepo = _unitOfWork.GetRepository<VaccinationAppointment>();

                // ✅ Sử dụng phân trang
                var result = await appointmentRepo.GetAllAsync(
                    filter: a => a.Schedule.FacilityId == facilityId &&
                                a.Schedule.Date >= startDate &&
                                a.Schedule.Date <= endDate,
                    orderBy: q => q.OrderBy(a => a.Schedule.Date).ThenBy(a => a.Schedule.Slot.StartTime),
                    include: "Schedule.Slot,Schedule.Facility,Child.Member.Account,Order.OrderDetails.FacilityVaccine.Vaccine,Order.OrderDetails.Disease,Order.Member",
                    pageIndex: pageIndex,
                    pageSize: pageSize
                );

                var appointmentDTOs = new List<FacilityAppointmentDTO>();
                foreach (var appointment in result.Data)
                {
                    var dto = await MapToFacilityAppointmentDTO(appointment);
                    appointmentDTOs.Add(dto);
                }

                // ✅ Calculate statistics từ tất cả appointments trong tuần
                var allAppointments = await appointmentRepo.FindAsync(
                    a => a.Schedule.FacilityId == facilityId &&
                         a.Schedule.Date >= startDate &&
                         a.Schedule.Date <= endDate,
                    "Schedule.Slot,Schedule.Facility");
                var stats = CalculateFacilityAppointmentStatistics(allAppointments);

                // ✅ Tính total pages
                var totalPages = (int)Math.Ceiling((double)result.TotalCount / pageSize);

                return new FacilityAppointmentResponseDTO
                {
                    // Phân trang
                    TotalCount = result.TotalCount,
                    TotalPages = totalPages,
                    CurrentPage = pageIndex,
                    PageSize = pageSize,

                    Appointments = appointmentDTOs,

                    // Thống kê
                    PendingCount = stats.PendingCount,
                    ConfirmedCount = stats.ConfirmedCount,
                    CompletedCount = stats.CompletedCount,
                    CancelledCount = stats.CancelledCount,
                    RefundingCount = stats.RefundingCount,
                    RefundedCount = stats.RefundedCount,
                    TodayCount = stats.TodayCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy lịch đặt theo tuần cho facility {FacilityId}", facilityId);
                throw;
            }
        }

        public async Task<FacilityAppointmentResponseDTO> GetFacilityAppointmentsByMonthAsync(int facilityId, DateTime month, int pageIndex = 1, int pageSize = 50)
        {
            try
            {
                _logger.LogInformation("Lấy lịch đặt theo tháng {Month} cho facility {FacilityId} - Page {PageIndex}, Size {PageSize}",
                    month.ToString("yyyy-MM"), facilityId, pageIndex, pageSize);

                var startDate = DateOnly.FromDateTime(new DateTime(month.Year, month.Month, 1));
                var endDate = startDate.AddMonths(1).AddDays(-1);

                var appointmentRepo = _unitOfWork.GetRepository<VaccinationAppointment>();

                // ✅ Sử dụng phân trang
                var result = await appointmentRepo.GetAllAsync(
                    filter: a => a.Schedule.FacilityId == facilityId &&
                                a.Schedule.Date >= startDate &&
                                a.Schedule.Date <= endDate,
                    orderBy: q => q.OrderBy(a => a.Schedule.Date).ThenBy(a => a.Schedule.Slot.StartTime),
                    include: "Schedule.Slot,Schedule.Facility,Child.Member.Account,Order.OrderDetails.FacilityVaccine.Vaccine,Order.OrderDetails.Disease,Order.Member",
                    pageIndex: pageIndex,
                    pageSize: pageSize
                );

                var appointmentDTOs = new List<FacilityAppointmentDTO>();
                foreach (var appointment in result.Data)
                {
                    var dto = await MapToFacilityAppointmentDTO(appointment);
                    appointmentDTOs.Add(dto);
                }

                // ✅ Calculate statistics từ tất cả appointments trong tháng
                var allAppointments = await appointmentRepo.FindAsync(
                    a => a.Schedule.FacilityId == facilityId &&
                         a.Schedule.Date >= startDate &&
                         a.Schedule.Date <= endDate,
                    "Schedule.Slot,Schedule.Facility");
                var stats = CalculateFacilityAppointmentStatistics(allAppointments);

                // ✅ Tính total pages
                var totalPages = (int)Math.Ceiling((double)result.TotalCount / pageSize);

                return new FacilityAppointmentResponseDTO
                {
                    // Phân trang
                    TotalCount = result.TotalCount,
                    TotalPages = totalPages,
                    CurrentPage = pageIndex,
                    PageSize = pageSize,

                    Appointments = appointmentDTOs,

                    // Thống kê
                    PendingCount = stats.PendingCount,
                    ConfirmedCount = stats.ConfirmedCount,
                    CompletedCount = stats.CompletedCount,
                    CancelledCount = stats.CancelledCount,
                    RefundingCount = stats.RefundingCount,
                    RefundedCount = stats.RefundedCount,
                    TodayCount = stats.TodayCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy lịch đặt theo tháng cho facility {FacilityId}", facilityId);
                throw;
            }
        }

        public async Task<FacilityAppointmentDTO> GetFacilityAppointmentByIdAsync(int appointmentId, int facilityId)
        {
            try
            {
                _logger.LogInformation("Lấy chi tiết lịch đặt {AppointmentId} cho facility {FacilityId}", appointmentId, facilityId);

                var appointmentRepo = _unitOfWork.GetRepository<VaccinationAppointment>();
                var appointments = await appointmentRepo.FindAsync(
                    a => a.AppointmentId == appointmentId,
                    "Schedule.Slot,Schedule.Facility,Child.Member.Account,Order.OrderDetails,Order.OrderDetails.FacilityVaccine,Order.OrderDetails.Disease,Order.OrderDetails.FacilityVaccine.Vaccine,Order.Member,VaccinationAppointmentDetails,VaccinationAppointmentDetails.Vaccine,Child.ChildVaccineProfiles,Child.ChildVaccineProfiles.Disease,Child.ChildVaccineProfiles.Vaccine");

                var appointment = appointments.FirstOrDefault();

                if (appointment == null)
                {
                    throw new ArgumentException($"Không tìm thấy lịch đặt {appointmentId}");
                }

                // Check facility ownership sau khi load
                if (appointment.Schedule?.FacilityId != facilityId)
                {
                    throw new ArgumentException($"Lịch đặt {appointmentId} không thuộc về facility {facilityId}");
                }

                // Manual load Schedule nếu vẫn null
                if (appointment.Schedule == null)
                {
                    _logger.LogWarning("Schedule null, trying manual load for appointment {AppointmentId}", appointmentId);
                    var scheduleRepo = _unitOfWork.GetRepository<AppointmentSchedule>();
                    appointment.Schedule = await scheduleRepo.GetAsync(s => s.ScheduleId == appointment.ScheduleId, "Slot,Facility");

                    if (appointment.Schedule == null)
                    {
                        throw new InvalidOperationException($"Không thể load Schedule cho appointment {appointmentId}");
                    }
                }

                return await MapToFacilityAppointmentDTO(appointment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy chi tiết lịch đặt {AppointmentId} cho facility {FacilityId}", appointmentId, facilityId);
                throw;
            }
        }

        public async Task<bool> UpdateAppointmentStatusAsync(int appointmentId, int facilityId, UpdateAppointmentStatusDTO updateDto)
        {
            try
            {
                _logger.LogInformation("Cập nhật trạng thái lịch đặt {AppointmentId} thành {Status} cho facility {FacilityId}",
                    appointmentId, updateDto.Status, facilityId);

                var appointmentRepo = _unitOfWork.GetRepository<VaccinationAppointment>();
                var appointments = await appointmentRepo.FindAsync(
                    a => a.AppointmentId == appointmentId,
                    "Schedule,Schedule.Slot,Schedule.Facility,Child,Child.Member,Child.Member.Account,Order,Order.OrderDetails,VaccinationAppointmentDetails,VaccinationAppointmentDetails.Vaccine");

                var appointment = appointments.FirstOrDefault();

                if (appointment == null)
                {
                    throw new ArgumentException($"Không tìm thấy lịch đặt {appointmentId} cho facility {facilityId}");
                }

                // Check facility ownership sau khi load
                if (appointment.Schedule?.FacilityId != facilityId)
                {
                    throw new ArgumentException($"Lịch đặt {appointmentId} không thuộc về facility {facilityId}");
                }

                // Manual load Schedule nếu vẫn null
                if (appointment.Schedule == null)
                {
                    _logger.LogWarning("Schedule null, trying manual load for appointment {AppointmentId}", appointmentId);
                    var scheduleRepo = _unitOfWork.GetRepository<AppointmentSchedule>();
                    appointment.Schedule = await scheduleRepo.GetAsync(s => s.ScheduleId == appointment.ScheduleId);

                    if (appointment.Schedule == null)
                    {
                        throw new InvalidOperationException($"Không thể load Schedule cho appointment {appointmentId}");
                    }
                }

                // Validate status transition
                _logger.LogInformation("Status transition: {CurrentStatus} -> {NewStatus}", appointment.Status, updateDto.Status);

                if (!IsValidStatusTransition(appointment.Status, updateDto.Status))
                {
                    throw new InvalidOperationException($"Không thể chuyển từ trạng thái {appointment.Status} sang {updateDto.Status}");
                }

                // Update status
                appointment.Status = updateDto.Status;
                appointment.Note = updateDto.Note ?? appointment.Note;
                appointment.UpdatedAt = DateTime.UtcNow;

                // ✅ Nếu chuyển sang Paid, cập nhật VaccinationAppointmentDetails VÀ Order status
                if (updateDto.Status == "Paid")
                {
                    _logger.LogInformation("Đang cập nhật VaccinationAppointmentDetails và Order status cho appointment {AppointmentId}", appointmentId);

                    if (appointment.Schedule == null)
                    {
                        _logger.LogError("appointment.Schedule is null for appointment {AppointmentId}", appointmentId);
                        throw new InvalidOperationException("Không thể lấy thông tin lịch hẹn");
                    }

                    // ✅ Cập nhật VaccinationAppointmentDetails
                    var detailRepo = _unitOfWork.GetRepository<VaccinationAppointmentDetail>();
                    var details = await detailRepo.FindAsync(d => d.AppointmentId == appointmentId);

                    _logger.LogInformation("Tìm được {Count} VaccinationAppointmentDetails cho appointment {AppointmentId}", details.Count, appointmentId);

                    foreach (var detail in details)
                    {
                        detail.VaccinationDate = appointment.Schedule.Date;
                        _logger.LogInformation("Cập nhật VaccinationDate cho appointment {AppointmentId} thành {Date}", appointmentId, appointment.Schedule.Date);
                    }

                    // ✅ Cập nhật Order status nếu appointment có OrderId
                    if (appointment.OrderId.HasValue)
                    {
                        var orderRepo = _unitOfWork.GetRepository<Order>();
                        var order = await orderRepo.GetAsync(o => o.OrderId == appointment.OrderId.Value, "OrderDetails");

                        if (order != null)
                        {
                            if (order.Status == "Pending")
                            {
                                order.Status = "Paid";
                                order.UpdatedAt = DateTime.UtcNow;
                                orderRepo.Update(order);
                                _logger.LogInformation("Đã cập nhật Order {OrderId} status từ Pending sang Paid", order.OrderId);
                            }
                            else
                            {
                                _logger.LogInformation("Order {OrderId} đã có status {Status}, không cần cập nhật", order.OrderId, order.Status);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Không tìm thấy Order {OrderId} cho appointment {AppointmentId}", appointment.OrderId.Value, appointmentId);
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Appointment {AppointmentId} không có OrderId, chỉ cập nhật VaccinationAppointmentDetails", appointmentId);
                    }

                    // ✅ Cập nhật ChildVaccineProfile status thành "Completed" nếu có
                    var childVaccineProfileRepo = _unitOfWork.GetRepository<ChildVaccineProfile>();
                    var childVaccineProfiles = await childVaccineProfileRepo.FindAsync(
                        p => p.AppointmentId == appointmentId && p.Status == "Pending");

                    foreach (var profile in childVaccineProfiles)
                    {
                        profile.Status = "Completed";
                        profile.ActualDate = appointment.Schedule.Date;
                        profile.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        childVaccineProfileRepo.Update(profile);
                        _logger.LogInformation("Đã cập nhật ChildVaccineProfile {ProfileId} status thành Completed", profile.VaccineProfileId);
                    }

                    // ✅ RemainingQuantity đã được trừ khi đặt lịch, không cần trừ lại khi chuyển sang Paid
                    _logger.LogInformation("RemainingQuantity đã được trừ khi đặt lịch, không cần trừ lại khi chuyển sang Paid cho appointment {AppointmentId}", appointmentId);
                }
                // ✅ Nếu chuyển sang Cancelled, xoá appointmentId khỏi ChildVaccineProfile và trả lại số lượng vaccine
                else if (updateDto.Status == "Cancelled")
                {
                    _logger.LogInformation("Đang xử lý hủy appointment {AppointmentId} - xoá appointmentId khỏi ChildVaccineProfile và trả lại số lượng vaccine", appointmentId);

                    // ✅ Xoá appointmentId khỏi ChildVaccineProfile
                    var childVaccineProfileRepo = _unitOfWork.GetRepository<ChildVaccineProfile>();
                    var childVaccineProfiles = await childVaccineProfileRepo.FindAsync(
                        p => p.AppointmentId == appointmentId);

                    foreach (var profile in childVaccineProfiles)
                    {
                        // ✅ Xoá appointmentId và đặt lại status về "Pending" để user có thể đặt lại
                        profile.AppointmentId = null;
                        profile.Status = "Pending";
                        profile.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        childVaccineProfileRepo.Update(profile);
                        _logger.LogInformation("Đã xoá appointmentId khỏi ChildVaccineProfile {ProfileId} và đặt status về Pending", profile.VaccineProfileId);
                    }

                    // ✅ Trả lại số lượng vaccine cho Order hoặc FacilityVaccine
                    await RestoreVaccineQuantityOnCancelAsync(appointment);
                }

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Cập nhật trạng thái lịch đặt {AppointmentId} thành công", appointmentId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật trạng thái lịch đặt {AppointmentId} cho facility {FacilityId}. Chi tiết: {Message}",
                    appointmentId, facilityId, ex.Message);
                _logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);
                throw;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Kiểm tra slot có thể book được không (theo thời gian)
        /// </summary>
        private bool IsSlotBookable(AppointmentSchedule schedule)
        {
            if (schedule?.Slot?.StartTime == null)
                return false;

            var slotDateTime = schedule.Date.ToDateTime(schedule.Slot.StartTime.Value);
            var now = DateTime.Now;

            // Chỉ không được book trong quá khứ (khi slot đã bắt đầu)
            return slotDateTime >= now;
        }

        /// <summary>
        /// Map VaccinationAppointment sang AppointmentHistoryDTO
        /// </summary>
        private async Task<AppointmentHistoryDTO> MapToAppointmentHistoryDTO(VaccinationAppointment appointment)
        {
            var slotDateTime = appointment.Schedule.Date.ToDateTime(appointment.Schedule.Slot.StartTime ?? TimeOnly.MinValue);
            var now = DateTime.Now;

            // Get vaccine names from appointment details
            var vaccineNames = new List<string>();
            var detailRepo = _unitOfWork.GetRepository<VaccinationAppointmentDetail>();
            var details = await detailRepo.FindAsync(d => d.AppointmentId == appointment.AppointmentId, "Vaccine");
            vaccineNames = details.Select(d => d.Vaccine.Name).ToList();

            // Get package name if exists (from order)
            string? packageName = null;
            if (appointment.Order != null && appointment.Order.PackageId > 0)
            {
                var packageRepo = _unitOfWork.GetRepository<VaccinePackage>();
                var package = await packageRepo.GetByIdAsync(appointment.Order.PackageId);
                packageName = package?.Name;
            }

            // Calculate estimated cost - ✅ SỬ DỤNG GIÁ ĐÃ SNAPSHOT + KIỂM TRA ORDER STATUS
            decimal estimatedCostTotal = 0;
            if (appointment.Order?.OrderId != null)
            {
                // Trường hợp có Order - kiểm tra status để quyết định EstimatedCost
                if (appointment.Order.Status == "Paid")
                {
                    estimatedCostTotal = 0; // Đã thanh toán
                    _logger.LogInformation("💰 EstimatedCost = 0 (Order {OrderId} đã Paid)", appointment.Order.OrderId);
                }
                else
                {
                    estimatedCostTotal = appointment.Order.TotalAmount; // Chưa thanh toán
                    _logger.LogInformation("💰 EstimatedCost from Order: {OrderId} Status={Status} = {TotalAmount}", 
                        appointment.Order.OrderId, appointment.Order.Status, estimatedCostTotal);
                }
            }
            else
            {
                // Trường hợp vaccine lẻ - sử dụng FacilityVaccinePrice đã snapshot
                foreach (var detail in details)
                {
                    if (detail.FacilityVaccinePrice.HasValue)
                    {
                        estimatedCostTotal += detail.FacilityVaccinePrice.Value;
                        _logger.LogInformation("💰 EstimatedCost from VaccinationAppointmentDetail - VaccineId: {VaccineId}, Price: {Price}", 
                            detail.VaccineId, detail.FacilityVaccinePrice.Value);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ VaccinationAppointmentDetail {DetailId} không có FacilityVaccinePrice", detail.DetailId);
                        
                        // Fallback: lấy giá hiện tại nếu không có snapshot (data cũ)
                        var facilityVaccineRepo = _unitOfWork.GetRepository<FacilityVaccine>();
                        var fv = await facilityVaccineRepo.GetAsync(
                            f => f.FacilityId == appointment.Schedule.FacilityId && f.VaccineId == detail.VaccineId);
                        if (fv != null)
                        {
                            estimatedCostTotal += fv.Price;
                            _logger.LogInformation("💰 Fallback EstimatedCost from current FacilityVaccine - VaccineId: {VaccineId}, Price: {Price}", 
                                detail.VaccineId, fv.Price);
                        }
                    }
                }
            }

            var dto = new AppointmentHistoryDTO
            {
                AppointmentId = appointment.AppointmentId,
                ChildId = appointment.ChildId,
                ChildName = appointment.Child?.FullName ?? "",
                OrderId = appointment.OrderId,
                Status = appointment.Status,
                Note = appointment.Note,
                CreatedAt = appointment.CreatedAt,
                UpdatedAt = appointment.UpdatedAt,

                // Appointment info
                AppointmentDate = appointment.Schedule.Date,
                AppointmentTime = appointment.Schedule.Slot?.SlotTime ?? "",
                FacilityName = appointment.Schedule.Facility?.FacilityName ?? "",
                FacilityAddress = appointment.Schedule.Facility?.Address ?? "",

                // Vaccine info
                PackageName = packageName,
                VaccineNames = vaccineNames,

                // Cost
                EstimatedCost = estimatedCostTotal,

                // Status flags
                IsUpcoming = slotDateTime > now,
                IsPast = slotDateTime < now,
                CanCancel = appointment.Status == "Approval" && slotDateTime > now,
                CanReschedule = appointment.Status == "Approval" && slotDateTime > now,

                // Time countdown
                TimeUntilAppointment = CalculateTimeUntilAppointment(slotDateTime, now)
            };

            return dto;
        }

        /// <summary>
        /// Tính thống kê appointments
        /// </summary>
        private (int UpcomingCount, int CompletedCount, int CancelledCount) CalculateAppointmentStatistics(IEnumerable<VaccinationAppointment> appointments)
        {
            var now = DateTime.Now;
            var upcomingCount = 0;
            var completedCount = 0;
            var cancelledCount = 0;

            foreach (var appointment in appointments)
            {
                var slotDateTime = appointment.Schedule.Date.ToDateTime(appointment.Schedule.Slot?.StartTime ?? TimeOnly.MinValue);

                switch (appointment.Status)
                {
                    case "Paid":
                        completedCount++;
                        break;
                    case "Cancelled":
                        cancelledCount++;
                        break;
                    default:
                        if (slotDateTime > now)
                            upcomingCount++;
                        break;
                }
            }

            return (upcomingCount, completedCount, cancelledCount);
        }

        /// <summary>
        /// Tính thời gian đến appointment
        /// </summary>
        private string CalculateTimeUntilAppointment(DateTime slotDateTime, DateTime now)
        {
            if (slotDateTime < now)
            {
                return "Đã qua";
            }

            var timeSpan = slotDateTime - now;

            if (timeSpan.TotalDays >= 1)
            {
                return $"{(int)timeSpan.TotalDays} ngày nữa";
            }
            else if (timeSpan.TotalHours >= 1)
            {
                return $"{(int)timeSpan.TotalHours} giờ nữa";
            }
            else
            {
                return $"{(int)timeSpan.TotalMinutes} phút nữa";
            }
        }

        /// <summary>
        /// Tính BookedCount tự động cho các AppointmentSchedule từ appointments thực tế
        /// </summary>
        private async Task<List<AppointmentSchedule>> CalculateBookedCountForSchedules(List<AppointmentSchedule> schedules)
        {
            try
            {
                var scheduleIds = schedules.Select(s => s.ScheduleId).ToList();
                var appointmentRepo = _unitOfWork.GetRepository<VaccinationAppointment>();

                // Lấy tất cả appointments đã approval của các schedules
                var allAppointments = await appointmentRepo.FindAsync(
                    a => scheduleIds.Contains(a.ScheduleId) &&
                         a.Status == "Approval");

                _logger.LogInformation("Tìm được {Count} appointments approval", allAppointments.Count);

                // Load Slot manually nếu navigation property bị null
                var slotRepo = _unitOfWork.GetRepository<ScheduleSlot>();
                var slotIds = schedules.Select(s => s.SlotId).Distinct().ToList();
                var slots = await slotRepo.FindAsync(slot => slotIds.Contains(slot.SlotId));
                var slotDict = slots.ToDictionary(s => s.SlotId, s => s);

                // Tính BookedCount cho từng schedule
                foreach (var schedule in schedules)
                {
                    var bookedCount = allAppointments.Count(a => a.ScheduleId == schedule.ScheduleId);

                    // Safeguard: Đảm bảo BookedCount không vượt quá giới hạn hợp lý
                    if (bookedCount > 1000) // Giới hạn hợp lý
                    {
                        _logger.LogWarning("Schedule {ScheduleId} có BookedCount = {BookedCount} quá lớn, reset về 0",
                            schedule.ScheduleId, bookedCount);
                        bookedCount = 0;
                    }

                    schedule.BookedCount = bookedCount;

                    // Load Slot manually nếu null
                    if (schedule.Slot == null && slotDict.ContainsKey(schedule.SlotId))
                    {
                        schedule.Slot = slotDict[schedule.SlotId];
                        _logger.LogDebug("Loaded Slot manually for Schedule {ScheduleId}", schedule.ScheduleId);
                    }

                    _logger.LogDebug("Schedule {ScheduleId}: BookedCount = {BookedCount}, Slot = {SlotLoaded}",
                        schedule.ScheduleId, bookedCount, schedule.Slot != null ? "OK" : "NULL");
                }

                return schedules;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tính BookedCount cho schedules");

                // Fallback: Set tất cả BookedCount về 0 nếu có lỗi
                foreach (var schedule in schedules)
                {
                    schedule.BookedCount = 0;
                }

                return schedules;
            }
        }

        public async Task<List<AppointmentSuggestionDTO>> GenerateAppointmentSuggestionsAsync(AppointmentQuickBookingDTO request, int maxSuggestions = 5)
        {
            try
            {
                _logger.LogInformation("Tạo gợi ý đặt lịch cho trẻ {ChildId}", request.ChildId);

                var suggestions = new List<AppointmentSuggestionDTO>();

                // Find facilities with vaccines for this disease
                var facilities = await SearchFacilitiesByDiseaseAsync(request.DiseaseId);

                foreach (var facility in facilities.Facilities.Take(maxSuggestions))
                {
                    // Get next available slot for this facility
                    var fromDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1));
                    var toDate = fromDate.AddDays(30);

                    var availableSchedules = await GetAvailableSchedulesAsync(
                        facility.FacilityId,
                        fromDate,
                        toDate,
                        request.PreferredTimeSlots);

                    var nextAvailableSlot = availableSchedules.DailySchedules
                        .Where(d => d.IsAvailable)
                        .SelectMany(d => d.AvailableSlots)
                        .FirstOrDefault();

                    if (nextAvailableSlot != null)
                    {
                        // Calculate estimated cost
                        var vaccines = await GetFacilityVaccinesByDiseaseAsync(facility.FacilityId, request.DiseaseId);
                        var packageId = vaccines.VaccinePackages.FirstOrDefault()?.PackageId;
                        var facilityVaccineIds = vaccines.IndividualVaccines.Take(1).Select(v => v.FacilityVaccineId).ToList();

                        var cost = await CalculateEstimatedCostAsync(facility.FacilityId, null, packageId, facilityVaccineIds.Any() ? facilityVaccineIds : null);

                        suggestions.Add(new AppointmentSuggestionDTO
                        {
                            FacilityId = facility.FacilityId,
                            FacilityName = facility.FacilityName,
                            AvailableDate = availableSchedules.DailySchedules.First(d => d.IsAvailable).Date,
                            TimeSlot = nextAvailableSlot.SlotTime,
                            EstimatedCost = cost.TotalCost,
                            HasPackageOption = facility.HasPackages,
                            Distance = facility.Distance
                        });
                    }
                }

                return suggestions.OrderBy(s => s.AvailableDate).ThenBy(s => s.EstimatedCost).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo gợi ý đặt lịch cho trẻ {ChildId}", request.ChildId);
                throw;
            }
        }

        /// <summary>
        /// Map VaccinationAppointment sang FacilityAppointmentDTO
        /// </summary>
        private async Task<FacilityAppointmentDTO> MapToFacilityAppointmentDTO(VaccinationAppointment appointment)
        {
            var slotDateTime = appointment.Schedule.Date.ToDateTime(appointment.Schedule.Slot?.StartTime ?? TimeOnly.MinValue);
            var now = DateTime.Now;

            // Manual loading backup nếu Child chưa được load
            if (appointment.Child == null)
            {
                _logger.LogWarning("Child null, trying manual load for appointment {AppointmentId}", appointment.AppointmentId);
                var childRepo = _unitOfWork.GetRepository<Child>();
                appointment.Child = await childRepo.GetAsync(c => c.ChildId == appointment.ChildId, "Member,Member.Account");
            }

            // Manual loading backup nếu Order chưa được load
            if (appointment.OrderId.HasValue && appointment.Order == null)
            {
                _logger.LogWarning("Order null, trying manual load for appointment {AppointmentId}, OrderId {OrderId}",
                    appointment.AppointmentId, appointment.OrderId);
                var orderRepo = _unitOfWork.GetRepository<Order>();
                appointment.Order = await orderRepo.GetAsync(o => o.OrderId == appointment.OrderId.Value,
                    "OrderDetails.FacilityVaccine.Vaccine,OrderDetails.Disease,Member,Package");
            }

            // ✅ Lấy FacilityVaccines cho việc tính toán chi phí
            var facilityVaccines = new List<FacilityVaccineDTO>();
            
            // Get FacilityVaccines from Order.OrderDetails (for package/custom orders)
            if (appointment.Order?.OrderDetails != null && appointment.Order.OrderDetails.Any())
            {
                _logger.LogInformation("Found {OrderDetailsCount} OrderDetails for appointment {AppointmentId}",
                    appointment.Order.OrderDetails.Count, appointment.AppointmentId);

                foreach (var orderDetail in appointment.Order.OrderDetails)
                {
                    if (orderDetail.FacilityVaccine != null)
                    {
                        var facilityVaccineDto = _mapper.Map<FacilityVaccineDTO>(orderDetail.FacilityVaccine);
                        facilityVaccines.Add(facilityVaccineDto);
                        _logger.LogInformation("Added FacilityVaccine {FacilityVaccineId} from OrderDetail {OrderDetailId}",
                            orderDetail.FacilityVaccine.FacilityVaccineId, orderDetail.OrderDetailId);
                    }
                    else
                    {
                        _logger.LogWarning("OrderDetail {OrderDetailId} has null FacilityVaccine", orderDetail.OrderDetailId);
                    }
                }
            }
            // Fallback: Get FacilityVaccines from VaccinationAppointmentDetails (for individual vaccines)
            else
            {
                _logger.LogInformation("No Order or OrderDetails found, trying to get FacilityVaccines from VaccinationAppointmentDetails for appointment {AppointmentId}",
                    appointment.AppointmentId);

                var detailRepo = _unitOfWork.GetRepository<VaccinationAppointmentDetail>();
                var details = await detailRepo.FindAsync(d => d.AppointmentId == appointment.AppointmentId, "Vaccine");

                if (details.Any())
                {
                    var facilityVaccineRepo = _unitOfWork.GetRepository<FacilityVaccine>();
                    foreach (var detail in details)
                    {
                        // Find FacilityVaccine by VaccineId and FacilityId
                        var facilityVaccine = await facilityVaccineRepo.GetAsync(
                            fv => fv.VaccineId == detail.VaccineId && fv.FacilityId == appointment.Schedule.FacilityId,
                            "Vaccine");

                        if (facilityVaccine != null)
                        {
                            var facilityVaccineDto = _mapper.Map<FacilityVaccineDTO>(facilityVaccine);
                            facilityVaccines.Add(facilityVaccineDto);
                            _logger.LogInformation("Added FacilityVaccine {FacilityVaccineId} from VaccinationAppointmentDetail {DetailId}",
                                facilityVaccine.FacilityVaccineId, detail.DetailId);
                        }
                        else
                        {
                            _logger.LogWarning("No FacilityVaccine found for VaccineId {VaccineId} and FacilityId {FacilityId}",
                                detail.VaccineId, appointment.Schedule.FacilityId);
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("No VaccinationAppointmentDetails found for appointment {AppointmentId}", appointment.AppointmentId);
                }
            }

            // Calculate estimated cost - ✅ SỬ DỤNG GIÁ ĐÃ SNAPSHOT + KIỂM TRA ORDER STATUS
            decimal estimatedCostTotal = 0;
            
            if (appointment.Order?.OrderId != null)
            {
                // Trường hợp có Order - kiểm tra status để quyết định EstimatedCost
                if (appointment.Order.Status == "Paid")
                {
                    estimatedCostTotal = 0; // Đã thanh toán
                    _logger.LogInformation("💰 FacilityAppointment EstimatedCost = 0 (Order {OrderId} đã Paid)", appointment.Order.OrderId);
                }
                else
                {
                    estimatedCostTotal = appointment.Order.TotalAmount; // Chưa thanh toán
                    _logger.LogInformation("💰 FacilityAppointment EstimatedCost from Order: {OrderId} Status={Status} = {TotalAmount}", 
                        appointment.Order.OrderId, appointment.Order.Status, estimatedCostTotal);
                }
            }
            else
            {
                // Trường hợp vaccine lẻ - sử dụng FacilityVaccinePrice đã snapshot từ VaccinationAppointmentDetail
                var detailRepo = _unitOfWork.GetRepository<VaccinationAppointmentDetail>();
                var details = await detailRepo.FindAsync(d => d.AppointmentId == appointment.AppointmentId, "Vaccine");
                
                foreach (var detail in details)
                {
                    if (detail.FacilityVaccinePrice.HasValue)
                    {
                        estimatedCostTotal += detail.FacilityVaccinePrice.Value;
                        _logger.LogInformation("💰 FacilityAppointment EstimatedCost from VaccinationAppointmentDetail - VaccineId: {VaccineId}, Price: {Price}", 
                            detail.VaccineId, detail.FacilityVaccinePrice.Value);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ FacilityAppointment VaccinationAppointmentDetail {DetailId} không có FacilityVaccinePrice", detail.DetailId);
                        
                        // Fallback: lấy giá hiện tại nếu không có snapshot (data cũ)
                        var facilityVaccineRepo = _unitOfWork.GetRepository<FacilityVaccine>();
                        var fv = await facilityVaccineRepo.GetAsync(
                            f => f.FacilityId == appointment.Schedule.FacilityId && f.VaccineId == detail.VaccineId);
                        if (fv != null)
                        {
                            estimatedCostTotal += fv.Price;
                            _logger.LogInformation("💰 FacilityAppointment Fallback EstimatedCost from current FacilityVaccine - VaccineId: {VaccineId}, Price: {Price}", 
                                detail.VaccineId, fv.Price);
                        }
                    }
                }
            }

            var dto = new FacilityAppointmentDTO
            {
                AppointmentId = appointment.AppointmentId,
                Status = appointment.Status,
                CreatedAt = appointment.CreatedAt,
                UpdatedAt = appointment.UpdatedAt,
                Note = appointment.Note,

                // Member info
                MemberId = appointment.Child?.Member?.MemberId ?? 0,
                MemberName = appointment.Child?.Member?.FullName ?? "",
                MemberPhone = appointment.Child?.Member?.PhoneNumber ?? "",
                MemberEmail = appointment.Child?.Member?.Account?.Email ?? "",

                // Child info
                Child = _mapper.Map<ChildDTO>(appointment.Child),

                // Order and vaccines info
                OrderId = appointment.OrderId,
                Order = appointment.Order != null ? _mapper.Map<OrderDTO>(appointment.Order) : null,
                FacilityVaccines = facilityVaccines,

                // Appointment info
                AppointmentDate = appointment.Schedule.Date,
                AppointmentTime = appointment.Schedule.Slot?.SlotTime ?? "",
                SlotTime = appointment.Schedule.Slot?.SlotTime ?? "",

                // Cost
                EstimatedCost = estimatedCostTotal,

                // Status flags
                IsUpcoming = slotDateTime > now,
                IsPast = slotDateTime < now,
                CanApprove = appointment.Status == "Pending" && slotDateTime > now,
                CanReject = appointment.Status == "Pending" && slotDateTime > now,
                CanComplete = appointment.Status == "Approval" && slotDateTime <= now
            };

            // ✅ NEW LOGIC: Kiểm tra xem có nên hiển thị VaccinesToInject không
            bool shouldShowVaccinesToInject = true;
            
            // Nếu có Order, kiểm tra xem order đã bị sử dụng (mất mũi) chưa
            if (appointment.Order?.OrderDetails != null && appointment.Order.OrderDetails.Any())
            {
                // Lấy thông tin ban đầu của OrderDetails từ database để so sánh
                var orderDetailRepo = _unitOfWork.GetRepository<OrderDetail>();
                var currentOrderDetails = await orderDetailRepo.FindAsync(
                    od => od.OrderId == appointment.Order.OrderId,
                    includeProperties: "FacilityVaccine");
                
                bool hasUsedVaccines = false;
                
                // Kiểm tra từng OrderDetail xem có bị giảm RemainingQuantity không
                foreach (var orderDetail in currentOrderDetails)
                {
                    // Logic: Nếu order đã được mua và tiêm ít nhất 1 mũi, thì RemainingQuantity sẽ nhỏ hơn số lượng ban đầu
                    // Hoặc có thể kiểm tra xem có VaccinationAppointment nào khác đã Completed với order này không
                    
                    // Cách 1: Kiểm tra có appointment nào khác đã hoàn thành với order này không
                    var completedAppointments = await _unitOfWork.GetRepository<VaccinationAppointment>()
                        .FindAsync(va => va.OrderId == appointment.Order.OrderId 
                                      && va.AppointmentId != appointment.AppointmentId 
                                      && va.Status == "Completed");
                    
                    if (completedAppointments.Any())
                    {
                        hasUsedVaccines = true;
                        _logger.LogInformation("✅ Tìm thấy {Count} appointment đã completed với Order {OrderId}", 
                            completedAppointments.Count(), appointment.Order.OrderId);
                        break;
                    }
                    
                    // Cách 2: Kiểm tra RemainingQuantity (nếu < quantity ban đầu thì đã sử dụng)
                    // Tạm thời giả sử nếu RemainingQuantity = 0 thì đã sử dụng hết
                    if (orderDetail.RemainingQuantity == 0)
                    {
                        hasUsedVaccines = true;
                        _logger.LogInformation("✅ OrderDetail {OrderDetailId} có RemainingQuantity = 0, order đã được sử dụng", 
                            orderDetail.OrderDetailId);
                        break;
                    }
                }
                
                // Nếu order mới lần đầu (chưa có appointment completed và vẫn còn full quantity), không hiện VaccinesToInject
                if (!hasUsedVaccines)
                {
                    shouldShowVaccinesToInject = false;
                    _logger.LogInformation("🚫 Order {OrderId} chưa được sử dụng lần nào, không hiển thị VaccinesToInject cho appointment {AppointmentId}", 
                        appointment.Order.OrderId, appointment.AppointmentId);
                }
                else
                {
                    _logger.LogInformation("✅ Order {OrderId} đã được sử dụng trước đó, hiển thị VaccinesToInject cho appointment {AppointmentId}", 
                        appointment.Order.OrderId, appointment.AppointmentId);
                }
            }
            
            // ✅ Chỉ lấy vaccines từ VaccinationAppointmentDetails nếu shouldShowVaccinesToInject = true
            if (shouldShowVaccinesToInject)
            {
            _logger.LogInformation("🔍 DEBUG: Checking VaccinationAppointmentDetails for appointment {AppointmentId}: Count={Count}", 
                appointment.AppointmentId, appointment.VaccinationAppointmentDetails?.Count ?? 0);
                
            if (appointment.VaccinationAppointmentDetails != null && appointment.VaccinationAppointmentDetails.Any())
            {
                foreach (var vaccinationDetail in appointment.VaccinationAppointmentDetails)
                {
                    // Tìm ChildVaccineProfile để lấy DoseNumber và Disease
                    var childVaccineProfile = appointment.Child?.ChildVaccineProfiles?
                        .FirstOrDefault(cvp => cvp.VaccineId == vaccinationDetail.VaccineId 
                                            && cvp.AppointmentId == appointment.AppointmentId);
                    
                    if (vaccinationDetail.Vaccine != null)
                    {
                        // Ưu tiên DoseNum từ ChildVaccineProfile, fallback về VaccinationAppointmentDetail
                        var doseNumber = childVaccineProfile?.DoseNum.ToString() ?? vaccinationDetail.DoseNumber;
                        
                        // Lấy disease name từ ChildVaccineProfile hoặc fallback
                        var diseaseName = childVaccineProfile?.Disease?.Name ?? "Unknown Disease";
                        
                        // Tìm FacilityVaccineId từ VaccinationAppointmentDetail
                        int? facilityVaccineId = null;
                        
                        // Cách 1: Tìm từ Order.OrderDetails nếu có
                        if (appointment.Order?.OrderDetails != null)
                        {
                            var matchingOrderDetail = appointment.Order.OrderDetails
                                .FirstOrDefault(od => od.FacilityVaccine?.VaccineId == vaccinationDetail.VaccineId);
                            facilityVaccineId = matchingOrderDetail?.FacilityVaccineId;
                        }
                        
                        // Cách 2: Nếu không tìm được, query trực tiếp từ DB
                        if (!facilityVaccineId.HasValue)
                        {
                            var facilityVaccineRepo = _unitOfWork.GetRepository<FacilityVaccine>();
                            var facilityVaccine = await facilityVaccineRepo.GetAsync(
                                fv => fv.VaccineId == vaccinationDetail.VaccineId 
                                   && fv.FacilityId == appointment.Schedule.FacilityId);
                            facilityVaccineId = facilityVaccine?.FacilityVaccineId;
                        }

                        var vaccineToInject = new VaccineToInjectDTO
                        {
                            VaccineId = vaccinationDetail.VaccineId,
                            VaccineName = vaccinationDetail.Vaccine.Name,
                            DiseaseName = diseaseName,
                            DoseNumber = doseNumber,
                            Notes = vaccinationDetail.Notes,
                            FacilityVaccineId = facilityVaccineId,
                            Manufacturer = vaccinationDetail.Vaccine.Manufacturer,
                            SideEffects = vaccinationDetail.Vaccine.SideEffects,
                            Contraindications = vaccinationDetail.Vaccine.Contraindications
                        };
                        
                        dto.VaccinesToInject.Add(vaccineToInject);
                        
                        _logger.LogInformation("✅ Found vaccine to inject: {VaccineName} for disease {DiseaseName}, dose {DoseNumber}, FacilityVaccineId: {FacilityVaccineId}", 
                            vaccinationDetail.Vaccine.Name, diseaseName, doseNumber, facilityVaccineId);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Skipped VaccinationAppointmentDetail {DetailId}: Missing vaccine", 
                            vaccinationDetail.DetailId);
                    }
                }
            }
            else
            {
                _logger.LogWarning("⚠️ DEBUG: No VaccinationAppointmentDetails found for appointment {AppointmentId}, trying ChildVaccineProfile fallback", 
                    appointment.AppointmentId);
                
                // ✅ FALLBACK: Lấy từ ChildVaccineProfile với AppointmentId cụ thể này
                if (appointment.Child?.ChildVaccineProfiles != null && appointment.Child.ChildVaccineProfiles.Any())
                {
                    var relevantProfiles = appointment.Child.ChildVaccineProfiles
                        .Where(cvp => cvp.AppointmentId == appointment.AppointmentId)
                        .ToList();
                        
                    _logger.LogInformation("🔍 Found {ProfileCount} ChildVaccineProfiles for appointment {AppointmentId}", 
                        relevantProfiles.Count, appointment.AppointmentId);
                    
                    foreach (var profile in relevantProfiles)
                    {
                        if (profile.Vaccine != null && profile.Disease != null)
                        {
                            // Tìm FacilityVaccineId từ ChildVaccineProfile
                            int? facilityVaccineId = null;
                            
                            // Cách 1: Tìm từ Order.OrderDetails nếu có
                            if (appointment.Order?.OrderDetails != null)
                            {
                                var matchingOrderDetail = appointment.Order.OrderDetails
                                    .FirstOrDefault(od => od.FacilityVaccine?.VaccineId == profile.VaccineId);
                                facilityVaccineId = matchingOrderDetail?.FacilityVaccineId;
                            }
                            
                            // Cách 2: Nếu không tìm được, query trực tiếp từ DB
                            if (!facilityVaccineId.HasValue)
                            {
                                var facilityVaccineRepo = _unitOfWork.GetRepository<FacilityVaccine>();
                                var facilityVaccine = await facilityVaccineRepo.GetAsync(
                                    fv => fv.VaccineId == profile.VaccineId 
                                       && fv.FacilityId == appointment.Schedule.FacilityId);
                                facilityVaccineId = facilityVaccine?.FacilityVaccineId;
                            }

                            var vaccineToInject = new VaccineToInjectDTO
                            {
                                VaccineId = profile.VaccineId,
                                VaccineName = profile.Vaccine.Name,
                                DiseaseName = profile.Disease.Name,
                                DoseNumber = profile.DoseNum.ToString(),
                                Notes = profile.Note,
                                FacilityVaccineId = facilityVaccineId,
                                Manufacturer = profile.Vaccine.Manufacturer,
                                SideEffects = profile.Vaccine.SideEffects,
                                Contraindications = profile.Vaccine.Contraindications
                            };
                            
                            dto.VaccinesToInject.Add(vaccineToInject);
                            
                            _logger.LogInformation("✅ Found vaccine from ChildVaccineProfile: {VaccineName} for disease {DiseaseName}, dose {DoseNumber}, FacilityVaccineId: {FacilityVaccineId}", 
                                profile.Vaccine.Name, profile.Disease.Name, profile.DoseNum, facilityVaccineId);
                        }
                    }
                }
                }
            }
            else
            {
                _logger.LogInformation("🚫 Không hiển thị VaccinesToInject cho appointment {AppointmentId} vì order chưa được sử dụng", 
                    appointment.AppointmentId);
            }

            _logger.LogInformation("Mapped FacilityAppointmentDTO for appointment {AppointmentId} with {FacilityVaccinesCount} vaccines and {VaccinesToInjectCount} vaccines to inject",
                appointment.AppointmentId, facilityVaccines.Count, dto.VaccinesToInject.Count);

            return dto;
        }

        /// <summary>
        /// Tính thống kê appointments cho facility
        /// </summary>
        private (int PendingCount, int ConfirmedCount, int CompletedCount, int CancelledCount, int RefundingCount, int RefundedCount, int TodayCount)
            CalculateFacilityAppointmentStatistics(IEnumerable<VaccinationAppointment> appointments)
        {
            var now = DateTime.Now;
            var today = DateOnly.FromDateTime(DateTime.Today);
            var pendingCount = 0;
            var confirmedCount = 0;
            var completedCount = 0;
            var cancelledCount = 0;
            var refundingCount = 0;     // ✅ Đang chờ Manager duyệt hoàn tiền
            var refundedCount = 0;      // ✅ Đã được duyệt hoàn tiền
            var todayCount = 0;

            foreach (var appointment in appointments)
            {
                var slotDateTime = appointment.Schedule.Date.ToDateTime(appointment.Schedule.Slot?.StartTime ?? TimeOnly.MinValue);

                // Count by status
                switch (appointment.Status)
                {
                    case "Pending":
                        pendingCount++;
                        break;
                    case "Approval":
                        confirmedCount++;
                        break;
                    case "Paid":
                        completedCount++;
                        break;
                    case "Cancelled":
                        cancelledCount++;
                        break;
                    case "Refunding":           // ✅ Đang chờ duyệt hoàn tiền
                        refundingCount++;
                        break;
                    case "Accepted":            // ✅ Đã duyệt hoàn tiền (refunded)
                        refundedCount++;
                        break;
                }

                // Count today's appointments
                if (appointment.Schedule.Date == today)
                {
                    todayCount++;
                }
            }

            return (pendingCount, confirmedCount, completedCount, cancelledCount, refundingCount, refundedCount, todayCount);
        }

        /// <summary>
        /// Kiểm tra chuyển đổi trạng thái có hợp lệ không
        /// </summary>
        private bool IsValidStatusTransition(string currentStatus, string newStatus)
        {
            return (currentStatus, newStatus) switch
            {
                ("Pending", "Approval") => true,
                ("Pending", "Rejected") => true,
                ("Approval", "Paid") => true,
                ("Approval", "Cancelled") => true,
                ("Paid", "Refunding") => true,          // ✅ Staff có thể chuyển từ Paid sang Refunding
                ("Refunding", "Accepted") => true,      // ✅ Manager có thể approve refund
                _ => false
            };
        }

        /// <summary>
        /// Manager approve refund (Refunding -> Accepted)
        /// </summary>
        public async Task<bool> ApproveRefundAsync(int appointmentId, int facilityId, string? note = null)
        {
            try
            {
                _logger.LogInformation("Manager duyệt hoàn tiền cho appointment {AppointmentId} tại facility {FacilityId}",
                    appointmentId, facilityId);

                var appointmentRepo = _unitOfWork.GetRepository<VaccinationAppointment>();
                var appointment = await appointmentRepo.GetAsync(
                    a => a.AppointmentId == appointmentId,
                    "Schedule,Schedule.Facility");

                if (appointment == null)
                {
                    throw new ArgumentException($"Không tìm thấy lịch đặt {appointmentId}");
                }

                // Verify facility ownership
                if (appointment.Schedule?.FacilityId != facilityId)
                {
                    throw new ArgumentException($"Lịch đặt {appointmentId} không thuộc về facility {facilityId}");
                }

                // Validate current status must be Refunding
                if (appointment.Status != "Refunding")
                {
                    throw new InvalidOperationException($"Không thể duyệt hoàn tiền cho appointment có trạng thái {appointment.Status}. Chỉ có thể duyệt khi trạng thái là Refunding.");
                }

                // Update to Accepted
                appointment.Status = "Accepted";
                appointment.Note = !string.IsNullOrEmpty(note)
                    ? $"{appointment.Note}\n[Manager Approved Refund]: {note}"
                    : $"{appointment.Note}\n[Manager Approved Refund]";
                appointment.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Duyệt hoàn tiền thành công cho appointment {AppointmentId}", appointmentId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi duyệt hoàn tiền cho appointment {AppointmentId} tại facility {FacilityId}",
                    appointmentId, facilityId);
                throw;
            }
        }

        #endregion

        #region Rebooking Methods

        public async Task<ResponseDataModel<AppointmentRebookingValidationDTO>> ValidateRebookingRequestAsync(int childVaccineProfileId, int accountId)
        {
            try
            {
                _logger.LogInformation("Validating rebooking request for ChildVaccineProfile {ProfileId} by Account {AccountId}",
                    childVaccineProfileId, accountId);

                // 1. Lấy thông tin ChildVaccineProfile
                var profileRepo = _unitOfWork.GetRepository<ChildVaccineProfile>();
                var profile = await profileRepo.GetAsync(
                    p => p.VaccineProfileId == childVaccineProfileId,
                    includeProperties: "Child,Child.Member,Vaccine,Disease"
                );

                if (profile == null)
                {
                    var errorResponse = new AppointmentRebookingValidationDTO
                    {
                        CanRebook = false,
                        ReasonCannotRebook = "Không tìm thấy lịch tiêm vaccine",
                        HasApplicableOrder = false,
                        RequiresPayment = true,
                        EstimatedCost = 0
                    };
                    return CreateErrorResponse("Không tìm thấy lịch tiêm vaccine", errorResponse);
                }

                // 2. Validate ownership - profile phải thuộc về member của account hiện tại
                var memberRepo = _unitOfWork.GetRepository<Member>();
                var member = await memberRepo.GetAsync(m => m.AccountId == accountId);
                if (member == null || profile.Child.MemberId != member.MemberId)
                {
                    var errorResponse = new AppointmentRebookingValidationDTO
                    {
                        CanRebook = false,
                        ReasonCannotRebook = "Bạn không có quyền đặt lịch cho trẻ này",
                        HasApplicableOrder = false,
                        RequiresPayment = true,
                        EstimatedCost = 0
                    };
                    return CreateErrorResponse("Bạn không có quyền đặt lịch cho trẻ này", errorResponse);
                }

                // 3. Validate trạng thái profile - phải chưa được đặt lịch (AppointmentId = null)
                if (profile.AppointmentId.HasValue)
                {
                    var errorResponse = new AppointmentRebookingValidationDTO
                    {
                        CanRebook = false,
                        ReasonCannotRebook = "Lịch tiêm này đã được đặt rồi",
                        HasApplicableOrder = false,
                        RequiresPayment = true,
                        EstimatedCost = 0
                    };
                    return CreateErrorResponse("Lịch tiêm này đã được đặt rồi", errorResponse);
                }

                // 4. Validate ExpectedDate - phải có ngày dự kiến
                if (!profile.ExpectedDate.HasValue)
                {
                    var errorResponse = new AppointmentRebookingValidationDTO
                    {
                        CanRebook = false,
                        ReasonCannotRebook = "Chưa có ngày dự kiến tiêm vaccine này",
                        HasApplicableOrder = false,
                        RequiresPayment = true,
                        EstimatedCost = 0
                    };
                    return CreateErrorResponse("Chưa có ngày dự kiến tiêm vaccine này", errorResponse);
                }

                // 5. Validate DoseNum - phải nhỏ hơn hoặc bằng số liều tối đa của vaccine
                if (profile.DoseNum > profile.Vaccine.NumberOfDoses)
                {
                    var errorMessage = $"Số mũi {profile.DoseNum} vượt quá số liều tối đa {profile.Vaccine.NumberOfDoses} của vaccine {profile.Vaccine.Name}";
                    var errorResponse = new AppointmentRebookingValidationDTO
                    {
                        CanRebook = false,
                        ReasonCannotRebook = errorMessage,
                        HasApplicableOrder = false,
                        RequiresPayment = true,
                        EstimatedCost = 0
                    };
                    return CreateErrorResponse(errorMessage, errorResponse);
                }

                // 6. Validate ActualDate - nếu đã có ActualDate thì không cho rebook (đã tiêm xong)
                if (profile.ActualDate.HasValue)
                {
                    var errorMessage = $"Mũi {profile.DoseNum} của vaccine {profile.Vaccine.Name} đã được tiêm vào ngày {profile.ActualDate.Value:dd/MM/yyyy}";
                    var errorResponse = new AppointmentRebookingValidationDTO
                    {
                        CanRebook = false,
                        ReasonCannotRebook = errorMessage,
                        HasApplicableOrder = false,
                        RequiresPayment = true,
                        EstimatedCost = 0
                    };
                    return CreateErrorResponse(errorMessage, errorResponse);
                }

                // Tiếp tục logic validation
                var validationResult = await ValidateOrderAndCostAsync(profile, accountId);
                return CreateSuccessResponse(validationResult, "Validation thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi validate rebooking request cho ChildVaccineProfile {ProfileId}", childVaccineProfileId);
                var errorResponse = new AppointmentRebookingValidationDTO
                {
                    CanRebook = false,
                    ReasonCannotRebook = $"Có lỗi xảy ra khi kiểm tra: {ex.Message}",
                    HasApplicableOrder = false,
                    RequiresPayment = true,
                    EstimatedCost = 0
                };
                return CreateErrorResponse($"Có lỗi xảy ra khi kiểm tra: {ex.Message}", errorResponse);
            }
        }

        #endregion

        #region Helper Methods for ChildVaccineProfile

        /// <summary>
        /// Tạo ChildVaccineProfile với status "Pending" cho tất cả vaccines được book trong appointment
        /// </summary>
        private async Task CreateChildVaccineProfilesForAppointmentAsync(VaccinationAppointment appointment, AppointmentBookingRequestDTO request, DateOnly expectedDate)
        {
            try
            {
                _logger.LogInformation("🚀 Tạo ChildVaccineProfile cho appointment {AppointmentId}, Child {ChildId}, Disease {DiseaseId}", 
                    appointment.AppointmentId, appointment.ChildId, request.DiseaseId);

                var childVaccineProfileRepo = _unitOfWork.GetRepository<ChildVaccineProfile>();
                if (!request.DiseaseId.HasValue)
                {
                    _logger.LogWarning("Không có DiseaseId trong request khi tạo ChildVaccineProfile cho appointment {AppointmentId}", appointment.AppointmentId);
                    return;
                }
                var diseaseId = request.DiseaseId.Value;

                // LUỒNG 1: Order đã có - Lấy vaccines từ OrderDetails
                if (request.OrderId.HasValue && request.OrderId.Value > 0)
                {
                    _logger.LogInformation("Tạo ChildVaccineProfile từ Order {OrderId}", request.OrderId.Value);

                    var orderDetailRepo = _unitOfWork.GetRepository<OrderDetail>();
                    var orderDetails = await orderDetailRepo.FindAsync(
                        od => od.OrderId == request.OrderId.Value,
                        includeProperties: "FacilityVaccine,FacilityVaccine.Vaccine,FacilityVaccine.Vaccine.VaccineDiseases");

                    // Chỉ chọn 1 dòng order phù hợp với bệnh đã chọn và đúng cơ sở của lịch
                    var matchedDetails = orderDetails
                        .Where(od => od.DiseaseId == diseaseId
                                  && od.FacilityVaccine != null
                                  && od.FacilityVaccine.FacilityId == request.FacilityId)
                        .ToList();

                    var selectedDetail = matchedDetails
                        .OrderByDescending(od => od.RemainingQuantity)
                        .ThenBy(od => od.OrderDetailId)
                        .FirstOrDefault();

                    if (selectedDetail != null && selectedDetail.FacilityVaccine?.Vaccine != null)
                    {
                        _logger.LogInformation("📋 ORDER: Sử dụng VaccineId {VaccineId} từ OrderDetail {OrderDetailId} cho Disease {DiseaseId}", 
                            selectedDetail.FacilityVaccine.VaccineId, selectedDetail.OrderDetailId, selectedDetail.DiseaseId);
                            
                        // ✅ Tạo ChildVaccineProfile cho TẤT CẢ bệnh mà vaccine có thể chữa
                        await CreateChildVaccineProfilesForMultiDiseaseVaccineAsync(
                            childVaccineProfileRepo,
                            appointment.ChildId,
                            selectedDetail.FacilityVaccine.VaccineId,
                            selectedDetail.DiseaseId, // Disease được chọn chính
                            appointment.AppointmentId,
                            expectedDate,
                            selectedDetail.FacilityVaccine.Vaccine.NumberOfDoses,
                            selectedDetail.FacilityVaccine.Vaccine.VaccineDiseases);
                    }
                }
                // LUỒNG 2: Package mới - Lấy vaccines từ PackageVaccines
                else if (request.PackageId.HasValue && request.PackageId.Value > 0)
                {
                    _logger.LogInformation("Tạo ChildVaccineProfile từ Package {PackageId}", request.PackageId.Value);

                    var packageVaccineRepo = _unitOfWork.GetRepository<PackageVaccine>();
                    var packageVaccines = await packageVaccineRepo.FindAsync(
                        pv => pv.PackageId == request.PackageId.Value,
                        includeProperties: "FacilityVaccine,FacilityVaccine.Vaccine,FacilityVaccine.Vaccine.VaccineDiseases");

                    // Chỉ chọn 1 vaccine trong gói phù hợp với bệnh đã chọn (và ưu tiên đúng cơ sở đặt lịch)
                    var matchedPackageVaccines = packageVaccines
                        .Where(pv => pv.DiseaseId == diseaseId
                                  && pv.FacilityVaccine != null
                                  && pv.FacilityVaccine.FacilityId == request.FacilityId)
                        .ToList();

                    var selectedPackageVaccine = matchedPackageVaccines
                        .OrderBy(pv => pv.PackageVaccineId)
                        .FirstOrDefault() ?? packageVaccines
                        .Where(pv => pv.DiseaseId == diseaseId)
                        .OrderBy(pv => pv.PackageVaccineId)
                        .FirstOrDefault();

                    if (selectedPackageVaccine != null && selectedPackageVaccine.FacilityVaccine?.Vaccine != null)
                    {
                        _logger.LogInformation("📦 PACKAGE: Sử dụng VaccineId {VaccineId} từ Package {PackageId} cho Disease {DiseaseId}", 
                            selectedPackageVaccine.FacilityVaccine.VaccineId, request.PackageId.Value, selectedPackageVaccine.DiseaseId);
                            
                        // ✅ Tạo ChildVaccineProfile cho TẤT CẢ bệnh mà vaccine có thể chữa
                        await CreateChildVaccineProfilesForMultiDiseaseVaccineAsync(
                            childVaccineProfileRepo,
                            appointment.ChildId,
                            selectedPackageVaccine.FacilityVaccine.VaccineId,
                            selectedPackageVaccine.DiseaseId, // Disease được chọn chính
                            appointment.AppointmentId,
                            expectedDate,
                            selectedPackageVaccine.FacilityVaccine.Vaccine.NumberOfDoses,
                            selectedPackageVaccine.FacilityVaccine.Vaccine.VaccineDiseases);
                    }
                }
                // LUỒNG 3: Vaccine lẻ - Lấy từ FacilityVaccineIds
                else if (request.FacilityVaccineIds != null && request.FacilityVaccineIds.Any())
                {
                    _logger.LogInformation("Tạo ChildVaccineProfile từ {Count} vaccine lẻ", request.FacilityVaccineIds.Count);

                    var facilityVaccineRepo = _unitOfWork.GetRepository<FacilityVaccine>();

                    foreach (var facilityVaccineId in request.FacilityVaccineIds)
                    {
                        var facilityVaccine = await facilityVaccineRepo.GetAsync(
                            fv => fv.FacilityVaccineId == facilityVaccineId,
                            includeProperties: "Vaccine,Vaccine.VaccineDiseases");

                        if (facilityVaccine?.Vaccine != null)
                        {
                            var canTreat = facilityVaccine.Vaccine.VaccineDiseases?.Any(vd => vd.DiseaseId == diseaseId) ?? false;
                            if (canTreat)
                            {
                                _logger.LogInformation("💉 VACCINE LẺ: Sử dụng VaccineId {VaccineId} từ FacilityVaccine {FacilityVaccineId} cho Disease {DiseaseId}", 
                                    facilityVaccine.VaccineId, facilityVaccineId, diseaseId);
                                    
                                // ✅ Tạo ChildVaccineProfile cho TẤT CẢ bệnh mà vaccine có thể chữa
                                await CreateChildVaccineProfilesForMultiDiseaseVaccineAsync(
                                    childVaccineProfileRepo,
                                    appointment.ChildId,
                                    facilityVaccine.VaccineId,
                                    diseaseId, // Disease được chọn chính
                                    appointment.AppointmentId,
                                    expectedDate,
                                    facilityVaccine.Vaccine.NumberOfDoses,
                                    facilityVaccine.Vaccine.VaccineDiseases);
                                break; // Đã xử lý vaccine này
                            }
                        }
                    }
                }

                _logger.LogInformation("Hoàn thành tạo ChildVaccineProfile cho appointment {AppointmentId}", appointment.AppointmentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo ChildVaccineProfile cho appointment {AppointmentId}", appointment.AppointmentId);
                throw;
            }
        }

        /// <summary>
        /// Tạo một ChildVaccineProfile với logic kiểm tra trùng lặp
        /// </summary>
        private async Task CreateChildVaccineProfileAsync(
            IGenericRepository<ChildVaccineProfile> childVaccineProfileRepo,
            int childId,
            int vaccineId,
            int diseaseId,
            int appointmentId,
            DateOnly expectedDate,
            int totalDoses)
        {
            try
            {
                // ✅ FIX: Kiểm tra duplicate theo AppointmentId (không tạo 2 CVP cho cùng 1 appointment)
                var duplicateForAppointment = await childVaccineProfileRepo.GetAsync(
                    p => p.ChildId == childId &&
                         p.VaccineId == vaccineId &&
                         p.DiseaseId == diseaseId &&
                         p.AppointmentId == appointmentId);

                if (duplicateForAppointment != null)
                {
                    _logger.LogInformation("ChildVaccineProfile đã tồn tại cho Appointment {AppointmentId}, bỏ qua",
                        appointmentId);
                    return;
                }

                // ✅ LOGIC MỚI: Ưu tiên dùng lại CVP "Scheduled" trước khi tạo mới
                var existingProfiles = await childVaccineProfileRepo.FindAsync(
                    p => p.ChildId == childId &&
                         p.VaccineId == vaccineId &&
                         p.DiseaseId == diseaseId);

                _logger.LogInformation("🔍 DEBUG: Tìm thấy {Count} CVP existing cho Child {ChildId}, Vaccine {VaccineId}, Disease {DiseaseId}", 
                    existingProfiles.Count(), childId, vaccineId, diseaseId);

                foreach (var profile in existingProfiles)
                {
                    _logger.LogInformation("🔍 DEBUG: CVP ID {ProfileId}, Dose {DoseNum}, Status '{Status}', AppointmentId {AppointmentId}", 
                        profile.VaccineProfileId, profile.DoseNum, profile.Status, profile.AppointmentId);
                }

                // 1. Tìm CVP "Pending" không có appointmentId để dùng lại (ưu tiên cao nhất - từ cancel appointment)
                var pendingProfile = existingProfiles
                    .Where(p => p.Status == "Pending" && p.AppointmentId == null)
                    .OrderBy(p => p.DoseNum)
                    .FirstOrDefault();

                if (pendingProfile != null)
                {
                    _logger.LogInformation("🎯 DEBUG: Tìm thấy CVP Pending (không có appointmentId) để dùng lại - ID {ProfileId}, Dose {DoseNum}", 
                        pendingProfile.VaccineProfileId, pendingProfile.DoseNum);
                    
                    // ✅ Dùng lại CVP "Pending" - gắn appointment mới
                    pendingProfile.AppointmentId = appointmentId;
                    pendingProfile.ExpectedDate = expectedDate;
                    pendingProfile.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    childVaccineProfileRepo.Update(pendingProfile);
                    
                    _logger.LogInformation("✅ Đã dùng lại CVP Pending (ID: {ProfileId}, Dose: {DoseNum}) và gắn appointment {AppointmentId} cho Child {ChildId}, Vaccine {VaccineId}",
                        pendingProfile.VaccineProfileId, pendingProfile.DoseNum, appointmentId, childId, vaccineId);
                    return;
                }

                // 2. Tìm CVP "Scheduled" để dùng lại (ưu tiên thứ hai)
                var scheduledProfile = existingProfiles
                    .Where(p => p.Status == "Scheduled")
                    .OrderBy(p => p.DoseNum)
                    .FirstOrDefault();

                if (scheduledProfile != null)
                {
                    _logger.LogInformation("🎯 DEBUG: Tìm thấy CVP Scheduled để dùng lại - ID {ProfileId}, Dose {DoseNum}", 
                        scheduledProfile.VaccineProfileId, scheduledProfile.DoseNum);
                    
                    // ✅ Dùng lại CVP "Scheduled" - chuyển thành "Pending" và gắn appointment
                    scheduledProfile.AppointmentId = appointmentId;
                    scheduledProfile.Status = "Pending";
                    scheduledProfile.ExpectedDate = expectedDate;
                    scheduledProfile.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    childVaccineProfileRepo.Update(scheduledProfile);
                    
                    _logger.LogInformation("✅ Đã dùng lại CVP Scheduled (ID: {ProfileId}, Dose: {DoseNum}) và chuyển thành 'Pending' cho Child {ChildId}, Vaccine {VaccineId}",
                        scheduledProfile.VaccineProfileId, scheduledProfile.DoseNum, childId, vaccineId);
                    return;
                }
                else
                {
                    _logger.LogInformation("⚠️ DEBUG: KHÔNG tìm thấy CVP nào có status 'Pending' hoặc 'Scheduled' để dùng lại cho Child {ChildId}, Vaccine {VaccineId}, Disease {DiseaseId}", 
                        childId, vaccineId, diseaseId);
                }

                // 3. Nếu không có CVP "Pending" hoặc "Scheduled", tính dose number mới
                var nextDoseNum = 1;
                if (existingProfiles.Any())
                {
                    var maxDose = existingProfiles.Max(p => p.DoseNum);
                    nextDoseNum = maxDose + 1;
                    
                    _logger.LogInformation("Child {ChildId} đã có {ExistingCount} liều cho Vaccine {VaccineId}/Disease {DiseaseId}, dose tiếp theo: {NextDose}",
                        childId, existingProfiles.Count(), vaccineId, diseaseId, nextDoseNum);
                }

                // 4. Kiểm tra không vượt quá số liều tối đa
                if (nextDoseNum > totalDoses)
                {
                    _logger.LogWarning("Dose {DoseNum} vượt quá số liều tối đa {TotalDoses} cho vaccine {VaccineId}, không tạo CVP",
                        nextDoseNum, totalDoses, vaccineId);
                    return;
                }

                // 5. Kiểm tra xem đã có CVP "Pending" cho dose này chưa (tránh duplicate)
                var existingPendingForDose = await childVaccineProfileRepo.GetAsync(
                    p => p.ChildId == childId &&
                         p.VaccineId == vaccineId &&
                         p.DiseaseId == diseaseId &&
                         p.DoseNum == nextDoseNum &&
                         p.Status == "Pending");

                if (existingPendingForDose != null)
                {
                    _logger.LogWarning("Đã có CVP Pending cho Child {ChildId}, Vaccine {VaccineId}, Disease {DiseaseId}, Dose {DoseNum}. Không tạo thêm.",
                        childId, vaccineId, diseaseId, nextDoseNum);
                    return;
                }

                // Tạo ChildVaccineProfile mới
                var newProfile = new ChildVaccineProfile
                {
                    ChildId = childId,
                    VaccineId = vaccineId,
                    DiseaseId = diseaseId,
                    AppointmentId = appointmentId,
                    DoseNum = nextDoseNum,
                    ExpectedDate = expectedDate,
                    Status = "Pending", // ✅ Đặt status thành "Pending" khi book appointment
                    IsRequired = true,
                    Priority = "High",
                    Note = $"Mũi {nextDoseNum}/{totalDoses} - Được tạo từ appointment booking",
                    CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };

                await childVaccineProfileRepo.AddAsync(newProfile);

                _logger.LogInformation("✅ Đã tạo ChildVaccineProfile mới cho Child {ChildId}, Vaccine {VaccineId}, Disease {DiseaseId}, Dose {DoseNum}/{TotalDoses} với status 'Pending'",
                    childId, vaccineId, diseaseId, nextDoseNum, totalDoses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo ChildVaccineProfile cho Child {ChildId}, Vaccine {VaccineId}, Disease {DiseaseId}",
                    childId, vaccineId, diseaseId);
                throw;
            }
        }

        #endregion

        public async Task<AppointmentStatsDTO> GetAppointmentStatsByFacilityAsync(int facilityId)
        {
            try
            {
                _logger.LogInformation($"Calculating appointment stats for FacilityId: {facilityId}");
                var repository = _unitOfWork.GetRepository<VaccinationAppointment>();

                var appointments = await repository.GetAllAsync(
                    filter: a => a.Schedule.FacilityId == facilityId,
                    include: "Schedule,Order" 
                );

                var data = appointments.Data;

                var totalAppointments = data.Count();

                var packageAppointments = data.Count(a => a.OrderId.HasValue && a.Order != null && a.Order.PackageId > 0);
                var individualAppointments = data.Count(a => !a.OrderId.HasValue);

                var pending = data.Count(a => a.Status == "Pending");
                var completed = data.Count(a => a.Status == "Completed");
                var approval = data.Count(a => a.Status == "Approval");
                var cancelled = data.Count(a => a.Status == "Cancelled");
                var paid = data.Count(a => a.Status == "Paid");

                var uniqueChildrenVaccinated = data
                    .Where(a => a.Status == "Completed")
                    .Select(a => a.ChildId)
                    .Distinct()
                    .Count();

                return new AppointmentStatsDTO
                {
                    TotalAppointments = totalAppointments,
                    PackageAppointments = packageAppointments,
                    IndividualAppointments = individualAppointments,
                    Pending = pending,
                    Completed = completed,
                    Approval = approval,
                    Cancelled = cancelled,
                    Paid = paid,
                    UniqueChildrenVaccinated = uniqueChildrenVaccinated
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error calculating appointment stats for FacilityId {facilityId}");
                throw;
            }
        }
        public async Task<AppointmentStatsDTO> GetAppointmentStatsAsync()
        {
            try
            {
                _logger.LogInformation("Calculating appointment stats for the entire system");
                var repository = _unitOfWork.GetRepository<VaccinationAppointment>();

                var appointments = await repository.GetAllAsync(
                    include: "Schedule,Order"
                );

                var data = appointments.Data;

                var totalAppointments = data.Count();

                var packageAppointments = data.Count(a => a.OrderId.HasValue && a.Order != null && a.Order.PackageId > 0);
                var individualAppointments = data.Count(a => !a.OrderId.HasValue);

                var pending = data.Count(a => a.Status == "Pending");
                var completed = data.Count(a => a.Status == "Completed");
                var approval = data.Count(a => a.Status == "Approval");
                var cancelled = data.Count(a => a.Status == "Cancelled");
                var paid = data.Count(a => a.Status == "Paid");

                var uniqueChildrenVaccinated = data
                    .Where(a => a.Status == "Completed")
                    .Select(a => a.ChildId)
                    .Distinct()
                    .Count();

                return new AppointmentStatsDTO
                {
                    TotalAppointments = totalAppointments,
                    PackageAppointments = packageAppointments,
                    IndividualAppointments = individualAppointments,
                    Pending = pending,
                    Completed = completed,
                    Approval = approval,
                    Cancelled = cancelled,
                    Paid = paid,
                    UniqueChildrenVaccinated = uniqueChildrenVaccinated
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating appointment stats for the entire system");
                throw;
            }
        }

        #region Cancel and Rebook Methods

        /// <summary>
        /// Cancel appointment hiện tại và đặt lại lịch mới cho user
        /// </summary>
        public async Task<ResponseDataModel<CancelAndRebookResponseDTO>> CancelAndRebookAppointmentAsync(CancelAndRebookRequestDTO request, int staffAccountId)
        {
            Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction = null;
            try
            {
                transaction = await _unitOfWork.BeginTransactionAsync();
                _logger.LogInformation("Bắt đầu Cancel và Rebook appointment {CurrentAppointmentId} -> Schedule {NewScheduleId}", 
                    request.CurrentAppointmentId, request.NewScheduleId);

                // 1. Validate staff permissions
                var staffRepo = _unitOfWork.GetRepository<FacilityStaff>();
                var staff = await staffRepo.GetAsync(s => s.AccountId == staffAccountId);
                if (staff == null)
                {
                    return CreateErrorResponse<CancelAndRebookResponseDTO>("Bạn không có quyền thực hiện thao tác này");
                }

                // 2. Get current appointment với VaccinationAppointmentDetails
                var appointmentRepo = _unitOfWork.GetRepository<VaccinationAppointment>();
                var currentAppointment = await appointmentRepo.GetAsync(
                    a => a.AppointmentId == request.CurrentAppointmentId,
                    includeProperties: "Schedule,Schedule.Slot,Schedule.Facility,Child,VaccinationAppointmentDetails,VaccinationAppointmentDetails.Vaccine");

                if (currentAppointment == null)
                {
                    return CreateErrorResponse<CancelAndRebookResponseDTO>("Không tìm thấy appointment cần hủy");
                }

                // 3. Validate current appointment status
                if (currentAppointment.Status != "Approval")
                {
                    return CreateErrorResponse<CancelAndRebookResponseDTO>(
                        $"Chỉ có thể cancel và rebook appointment có status 'Approval'. Status hiện tại: {currentAppointment.Status}");
                }

                // 4. Validate staff belongs to same facility
                if (currentAppointment.Schedule?.FacilityId != staff.FacilityId)
                {
                    return CreateErrorResponse<CancelAndRebookResponseDTO>("Bạn chỉ có thể thao tác với appointment thuộc cơ sở của mình");
                }

                // 5. Get ChildVaccineProfile
                var profileRepo = _unitOfWork.GetRepository<ChildVaccineProfile>();
                var childVaccineProfile = await profileRepo.GetAsync(
                    p => p.VaccineProfileId == request.ChildVaccineProfileId && p.AppointmentId == request.CurrentAppointmentId,
                    includeProperties: "Disease,Vaccine");

                if (childVaccineProfile == null)
                {
                    return CreateErrorResponse<CancelAndRebookResponseDTO>("Không tìm thấy ChildVaccineProfile tương ứng");
                }

                // 6. Get new schedule
                var scheduleRepo = _unitOfWork.GetRepository<AppointmentSchedule>();
                var newSchedule = await scheduleRepo.GetAsync(
                    s => s.ScheduleId == request.NewScheduleId,
                    includeProperties: "Slot,Facility");

                if (newSchedule == null)
                {
                    return CreateErrorResponse<CancelAndRebookResponseDTO>("Không tìm thấy schedule mới");
                }

                // 7. Validate new schedule belongs to same facility
                if (newSchedule.FacilityId != staff.FacilityId)
                {
                    return CreateErrorResponse<CancelAndRebookResponseDTO>("Schedule mới phải thuộc cùng cơ sở");
                }

                // 8. Validate new schedule is available
                if (newSchedule.Status != "Available")
                {
                    return CreateErrorResponse<CancelAndRebookResponseDTO>("Schedule mới không khả dụng");
                }

                // 9. Check capacity of new schedule
                var existingAppointmentsCount = await appointmentRepo.CountAsync(
                    a => a.ScheduleId == request.NewScheduleId && 
                         (a.Status == "Pending" || a.Status == "Approval" || a.Status == "Paid"));

                // Lấy thông tin slot để check capacity
                var slotRepo = _unitOfWork.GetRepository<ScheduleSlot>();
                var slot = await slotRepo.GetByIdAsync(newSchedule.SlotId);
                
                if (slot != null && existingAppointmentsCount >= slot.MaxCapacity)
                {
                    return CreateErrorResponse<CancelAndRebookResponseDTO>("Schedule mới đã đầy");
                }

                // 10. Cancel current appointment
                currentAppointment.Status = "Cancelled";
                currentAppointment.Note = "Cancelled by staff for rebooking";
                currentAppointment.UpdatedAt = DateTime.UtcNow;

                // 11. Create new appointment
                var newAppointment = new VaccinationAppointment
                {
                    ChildId = currentAppointment.ChildId,
                    ScheduleId = request.NewScheduleId,
                    OrderId = currentAppointment.OrderId,
                    Status = "Pending", // New appointment starts as Pending
                    Note = request.Note ?? $"Rebooked from cancelled appointment {request.CurrentAppointmentId}",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await appointmentRepo.AddAsync(newAppointment);
                await _unitOfWork.SaveChangesAsync();

                // 11.1. Tạo VaccinationAppointmentDetails cho appointment mới - ĐẢM BẢO LUÔN CÓ
                await CreateVaccinationAppointmentDetailForCancelRebookAsync(currentAppointment, newAppointment, childVaccineProfile, newSchedule, request.Note);

                // 12. Update ChildVaccineProfile với validation consistency
                childVaccineProfile.AppointmentId = newAppointment.AppointmentId;
                childVaccineProfile.Status = "Pending"; // ✅ Đặt thành "Pending" để nhất quán với rebook API
                childVaccineProfile.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                // ✅ FUTURE-PROOF: Validate vaccine-disease consistency (trong trường hợp tương lai có thay đổi vaccine)
                var vaccineRepo = _unitOfWork.GetRepository<Vaccine>();
                var currentVaccine = await vaccineRepo.GetAsync(v => v.VaccineId == childVaccineProfile.VaccineId, "VaccineDiseases");
                
                if (currentVaccine?.VaccineDiseases != null)
                {
                    var canTreatDisease = currentVaccine.VaccineDiseases.Any(vd => vd.DiseaseId == childVaccineProfile.DiseaseId);
                    if (!canTreatDisease)
                    {
                        _logger.LogWarning("⚠️ CONSISTENCY WARNING: Vaccine {VaccineId} không thể chữa Disease {DiseaseId} trong ChildVaccineProfile {ProfileId} sau rebook", 
                            childVaccineProfile.VaccineId, childVaccineProfile.DiseaseId, childVaccineProfile.VaccineProfileId);
                        
                        // Có thể thêm logic auto-fix ở đây trong tương lai nếu cần
                    }
                    else
                    {
                        _logger.LogInformation("✅ Vaccine-Disease consistency validated cho ChildVaccineProfile {ProfileId} sau rebook", 
                            childVaccineProfile.VaccineProfileId);
                    }
                }

                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                // 13. Create response
                var response = new CancelAndRebookResponseDTO
                {
                    ChildVaccineProfileId = childVaccineProfile.VaccineProfileId,
                    CancelledAppointmentId = currentAppointment.AppointmentId,
                    NewAppointmentId = newAppointment.AppointmentId,
                    Status = "Success",
                    CancelledAt = currentAppointment.UpdatedAt,
                    NewAppointmentDate = newSchedule.Date.ToDateTime(newSchedule.Slot.StartTime ?? TimeOnly.MinValue),
                    FacilityName = newSchedule.Facility.FacilityName,
                    Message = "Đã hủy appointment cũ và đặt lại lịch mới thành công"
                };

                _logger.LogInformation("Cancel và Rebook thành công: Cancelled {CancelledId}, Created {NewId}", 
                    currentAppointment.AppointmentId, newAppointment.AppointmentId);

                return CreateSuccessResponse(response, "Đã hủy và đặt lại lịch thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cancel và rebook appointment {CurrentAppointmentId}", request.CurrentAppointmentId);
                try { if (transaction != null) await transaction.RollbackAsync(); } catch { /* ignore */ }
                return CreateErrorResponse<CancelAndRebookResponseDTO>($"Có lỗi xảy ra: {ex.Message}");
            }
        }

        /// <summary>
        /// Tạo VaccinationAppointmentDetails cho TẤT CẢ các trường hợp booking (Order, Package, Vaccine lẻ)
        /// </summary>
        private async Task CreateVaccinationAppointmentDetailsAsync(VaccinationAppointment appointment, AppointmentBookingRequestDTO request, DateOnly vaccinationDate)
        {
            _logger.LogInformation("🚨 VÀO METHOD CreateVaccinationAppointmentDetailsAsync - AppointmentId: {AppointmentId}", appointment.AppointmentId);
            try
            {
                _logger.LogInformation("🎯 Tạo VaccinationAppointmentDetails cho appointment {AppointmentId}", appointment.AppointmentId);

                var detailRepo = _unitOfWork.GetRepository<VaccinationAppointmentDetail>();
                var vaccineInfoList = new List<(int VaccineId, string DoseNumber)>();

                // Lấy danh sách DiseaseIds từ request
                var diseaseIds = new List<int>();
                if (request.DiseaseId.HasValue)
                    diseaseIds.Add(request.DiseaseId.Value);
                if (request.DiseaseIds != null && request.DiseaseIds.Any())
                    diseaseIds.AddRange(request.DiseaseIds);

                _logger.LogInformation("🔍 DiseaseIds để xử lý: {DiseaseIds}", diseaseIds.Any() ? string.Join(",", diseaseIds) : "Không có");
                _logger.LogInformation("🔍 Request - OrderId: {OrderId}, PackageId: {PackageId}, FacilityVaccineIds: {FacilityVaccineIds}", 
                    request.OrderId, request.PackageId, 
                    request.FacilityVaccineIds != null ? string.Join(",", request.FacilityVaccineIds) : "null");

                // Nếu không có DiseaseIds, thử lấy từ FacilityVaccineIds để xác định
                if (!diseaseIds.Any() && request.FacilityVaccineIds != null && request.FacilityVaccineIds.Any())
                {
                    _logger.LogInformation("🔍 Không có DiseaseIds, sẽ xử lý FacilityVaccineIds trực tiếp");
                }

                // TRƯỜNG HỢP 1: Có Order - Lấy vaccine từ OrderDetails
                if (request.OrderId.HasValue && request.OrderId.Value > 0)
                {
                    _logger.LogInformation("📋 Tạo VaccinationAppointmentDetail từ Order {OrderId}", request.OrderId.Value);

                    var orderDetailRepo = _unitOfWork.GetRepository<OrderDetail>();
                    var orderDetails = await orderDetailRepo.FindAsync(
                        od => od.OrderId == request.OrderId.Value,
                        includeProperties: "FacilityVaccine,FacilityVaccine.Vaccine");

                    if (diseaseIds.Any())
                    {
                        // Có DiseaseIds - xử lý theo từng disease
                        foreach (var diseaseId in diseaseIds)
                        {
                            var matchedDetails = orderDetails
                                .Where(od => od.DiseaseId == diseaseId
                                          && od.FacilityVaccine != null
                                          && od.FacilityVaccine.FacilityId == request.FacilityId)
                                .ToList();

                            var selectedDetail = matchedDetails
                                .OrderByDescending(od => od.RemainingQuantity)
                                .ThenBy(od => od.OrderDetailId)
                                .FirstOrDefault();

                            if (selectedDetail?.FacilityVaccine?.Vaccine != null)
                            {
                                vaccineInfoList.Add((selectedDetail.FacilityVaccine.VaccineId, "1"));
                                _logger.LogInformation("🎯 ORDER: Thêm VaccineId {VaccineId} từ OrderDetail {OrderDetailId} cho Disease {DiseaseId}", 
                                    selectedDetail.FacilityVaccine.VaccineId, selectedDetail.OrderDetailId, diseaseId);
                            }
                            else
                            {
                                _logger.LogWarning("⚠️ ORDER: Không tìm thấy OrderDetail phù hợp cho Disease {DiseaseId}", diseaseId);
                            }
                        }
                    }
                    else
                    {
                        // Không có DiseaseIds - lấy tất cả OrderDetails của cơ sở này
                        var allDetails = orderDetails
                            .Where(od => od.FacilityVaccine != null
                                      && od.FacilityVaccine.FacilityId == request.FacilityId
                                      && od.RemainingQuantity > 0)
                            .ToList();

                        foreach (var detail in allDetails)
                        {
                            if (detail?.FacilityVaccine?.Vaccine != null)
                            {
                                vaccineInfoList.Add((detail.FacilityVaccine.VaccineId, "1"));
                                _logger.LogInformation("🎯 ORDER: Thêm VaccineId {VaccineId} từ OrderDetail {OrderDetailId} (không có DiseaseId filter)", 
                                    detail.FacilityVaccine.VaccineId, detail.OrderDetailId);
                            }
                        }
                    }
                }
                // TRƯỜNG HỢP 2: Có Package - Lấy vaccine từ PackageVaccines
                else if (request.PackageId.HasValue && request.PackageId.Value > 0)
                {
                    _logger.LogInformation("📦 Tạo VaccinationAppointmentDetail từ Package {PackageId}", request.PackageId.Value);

                    var packageVaccineRepo = _unitOfWork.GetRepository<PackageVaccine>();
                    var packageVaccines = await packageVaccineRepo.FindAsync(
                        pv => pv.PackageId == request.PackageId.Value,
                        includeProperties: "FacilityVaccine,FacilityVaccine.Vaccine");

                    if (diseaseIds.Any())
                    {
                        // Có DiseaseIds - xử lý theo từng disease
                        foreach (var diseaseId in diseaseIds)
                        {
                            var matchedPackageVaccines = packageVaccines
                                .Where(pv => pv.DiseaseId == diseaseId
                                          && pv.FacilityVaccine != null
                                          && pv.FacilityVaccine.FacilityId == request.FacilityId)
                                .ToList();

                            var selectedPackageVaccine = matchedPackageVaccines
                                .OrderBy(pv => pv.PackageVaccineId)
                                .FirstOrDefault() ?? packageVaccines
                                .Where(pv => pv.DiseaseId == diseaseId)
                                .OrderBy(pv => pv.PackageVaccineId)
                                .FirstOrDefault();

                            if (selectedPackageVaccine?.FacilityVaccine?.Vaccine != null)
                            {
                                vaccineInfoList.Add((selectedPackageVaccine.FacilityVaccine.VaccineId, "1"));
                                _logger.LogInformation("🎯 PACKAGE: Thêm VaccineId {VaccineId} từ Package {PackageId} cho Disease {DiseaseId}", 
                                    selectedPackageVaccine.FacilityVaccine.VaccineId, request.PackageId.Value, diseaseId);
                            }
                            else
                            {
                                _logger.LogWarning("⚠️ PACKAGE: Không tìm thấy PackageVaccine phù hợp cho Disease {DiseaseId}", diseaseId);
                            }
                        }
                    }
                    else
                    {
                        // Không có DiseaseIds - lấy tất cả PackageVaccines của cơ sở này
                        var allPackageVaccines = packageVaccines
                            .Where(pv => pv.FacilityVaccine != null
                                      && pv.FacilityVaccine.FacilityId == request.FacilityId)
                            .ToList();

                        foreach (var pv in allPackageVaccines)
                        {
                            if (pv?.FacilityVaccine?.Vaccine != null)
                            {
                                vaccineInfoList.Add((pv.FacilityVaccine.VaccineId, "1"));
                                _logger.LogInformation("🎯 PACKAGE: Thêm VaccineId {VaccineId} từ Package {PackageId} (không có DiseaseId filter)", 
                                    pv.FacilityVaccine.VaccineId, request.PackageId.Value);
                            }
                        }
                    }
                }
                // TRƯỜNG HỢP 3: Vaccine lẻ - Lấy từ FacilityVaccineIds
                else if (request.FacilityVaccineIds != null && request.FacilityVaccineIds.Any())
                {
                    _logger.LogInformation("💊 Tạo VaccinationAppointmentDetail từ {Count} vaccine lẻ", request.FacilityVaccineIds.Count);

                    var facilityVaccineRepo = _unitOfWork.GetRepository<FacilityVaccine>();

                    foreach (var facilityVaccineId in request.FacilityVaccineIds)
                    {
                        var facilityVaccine = await facilityVaccineRepo.GetAsync(
                            fv => fv.FacilityVaccineId == facilityVaccineId,
                            includeProperties: "Vaccine");

                        if (facilityVaccine?.Vaccine != null)
                        {
                            vaccineInfoList.Add((facilityVaccine.VaccineId, "1"));
                            _logger.LogInformation("🎯 INDIVIDUAL: Thêm VaccineId {VaccineId} từ FacilityVaccine {FacilityVaccineId}", 
                                facilityVaccine.VaccineId, facilityVaccineId);
                        }
                        else
                        {
                            _logger.LogWarning("❌ Không tìm thấy FacilityVaccine với ID {FacilityVaccineId}", facilityVaccineId);
                        }
                    }
                }

                // Tạo VaccinationAppointmentDetail cho tất cả vaccines đã xác định
                if (vaccineInfoList.Any())
                {
                    // Get facility ID for price lookup
                    var scheduleRepo = _unitOfWork.GetRepository<AppointmentSchedule>();
                    var scheduleForPrice = await scheduleRepo.GetAsync(s => s.ScheduleId == appointment.ScheduleId, "Facility");
                    var facilityId = scheduleForPrice?.FacilityId ?? 0;
                    
                    var facilityVaccineRepo = _unitOfWork.GetRepository<FacilityVaccine>();
                    
                    foreach (var (vaccineId, doseNumber) in vaccineInfoList)
                    {
                        // 🎯 Lấy giá hiện tại của FacilityVaccine để snapshot
                        decimal? facilityVaccinePrice = null;
                        try
                        {
                            var facilityVaccine = await facilityVaccineRepo.GetAsync(
                                fv => fv.FacilityId == facilityId && fv.VaccineId == vaccineId,
                                includeProperties: "Vaccine");
                            
                            if (facilityVaccine != null)
                            {
                                facilityVaccinePrice = facilityVaccine.Price;
                                _logger.LogInformation("💰 Snapshot giá cho VaccineId {VaccineId}: {Price}", 
                                    vaccineId, facilityVaccinePrice);
                            }
                            else
                            {
                                _logger.LogWarning("⚠️ Không tìm thấy FacilityVaccine cho VaccineId {VaccineId} tại Facility {FacilityId}", 
                                    vaccineId, facilityId);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "❌ Lỗi khi lấy giá FacilityVaccine cho VaccineId {VaccineId}", vaccineId);
                        }
                        
                        var detail = new VaccinationAppointmentDetail
                        {
                            AppointmentId = appointment.AppointmentId,
                            VaccineId = vaccineId,
                            VaccinationDate = vaccinationDate,
                            DoseNumber = doseNumber,
                            FacilityVaccinePrice = facilityVaccinePrice, // 🎯 LƯU GIÁ TẠI THỜI ĐIỂM BOOK
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        await detailRepo.AddAsync(detail);
                        _logger.LogInformation("✅ Tạo VaccinationAppointmentDetail cho VaccineId {VaccineId}, DoseNumber {DoseNumber}, Price Snapshot: {Price}", 
                            vaccineId, doseNumber, facilityVaccinePrice);
                    }

                    await _unitOfWork.SaveChangesAsync();
                    _logger.LogInformation("🎉 Đã tạo {Count} VaccinationAppointmentDetail cho appointment {AppointmentId}", 
                        vaccineInfoList.Count, appointment.AppointmentId);
                }
                else
                {
                    _logger.LogWarning("⚠️ Không có vaccine nào để tạo VaccinationAppointmentDetail cho appointment {AppointmentId}", 
                        appointment.AppointmentId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi khi tạo VaccinationAppointmentDetails cho appointment {AppointmentId}", 
                    appointment.AppointmentId);
                _logger.LogError("❌ Chi tiết exception: {Message}", ex.Message);
                _logger.LogError("❌ StackTrace: {StackTrace}", ex.StackTrace);
                throw;
            }
        }

        /// <summary>
        /// Tạo VaccinationAppointmentDetail cho Cancel and Rebook - ĐẢM BẢO LUÔN CÓ
        /// </summary>
        private async Task CreateVaccinationAppointmentDetailForCancelRebookAsync(
            VaccinationAppointment currentAppointment, 
            VaccinationAppointment newAppointment, 
            ChildVaccineProfile childVaccineProfile, 
            AppointmentSchedule newSchedule, 
            string? note)
        {
            try
            {
                _logger.LogInformation("🎯 Tạo VaccinationAppointmentDetail cho Cancel & Rebook - Old: {OldId}, New: {NewId}", 
                    currentAppointment.AppointmentId, newAppointment.AppointmentId);

                var appointmentDetailRepo = _unitOfWork.GetRepository<VaccinationAppointmentDetail>();

                // TRƯỜNG HỢP 1: Copy từ appointment cũ nếu có VaccinationAppointmentDetails
                if (currentAppointment.VaccinationAppointmentDetails != null && currentAppointment.VaccinationAppointmentDetails.Any())
                {
                    _logger.LogInformation("📋 Copy {Count} VaccinationAppointmentDetails từ appointment cũ", 
                        currentAppointment.VaccinationAppointmentDetails.Count);

                    foreach (var oldDetail in currentAppointment.VaccinationAppointmentDetails)
                    {
                        // 🎯 Lấy giá hiện tại của FacilityVaccine để snapshot cho appointment mới
                        decimal? facilityVaccinePrice = null;
                        try
                        {
                            var facilityVaccineRepo = _unitOfWork.GetRepository<FacilityVaccine>();
                            var facilityVaccine = await facilityVaccineRepo.GetAsync(
                                fv => fv.FacilityId == newSchedule.FacilityId && fv.VaccineId == oldDetail.VaccineId,
                                includeProperties: "Vaccine");
                            
                            if (facilityVaccine != null)
                            {
                                facilityVaccinePrice = facilityVaccine.Price;
                                _logger.LogInformation("💰 Cancel&Rebook - Snapshot giá cho VaccineId {VaccineId}: {Price}", 
                                    oldDetail.VaccineId, facilityVaccinePrice);
                            }
                            else
                            {
                                // Fallback: giữ giá cũ nếu có
                                facilityVaccinePrice = oldDetail.FacilityVaccinePrice;
                                _logger.LogWarning("⚠️ Cancel&Rebook - Không tìm thấy FacilityVaccine mới, dùng giá cũ: {OldPrice}", facilityVaccinePrice);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "❌ Cancel&Rebook - Lỗi khi lấy giá FacilityVaccine cho VaccineId {VaccineId}", oldDetail.VaccineId);
                            facilityVaccinePrice = oldDetail.FacilityVaccinePrice; // Fallback
                        }
                        
                        var newDetail = new VaccinationAppointmentDetail
                        {
                            AppointmentId = newAppointment.AppointmentId,
                            VaccineId = oldDetail.VaccineId,
                            VaccinationDate = newSchedule.Date, // Cập nhật ngày mới
                            DoseNumber = oldDetail.DoseNumber,
                            FacilityVaccinePrice = facilityVaccinePrice, // 🎯 LƯU GIÁ SNAPSHOT MỚI
                            Notes = note ?? oldDetail.Notes ?? "Cancel and rebook appointment detail",
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        await appointmentDetailRepo.AddAsync(newDetail);
                        
                        _logger.LogInformation("✅ Copy VaccinationAppointmentDetail - VaccineId {VaccineId}, DoseNumber {DoseNumber}, Price Snapshot: {Price}", 
                            oldDetail.VaccineId, oldDetail.DoseNumber, facilityVaccinePrice);
                    }
                }
                // TRƯỜNG HỢP 2: Appointment cũ không có VaccinationAppointmentDetails - Tạo từ ChildVaccineProfile
                else
                {
                    _logger.LogWarning("⚠️ Appointment cũ không có VaccinationAppointmentDetails, tạo từ ChildVaccineProfile");

                    // 🎯 Lấy giá hiện tại của FacilityVaccine để snapshot
                    decimal? facilityVaccinePrice = null;
                    try
                    {
                        var facilityVaccineRepo = _unitOfWork.GetRepository<FacilityVaccine>();
                        var facilityVaccine = await facilityVaccineRepo.GetAsync(
                            fv => fv.FacilityId == newSchedule.FacilityId && fv.VaccineId == childVaccineProfile.VaccineId,
                            includeProperties: "Vaccine");
                        
                        if (facilityVaccine != null)
                        {
                            facilityVaccinePrice = facilityVaccine.Price;
                            _logger.LogInformation("💰 Cancel&Rebook from Profile - Snapshot giá cho VaccineId {VaccineId}: {Price}", 
                                childVaccineProfile.VaccineId, facilityVaccinePrice);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Cancel&Rebook from Profile - Lỗi khi lấy giá FacilityVaccine cho VaccineId {VaccineId}", childVaccineProfile.VaccineId);
                    }

                    var newDetail = new VaccinationAppointmentDetail
                    {
                        AppointmentId = newAppointment.AppointmentId,
                        VaccineId = childVaccineProfile.VaccineId,
                        VaccinationDate = newSchedule.Date,
                        DoseNumber = childVaccineProfile.DoseNum.ToString(),
                        FacilityVaccinePrice = facilityVaccinePrice, // 🎯 LƯU GIÁ SNAPSHOT
                        Notes = note ?? "Cancel and rebook - created from ChildVaccineProfile",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await appointmentDetailRepo.AddAsync(newDetail);

                    _logger.LogInformation("✅ Tạo VaccinationAppointmentDetail từ ChildVaccineProfile - VaccineId {VaccineId}, DoseNumber {DoseNumber}, Price Snapshot: {Price}", 
                        childVaccineProfile.VaccineId, childVaccineProfile.DoseNum, facilityVaccinePrice);
                }

                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("🎉 Hoàn thành tạo VaccinationAppointmentDetail cho Cancel & Rebook");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi khi tạo VaccinationAppointmentDetail cho Cancel & Rebook - Old: {OldId}, New: {NewId}", 
                    currentAppointment.AppointmentId, newAppointment.AppointmentId);
                throw;
            }
        }

        /// <summary>
        /// Fallback method để tạo VaccinationAppointmentDetail khi method chính bị lỗi
        /// </summary>
        private async Task CreateFallbackVaccinationAppointmentDetailAsync(VaccinationAppointment appointment, AppointmentBookingRequestDTO request, DateOnly vaccinationDate)
        {
            try
            {
                _logger.LogInformation("🆘 FALLBACK: Tạo VaccinationAppointmentDetail cho appointment {AppointmentId}", appointment.AppointmentId);

                var detailRepo = _unitOfWork.GetRepository<VaccinationAppointmentDetail>();

                // Lấy danh sách DiseaseIds từ request
                var diseaseIds = new List<int>();
                if (request.DiseaseId.HasValue)
                    diseaseIds.Add(request.DiseaseId.Value);
                if (request.DiseaseIds != null && request.DiseaseIds.Any())
                    diseaseIds.AddRange(request.DiseaseIds);

                _logger.LogInformation("🆘 FALLBACK: DiseaseIds để tìm vaccine: {DiseaseIds}", 
                    diseaseIds.Any() ? string.Join(",", diseaseIds) : "Không có");

                var vaccineInfoList = new List<(int VaccineId, string DoseNumber)>();

                // Trường hợp 1: Có Order - lấy từ OrderDetails theo DiseaseId
                if (request.OrderId.HasValue && request.OrderId.Value > 0)
                {
                    _logger.LogInformation("🆘 FALLBACK: Lấy vaccine từ Order {OrderId}", request.OrderId.Value);
                    
                    var orderDetailRepo = _unitOfWork.GetRepository<OrderDetail>();
                    var orderDetails = await orderDetailRepo.FindAsync(
                        od => od.OrderId == request.OrderId.Value,
                        includeProperties: "FacilityVaccine");

                    if (diseaseIds.Any())
                    {
                        // Có DiseaseIds - tìm theo từng disease
                        foreach (var diseaseId in diseaseIds)
                        {
                            var matchedDetail = orderDetails
                                .FirstOrDefault(od => od.DiseaseId == diseaseId 
                                                   && od.FacilityVaccine != null
                                                   && od.FacilityVaccine.FacilityId == request.FacilityId);

                            if (matchedDetail?.FacilityVaccine != null)
                            {
                                vaccineInfoList.Add((matchedDetail.FacilityVaccine.VaccineId, "1"));
                                _logger.LogInformation("🆘 FALLBACK: Tìm thấy VaccineId {VaccineId} từ Order cho Disease {DiseaseId}", 
                                    matchedDetail.FacilityVaccine.VaccineId, diseaseId);
                            }
                        }
                    }
                    else
                    {
                        // Không có DiseaseIds - lấy tất cả OrderDetails của cơ sở
                        var allDetails = orderDetails
                            .Where(od => od.FacilityVaccine != null 
                                      && od.FacilityVaccine.FacilityId == request.FacilityId)
                            .ToList();

                        foreach (var detail in allDetails)
                        {
                            if (detail?.FacilityVaccine != null)
                            {
                                vaccineInfoList.Add((detail.FacilityVaccine.VaccineId, "1"));
                                _logger.LogInformation("🆘 FALLBACK: Tìm thấy VaccineId {VaccineId} từ Order (không filter Disease)", 
                                    detail.FacilityVaccine.VaccineId);
                            }
                        }
                    }
                }
                // Trường hợp 2: Có Package - lấy từ PackageVaccines theo DiseaseId
                else if (request.PackageId.HasValue && request.PackageId.Value > 0)
                {
                    _logger.LogInformation("🆘 FALLBACK: Lấy vaccine từ Package {PackageId}", request.PackageId.Value);
                    
                    var packageVaccineRepo = _unitOfWork.GetRepository<PackageVaccine>();
                    var packageVaccines = await packageVaccineRepo.FindAsync(
                        pv => pv.PackageId == request.PackageId.Value,
                        includeProperties: "FacilityVaccine");

                    if (diseaseIds.Any())
                    {
                        // Có DiseaseIds - tìm theo từng disease
                        foreach (var diseaseId in diseaseIds)
                        {
                            var matchedPackageVaccine = packageVaccines
                                .FirstOrDefault(pv => pv.DiseaseId == diseaseId
                                                   && pv.FacilityVaccine != null
                                                   && pv.FacilityVaccine.FacilityId == request.FacilityId);

                            if (matchedPackageVaccine?.FacilityVaccine != null)
                            {
                                vaccineInfoList.Add((matchedPackageVaccine.FacilityVaccine.VaccineId, "1"));
                                _logger.LogInformation("🆘 FALLBACK: Tìm thấy VaccineId {VaccineId} từ Package cho Disease {DiseaseId}", 
                                    matchedPackageVaccine.FacilityVaccine.VaccineId, diseaseId);
                            }
                        }
                    }
                    else
                    {
                        // Không có DiseaseIds - lấy tất cả PackageVaccines của cơ sở
                        var allPackageVaccines = packageVaccines
                            .Where(pv => pv.FacilityVaccine != null 
                                      && pv.FacilityVaccine.FacilityId == request.FacilityId)
                            .ToList();

                        foreach (var pv in allPackageVaccines)
                        {
                            if (pv?.FacilityVaccine != null)
                            {
                                vaccineInfoList.Add((pv.FacilityVaccine.VaccineId, "1"));
                                _logger.LogInformation("🆘 FALLBACK: Tìm thấy VaccineId {VaccineId} từ Package (không filter Disease)", 
                                    pv.FacilityVaccine.VaccineId);
                            }
                        }
                    }
                }
                // Trường hợp 3: Có FacilityVaccineIds - lấy từ FacilityVaccine
                else if (request.FacilityVaccineIds != null && request.FacilityVaccineIds.Any())
                {
                    _logger.LogInformation("🆘 FALLBACK: Lấy vaccine từ {Count} FacilityVaccineIds", request.FacilityVaccineIds.Count);
                    
                    var facilityVaccineRepo = _unitOfWork.GetRepository<FacilityVaccine>();

                    foreach (var facilityVaccineId in request.FacilityVaccineIds)
                    {
                        var facilityVaccine = await facilityVaccineRepo.GetByIdAsync(facilityVaccineId);
                        if (facilityVaccine != null)
                        {
                            vaccineInfoList.Add((facilityVaccine.VaccineId, "1"));
                            _logger.LogInformation("🆘 FALLBACK: Tìm thấy VaccineId {VaccineId} từ FacilityVaccine {FacilityVaccineId}", 
                                facilityVaccine.VaccineId, facilityVaccineId);
                        }
                    }
                }

                // Tạo VaccinationAppointmentDetail cho tất cả vaccines đã tìm được
                if (vaccineInfoList.Any())
                {
                    foreach (var (vaccineId, doseNumber) in vaccineInfoList)
                    {
                        var detail = new VaccinationAppointmentDetail
                        {
                            AppointmentId = appointment.AppointmentId,
                            VaccineId = vaccineId,
                            VaccinationDate = vaccinationDate,
                            DoseNumber = doseNumber,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        await detailRepo.AddAsync(detail);
                        _logger.LogInformation("✅ FALLBACK: Tạo VaccinationAppointmentDetail cho VaccineId {VaccineId}", vaccineId);
                    }

                    await _unitOfWork.SaveChangesAsync();
                    _logger.LogInformation("🎉 FALLBACK: Đã tạo {Count} VaccinationAppointmentDetail cho appointment {AppointmentId}", 
                        vaccineInfoList.Count, appointment.AppointmentId);
                }
                else
                {
                    _logger.LogWarning("⚠️ FALLBACK: Không tìm thấy vaccine nào phù hợp để tạo VaccinationAppointmentDetail");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ FALLBACK: Lỗi khi tạo VaccinationAppointmentDetail fallback cho appointment {AppointmentId}", appointment.AppointmentId);
            }
        }

        #endregion

        #region Helper Methods for Vaccine Update

        /// <summary>
        /// Lấy danh sách disease IDs được book trong appointment
        /// </summary>
        private async Task<List<int>> GetBookedDiseaseIdsAsync(int appointmentId)
        {
            var childVaccineProfileRepo = _unitOfWork.GetRepository<ChildVaccineProfile>();
            var profiles = await childVaccineProfileRepo.FindAsync(
                p => p.AppointmentId == appointmentId,
                includeProperties: "Disease"
            );

            return profiles.Select(p => p.DiseaseId).Distinct().ToList();
        }

        /// <summary>
        /// Lấy tên các diseases từ disease IDs
        /// </summary>
        private async Task<List<string>> GetDiseaseNamesAsync(List<int> diseaseIds)
        {
            if (!diseaseIds.Any()) return new List<string>();

            var diseaseRepo = _unitOfWork.GetRepository<Disease>();
            var diseases = await diseaseRepo.FindAsync(d => diseaseIds.Contains(d.DiseaseId));
            return diseases.Select(d => d.Name).ToList();
        }

        /// <summary>
        /// Xử lý cập nhật số lượng vaccine cũ (trả lại kho)
        /// </summary>
        private async Task HandleOldVaccineQuantityAsync(int oldVaccineId, int facilityId, string sourceType, int? orderDetailId)
        {
            if (sourceType == "Order" && orderDetailId.HasValue)
            {
                // Trả lại RemainingQuantity cho OrderDetail
                var orderDetailRepo = _unitOfWork.GetRepository<OrderDetail>();
                var orderDetail = await orderDetailRepo.GetAsync(od => od.OrderDetailId == orderDetailId.Value);
                if (orderDetail != null)
                {
                    orderDetail.RemainingQuantity += 1;
                    orderDetailRepo.Update(orderDetail);
                    _logger.LogInformation("Trả lại 1 vaccine cho OrderDetail {OrderDetailId}, RemainingQuantity = {Quantity}", 
                        orderDetailId, orderDetail.RemainingQuantity);
                }
            }
            else
            {
                // Trả lại AvailableQuantity cho FacilityVaccine
                var facilityVaccineRepo = _unitOfWork.GetRepository<FacilityVaccine>();
                var facilityVaccine = await facilityVaccineRepo.GetAsync(
                    fv => fv.VaccineId == oldVaccineId && fv.FacilityId == facilityId
                );
                if (facilityVaccine != null)
                {
                    facilityVaccine.AvailableQuantity += 1;
                    facilityVaccineRepo.Update(facilityVaccine);
                    _logger.LogInformation("Trả lại 1 vaccine cho FacilityVaccine {FacilityVaccineId}, AvailableQuantity = {Quantity}", 
                        facilityVaccine.FacilityVaccineId, facilityVaccine.AvailableQuantity);
                }
            }
        }

        /// <summary>
        /// Xử lý cập nhật số lượng vaccine mới (trừ khỏi kho)
        /// </summary>
        private async Task HandleNewVaccineQuantityAsync(int newVaccineId, int facilityId, string sourceType, int? orderDetailId)
        {
            if (sourceType == "Order" && orderDetailId.HasValue)
            {
                // Trừ RemainingQuantity từ OrderDetail
                var orderDetailRepo = _unitOfWork.GetRepository<OrderDetail>();
                var orderDetail = await orderDetailRepo.GetAsync(od => od.OrderDetailId == orderDetailId.Value);
                if (orderDetail != null && orderDetail.RemainingQuantity > 0)
                {
                    orderDetail.RemainingQuantity -= 1;
                    orderDetailRepo.Update(orderDetail);
                    _logger.LogInformation("Trừ 1 vaccine từ OrderDetail {OrderDetailId}, RemainingQuantity = {Quantity}", 
                        orderDetailId, orderDetail.RemainingQuantity);
                }
            }
            else
            {
                // Trừ AvailableQuantity từ FacilityVaccine
                var facilityVaccineRepo = _unitOfWork.GetRepository<FacilityVaccine>();
                var facilityVaccine = await facilityVaccineRepo.GetAsync(
                    fv => fv.VaccineId == newVaccineId && fv.FacilityId == facilityId
                );
                if (facilityVaccine != null && facilityVaccine.AvailableQuantity > 0)
                {
                    facilityVaccine.AvailableQuantity -= 1;
                    facilityVaccineRepo.Update(facilityVaccine);
                    _logger.LogInformation("Trừ 1 vaccine từ FacilityVaccine {FacilityVaccineId}, AvailableQuantity = {Quantity}", 
                        facilityVaccine.FacilityVaccineId, facilityVaccine.AvailableQuantity);
                }
            }
        }

        /// <summary>
        /// Trả lại số lượng vaccine khi cancel appointment
        /// </summary>
        private async Task RestoreVaccineQuantityOnCancelAsync(VaccinationAppointment appointment)
        {
            try
            {
                _logger.LogInformation("Bắt đầu trả lại số lượng vaccine cho appointment {AppointmentId}", appointment.AppointmentId);

                // Lấy danh sách VaccinationAppointmentDetails để biết vaccine nào cần trả lại
                var detailRepo = _unitOfWork.GetRepository<VaccinationAppointmentDetail>();
                var appointmentDetails = await detailRepo.FindAsync(
                    d => d.AppointmentId == appointment.AppointmentId,
                    includeProperties: "Vaccine");

                if (!appointmentDetails.Any())
                {
                    _logger.LogWarning("Không tìm thấy VaccinationAppointmentDetail cho appointment {AppointmentId}", appointment.AppointmentId);
                    return;
                }

                // Nếu appointment có OrderId - trả lại cho OrderDetail
                if (appointment.OrderId.HasValue)
                {
                    _logger.LogInformation("Trả lại số lượng cho Order {OrderId}", appointment.OrderId.Value);
                    
                    var orderDetailRepo = _unitOfWork.GetRepository<OrderDetail>();
                    var orderDetails = await orderDetailRepo.FindAsync(
                        od => od.OrderId == appointment.OrderId.Value,
                        includeProperties: "FacilityVaccine");

                    foreach (var appointmentDetail in appointmentDetails)
                    {
                        // Tìm OrderDetail tương ứng với vaccine này thông qua FacilityVaccine.VaccineId
                        var matchingOrderDetail = orderDetails.FirstOrDefault(od => 
                            od.FacilityVaccine != null && od.FacilityVaccine.VaccineId == appointmentDetail.VaccineId);

                        if (matchingOrderDetail != null)
                        {
                            matchingOrderDetail.RemainingQuantity += 1;
                            orderDetailRepo.Update(matchingOrderDetail);
                            _logger.LogInformation("Trả lại 1 vaccine cho OrderDetail {OrderDetailId} (VaccineId: {VaccineId}), RemainingQuantity = {Quantity}", 
                                matchingOrderDetail.OrderDetailId, appointmentDetail.VaccineId, matchingOrderDetail.RemainingQuantity);
                        }
                        else
                        {
                            _logger.LogWarning("Không tìm thấy OrderDetail tương ứng với VaccineId {VaccineId} trong Order {OrderId}", 
                                appointmentDetail.VaccineId, appointment.OrderId.Value);
                        }
                    }
                }
                else
                {
                    // Nếu không có OrderId - trả lại cho FacilityVaccine (vaccine lẻ)
                    _logger.LogInformation("Trả lại số lượng cho FacilityVaccine (vaccine lẻ)");
                    
                    var facilityVaccineRepo = _unitOfWork.GetRepository<FacilityVaccine>();
                    var facilityId = appointment.Schedule?.FacilityId;

                    if (!facilityId.HasValue)
                    {
                        _logger.LogError("Không thể xác định FacilityId cho appointment {AppointmentId}", appointment.AppointmentId);
                        return;
                    }

                    foreach (var appointmentDetail in appointmentDetails)
                    {
                        var facilityVaccine = await facilityVaccineRepo.GetAsync(
                            fv => fv.VaccineId == appointmentDetail.VaccineId && fv.FacilityId == facilityId.Value);

                        if (facilityVaccine != null)
                        {
                            facilityVaccine.AvailableQuantity += 1;
                            facilityVaccineRepo.Update(facilityVaccine);
                            _logger.LogInformation("Trả lại 1 vaccine cho FacilityVaccine {FacilityVaccineId} (VaccineId: {VaccineId}), AvailableQuantity = {Quantity}", 
                                facilityVaccine.FacilityVaccineId, appointmentDetail.VaccineId, facilityVaccine.AvailableQuantity);
                        }
                        else
                        {
                            _logger.LogWarning("Không tìm thấy FacilityVaccine tương ứng với VaccineId {VaccineId} tại Facility {FacilityId}", 
                                appointmentDetail.VaccineId, facilityId.Value);
                        }
                    }
                }

                _logger.LogInformation("Hoàn thành trả lại số lượng vaccine cho appointment {AppointmentId}", appointment.AppointmentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi trả lại số lượng vaccine cho appointment {AppointmentId}", appointment.AppointmentId);
                throw;
            }
        }

        /// <summary>
        /// Lấy danh sách orders của member tại facility cụ thể
        /// </summary>
        private async Task<List<Order>> GetMemberOrdersAtFacilityAsync(int memberId, int facilityId)
        {
            var orderRepo = _unitOfWork.GetRepository<Order>();
            var orders = await orderRepo.FindAsync(
                o => o.MemberId == memberId && 
                     o.Status == "Paid" &&
                     o.OrderDetails.Any(od => od.FacilityVaccine.FacilityId == facilityId),
                includeProperties: "OrderDetails,OrderDetails.FacilityVaccine,OrderDetails.FacilityVaccine.Vaccine,Package"
            );
            return orders.ToList();
        }

        /// <summary>
        /// Lấy thông tin nguồn vaccine (từ orders + individual stock)
        /// </summary>
        private async Task<List<VaccineSourceInfo>> GetVaccineSourcesAsync(int vaccineId, int facilityId, List<Order> memberOrders)
        {
            var sources = new List<VaccineSourceInfo>();

            // 1. Nguồn từ các orders đã trả tiền
            foreach (var order in memberOrders)
            {
                var orderDetails = order.OrderDetails?
                    .Where(od => od.FacilityVaccine?.VaccineId == vaccineId && 
                                od.FacilityVaccine.FacilityId == facilityId &&
                                od.RemainingQuantity > 0)
                    .ToList() ?? new List<OrderDetail>();

                foreach (var orderDetail in orderDetails)
                {
                    sources.Add(new VaccineSourceInfo
                    {
                        SourceType = "Order",
                        OrderId = order.OrderId,
                        OrderDetailId = orderDetail.OrderDetailId,
                        PackageName = order.Package?.Name ?? "Custom Order",
                        RemainingQuantity = orderDetail.RemainingQuantity,
                        IsPaid = order.Status == "Paid",
                        Price = 0, // Miễn phí vì đã trả tiền
                        IsPriority = true // Ưu tiên sử dụng nguồn đã trả tiền
                    });
                }
            }

            // 2. Nguồn từ facility stock (mua lẻ)
            var facilityVaccineRepo = _unitOfWork.GetRepository<FacilityVaccine>();
            var facilityVaccine = await facilityVaccineRepo.GetAsync(
                fv => fv.VaccineId == vaccineId && fv.FacilityId == facilityId && fv.Status == "active"
            );

            if (facilityVaccine != null && facilityVaccine.AvailableQuantity > 0)
            {
                sources.Add(new VaccineSourceInfo
                {
                    SourceType = "Individual",
                    RemainingQuantity = facilityVaccine.AvailableQuantity,
                    IsPaid = false,
                    Price = facilityVaccine.Price,
                    IsPriority = false
                });
            }

            return sources.OrderByDescending(s => s.IsPriority).ThenBy(s => s.Price).ToList();
        }

        /// <summary>
        /// Lấy tên package từ OrderDetailId
        /// </summary>
        private async Task<string?> GetPackageNameByOrderDetailIdAsync(int orderDetailId)
        {
            var orderDetailRepo = _unitOfWork.GetRepository<OrderDetail>();
            var orderDetail = await orderDetailRepo.GetAsync(
                od => od.OrderDetailId == orderDetailId,
                includeProperties: "Order,Order.Package"
            );

            return orderDetail?.Order?.Package?.Name;
        }

        /// <summary>
        /// Tạo ChildVaccineProfile cho TẤT CẢ bệnh mà vaccine có thể chữa (Multi-Disease Vaccine)
        /// </summary>
        private async Task CreateChildVaccineProfilesForMultiDiseaseVaccineAsync(
            IGenericRepository<ChildVaccineProfile> childVaccineProfileRepo,
            int childId,
            int vaccineId,
            int primaryDiseaseId, // Bệnh được chọn chính
            int appointmentId,
            DateOnly expectedDate,
            int totalDoses,
            ICollection<VaccineDisease>? vaccineDiseases)
        {
            try
            {
                if (vaccineDiseases == null || !vaccineDiseases.Any())
                {
                    _logger.LogWarning("Vaccine {VaccineId} không có VaccineDiseases. Fallback về single disease {DiseaseId}", 
                        vaccineId, primaryDiseaseId);
                    
                    // Fallback: Tạo CVP cho disease được chọn
                    await CreateChildVaccineProfileAsync(
                        childVaccineProfileRepo, childId, vaccineId, primaryDiseaseId, 
                        appointmentId, expectedDate, totalDoses);
                    return;
                }

                var diseaseIds = vaccineDiseases.Select(vd => vd.DiseaseId).ToList();
                _logger.LogInformation("🦠 MULTI-DISEASE VACCINE: VaccineId {VaccineId} có thể chữa {DiseaseCount} bệnh: [{DiseaseIds}]", 
                    vaccineId, diseaseIds.Count, string.Join(", ", diseaseIds));

                // Tạo ChildVaccineProfile cho TẤT CẢ bệnh mà vaccine có thể chữa
                foreach (var diseaseId in diseaseIds)
                {
                    try
                    {
                        _logger.LogInformation("🎯 Tạo ChildVaccineProfile cho Child {ChildId}, Vaccine {VaccineId}, Disease {DiseaseId}", 
                            childId, vaccineId, diseaseId);

                        await CreateChildVaccineProfileAsync(
                            childVaccineProfileRepo,
                            childId,
                            vaccineId,
                            diseaseId,
                            appointmentId,
                            expectedDate,
                            totalDoses);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi khi tạo ChildVaccineProfile cho Disease {DiseaseId} của Vaccine {VaccineId}", 
                            diseaseId, vaccineId);
                        // Tiếp tục tạo cho các disease khác, không throw exception
                    }
                }

                _logger.LogInformation("✅ Hoàn thành tạo ChildVaccineProfile cho {DiseaseCount} bệnh của Vaccine {VaccineId}", 
                    diseaseIds.Count, vaccineId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi trong CreateChildVaccineProfilesForMultiDiseaseVaccineAsync cho Vaccine {VaccineId}", vaccineId);
                throw;
            }
        }

        #endregion

        #region Cleanup Methods

        /// <summary>
        /// Tự động dọn dẹp các appointment đã quá hạn và xóa AppointmentId khỏi ChildVaccineProfile
        /// </summary>
        public async Task<AppointmentCleanupResultDTO> CleanupExpiredAppointmentsAsync()
        {
            var result = new AppointmentCleanupResultDTO
            {
                ProcessedAt = DateTime.UtcNow
            };

            try
            {
                _logger.LogInformation("Bắt đầu cleanup expired appointments");

                var appointmentRepo = _unitOfWork.GetRepository<VaccinationAppointment>();
                var childVaccineProfileRepo = _unitOfWork.GetRepository<ChildVaccineProfile>();

                // Lấy tất cả appointment có status "Pending" hoặc "Approval" và đã quá hạn
                var now = DateTime.Now;
                var expiredAppointments = await appointmentRepo.FindAsync(
                    a => (a.Status == "Pending" || a.Status == "Approval"),
                    includeProperties: "Schedule,Schedule.Slot"
                );

                var expiredList = new List<VaccinationAppointment>();
                var cancelledList = new List<VaccinationAppointment>();

                foreach (var appointment in expiredAppointments)
                {
                    if (appointment.Schedule?.Slot?.StartTime.HasValue == true)
                    {
                        var appointmentDateTime = appointment.Schedule.Date.ToDateTime(appointment.Schedule.Slot.StartTime.Value);
                        
                        // Appointment được coi là expired nếu đã qua thời gian hẹn 2 giờ
                        if (appointmentDateTime.AddHours(2) < now)
                        {
                            if (appointment.Status == "Pending")
                            {
                                // Appointment Pending quá 24h sẽ bị hủy
                                if (appointmentDateTime.AddHours(24) < now)
                                {
                                    cancelledList.Add(appointment);
                                }
                            }
                            else if (appointment.Status == "Approval")
                            {
                                // Appointment Approval quá 2h sẽ bị coi là expired
                                expiredList.Add(appointment);
                            }
                        }
                    }
                    else
                    {
                        // Nếu không có StartTime, chỉ kiểm tra theo ngày
                        var appointmentDate = appointment.Schedule?.Date;
                        if (appointmentDate.HasValue && appointmentDate.Value < DateOnly.FromDateTime(now.AddDays(-1)))
                        {
                            if (appointment.Status == "Pending")
                            {
                                cancelledList.Add(appointment);
                            }
                            else if (appointment.Status == "Approval")
                            {
                                expiredList.Add(appointment);
                            }
                        }
                    }
                }

                _logger.LogInformation("Tìm thấy {ExpiredCount} expired appointments và {CancelledCount} cancelled appointments", 
                    expiredList.Count, cancelledList.Count);

                // Xử lý expired appointments (chuyển status thành "Expired")
                foreach (var appointment in expiredList)
                {
                    await ProcessExpiredAppointmentAsync(appointment, childVaccineProfileRepo);
                    result.ProcessedAppointmentIds.Add(appointment.AppointmentId);
                }

                // Xử lý cancelled appointments (chuyển status thành "Cancelled")
                foreach (var appointment in cancelledList)
                {
                    await ProcessCancelledAppointmentAsync(appointment, childVaccineProfileRepo);
                    result.ProcessedAppointmentIds.Add(appointment.AppointmentId);
                }

                // Lưu thay đổi
                await _unitOfWork.SaveChangesAsync();

                // Cập nhật kết quả
                result.ExpiredAppointmentsCount = expiredList.Count;
                result.CancelledAppointmentsCount = cancelledList.Count;
                result.TotalProcessed = expiredList.Count + cancelledList.Count;
                result.Message = $"Đã xử lý {result.TotalProcessed} appointments: {result.ExpiredAppointmentsCount} expired, {result.CancelledAppointmentsCount} cancelled";

                _logger.LogInformation("Hoàn thành cleanup: {Message}", result.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi trong quá trình cleanup expired appointments");
                result.HasErrors = true;
                result.Errors.Add($"Lỗi cleanup: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Xử lý appointment đã expired (quá giờ hẹn)
        /// </summary>
        private async Task ProcessExpiredAppointmentAsync(VaccinationAppointment appointment, IGenericRepository<ChildVaccineProfile> childVaccineProfileRepo)
        {
            _logger.LogInformation("Xử lý expired appointment {AppointmentId}", appointment.AppointmentId);

            // Cập nhật status appointment thành "Expired"
            appointment.Status = "Expired";
            appointment.UpdatedAt = DateTime.UtcNow;
            
            var appointmentRepo = _unitOfWork.GetRepository<VaccinationAppointment>();
            appointmentRepo.Update(appointment);

            // Xóa AppointmentId khỏi ChildVaccineProfile và đặt lại status về "Pending"
            await RemoveAppointmentFromChildVaccineProfilesAsync(appointment.AppointmentId, childVaccineProfileRepo);

            // Trả lại số lượng vaccine nếu cần
            await RestoreVaccineQuantityOnExpiredAsync(appointment);
        }

        /// <summary>
        /// Xử lý appointment bị cancelled (quá lâu không được approve)
        /// </summary>
        private async Task ProcessCancelledAppointmentAsync(VaccinationAppointment appointment, IGenericRepository<ChildVaccineProfile> childVaccineProfileRepo)
        {
            _logger.LogInformation("Xử lý cancelled appointment {AppointmentId}", appointment.AppointmentId);

            // Cập nhật status appointment thành "Cancelled"
            appointment.Status = "Cancelled";
            appointment.UpdatedAt = DateTime.UtcNow;
            appointment.Note = "Tự động hủy do quá thời gian chờ xác nhận";
            
            var appointmentRepo = _unitOfWork.GetRepository<VaccinationAppointment>();
            appointmentRepo.Update(appointment);

            // Xóa AppointmentId khỏi ChildVaccineProfile và đặt lại status về "Pending"
            await RemoveAppointmentFromChildVaccineProfilesAsync(appointment.AppointmentId, childVaccineProfileRepo);

            // Trả lại số lượng vaccine
            await RestoreVaccineQuantityOnCancelAsync(appointment);
        }

        /// <summary>
        /// Xóa AppointmentId khỏi ChildVaccineProfile và đặt lại status về "Pending"
        /// </summary>
        private async Task RemoveAppointmentFromChildVaccineProfilesAsync(int appointmentId, IGenericRepository<ChildVaccineProfile> childVaccineProfileRepo)
        {
            var childVaccineProfiles = await childVaccineProfileRepo.FindAsync(p => p.AppointmentId == appointmentId);

            foreach (var profile in childVaccineProfiles)
            {
                _logger.LogInformation("Xóa AppointmentId {AppointmentId} khỏi ChildVaccineProfile {ProfileId}", 
                    appointmentId, profile.VaccineProfileId);

                // Xóa AppointmentId và đặt lại status về "Pending"
                profile.AppointmentId = null;
                profile.Status = "Pending";
                profile.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                
                childVaccineProfileRepo.Update(profile);
            }

            _logger.LogInformation("Đã xóa AppointmentId khỏi {Count} ChildVaccineProfiles", childVaccineProfiles.Count());
        }

        /// <summary>
        /// Trả lại số lượng vaccine khi appointment expired
        /// </summary>
        private async Task RestoreVaccineQuantityOnExpiredAsync(VaccinationAppointment appointment)
        {
            try
            {
                // Logic tương tự như RestoreVaccineQuantityOnCancelAsync nhưng với log khác
                _logger.LogInformation("Trả lại số lượng vaccine cho expired appointment {AppointmentId}", appointment.AppointmentId);
                await RestoreVaccineQuantityOnCancelAsync(appointment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi trả lại số lượng vaccine cho expired appointment {AppointmentId}", appointment.AppointmentId);
            }
        }

        #endregion
    }
}
