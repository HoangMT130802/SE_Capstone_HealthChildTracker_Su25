using System.Security.Claims;
using DataAccess.Entities;

namespace BusinessLogic.Services.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(User user);
        ClaimsPrincipal ValidateToken(string token);
    }
} 