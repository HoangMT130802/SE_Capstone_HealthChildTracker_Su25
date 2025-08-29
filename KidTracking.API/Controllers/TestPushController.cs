using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace KidTracking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestPushController : ControllerBase
    {
        private readonly IPushNotificationService _pushNotificationService;
        private readonly ILogger<TestPushController> _logger;

        public TestPushController(IPushNotificationService pushNotificationService, ILogger<TestPushController> logger)
        {
            _pushNotificationService = pushNotificationService;
            _logger = logger;
        }

        [HttpPost("check-firebase")]
        public async Task<IActionResult> CheckFirebase()
        {
            try
            {
                _logger.LogInformation("Testing Firebase connection...");
                
                // Test với fake token để xem Firebase có hoạt động không
                var result = await _pushNotificationService.SendCustomPushAsync(
                    "fake-token-test", 
                    "Test Firebase", 
                    "Kiểm tra kết nối Firebase"
                );

                if (result != null)
                {
                    return Ok(new { 
                        status = "success", 
                        message = "Firebase connected successfully",
                        messageId = result
                    });
                }
                else
                {
                    return Ok(new { 
                        status = "warning", 
                        message = "Firebase initialized but no message ID returned"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Firebase test failed");
                return Ok(new { 
                    status = "error", 
                    message = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        [HttpPost("test-real-token")]
        public async Task<IActionResult> TestRealToken([FromBody] TestTokenRequest request)
        {
            try
            {
                _logger.LogInformation("Testing real FCM token: {Token}", request.Token?.Substring(0, Math.Min(10, request.Token?.Length ?? 0)) + "...");
                
                var result = await _pushNotificationService.SendCustomPushAsync(
                    request.Token, 
                    "🩺 Test từ Backend", 
                    "Nếu bạn nhận được tin nhắn này thì FCM đã hoạt động!"
                );

                return Ok(new { 
                    status = "success", 
                    message = "Push notification sent",
                    messageId = result,
                    tokenUsed = request.Token?.Substring(0, Math.Min(10, request.Token?.Length ?? 0)) + "..."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Real token test failed");
                return BadRequest(new { 
                    status = "error", 
                    message = ex.Message,
                    details = ex.InnerException?.Message
                });
            }
        }
    }

    public class TestTokenRequest
    {
        public string Token { get; set; }
    }
}

