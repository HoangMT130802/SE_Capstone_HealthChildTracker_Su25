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
