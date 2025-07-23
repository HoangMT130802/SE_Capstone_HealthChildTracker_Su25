namespace Contracts.DTOs.UserMembership
{
    public class UserMembershipDTO
    {
        public int UserMembershipId { get; set; }
        public int AccountId { get; set; }
        public int MembershipId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool Status { get; set; }
        public long RemainingConsultations { get; set; }
        public DateOnly LastRenewalDate { get; set; }
        
        // Navigation properties
        public string AccountName { get; set; }
        public string MembershipName { get; set; }
        public string MembershipDescription { get; set; }
        public decimal MembershipPrice { get; set; }
        public string MembershipBenefits { get; set; }
        
        // Computed properties
        public bool IsActive => Status && DateTime.Now >= StartDate && DateTime.Now <= EndDate;
        public int DaysRemaining => IsActive ? (int)(EndDate - DateTime.Now).TotalDays : 0;
    }
} 