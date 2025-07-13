using Contracts.DTOs.Child;
using Contracts.DTOs.Disease;
using Contracts.DTOs.FacilitySchedule;
using Contracts.DTOs.VaccinationFacility;
using Contracts.DTOs.VaccinePackage;

namespace Contracts.DTOs.Appointment
{
    /// <summary>
    /// DTO cho response khi đặt lịch thành công
    /// </summary>
    public class AppointmentBookingResponseDTO
    {
        public int AppointmentId { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Note { get; set; }

        // Thông tin trẻ
        public ChildDTO Child { get; set; }

        // Thông tin bệnh
        public DiseaseDTO Disease { get; set; }

        // Thông tin cơ sở
        public VaccinationFacilityDTO Facility { get; set; }

        // Thông tin gói vaccine (nếu có)
        public VaccinePackageDTO? Package { get; set; }

        // Thông tin lịch hẹn
        public AppointmentScheduleDTO Schedule { get; set; }

        // Tổng chi phí dự kiến
        public decimal EstimatedCost { get; set; }
    }
} 