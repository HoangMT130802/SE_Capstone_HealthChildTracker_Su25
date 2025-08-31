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

        public async Task SendThankYouEmailAsync(string email, string memberName, int totalChildren = 0, int totalAppointments = 0, int totalVaccinations = 0)
        {
            try
            {
                var subject = "🙏 Cảm ơn bạn đã tin tướng và sử dụng Health Child Tracker";
                
                var body = $@"
                    <html>
                    <head>
                        <style>
                            .container {{ max-width: 600px; margin: 0 auto; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f8f9fa; }}
                            .header {{ background: linear-gradient(135deg, #007bff, #28a745); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
                            .header h1 {{ margin: 0; font-size: 28px; font-weight: 600; }}
                            .header p {{ margin: 10px 0 0 0; font-size: 16px; opacity: 0.9; }}
                            .content {{ padding: 30px; background-color: white; }}
                            .thank-you-message {{ font-size: 18px; line-height: 1.6; color: #333; margin-bottom: 25px; }}
                            .stats-section {{ background-color: #f8f9fa; padding: 20px; border-radius: 8px; margin: 20px 0; }}
                            .stats-title {{ font-size: 20px; font-weight: 600; color: #007bff; margin-bottom: 15px; text-align: center; }}
                            .stats-grid {{ display: flex; justify-content: space-around; flex-wrap: wrap; }}
                            .stat-item {{ text-align: center; margin: 10px; min-width: 120px; }}
                            .stat-number {{ font-size: 24px; font-weight: bold; color: #28a745; }}
                            .stat-label {{ font-size: 14px; color: #666; margin-top: 5px; }}
                            .features-section {{ margin: 25px 0; }}
                            .feature-item {{ background-color: #fff; border-left: 4px solid #007bff; padding: 15px; margin: 10px 0; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
                            .feature-title {{ font-weight: 600; color: #007bff; margin-bottom: 5px; }}
                            .testimonial {{ background-color: #e8f5e8; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #28a745; }}
                            .testimonial-text {{ font-style: italic; color: #555; }}
                            .cta-section {{ text-align: center; margin: 30px 0; }}
                            .cta-button {{ background: linear-gradient(135deg, #007bff, #28a745); color: white; padding: 15px 30px; text-decoration: none; border-radius: 25px; display: inline-block; font-weight: 600; font-size: 16px; }}
                            .footer {{ background-color: #343a40; color: white; padding: 25px; text-align: center; border-radius: 0 0 10px 10px; }}
                            .footer p {{ margin: 5px 0; }}
                            .social-links {{ margin: 15px 0; }}
                            .social-links a {{ color: #007bff; text-decoration: none; margin: 0 10px; }}
                            .emoji {{ font-size: 20px; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='header'>
                                <h1><span class='emoji'>🙏</span> Cảm ơn bạn, {memberName}!</h1>
                                <p>Vì đã tin tưởng và đồng hành cùng Health Child Tracker</p>
                            </div>
                            
                            <div class='content'>
                                <div class='thank-you-message'>
                                    <p>Kính gửi <strong>{memberName}</strong>,</p>
                                    
                                    <p>Chúng tôi xin gửi lời cảm ơn sâu sắc nhất đến bạn vì đã tin tưởng và sử dụng hệ thống <strong>Health Child Tracker</strong> để chăm sóc sức khỏe của con em mình.</p>
                                    
                                    <p>Sự tin tưởng của bạn là động lực to lớn giúp chúng tôi không ngừng cải tiến và phát triển, mang đến những dịch vụ chăm sóc sức khỏe trẻ em tốt nhất.</p>
                                </div>";

                // Thêm thống kê nếu có dữ liệu
                if (totalChildren > 0 || totalAppointments > 0 || totalVaccinations > 0)
                {
                    body += $@"
                                <div class='stats-section'>
                                    <div class='stats-title'><span class='emoji'>📊</span> Hành trình của bạn với Health Child Tracker</div>
                                    <div class='stats-grid'>
                                        <div class='stat-item'>
                                            <div class='stat-number'>{totalChildren}</div>
                                            <div class='stat-label'>Bé yêu đã đăng ký</div>
                                        </div>
                                        <div class='stat-item'>
                                            <div class='stat-number'>{totalAppointments}</div>
                                            <div class='stat-label'>Lịch hẹn đã đặt</div>
                                        </div>
                                        <div class='stat-item'>
                                            <div class='stat-number'>{totalVaccinations}</div>
                                            <div class='stat-label'>Mũi tiêm hoàn thành</div>
                                        </div>
                                    </div>
                                </div>";
                }

                body += $@"
                                <div class='features-section'>
                                    <h3 style='color: #007bff; text-align: center;'><span class='emoji'>✨</span> Những giá trị mà chúng tôi mang lại</h3>
                                    
                                    <div class='feature-item'>
                                        <div class='feature-title'><span class='emoji'>📱</span> Quản lý tiêm chủng dễ dàng</div>
                                        <p>Theo dõi lịch tiêm, nhắc nhở tự động, lưu trữ hồ sơ vaccine an toàn</p>
                                    </div>
                                    
                                    <div class='feature-item'>
                                        <div class='feature-title'><span class='emoji'>🏥</span> Kết nối cơ sở y tế uy tín</div>
                                        <p>Đặt lịch trực tuyến tại các cơ sở y tế chất lượng, tiết kiệm thời gian</p>
                                    </div>
                                    
                                    <div class='feature-item'>
                                        <div class='feature-title'><span class='emoji'>📈</span> Theo dõi tăng trưởng toàn diện</div>
                                        <p>Ghi nhận cân nặng, chiều cao, đánh giá sự phát triển của bé</p>
                                    </div>
                                    
                                    <div class='feature-item'>
                                        <div class='feature-title'><span class='emoji'>🛡️</span> An toàn và bảo mật</div>
                                        <p>Dữ liệu được mã hóa và bảo vệ theo tiêu chuẩn quốc tế</p>
                                    </div>
                                </div>
                                
                                <div class='testimonial'>
                                    <div class='testimonial-text'>
                                        ""Sức khỏe của trẻ em là tương lai của đất nước. Chúng tôi cam kết đồng hành cùng các bậc phụ huynh trong hành trình chăm sóc và bảo vệ sức khỏe cho thế hệ tương lai.""
                                    </div>
                                    <div style='text-align: right; margin-top: 10px; font-weight: 600; color: #007bff;'>
                                        - Đội ngũ Health Child Tracker
                                    </div>
                                </div>
                                
                                <div class='cta-section'>
                                    <p style='font-size: 16px; margin-bottom: 20px;'>Hãy tiếp tục đồng hành cùng chúng tôi!</p>
                                    <a href='#' class='cta-button'>
                                        <span class='emoji'>📱</span> Khám phá thêm tính năng mới
                                    </a>
                                </div>
                                
                                <div style='margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee;'>
                                    <p><strong>Nếu bạn có bất kỳ thắc mắc nào:</strong></p>
                                    <ul style='color: #666; line-height: 1.6;'>
                                        <li><span class='emoji'>📧</span> Email: support@healthchildtracker.com</li>
                                        <li><span class='emoji'>📞</span> Hotline: 1900-xxxx (8:00 - 18:00, T2-T6)</li>
                                        <li><span class='emoji'>💬</span> Chat trong app: Luôn sẵn sàng hỗ trợ 24/7</li>
                                    </ul>
                                </div>
                            </div>
                            
                            <div class='footer'>
                                <p><strong>Health Child Tracker</strong></p>
                                <p><span class='emoji'>🏆</span> Hệ thống quản lý sức khỏe trẻ em hàng đầu Việt Nam</p>
                                <div class='social-links'>
                                    <a href='#'>Facebook</a> |
                                    <a href='#'>Website</a> |
                                    <a href='#'>Zalo</a>
                                </div>
                                <p style='font-size: 12px; opacity: 0.8; margin-top: 15px;'>
                                    © 2024 Health Child Tracker. Tất cả quyền được bảo lưu.<br>
                                    Email này được gửi tự động, vui lòng không trả lời trực tiếp.
                                </p>
                            </div>
                        </div>
                    </body>
                    </html>";

                await SendEmailAsync(email, subject, body);
                _logger.LogInformation($"Thank you email sent to {email} for member {memberName}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send thank you email to {email}: {ex.Message}");
                throw;
            }
        }

        public async Task SendUpcomingVaccinationEmailAsync(string email, string memberName, List<Contracts.DTOs.Email.UpcomingVaccinationItemDTO> upcomingVaccinations)
        {
            try
            {
                var subject = $"🗓️ Lịch tiêm chủng sắp tới - {memberName}";
                
                // Tạo danh sách vaccinations
                var vaccinationListHtml = "";
                if (upcomingVaccinations.Any())
                {
                    foreach (var vaccination in upcomingVaccinations)
                    {
                        var statusColor = vaccination.Status switch
                        {
                            "Confirmed" => "#28a745",
                            "Paid" => "#007bff", 
                            "Pending" => "#ffc107",
                            _ => "#6c757d"
                        };
                        
                        var statusText = vaccination.Status switch
                        {
                            "Confirmed" => "✅ Đã xác nhận",
                            "Paid" => "💳 Đã thanh toán",
                            "Pending" => "⏳ Chờ xác nhận",
                            _ => "📋 " + vaccination.Status
                        };

                        var urgencyClass = vaccination.DaysUntilAppointment <= 3 ? "urgent" : "";
                        
                        vaccinationListHtml += $@"
                            <div class='vaccination-item {urgencyClass}' style='background: #fff; border: 1px solid #ddd; border-radius: 8px; padding: 20px; margin-bottom: 15px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
                                <div style='display: flex; align-items: center; margin-bottom: 15px;'>
                                    <span style='background: {statusColor}; color: white; padding: 4px 12px; border-radius: 20px; font-size: 12px; margin-right: 15px;'>{statusText}</span>
                                    {(vaccination.DaysUntilAppointment <= 3 ? "<span style='background: #dc3545; color: white; padding: 4px 8px; border-radius: 4px; font-size: 11px;'>🚨 GẤP</span>" : "")}
                                </div>
                                <h3 style='color: #2c3e50; margin: 0 0 10px 0; font-size: 18px;'>
                                    <span class='emoji'>👶</span> {vaccination.ChildName} ({vaccination.ChildAge} tháng tuổi)
                                </h3>
                                <div style='margin-bottom: 12px;'>
                                    <strong style='color: #27ae60;'><span class='emoji'>💉</span> {vaccination.VaccineName}</strong>
                                    <span style='background: #e8f5e8; color: #27ae60; padding: 2px 8px; border-radius: 12px; font-size: 12px; margin-left: 10px;'>Mũi {vaccination.DoseNumber}</span>
                                </div>
                                <div style='color: #666; line-height: 1.6;'>
                                    <p style='margin: 5px 0;'><span class='emoji'>📅</span> <strong>Ngày hẹn:</strong> {vaccination.AppointmentDate:dd/MM/yyyy} lúc {vaccination.AppointmentTime}</p>
                                    <p style='margin: 5px 0;'><span class='emoji'>🏥</span> <strong>Cơ sở:</strong> {vaccination.FacilityName}</p>
                                    <p style='margin: 5px 0;'><span class='emoji'>📍</span> <strong>Địa chỉ:</strong> {vaccination.FacilityAddress}</p>
                                    <p style='margin: 5px 0;'><span class='emoji'>⏰</span> <strong>Còn {vaccination.DaysUntilAppointment} ngày</strong></p>
                                </div>
                            </div>";
                    }
                }
                else
                {
                    vaccinationListHtml = @"
                        <div style='text-align: center; padding: 40px; color: #666;'>
                            <span class='emoji' style='font-size: 48px;'>✅</span>
                            <h3 style='color: #27ae60; margin: 20px 0 10px 0;'>Tuyệt vời!</h3>
                            <p>Hiện tại không có lịch tiêm chủng nào sắp tới.</p>
                            <p style='margin-top: 15px;'><em>Hệ thống sẽ tự động thông báo khi có lịch tiêm mới.</em></p>
                        </div>";
                }

                var body = $@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <meta charset='utf-8'>
                        <style>
                            .emoji {{ font-family: 'Apple Color Emoji', 'Segoe UI Emoji', sans-serif; }}
                            .vaccination-item.urgent {{ border-left: 4px solid #dc3545 !important; }}
                            .cta-button {{ 
                                display: inline-block; 
                                background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); 
                                color: white; 
                                padding: 15px 30px; 
                                text-decoration: none; 
                                border-radius: 25px; 
                                font-weight: bold;
                                margin: 20px 0;
                                box-shadow: 0 4px 15px rgba(0,0,0,0.2);
                                transition: transform 0.2s;
                            }}
                            .cta-button:hover {{ transform: translateY(-2px); }}
                            .footer {{ background: #f8f9fa; padding: 20px; text-align: center; color: #666; border-radius: 8px; margin-top: 30px; }}
                        </style>
                    </head>
                    <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; text-align: center; border-radius: 15px 15px 0 0; color: white;'>
                            <h1 style='margin: 0; font-size: 24px;'>
                                <span class='emoji'>🗓️</span> Lịch Tiêm Chủng Sắp Tới
                            </h1>
                            <p style='margin: 10px 0 0 0; opacity: 0.9;'>Chào {memberName}! Đây là lịch tiêm chủng sắp tới của bạn</p>
                        </div>
                        
                        <div style='background: #f8f9fa; padding: 30px; border-radius: 0 0 15px 15px;'>
                            <div style='background: white; padding: 25px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1);'>
                                <h2 style='color: #2c3e50; margin-top: 0;'>
                                    <span class='emoji'>📋</span> Danh Sách Lịch Hẹn ({upcomingVaccinations.Count} lịch)
                                </h2>
                                
                                {vaccinationListHtml}
                                
                                <div style='text-align: center; margin-top: 30px;'>
                                    <p style='color: #666; margin-bottom: 20px;'>💡 <strong>Lưu ý quan trọng:</strong></p>
                                    <ul style='text-align: left; color: #666; line-height: 1.8;'>
                                        <li>Vui lòng đến đúng giờ hẹn để đảm bảo chất lượng dịch vụ</li>
                                        <li>Mang theo sổ tiêm chủng và giấy tờ tùy thân</li>
                                        <li>Liên hệ cơ sở y tế nếu cần thay đổi lịch hẹn</li>
                                        <li>Theo dõi sức khỏe trẻ trước và sau khi tiêm</li>
                                    </ul>
                                    <a href='#' class='cta-button'>
                                        <span class='emoji'>📱</span> Mở ứng dụng để xem chi tiết
                                    </a>
                                </div>
                                
                                <div style='margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee;'>
                                    <p><strong>Cần hỗ trợ?</strong></p>
                                    <ul style='color: #666; line-height: 1.6;'>
                                        <li><span class='emoji'>📧</span> Email: support@healthchildtracker.com</li>
                                        <li><span class='emoji'>📞</span> Hotline: 1900-xxxx (8:00 - 18:00, T2-T6)</li>
                                        <li><span class='emoji'>💬</span> Chat trong app: Luôn sẵn sàng hỗ trợ 24/7</li>
                                    </ul>
                                </div>
                            </div>
                            
                            <div class='footer'>
                                <p><strong>Health Child Tracker</strong></p>
                                <p style='font-size: 12px; opacity: 0.8; margin-top: 15px;'>
                                    Email này được gửi tự động, vui lòng không trả lời trực tiếp.
                                </p>
                            </div>
                        </div>
                    </body>
                    </html>";

                await SendEmailAsync(email, subject, body);
                _logger.LogInformation($"Upcoming vaccination email sent to {email} for member {memberName} with {upcomingVaccinations.Count} appointments");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send upcoming vaccination email to {email}: {ex.Message}");
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
