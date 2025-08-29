using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace KidTracking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PushNotificationController : ControllerBase
    {
        private readonly IPushNotificationService _pushNotificationService;
        private readonly IDeviceTokenService _deviceTokenService;

        public PushNotificationController(
            IPushNotificationService pushNotificationService,
            IDeviceTokenService deviceTokenService)
        {
            _pushNotificationService = pushNotificationService ?? throw new ArgumentNullException(nameof(pushNotificationService));
            _deviceTokenService = deviceTokenService ?? throw new ArgumentNullException(nameof(deviceTokenService));
        }

        /// <summary>
        /// Test gửi push notification (Admin only)
        /// </summary>
        [HttpPost("test")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> TestPushNotification([FromBody] TestPushNotificationRequest request)
        {
            try
            {
                await _pushNotificationService.SendCustomPushAsync(
                    request.DeviceToken,
                    request.Title,
                    request.Body,
                    request.Data
                );

                return Ok(new { message = "Test push notification sent successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Failed to send test push notification", error = ex.Message });
            }
        }

        /// <summary>
        /// Test gửi vaccine reminder push notification (Admin only)
        /// </summary>
        [HttpPost("test-vaccine-reminder")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> TestVaccineReminderPush([FromBody] TestVaccineReminderRequest request)
        {
            try
            {
                await _pushNotificationService.SendVaccineReminderPushAsync(
                    request.DeviceToken,
                    request.ChildName,
                    request.VaccineName,
                    request.DoseNumber,
                    request.ExpectedDate,
                    request.FacilityName
                );

                return Ok(new { message = "Test vaccine reminder push sent successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Failed to send test vaccine reminder push", error = ex.Message });
            }
        }

        /// <summary>
        /// Gửi push notification cho tất cả device của user
        /// </summary>
        [HttpPost("send-to-user")]
        public async Task<IActionResult> SendPushToUser([FromBody] SendPushToUserRequest request)
        {
            try
            {
                var accountId = GetCurrentAccountId();
                var deviceTokens = await _deviceTokenService.GetUserActiveTokensAsync(accountId);

                if (deviceTokens.Count == 0)
                {
                    return BadRequest(new { message = "No active device tokens found for user" });
                }

                await _pushNotificationService.SendMulticastPushAsync(
                    deviceTokens,
                    request.Title,
                    request.Body,
                    request.Data
                );

                return Ok(new { 
                    message = "Push notification sent to all user devices", 
                    deviceCount = deviceTokens.Count 
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Failed to send push notification", error = ex.Message });
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

    public class TestPushNotificationRequest
    {
        public string DeviceToken { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public Dictionary<string, string> Data { get; set; }
    }

    public class TestVaccineReminderRequest
    {
        public string DeviceToken { get; set; }
        public string ChildName { get; set; }
        public string VaccineName { get; set; }
        public int DoseNumber { get; set; }
        public string ExpectedDate { get; set; }
        public string FacilityName { get; set; }
    }

    public class SendPushToUserRequest
    {
        public string Title { get; set; }
        public string Body { get; set; }
        public Dictionary<string, string> Data { get; set; }
    }
}
