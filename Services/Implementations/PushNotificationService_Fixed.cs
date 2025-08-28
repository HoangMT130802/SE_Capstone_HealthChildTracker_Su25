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
        private readonly FirebaseMessaging _messaging;
        private readonly INotificationHistoryService _notificationHistoryService;

        public PushNotificationService(ILogger<PushNotificationService> logger, IConfiguration configuration, 
            INotificationHistoryService notificationHistoryService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _notificationHistoryService = notificationHistoryService ?? throw new ArgumentNullException(nameof(notificationHistoryService));
            
            try
            {
                // Khởi tạo Firebase App nếu chưa có
                if (FirebaseApp.DefaultInstance == null)
                {
                    var firebaseCredentialsPath = configuration["Firebase:CredentialsPath"];
                    if (!string.IsNullOrEmpty(firebaseCredentialsPath))
                    {
                        if (System.IO.File.Exists(firebaseCredentialsPath))
                        {
                            FirebaseApp.Create(new AppOptions()
                            {
                                Credential = GoogleCredential.FromFile(firebaseCredentialsPath)
                            });
                            _logger.LogInformation("Firebase initialized with credentials file: {Path}", firebaseCredentialsPath);
                        }
                        else
                        {
                            _logger.LogError("Firebase credentials file not found at: {Path}", firebaseCredentialsPath);
                        }
                    }
                    else
                    {
                        // Fallback: sử dụng service account JSON từ environment variable
                        var serviceAccountJson = configuration["Firebase:ServiceAccountJson"];
                        if (!string.IsNullOrEmpty(serviceAccountJson))
                        {
                            FirebaseApp.Create(new AppOptions()
                            {
                                Credential = GoogleCredential.FromJson(serviceAccountJson)
                            });
                            _logger.LogInformation("Firebase initialized with service account JSON");
                        }
                        else
                        {
                            _logger.LogWarning("No Firebase credentials found in configuration");
                        }
                    }
                }

                _messaging = FirebaseMessaging.DefaultInstance;
                _logger.LogInformation("Firebase messaging initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Firebase messaging");
            }
        }

        public async Task<string?> SendVaccineReminderPushAsync(string deviceToken, string childName, string vaccineName, 
            int doseNumber, string expectedDate, string facilityName = null)
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

                var messageId = await SendPushNotificationAsync(deviceToken, title, body, data);
                
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
            string appointmentTime, string facilityName, string facilityAddress = null)
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
            int doseNumber, string nextVaccineDate = null)
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
            Dictionary<string, string> data = null)
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
            Dictionary<string, string> data = null)
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

                var message = new MulticastMessage()
                {
                    Tokens = validTokens,
                    Notification = new Notification()
                    {
                        Title = title,
                        Body = body
                    },
                    Data = data ?? new Dictionary<string, string>(),
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
                    _logger.LogWarning("Firebase messaging not initialized, cannot send multicast push notification");
                    return messageIds;
                }

                var response = await _messaging.SendMulticastAsync(message);
                
                _logger.LogInformation("Multicast push sent to {TokenCount} devices. Success: {SuccessCount}, Failed: {FailureCount}", 
                    validTokens.Count, response.SuccessCount, response.FailureCount);

                // Extract message IDs từ response
                for (int i = 0; i < response.Responses.Count; i++)
                {
                    var sendResponse = response.Responses[i];
                    if (sendResponse.IsSuccess)
                    {
                        messageIds.Add(sendResponse.MessageId);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to send push to device {DeviceToken}: {Error}", 
                            MaskDeviceToken(validTokens[i]), sendResponse.Exception?.Message);
                    }
                }

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
    }
}
