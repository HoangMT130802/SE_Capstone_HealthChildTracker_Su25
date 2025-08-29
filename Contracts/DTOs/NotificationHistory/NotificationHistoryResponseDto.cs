using System;
using System.Collections.Generic;

namespace Contracts.DTOs.NotificationHistory;

public class NotificationHistoryResponseDto
{
    public int NotificationHistoryId { get; set; }
    
    public string NotificationType { get; set; }
    
    public string Title { get; set; }
    
    public string Body { get; set; }
    
    public string? Data { get; set; }
    
    public DateTime SentAt { get; set; }
    
    public string Status { get; set; }
    
    public string? ErrorMessage { get; set; }
    
    public int? ChildId { get; set; }
    
    public string? ChildName { get; set; }
    
    public int? VaccineId { get; set; }
    
    public string? VaccineName { get; set; }
    
    public int? AppointmentId { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public List<NotificationDeliveryStatusDto> DeliveryStatuses { get; set; } = new();
}

public class NotificationDeliveryStatusDto
{
    public int NotificationDeliveryStatusId { get; set; }
    
    public int DeviceTokenId { get; set; }
    
    public string DeviceType { get; set; }
    
    public string Status { get; set; }
    
    public string? FirebaseMessageId { get; set; }
    
    public string? ErrorMessage { get; set; }
    
    public DateTime SentAt { get; set; }
    
    public DateTime? DeliveredAt { get; set; }
    
    public DateTime? ClickedAt { get; set; }
}

public class NotificationHistoryListDto
{
    public int TotalCount { get; set; }
    
    public int PageNumber { get; set; }
    
    public int PageSize { get; set; }
    
    public List<NotificationHistoryResponseDto> Items { get; set; } = new();
}

