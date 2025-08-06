using System.ComponentModel.DataAnnotations;

namespace Contracts.DTOs.Appointment
{
    /// <summary>
    /// DTO cho yêu cầu đặt lịch tiêm chủng - chứa tất cả thông tin từ luồng đặt lịch
    /// </summary>
    public class AppointmentBookingRequestDTO
    {
        [Required(ErrorMessage = "ID trẻ là bắt buộc")]
        public int ChildId { get; set; }

        /// <summary>
        /// ID bệnh (cho tương thích ngược)
        /// </summary>
        public int? DiseaseId { get; set; }

        /// <summary>
        /// Danh sách ID bệnh (hỗ trợ đặt nhiều bệnh cùng lúc)
        /// </summary>
        public List<int>? DiseaseIds { get; set; }

        [Required(ErrorMessage = "ID cơ sở tiêm chủng là bắt buộc")]
        public int FacilityId { get; set; }

        /// <summary>
        /// ID Order đã mua (sử dụng gói đã tồn tại)
        /// </summary>
        public int? OrderId { get; set; }

        /// <summary>
        /// ID gói vaccine (để mua gói mới cùng lúc với đặt lịch)
        /// </summary>
        public int? PackageId { get; set; }

        /// <summary>
        /// Danh sách ID vaccine nếu đặt vaccine lẻ (không qua gói)
        /// </summary>
        public List<int>? FacilityVaccineIds { get; set; }

        [Required(ErrorMessage = "ID lịch hẹn là bắt buộc")]
        public int ScheduleId { get; set; }

        /// <summary>
        /// Ghi chú thêm cho cuộc hẹn
        /// </summary>
        [StringLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự")]
        public string? Note { get; set; }
    }
} 