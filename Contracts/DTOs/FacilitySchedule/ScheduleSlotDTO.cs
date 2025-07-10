namespace Contracts.DTOs.FacilitySchedule
{
    public class ScheduleSlotDTO
    {
        public int SlotId { get; set; }
        public int SlotNumber { get; set; }
        
        // ✅ Single Slot Time
        public string? SlotTime { get; set; }
        
        // ✅ Working Hours Config
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        public int? SlotDurationMinutes { get; set; }
        public TimeOnly? LunchBreakStart { get; set; }
        public TimeOnly? LunchBreakEnd { get; set; }
        
        public int MaxCapacity { get; set; }
        public int BookedCount { get; set; }
        public int AvailableCapacity { get; set; }
        public string Status { get; set; }
        public bool IsWorkingHours { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
} 