using System.ComponentModel.DataAnnotations;

namespace Contracts.DTOs.FacilitySchedule
{
    public class UpdateScheduleSlotDTO
    {
        // ✅ Single Slot Update (chỉ cho IsWorkingHours = false)
        [RegularExpression(@"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]\s*-\s*([0-1]?[0-9]|2[0-3]):[0-5][0-9]$",
            ErrorMessage = "SlotTime phải có định dạng HH:mm - HH:mm (ví dụ: 08:00 - 09:00)")]
        public string? SlotTime { get; set; }

        [Range(1, 100, ErrorMessage = "MaxCapacity phải từ 1 đến 100")]
        public int MaxCapacity { get; set; }

        // ❌ Bỏ BookedCount - sẽ tính tự động từ appointments
        // [Range(0, int.MaxValue, ErrorMessage = "BookedCount phải >= 0")]
        // public int BookedCount { get; set; }

        [RegularExpression("^(Active|Inactive)$", 
            ErrorMessage = "Status phải là: Active hoặc Inactive")]
        public string Status { get; set; }
        
        // Note: Working Hours không update từng slot, mà delete + recreate toàn bộ
    }
} 