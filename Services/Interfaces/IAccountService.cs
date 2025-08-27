using Contracts.DTOs.Account;
using Contracts.DTOs.Member;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IAccountService
    {
        Task<AccountDTO> UpdateAccountAsync(UpdateAccountDTO request, int currentUserId);
        Task<AccountDTO>GetCurrentAccountAsync(int currentUserId);
        Task<MemberInfoResponseDTO> UpdateMemberInfoAsync(UpdateMemberInfoDTO request, int currentUserId);
    }
}
