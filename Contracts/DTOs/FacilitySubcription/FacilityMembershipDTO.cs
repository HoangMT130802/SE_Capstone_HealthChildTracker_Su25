namespace Contracts.DTOs.FacilitySubcription
{
    public class FacilityMembershipDTO
    {
        public int FacilityMembershipId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Duration { get; set; } // Thời hạn (tháng)
        public decimal Price { get; set; }
        public string Benefits { get; set; }
        public bool Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateFacilityMembershipDTO
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int Duration { get; set; }
        public decimal Price { get; set; }
        public string Benefits { get; set; }
        public bool Status { get; set; } = true;
    }

    public class UpdateFacilityMembershipDTO
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int Duration { get; set; }
        public decimal Price { get; set; }
        public string Benefits { get; set; }
        public bool Status { get; set; }
    }
} 