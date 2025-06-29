using System.ComponentModel.DataAnnotations;

namespace Contracts.DTOs.VaccinationFacility
{
    public class CreateVaccinationFacilityDTO
    {
        [Required(ErrorMessage = "Tên cơ sở là bắt buộc")]
        [StringLength(200, ErrorMessage = "Tên cơ sở không được vượt quá 200 ký tự")]
        public string FacilityName { get; set; }

        [Required(ErrorMessage = "Số giấy phép là bắt buộc")]
        public int LicenseNumber { get; set; }

        [Required(ErrorMessage = "Địa chỉ là bắt buộc")]
        [StringLength(500, ErrorMessage = "Địa chỉ không được vượt quá 500 ký tự")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        public int Phone { get; set; }

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự")]
        public string Email { get; set; }

        [StringLength(1000, ErrorMessage = "Mô tả không được vượt quá 1000 ký tự")]
        public string Description { get; set; }

        public long Status { get; set; } = 1; // Active by default
    }
} 