using BusinessLogic.DTOs.Children;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Services.Interfaces
{
    public interface IChildService
    {
        Task<IEnumerable<ChildDTO>> GetAllChildrenByUserIdAsync(int userId);
        Task<ChildDTO> GetChildByIdAsync(int childId, int userId);
        Task<ChildDTO> CreateChildAsync(int userId, CreateChildDTO childDTO);
        Task<ChildDTO> UpdateChildAsync(int childId, int userId, UpdateChildDTO childDTO);
        Task<bool> SoftDeleteChildAsync(int childId, int userId);
        Task<bool> HardDeleteChildAsync(int childId, int userId);
    }
}
