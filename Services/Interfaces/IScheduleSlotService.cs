using Contracts.DTOs.FacilitySchedule;

namespace Services.Interfaces
{
    public interface IScheduleSlotService
    {
        // ✅ Basic CRUD cho slots
        Task<List<ScheduleSlotDTO>> GetAllSlotsAsync();
        Task<ScheduleSlotDTO> GetSlotByIdAsync(int slotId);
        Task<List<ScheduleSlotDTO>> CreateSlotAsync(CreateScheduleSlotDTO createDto); // Return List vì working hours tạo nhiều slots
        Task<ScheduleSlotDTO> UpdateSlotAsync(int slotId, UpdateScheduleSlotDTO updateDto);
        Task<bool> DeleteSlotAsync(int slotId);
        
        // ✅ Working Hours Management
        Task<List<ScheduleSlotDTO>> GetWorkingHoursSlotsAsync(TimeOnly startTime, TimeOnly endTime);
        Task<bool> DeleteWorkingHoursAsync(TimeOnly startTime, TimeOnly endTime);
        Task<List<ScheduleSlotDTO>> UpdateWorkingHoursAsync(TimeOnly oldStartTime, TimeOnly oldEndTime, CreateScheduleSlotDTO newConfig);
        
        // ✅ Status Management
        Task<bool> UpdateSlotStatusAsync(int slotId, string status);
        Task<bool> DeleteMultipleSlotsAsync(List<int> slotIds);
    }
} 