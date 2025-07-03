namespace Contracts.DTOs.FacilitySchedule
{
    public class ScheduleSlotDTO
    {
        public int SlotId { get; set; }
        public string SlotTime { get; set; }
        public int MaxCapacity { get; set; }
        public int BookedCount { get; set; }
        public int AvailableCapacity { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
} 