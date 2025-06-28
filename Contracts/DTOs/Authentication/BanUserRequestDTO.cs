using System.ComponentModel.DataAnnotations;

namespace Contracts.DTOs.Authentication
{
    public class BanUserRequestDTO
    {
        [Required(ErrorMessage = "ID tài khoản là bắt buộc")]
        public int AccountId { get; set; }

        [Required(ErrorMessage = "Trạng thái là bắt buộc")]
        public bool Status { get; set; }

        [StringLength(200, ErrorMessage = "Lý do không được vượt quá 200 ký tự")]
        public string? Reason { get; set; }
    }
} 