namespace Contracts.DTOs.FacilitySubcription
{
    public class FacilityMembershipSubscriptionDTO
    {
        public int SubscriptionId { get; set; }
        public int FacilityId { get; set; }
        public int FacilityMembershipId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Navigation properties
        public string FacilityName { get; set; }
        public string FacilityMembershipName { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        
        // Computed properties
        public bool IsActive => Status && DateTime.Now >= StartDate && DateTime.Now <= EndDate;
        public int DaysRemaining => IsActive ? (int)(EndDate - DateTime.Now).TotalDays : 0;
    }
} 