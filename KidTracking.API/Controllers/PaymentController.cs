using Contracts.DTOs.Transaction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using System.Security.Claims;
using Repositories.Interfaces;
using Repositories.Entities;

namespace KidTracking.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentController> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public PaymentController(
            IPaymentService paymentService,
            ILogger<PaymentController> logger,
            IUnitOfWork unitOfWork)
        {
            _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        /// <summary>
        /// Tạo payment link cho UserMembership hoặc FacilityMembership
        /// </summary>
        [HttpPost("create")]
        [Authorize]
        public async Task<ActionResult<PaymentResponseDTO>> CreatePayment([FromBody] PaymentRequestDTO request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Validate chỉ có một trong hai: MembershipId hoặc FacilityMembershipId
                if (!request.MembershipId.HasValue && !request.FacilityMembershipId.HasValue)
                {
                    return BadRequest(new { success = false, message = "Phải chọn một trong hai: MembershipId hoặc FacilityMembershipId" });
                }

                if (request.MembershipId.HasValue && request.FacilityMembershipId.HasValue)
                {
                    return BadRequest(new { success = false, message = "Chỉ được chọn một trong hai: MembershipId hoặc FacilityMembershipId" });
                }

                // Lấy AccountId từ token
                var currentAccountIdClaim = User.FindFirst("AccountId")?.Value;
                if (string.IsNullOrEmpty(currentAccountIdClaim) || !int.TryParse(currentAccountIdClaim, out int currentAccountId))
                {
                    return Unauthorized("Không thể xác định AccountId từ token");
                }

                // Validate quyền truy cập
                if (request.AccountId != currentAccountId)
                {
                    var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                    if (userRole != "Admin")
                    {
                        return Forbid("Không có quyền tạo payment cho tài khoản khác");
                    }
                }

                string transactionType = request.MembershipId.HasValue ? "UserMembership" : "FacilityMembership";
                _logger.LogInformation("Tạo payment cho AccountId {AccountId}, Type: {TransactionType}", 
                    request.AccountId, transactionType);

                var result = await _paymentService.CreatePaymentAsync(request);

                if (result == null)
                {
                    return BadRequest(new { success = false, message = "Không thể tạo payment" });
                }

                return Ok(new
                {
                    success = true,
                    data = result
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument khi tạo payment");
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation khi tạo payment");
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo payment cho AccountId {AccountId}", request?.AccountId);
                return StatusCode(500, new { success = false, message = "Có lỗi xảy ra khi tạo payment" });
            }
        }

        /// <summary>
        /// Kiểm tra trạng thái thanh toán từ database (chỉ đọc, không gọi PayOS)
        /// Webhook sẽ tự động cập nhật status khi payment thành công
        /// </summary>
        [HttpGet("status/{orderId}")]
        [Authorize]
        public async Task<ActionResult> GetPaymentStatus(string orderId)
        {
            try
            {
                if (string.IsNullOrEmpty(orderId))
                {
                    return BadRequest(new { success = false, message = "OrderId không được để trống" });
                }

                _logger.LogInformation("Kiểm tra trạng thái payment từ DB cho OrderId: {OrderId}", orderId);
                
                // Chỉ lấy status từ database, webhook sẽ tự động cập nhật
                var transactionRepo = _unitOfWork.GetRepository<Transaction>();
                var transaction = await transactionRepo.GetAsync(t => t.TransactionCode == orderId);

                if (transaction == null)
                {
                    return NotFound(new { success = false, message = $"Không tìm thấy giao dịch với mã {orderId}" });
                }

                var result = new PaymentStatusDTO
                {
                    Success = string.Equals(transaction.Status, "PAID", StringComparison.OrdinalIgnoreCase),
                    Status = transaction.Status,
                    Message = GetPaymentStatusMessage(transaction.Status),
                    Amount = transaction.Amount,
                    PaidAt = string.Equals(transaction.Status, "PAID", StringComparison.OrdinalIgnoreCase) ? DateTime.UtcNow : null
                };

                return Ok(new
                {
                    success = true,
                    data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi kiểm tra trạng thái payment cho OrderId: {OrderId}", orderId);
                return StatusCode(500, new { success = false, message = "Có lỗi xảy ra khi kiểm tra trạng thái thanh toán" });
            }
        }

        private string GetPaymentStatusMessage(string status)
        {
            return status switch
            {
                "PAID" => "Thanh toán thành công",
                "CANCELLED" => "Thanh toán đã bị hủy", 
                "PENDING" => "Đang chờ thanh toán",
                "FAILED" => "Thanh toán thất bại",
                _ => "Trạng thái không xác định"
            };
        }

        /// <summary>
        /// 🔔 Webhook endpoint cho PayOS - tự động xử lý khi thanh toán hoàn thành
        /// Đây là endpoint chính để PayOS gọi khi có thay đổi status payment
        /// </summary>
        [HttpPost("webhook")]
        [AllowAnonymous] // PayOS cần gọi được mà không cần auth
        public async Task<ActionResult> PaymentWebhook([FromBody] PaymentWebhookDTO webhookData)
        {
            try
            {
                if (webhookData == null || string.IsNullOrEmpty(webhookData.OrderId))
                {
                    _logger.LogWarning("❌ Webhook data không hợp lệ: {WebhookData}", 
                        webhookData != null ? $"OrderId: {webhookData.OrderId}" : "null");
                    return BadRequest(new { success = false, message = "Webhook data không hợp lệ" });
                }

                _logger.LogInformation("🔔 Nhận webhook từ PayOS - OrderId: {OrderId}, Status: {Status}, Amount: {Amount}", 
                    webhookData.OrderId, webhookData.Status, webhookData.Amount);

                // Validate webhook data
                if (string.IsNullOrEmpty(webhookData.Status))
                {
                    _logger.LogWarning("❌ Webhook thiếu Status cho OrderId: {OrderId}", webhookData.OrderId);
                    return BadRequest(new { success = false, message = "Status không được để trống" });
                }

                // Xử lý webhook
                var result = await _paymentService.ProcessPaymentWebhookAsync(
                    webhookData.OrderId, 
                    webhookData.Status, 
                    webhookData.Amount);

                if (result)
                {
                    _logger.LogInformation("✅ Xử lý webhook thành công cho OrderId: {OrderId}, Status: {Status}", 
                        webhookData.OrderId, webhookData.Status);
                    return Ok(new { 
                        success = true, 
                        message = "Webhook processed successfully",
                        orderId = webhookData.OrderId,
                        status = webhookData.Status
                    });
                }
                else
                {
                    _logger.LogWarning("❌ Xử lý webhook thất bại cho OrderId: {OrderId}", webhookData.OrderId);
                    return BadRequest(new { 
                        success = false, 
                        message = "Failed to process webhook",
                        orderId = webhookData.OrderId
                    });
                }
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "❌ Không tìm thấy transaction cho webhook OrderId: {OrderId}", webhookData?.OrderId);
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi khi xử lý webhook cho OrderId: {OrderId}", webhookData?.OrderId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Success page endpoint - chỉ để hiển thị thông báo thành công
        /// </summary>
        [HttpGet("success")]
        public ActionResult PaymentSuccess([FromQuery] string orderId, [FromQuery] string status)
        {
            _logger.LogInformation("User được redirect về success page - OrderId: {OrderId}, Status: {Status}", orderId, status);
            
            // Chỉ trả về success page, không xử lý logic thanh toán vì đã có webhook
            return Ok(new { 
                success = true, 
                message = "Thanh toán thành công! Membership của bạn sẽ được kích hoạt trong giây lát.",
                orderId = orderId,
                status = status
            });
        }

        /// <summary>
        /// Cancel page endpoint - hiển thị khi user hủy thanh toán
        /// </summary>
        [HttpGet("cancel")]
        public ActionResult PaymentCancel([FromQuery] string orderId)
        {
            _logger.LogInformation("User hủy thanh toán - OrderId: {OrderId}", orderId);
            
            return Ok(new { 
                success = false, 
                message = "Thanh toán đã bị hủy.",
                orderId = orderId
            });
        }

    }

    /// <summary>
    /// DTO cho webhook từ PayOS
    /// </summary>
    public class PaymentWebhookDTO
    {
        public required string OrderId { get; set; }
        public required string Status { get; set; }
        public decimal Amount { get; set; }
    }


} 