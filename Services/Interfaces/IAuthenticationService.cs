using BusinessLogic.DTOs.Authentication;

namespace BusinessLogic.Services.Interfaces
{
    public interface IAuthenticationService
    {
        Task<UserResponseDTO> LoginAsync(LoginRequestDTO request);
        Task<UserResponseDTO> RegisterAsync(RegisterRequestDTO request);
    }
}
