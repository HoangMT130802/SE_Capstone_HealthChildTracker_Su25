namespace Contracts.DTOs.UserMembership
{
    public class UserMembershipResponseDTO
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public UserMembershipDTO? UserMembership { get; set; }
    }
} 