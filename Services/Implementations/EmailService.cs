using Services;
using Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Mail;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Services.Implementations
{
    public class EmailService : IEmailService
    {
        private readonly IOtpCacheService _otpCacheService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            IOtpCacheService otpCacheService,
            IConfiguration configuration,
            ILogger<EmailService> logger)
        {
            _otpCacheService = otpCacheService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendVerificationEmailAsync(string email, string otpCode, string fullName)
        {
            try
            {
                var subject = "Xác thực tài khoản Health Child Tracker";
                var body = $@"
                    <html>
                    <body>
                        <h2>Chào {fullName},</h2>
                        <p>Cảm ơn bạn đã đăng ký tài khoản tại Health Child Tracker!</p>
                        <p>Để hoàn tất việc đăng ký, vui lòng nhập mã xác thực sau:</p>
                        <h3 style='color: #007bff; font-size: 24px; letter-spacing: 3px;'>{otpCode}</h3>
                        <p>Mã xác thực này có hiệu lực trong 15 phút.</p>
                        <p>Nếu bạn không yêu cầu đăng ký tài khoản, vui lòng bỏ qua email này.</p>
                        <br>
                        <p>Trân trọng,<br>Đội ngũ Health Child Tracker</p>
                    </body>
                    </html>";

                await SendEmailAsync(email, subject, body);
                _logger.LogInformation($"Verification email sent to {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send verification email to {email}: {ex.Message}");
                throw;
            }
        }

        public async Task SendForgotPasswordEmailAsync(string email, string otpCode, string fullName)
        {
            try
            {
                var subject = "Khôi phục mật khẩu Health Child Tracker";
                var body = $@"
                    <html>
                    <body>
                        <h2>Chào {fullName},</h2>
                        <p>Chúng tôi nhận được yêu cầu khôi phục mật khẩu cho tài khoản của bạn.</p>
                        <p>Để đặt lại mật khẩu, vui lòng nhập mã xác thực sau:</p>
                        <h3 style='color: #007bff; font-size: 24px; letter-spacing: 3px;'>{otpCode}</h3>
                        <p>Mã xác thực này có hiệu lực trong 15 phút.</p>
                        <p>Nếu bạn không yêu cầu khôi phục mật khẩu, vui lòng bỏ qua email này.</p>
                        <br>
                        <p>Trân trọng,<br>Đội ngũ Health Child Tracker</p>
                    </body>
                    </html>";

                await SendEmailAsync(email, subject, body);
                _logger.LogInformation($"Forgot password email sent to {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send forgot password email to {email}: {ex.Message}");
                throw;
            }
        }

        public async Task<string> GenerateOtpCodeAsync()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            var randomNumber = Math.Abs(BitConverter.ToInt32(bytes, 0));
            return (randomNumber % 1000000).ToString("D6");
        }

        public async Task<bool> SaveEmailVerificationAsync(string email, string otpCode, string type, int? accountId = null)
        {
            try
            {
                var otpInfo = new OtpInfo
                {
                    Email = email,
                    OtpCode = otpCode,
                    Type = type,
                    AccountId = accountId,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                    IsUsed = false
                };

                await _otpCacheService.SaveOtpAsync(otpInfo);

                _logger.LogInformation($"OTP saved for email {email}, type {type}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to save OTP for email {email}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> VerifyOtpCodeAsync(string email, string otpCode, string type)
        {
            try
            {
                var isValid = await _otpCacheService.VerifyAndConsumeOtpAsync(email, otpCode, type);

                if (!isValid)
                {
                    _logger.LogWarning($"Invalid or expired OTP for email {email}");
                    return false;
                }

                _logger.LogInformation($"OTP verified successfully for email {email}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to verify OTP for email {email}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SaveRegistrationDataAsync(string email, string otpCode, string accountName, string password, string fullName, string phone, string address)
        {
            try
            {
                var otpInfo = new OtpInfo
                {
                    Email = email,
                    OtpCode = otpCode,
                    Type = "Registration",
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                    IsUsed = false,
                    // Thông tin đăng ký
                    AccountName = accountName,
                    Password = password,
                    FullName = fullName,
                    Phone = phone,
                    Address = address
                };

                await _otpCacheService.SaveOtpAsync(otpInfo);

                _logger.LogInformation($"Registration data saved for email {email}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to save registration data for email {email}: {ex.Message}");
                return false;
            }
        }

        public async Task<OtpInfo> GetRegistrationDataAsync(string email, string otpCode)
        {
            try
            {
                var otpInfo = await _otpCacheService.GetOtpAsync(email, otpCode, "Registration");
                return otpInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to get registration data for email {email}: {ex.Message}");
                return null;
            }
        }

        public async Task CleanupExpiredOtpAsync()
        {
            try
            {
                await _otpCacheService.CleanupExpiredOtpAsync();
                _logger.LogInformation("Cleaned up expired OTPs from cache");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to cleanup expired OTPs: {ex.Message}");
            }
        }

        public async Task SendVaccineReminderEmailAsync(string email, string parentName, string childName, string vaccineName, int doseNumber, DateOnly expectedDate, string facilityName = null)
        {
            try
            {
                var subject = $"🩺 Nhắc nhở tiêm vaccine cho {childName}";
                var facilityInfo = !string.IsNullOrEmpty(facilityName) 
                    ? $"<p><strong>Cơ sở y tế gợi ý:</strong> {facilityName}</p>" 
                    : "<p>Bạn có thể tìm kiếm cơ sở y tế phù hợp trong ứng dụng Health Child Tracker.</p>";
                
                var body = $@"
                    <html>
                    <head>
                        <style>
                            .container {{ max-width: 600px; margin: 0 auto; font-family: Arial, sans-serif; }}
                            .header {{ background-color: #007bff; color: white; padding: 20px; text-align: center; }}
                            .content {{ padding: 20px; background-color: #f8f9fa; }}
                            .vaccine-info {{ background-color: white; padding: 15px; border-radius: 8px; margin: 15px 0; }}
                            .important {{ color: #dc3545; font-weight: bold; }}
                            .button {{ background-color: #007bff; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='header'>
                                <h2>🩺 Health Child Tracker</h2>
                                <p>Nhắc nhở tiêm vaccine</p>
                            </div>
                            <div class='content'>
                                <h3>Chào {parentName},</h3>
                                <p>Đây là lời nhắc nhở về việc tiêm vaccine cho con bạn.</p>
                                
                                <div class='vaccine-info'>
                                    <h4>📋 Thông tin vaccine:</h4>
                                    <p><strong>Tên trẻ:</strong> {childName}</p>
                                    <p><strong>Vaccine:</strong> {vaccineName}</p>
                                    <p><strong>Mũi thứ:</strong> {doseNumber}</p>
                                    <p><strong>Ngày dự kiến:</strong> {expectedDate:dd/MM/yyyy}</p>
                                </div>
                                
                                {facilityInfo}
                                
                                <p class='important'>⏰ Lưu ý: Việc tiêm vaccine đúng lịch rất quan trọng cho sức khỏe của trẻ.</p>
                                
                                <p style='text-align: center; margin: 30px 0;'>
                                    <a href='#' class='button'>Đặt lịch ngay</a>
                                </p>
                                
                                <p>Nếu bạn có bất kỳ câu hỏi nào, vui lòng liên hệ với chúng tôi qua ứng dụng Health Child Tracker.</p>
                                
                                <br>
                                <p>Trân trọng,<br>Đội ngũ Health Child Tracker</p>
                            </div>
                        </div>
                    </body>
                    </html>";

                await SendEmailAsync(email, subject, body);
                _logger.LogInformation($"Vaccine reminder email sent to {email} for child {childName}, vaccine {vaccineName}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send vaccine reminder email to {email}: {ex.Message}");
                throw;
            }
        }

        public async Task SendAppointmentReminderEmailAsync(string email, string parentName, string childName, DateOnly appointmentDate, string timeSlot, string facilityName, string facilityAddress, string vaccineName)
        {
            try
            {
                var subject = $"📅 Nhắc nhở lịch hẹn tiêm vaccine - {childName}";
                
                var body = $@"
                    <html>
                    <head>
                        <style>
                            .container {{ max-width: 600px; margin: 0 auto; font-family: Arial, sans-serif; }}
                            .header {{ background-color: #28a745; color: white; padding: 20px; text-align: center; }}
                            .content {{ padding: 20px; background-color: #f8f9fa; }}
                            .appointment-info {{ background-color: white; padding: 15px; border-radius: 8px; margin: 15px 0; border-left: 4px solid #28a745; }}
                            .important {{ color: #dc3545; font-weight: bold; }}
                            .button {{ background-color: #28a745; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block; margin: 5px; }}
                            .button-secondary {{ background-color: #6c757d; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block; margin: 5px; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='header'>
                                <h2>📅 Health Child Tracker</h2>
                                <p>Nhắc nhở lịch hẹn tiêm vaccine</p>
                            </div>
                            <div class='content'>
                                <h3>Chào {parentName},</h3>
                                <p>Bạn có lịch hẹn tiêm vaccine sắp tới cho {childName}.</p>
                                
                                <div class='appointment-info'>
                                    <h4>📋 Thông tin lịch hẹn:</h4>
                                    <p><strong>Tên trẻ:</strong> {childName}</p>
                                    <p><strong>Vaccine:</strong> {vaccineName}</p>
                                    <p><strong>Ngày hẹn:</strong> {appointmentDate:dd/MM/yyyy}</p>
                                    <p><strong>Giờ hẹn:</strong> {timeSlot}</p>
                                    <p><strong>Cơ sở y tế:</strong> {facilityName}</p>
                                    <p><strong>Địa chỉ:</strong> {facilityAddress}</p>
                                </div>
                                
                                <div style='background-color: #fff3cd; padding: 15px; border-radius: 8px; margin: 15px 0;'>
                                    <h4>📝 Lưu ý chuẩn bị:</h4>
                                    <ul>
                                        <li>Mang theo sổ tiêm chủng của trẻ</li>
                                        <li>Cho trẻ ăn no trước khi tiêm (30-60 phút)</li>
                                        <li>Đến đúng giờ hẹn để tránh chờ đợi</li>
                                        <li>Mang theo giấy tờ tùy thân của trẻ</li>
                                    </ul>
                                </div>
                                
                                <p style='text-align: center; margin: 30px 0;'>
                                    <a href='#' class='button'>Xác nhận tham gia</a>
                                    <a href='#' class='button-secondary'>Thay đổi lịch</a>
                                </p>
                                
                                <p class='important'>⚠️ Nếu không thể tham gia, vui lòng hủy lịch trước 24 giờ.</p>
                                
                                <br>
                                <p>Trân trọng,<br>Đội ngũ Health Child Tracker</p>
                            </div>
                        </div>
                    </body>
                    </html>";

                await SendEmailAsync(email, subject, body);
                _logger.LogInformation($"Appointment reminder email sent to {email} for appointment on {appointmentDate}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send appointment reminder email to {email}: {ex.Message}");
                throw;
            }
        }

        public async Task SendVaccinationCompletionEmailAsync(string email, string parentName, string childName, string vaccineName, int doseNumber, DateOnly vaccinationDate, DateOnly? nextDoseDate = null)
        {
            try
            {
                var subject = $"✅ Hoàn thành tiêm vaccine - {childName}";
                var nextDoseInfo = nextDoseDate.HasValue 
                    ? $@"<div style='background-color: #d1ecf1; padding: 15px; border-radius: 8px; margin: 15px 0;'>
                           <h4>📅 Mũi tiêm tiếp theo:</h4>
                           <p><strong>Ngày dự kiến:</strong> {nextDoseDate.Value:dd/MM/yyyy}</p>
                           <p>Chúng tôi sẽ nhắc nhở bạn trước ngày tiêm.</p>
                         </div>" 
                    : "<p style='color: #28a745; font-weight: bold;'>🎉 Chúc mừng! Trẻ đã hoàn thành đầy đủ các mũi tiêm cho vaccine này.</p>";
                
                var body = $@"
                    <html>
                    <head>
                        <style>
                            .container {{ max-width: 600px; margin: 0 auto; font-family: Arial, sans-serif; }}
                            .header {{ background-color: #28a745; color: white; padding: 20px; text-align: center; }}
                            .content {{ padding: 20px; background-color: #f8f9fa; }}
                            .completion-info {{ background-color: white; padding: 15px; border-radius: 8px; margin: 15px 0; border-left: 4px solid #28a745; }}
                            .button {{ background-color: #007bff; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='header'>
                                <h2>✅ Health Child Tracker</h2>
                                <p>Hoàn thành tiêm vaccine</p>
                            </div>
                            <div class='content'>
                                <h3>Chào {parentName},</h3>
                                <p>Chúc mừng! {childName} đã hoàn thành việc tiêm vaccine thành công.</p>
                                
                                <div class='completion-info'>
                                    <h4>📋 Thông tin vaccine đã tiêm:</h4>
                                    <p><strong>Tên trẻ:</strong> {childName}</p>
                                    <p><strong>Vaccine:</strong> {vaccineName}</p>
                                    <p><strong>Mũi thứ:</strong> {doseNumber}</p>
                                    <p><strong>Ngày tiêm:</strong> {vaccinationDate:dd/MM/yyyy}</p>
                                </div>
                                
                                {nextDoseInfo}
                                
                                <p style='text-align: center; margin: 30px 0;'>
                                    <a href='#' class='button'>Xem lịch sử tiêm vaccine</a>
                                </p>
                                
                                <p>Cảm ơn bạn đã tin tưởng và sử dụng dịch vụ Health Child Tracker để theo dõi sức khỏe của trẻ.</p>
                                
                                <br>
                                <p>Trân trọng,<br>Đội ngũ Health Child Tracker</p>
                            </div>
                        </div>
                    </body>
                    </html>";

                await SendEmailAsync(email, subject, body);
                _logger.LogInformation($"Vaccination completion email sent to {email} for child {childName}, vaccine {vaccineName}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send vaccination completion email to {email}: {ex.Message}");
                throw;
            }
        }

        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var emailSettings = _configuration.GetSection("EmailSettings");
            
            using var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(emailSettings["SenderEmail"], emailSettings["SenderPassword"]),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(emailSettings["SenderEmail"], emailSettings["SenderName"]),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
            };

            mailMessage.To.Add(toEmail);

            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}
