using System.ComponentModel.DataAnnotations;

namespace Contracts.DTOs.Appointment
{
    public class CreateAppointmentScheduleDTO
    {
        [Required(ErrorMessage = "FacilityId là bắt buộc")]
        public int FacilityId { get; set; }

        [Required(ErrorMessage = "SlotId là bắt buộc")]
        public int SlotId { get; set; }

        [Required(ErrorMessage = "Date là bắt buộc")]
        public DateOnly Date { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "BookedCount phải >= 0")]
        public int? BookedCount { get; set; } = 0;

        [Required(ErrorMessage = "Status là bắt buộc")]
        [RegularExpression("^(Active|Inactive|Holiday|Maintenance)$", 
            ErrorMessage = "Status phải là: Active, Inactive, Holiday, hoặc Maintenance")]
        public string Status { get; set; } = "Active";
    }
} 