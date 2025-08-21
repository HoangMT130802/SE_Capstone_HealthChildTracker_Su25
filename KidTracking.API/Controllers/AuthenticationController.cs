using Contracts.DTOs.Authentication;
using Contracts.DTOs.Member;
using Contracts.DTOs.FacilityStaff;
using Services;
using Services.Interfaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BCrypt.Net;
using Repositories.Interfaces;
using System.Security.Claims;
using Repositories.Models.QueryModels;

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

        [HttpPost("login-staff")]
        [AllowAnonymous]
        public async Task<ActionResult<StaffResponseDTO>> LoginStaff([FromBody] LoginRequestDTO request)
        {
            try
            {
                var response = await _authService.LoginStaffAsync(request);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning($"Staff login failed: {ex.Message}");
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Staff login error: {ex.Message}");
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

        [HttpPost("create-manager")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<StaffResponseDTO>> CreateManager([FromBody] CreateManagerDTO request)
        {
            try
            {
                var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserIdClaim) || !int.TryParse(currentUserIdClaim, out int currentUserId))
                {
                    return Unauthorized(new { message = "Token không hợp lệ" });
                }

                var response = await _authService.CreateManagerAsync(request, currentUserId);
                return CreatedAtAction(nameof(Login), new { accountName = response.AccountName }, response);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning($"Create manager unauthorized: {ex.Message}");
                return Forbid(ex.Message);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning($"Create manager validation failed: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Create manager failed: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Create manager error: {ex.Message}");
                return StatusCode(500, new { message = "Đã có lỗi xảy ra khi tạo tài khoản Manager" });
            }
        }

        [HttpPost("create-manager-with-facility")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ManagerWithFacilityResponseDTO>> CreateManagerWithFacility([FromBody] CreateManagerWithFacilityDTO request)
        {
            try
            {
                var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserIdClaim) || !int.TryParse(currentUserIdClaim, out int currentUserId))
                {
                    return Unauthorized(new { message = "Token không hợp lệ" });
                }

                var response = await _authService.CreateManagerWithFacilityAsync(request, currentUserId);
                return CreatedAtAction(nameof(Login), new { accountName = response.Manager.AccountName }, response);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning($"Create manager with facility unauthorized: {ex.Message}");
                return Forbid(ex.Message);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning($"Create manager with facility validation failed: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Create manager with facility failed: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Create manager with facility error: {ex.Message}");
                return StatusCode(500, new { message = "Đã có lỗi xảy ra khi tạo Manager và cơ sở" });
            }
        }

        [HttpPost("create-staff")]
        [Authorize(Roles = "FacilityStaff")]
        public async Task<ActionResult<StaffResponseDTO>> CreateStaff([FromBody] CreateStaffDTO request)
        {
            try
            {
                var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserIdClaim) || !int.TryParse(currentUserIdClaim, out int currentUserId))
                {
                    return Unauthorized(new { message = "Token không hợp lệ" });
                }

                var response = await _authService.CreateStaffAsync(request, currentUserId);
                return CreatedAtAction(nameof(Login), new { accountName = response.AccountName }, response);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning($"Create staff unauthorized: {ex.Message}");
                return Forbid(ex.Message);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning($"Create staff validation failed: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Create staff failed: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Create staff error: {ex.Message}");
                return StatusCode(500, new { message = "Đã có lỗi xảy ra khi tạo tài khoản Staff/Doctor" });
            }
        }
        [HttpPut("update-member-profile")]
        [Authorize(Roles = "Member")]
        public async Task<ActionResult<MemberInfoResponseDTO>> UpdateMemberInfo([FromBody] UpdateMemberInfoDTO request)
        {
            try
            {
                var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserIdClaim) || !int.TryParse(currentUserIdClaim, out int currentUserId))
                {
                    return Unauthorized(new { message = "Token không hợp lệ" });
                }

                var response = await _authService.UpdateMemberInfoAsync(request, currentUserId);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning($"Update user info unauthorized: {ex.Message}");
                return Forbid(ex.Message);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning($"Update user info validation failed: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Update user info failed: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Update user info error: {ex.Message}");
                return StatusCode(500, new { message = "Đã có lỗi xảy ra khi cập nhật thông tin người dùng" });
            }
        }

        [HttpPut("update-facility-staff-info")]
        [Authorize(Roles = "Admin,FacilityStaff")]
        public async Task<ActionResult<FacilityStaffInfoResponseDTO>> UpdateFacilityStaffInfo([FromBody] UpdateFacilityStaffInfoDTO request)
        {
            try
            {
                var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserIdClaim) || !int.TryParse(currentUserIdClaim, out int currentUserId))
                {
                    return Unauthorized(new { message = "Token không hợp lệ" });
                }

                var response = await _authService.UpdateFacilityStaffInfoAsync(request, currentUserId);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning($"Update staff info unauthorized: {ex.Message}");
                return Forbid(ex.Message);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning($"Update staff info validation failed: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Update staff info failed: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Update staff info error: {ex.Message}");
                return StatusCode(500, new { message = "Đã có lỗi xảy ra khi cập nhật thông tin staff" });
            }
        }

        [HttpPut("ban-user")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UserResponseDTO>> BanUser([FromBody] BanUserRequestDTO request)
        {
            try
            {
                var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserIdClaim) || !int.TryParse(currentUserIdClaim, out int currentUserId))
                {
                    return Unauthorized(new { message = "Token không hợp lệ" });
                }

                var response = await _authService.BanUserAsync(request, currentUserId);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning($"Ban user unauthorized: {ex.Message}");
                return Forbid(ex.Message);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning($"Ban user validation failed: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Ban user failed: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ban user error: {ex.Message}");
                return StatusCode(500, new { message = "Đã có lỗi xảy ra khi ban/unban tài khoản" });
            }
        }

        [HttpDelete("delete-staff/{staffId}")]
        [Authorize(Roles = "FacilityStaff")]
        public async Task<ActionResult> DeleteStaff(int staffId)
        {
            try
            {
                var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserIdClaim) || !int.TryParse(currentUserIdClaim, out int currentUserId))
                {
                    return Unauthorized(new { message = "Token không hợp lệ" });
                }

                var result = await _authService.DeleteStaffAsync(staffId, currentUserId);
                if (result)
                {
                    return Ok(new { message = "Xóa staff/doctor thành công" });
                }
                return BadRequest(new { message = "Không thể xóa staff/doctor" });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning($"Delete staff unauthorized: {ex.Message}");
                return Forbid(ex.Message);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning($"Delete staff validation failed: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Delete staff failed: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Delete staff error: {ex.Message}");
                return StatusCode(500, new { message = "Đã có lỗi xảy ra khi xóa staff/doctor" });
            }
        }
        [HttpGet("members")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(QueryResultModel<List<MemberDTO>>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<QueryResultModel<List<MemberDTO>>>> GetAllMembers(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10)
        {
            try
            {
                var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserIdClaim) || !int.TryParse(currentUserIdClaim, out int currentUserId))
                {
                    _logger.LogWarning("Invalid token for GetAllMembers request");
                    return Unauthorized(new { message = "Token không hợp lệ" });
                }

                var result = await _authService.GetAllMembersAsync(currentUserId, pageIndex, pageSize);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning($"Get all members unauthorized: {ex.Message}");
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Get all members error: {ex.Message}");
                return StatusCode(500, new { message = "Đã có lỗi xảy ra khi lấy danh sách thành viên" });
            }
        }

        [HttpPost("send-verification-email")]
        [AllowAnonymous]
        public async Task<ActionResult> SendVerificationEmail([FromBody] ResendVerificationRequestDTO request)
        {
            try
            {
                await _authService.SendVerificationEmailAsync(request.Email);
                return Ok(new { message = "Email xác thực đã được gửi" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Send verification email failed: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Send verification email error: {ex.Message}");
                return StatusCode(500, new { message = "Đã có lỗi xảy ra khi gửi email xác thực" });
            }
        }



        [HttpPost("complete-registration")]
        [AllowAnonymous]
        public async Task<ActionResult<UserResponseDTO>> CompleteRegistration([FromBody] VerifyEmailRequestDTO request)
        {
            try
            {
                // Inject IEmailService để lấy thông tin đăng ký
                var emailService = HttpContext.RequestServices.GetRequiredService<Services.Interfaces.IEmailService>();
                
                // Lấy thông tin đăng ký từ cache
                var registrationData = await emailService.GetRegistrationDataAsync(request.Email, request.OtpCode);
                if (registrationData == null)
                {
                    return BadRequest(new { message = "Mã OTP không hợp lệ hoặc đã hết hạn" });
                }

                // Xác thực OTP trực tiếp qua EmailService
                var isValidOtp = await emailService.VerifyOtpCodeAsync(request.Email, request.OtpCode, "Registration");
                if (!isValidOtp)
                {
                    return BadRequest(new { message = "Mã OTP không hợp lệ hoặc đã hết hạn" });
                }

                // Hoàn tất đăng ký với thông tin từ cache
                var registerRequest = new RegisterRequestDTO
                {
                    AccountName = registrationData.AccountName,
                    Password = registrationData.Password,
                    Email = registrationData.Email,
                    FullName = registrationData.FullName,
                    Phone = registrationData.Phone,
                    Address = registrationData.Address
                };

                var response = await _authService.CompleteRegistrationAsync(registerRequest);
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning($"Complete registration validation failed: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Complete registration failed: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Complete registration error: {ex.Message}");
                return StatusCode(500, new { message = "Đã có lỗi xảy ra khi hoàn tất đăng ký" });
            }
        }

        [HttpPost("resend-verification")]
        [AllowAnonymous]
        public async Task<ActionResult> ResendVerification([FromBody] ResendVerificationRequestDTO request)
        {
            try
            {
                await _authService.ResendVerificationEmailAsync(request);
                return Ok(new { message = "Email xác thực đã được gửi lại" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Resend verification failed: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Resend verification error: {ex.Message}");
                return StatusCode(500, new { message = "Đã có lỗi xảy ra khi gửi lại email xác thực" });
            }
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDTO request)
        {
            try
            {
                await _authService.SendForgotPasswordEmailAsync(request);
                return Ok(new { message = "Email khôi phục mật khẩu đã được gửi" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Forgot password failed: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Forgot password error: {ex.Message}");
                return StatusCode(500, new { message = "Đã có lỗi xảy ra khi gửi email khôi phục mật khẩu" });
            }
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordRequestDTO request)
        {
            try
            {
                await _authService.ResetPasswordAsync(request);
                return Ok(new { message = "Mật khẩu đã được đặt lại thành công" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Reset password failed: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Reset password error: {ex.Message}");
                return StatusCode(500, new { message = "Đã có lỗi xảy ra khi đặt lại mật khẩu" });
            }
        }
    }
}