using Contracts.DTOs.Authentication;

namespace Services.Interfaces
{
    public interface IAuthenticationService
    {
        Task<UserResponseDTO> LoginAsync(LoginRequestDTO request);
        Task<UserResponseDTO> RegisterAsync(RegisterRequestDTO request);
    }
}
