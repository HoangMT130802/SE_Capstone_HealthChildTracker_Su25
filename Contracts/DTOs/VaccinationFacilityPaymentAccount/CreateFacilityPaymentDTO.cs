using System.ComponentModel.DataAnnotations;

namespace Contracts.DTOs.VaccinationFacilityPaymentAccount
{
    /// <summary>
    /// DTO thống nhất cho tất cả loại thanh toán: Order, Package, Individual Vaccine
    /// </summary>
    public class CreateFacilityPaymentDTO
    {
        [Required(ErrorMessage = "FacilityId là bắt buộc")]
        public int FacilityId { get; set; }

        [Required(ErrorMessage = "AppointmentId là bắt buộc")]
        public int AppointmentId { get; set; }

        /// <summary>
        /// Loại thanh toán: "ORDER", "PACKAGE", "INDIVIDUAL_VACCINE"
        /// </summary>
        [Required(ErrorMessage = "PaymentType là bắt buộc")]
        public string PaymentType { get; set; }

        // === ORDER Payment ===
        /// <summary>
        /// Sử dụng khi PaymentType = "ORDER"
        /// </summary>
        public int? OrderId { get; set; }

        // === PACKAGE Payment ===
        /// <summary>
        /// Sử dụng khi PaymentType = "PACKAGE"
        /// </summary>
        public int? PackageId { get; set; }

        /// <summary>
        /// Danh sách trẻ em áp dụng gói vaccine (cho PACKAGE)
        /// </summary>
        public List<int>? ChildIds { get; set; }

        // === INDIVIDUAL_VACCINE Payment ===
        /// <summary>
        /// Sử dụng khi PaymentType = "INDIVIDUAL_VACCINE"
        /// </summary>
        public List<int>? FacilityVaccineIds { get; set; }

        /// <summary>
        /// Ghi chú thêm
        /// </summary>
        public string? Note { get; set; }
    }
}

