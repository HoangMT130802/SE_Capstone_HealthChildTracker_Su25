using System.Threading.Tasks;
using Services;

namespace Services.Interfaces
{
    public interface IEmailService
    {
        Task SendVerificationEmailAsync(string email, string otpCode, string fullName);
        Task SendForgotPasswordEmailAsync(string email, string otpCode, string fullName);
        Task<string> GenerateOtpCodeAsync();
        Task<bool> SaveEmailVerificationAsync(string email, string otpCode, string type, int? accountId = null);
        Task<bool> SaveRegistrationDataAsync(string email, string otpCode, string accountName, string password, string fullName, string phone, string address);
        Task<OtpInfo> GetRegistrationDataAsync(string email, string otpCode);
        Task<bool> VerifyOtpCodeAsync(string email, string otpCode, string type);
        Task CleanupExpiredOtpAsync();
    }
}
