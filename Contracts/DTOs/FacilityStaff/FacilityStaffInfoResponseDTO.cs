namespace Contracts.DTOs.FacilityStaff
{
    public class FacilityStaffInfoResponseDTO
    {
        public int StaffId { get; set; }
        public int AccountId { get; set; }
        public string AccountName { get; set; } = string.Empty;
        public int FacilityId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Role { get; set; } = string.Empty;
        public bool Status { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
} 