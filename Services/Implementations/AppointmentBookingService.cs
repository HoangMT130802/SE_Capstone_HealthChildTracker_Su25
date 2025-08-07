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

                // ✅ Validate disease selection - phải có ít nhất 1 bệnh
                var diseaseIds = new List<int>();
                if (request.DiseaseId.HasValue)
                {
                    diseaseIds.Add(request.DiseaseId.Value);
                }
                if (request.DiseaseIds != null && request.DiseaseIds.Any())
                {
                    diseaseIds.AddRange(request.DiseaseIds);
                }
                
                if (!diseaseIds.Any())
                {
                    validation.CanBook = false;
                    validation.Errors.Add(new ValidationErrorDTO
                    {
                        Code = "NO_DISEASE_SELECTED",
                        Message = "Phải chọn ít nhất 1 bệnh",
                        Field = "DiseaseId,DiseaseIds",
                        Severity = ValidationSeverity.Error
                    });
                    return validation;
                }

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
                        
                        // Kiểm tra vaccine có điều trị được disease không
                        var canTreatDisease = facilityVaccine.Vaccine?.VaccineDiseases?.Any(vd => diseaseIds.Contains(vd.DiseaseId)) ?? false;
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
                    var order = await orderRepo.GetAsync(o => o.OrderId == request.OrderId.Value, "Member");
                    
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
                   /* else if (order.Status != "Pending" || order.Status != "Paid")
                    {
                        validation.CanBook = false;
                        validation.Errors.Add(new ValidationErrorDTO
                        {
                            Code = "ORDER_NOT_PAID",
                            Message = "Order chưa được thanh toán",
                            Field = "OrderId",
                            Severity = ValidationSeverity.Error
                        });
                    }*/
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
                    }
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
                _logger.LogInformation("Tính chi phí cho cơ sở {FacilityId}", facilityId);

                decimal vaccineCost = 0;
                var items = new List<CostItemDTO>();

                if (orderId.HasValue)
                {
                    // Calculate cost from existing order
                    var orderRepo = _unitOfWork.GetRepository<Order>();
                    var order = await orderRepo.GetAsync(o => o.OrderId == orderId.Value, "Package");
                    if (order != null)
                    {
                        vaccineCost = order.TotalAmount;
                        items.Add(new CostItemDTO
                        {
                            Name = order.Package?.Name ?? "Order Package",
                            Type = "Existing Order",
                            Quantity = 1,
                            UnitPrice = order.TotalAmount,
                            TotalPrice = order.TotalAmount
                        });
                    }
                }
                else if (packageId.HasValue)
                {
                    // Calculate package cost
                    var packageRepo = _unitOfWork.GetRepository<VaccinePackage>();
                    var package = await packageRepo.GetAsync(p => p.PackageId == packageId.Value);
                    if (package != null)
                    {
                        vaccineCost = package.Price;
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
                    // Calculate individual vaccines cost
                    var facilityVaccineRepo = _unitOfWork.GetRepository<FacilityVaccine>();
                    foreach (var vaccineId in facilityVaccineIds)
                    {
                        var facilityVaccine = await facilityVaccineRepo.GetAsync(
                            fv => fv.FacilityVaccineId == vaccineId,
                            includeProperties: "Vaccine");
                        if (facilityVaccine != null)
                        {
                            vaccineCost += facilityVaccine.Price;
                            items.Add(new CostItemDTO
                            {
                                Name = facilityVaccine.Vaccine?.Name ?? "Unknown Vaccine",
                                Type = "Vaccine",
                                Quantity = 1,
                                UnitPrice = facilityVaccine.Price,
                                TotalPrice = facilityVaccine.Price
                            });
                        }
                    }
                }

                // Calculate fees (skip for existing orders)
                decimal serviceFee = 0;
                decimal bookingFee = 0; 
                decimal tax = 0;
                decimal totalCost = vaccineCost;

                if (!orderId.HasValue) // Chỉ tính phí cho Order mới hoặc vaccine lẻ
                {
                    serviceFee = vaccineCost * 0.1m; // 10% service fee
                    bookingFee = 50000; // 50k booking fee
                    tax = (vaccineCost + serviceFee) * 0.1m; // 10% VAT
                    totalCost = vaccineCost + serviceFee + bookingFee + tax;
                }

                return new CostBreakdownDTO
                {
                    VaccineCost = vaccineCost,
                    ServiceFee = serviceFee,
                    BookingFee = bookingFee,
                    Tax = tax,
                    Discount = 0,
                    TotalCost = totalCost,
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

        #region Booking Methods

        public async Task<AppointmentBookingResponseDTO> BookAppointmentAsync(AppointmentBookingRequestDTO request)
        {
            try
            {
                _logger.LogInformation("Đặt lịch cho trẻ {ChildId}, OrderId: {OrderId}, PackageId: {PackageId}, FacilityVaccineIds: {FacilityVaccineIds}", 
                    request.ChildId, request.OrderId, request.PackageId, request.FacilityVaccineIds != null ? string.Join(",", request.FacilityVaccineIds) : "null");

                // Validate first
                var validation = await ValidateBookingRequestAsync(request);
                if (!validation.CanBook)
                {
                    var errors = string.Join(", ", validation.Errors.Select(e => e.Message));
                    _logger.LogWarning("Validation failed: {Errors}", errors);
                    throw new InvalidOperationException("Validation failed: " + errors);
                }

                // Get child info (needed for both package and response)
                var childRepo = _unitOfWork.GetRepository<Child>();
                var child = await childRepo.GetAsync(c => c.ChildId == request.ChildId);
                if (child == null)
                {
                    throw new ArgumentException($"Không tìm thấy trẻ với ID {request.ChildId}");
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
                        throw new ArgumentException($"Không tìm thấy Order với ID {request.OrderId.Value}");
                    }
                    
                   /* if (existingOrder.Status != "Pending" || existingOrder.Status != "Paid")
                    {
                        throw new InvalidOperationException($"Order {request.OrderId.Value} chưa được thanh toán");
                    }*/
                    
                    // ✅ KHÔNG trừ RemainingQuantity ngay khi book - sẽ trừ khi tiêm thành công
                    // Chỉ validate xem có đủ vaccine không
                    var orderDetailRepo = _unitOfWork.GetRepository<OrderDetail>();
                    var orderDetails = await orderDetailRepo.FindAsync(od => od.OrderId == request.OrderId.Value);
                    
                    // Lấy diseaseIds từ request để xác định vaccine nào được chọn
                    var selectedDiseaseIds = new List<int>();
                    if (request.DiseaseId.HasValue)
                    {
                        selectedDiseaseIds.Add(request.DiseaseId.Value);
                    }
                    if (request.DiseaseIds != null && request.DiseaseIds.Any())
                    {
                        selectedDiseaseIds.AddRange(request.DiseaseIds);
                    }
                    
                    // Validate có đủ vaccine cho disease được chọn không
                    foreach (var orderDetail in orderDetails)
                    {
                        if (selectedDiseaseIds.Contains(orderDetail.DiseaseId))
                        {
                            if (orderDetail.RemainingQuantity <= 0)
                            {
                                throw new InvalidOperationException($"OrderDetail {orderDetail.OrderDetailId} cho DiseaseId {orderDetail.DiseaseId} đã hết vaccine (RemainingQuantity = 0)");
                            }
                            _logger.LogInformation("OrderDetail {OrderDetailId} cho DiseaseId {DiseaseId} có {RemainingQuantity} vaccine khả dụng", 
                                orderDetail.OrderDetailId, orderDetail.DiseaseId, orderDetail.RemainingQuantity);
                        }
                    }
                    
                    appointment.OrderId = request.OrderId.Value;
                    _logger.LogInformation("Sử dụng Order đã có {OrderId} thành công - sẽ trừ RemainingQuantity khi tiêm thành công", request.OrderId.Value);
                }
                // LUỒNG 2: Tạo Order mới nếu chọn gói vaccine
                else if (request.PackageId.HasValue && request.PackageId.Value > 0)
                {
                    _logger.LogInformation("Xử lý đặt lịch với gói vaccine mới {PackageId}", request.PackageId.Value);
                    
                    var memberRepo = _unitOfWork.GetRepository<Member>();
                    var member = await memberRepo.GetAsync(m => m.MemberId == child.MemberId);
                    if (member == null)
                    {
                        throw new ArgumentException($"Không tìm thấy member với ID {child.MemberId}");
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
                var scheduleRepo = _unitOfWork.GetRepository<AppointmentSchedule>();
                var schedule = await scheduleRepo.GetByIdAsync(request.ScheduleId);
                if (schedule == null)
                {
                    throw new ArgumentException($"Không tìm thấy schedule với ID {request.ScheduleId}");
                }

                // LUỒNG 4: Tạo VaccinationAppointmentDetails cho vaccine lẻ
                if (request.FacilityVaccineIds != null && request.FacilityVaccineIds.Any())
                {
                    _logger.LogInformation("Tạo VaccinationAppointmentDetails cho {Count} vaccine", request.FacilityVaccineIds.Count);
                    
                    var detailRepo = _unitOfWork.GetRepository<VaccinationAppointmentDetail>();
                    var facilityVaccineRepo = _unitOfWork.GetRepository<FacilityVaccine>();
                    
                    foreach (var facilityVaccineId in request.FacilityVaccineIds)
                    {
                        // Get the actual VaccineId from FacilityVaccine
                        var facilityVaccine = await facilityVaccineRepo.GetAsync(fv => fv.FacilityVaccineId == facilityVaccineId);
                        if (facilityVaccine != null)
                        {
                            var detail = new VaccinationAppointmentDetail
                            {
                                AppointmentId = appointment.AppointmentId,
                                VaccineId = facilityVaccine.VaccineId,
                                VaccinationDate = schedule.Date,
                                DoseNumber = "1", // TODO: Calculate proper dose number
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };
                            await detailRepo.AddAsync(detail);
                            _logger.LogInformation("Tạo VaccinationAppointmentDetail cho VaccineId {VaccineId}", facilityVaccine.VaccineId);
                        }
                        else
                        {
                            _logger.LogWarning("Không tìm thấy FacilityVaccine với ID {FacilityVaccineId}", facilityVaccineId);
                        }
                    }
                    await _unitOfWork.SaveChangesAsync();
                }

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
                    Facility = scheduleWithDetails?.Facility != null ? new Contracts.DTOs.VaccinationFacility.VaccinationFacilityDTO
                    {
                        FacilityId = scheduleWithDetails.Facility.FacilityId,
                        FacilityName = scheduleWithDetails.Facility.FacilityName,
                        Address = scheduleWithDetails.Facility.Address,
                        Phone = scheduleWithDetails.Facility.Phone
                    } : null,
                    
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
                        Facility = scheduleWithDetails.Facility != null ? new Contracts.DTOs.VaccinationFacility.VaccinationFacilityDTO
                        {
                            FacilityId = scheduleWithDetails.Facility.FacilityId,
                            FacilityName = scheduleWithDetails.Facility.FacilityName,
                            Address = scheduleWithDetails.Facility.Address,
                            Phone = scheduleWithDetails.Facility.Phone
                        } : null,
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
                    
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đặt lịch cho trẻ {ChildId}, PackageId: {PackageId}, FacilityVaccineIds: {FacilityVaccineIds}", 
                    request.ChildId, request.PackageId, request.FacilityVaccineIds != null ? string.Join(",", request.FacilityVaccineIds) : "null");
                throw;
            }
        }

        public async Task<AppointmentQuickBookingResponseDTO> QuickBookAppointmentAsync(AppointmentQuickBookingDTO request)
        {
            try
            {
                _logger.LogInformation("Đặt lịch nhanh cho trẻ {ChildId}", request.ChildId);

                // Find best facility and schedule for the disease
                var facilities = await SearchFacilitiesByDiseaseAsync(request.DiseaseId);
                
                if (!facilities.Facilities.Any())
                {
                    return new AppointmentQuickBookingResponseDTO 
                    { 
                        IsSuccess = false, 
                        FailureReason = "Không tìm thấy cơ sở có vaccine phù hợp",
                        Suggestions = new List<AppointmentSuggestionDTO>()
                    };
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
                    return new AppointmentQuickBookingResponseDTO 
                    { 
                        IsSuccess = false, 
                        FailureReason = "Không có lịch trống phù hợp",
                        Suggestions = await GenerateAppointmentSuggestionsAsync(request)
                    };
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

                return new AppointmentQuickBookingResponseDTO 
                { 
                    IsSuccess = true, 
                    Appointment = bookingResult,
                    Suggestions = new List<AppointmentSuggestionDTO>()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đặt lịch nhanh cho trẻ {ChildId}", request.ChildId);
                return new AppointmentQuickBookingResponseDTO 
                { 
                    IsSuccess = false, 
                    FailureReason = ex.Message,
                    Suggestions = new List<AppointmentSuggestionDTO>()
                };
            }
        }

        public async Task<bool> CancelAppointmentAsync(int appointmentId, string reason)
        {
            try
            {
                _logger.LogInformation("Xóa lịch hẹn {AppointmentId}", appointmentId);

                var appointmentRepo = _unitOfWork.GetRepository<VaccinationAppointment>();
                var appointment = await appointmentRepo.GetByIdAsync(appointmentId);
                
                if (appointment == null)
                {
                    throw new ArgumentException($"Không tìm thấy lịch hẹn với ID {appointmentId}");
                }

                // ✅ Lưu ScheduleId trước khi xóa appointment
                var scheduleId = appointment.ScheduleId;

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

                _logger.LogInformation("Xóa lịch hẹn {AppointmentId} thành công", appointmentId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa lịch hẹn {AppointmentId}", appointmentId);
                throw;
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

        public async Task<FacilityAppointmentResponseDTO> GetAllFacilityAppointmentsAsync(int facilityId)
        {
            try
            {
                _logger.LogInformation("Lấy tất cả lịch đặt cho facility {FacilityId}", facilityId);
                
                // Get appointments của facility
                var appointmentRepo = _unitOfWork.GetRepository<VaccinationAppointment>();
                var appointments = await appointmentRepo.FindAsync(
                    a => a.Schedule.FacilityId == facilityId,
                    "Schedule.Slot,Schedule.Facility,Child.Member.Account,Order.OrderDetails.FacilityVaccine.Vaccine,Order.OrderDetails.Disease,Order.Member");
                
                var appointmentDTOs = new List<FacilityAppointmentDTO>();
                foreach (var appointment in appointments)
                {
                    var dto = await MapToFacilityAppointmentDTO(appointment);
                    appointmentDTOs.Add(dto);
                }
                
                // Calculate statistics
                var stats = CalculateFacilityAppointmentStatistics(appointments);
                
                return new FacilityAppointmentResponseDTO
                {
                    Appointments = appointmentDTOs.OrderByDescending(a => a.AppointmentDate).ThenByDescending(a => a.AppointmentTime).ToList(),
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

        public async Task<FacilityAppointmentResponseDTO> GetFacilityAppointmentsByDateAsync(int facilityId, DateTime date)
        {
            try
            {
                _logger.LogInformation("Lấy lịch đặt theo ngày {Date} cho facility {FacilityId}", date.Date, facilityId);
                
                var dateOnly = DateOnly.FromDateTime(date.Date);
                var appointmentRepo = _unitOfWork.GetRepository<VaccinationAppointment>();
                var appointments = await appointmentRepo.FindAsync(
                    a => a.Schedule.FacilityId == facilityId && a.Schedule.Date == dateOnly,
                    "Schedule.Slot,Schedule.Facility,Child.Member.Account,Order.OrderDetails.FacilityVaccine.Vaccine,Order.OrderDetails.Disease,Order.Member");
                
                var appointmentDTOs = new List<FacilityAppointmentDTO>();
                foreach (var appointment in appointments)
                {
                    var dto = await MapToFacilityAppointmentDTO(appointment);
                    appointmentDTOs.Add(dto);
                }
                
                var stats = CalculateFacilityAppointmentStatistics(appointments);
                
                return new FacilityAppointmentResponseDTO
                {
                    Appointments = appointmentDTOs.OrderBy(a => a.AppointmentTime).ToList(),
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

        public async Task<FacilityAppointmentResponseDTO> GetFacilityAppointmentsByWeekAsync(int facilityId, DateTime startOfWeek)
        {
            try
            {
                _logger.LogInformation("Lấy lịch đặt theo tuần {Week} cho facility {FacilityId}", startOfWeek.Date, facilityId);
                
                var startDate = DateOnly.FromDateTime(startOfWeek.Date);
                var endDate = startDate.AddDays(6);
                
                var appointmentRepo = _unitOfWork.GetRepository<VaccinationAppointment>();
                var appointments = await appointmentRepo.FindAsync(
                    a => a.Schedule.FacilityId == facilityId && 
                         a.Schedule.Date >= startDate && 
                         a.Schedule.Date <= endDate,
                    "Schedule.Slot,Schedule.Facility,Child.Member.Account,Order.OrderDetails.FacilityVaccine.Vaccine,Order.OrderDetails.Disease,Order.Member");
                
                var appointmentDTOs = new List<FacilityAppointmentDTO>();
                foreach (var appointment in appointments)
                {
                    var dto = await MapToFacilityAppointmentDTO(appointment);
                    appointmentDTOs.Add(dto);
                }
                
                var stats = CalculateFacilityAppointmentStatistics(appointments);
                
                return new FacilityAppointmentResponseDTO
                {
                    Appointments = appointmentDTOs.OrderBy(a => a.AppointmentDate).ThenBy(a => a.AppointmentTime).ToList(),
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

        public async Task<FacilityAppointmentResponseDTO> GetFacilityAppointmentsByMonthAsync(int facilityId, DateTime month)
        {
            try
            {
                _logger.LogInformation("Lấy lịch đặt theo tháng {Month} cho facility {FacilityId}", month.ToString("yyyy-MM"), facilityId);
                
                var startDate = DateOnly.FromDateTime(new DateTime(month.Year, month.Month, 1));
                var endDate = startDate.AddMonths(1).AddDays(-1);
                
                var appointmentRepo = _unitOfWork.GetRepository<VaccinationAppointment>();
                var appointments = await appointmentRepo.FindAsync(
                    a => a.Schedule.FacilityId == facilityId && 
                         a.Schedule.Date >= startDate && 
                         a.Schedule.Date <= endDate,
                    "Schedule.Slot,Schedule.Facility,Child.Member.Account,Order.OrderDetails.FacilityVaccine.Vaccine,Order.OrderDetails.Disease,Order.Member");
                
                var appointmentDTOs = new List<FacilityAppointmentDTO>();
                foreach (var appointment in appointments)
                {
                    var dto = await MapToFacilityAppointmentDTO(appointment);
                    appointmentDTOs.Add(dto);
                }
                
                var stats = CalculateFacilityAppointmentStatistics(appointments);
                
                return new FacilityAppointmentResponseDTO
                {
                    Appointments = appointmentDTOs.OrderBy(a => a.AppointmentDate).ThenBy(a => a.AppointmentTime).ToList(),
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
                    "Schedule,Schedule.Slot,Schedule.Facility,Child,Child.Member,Child.Member.Account,Order,Order.OrderDetails,VaccinationAppointmentDetails,VaccinationAppointmentDetails.Vaccine");
                
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
                    appointment.Schedule = await scheduleRepo.GetAsync(s => s.ScheduleId == appointment.ScheduleId);
                    
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
                    
                    // ✅ Trừ RemainingQuantity từ OrderDetail khi chuyển sang Paid (nếu có OrderId)
                    if (appointment.OrderId.HasValue)
                    {
                        _logger.LogInformation("Trừ RemainingQuantity từ Order {OrderId} cho appointment {AppointmentId}", 
                            appointment.OrderId.Value, appointmentId);
                        
                        var orderDetailRepo = _unitOfWork.GetRepository<OrderDetail>();
                        var orderDetails = await orderDetailRepo.FindAsync(od => od.OrderId == appointment.OrderId.Value, "FacilityVaccine");
                        
                        // Lấy diseaseIds từ ChildVaccineProfile để biết vaccine nào được tiêm
                        var childVaccineProfileRepo2 = _unitOfWork.GetRepository<ChildVaccineProfile>();
                        var profilesForAppointment = await childVaccineProfileRepo2.FindAsync(
                            p => p.AppointmentId == appointmentId);
                        
                        foreach (var profile in profilesForAppointment)
                        {
                            bool hasSubtractedForProfile = false; // Flag để tránh trừ nhiều lần cho mỗi profile
                            
                            foreach (var orderDetail in orderDetails)
                            {
                                // ✅ Kiểm tra null trước khi truy cập FacilityVaccine
                                if (orderDetail.FacilityVaccine == null)
                                {
                                    _logger.LogWarning("OrderDetail {OrderDetailId} có FacilityVaccine null, bỏ qua", orderDetail.OrderDetailId);
                                    continue;
                                }
                                
                                // Chỉ trừ OrderDetail phù hợp với vaccine và disease đã tiêm
                                if (orderDetail.FacilityVaccine.VaccineId == profile.VaccineId && 
                                    orderDetail.DiseaseId == profile.DiseaseId && 
                                    orderDetail.RemainingQuantity > 0 && 
                                    !hasSubtractedForProfile) // Chỉ trừ 1 lần cho mỗi profile
                                {
                                    var oldRemainingQuantity = orderDetail.RemainingQuantity;
                                    orderDetail.RemainingQuantity -= 1; // Trừ 1 vaccine
                                    orderDetail.UpdatedAt = DateTime.UtcNow;
                                    orderDetailRepo.Update(orderDetail);
                                    
                                    hasSubtractedForProfile = true; // Đánh dấu đã trừ cho profile này
                                    
                                    _logger.LogInformation("Đã trừ 1 vaccine từ OrderDetail {OrderDetailId} cho VaccineId {VaccineId} DiseaseId {DiseaseId}. Từ {OldQuantity} xuống {NewQuantity}", 
                                        orderDetail.OrderDetailId, profile.VaccineId, profile.DiseaseId, oldRemainingQuantity, orderDetail.RemainingQuantity);
                                    break; // Chỉ trừ 1 lần cho mỗi vaccine
                                }
                            }
                            
                            if (!hasSubtractedForProfile)
                            {
                                _logger.LogWarning("Không tìm thấy OrderDetail phù hợp để trừ RemainingQuantity cho VaccineId {VaccineId} DiseaseId {DiseaseId}", 
                                    profile.VaccineId, profile.DiseaseId);
                            }
                        }
                    }
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
            
            // Calculate estimated cost - sử dụng VaccineId từ details
            var facilityVaccineIds = details.Any() ? 
                details.Select(d => d.VaccineId).ToList() : 
                null;
            
            var estimatedCost = await CalculateEstimatedCostAsync(
                appointment.Schedule.FacilityId, 
                appointment.Order?.OrderId, // Sử dụng OrderId từ Order
                appointment.Order?.PackageId > 0 ? appointment.Order.PackageId : null, // Sử dụng PackageId từ Order
                facilityVaccineIds);
            
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
                EstimatedCost = estimatedCost.TotalCost,
                
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
            
            // Get FacilityVaccines from Order.OrderDetails (for package/custom orders)
            var facilityVaccines = new List<FacilityVaccineDTO>();
            if (appointment.Order?.OrderDetails != null && appointment.Order.OrderDetails.Any())
            {
                foreach (var orderDetail in appointment.Order.OrderDetails)
                {
                    if (orderDetail.FacilityVaccine != null)
                    {
                        var facilityVaccineDto = _mapper.Map<FacilityVaccineDTO>(orderDetail.FacilityVaccine);
                        facilityVaccines.Add(facilityVaccineDto);
                    }
                }
            }
            // Fallback: Get FacilityVaccines from VaccinationAppointmentDetails (for individual vaccines)
            else
            {
                _logger.LogInformation("No Order found, trying to get FacilityVaccines from VaccinationAppointmentDetails for appointment {AppointmentId}", 
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
                        }
                    }
                }
            }
            
            // Calculate estimated cost
            var facilityVaccineIds = facilityVaccines.Select(fv => fv.FacilityVaccineId).ToList();
            var estimatedCost = await CalculateEstimatedCostAsync(
                appointment.Schedule.FacilityId, 
                appointment.Order?.OrderId,
                appointment.Order?.PackageId > 0 ? appointment.Order.PackageId : null,
                facilityVaccineIds.Any() ? facilityVaccineIds : null);
            
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
                EstimatedCost = estimatedCost.TotalCost,
                
                // Status flags
                IsUpcoming = slotDateTime > now,
                IsPast = slotDateTime < now,
                CanApprove = appointment.Status == "Pending" && slotDateTime > now,
                CanReject = appointment.Status == "Pending" && slotDateTime > now,
                CanComplete = appointment.Status == "Approval" && slotDateTime <= now
            };
            
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

        public async Task<AppointmentRebookingValidationDTO> ValidateRebookingRequestAsync(int childVaccineProfileId, int accountId)
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
                    return new AppointmentRebookingValidationDTO
                    {
                        CanRebook = false,
                        ReasonCannotRebook = "Không tìm thấy lịch tiêm vaccine",
                        HasApplicableOrder = false,
                        RequiresPayment = true,
                        EstimatedCost = 0
                    };
                }

                // 2. Validate ownership - profile phải thuộc về member của account hiện tại
                var memberRepo = _unitOfWork.GetRepository<Member>();
                var member = await memberRepo.GetAsync(m => m.AccountId == accountId);
                if (member == null || profile.Child.MemberId != member.MemberId)
                {
                    return new AppointmentRebookingValidationDTO
                    {
                        CanRebook = false,
                        ReasonCannotRebook = "Bạn không có quyền đặt lịch cho trẻ này",
                        HasApplicableOrder = false,
                        RequiresPayment = true,
                        EstimatedCost = 0
                    };
                }

                // 3. Validate trạng thái profile - phải chưa được đặt lịch (AppointmentId = null)
                if (profile.AppointmentId.HasValue)
                {
                    return new AppointmentRebookingValidationDTO
                    {
                        CanRebook = false,
                        ReasonCannotRebook = "Lịch tiêm này đã được đặt rồi",
                        HasApplicableOrder = false,
                        RequiresPayment = true,
                        EstimatedCost = 0
                    };
                }

                // 4. Validate ExpectedDate - phải có ngày dự kiến
                if (!profile.ExpectedDate.HasValue)
                {
                    return new AppointmentRebookingValidationDTO
                    {
                        CanRebook = false,
                        ReasonCannotRebook = "Chưa có ngày dự kiến tiêm vaccine này",
                        HasApplicableOrder = false,
                        RequiresPayment = true,
                        EstimatedCost = 0
                    };
                }

                // 5. Validate DoseNum - phải nhỏ hơn hoặc bằng số liều tối đa của vaccine
                if (profile.DoseNum > profile.Vaccine.NumberOfDoses)
                {
                    return new AppointmentRebookingValidationDTO
                    {
                        CanRebook = false,
                        ReasonCannotRebook = $"Số mũi {profile.DoseNum} vượt quá số liều tối đa {profile.Vaccine.NumberOfDoses} của vaccine {profile.Vaccine.Name}",
                        HasApplicableOrder = false,
                        RequiresPayment = true,
                        EstimatedCost = 0
                    };
                }

                // 6. Validate ActualDate - nếu đã có ActualDate thì không cho rebook (đã tiêm xong)
                if (profile.ActualDate.HasValue)
                {
                    return new AppointmentRebookingValidationDTO
                    {
                        CanRebook = false,
                        ReasonCannotRebook = $"Mũi {profile.DoseNum} của vaccine {profile.Vaccine.Name} đã được tiêm vào ngày {profile.ActualDate.Value:dd/MM/yyyy}",
                        HasApplicableOrder = false,
                        RequiresPayment = true,
                        EstimatedCost = 0
                    };
                }

                // Tiếp tục logic validation
                return await ValidateOrderAndCostAsync(profile, accountId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi validate rebooking request cho ChildVaccineProfile {ProfileId}", childVaccineProfileId);
                throw;
            }
        }

        #endregion
    }
} 
