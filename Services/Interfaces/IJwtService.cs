using System.Security.Claims;
using Repositories.Entities;

namespace Services.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(Account account);
        ClaimsPrincipal? ValidateToken(string token);
    }
} 