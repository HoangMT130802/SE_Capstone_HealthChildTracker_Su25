using System.ComponentModel.DataAnnotations;

namespace Contracts.DTOs.Transaction
{
    public class PaymentRequestDTO
    {
        [Required(ErrorMessage = "AccountId là bắt buộc")]
        public int AccountId { get; set; }
        
        public int? MembershipId { get; set; } // Cho UserMembership
        
        public int? FacilityMembershipId { get; set; } // Cho FacilityMembership
        public int? FacilityId { get; set; } // Cho FacilityMembership
        
        [Required(ErrorMessage = "TransactionType là bắt buộc")]
        [RegularExpression("^(UserMembership|FacilityMembership)$", ErrorMessage = "TransactionType phải là UserMembership hoặc FacilityMembership")]
        public string TransactionType { get; set; }
        
        public string? PaymentMethod { get; set; } = "PAYOS";
        public string? Description { get; set; }
    }
    
    public class PaymentResponseDTO
    {
        public string PaymentUrl { get; set; }
        public string OrderId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
    }
    
    public class PaymentStatusDTO
    {
        public bool Success { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public decimal Amount { get; set; }
        public DateTime? PaidAt { get; set; }
    }
} 