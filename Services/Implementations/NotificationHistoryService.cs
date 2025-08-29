using AutoMapper;
using Contracts.DTOs.NotificationHistory;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repositories.Common;
using Repositories.Entities;
using Repositories.Interfaces;
using Services.Interfaces;

namespace Services.Implementations;

public class NotificationHistoryService : INotificationHistoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<NotificationHistoryService> _logger;

    public NotificationHistoryService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<NotificationHistoryService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<int> SaveNotificationHistoryAsync(int accountId, string notificationType, string title, string body, 
        string? data = null, int? childId = null, int? vaccineId = null, int? appointmentId = null)
    {
        try
        {
            var notificationRepo = _unitOfWork.GetRepository<NotificationHistory>();

            var notification = new NotificationHistory
            {
                AccountId = accountId,
                NotificationType = notificationType,
                Title = title,
                Body = body,
                Data = data,
                ChildId = childId,
                VaccineId = vaccineId,
                AppointmentId = appointmentId,
                Status = "Pending",
                SentAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await notificationRepo.AddAsync(notification);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Saved notification history {NotificationId} for account {AccountId}, type: {Type}", 
                notification.NotificationHistoryId, accountId, notificationType);

            return notification.NotificationHistoryId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving notification history for account {AccountId}", accountId);
            throw;
        }
    }

    public async Task SaveDeliveryStatusAsync(int notificationHistoryId, int deviceTokenId, string status, 
        string? firebaseMessageId = null, string? errorMessage = null)
    {
        try
        {
            var deliveryRepo = _unitOfWork.GetRepository<NotificationDeliveryStatus>();

            var deliveryStatus = new NotificationDeliveryStatus
            {
                NotificationHistoryId = notificationHistoryId,
                DeviceTokenId = deviceTokenId,
                Status = status,
                FirebaseMessageId = firebaseMessageId,
                ErrorMessage = errorMessage,
                SentAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await deliveryRepo.AddAsync(deliveryStatus);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Saved delivery status for notification {NotificationId}, device {DeviceTokenId}, status: {Status}", 
                notificationHistoryId, deviceTokenId, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving delivery status for notification {NotificationId}", notificationHistoryId);
            throw;
        }
    }

    public async Task UpdateDeliveryStatusAsync(int deliveryStatusId, string status, DateTime? deliveredAt = null, DateTime? clickedAt = null)
    {
        try
        {
            var deliveryRepo = _unitOfWork.GetRepository<NotificationDeliveryStatus>();
            var deliveryStatus = await deliveryRepo.GetByIdAsync(deliveryStatusId);

            if (deliveryStatus != null)
            {
                deliveryStatus.Status = status;
                deliveryStatus.DeliveredAt = deliveredAt;
                deliveryStatus.ClickedAt = clickedAt;
                deliveryStatus.UpdatedAt = DateTime.UtcNow;

                deliveryRepo.Update(deliveryStatus);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Updated delivery status {DeliveryStatusId} to {Status}", deliveryStatusId, status);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating delivery status {DeliveryStatusId}", deliveryStatusId);
            throw;
        }
    }

    public async Task<NotificationHistoryListDto> GetUserNotificationHistoryAsync(int accountId, int pageNumber = 1, int pageSize = 20, 
        string? notificationType = null)
    {
        try
        {
            var notificationRepo = _unitOfWork.GetRepository<NotificationHistory>();

            var baseQuery = notificationRepo.GetAllQueryable()
                .Where(n => n.AccountId == accountId);

            if (!string.IsNullOrEmpty(notificationType))
            {
                baseQuery = baseQuery.Where(n => n.NotificationType == notificationType);
            }

            var totalCount = await baseQuery.CountAsync();

            var query = baseQuery.Include(n => n.Child)
                .Include(n => n.Vaccine)
                .Include(n => n.NotificationDeliveryStatuses)
                    .ThenInclude(ds => ds.DeviceToken);

            var notifications = await query
                .OrderByDescending(n => n.SentAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var notificationDtos = _mapper.Map<List<NotificationHistoryResponseDto>>(notifications);

            return new NotificationHistoryListDto
            {
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Items = notificationDtos
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification history for account {AccountId}", accountId);
            throw;
        }
    }

    public async Task<NotificationHistoryResponseDto?> GetNotificationDetailAsync(int notificationHistoryId, int accountId)
    {
        try
        {
            var notificationRepo = _unitOfWork.GetRepository<NotificationHistory>();

            var notification = await notificationRepo.GetAllQueryable()
                .Where(n => n.NotificationHistoryId == notificationHistoryId && n.AccountId == accountId)
                .Include(n => n.Child)
                .Include(n => n.Vaccine)
                .Include(n => n.Appointment)
                .Include(n => n.NotificationDeliveryStatuses)
                    .ThenInclude(ds => ds.DeviceToken)
                .FirstOrDefaultAsync();

            return notification != null ? _mapper.Map<NotificationHistoryResponseDto>(notification) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification detail {NotificationId} for account {AccountId}", 
                notificationHistoryId, accountId);
            throw;
        }
    }

    public async Task CleanupOldNotificationsAsync(int daysToKeep = 90)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
            var notificationRepo = _unitOfWork.GetRepository<NotificationHistory>();

            var oldNotifications = await notificationRepo.GetAllQueryable()
                .Where(n => n.CreatedAt < cutoffDate)
                .ToListAsync();

            if (oldNotifications.Any())
            {
                foreach (var notification in oldNotifications)
                {
                    notificationRepo.Delete(notification);
                }

                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("Cleaned up {Count} old notifications older than {Days} days", 
                    oldNotifications.Count, daysToKeep);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old notifications");
            throw;
        }
    }

    public async Task<Dictionary<string, object>> GetDeliveryStatsAsync(int accountId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        try
        {
            var notificationRepo = _unitOfWork.GetRepository<NotificationHistory>();
            var deliveryRepo = _unitOfWork.GetRepository<NotificationDeliveryStatus>();

            fromDate ??= DateTime.UtcNow.AddDays(-30);
            toDate ??= DateTime.UtcNow;

            var notificationIds = await notificationRepo.GetAllQueryable()
                .Where(n => n.AccountId == accountId && n.SentAt >= fromDate && n.SentAt <= toDate)
                .Select(n => n.NotificationHistoryId)
                .ToListAsync();

            if (!notificationIds.Any())
            {
                return new Dictionary<string, object>
                {
                    ["TotalSent"] = 0,
                    ["TotalDelivered"] = 0,
                    ["TotalFailed"] = 0,
                    ["TotalClicked"] = 0,
                    ["DeliveryRate"] = 0.0,
                    ["ClickRate"] = 0.0
                };
            }

            var deliveryStats = await deliveryRepo.GetAllQueryable()
                .Where(ds => notificationIds.Contains(ds.NotificationHistoryId))
                .GroupBy(ds => ds.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var totalSent = deliveryStats.Sum(s => s.Count);
            var totalDelivered = deliveryStats.Where(s => s.Status == "Delivered").Sum(s => s.Count);
            var totalFailed = deliveryStats.Where(s => s.Status == "Failed").Sum(s => s.Count);
            var totalClicked = deliveryStats.Where(s => s.Status == "Clicked").Sum(s => s.Count);

            return new Dictionary<string, object>
            {
                ["TotalSent"] = totalSent,
                ["TotalDelivered"] = totalDelivered,
                ["TotalFailed"] = totalFailed,
                ["TotalClicked"] = totalClicked,
                ["DeliveryRate"] = totalSent > 0 ? (double)totalDelivered / totalSent * 100 : 0.0,
                ["ClickRate"] = totalDelivered > 0 ? (double)totalClicked / totalDelivered * 100 : 0.0,
                ["FromDate"] = fromDate,
                ["ToDate"] = toDate
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting delivery stats for account {AccountId}", accountId);
            throw;
        }
    }
}

