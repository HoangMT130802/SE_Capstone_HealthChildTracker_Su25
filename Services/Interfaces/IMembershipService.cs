using BusinessLogic.DTOs.Membership;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Services.Interfaces
{
    public interface IMembershipService
    {
        Task<IEnumerable<MembershipDTO>> GetAllMembershipsAsync();
        Task<MembershipDTO> GetMembershipByIdAsync(int membershipId);
        Task<MembershipDTO> UpdateMembershipPriceAsync(int membershipId, decimal newPrice);
        Task<MembershipDTO> UpdateMembershipStatusAsync(int membershipId, bool status);
    }
}
