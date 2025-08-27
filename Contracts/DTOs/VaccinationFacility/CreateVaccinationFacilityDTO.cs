using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Contracts.DTOs.VaccinationFacility
{
    public class CreateVaccinationFacilityDTO
    {
        [Required(ErrorMessage = "Tên cơ sở là bắt buộc")]
        [StringLength(200, ErrorMessage = "Tên cơ sở không được vượt quá 200 ký tự")]
        public string FacilityName { get; set; }

        [Required(ErrorMessage = "Số giấy phép là bắt buộc")]
        [Range(1, 999999999, ErrorMessage = "Số giấy phép phải từ 1 đến 999,999,999")]
        public int LicenseNumber { get; set; }

        [Required(ErrorMessage = "Địa chỉ là bắt buộc")]
        [StringLength(500, ErrorMessage = "Địa chỉ không được vượt quá 500 ký tự")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        [Range(100000000, 999999999, ErrorMessage = "Số điện thoại phải có 9 chữ số (100,000,000 - 999,999,999)")]
        public int Phone { get; set; }

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự")]
        public string Email { get; set; }

        [StringLength(1000, ErrorMessage = "Mô tả không được vượt quá 1000 ký tự")]
        public string Description { get; set; }
        [Required(ErrorMessage = "File giấy phép là bắt buộc")]
        public IFormFile LicenseFile { get; set; }

        // Status sẽ được set tự động trong service = 1 (Active)
    }
} 