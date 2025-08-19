using System.ComponentModel.DataAnnotations;

namespace Contracts.DTOs.VaccinationFacilityPaymentAccount
{
    /// <summary>
    /// DTO đơn giản cho facility payment - chỉ cần AppointmentId
    /// </summary>
    public class CreateFacilityPaymentDTO
    {
        /// <summary>
        /// ID cuộc hẹn - từ đây có thể truy xuất tất cả thông tin cần thiết:
        /// - FacilityId từ Schedule
        /// - OrderId (nếu có) từ VaccinationAppointment
        /// - VaccineIds từ VaccinationAppointmentDetail (nếu tiêm lẻ)
        /// - ChildId từ VaccinationAppointment
        /// </summary>
        [Required(ErrorMessage = "AppointmentId là bắt buộc")]
        public int AppointmentId { get; set; }
    }
}

