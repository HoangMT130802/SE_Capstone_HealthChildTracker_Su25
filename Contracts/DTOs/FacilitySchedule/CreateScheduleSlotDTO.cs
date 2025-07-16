using System.ComponentModel.DataAnnotations;

namespace Contracts.DTOs.FacilitySchedule
{
    public class CreateScheduleSlotDTO
    {
        // ✅ Working hours configuration
        [Required(ErrorMessage = "StartTime là bắt buộc")]
        public TimeOnly StartTime { get; set; }
        
        [Required(ErrorMessage = "EndTime là bắt buộc")]
        public TimeOnly EndTime { get; set; }
        
        [Range(1, 1440, ErrorMessage = "SlotDurationMinutes phải từ 1 đến 1440 phút")]
        public int SlotDurationMinutes { get; set; } = 60; // Default 1 hour

        // ✅ Lunch break config (optional)
        public TimeOnly? LunchBreakStart { get; set; }
        public TimeOnly? LunchBreakEnd { get; set; }

        [Required(ErrorMessage = "MaxCapacity là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "MaxCapacity phải lớn hơn 0")]
        public int MaxCapacity { get; set; } = 10;

        public string Status { get; set; } = "Available";

        // ✅ Luôn tạo working hours (tương thích với entity)
        public bool IsWorkingHours { get; set; } = true;

        // ✅ Validation logic
        public bool IsValid()
        {
            // Kiểm tra working hours hợp lệ
            if (StartTime >= EndTime)
                return false;

            // Kiểm tra lunch break nếu có
            if (LunchBreakStart.HasValue && LunchBreakEnd.HasValue)
            {
                if (LunchBreakStart.Value >= LunchBreakEnd.Value)
                    return false;
                
                // Lunch break phải nằm trong working hours
                if (LunchBreakStart.Value < StartTime || LunchBreakEnd.Value > EndTime)
                    return false;
            }

            return true;
        }
    }
} 