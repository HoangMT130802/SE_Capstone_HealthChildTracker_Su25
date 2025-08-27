using Contracts.DTOs.Account;
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
    }
}
