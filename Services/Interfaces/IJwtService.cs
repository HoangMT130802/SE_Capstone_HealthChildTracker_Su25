using System.Security.Claims;
using Repositories.Entities;

namespace Services.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(Account account);
        string GenerateToken(Account account, int? facilityId); // ✅ Overload cho Staff/Manager
        ClaimsPrincipal? ValidateToken(string token);
    }
} 