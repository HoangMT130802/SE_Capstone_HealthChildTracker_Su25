using Contracts.DTOs.FacilitySchedule;

namespace Contracts.DTOs.Appointment
{
    /// <summary>
    /// DTO cho lịch trống có sẵn của cơ sở
    /// </summary>
    public class AvailableSchedulesDTO
    {
        public int FacilityId { get; set; }
        public string FacilityName { get; set; }
        
        /// <summary>
        /// Ngày từ
        /// </summary>
        public DateOnly FromDate { get; set; }
        
        /// <summary>
        /// Ngày đến
        /// </summary>
        public DateOnly ToDate { get; set; }

        /// <summary>
        /// Danh sách lịch theo ngày
        /// </summary>
        public List<DailyScheduleDTO> DailySchedules { get; set; } = new List<DailyScheduleDTO>();
    }

    /// <summary>
    /// DTO cho lịch trong một ngày
    /// </summary>
    public class DailyScheduleDTO
    {
        public DateOnly Date { get; set; }
        public string DayOfWeek { get; set; }
        public bool IsAvailable { get; set; }
        public List<AvailableSlotDTO> AvailableSlots { get; set; } = new List<AvailableSlotDTO>();
    }

    /// <summary>
    /// DTO cho slot thời gian có sẵn
    /// </summary>
    public class AvailableSlotDTO
    {
        public int ScheduleId { get; set; }
        public int SlotId { get; set; }
        public string SlotTime { get; set; }
        public int MaxCapacity { get; set; }
        public int BookedCount { get; set; }
        public int AvailableCapacity { get; set; }
        public string Status { get; set; }
        
        /// <summary>
        /// Có thể đặt lịch không
        /// </summary>
        public bool IsBookable => AvailableCapacity > 0 && Status == "Active";
        
        /// <summary>
        /// Phần trăm đã đặt
        /// </summary>
        public double BookingPercentage => MaxCapacity > 0 ? (double)BookedCount / MaxCapacity * 100 : 0;
    }
} 