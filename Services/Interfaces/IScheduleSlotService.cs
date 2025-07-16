using Contracts.DTOs.FacilitySchedule;

namespace Services.Interfaces
{
    public interface IScheduleSlotService
    {
        // ✅ Basic CRUD operations
        Task<List<ScheduleSlotDTO>> GetAllSlotsAsync();
        Task<List<ScheduleSlotDTO>> GetSlotsByFacilityAsync(int facilityId);
        Task<ScheduleSlotDTO> GetSlotByIdAsync(int slotId);
        Task<ScheduleSlotDTO> GetSlotByIdWithFacilityCheckAsync(int slotId, int facilityId);
        
        // ✅ Slot creation (working hours)
        Task<List<ScheduleSlotDTO>> CreateSlotAsync(CreateScheduleSlotDTO createDto, int facilityId);
        
        // ✅ Slot updates
        Task<ScheduleSlotDTO> UpdateSlotAsync(int slotId, UpdateScheduleSlotDTO updateDto, int facilityId);
        Task<bool> UpdateSlotStatusAsync(int slotId, string status);
        
        // ✅ Slot deletion
        Task<bool> DeleteSlotAsync(int slotId, int facilityId);
        Task<bool> DeleteMultipleSlotsAsync(List<int> slotIds, int facilityId);
        
        // ✅ Working hours management
        Task<bool> DeleteWorkingHoursAsync(TimeOnly startTime, TimeOnly endTime);
        Task<List<ScheduleSlotDTO>> UpdateWorkingHoursAsync(TimeOnly oldStartTime, TimeOnly oldEndTime, CreateScheduleSlotDTO newConfig, int facilityId);
        Task<List<ScheduleSlotDTO>> GetWorkingHoursSlotsAsync(TimeOnly startTime, TimeOnly endTime);
        
        // ✅ WorkingHoursGroupId management
        Task<List<ScheduleSlotDTO>> GetSlotsByWorkingHoursGroupIdAsync(string workingHoursGroupId);
        Task<List<WorkingHoursGroupDTO>> GetWorkingHoursGroupsByFacilityAsync(int facilityId);
    }
}

// ✅ DTO for Working Hours Group display
public class WorkingHoursGroupDTO
{
    public string GroupId { get; set; }
    public string Description { get; set; }
    public int TotalSlots { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int SlotDurationMinutes { get; set; }
    public TimeOnly? LunchBreakStart { get; set; }
    public TimeOnly? LunchBreakEnd { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ScheduleSlotDTO> Slots { get; set; } = new List<ScheduleSlotDTO>();
} 