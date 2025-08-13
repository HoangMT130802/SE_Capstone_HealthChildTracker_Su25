using Contracts.DTOs.Transaction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using System.Security.Claims;
using Repositories.Interfaces;
using Repositories.Entities;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using System.Linq;

namespace KidTracking.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentController> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _memoryCache;

        public PaymentController(
            IPaymentService paymentService,
            ILogger<PaymentController> logger,
            IUnitOfWork unitOfWork,
            IConfiguration configuration,
            IMemoryCache memoryCache)
        {
            _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
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
        /// Kiểm tra trạng thái thanh toán (poll PayOS) và đồng bộ DB
        /// </summary>
        [HttpGet("check-status/{orderId}")]
        [Authorize]
        public async Task<ActionResult> CheckPaymentStatus(string orderId)
        {
            try
            {
                if (string.IsNullOrEmpty(orderId))
                {
                    return BadRequest(new { success = false, message = "OrderId không được để trống" });
                }

                _logger.LogInformation("Kiểm tra trạng thái payment (PayOS) cho OrderId: {OrderId}", orderId);
                var result = await _paymentService.CheckPaymentStatusAsync(orderId);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        status = result.Status,
                        message = result.Message,
                        success = result.Success,
                        amount = result.Amount,
                        paidAt = result.PaidAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi kiểm tra trạng thái payment cho OrderId: {OrderId}", orderId);
                return BadRequest(new { success = false, message = ex.Message });
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

        // Loại bỏ webhook/success/cancel theo yêu cầu đơn giản hóa

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