using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Services.Interfaces;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Services.Implementations
{
    public class PushNotificationService : IPushNotificationService
    {
        private readonly ILogger<PushNotificationService> _logger;
        private readonly FirebaseMessaging? _messaging;
        private readonly INotificationHistoryService _notificationHistoryService;
        private readonly IDeviceTokenService _deviceTokenService;

        public PushNotificationService(ILogger<PushNotificationService> logger, IConfiguration configuration, 
            INotificationHistoryService notificationHistoryService, IDeviceTokenService deviceTokenService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _notificationHistoryService = notificationHistoryService ?? throw new ArgumentNullException(nameof(notificationHistoryService));
            _deviceTokenService = deviceTokenService ?? throw new ArgumentNullException(nameof(deviceTokenService));
            
            try
            {
                // Khởi tạo Firebase App nếu chưa có
                if (FirebaseApp.DefaultInstance == null)
                {
                    // Thử Environment Variable trước (cho Production)
                    var firebaseServiceAccountJson = Environment.GetEnvironmentVariable("FIREBASE_ACCOUNT_SERVICE") 
                                                   ?? Environment.GetEnvironmentVariable("FIREBASE_SERVICE_ACCOUNT") 
                                                   ?? configuration["Firebase:ServiceAccountJson"];
                    
                    if (!string.IsNullOrEmpty(firebaseServiceAccountJson))
                    {
                        // Sử dụng JSON từ Environment Variable
                        FirebaseApp.Create(new AppOptions()
                        {
                            Credential = GoogleCredential.FromJson(firebaseServiceAccountJson)
                        });
                        _logger.LogInformation("Firebase initialized with service account JSON from environment");
                    }
                    else
                    {
                        // Fallback: sử dụng file path (cho Development)
                        var firebaseCredentialsPath = configuration["Firebase:CredentialsPath"];
                        
                        if (!string.IsNullOrEmpty(firebaseCredentialsPath) && System.IO.File.Exists(firebaseCredentialsPath))
                        {
                            FirebaseApp.Create(new AppOptions()
                            {
                                Credential = GoogleCredential.FromFile(firebaseCredentialsPath)
                            });
                            _logger.LogInformation("Firebase initialized with credentials file: {Path}", firebaseCredentialsPath);
                        }
                        else
                        {
                            _logger.LogError("Firebase credentials not found. Check FIREBASE_SERVICE_ACCOUNT env var or file path: {Path}", firebaseCredentialsPath);
                            return;
                        }
                    }
                }

                _messaging = FirebaseMessaging.DefaultInstance;
                _logger.LogInformation("Firebase messaging initialized successfully");
                
                // Log thêm thông tin để debug
                var projectId = configuration["Firebase:ProjectId"] ?? "kidtrack-78a49";
                _logger.LogInformation("Firebase project ID: {ProjectId}", projectId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Firebase messaging");
                _messaging = null;
            }
        }

        // Backward compatibility method
        public async Task<string?> SendVaccineReminderPushAsync(string deviceToken, string childName, string vaccineName, 
            int doseNumber, string expectedDate, string? facilityName = null)
        {
            return await SendVaccineReminderPushAsync(deviceToken, childName, vaccineName, doseNumber, expectedDate, facilityName, null, null, null);
        }

        // Full method with notification history support
        public async Task<string?> SendVaccineReminderPushAsync(string deviceToken, string childName, string vaccineName, 
            int doseNumber, string expectedDate, string? facilityName = null, int? accountId = null, int? childId = null, int? vaccineId = null)
        {
            try
            {
                if (string.IsNullOrEmpty(deviceToken))
                {
                    _logger.LogWarning("Device token is empty, skipping push notification");
                    return null;
                }

                var title = "🩺 Nhắc nhở tiêm vaccine";
                var body = $"{childName} sắp đến lịch tiêm {vaccineName} mũi {doseNumber} vào ngày {expectedDate}";
                
                if (!string.IsNullOrEmpty(facilityName))
                {
                    body += $" tại {facilityName}";
                }

                var data = new Dictionary<string, string>
                {
                    {"type", "vaccine_reminder"},
                    {"childName", childName},
                    {"vaccineName", vaccineName},
                    {"doseNumber", doseNumber.ToString()},
                    {"expectedDate", expectedDate},
                    {"facilityName", facilityName ?? ""}
                };

                // Lưu notification history nếu có accountId
                int? notificationHistoryId = null;
                if (accountId.HasValue)
                {
                    try
                    {
                        notificationHistoryId = await _notificationHistoryService.SaveNotificationHistoryAsync(
                            accountId.Value,
                            "vaccine_reminder",
                            title,
                            body,
                            System.Text.Json.JsonSerializer.Serialize(data),
                            childId,
                            vaccineId
                        );
                        _logger.LogDebug("Saved notification history {NotificationId} for vaccine reminder", notificationHistoryId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to save notification history for vaccine reminder");
                    }
                }

                var messageId = await SendPushNotificationAsync(deviceToken, title, body, data);
                
                // Lưu delivery status nếu có notification history và device token info
                if (notificationHistoryId.HasValue && !string.IsNullOrEmpty(messageId))
                {
                    try
                    {
                        // Tìm device token ID từ token string (cần implement helper method)
                        var deviceTokenId = await GetDeviceTokenIdAsync(deviceToken);
                        if (deviceTokenId.HasValue)
                        {
                            await _notificationHistoryService.SaveDeliveryStatusAsync(
                                notificationHistoryId.Value,
                                deviceTokenId.Value,
                                "Sent",
                                messageId
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to save delivery status for notification {NotificationId}", notificationHistoryId);
                    }
                }
                
                _logger.LogInformation("Vaccine reminder push sent to device {DeviceToken} for child {ChildName}, MessageId: {MessageId}", 
                    MaskDeviceToken(deviceToken), childName, messageId);
                    
                return messageId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send vaccine reminder push for child {ChildName}", childName);
                throw;
            }
        }

        public async Task<string?> SendAppointmentReminderPushAsync(string deviceToken, string childName, string appointmentDate,
            string appointmentTime, string facilityName, string? facilityAddress = null)
        {
            try
            {
                if (string.IsNullOrEmpty(deviceToken))
                {
                    _logger.LogWarning("Device token is empty, skipping push notification");
                    return null;
                }

                var title = "📅 Nhắc nhở lịch hẹn";
                var body = $"{childName} có lịch hẹn vào ngày {appointmentDate} lúc {appointmentTime} tại {facilityName}";
                
                if (!string.IsNullOrEmpty(facilityAddress))
                {
                    body += $", {facilityAddress}";
                }

                var data = new Dictionary<string, string>
                {
                    {"type", "appointment_reminder"},
                    {"childName", childName},
                    {"appointmentDate", appointmentDate},
                    {"appointmentTime", appointmentTime},
                    {"facilityName", facilityName},
                    {"facilityAddress", facilityAddress ?? ""}
                };

                var messageId = await SendPushNotificationAsync(deviceToken, title, body, data);
                
                _logger.LogInformation("Appointment reminder push sent to device {DeviceToken} for child {ChildName}, MessageId: {MessageId}", 
                    MaskDeviceToken(deviceToken), childName, messageId);
                    
                return messageId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send appointment reminder push for child {ChildName}", childName);
                throw;
            }
        }

        public async Task<string?> SendVaccinationCompletionPushAsync(string deviceToken, string childName, string vaccineName,
            int doseNumber, string? nextVaccineDate = null)
        {
            try
            {
                if (string.IsNullOrEmpty(deviceToken))
                {
                    _logger.LogWarning("Device token is empty, skipping push notification");
                    return null;
                }

                var title = "✅ Tiêm vaccine thành công";
                var body = $"{childName} đã tiêm {vaccineName} mũi {doseNumber} thành công";
                
                if (!string.IsNullOrEmpty(nextVaccineDate))
                {
                    body += $". Mũi tiếp theo dự kiến vào {nextVaccineDate}";
                }

                var data = new Dictionary<string, string>
                {
                    {"type", "vaccination_completion"},
                    {"childName", childName},
                    {"vaccineName", vaccineName},
                    {"doseNumber", doseNumber.ToString()},
                    {"nextVaccineDate", nextVaccineDate ?? ""}
                };

                var messageId = await SendPushNotificationAsync(deviceToken, title, body, data);
                
                _logger.LogInformation("Vaccination completion push sent to device {DeviceToken} for child {ChildName}, MessageId: {MessageId}", 
                    MaskDeviceToken(deviceToken), childName, messageId);
                    
                return messageId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send vaccination completion push for child {ChildName}", childName);
                throw;
            }
        }

        public async Task<string?> SendCustomPushAsync(string deviceToken, string title, string body, 
            Dictionary<string, string>? data = null)
        {
            try
            {
                if (string.IsNullOrEmpty(deviceToken))
                {
                    _logger.LogWarning("Device token is empty, skipping push notification");
                    return null;
                }

                var messageId = await SendPushNotificationAsync(deviceToken, title, body, data ?? new Dictionary<string, string>());
                
                _logger.LogInformation("Custom push sent to device {DeviceToken}, MessageId: {MessageId}", 
                    MaskDeviceToken(deviceToken), messageId);
                    
                return messageId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send custom push notification");
                throw;
            }
        }

        public async Task<List<string>> SendMulticastPushAsync(List<string> deviceTokens, string title, string body,
            Dictionary<string, string>? data = null)
        {
            var messageIds = new List<string>();
            
            try
            {
                if (deviceTokens == null || !deviceTokens.Any())
                {
                    _logger.LogWarning("Device tokens list is empty, skipping multicast push notification");
                    return messageIds;
                }

                // Lọc bỏ tokens rỗng
                var validTokens = deviceTokens.Where(token => !string.IsNullOrEmpty(token)).ToList();
                
                if (!validTokens.Any())
                {
                    _logger.LogWarning("No valid device tokens found, skipping multicast push notification");
                    return messageIds;
                }

                if (_messaging == null)
                {
                    _logger.LogWarning("Firebase messaging not initialized, cannot send multicast push notification");
                    return messageIds;
                }

                // Thay vì multicast, gửi từng message riêng lẻ để tránh lỗi 404
                _logger.LogInformation("Sending {TokenCount} individual messages instead of multicast to avoid 404 error", validTokens.Count);
                
                foreach (var token in validTokens)
                {
                    try
                    {
                        var messageId = await SendPushNotificationAsync(token, title, body, data ?? new Dictionary<string, string>());
                        if (!string.IsNullOrEmpty(messageId))
                        {
                            messageIds.Add(messageId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send push to device {DeviceToken}", MaskDeviceToken(token));
                    }
                }

                _logger.LogInformation("Individual push sent to {TokenCount} devices. Success: {SuccessCount}", 
                    validTokens.Count, messageIds.Count);

                return messageIds;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send multicast push notification");
                throw;
            }
        }

        private async Task<string?> SendPushNotificationAsync(string deviceToken, string title, string body, 
            Dictionary<string, string> data)
        {
            var message = new Message()
            {
                Token = deviceToken,
                Notification = new Notification()
                {
                    Title = title,
                    Body = body
                },
                Data = data,
                Android = new AndroidConfig()
                {
                    Priority = Priority.High,
                    Notification = new AndroidNotification()
                    {
                        Icon = "ic_notification",
                        Color = "#2196F3",
                        Sound = "default",
                        ChannelId = "vaccine_reminders"
                    }
                },
                Apns = new ApnsConfig()
                {
                    Aps = new Aps()
                    {
                        Alert = new ApsAlert()
                        {
                            Title = title,
                            Body = body
                        },
                        Sound = "default",
                        Badge = 1
                    }
                }
            };

            if (_messaging == null)
            {
                _logger.LogWarning("Firebase messaging not initialized, cannot send push notification");
                return null;
            }


            var response = await _messaging.SendAsync(message);
            _logger.LogDebug("Push notification sent successfully. Message ID: {MessageId}", response);
            return response;
        }

        private string MaskDeviceToken(string token)
        {
            if (string.IsNullOrEmpty(token) || token.Length < 10)
                return "***";
            
            return $"{token.Substring(0, 6)}...{token.Substring(token.Length - 4)}";
        }

        private async Task<int?> GetDeviceTokenIdAsync(string token)
        {
            try
            {
                return await _deviceTokenService.GetDeviceTokenIdByTokenAsync(token);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get device token ID for token {Token}", MaskDeviceToken(token));
                return null;
            }
        }
    }
}



