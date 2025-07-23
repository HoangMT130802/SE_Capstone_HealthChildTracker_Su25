using System.ComponentModel.DataAnnotations;

namespace Contracts.DTOs.UserMembership
{
    public class SubscribeMembershipDTO
    {
        [Required(ErrorMessage = "MembershipId không được để trống")]
        public int MembershipId { get; set; }
    }
} 