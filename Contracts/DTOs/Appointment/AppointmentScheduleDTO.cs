using System.ComponentModel.DataAnnotations;

namespace Contracts.DTOs.Appointment
{
    public class AppointmentScheduleDTO
    {
        public int ScheduleId { get; set; }
        public int FacilityId { get; set; }
        public string FacilityName { get; set; }
        public int SlotId { get; set; }
        public string SlotTime { get; set; }
        public DateOnly Date { get; set; }
        public int? BookedCount { get; set; }
        public int MaxCapacity { get; set; }
        public int AvailableSlots { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
} 