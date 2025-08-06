using Contracts.DTOs.FacilitySchedule;
using Contracts.DTOs.VaccinationFacility;

namespace Contracts.DTOs.Appointment
{
    /// <summary>
    /// DTO cho thông tin lịch hẹn chi tiết
    /// </summary>
    public class AppointmentScheduleDTO
    {
        public int ScheduleId { get; set; }
        public int FacilityId { get; set; }
        public int SlotId { get; set; }
        public DateOnly Date { get; set; }
        public int BookedCount { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Thông tin cơ sở
        public VaccinationFacilityDTO Facility { get; set; }

        // Thông tin slot thời gian
        public ScheduleSlotDTO Slot { get; set; }

        // ✅ Thêm flat properties để dễ sử dụng
        public string FacilityName => Facility?.FacilityName ?? "";
        public string SlotTime => Slot?.SlotTime ?? "";
        public int MaxCapacity => Slot?.MaxCapacity ?? 0;
        public int AvailableSlots => MaxCapacity - BookedCount;

        // Thông tin bổ sung
        public int AvailableCapacity => Slot?.MaxCapacity - BookedCount ?? 0;
        public bool IsAvailable => AvailableCapacity > 0 && Status == "active";
    }
} 