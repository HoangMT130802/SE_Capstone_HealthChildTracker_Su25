using Contracts.DTOs.Membership;
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
    public class MembershipsController : ControllerBase
    {
        private readonly IMembershipService _membershipService;
        private readonly ILogger<MembershipsController> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public MembershipsController(IMembershipService membershipService, ILogger<MembershipsController> logger, IUnitOfWork unitOfWork)
        {
            _membershipService = membershipService ?? throw new ArgumentNullException(nameof(membershipService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        private async Task<bool> ValidateAdminAccess()
        {
            try
            {
                var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserIdClaim) || !int.TryParse(currentUserIdClaim, out int currentUserId))
                {
                    return false;
                }

                var accountRepository = _unitOfWork.GetRepository<Account>();
                var account = await accountRepository.GetAsync(a => a.AccountId == currentUserId);
                return account != null && account.Role == "Admin";
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Lấy tất cả memberships (Admin only)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllMemberships([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10, [FromQuery] bool? status = null)
        {
            try
            {
                if (!await ValidateAdminAccess())
                {
                    return Forbid("Chỉ Admin mới có quyền xem danh sách membership");
                }

                var result = await _membershipService.GetAllMembershipsAsync(pageIndex, pageSize, status);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách membership");
                return StatusCode(500, new { message = "Lỗi server nội bộ" });
            }
        }

        /// <summary>
        /// Lấy membership theo ID (Admin only)
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetMembershipById(int id)
        {
            try
            {
                if (!await ValidateAdminAccess())
                {
                    return Forbid("Chỉ Admin mới có quyền xem chi tiết membership");
                }

                var membership = await _membershipService.GetMembershipByIdAsync(id);
                return Ok(membership);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy membership {id}");
                return StatusCode(500, new { message = "Lỗi server nội bộ" });
            }
        }

        /// <summary>
        /// Tạo membership mới (Admin only)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateMembership([FromBody] CreateMembershipDTO createDto)
        {
            try
            {
                if (!await ValidateAdminAccess())
                {
                    return Forbid("Chỉ Admin mới có quyền tạo membership");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var membership = await _membershipService.CreateMembershipAsync(createDto);
                return CreatedAtAction(nameof(GetMembershipById), new { id = membership.MembershipId }, membership);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo membership");
                return StatusCode(500, new { message = "Lỗi server nội bộ" });
            }
        }

        /// <summary>
        /// Cập nhật membership (Admin only)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateMembership(int id, [FromBody] UpdateMembershipDTO updateDto)
        {
            try
            {
                if (!await ValidateAdminAccess())
                {
                    return Forbid("Chỉ Admin mới có quyền cập nhật membership");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var membership = await _membershipService.UpdateMembershipAsync(id, updateDto);
                return Ok(membership);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi cập nhật membership {id}");
                return StatusCode(500, new { message = "Lỗi server nội bộ" });
            }
        }

        /// <summary>
        /// Xóa membership (Admin only)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteMembership(int id)
        {
            try
            {
                if (!await ValidateAdminAccess())
                {
                    return Forbid("Chỉ Admin mới có quyền xóa membership");
                }

                await _membershipService.DeleteMembershipAsync(id);
                return NoContent();
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
                _logger.LogError(ex, $"Lỗi khi xóa membership {id}");
                return StatusCode(500, new { message = "Lỗi server nội bộ" });
            }
        }

        /// <summary>
        /// Toggle trạng thái membership (Admin only)
        /// </summary>
        [HttpPatch("{id}/toggle-status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleMembershipStatus(int id)
        {
            try
            {
                if (!await ValidateAdminAccess())
                {
                    return Forbid("Chỉ Admin mới có quyền thay đổi trạng thái membership");
                }

                await _membershipService.ToggleMembershipStatusAsync(id);
                return Ok(new { message = "Đã thay đổi trạng thái membership thành công" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi toggle trạng thái membership {id}");
                return StatusCode(500, new { message = "Lỗi server nội bộ" });
            }
        }

        /// <summary>
        /// Lấy danh sách membership đang hoạt động (Public cho Guest/Member)
        /// </summary>
        [HttpGet("active")]
        [AllowAnonymous]
        public async Task<IActionResult> GetActiveMemberships()
        {
            try
            {
                var memberships = await _membershipService.GetActiveMembershipsAsync();
                return Ok(memberships);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách membership đang hoạt động");
                return StatusCode(500, new { message = "Lỗi server nội bộ" });
            }
        }
    }
} 