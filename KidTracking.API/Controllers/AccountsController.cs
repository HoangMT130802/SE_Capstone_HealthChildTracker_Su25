using Contracts.DTOs.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using System.Security.Claims;

namespace KidTracking.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AccountsController : ControllerBase
    {
        private readonly IAccountService _accountService;
        private readonly ILogger<AccountsController> _logger;

        public AccountsController(IAccountService accountService, ILogger<AccountsController> logger)
        {
            _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private int GetCurrentAccountId()
        {
            var user = HttpContext.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                var accountIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(accountIdClaim, out int accountId))
                {
                    return accountId;
                }
            }
            return 0;
        }
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentAccount()
        {
            try
            {
                var currentUserId = GetCurrentAccountId();
                if (currentUserId == 0)
                {
                    return Unauthorized(new { message = "Không thể xác định AccountId từ token" });
                }

                var account = await _accountService.GetCurrentAccountAsync(currentUserId);
                return Ok(account);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current account");
                return StatusCode(500, new { message = "Lỗi hệ thống khi lấy thông tin tài khoản" });
            }
        }

        [HttpPut("info")]
        public async Task<IActionResult> UpdateAccount([FromForm] UpdateAccountDTO request)
        {
            try
            {
                var currentUserId = GetCurrentAccountId();
                if (currentUserId == 0)
                {
                    return Unauthorized(new { message = "Không thể xác định AccountId từ token" });
                }

                var response = await _accountService.UpdateAccountAsync(request, currentUserId);
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating account");
                return StatusCode(500, new { message = "Lỗi hệ thống khi cập nhật thông tin tài khoản" });
            }
        }
    }
}
