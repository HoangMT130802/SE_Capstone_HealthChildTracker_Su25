using BusinessLogic.DTOs.UserMembership;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Services.Interfaces
{
    public interface IUserMembershipService
{
    Task<IEnumerable<UserMembershipDTO>> GetAllUserMembershipsAsync();
    Task<UserMembershipDTO> GetUserMembershipByIdAsync(int userMembershipId);
    Task<UserMembershipDTO> GetActiveUserMembershipAsync(int userId);
    Task<UserMembershipDTO> CreateUserMembershipAsync(CreateUserMembershipDTO dto);
    Task<bool> UpdateMembershipStatusAsync(int userMembershipId, string status);
    Task<bool> DecrementConsultationCountAsync(int userMembershipId);
    Task<bool> RenewMembershipAsync(int userMembershipId);
    }
}
