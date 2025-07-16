using System.ComponentModel.DataAnnotations;

namespace Contracts.DTOs.FacilitySchedule
{
    public class ScheduleSlotDTO
    {
        public int SlotId { get; set; }

        [Required(ErrorMessage = "FacilityId là bắt buộc")]
        public int FacilityId { get; set; }

        public string FacilityName { get; set; }

        // ✅ Working Hours Group ID để nhóm slots
        public string WorkingHoursGroupId { get; set; }

        // ✅ SlotTime để hiển thị "08:00 - 09:00" cho frontend
        public string SlotTime { get; set; }

        // ✅ Thời gian bắt đầu và kết thúc cụ thể của slot
        [Required(ErrorMessage = "StartTime là bắt buộc")]
        public TimeOnly StartTime { get; set; }

        [Required(ErrorMessage = "EndTime là bắt buộc")]
        public TimeOnly EndTime { get; set; }

        // ✅ Thời lượng slot (tính bằng phút)
        [Required(ErrorMessage = "SlotDurationMinutes là bắt buộc")]
        public int SlotDurationMinutes { get; set; }

        // ✅ Lunch break info (optional)
        public TimeOnly? LunchBreakStart { get; set; }
        public TimeOnly? LunchBreakEnd { get; set; }

        [Required(ErrorMessage = "MaxCapacity là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "MaxCapacity phải lớn hơn 0")]
        public int MaxCapacity { get; set; }

        public int BookedCount { get; set; }

        public int AvailableCapacity => MaxCapacity - BookedCount;

        [Required(ErrorMessage = "Status là bắt buộc")]
        public string Status { get; set; }

        // ✅ Working hours flag
        public bool IsWorkingHours { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        // ✅ Thông tin để hiển thị
        public int SlotNumber { get; set; }
    }
} 