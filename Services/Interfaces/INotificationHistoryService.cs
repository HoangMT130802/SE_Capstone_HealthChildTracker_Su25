using Contracts.DTOs.NotificationHistory;

namespace Services.Interfaces;

public interface INotificationHistoryService
{
    /// <summary>
    /// Lưu lịch sử thông báo đã gửi
    /// </summary>
    Task<int> SaveNotificationHistoryAsync(int accountId, string notificationType, string title, string body, 
        string? data = null, int? childId = null, int? vaccineId = null, int? appointmentId = null);

    /// <summary>
    /// Lưu trạng thái delivery cho từng device
    /// </summary>
    Task SaveDeliveryStatusAsync(int notificationHistoryId, int deviceTokenId, string status, 
        string? firebaseMessageId = null, string? errorMessage = null);

    /// <summary>
    /// Cập nhật trạng thái delivery (delivered, clicked, etc.)
    /// </summary>
    Task UpdateDeliveryStatusAsync(int deliveryStatusId, string status, DateTime? deliveredAt = null, DateTime? clickedAt = null);

    /// <summary>
    /// Lấy lịch sử thông báo của user với phân trang
    /// </summary>
    Task<NotificationHistoryListDto> GetUserNotificationHistoryAsync(int accountId, int pageNumber = 1, int pageSize = 20, 
        string? notificationType = null);

    /// <summary>
    /// Lấy chi tiết một thông báo
    /// </summary>
    Task<NotificationHistoryResponseDto?> GetNotificationDetailAsync(int notificationHistoryId, int accountId);

    /// <summary>
    /// Xóa các thông báo cũ (cleanup job)
    /// </summary>
    Task CleanupOldNotificationsAsync(int daysToKeep = 90);

    /// <summary>
    /// Lấy thống kê delivery rate
    /// </summary>
    Task<Dictionary<string, object>> GetDeliveryStatsAsync(int accountId, DateTime? fromDate = null, DateTime? toDate = null);
}

