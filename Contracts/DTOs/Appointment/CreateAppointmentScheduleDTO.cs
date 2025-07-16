using System.ComponentModel.DataAnnotations;

namespace Contracts.DTOs.Appointment
{
    public class CreateAppointmentScheduleDTO
    {
        [Required(ErrorMessage = "FacilityId là bắt buộc")]
        public int FacilityId { get; set; }

        /// <summary>
        /// SlotId cho việc tạo single appointment schedule
        /// </summary>
        public int? SlotId { get; set; }

        /// <summary>
        /// WorkingHoursGroupId cho việc tạo multiple appointment schedules
        /// </summary>
        public string? WorkingHoursGroupId { get; set; }

        [Required(ErrorMessage = "Date là bắt buộc")]
        public DateOnly Date { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "BookedCount phải >= 0")]
        public int? BookedCount { get; set; } = 0;

        [Required(ErrorMessage = "Status là bắt buộc")]
        [RegularExpression("^(Available|Unavailable|Holiday|Maintenance)$", 
            ErrorMessage = "Status phải là: Available, Unavailable, Holiday, hoặc Maintenance")]
        public string Status { get; set; } = "Available";

        /// <summary>
        /// Validation để đảm bảo có ít nhất SlotId hoặc WorkingHoursGroupId
        /// </summary>
        public bool IsValid()
        {
            return SlotId.HasValue || !string.IsNullOrEmpty(WorkingHoursGroupId);
        }
    }
} 