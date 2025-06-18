using BusinessLogic.DTOs.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Services.Interfaces
{
    public interface IUserService
    {
        Task<List<UserDTO>> GetAllUsersAsync();
        Task<UserDTO> GetCurrentUserDetailAsync(int currentUserId);
        Task<UserDTO> UpdateUserProfileAsync(int userId, UpdateUserProfileDTO updateUserDTO);
        Task<bool> UpdateUserStatusAsync(int userId, bool status);
    }
}
