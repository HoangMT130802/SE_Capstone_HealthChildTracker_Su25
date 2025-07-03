using Contracts.DTOs.Appointment;

namespace Services.Interfaces
{
    public interface IAppointmentScheduleService
    {
        // CRUD Operations
        Task<AppointmentScheduleDTO> CreateScheduleAsync(CreateAppointmentScheduleDTO createDto);
        Task<AppointmentScheduleDTO> GetScheduleByIdAsync(int scheduleId);
        Task<List<AppointmentScheduleDTO>> GetSchedulesAsync(int page, int size, int? facilityId = null, string? status = null);
        Task<AppointmentScheduleDTO> UpdateScheduleAsync(int scheduleId, UpdateAppointmentScheduleDTO updateDto);
        Task<bool> DeleteScheduleAsync(int scheduleId);

        // Business Operations
        Task<List<AppointmentScheduleDTO>> GetSchedulesByFacilityAsync(int facilityId);
        Task<List<AppointmentScheduleDTO>> GetSchedulesByDateAsync(DateTime date);
        Task<List<AppointmentScheduleDTO>> GetAvailableSchedulesAsync(DateTime date, int? facilityId = null);
        Task<bool> BookScheduleAsync(int scheduleId, int memberId);
        Task<bool> CancelScheduleAsync(int scheduleId);

        // Manager Operations
        Task<bool> SetHolidayAsync(int facilityId, DateTime date, string reason);
        Task<bool> SetMaintenanceAsync(int facilityId, DateTime date, string reason);

        // Batch Operations
        Task<List<AppointmentScheduleDTO>> CreateSchedulesForDateRangeAsync(int facilityId, DateTime startDate, DateTime endDate);
        Task<bool> UpdateScheduleStatusAsync(int scheduleId, string status);
        Task<bool> IsScheduleAvailableAsync(int scheduleId);
        Task<List<AppointmentScheduleDTO>> GetSchedulesByManagerAsync(int managerId);
        Task<List<AppointmentScheduleDTO>> CreateMultipleSchedulesAsync(List<CreateAppointmentScheduleDTO> createDtos);
        Task<bool> UpdateMultipleSchedulesStatusAsync(List<int> scheduleIds, string status);
    }
} 