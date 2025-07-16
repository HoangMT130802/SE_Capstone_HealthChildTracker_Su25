using AutoMapper;
using Contracts.DTOs.Appointment;
using Contracts.DTOs.FacilitySchedule;
using Microsoft.Extensions.Logging;
using Repositories.Entities;
using Repositories.Interfaces;
using Services.Interfaces;

namespace Services.Implementations
{
    public class AppointmentScheduleService : IAppointmentScheduleService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<AppointmentScheduleService> _logger;
        private readonly IScheduleSlotService _scheduleSlotService;

        public AppointmentScheduleService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<AppointmentScheduleService> logger, IScheduleSlotService scheduleSlotService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _scheduleSlotService = scheduleSlotService;
        }

        public async Task<List<AppointmentScheduleDTO>> GetAllSchedulesAsync()
        {
            try
            {
                var repository = _unitOfWork.GetRepository<AppointmentSchedule>();
                var schedules = await repository.GetAllAsync("");

                var result = schedules.OrderBy(s => s.Date).ThenBy(s => s.SlotId).ToList();
                return _mapper.Map<List<AppointmentScheduleDTO>>(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy tất cả lịch hẹn");
                throw;
            }
        }

        public async Task<List<AppointmentScheduleDTO>> GetSchedulesByWeekAsync(DateTime startOfWeek)
        {
            try
            {
                var repository = _unitOfWork.GetRepository<AppointmentSchedule>();
                var schedules = await repository.GetAllAsync("");

                var endOfWeek = startOfWeek.AddDays(6);
                var startDateOnly = DateOnly.FromDateTime(startOfWeek);
                var endDateOnly = DateOnly.FromDateTime(endOfWeek);

                var result = schedules
                    .Where(s => s.Date >= startDateOnly && s.Date <= endDateOnly)
                    .OrderBy(s => s.Date)
                    .ThenBy(s => s.SlotId)
                    .ToList();

                return _mapper.Map<List<AppointmentScheduleDTO>>(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy lịch hẹn theo tuần");
                throw;
            }
        }

        public async Task<List<AppointmentScheduleDTO>> GetSchedulesByMonthAsync(DateTime month)
        {
            try
            {
                var repository = _unitOfWork.GetRepository<AppointmentSchedule>();
                var schedules = await repository.GetAllAsync("");

                var firstDayOfMonth = new DateTime(month.Year, month.Month, 1);
                var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
                var startDateOnly = DateOnly.FromDateTime(firstDayOfMonth);
                var endDateOnly = DateOnly.FromDateTime(lastDayOfMonth);

                var result = schedules
                    .Where(s => s.Date >= startDateOnly && s.Date <= endDateOnly)
                    .OrderBy(s => s.Date)
                    .ThenBy(s => s.SlotId)
                    .ToList();

                return _mapper.Map<List<AppointmentScheduleDTO>>(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy lịch hẹn theo tháng");
                throw;
            }
        }

        public async Task<List<AppointmentScheduleDTO>> GetSchedulesByDateAsync(DateTime date)
        {
            try
            {
                var repository = _unitOfWork.GetRepository<AppointmentSchedule>();
                var schedules = await repository.GetAllAsync("");

                var dateOnly = DateOnly.FromDateTime(date);
                var result = schedules
                    .Where(s => s.Date == dateOnly)
                    .OrderBy(s => s.SlotId)
                    .ToList();

                return _mapper.Map<List<AppointmentScheduleDTO>>(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy lịch hẹn theo ngày");
                throw;
            }
        }

        public async Task<List<AppointmentScheduleDTO>> CreateScheduleAsync(CreateAppointmentScheduleDTO createDto)
        {
            try
            {
                // Validate input
                if (!createDto.IsValid())
                {
                    throw new ArgumentException("Phải có SlotId hoặc WorkingHoursGroupId");
                }

                var repository = _unitOfWork.GetRepository<AppointmentSchedule>();
                var createdSchedules = new List<AppointmentSchedule>();

                // Case 1: Single slot assignment
                if (createDto.SlotId.HasValue)
                {
                    _logger.LogInformation("Tạo lịch hẹn cho slot {SlotId}", createDto.SlotId.Value);

                    // Kiểm tra xem schedule đã tồn tại chưa
                    var existingSchedules = await repository.GetAllAsync("");
                    var exists = existingSchedules.Any(s => s.Date == createDto.Date && s.SlotId == createDto.SlotId.Value);

                    if (exists)
                    {
                        throw new InvalidOperationException($"Lịch hẹn cho slot {createDto.SlotId.Value} vào ngày {createDto.Date:yyyy-MM-dd} đã tồn tại");
                    }

                    var schedule = new AppointmentSchedule
                    {
                        FacilityId = createDto.FacilityId,
                        SlotId = createDto.SlotId.Value,
                        Date = createDto.Date,
                        BookedCount = createDto.BookedCount ?? 0,
                        Status = createDto.Status,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    createdSchedules.Add(schedule);
                }
                // Case 2: Working hours group assignment
                else if (!string.IsNullOrEmpty(createDto.WorkingHoursGroupId))
                {
                    _logger.LogInformation("Tạo lịch hẹn cho working hours group {GroupId}", createDto.WorkingHoursGroupId);

                    // Lấy tất cả slots trong working hours group
                    var groupSlots = await _scheduleSlotService.GetSlotsByWorkingHoursGroupIdAsync(createDto.WorkingHoursGroupId);
                    
                    if (!groupSlots.Any())
                    {
                        throw new ArgumentException($"Không tìm thấy slots trong working hours group {createDto.WorkingHoursGroupId}");
                    }

                    // Kiểm tra facility có đúng không
                    var firstSlot = groupSlots.First();
                    if (firstSlot.FacilityId != createDto.FacilityId)
                    {
                        throw new ArgumentException("Working hours group không thuộc về facility được chỉ định");
                    }

                    var existingSchedules = await repository.GetAllAsync("");
                    
                    foreach (var slot in groupSlots)
                    {
                        // Kiểm tra xem schedule đã tồn tại chưa
                        var exists = existingSchedules.Any(s => s.Date == createDto.Date && s.SlotId == slot.SlotId);
                        
                        if (!exists)
                        {
                            var schedule = new AppointmentSchedule
                            {
                                FacilityId = createDto.FacilityId,
                                SlotId = slot.SlotId,
                                Date = createDto.Date,
                                BookedCount = createDto.BookedCount ?? 0,
                                Status = createDto.Status,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };

                            createdSchedules.Add(schedule);
                        }
                    }
                }

                // Lưu tất cả schedules
                if (createdSchedules.Any())
                {
                    await repository.AddRangeAsync(createdSchedules);
                    await _unitOfWork.SaveChangesAsync();
                }

                _logger.LogInformation("Tạo {Count} lịch hẹn thành công", createdSchedules.Count);
                return _mapper.Map<List<AppointmentScheduleDTO>>(createdSchedules);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo lịch hẹn");
                throw;
            }
        }

        public async Task<AppointmentScheduleDTO> UpdateScheduleAsync(int scheduleId, UpdateAppointmentScheduleDTO updateDto)
        {
            try
            {
                _logger.LogInformation("Cập nhật lịch hẹn ID: {ScheduleId}", scheduleId);

                var repository = _unitOfWork.GetRepository<AppointmentSchedule>();
                var schedule = await repository.GetAsync(s => s.ScheduleId == scheduleId);

                if (schedule == null)
                {
                    throw new ArgumentException($"Lịch hẹn với ID {scheduleId} không tồn tại");
                }

                _mapper.Map(updateDto, schedule);
                repository.Update(schedule);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Cập nhật lịch hẹn thành công");
                return _mapper.Map<AppointmentScheduleDTO>(schedule);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật lịch hẹn ID: {ScheduleId}", scheduleId);
                throw;
            }
        }

        public async Task<bool> DeleteScheduleAsync(int scheduleId)
        {
            try
            {
                _logger.LogInformation("Xóa lịch hẹn ID: {ScheduleId}", scheduleId);

                var repository = _unitOfWork.GetRepository<AppointmentSchedule>();
                var schedule = await repository.GetAsync(s => s.ScheduleId == scheduleId);

                if (schedule == null)
                {
                    throw new ArgumentException($"Lịch hẹn với ID {scheduleId} không tồn tại");
                }

                repository.Delete(schedule);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Xóa lịch hẹn thành công");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa lịch hẹn ID: {ScheduleId}", scheduleId);
                throw;
            }
        }

        public async Task<bool> DeleteSchedulesByDateAsync(DateTime date)
        {
            try
            {
                _logger.LogInformation("Xóa tất cả lịch hẹn của ngày: {Date}", date.ToString("yyyy-MM-dd"));

                var repository = _unitOfWork.GetRepository<AppointmentSchedule>();
                var schedules = await repository.GetAllAsync("");

                var dateOnly = DateOnly.FromDateTime(date);
                var schedulesToDelete = schedules.Where(s => s.Date == dateOnly).ToList();

                if (schedulesToDelete.Any())
                {
                    foreach (var schedule in schedulesToDelete)
                    {
                        repository.Delete(schedule);
                    }
                    await _unitOfWork.SaveChangesAsync();
                }

                _logger.LogInformation("Xóa lịch hẹn theo ngày thành công");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa lịch hẹn theo ngày: {Date}", date.ToString("yyyy-MM-dd"));
                throw;
            }
        }

        public async Task<bool> UpdateDayStatusAsync(DateTime date, string status)
        {
            try
            {
                _logger.LogInformation("Cập nhật trạng thái ngày {Date} thành {Status}", date.ToString("yyyy-MM-dd"), status);

                var repository = _unitOfWork.GetRepository<AppointmentSchedule>();
                var schedules = await repository.GetAllAsync("");

                var dateOnly = DateOnly.FromDateTime(date);
                var schedulesToUpdate = schedules.Where(s => s.Date == dateOnly).ToList();

                foreach (var schedule in schedulesToUpdate)
                {
                    schedule.Status = status;
                    schedule.UpdatedAt = DateTime.UtcNow;
                }

                if (schedulesToUpdate.Any())
                {
                    repository.UpdateRange(schedulesToUpdate);
                    await _unitOfWork.SaveChangesAsync();
                }

                _logger.LogInformation("Cập nhật trạng thái ngày thành công");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật trạng thái ngày: {Date}", date.ToString("yyyy-MM-dd"));
                throw;
            }
        }

        public async Task<List<AppointmentScheduleDTO>> AddSlotsToScheduleAsync(DateTime date, List<int> slotIds)
        {
            try
            {
                _logger.LogInformation("Thêm slots vào lịch ngày {Date}", date.ToString("yyyy-MM-dd"));

                var repository = _unitOfWork.GetRepository<AppointmentSchedule>();
                var createdSchedules = new List<AppointmentSchedule>();

                var dateOnly = DateOnly.FromDateTime(date);

                foreach (var slotId in slotIds)
                {
                    // Kiểm tra xem schedule đã tồn tại chưa
                    var existingSchedules = await repository.GetAllAsync("");
                    var exists = existingSchedules.Any(s => s.Date == dateOnly && s.SlotId == slotId);

                    if (!exists)
                    {
                        var schedule = new AppointmentSchedule
                        {
                            FacilityId = 1, // Mặc định facility ID, có thể thay đổi sau
                            SlotId = slotId,
                            Date = dateOnly,
                            Status = "Available",
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        createdSchedules.Add(schedule);
                    }
                }

                if (createdSchedules.Any())
                {
                    await repository.AddRangeAsync(createdSchedules);
                    await _unitOfWork.SaveChangesAsync();
                }

                _logger.LogInformation("Thêm slots vào lịch thành công");
                return _mapper.Map<List<AppointmentScheduleDTO>>(createdSchedules);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thêm slots vào lịch ngày: {Date}", date.ToString("yyyy-MM-dd"));
                throw;
            }
        }

        public async Task<List<AppointmentScheduleDTO>> GetDayScheduleWithSlotsAsync(DateTime date)
        {
            try
            {
                var repository = _unitOfWork.GetRepository<AppointmentSchedule>();
                var schedules = await repository.GetAllAsync("");

                var dateOnly = DateOnly.FromDateTime(date);
                var result = schedules
                    .Where(s => s.Date == dateOnly)
                    .OrderBy(s => s.SlotId)
                    .ToList();

                return _mapper.Map<List<AppointmentScheduleDTO>>(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy lịch hẹn với slots trong ngày: {Date}", date.ToString("yyyy-MM-dd"));
                throw;
            }
        }

        public async Task<BulkAssignWorkingHoursResponseDTO> BulkAssignWorkingHoursAsync(BulkAssignWorkingHoursDTO bulkAssignDto)
        {
            try
            {
                _logger.LogInformation("Bắt đầu bulk assign working hours group {GroupId} cho ngày {Date}", 
                    bulkAssignDto.WorkingHoursGroupId, bulkAssignDto.Date.ToString("yyyy-MM-dd"));

                // Lấy tất cả slots trong working hours group
                var groupSlots = await _scheduleSlotService.GetSlotsByWorkingHoursGroupIdAsync(bulkAssignDto.WorkingHoursGroupId);
                
                if (!groupSlots.Any())
                {
                    return new BulkAssignWorkingHoursResponseDTO
                    {
                        IsSuccess = false,
                        Message = $"Không tìm thấy slots trong working hours group {bulkAssignDto.WorkingHoursGroupId}",
                        TotalSlotsAssigned = 0,
                        ExistingSlotsSkipped = 0
                    };
                }

                // Kiểm tra facility có đúng không
                var firstSlot = groupSlots.First();
                if (firstSlot.FacilityId != bulkAssignDto.FacilityId)
                {
                    return new BulkAssignWorkingHoursResponseDTO
                    {
                        IsSuccess = false,
                        Message = "Working hours group không thuộc về facility được chỉ định",
                        TotalSlotsAssigned = 0,
                        ExistingSlotsSkipped = 0
                    };
                }

                var repository = _unitOfWork.GetRepository<AppointmentSchedule>();
                var existingSchedules = await repository.GetAllAsync("");
                
                var createdSchedules = new List<AppointmentSchedule>();
                int existingCount = 0;

                foreach (var slot in groupSlots)
                {
                    // Kiểm tra xem schedule đã tồn tại chưa
                    var exists = existingSchedules.Any(s => s.Date == bulkAssignDto.Date && s.SlotId == slot.SlotId);
                    
                    if (!exists)
                    {
                        var schedule = new AppointmentSchedule
                        {
                            FacilityId = bulkAssignDto.FacilityId,
                            SlotId = slot.SlotId,
                            Date = bulkAssignDto.Date,
                            BookedCount = 0,
                            Status = bulkAssignDto.Status,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        createdSchedules.Add(schedule);
                    }
                    else
                    {
                        existingCount++;
                    }
                }

                // Lưu các schedule mới
                if (createdSchedules.Any())
                {
                    await repository.AddRangeAsync(createdSchedules);
                    await _unitOfWork.SaveChangesAsync();
                }

                // Lấy working hours group info
                var workingHoursGroups = await _scheduleSlotService.GetWorkingHoursGroupsByFacilityAsync(bulkAssignDto.FacilityId);
                var groupInfo = workingHoursGroups.FirstOrDefault(g => g.GroupId == bulkAssignDto.WorkingHoursGroupId);

                var response = new BulkAssignWorkingHoursResponseDTO
                {
                    IsSuccess = true,
                    Message = $"Đã assign {createdSchedules.Count} slots thành công cho ngày {bulkAssignDto.Date:yyyy-MM-dd}",
                    TotalSlotsAssigned = createdSchedules.Count,
                    ExistingSlotsSkipped = existingCount,
                    CreatedSchedules = _mapper.Map<List<AppointmentScheduleDTO>>(createdSchedules),
                    WorkingHoursGroup = new WorkingHoursGroupInfoDTO
                    {
                        WorkingHoursGroupId = bulkAssignDto.WorkingHoursGroupId,
                        Description = groupInfo?.Description ?? "Working Hours Group",
                        TotalSlots = groupSlots.Count,
                        TimeRange = groupInfo != null ? $"{groupInfo.StartTime:HH:mm} - {groupInfo.EndTime:HH:mm}" : "N/A",
                        AssignedDate = bulkAssignDto.Date
                    }
                };

                _logger.LogInformation("Bulk assign thành công: {Assigned} slots mới, {Skipped} slots đã tồn tại", 
                    createdSchedules.Count, existingCount);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi bulk assign working hours group {GroupId} cho ngày {Date}", 
                    bulkAssignDto.WorkingHoursGroupId, bulkAssignDto.Date.ToString("yyyy-MM-dd"));
                
                return new BulkAssignWorkingHoursResponseDTO
                {
                    IsSuccess = false,
                    Message = "Có lỗi xảy ra khi assign working hours group",
                    TotalSlotsAssigned = 0,
                    ExistingSlotsSkipped = 0
                };
            }
        }
    }
}