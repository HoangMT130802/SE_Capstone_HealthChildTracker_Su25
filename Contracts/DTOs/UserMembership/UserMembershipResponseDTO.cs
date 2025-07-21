namespace Contracts.DTOs.UserMembership
{
    public class UserMembershipResponseDTO
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public UserMembershipDTO UserMembership { get; set; }
        
        // Thông tin Member được tạo mới (nếu upgrade từ Guest)
        public bool WasUpgradedFromGuest { get; set; }
        public int? MemberId { get; set; }
        public string MemberFullName { get; set; }
    }
} 