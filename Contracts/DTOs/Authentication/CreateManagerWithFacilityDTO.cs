using System.ComponentModel.DataAnnotations;

namespace Contracts.DTOs.Authentication
{
    public class CreateManagerWithFacilityDTO
    {
        // Manager Information
        [Required(ErrorMessage = "Tên tài khoản không được để trống")]
        [StringLength(50, ErrorMessage = "Tên tài khoản không được vượt quá 50 ký tự")]
        public string AccountName { get; set; }

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Email Manager không được để trống")]
        [EmailAddress(ErrorMessage = "Email Manager không hợp lệ")]
        public string ManagerEmail { get; set; }

        [Required(ErrorMessage = "Họ tên Manager không được để trống")]
        [StringLength(100, ErrorMessage = "Họ tên Manager không được vượt quá 100 ký tự")]
        public string ManagerFullName { get; set; }

        [Phone(ErrorMessage = "Số điện thoại Manager không hợp lệ")]
        public string ManagerPhone { get; set; }

        public string ManagerDescription { get; set; }

        // Facility Information
        [Required(ErrorMessage = "Tên cơ sở là bắt buộc")]
        [StringLength(200, ErrorMessage = "Tên cơ sở không được vượt quá 200 ký tự")]
        public string FacilityName { get; set; }

        [Required(ErrorMessage = "Số giấy phép là bắt buộc")]
        [Range(1, 999999999, ErrorMessage = "Số giấy phép phải từ 1 đến 999,999,999")]
        public int LicenseNumber { get; set; }

        [Required(ErrorMessage = "Địa chỉ cơ sở là bắt buộc")]
        [StringLength(500, ErrorMessage = "Địa chỉ cơ sở không được vượt quá 500 ký tự")]
        public string FacilityAddress { get; set; }

        [Required(ErrorMessage = "Số điện thoại cơ sở là bắt buộc")]
        [Range(100000000, 999999999, ErrorMessage = "Số điện thoại cơ sở phải có  chữ số (100,000,000 - 999,999,999)")]
        public int FacilityPhone { get; set; }

        [Required(ErrorMessage = "Email cơ sở là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email cơ sở không hợp lệ")]
        [StringLength(100, ErrorMessage = "Email cơ sở không được vượt quá 100 ký tự")]
        public string FacilityEmail { get; set; }

        [StringLength(1000, ErrorMessage = "Mô tả cơ sở không được vượt quá 1000 ký tự")]
        public string FacilityDescription { get; set; }
    }
}

