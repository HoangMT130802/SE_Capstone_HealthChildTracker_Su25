using Services;
using Services.Interfaces;
using System.Collections.Concurrent;

namespace Services.Implementations
{
    public class OtpCacheService : IOtpCacheService
    {
        private readonly ConcurrentDictionary<string, List<OtpInfo>> _otpCache = new();

        public async Task SaveOtpAsync(OtpInfo otpInfo)
        {
            var key = GetCacheKey(otpInfo.Email, otpInfo.Type);
            
            _otpCache.AddOrUpdate(key, 
                new List<OtpInfo> { otpInfo },
                (existingKey, existingList) =>
                {
                    // Xóa các OTP cũ chưa sử dụng
                    var newList = existingList.Where(x => x.IsUsed || x.ExpiresAt > DateTime.UtcNow).ToList();
                    newList.Add(otpInfo);
                    return newList;
                });

            await Task.CompletedTask;
        }

        public async Task<OtpInfo> GetOtpAsync(string email, string otpCode, string type)
        {
            var key = GetCacheKey(email, type);
            
            if (_otpCache.TryGetValue(key, out var otpList))
            {
                var otp = otpList.FirstOrDefault(x => 
                    x.OtpCode == otpCode && 
                    !x.IsUsed && 
                    x.ExpiresAt > DateTime.UtcNow);
                
                return await Task.FromResult(otp);
            }

            return await Task.FromResult<OtpInfo>(null);
        }

        public async Task<bool> VerifyAndConsumeOtpAsync(string email, string otpCode, string type)
        {
            var key = GetCacheKey(email, type);
            
            if (_otpCache.TryGetValue(key, out var otpList))
            {
                var otp = otpList.FirstOrDefault(x => 
                    x.OtpCode == otpCode && 
                    !x.IsUsed && 
                    x.ExpiresAt > DateTime.UtcNow);
                
                if (otp != null)
                {
                    otp.IsUsed = true;
                    return await Task.FromResult(true);
                }
            }

            return await Task.FromResult(false);
        }

        public async Task RemoveOtpAsync(string email, string type)
        {
            var key = GetCacheKey(email, type);
            _otpCache.TryRemove(key, out _);
            await Task.CompletedTask;
        }

        public async Task CleanupExpiredOtpAsync()
        {
            var keysToRemove = new List<string>();
            
            foreach (var kvp in _otpCache)
            {
                var validOtps = kvp.Value.Where(x => x.ExpiresAt > DateTime.UtcNow).ToList();
                
                if (validOtps.Count == 0)
                {
                    keysToRemove.Add(kvp.Key);
                }
                else if (validOtps.Count != kvp.Value.Count)
                {
                    _otpCache[kvp.Key] = validOtps;
                }
            }

            foreach (var key in keysToRemove)
            {
                _otpCache.TryRemove(key, out _);
            }

            await Task.CompletedTask;
        }

        private static string GetCacheKey(string email, string type)
        {
            return $"{email.ToLower()}:{type}";
        }
    }
}
