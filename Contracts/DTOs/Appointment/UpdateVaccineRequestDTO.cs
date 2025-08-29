using System.ComponentModel.DataAnnotations;

namespace Contracts.DTOs.Appointment
{
    /// <summary>
    /// DTO cho việc thay đổi vaccine trong appointment (dành cho bác sĩ/staff)
    /// </summary>
    public class UpdateVaccineRequestDTO
    {
        /// <summary>
        /// ID của VaccinationAppointmentDetail cần thay đổi
        /// </summary>
        [Required(ErrorMessage = "AppointmentDetailId là bắt buộc")]
        public int AppointmentDetailId { get; set; }

        /// <summary>
        /// ID của vaccine mới
        /// </summary>
        [Required(ErrorMessage = "NewVaccineId là bắt buộc")]
        public int NewVaccineId { get; set; }

        /// <summary>
        /// Lý do thay đổi vaccine
        /// </summary>
        [Required(ErrorMessage = "Reason là bắt buộc")]
        [StringLength(500, ErrorMessage = "Lý do không được vượt quá 500 ký tự")]
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// Ghi chú thêm
        /// </summary>
        [StringLength(1000, ErrorMessage = "Ghi chú không được vượt quá 1000 ký tự")]
        public string? Notes { get; set; }
    }
}
