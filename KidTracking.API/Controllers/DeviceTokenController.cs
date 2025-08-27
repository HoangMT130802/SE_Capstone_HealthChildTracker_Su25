using Contracts.DTOs.DeviceToken;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace KidTracking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DeviceTokenController : ControllerBase
    {
        private readonly IDeviceTokenService _deviceTokenService;

        public DeviceTokenController(IDeviceTokenService deviceTokenService)
        {
            _deviceTokenService = deviceTokenService ?? throw new ArgumentNullException(nameof(deviceTokenService));
        }

        /// <summary>
        /// Đăng ký device token cho push notifications
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> RegisterDeviceToken([FromBody] DeviceTokenCreateDto deviceTokenDto)
        {
            try
            {
                var accountId = GetCurrentAccountId();
                var result = await _deviceTokenService.RegisterDeviceTokenAsync(accountId, deviceTokenDto);
                
                return Ok(new { message = "Device token registered successfully", data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Failed to register device token", error = ex.Message });
            }
        }

        /// <summary>
        /// Xóa device token (khi logout hoặc uninstall app)
        /// </summary>
        [HttpDelete("remove")]
        public async Task<IActionResult> RemoveDeviceToken([FromQuery] string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                {
                    return BadRequest(new { message = "Token is required" });
                }

                var accountId = GetCurrentAccountId();
                var success = await _deviceTokenService.RemoveDeviceTokenAsync(accountId, token);
                
                if (success)
                {
                    return Ok(new { message = "Device token removed successfully" });
                }
                else
                {
                    return NotFound(new { message = "Device token not found" });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Failed to remove device token", error = ex.Message });
            }
        }

        /// <summary>
        /// Lấy danh sách device tokens của user hiện tại
        /// </summary>
        [HttpGet("my-tokens")]
        public async Task<IActionResult> GetMyDeviceTokens()
        {
            try
            {
                var accountId = GetCurrentAccountId();
                var tokens = await _deviceTokenService.GetUserDeviceTokensAsync(accountId);
                
                return Ok(new { data = tokens });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Failed to get device tokens", error = ex.Message });
            }
        }

        /// <summary>
        /// Cleanup inactive tokens (Admin only)
        /// </summary>
        [HttpPost("cleanup-inactive")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CleanupInactiveTokens([FromQuery] int daysInactive = 30)
        {
            try
            {
                var cleanedCount = await _deviceTokenService.CleanupInactiveTokensAsync(daysInactive);
                
                return Ok(new { message = $"Cleaned up {cleanedCount} inactive device tokens" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Failed to cleanup inactive tokens", error = ex.Message });
            }
        }

        private int GetCurrentAccountId()
        {
            var accountIdClaim = User.FindFirst("AccountId")?.Value;
            if (string.IsNullOrEmpty(accountIdClaim) || !int.TryParse(accountIdClaim, out int accountId))
            {
                throw new UnauthorizedAccessException("Invalid or missing account information");
            }
            return accountId;
        }
    }
}
