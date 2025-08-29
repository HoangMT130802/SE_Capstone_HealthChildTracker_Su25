using Contracts.DTOs.NotificationHistory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using System.Security.Claims;

namespace KidTracking.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationHistoryController : ControllerBase
{
    private readonly INotificationHistoryService _notificationHistoryService;
    private readonly ILogger<NotificationHistoryController> _logger;

    public NotificationHistoryController(INotificationHistoryService notificationHistoryService,
        ILogger<NotificationHistoryController> logger)
    {
        _notificationHistoryService = notificationHistoryService;
        _logger = logger;
    }

    /// <summary>
    /// Lấy lịch sử thông báo của user hiện tại
    /// </summary>
    [HttpGet("my-notifications")]
    public async Task<ActionResult<NotificationHistoryListDto>> GetMyNotifications(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? notificationType = null)
    {
        try
        {
            var accountId = GetCurrentAccountId();
            if (accountId == null)
            {
                return Unauthorized("Không tìm thấy thông tin tài khoản");
            }

            var result = await _notificationHistoryService.GetUserNotificationHistoryAsync(
                accountId.Value, pageNumber, pageSize, notificationType);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification history for account {AccountId}", GetCurrentAccountId());
            return StatusCode(500, "Đã xảy ra lỗi khi lấy lịch sử thông báo");
        }
    }

    /// <summary>
    /// Lấy chi tiết một thông báo
    /// </summary>
    [HttpGet("{notificationId}")]
    public async Task<ActionResult<NotificationHistoryResponseDto>> GetNotificationDetail(int notificationId)
    {
        try
        {
            var accountId = GetCurrentAccountId();
            if (accountId == null)
            {
                return Unauthorized("Không tìm thấy thông tin tài khoản");
            }

            var result = await _notificationHistoryService.GetNotificationDetailAsync(notificationId, accountId.Value);

            if (result == null)
            {
                return NotFound("Không tìm thấy thông báo");
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification detail {NotificationId} for account {AccountId}", 
                notificationId, GetCurrentAccountId());
            return StatusCode(500, "Đã xảy ra lỗi khi lấy chi tiết thông báo");
        }
    }

    /// <summary>
    /// Lấy thống kê delivery rate của user
    /// </summary>
    [HttpGet("delivery-stats")]
    public async Task<ActionResult<Dictionary<string, object>>> GetDeliveryStats(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var accountId = GetCurrentAccountId();
            if (accountId == null)
            {
                return Unauthorized("Không tìm thấy thông tin tài khoản");
            }

            var result = await _notificationHistoryService.GetDeliveryStatsAsync(accountId.Value, fromDate, toDate);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting delivery stats for account {AccountId}", GetCurrentAccountId());
            return StatusCode(500, "Đã xảy ra lỗi khi lấy thống kê delivery");
        }
    }

    /// <summary>
    /// Cập nhật trạng thái delivery (webhook từ Firebase hoặc mobile app)
    /// </summary>
    [HttpPost("delivery-status/{deliveryStatusId}")]
    public async Task<ActionResult> UpdateDeliveryStatus(int deliveryStatusId, [FromBody] UpdateDeliveryStatusRequest request)
    {
        try
        {
            await _notificationHistoryService.UpdateDeliveryStatusAsync(
                deliveryStatusId, 
                request.Status, 
                request.DeliveredAt, 
                request.ClickedAt);

            _logger.LogInformation("Updated delivery status {DeliveryStatusId} to {Status}", deliveryStatusId, request.Status);

            return Ok(new { message = "Cập nhật trạng thái thành công" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating delivery status {DeliveryStatusId}", deliveryStatusId);
            return StatusCode(500, "Đã xảy ra lỗi khi cập nhật trạng thái");
        }
    }

    /// <summary>
    /// Admin: Cleanup old notifications
    /// </summary>
    [HttpPost("admin/cleanup")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<ActionResult> CleanupOldNotifications([FromQuery] int daysToKeep = 90)
    {
        try
        {
            await _notificationHistoryService.CleanupOldNotificationsAsync(daysToKeep);

            _logger.LogInformation("Cleaned up old notifications older than {Days} days", daysToKeep);

            return Ok(new { message = $"Đã dọn dẹp các thông báo cũ hơn {daysToKeep} ngày" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old notifications");
            return StatusCode(500, "Đã xảy ra lỗi khi dọn dẹp thông báo cũ");
        }
    }

    private int? GetCurrentAccountId()
    {
        var accountIdClaim = User.FindFirst("AccountId")?.Value;
        return int.TryParse(accountIdClaim, out var accountId) ? accountId : null;
    }

    // Request DTOs
    public class UpdateDeliveryStatusRequest
    {
        public string Status { get; set; } // Delivered, Clicked, Dismissed
        public DateTime? DeliveredAt { get; set; }
        public DateTime? ClickedAt { get; set; }
    }
}
