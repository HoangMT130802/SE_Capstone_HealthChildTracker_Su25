using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services.Implementations
{
    public class PushNotificationService : IPushNotificationService
    {
        private readonly ILogger<PushNotificationService> _logger;
        private readonly FirebaseMessaging _messaging;

        public PushNotificationService(ILogger<PushNotificationService> logger, IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            try
            {
                // Khởi tạo Firebase App nếu chưa có
                if (FirebaseApp.DefaultInstance == null)
                {
                    var firebaseCredentialsPath = configuration["Firebase:CredentialsPath"];
                    if (!string.IsNullOrEmpty(firebaseCredentialsPath))
                    {
                        FirebaseApp.Create(new AppOptions()
                        {
                            Credential = GoogleCredential.FromFile(firebaseCredentialsPath)
                        });
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
                        }
                        else
                        {
                            _logger.LogWarning("Firebase credentials not configured. Push notifications will not work.");
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

        public async Task SendVaccineReminderPushAsync(string deviceToken, string childName, string vaccineName, 
            int doseNumber, string expectedDate, string facilityName = null)
        {
            try
            {
                if (string.IsNullOrEmpty(deviceToken))
                {
                    _logger.LogWarning("Device token is empty, skipping push notification");
                    return;
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

                await SendPushNotificationAsync(deviceToken, title, body, data);
                
                _logger.LogInformation("Vaccine reminder push sent to device {DeviceToken} for child {ChildName}", 
                    MaskDeviceToken(deviceToken), childName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send vaccine reminder push for child {ChildName}", childName);
                throw;
            }
        }

        public async Task SendAppointmentReminderPushAsync(string deviceToken, string childName, string appointmentDate,
            string appointmentTime, string facilityName, string facilityAddress = null)
        {
            try
            {
                if (string.IsNullOrEmpty(deviceToken))
                {
                    _logger.LogWarning("Device token is empty, skipping push notification");
                    return;
                }

                var title = "📅 Nhắc nhở lịch hẹn";
                var body = $"{childName} có lịch hẹn tiêm vaccine vào {appointmentTime} ngày {appointmentDate} tại {facilityName}";

                var data = new Dictionary<string, string>
                {
                    {"type", "appointment_reminder"},
                    {"childName", childName},
                    {"appointmentDate", appointmentDate},
                    {"appointmentTime", appointmentTime},
                    {"facilityName", facilityName},
                    {"facilityAddress", facilityAddress ?? ""}
                };

                await SendPushNotificationAsync(deviceToken, title, body, data);
                
                _logger.LogInformation("Appointment reminder push sent to device {DeviceToken} for child {ChildName}", 
                    MaskDeviceToken(deviceToken), childName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send appointment reminder push for child {ChildName}", childName);
                throw;
            }
        }

        public async Task SendVaccinationCompletionPushAsync(string deviceToken, string childName, string vaccineName,
            int doseNumber, string nextVaccineDate = null)
        {
            try
            {
                if (string.IsNullOrEmpty(deviceToken))
                {
                    _logger.LogWarning("Device token is empty, skipping push notification");
                    return;
                }

                var title = "✅ Hoàn thành tiêm vaccine";
                var body = $"{childName} đã hoàn thành tiêm {vaccineName} mũi {doseNumber}";
                
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

                await SendPushNotificationAsync(deviceToken, title, body, data);
                
                _logger.LogInformation("Vaccination completion push sent to device {DeviceToken} for child {ChildName}", 
                    MaskDeviceToken(deviceToken), childName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send vaccination completion push for child {ChildName}", childName);
                throw;
            }
        }

        public async Task SendCustomPushAsync(string deviceToken, string title, string body, 
            Dictionary<string, string> data = null)
        {
            try
            {
                if (string.IsNullOrEmpty(deviceToken))
                {
                    _logger.LogWarning("Device token is empty, skipping push notification");
                    return;
                }

                await SendPushNotificationAsync(deviceToken, title, body, data ?? new Dictionary<string, string>());
                
                _logger.LogInformation("Custom push sent to device {DeviceToken}", MaskDeviceToken(deviceToken));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send custom push notification");
                throw;
            }
        }

        public async Task SendMulticastPushAsync(List<string> deviceTokens, string title, string body,
            Dictionary<string, string> data = null)
        {
            try
            {
                if (deviceTokens == null || deviceTokens.Count == 0)
                {
                    _logger.LogWarning("Device tokens list is empty, skipping multicast push");
                    return;
                }

                var validTokens = deviceTokens.Where(token => !string.IsNullOrEmpty(token)).ToList();
                if (validTokens.Count == 0)
                {
                    _logger.LogWarning("No valid device tokens found, skipping multicast push");
                    return;
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
                            Sound = "default"
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
                    return;
                }

                var response = await _messaging.SendEachForMulticastAsync(message);
                
                _logger.LogInformation("Multicast push sent to {TotalTokens} devices. Success: {SuccessCount}, Failed: {FailureCount}", 
                    validTokens.Count, response.SuccessCount, response.FailureCount);

                // Log failed tokens để có thể cleanup
                if (response.FailureCount > 0)
                {
                    for (int i = 0; i < response.Responses.Count; i++)
                    {
                        if (!response.Responses[i].IsSuccess)
                        {
                            _logger.LogWarning("Failed to send push to token {Token}: {Error}", 
                                MaskDeviceToken(validTokens[i]), response.Responses[i].Exception?.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send multicast push notification");
                throw;
            }
        }

        private async Task SendPushNotificationAsync(string deviceToken, string title, string body, 
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
                return;
            }

            var response = await _messaging.SendAsync(message);
            _logger.LogDebug("Push notification sent successfully. Message ID: {MessageId}", response);
        }

        private string MaskDeviceToken(string token)
        {
            if (string.IsNullOrEmpty(token) || token.Length < 10)
                return "***";
            
            return $"{token.Substring(0, 6)}...{token.Substring(token.Length - 4)}";
        }
    }
}
