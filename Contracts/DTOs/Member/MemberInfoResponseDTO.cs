namespace Contracts.DTOs.Member
{
    public class MemberInfoResponseDTO
    {
        public int MemberId { get; set; }
        public int AccountId { get; set; }
        public string AccountName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool Status { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
    }
}