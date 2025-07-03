using AutoMapper;
using Contracts.DTOs.Appointment;
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

        public AppointmentScheduleService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<AppointmentScheduleService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<AppointmentScheduleDTO> CreateScheduleAsync(CreateAppointmentScheduleDTO createDto)
        {
            try
            {
                _logger.LogInformation("Creating appointment schedule");

                var schedule = _mapper.Map<AppointmentSchedule>(createDto);
                var repository = _unitOfWork.GetRepository<AppointmentSchedule>();
                await repository.AddAsync(schedule);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully created appointment schedule");
                return _mapper.Map<AppointmentScheduleDTO>(schedule);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating appointment schedule");
                throw;
            }
        }

        public async Task<AppointmentScheduleDTO> GetScheduleByIdAsync(int scheduleId)
        {
            try
            {
                var repository = _unitOfWork.GetRepository<AppointmentSchedule>();
                var schedule = await repository.GetAsync(s => s.ScheduleId == scheduleId);

                if (schedule == null)
                {
                    throw new ArgumentException($"Schedule với ID {scheduleId} không tồn tại");
                }

                return _mapper.Map<AppointmentScheduleDTO>(schedule);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting schedule by ID");
                throw;
            }
        }

        public async Task<List<AppointmentScheduleDTO>> GetSchedulesAsync(int page, int size, int? facilityId = null, string? status = null)
        {
            try
            {
                var repository = _unitOfWork.GetRepository<AppointmentSchedule>();
                var schedules = await repository.GetAllAsync("");

                var result = schedules
                    .Where(s => (!facilityId.HasValue || s.FacilityId == facilityId.Value) && 
                               (string.IsNullOrEmpty(status) || s.Status == status))
                    .OrderBy(s => s.Date)
                    .ThenBy(s => s.SlotId)
                    .ToList();

                return _mapper.Map<List<AppointmentScheduleDTO>>(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting schedules");
                throw;
            }
        }

        public async Task<AppointmentScheduleDTO> UpdateScheduleAsync(int scheduleId, UpdateAppointmentScheduleDTO updateDto)
        {
            try
            {
                _logger.LogInformation("Updating appointment schedule");

                var repository = _unitOfWork.GetRepository<AppointmentSchedule>();
                var schedule = await repository.GetAsync(s => s.ScheduleId == scheduleId);

                if (schedule == null)
                {
                    throw new ArgumentException($"Schedule với ID {scheduleId} không tồn tại");
                }

                _mapper.Map(updateDto, schedule);
                repository.Update(schedule);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully updated appointment schedule");
                return _mapper.Map<AppointmentScheduleDTO>(schedule);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating appointment schedule");
                throw;
            }
        }

        public async Task<bool> DeleteScheduleAsync(int scheduleId)
        {
            try
            {
                _logger.LogInformation("Deleting appointment schedule");

                var repository = _unitOfWork.GetRepository<AppointmentSchedule>();
                var schedule = await repository.GetAsync(s => s.ScheduleId == scheduleId);

                if (schedule == null)
                {
                    throw new ArgumentException($"Schedule với ID {scheduleId} không tồn tại");
                }

                repository.Delete(schedule);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully deleted appointment schedule");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting appointment schedule");
                throw;
            }
        }

        public async Task<List<AppointmentScheduleDTO>> GetSchedulesByFacilityAsync(int facilityId)
        {
            try
            {
                var repository = _unitOfWork.GetRepository<AppointmentSchedule>();
                var schedules = await repository.GetAllAsync("");

                var result = schedules
                    .Where(s => s.FacilityId == facilityId)
                    .OrderBy(s => s.Date)
                    .ThenBy(s => s.SlotId)
                    .ToList();

                return _mapper.Map<List<AppointmentScheduleDTO>>(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting schedules by facility");
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
                    .OrderBy(s => s.FacilityId)
                    .ThenBy(s => s.SlotId)
                    .ToList();

                return _mapper.Map<List<AppointmentScheduleDTO>>(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting schedules by date");
                throw;
            }
        }

        public async Task<List<AppointmentScheduleDTO>> GetAvailableSchedulesAsync(DateTime date, int? facilityId = null)
        {
            try
            {
                var repository = _unitOfWork.GetRepository<AppointmentSchedule>();
                var schedules = await repository.GetAllAsync("");

                var dateOnly = DateOnly.FromDateTime(date);
                var result = schedules
                    .Where(s => s.Date == dateOnly && 
                               s.Status == "Available" &&
                               (!facilityId.HasValue || s.FacilityId == facilityId.Value))
                    .OrderBy(s => s.FacilityId)
                    .ThenBy(s => s.SlotId)
                    .ToList();

                return _mapper.Map<List<AppointmentScheduleDTO>>(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available schedules");
                throw;
            }
        }

        public async Task<bool> BookScheduleAsync(int scheduleId, int memberId)
        {
            try
            {
                _logger.LogInformation("Booking appointment schedule");

                var repository = _unitOfWork.GetRepository<AppointmentSchedule>();
                var schedule = await repository.GetAsync(s => s.ScheduleId == scheduleId);

                if (schedule == null)
                {
                    throw new ArgumentException($"Schedule với ID {scheduleId} không tồn tại");
                }

                if (schedule.Status != "Available")
                {
                    throw new InvalidOperationException("Schedule không khả dụng để đặt lịch");
                }

                schedule.Status = "Booked";
                schedule.UpdatedAt = DateTime.UtcNow;

                repository.Update(schedule);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully booked appointment schedule");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error booking appointment schedule");
                throw;
            }
        }

        public async Task<bool> CancelScheduleAsync(int scheduleId)
        {
            try
            {
                _logger.LogInformation("Canceling appointment schedule");

                var repository = _unitOfWork.GetRepository<AppointmentSchedule>();
                var schedule = await repository.GetAsync(s => s.ScheduleId == scheduleId);

                if (schedule == null)
                {
                    throw new ArgumentException($"Schedule với ID {scheduleId} không tồn tại");
                }

                if (schedule.Status != "Booked")
                {
                    throw new InvalidOperationException("Chỉ có thể hủy schedule đã được đặt");
                }

                schedule.Status = "Available";
                schedule.UpdatedAt = DateTime.UtcNow;

                repository.Update(schedule);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully canceled appointment schedule");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error canceling appointment schedule");
                throw;
            }
        }

        public async Task<bool> SetHolidayAsync(int facilityId, DateTime date, string reason)
        {
            try
            {
                _logger.LogInformation("Setting holiday schedule");

                var repository = _unitOfWork.GetRepository<AppointmentSchedule>();
                var schedules = await repository.GetAllAsync("");

                var dateOnly = DateOnly.FromDateTime(date);
                var targetSchedules = schedules
                    .Where(s => s.FacilityId == facilityId && s.Date == dateOnly)
                    .ToList();

                foreach (var schedule in targetSchedules)
                {
                    schedule.Status = "Holiday";
                    schedule.UpdatedAt = DateTime.UtcNow;
                }

                if (targetSchedules.Any())
                {
                    repository.UpdateRange(targetSchedules);
                    await _unitOfWork.SaveChangesAsync();
                }

                _logger.LogInformation("Successfully set holiday schedule");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting holiday schedule");
                throw;
            }
        }

        public async Task<bool> SetMaintenanceAsync(int facilityId, DateTime date, string reason)
        {
            try
            {
                _logger.LogInformation("Setting maintenance schedule");

                var repository = _unitOfWork.GetRepository<AppointmentSchedule>();
                var schedules = await repository.GetAllAsync("");

                var dateOnly = DateOnly.FromDateTime(date);
                var targetSchedules = schedules
                    .Where(s => s.FacilityId == facilityId && s.Date == dateOnly)
                    .ToList();

                foreach (var schedule in targetSchedules)
                {
                    schedule.Status = "Maintenance";
                    schedule.UpdatedAt = DateTime.UtcNow;
                }

                if (targetSchedules.Any())
                {
                    repository.UpdateRange(targetSchedules);
                    await _unitOfWork.SaveChangesAsync();
                }

                _logger.LogInformation("Successfully set maintenance schedule");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting maintenance schedule");
                throw;
            }
        }

        public async Task<List<AppointmentScheduleDTO>> CreateSchedulesForDateRangeAsync(int facilityId, DateTime startDate, DateTime endDate)
        {
            try
            {
                _logger.LogInformation("Creating schedules for date range");

                var slotRepository = _unitOfWork.GetRepository<ScheduleSlot>();
                var slots = await slotRepository.GetAllAsync("");
                var activeSlots = slots.Where(s => s.Status == "Active").ToList();

                if (!activeSlots.Any())
                {
                    throw new InvalidOperationException("Không có slot nào khả dụng");
                }

                var scheduleRepository = _unitOfWork.GetRepository<AppointmentSchedule>();
                var createdSchedules = new List<AppointmentSchedule>();

                for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
                {
                    var dateOnly = DateOnly.FromDateTime(date);
                    
                    foreach (var slot in activeSlots)
                    {
                        var existingSchedules = await scheduleRepository.GetAllAsync("");
                        var exists = existingSchedules.Any(s => s.FacilityId == facilityId && 
                                                                s.Date == dateOnly && 
                                                                s.SlotId == slot.SlotId);

                        if (!exists)
                        {
                            var schedule = new AppointmentSchedule
                            {
                                FacilityId = facilityId,
                                SlotId = slot.SlotId,
                                Date = dateOnly,
                                Status = "Available",
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };

                            createdSchedules.Add(schedule);
                        }
                    }
                }

                if (createdSchedules.Any())
                {
                    await scheduleRepository.AddRangeAsync(createdSchedules);
                    await _unitOfWork.SaveChangesAsync();
                }

                _logger.LogInformation("Successfully created schedules for date range");
                return _mapper.Map<List<AppointmentScheduleDTO>>(createdSchedules);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating schedules for date range");
                throw;
            }
        }

        public async Task<bool> UpdateScheduleStatusAsync(int scheduleId, string status)
        {
            try
            {
                _logger.LogInformation("Updating schedule status");

                var repository = _unitOfWork.GetRepository<AppointmentSchedule>();
                var schedule = await repository.GetAsync(s => s.ScheduleId == scheduleId);

                if (schedule == null)
                {
                    throw new ArgumentException($"Schedule với ID {scheduleId} không tồn tại");
                }

                schedule.Status = status;
                schedule.UpdatedAt = DateTime.UtcNow;

                repository.Update(schedule);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully updated schedule status");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating schedule status");
                throw;
            }
        }

        public async Task<bool> IsScheduleAvailableAsync(int scheduleId)
        {
            try
            {
                var repository = _unitOfWork.GetRepository<AppointmentSchedule>();
                var schedule = await repository.GetAsync(s => s.ScheduleId == scheduleId);

                if (schedule == null)
                {
                    return false;
                }

                return schedule.Status == "Available";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking schedule availability");
                throw;
            }
        }

        public async Task<List<AppointmentScheduleDTO>> GetSchedulesByManagerAsync(int managerId)
        {
            try
            {
                var facilityStaffRepo = _unitOfWork.GetRepository<FacilityStaff>();
                var allStaff = await facilityStaffRepo.GetAllAsync("");
                var managerStaff = allStaff.FirstOrDefault(fs => fs.StaffId == managerId && fs.Position == "Manager");

                if (managerStaff == null)
                {
                    throw new ArgumentException("Manager không tồn tại hoặc không có facility");
                }

                var repository = _unitOfWork.GetRepository<AppointmentSchedule>();
                var schedules = await repository.GetAllAsync("");

                var result = schedules
                    .Where(s => s.FacilityId == managerStaff.FacilityId)
                    .OrderBy(s => s.Date)
                    .ThenBy(s => s.SlotId)
                    .ToList();

                return _mapper.Map<List<AppointmentScheduleDTO>>(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting schedules by manager");
                throw;
            }
        }

        public async Task<List<AppointmentScheduleDTO>> CreateMultipleSchedulesAsync(List<CreateAppointmentScheduleDTO> createDtos)
        {
            try
            {
                _logger.LogInformation("Creating multiple appointment schedules");

                var repository = _unitOfWork.GetRepository<AppointmentSchedule>();
                var createdSchedules = new List<AppointmentSchedule>();

                foreach (var createDto in createDtos)
                {
                    var schedule = _mapper.Map<AppointmentSchedule>(createDto);
                    createdSchedules.Add(schedule);
                }

                await repository.AddRangeAsync(createdSchedules);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully created multiple appointment schedules");
                return _mapper.Map<List<AppointmentScheduleDTO>>(createdSchedules);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating multiple appointment schedules");
                throw;
            }
        }

        public async Task<bool> UpdateMultipleSchedulesStatusAsync(List<int> scheduleIds, string status)
        {
            try
            {
                _logger.LogInformation("Updating multiple schedules status");

                var repository = _unitOfWork.GetRepository<AppointmentSchedule>();
                var allSchedules = await repository.GetAllAsync("");
                var schedulesToUpdate = allSchedules.Where(s => scheduleIds.Contains(s.ScheduleId)).ToList();

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

                _logger.LogInformation("Successfully updated multiple schedules status");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating multiple schedules status");
                throw;
            }
        }
    }
}