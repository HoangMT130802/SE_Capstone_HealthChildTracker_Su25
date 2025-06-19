using Contracts.DTOs.Child;
using Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace KidTracking.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChildrenController : ControllerBase
    {
        private readonly IChildService _childService;
        private readonly ILogger<ChildrenController> _logger;

        public ChildrenController(IChildService childService, ILogger<ChildrenController> logger)
        {
            _childService = childService ?? throw new ArgumentNullException(nameof(childService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private int GetCurrentAccountId()
        {
            var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountIdClaim) || !int.TryParse(accountIdClaim, out int accountId))
            {
                throw new UnauthorizedAccessException("Không thể xác định account ID từ token");
            }
            return accountId;
        }

        private bool ValidateAdminAccess()
        {
            return User.IsInRole("Admin") || User.IsInRole("Manager");
        }

        [HttpGet("my-children")]
        public async Task<IActionResult> GetMyChildren()
        {
            try
            {
                var accountId = GetCurrentAccountId();
                var children = await _childService.GetAllChildrenByAccountIdAsync(accountId);
                return Ok(children);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting children for current account");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("{childId}")]
        public async Task<IActionResult> GetChildById(int childId)
        {
            try
            {
                var accountId = GetCurrentAccountId();
                var child = await _childService.GetChildByIdAsync(childId, accountId);
                return Ok(child);
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
                _logger.LogError(ex, $"Error getting child {childId}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateChild([FromBody] CreateChildDTO childDTO)
        {
            try
            {
                var accountId = GetCurrentAccountId();
                var child = await _childService.CreateChildAsync(accountId, childDTO);
                return CreatedAtAction(nameof(GetChildById), new { childId = child.ChildId }, child);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating child");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPut("{childId}")]
        public async Task<IActionResult> UpdateChild(int childId, [FromBody] UpdateChildDTO childDTO)
        {
            try
            {
                var accountId = GetCurrentAccountId();
                var child = await _childService.UpdateChildAsync(childId, accountId, childDTO);
                return Ok(child);
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
                _logger.LogError(ex, $"Error updating child {childId}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpDelete("{childId}")]
        public async Task<IActionResult> SoftDeleteChild(int childId)
        {
            try
            {
                var accountId = GetCurrentAccountId();
                var result = await _childService.SoftDeleteChildAsync(childId, accountId);
                return Ok(new { success = result, message = "Child đã được soft delete" });
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
                _logger.LogError(ex, $"Error soft deleting child {childId}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpDelete("{childId}/hard")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> HardDeleteChild(int childId)
        {
            try
            {
                var accountId = GetCurrentAccountId();
                var result = await _childService.HardDeleteChildAsync(childId, accountId);
                return Ok(new { success = result, message = "Child record đã được xóa vĩnh viễn" });
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
                _logger.LogError(ex, $"Error hard deleting child {childId}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // Admin endpoints
        [HttpGet("admin/account/{accountId}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetChildrenByAccountId(int accountId)
        {
            try
            {
                var children = await _childService.GetAllChildrenByAccountIdAsync(accountId);
                return Ok(children);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting children for account {accountId}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}
