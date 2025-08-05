using System.ComponentModel.DataAnnotations;

namespace Contracts.DTOs.Transaction
{
    public class PaymentRequestDTO
    {
        [Required(ErrorMessage = "AccountId là bắt buộc")]
        public int AccountId { get; set; }
        
        /// <summary>
        /// ID gói membership cho người dùng cá nhân (Member)
        /// </summary>
        public int? MembershipId { get; set; }
        
        /// <summary>
        /// ID gói membership cho cơ sở (FacilityStaff)
        /// </summary>
        public int? FacilityMembershipId { get; set; }
    }
    
    public class PaymentResponseDTO
    {
        public required string PaymentUrl { get; set; }
        public required string OrderId { get; set; }
        public decimal Amount { get; set; }
        public required string Status { get; set; }
        public required string ReturnUrl { get; set; }
        public required string CancelUrl { get; set; }
    }
    
    public class PaymentStatusDTO
    {
        public bool Success { get; set; }
        public required string Status { get; set; }
        public required string Message { get; set; }
        public decimal Amount { get; set; }
        public DateTime? PaidAt { get; set; }
    }
} 