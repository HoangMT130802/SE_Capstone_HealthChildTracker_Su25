using System.ComponentModel.DataAnnotations;

namespace Contracts.DTOs.Authentication
{
    public class CreateManagerDTO
    {
        [Required(ErrorMessage = "Tên tài khoản không được để trống")]
        [StringLength(50, ErrorMessage = "Tên tài khoản không được vượt quá 50 ký tự")]
        public string AccountName { get; set; }

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Họ tên không được để trống")]
        [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự")]
        public string FullName { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string Phone { get; set; }

        public string Description { get; set; }
    }
} 