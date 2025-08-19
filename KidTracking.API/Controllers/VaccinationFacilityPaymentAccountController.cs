using Contracts.DTOs.VaccinationFacilityPaymentAccount;
using Contracts.DTOs.Transaction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace KidTracking.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class VaccinationFacilityPaymentAccountController : ControllerBase
    {
        private readonly IVaccinationFacilityPaymentAccountService _paymentAccountService;
        private readonly ILogger<VaccinationFacilityPaymentAccountController> _logger;

        public VaccinationFacilityPaymentAccountController(IVaccinationFacilityPaymentAccountService paymentAccountService, ILogger<VaccinationFacilityPaymentAccountController> logger)
        {
            _paymentAccountService = paymentAccountService ?? throw new ArgumentNullException(nameof(paymentAccountService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private int GetAccountId()
        {
            var accountId = int.Parse(User.FindFirst("AccountId")?.Value ?? "0");
            if (accountId == 0) throw new UnauthorizedAccessException("AccountId not found in token");
            return accountId;
        }

        [HttpPost]
        public async Task<IActionResult> CreatePaymentAccount([FromBody] CreateVaccinationFacilityPaymentAccountDto paymentAccountDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var accountId = GetAccountId();
                var paymentAccountId = await _paymentAccountService.CreatePaymentAccountAsync(paymentAccountDto, accountId);
                return Ok(new { paymentAccountId, message = "Payment account created successfully" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment account: {Error}", ex.Message);
                return StatusCode(500, new { message = "Internal server error: " + ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePaymentAccount(int id, [FromBody] UpdateVaccinationFacilityPaymentAccountDto paymentAccountDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var accountId = GetAccountId();
                await _paymentAccountService.UpdatePaymentAccountAsync(id, paymentAccountDto, accountId);
                return Ok(new { message = "Payment account updated successfully" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error: " + ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePaymentAccount(int id)
        {
            try
            {
                var accountId = GetAccountId();
                await _paymentAccountService.DeletePaymentAccountAsync(id, accountId);
                return Ok(new { message = "Payment account deleted successfully" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error: " + ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPaymentAccountById(int id)
        {
            try
            {
                var paymentAccount = await _paymentAccountService.GetPaymentAccountByIdAsync(id);
                return Ok(paymentAccount);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllPaymentAccounts([FromQuery] bool? isActive = null, [FromQuery] int? pageIndex = 1, [FromQuery] int? pageSize = 10)
        {
            if (pageIndex <= 0 || pageSize <= 0)
            {
                return BadRequest(new { message = "PageIndex and PageSize must be positive" });
            }

            try
            {
                var paymentAccounts = await _paymentAccountService.GetAllPaymentAccountsAsync(isActive, pageIndex, pageSize);
                return Ok(new
                {
                    totalCount = paymentAccounts.TotalCount,
                    pageIndex,
                    pageSize,
                    data = paymentAccounts.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all payment accounts: {Error}", ex.Message);
                return StatusCode(500, new { message = "Internal server error: " + ex.Message });
            }
        }

        [HttpGet("byFacility/{facilityId}")]
        public async Task<IActionResult> GetPaymentAccountByFacilityId(int facilityId, [FromQuery] bool? isActive = null, [FromQuery] int? pageIndex = 1, [FromQuery] int? pageSize = 10)
        {
            if (pageIndex <= 0 || pageSize <= 0)
            {
                return BadRequest(new { message = "PageIndex and PageSize must be positive" });
            }

            try
            {
                var paymentAccounts = await _paymentAccountService.GetPaymentAccountByFacilityIdAsync(facilityId, isActive, pageIndex, pageSize);
                return Ok(new
                {
                    totalCount = paymentAccounts.TotalCount,
                    pageIndex,
                    pageSize,
                    data = paymentAccounts.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting payment accounts for FacilityId {facilityId}: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error: " + ex.Message });
            }
        }

        #region Payment Methods

        /// <summary>
        /// Tạo payment link dựa trên AppointmentId - tự động phát hiện loại thanh toán
        /// Hỗ trợ: ORDER (nếu appointment có OrderId) hoặc INDIVIDUAL_VACCINE (tiêm lẻ)
        /// </summary>
        [HttpPost("payment")]
        public async Task<IActionResult> CreateFacilityPayment([FromBody] CreateFacilityPaymentDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var accountId = GetAccountId();
                var result = await _paymentAccountService.CreateFacilityPaymentAsync(request, accountId);
                
                return Ok(new 
                { 
                    success = true,
                    data = result
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access to create facility payment");
                return StatusCode(403, new { success = false, message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument for facility payment");
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation for facility payment");
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating facility payment");
                return StatusCode(500, new { success = false, message = "Có lỗi xảy ra khi tạo payment" });
            }
        }

        /// <summary>
        /// Kiểm tra trạng thái thanh toán - chỉ cần OrderCode
        /// OrderCode format: {timestamp}_{facilityId}_{appointmentId}[_{orderId}]
        /// </summary>
        [HttpGet("payment-status/{orderCode}")]
        public async Task<IActionResult> CheckFacilityPaymentStatus(string orderCode)
        {
            if (string.IsNullOrEmpty(orderCode))
            {
                return BadRequest(new { success = false, message = "OrderCode không được để trống" });
            }

            try
            {
                var accountId = GetAccountId();
                
                // FacilityId sẽ được extract từ OrderCode trong service
                var result = await _paymentAccountService.CheckFacilityPaymentStatusAsync(orderCode);

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
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Transaction not found: {OrderCode}", orderCode);
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking facility payment status: {OrderCode}", orderCode);
                return StatusCode(500, new { success = false, message = "Có lỗi xảy ra khi kiểm tra trạng thái thanh toán" });
            }
        }

        #endregion
    }
}