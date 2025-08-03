using System.ComponentModel.DataAnnotations;

namespace Contracts.DTOs.Appointment
{
    public class AppointmentRebookingRequestDTO
    {
        [Required(ErrorMessage = "ChildVaccineProfileId là bắt buộc")]
        public int ChildVaccineProfileId { get; set; }
        
        [Required(ErrorMessage = "ScheduleId là bắt buộc")]
        public int ScheduleId { get; set; }
        
        public string? Note { get; set; }
    }
}