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

        public async Task<AppointmentScheduleDTO> CreateScheduleAsync(CreateAppointmentScheduleDTO createDto)
        {
            try
            {
                _logger.LogInformation("Tạo lịch hẹn mới");

                var schedule = _mapper.Map<AppointmentSchedule>(createDto);
                var repository = _unitOfWork.GetRepository<AppointmentSchedule>();
                await repository.AddAsync(schedule);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Tạo lịch hẹn thành công");
                return _mapper.Map<AppointmentScheduleDTO>(schedule);
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
    }
}