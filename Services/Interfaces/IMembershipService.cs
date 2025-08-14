using Contracts.DTOs.Membership;
using Repositories.Models.QueryModels;

namespace Services.Interfaces
{
    public interface IMembershipService
    {
        Task<QueryResultModel<List<MembershipDTO>>> GetAllMembershipsAsync(int pageIndex = 1, int pageSize = 10, bool? status = null);
        Task<MembershipDTO> GetMembershipByIdAsync(int membershipId);
        Task<MembershipDTO> CreateMembershipAsync(CreateMembershipDTO createDto);
        Task<MembershipDTO> UpdateMembershipAsync(int membershipId, UpdateMembershipDTO updateDto);
        Task<bool> DeleteMembershipAsync(int membershipId);
        Task<bool> ToggleMembershipStatusAsync(int membershipId);
        Task<List<MembershipDTO>> GetActiveMembershipsAsync();
        Task<int> GetTotalCountAsync();
    }
} 