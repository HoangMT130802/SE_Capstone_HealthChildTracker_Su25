using System.ComponentModel.DataAnnotations;

namespace Contracts.DTOs.Authentication
{
    public class ResetPasswordRequestDTO
    {
        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Mã OTP không được để trống")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Mã OTP phải có 6 ký tự")]
        public string OtpCode { get; set; }

        [Required(ErrorMessage = "Mật khẩu mới không được để trống")]
        [MinLength(6, ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự")]
        public string NewPassword { get; set; }
    }
}
