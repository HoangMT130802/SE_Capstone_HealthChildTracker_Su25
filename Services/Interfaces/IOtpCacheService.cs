using Services;

namespace Services.Interfaces
{
    public interface IOtpCacheService
    {
        Task SaveOtpAsync(OtpInfo otpInfo);
        Task<OtpInfo> GetOtpAsync(string email, string otpCode, string type);
        Task<bool> VerifyAndConsumeOtpAsync(string email, string otpCode, string type);
        Task RemoveOtpAsync(string email, string type);
        Task CleanupExpiredOtpAsync();
    }
}
