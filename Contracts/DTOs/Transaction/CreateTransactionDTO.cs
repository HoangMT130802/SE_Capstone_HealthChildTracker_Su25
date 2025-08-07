using System.ComponentModel.DataAnnotations;

namespace Contracts.DTOs.Transaction
{
    public class CreateTransactionDTO
    {
        public int? FacilityMembershipSubscriptionId { get; set; }
        public int? UserMembershipId { get; set; }
        
        [Required(ErrorMessage = "TransactionType là bắt buộc")]
        public string TransactionType { get; set; }
        
        [Required(ErrorMessage = "Amount là bắt buộc")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount phải lớn hơn 0")]
        public decimal Amount { get; set; }
        
        [Required(ErrorMessage = "PaymentMethod là bắt buộc")]
        public string PaymentMethod { get; set; }
        
        [Required(ErrorMessage = "TransactionCode là bắt buộc")]
        public string TransactionCode { get; set; }
        
        public string? Description { get; set; }
        
        [Required(ErrorMessage = "Status là bắt buộc")]
        public string Status { get; set; }
    }
} 