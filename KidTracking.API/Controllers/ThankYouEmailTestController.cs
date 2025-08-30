using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repositories.Interfaces;
using Services.Interfaces;

namespace KidTracking.API.Controllers
{
    /// <summary>
    /// Controller để test Thank You Email functionality (chỉ dành cho demo)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ThankYouEmailTestController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<ThankYouEmailTestController> _logger;

        public ThankYouEmailTestController(
            IEmailService emailService,
            ILogger<ThankYouEmailTestController> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        /// <summary>
        /// Test gửi email cảm ơn với dữ liệu demo (không cần memberId thật)
        /// </summary>
        /// <param name="email">Email nhận</param>
        /// <param name="memberName">Tên member (tùy chọn)</param>
        /// <returns>Kết quả test</returns>
        [HttpPost("send-demo")]
        public async Task<ActionResult> SendDemoThankYouEmail([FromQuery] string email, [FromQuery] string? memberName = null)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    return BadRequest(new { message = "Email không được để trống" });
                }

                // Validate email format
                if (!IsValidEmail(email))
                {
                    return BadRequest(new { message = "Email không hợp lệ" });
                }

                var demoMemberName = memberName ?? "Nguyễn Văn A";
                var demoStats = new
                {
                    totalChildren = 2,
                    totalAppointments = 8,
                    totalVaccinations = 6
                };

                _logger.LogInformation("Gửi demo thank you email đến {Email} cho {MemberName}", email, demoMemberName);

                // Gửi email với dữ liệu demo
                await _emailService.SendThankYouEmailAsync(
                    email: email,
                    memberName: demoMemberName,
                    totalChildren: demoStats.totalChildren,
                    totalAppointments: demoStats.totalAppointments,
                    totalVaccinations: demoStats.totalVaccinations
                );

                return Ok(new
                {
                    success = true,
                    message = "Demo email cảm ơn đã được gửi thành công",
                    demoData = new
                    {
                        recipientEmail = email,
                        memberName = demoMemberName,
                        statistics = demoStats,
                        note = "Đây là email demo với dữ liệu giả lập"
                    },
                    sentAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi demo thank you email đến {Email}", email);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Có lỗi xảy ra khi gửi demo email",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Gửi email cảm ơn đơn giản không có thống kê
        /// </summary>
        /// <param name="email">Email nhận</param>
        /// <param name="memberName">Tên member</param>
        /// <returns>Kết quả gửi email</returns>
        [HttpPost("send-simple")]
        public async Task<ActionResult> SendSimpleThankYouEmail([FromQuery] string email, [FromQuery] string? memberName = null)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    return BadRequest(new { message = "Email không được để trống" });
                }

                if (!IsValidEmail(email))
                {
                    return BadRequest(new { message = "Email không hợp lệ" });
                }

                var demoMemberName = memberName ?? "Quý khách";

                _logger.LogInformation("Gửi simple thank you email đến {Email} cho {MemberName}", email, demoMemberName);

                // Gửi email không có thống kê
                await _emailService.SendThankYouEmailAsync(
                    email: email,
                    memberName: demoMemberName,
                    totalChildren: 0,
                    totalAppointments: 0,
                    totalVaccinations: 0
                );

                return Ok(new
                {
                    success = true,
                    message = "Email cảm ơn đơn giản đã được gửi thành công",
                    recipientEmail = email,
                    memberName = demoMemberName,
                    note = "Email không bao gồm thống kê cá nhân",
                    sentAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi simple thank you email đến {Email}", email);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Có lỗi xảy ra khi gửi email",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Lấy template mẫu của email cảm ơn
        /// </summary>
        /// <returns>Thông tin template</returns>
        [HttpGet("template-info")]
        public ActionResult GetTemplateInfo()
        {
            return Ok(new
            {
                emailTemplate = new
                {
                    subject = "🙏 Cảm ơn bạn đã tin tướng và sử dụng Health Child Tracker",
                    sections = new[]
                    {
                        new { name = "Header", description = "Lời chào cá nhân với gradient background", icon = "🙏" },
                        new { name = "Thank you message", description = "Lời cảm ơn chân thành và ý nghĩa", icon = "💝" },
                        new { name = "Personal statistics", description = "Thống kê cá nhân (nếu có dữ liệu)", icon = "📊" },
                        new { name = "Features showcase", description = "Các tính năng nổi bật của hệ thống", icon = "✨" },
                        new { name = "Testimonial", description = "Quote từ đội ngũ phát triển", icon = "💭" },
                        new { name = "Call to action", description = "Khuyến khích tiếp tục sử dụng", icon = "🎯" },
                        new { name = "Contact info", description = "Thông tin hỗ trợ khách hàng", icon = "📞" },
                        new { name = "Footer", description = "Thông tin công ty và social links", icon = "🏢" }
                    },
                    features = new[]
                    {
                        "📱 Responsive design cho mobile và desktop",
                        "🎨 Gradient colors và modern styling", 
                        "📊 Dynamic statistics insertion",
                        "🔒 Professional và trustworthy appearance",
                        "💌 Personalized content với tên member",
                        "🌟 Emoji integration cho friendly tone"
                    },
                    colors = new
                    {
                        primary = "#007bff",
                        secondary = "#28a745", 
                        background = "#f8f9fa",
                        text = "#333333",
                        accent = "#dc3545"
                    }
                },
                usage = new
                {
                    demoEndpoint = "/api/thankyouemail/send-demo?email=your@email.com&memberName=Your Name",
                    realEndpoint = "/api/thankyouemail/send/{memberId}",
                    bulkEndpoint = "/api/thankyouemail/send-bulk",
                    note = "Sử dụng demo endpoint để test mà không cần dữ liệu thật"
                }
            });
        }

        /// <summary>
        /// Validate email format
        /// </summary>
        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}
