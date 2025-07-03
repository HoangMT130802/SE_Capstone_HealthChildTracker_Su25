using System.ComponentModel.DataAnnotations;

namespace Contracts.DTOs.FacilitySchedule
{
    public class CreateScheduleSlotDTO
    {
        [Required(ErrorMessage = "SlotTime là bắt buộc")]
        [RegularExpression(@"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]\s*-\s*([0-1]?[0-9]|2[0-3]):[0-5][0-9]$",
            ErrorMessage = "SlotTime phải có định dạng HH:mm - HH:mm (ví dụ: 08:00 - 09:00)")]
        public string SlotTime { get; set; }

        [Required(ErrorMessage = "MaxCapacity là bắt buộc")]
        [Range(1, 100, ErrorMessage = "MaxCapacity phải từ 1 đến 100")]
        public int MaxCapacity { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "BookedCount phải >= 0")]
        public int BookedCount { get; set; } = 0;

        [Required(ErrorMessage = "Status là bắt buộc")]
        [RegularExpression("^(Active|Inactive)$", 
            ErrorMessage = "Status phải là: Active hoặc Inactive")]
        public string Status { get; set; } = "Active";
    }
} 