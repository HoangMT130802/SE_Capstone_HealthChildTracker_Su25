namespace Contracts.DTOs.Authentication
{
    public class StaffResponseDTO
    {
        public int AccountId { get; set; }
        public int StaffId { get; set; }
        public string AccountName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
        public int FacilityId { get; set; }
        public string Position { get; set; }
        public string Description { get; set; }
        public bool Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Token { get; set; }
    }
} 