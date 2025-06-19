using Contracts.DTOs.Authentication;
using Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BCrypt.Net;
using Repositories.Interfaces;

namespace KidTracking.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthenticationController : ControllerBase
    {
        private readonly IAuthenticationService _authService;
        private readonly ILogger<AuthenticationController> _logger;

        public AuthenticationController(
            IAuthenticationService authService,
            ILogger<AuthenticationController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<UserResponseDTO>> Login([FromBody] LoginRequestDTO request)
        {
            try
            {
                var response = await _authService.LoginAsync(request);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning($"Login failed: {ex.Message}");
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Login error: {ex.Message}");
                return BadRequest(new { message = "Đã có lỗi xảy ra khi đăng nhập" });
            }
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<UserResponseDTO>> Register([FromBody] RegisterRequestDTO request)
        {
            try
            {
                var response = await _authService.RegisterAsync(request);
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning($"Registration validation failed: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Registration failed: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Registration error: {ex.Message}");
                return BadRequest(new { message = "Đã có lỗi xảy ra khi đăng ký" });
            }
        }

        [HttpPost("debug/test-password")]
        [AllowAnonymous]
        public IActionResult TestPasswordHash([FromBody] dynamic request)
        {
            try
            {
                string password = request.password;
                string hash = BCrypt.Net.BCrypt.HashPassword(password);
                bool verification = BCrypt.Net.BCrypt.Verify(password, hash);
                
                return Ok(new
                {
                    original = password,
                    hashed = hash,
                    verification = verification
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("debug/check-member")]
        [Authorize]
        public async Task<IActionResult> CheckMember()
        {
            try
            {
                var accountIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(accountIdClaim, out int accountId))
                {
                    return BadRequest(new { message = "Invalid AccountId in token" });
                }

                // Check Account exists
                var unitOfWork = HttpContext.RequestServices.GetRequiredService<IUnitOfWork>();
                var accountRepo = unitOfWork.GetRepository<Repositories.Entities.Account>();
                var account = await accountRepo.GetAsync(a => a.AccountId == accountId);

                // Check Member exists  
                var memberRepo = unitOfWork.GetRepository<Repositories.Entities.Member>();
                var member = await memberRepo.GetAsync(m => m.AccountId == accountId);

                return Ok(new
                {
                    accountId = accountId,
                    accountExists = account != null,
                    accountInfo = account != null ? new { account.AccountName, account.Email, account.Role } : null,
                    memberExists = member != null,
                    memberInfo = member != null ? new { member.MemberId, member.FullName, member.PhoneNumber } : null
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message, details = ex.ToString() });
            }
        }
    }
}