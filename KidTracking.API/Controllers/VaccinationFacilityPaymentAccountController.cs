using Contracts.DTOs;
using Contracts.DTOs.VaccinationFacilityPaymentAccount;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using System;

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
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreatePaymentAccount([FromForm] CreateVaccinationFacilityPaymentAccountDto paymentAccountDto)
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
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdatePaymentAccount(int id, [FromForm] UpdateVaccinationFacilityPaymentAccountDto paymentAccountDto)
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
    }
}