using Contracts.DTOs.UserMembership;
using Contracts.DTOs.FacilitySubcription;

namespace Contracts.DTOs.Transaction
{
    public class TransactionDTO
    {
        public int TransactionId { get; set; }
        public int? FacilityMembershipSubscriptionId { get; set; }
        public int? UserMembershipId { get; set; }
        public string TransactionType { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; }
        public string TransactionCode { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public DateOnly CreatedAt { get; set; }
        
        // Navigation properties
        public UserMembershipDTO? UserMembership { get; set; }
        public FacilityMembershipSubscriptionDTO? FacilityMembershipSubscription { get; set; }
    }
} 