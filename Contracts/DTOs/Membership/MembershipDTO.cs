namespace Contracts.DTOs.Membership
{
    public class MembershipDTO
    {
        public int MembershipId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Duration { get; set; } // Thời hạn (tháng)
        public decimal Price { get; set; }
        public string Benefits { get; set; }
        public bool Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
} 