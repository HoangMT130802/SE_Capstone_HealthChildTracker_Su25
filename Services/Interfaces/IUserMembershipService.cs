using Contracts.DTOs.UserMembership;
using Repositories.Models.QueryModels;

namespace Services.Interfaces
{
    public interface IUserMembershipService
    {
        Task<UserMembershipResponseDTO> SubscribeMembershipAsync(int accountId, SubscribeMembershipDTO subscribeDto);
        Task<UserMembershipResponseDTO> GuestSubscribeMembershipAsync(int accountId, GuestSubscribeMembershipDTO subscribeDto);
        Task<List<UserMembershipDTO>> GetUserMembershipsAsync(int accountId);
        Task<UserMembershipDTO> GetActiveUserMembershipAsync(int accountId);
        Task<bool> CancelUserMembershipAsync(int userMembershipId, int accountId);
        Task<bool> RenewUserMembershipAsync(int userMembershipId, int accountId);
        Task<QueryResultModel<List<UserMembershipDTO>>> GetAllUserMembershipsAsync(int pageIndex = 1, int pageSize = 10, bool? status = null, int? membershipId = null);
        Task<int> GetActiveCountAsync();
    }
} 