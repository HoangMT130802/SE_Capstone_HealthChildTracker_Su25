using Contracts.DTOs.Appointment;

namespace Services.Interfaces
{
    public interface IAppointmentScheduleService
    {
        // Manager quản lý appointment schedules
        Task<List<AppointmentScheduleDTO>> GetAllSchedulesAsync();
        Task<List<AppointmentScheduleDTO>> GetSchedulesByWeekAsync(DateTime startOfWeek);
        Task<List<AppointmentScheduleDTO>> GetSchedulesByMonthAsync(DateTime month);
        Task<List<AppointmentScheduleDTO>> GetSchedulesByDateAsync(DateTime date);
        
        // Manager tạo/sửa/xóa lịch - support cả single và bulk creation
        Task<List<AppointmentScheduleDTO>> CreateScheduleAsync(CreateAppointmentScheduleDTO createDto);
        Task<AppointmentScheduleDTO> UpdateScheduleAsync(int scheduleId, UpdateAppointmentScheduleDTO updateDto);
        Task<bool> DeleteScheduleAsync(int scheduleId);
        Task<bool> DeleteSchedulesByDateAsync(DateTime date);
        
        // Manager quản lý trạng thái ngày
        Task<bool> UpdateDayStatusAsync(DateTime date, string status);
        
        // Manager thêm slots vào lịch
        Task<List<AppointmentScheduleDTO>> AddSlotsToScheduleAsync(DateTime date, List<int> slotIds);
        
        // Manager bulk assign working hours group vào ngày
        Task<BulkAssignWorkingHoursResponseDTO> BulkAssignWorkingHoursAsync(BulkAssignWorkingHoursDTO bulkAssignDto);
        
        // Manager xem slots trong ngày
        Task<List<AppointmentScheduleDTO>> GetDayScheduleWithSlotsAsync(DateTime date);
    }
} 