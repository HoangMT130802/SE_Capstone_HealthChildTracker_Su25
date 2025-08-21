using System.ComponentModel.DataAnnotations;

namespace Contracts.DTOs.Authentication
{
    public class ResendVerificationRequestDTO
    {
        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }
    }
}
