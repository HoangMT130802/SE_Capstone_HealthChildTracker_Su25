namespace Contracts.DTOs.Authentication
{
    public class RegisterPendingResponseDTO
    {
        public string Message { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool RequiresVerification { get; set; } = true;
    }
}

