using System.ComponentModel.DataAnnotations;

namespace Contracts.DTOs.FacilitySchedule
{
    public class UpdateScheduleSlotDTO
    {
        // ✅ Chỉ cho phép update một số field nhất định
        [Range(1, 100, ErrorMessage = "MaxCapacity phải từ 1 đến 100")]
        public int MaxCapacity { get; set; }

        [RegularExpression("^(Available|Unavailable|Active|Inactive)$", 
            ErrorMessage = "Status phải là: Available, Unavailable, Active hoặc Inactive")]
        public string Status { get; set; }
        
        // ✅ Note: SlotTime, StartTime, EndTime, Duration không được update
        // ✅ WorkingHoursGroupId không được update
        // ✅ BookedCount sẽ tính tự động từ appointments
        // ✅ Working Hours không update từng slot, mà delete + recreate toàn bộ
    }
} 