using Contracts.DTOs.Child;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IChildService
    {
        Task<IEnumerable<ChildDTO>> GetAllChildrenByAccountIdAsync(int accountId);
        Task<ChildDTO> GetChildByIdAsync(int childId, int accountId);
        Task<ChildDTO> CreateChildAsync(int accountId, CreateChildDTO childDTO);
        Task<ChildDTO> UpdateChildAsync(int childId, int accountId, UpdateChildDTO childDTO);
        Task<bool> SoftDeleteChildAsync(int childId, int accountId);
        Task<bool> HardDeleteChildAsync(int childId, int accountId);
    }
}
