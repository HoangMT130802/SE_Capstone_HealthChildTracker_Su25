using Contracts.DTOs.DeviceToken;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IDeviceTokenService
    {
        /// <summary>
        /// Đăng ký hoặc cập nhật device token cho user
        /// </summary>
        Task<DeviceTokenResponseDto> RegisterDeviceTokenAsync(int accountId, DeviceTokenCreateDto deviceTokenDto);

        /// <summary>
        /// Xóa device token (khi user logout hoặc uninstall app)
        /// </summary>
        Task<bool> RemoveDeviceTokenAsync(int accountId, string token);

        /// <summary>
        /// Lấy tất cả active device tokens của user
        /// </summary>
        Task<List<DeviceTokenResponseDto>> GetUserDeviceTokensAsync(int accountId);

        /// <summary>
        /// Lấy tất cả active device tokens của user (chỉ token strings)
        /// </summary>
        Task<List<string>> GetUserActiveTokensAsync(int accountId);

        /// <summary>
        /// Vô hiệu hóa device token (khi token invalid hoặc expired)
        /// </summary>
        Task<bool> DeactivateDeviceTokenAsync(string token);

        /// <summary>
        /// Cleanup các tokens không hoạt động lâu
        /// </summary>
        Task<int> CleanupInactiveTokensAsync(int daysInactive = 30);

        /// <summary>
        /// Cập nhật last used time cho device token
        /// </summary>
        Task UpdateTokenLastUsedAsync(string token);
    }
}
