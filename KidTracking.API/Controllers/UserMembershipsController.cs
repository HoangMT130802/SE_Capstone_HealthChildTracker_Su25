using Contracts.DTOs.UserMembership;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repositories.Entities;
using Repositories.Interfaces;
using Services.Interfaces;
using System.Security.Claims;

namespace KidTracking.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserMembershipsController : ControllerBase
    {
        private readonly IUserMembershipService _userMembershipService;
        private readonly ILogger<UserMembershipsController> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public UserMembershipsController(IUserMembershipService userMembershipService, ILogger<UserMembershipsController> logger, IUnitOfWork unitOfWork)
        {
            _userMembershipService = userMembershipService ?? throw new ArgumentNullException(nameof(userMembershipService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        private async Task<int?> GetCurrentAccountId()
        {
            try
            {
                var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserIdClaim) || !int.TryParse(currentUserIdClaim, out int currentUserId))
                {
                    return null;
                }
                return currentUserId;
            }
            catch
            {
                return null;
            }
        }

        private async Task<bool> ValidateAdminAccess()
        {
            try
            {
                var accountId = await GetCurrentAccountId();
                if (!accountId.HasValue) return false;

                var accountRepository = _unitOfWork.GetRepository<Account>();
                var account = await accountRepository.GetAsync(a => a.AccountId == accountId.Value);
                
                return account != null && account.Role == "Admin";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ValidateAdminAccess");
                return false;
            }
        }

        /// <summary>
        /// Đăng ký membership cho Member
        /// </summary>
        [HttpPost("subscribe")]
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> SubscribeMembership([FromBody] SubscribeMembershipDTO subscribeDto)
        {
            try
            {
                var accountId = await GetCurrentAccountId();
                if (!accountId.HasValue)
                {
                    return Unauthorized("Không thể xác định tài khoản người dùng");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _userMembershipService.SubscribeMembershipAsync(accountId.Value, subscribeDto);
                
                if (result.IsSuccess)
                {
                    _logger.LogInformation($"Member {accountId} successfully subscribed to membership {subscribeDto.MembershipId}");
                    // Trả về response đơn giản, bỏ các field dư thừa
                    return Ok(new 
                    {
                        isSuccess = result.IsSuccess,
                        message = result.Message
                    });
                }
                else
                {
                    _logger.LogWarning($"Failed to subscribe membership for member {accountId}: {result.Message}");
                    return BadRequest(new 
                    {
                        isSuccess = result.IsSuccess,
                        message = result.Message
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đăng ký membership cho Member");
                return StatusCode(500, new { message = "Lỗi server nội bộ" });
            }
        }

        /// <summary>
        /// Lấy danh sách membership của người dùng hiện tại
        /// </summary>
        [HttpGet("my-memberships")]
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> GetMyMemberships()
        {
            try
            {
                var accountId = await GetCurrentAccountId();
                if (!accountId.HasValue)
                {
                    return Unauthorized("Không thể xác định tài khoản người dùng");
                }

                var memberships = await _userMembershipService.GetUserMembershipsAsync(accountId.Value);
                return Ok(memberships);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách membership của người dùng");
                return StatusCode(500, new { message = "Lỗi server nội bộ" });
            }
        }

        /// <summary>
        /// Lấy membership đang hoạt động của người dùng hiện tại
        /// </summary>
        [HttpGet("my-active-membership")]
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> GetMyActiveMembership()
        {
            try
            {
                var accountId = await GetCurrentAccountId();
                if (!accountId.HasValue)
                {
                    return Unauthorized("Không thể xác định tài khoản người dùng");
                }

                var activeMembership = await _userMembershipService.GetActiveUserMembershipAsync(accountId.Value);
                
                if (activeMembership == null)
                {
                    return NotFound(new { message = "Không có membership đang hoạt động" });
                }

                return Ok(activeMembership);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy membership đang hoạt động");
                return StatusCode(500, new { message = "Lỗi server nội bộ" });
            }
        }

        /// <summary>
        /// Hủy membership
        /// </summary>
        [HttpPatch("{userMembershipId}/cancel")]
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> CancelMembership(int userMembershipId)
        {
            try
            {
                var accountId = await GetCurrentAccountId();
                if (!accountId.HasValue)
                {
                    return Unauthorized("Không thể xác định tài khoản người dùng");
                }

                await _userMembershipService.CancelUserMembershipAsync(userMembershipId, accountId.Value);
                return Ok(new { message = "Hủy membership thành công" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi hủy membership {userMembershipId}");
                return StatusCode(500, new { message = "Lỗi server nội bộ" });
            }
        }

        /// <summary>
        /// Gia hạn membership
        /// </summary>
        [HttpPatch("{userMembershipId}/renew")]
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> RenewMembership(int userMembershipId)
        {
            try
            {
                var accountId = await GetCurrentAccountId();
                if (!accountId.HasValue)
                {
                    return Unauthorized("Không thể xác định tài khoản người dùng");
                }

                await _userMembershipService.RenewUserMembershipAsync(userMembershipId, accountId.Value);
                return Ok(new { message = "Gia hạn membership thành công" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi gia hạn membership {userMembershipId}");
                return StatusCode(500, new { message = "Lỗi server nội bộ" });
            }
        }

        /// <summary>
        /// Lấy tất cả user memberships (Admin only)
        /// </summary>
        [HttpGet("all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUserMemberships([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] bool? status = null, [FromQuery] int? membershipId = null)
        {
            try
            {
                if (!await ValidateAdminAccess())
                {
                    return Forbid("Chỉ Admin mới có quyền xem tất cả user membership");
                }

                var result = await _userMembershipService.GetAllUserMembershipsAsync(pageIndex, pageSize, status, membershipId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy tất cả user memberships");
                return StatusCode(500, new { message = "Lỗi server nội bộ" });
            }
        }

        /// <summary>
        /// Lấy user memberships theo account ID (Admin only)
        /// </summary>
        [HttpGet("by-account/{accountId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUserMembershipsByAccountId(int accountId)
        {
            try
            {
                if (!await ValidateAdminAccess())
                {
                    return Forbid("Chỉ Admin mới có quyền xem user membership của tài khoản khác");
                }

                var memberships = await _userMembershipService.GetUserMembershipsAsync(accountId);
                return Ok(memberships);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy user memberships cho account {accountId}");
                return StatusCode(500, new { message = "Lỗi server nội bộ" });
            }
        }

        /// <summary>
        /// Test endpoint để debug authentication
        /// </summary>
        [HttpGet("debug/auth")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DebugAuth()
        {
            try
            {
                var claims = User.Claims.Select(c => new { Type = c.Type, Value = c.Value }).ToList();
                var accountId = await GetCurrentAccountId();
                var isAdmin = await ValidateAdminAccess();
                
                var accountRepository = _unitOfWork.GetRepository<Account>();
                Account account = null;
                if (accountId.HasValue)
                {
                    account = await accountRepository.GetAsync(a => a.AccountId == accountId.Value);
                }

                return Ok(new
                {
                    Claims = claims,
                    AccountId = accountId,
                    IsAdmin = isAdmin,
                    Account = account != null ? new 
                    { 
                        account.AccountId, 
                        account.AccountName, 
                        account.Role,
                        account.Email 
                    } : null,
                    IsAuthenticated = User.Identity.IsAuthenticated,
                    Identity = User.Identity.Name
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    Error = ex.Message,
                    StackTrace = ex.StackTrace
                });
            }
        }
    }
} 