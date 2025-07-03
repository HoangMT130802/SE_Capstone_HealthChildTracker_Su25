using System.ComponentModel.DataAnnotations;

namespace Contracts.DTOs.Appointment
{
    public class UpdateAppointmentScheduleDTO
    {
        [Required(ErrorMessage = "Date là bắt buộc")]
        public DateOnly Date { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "BookedCount phải >= 0")]
        public int? BookedCount { get; set; }

        [Required(ErrorMessage = "Status là bắt buộc")]
        [RegularExpression("^(Active|Inactive|Holiday|Maintenance)$", 
            ErrorMessage = "Status phải là: Active, Inactive, Holiday, hoặc Maintenance")]
        public string Status { get; set; }
    }
} 