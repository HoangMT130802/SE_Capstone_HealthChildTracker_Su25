using Contracts.DTOs.Authentication;
using Contracts.DTOs.Member;
using Contracts.DTOs.FacilityStaff;
using Repositories.Models.QueryModels;
using System.Collections.Generic;

namespace Services.Interfaces
{
    public interface IAuthenticationService
    {
        Task<UserResponseDTO> LoginAsync(LoginRequestDTO request);
        Task<UserResponseDTO> RegisterAsync(RegisterRequestDTO request);
        Task<StaffResponseDTO> CreateManagerAsync(CreateManagerDTO request, int adminAccountId);
        Task<StaffResponseDTO> CreateStaffAsync(CreateStaffDTO request, int managerAccountId);
        Task<MemberInfoResponseDTO> UpdateMemberInfoAsync(UpdateMemberInfoDTO request, int currentUserId);
        Task<FacilityStaffInfoResponseDTO> UpdateFacilityStaffInfoAsync(UpdateFacilityStaffInfoDTO request, int currentUserId);
        Task<UserResponseDTO> BanUserAsync(BanUserRequestDTO request, int currentUserId);
        Task<bool> DeleteStaffAsync(int staffId, int managerAccountId);
        Task<QueryResultModel<List<MemberDTO>>> GetAllMembersAsync(int currentUserId, int pageIndex = 1, int pageSize = 10);
    }
}
