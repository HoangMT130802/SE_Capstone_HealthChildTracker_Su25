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
            try
            {
                _logger.LogInformation("=== DEBUG GetCurrentAccountId ===");
                _logger.LogInformation($"User.Identity.IsAuthenticated: {User.Identity?.IsAuthenticated}");
                _logger.LogInformation($"User.Identity.AuthenticationType: {User.Identity?.AuthenticationType}");
                _logger.LogInformation($"User.Identity.Name: {User.Identity?.Name}");
                _logger.LogInformation($"Claims count: {User.Claims?.Count()}");
                
         
                if (User.Claims?.Any() == true)
                {
                    _logger.LogInformation($"All user claims: {string.Join(", ", User.Claims.Select(c => $"{c.Type}={c.Value}"))}");
                }
                else
                {
                    _logger.LogWarning("No claims found in User object!");
                }
                
                var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation($"NameIdentifier claim: {accountIdClaim}");
                
        
                var customAccountIdClaim = User.FindFirst("AccountId")?.Value;
                _logger.LogInformation($"Custom AccountId claim: {customAccountIdClaim}");
                
                if (string.IsNullOrEmpty(accountIdClaim) || !int.TryParse(accountIdClaim, out int accountId))
                {
                    _logger.LogError($"Failed to parse AccountId from claims. NameIdentifier: {accountIdClaim}");
                    throw new UnauthorizedAccessException("Không thể xác định account ID từ token");
                }
                
                _logger.LogInformation($"Successfully extracted AccountId: {accountId}");
                return accountId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in GetCurrentAccountId");
                throw;
            }
        }

        private bool ValidateAdminAccess()
        {
            return User.IsInRole("Admin");
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
        [AllowAnonymous]
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

        /// <summary>
        /// API public để lấy thông tin child mà không cần check account ownership
        /// </summary>
        [HttpGet("public/{childId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetChildByIdPublic(int childId)
        {
            try
            {
                var child = await _childService.GetChildByIdPublicAsync(childId);
                return Ok(child);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting public child {childId}");
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

        [HttpPost("with-growth-record")]
        public async Task<IActionResult> CreateChildWithGrowthRecord([FromBody] CreateChildWithGrowthRecordDTO createDTO)
        {
            try
            {
                var accountId = GetCurrentAccountId();
                var result = await _childService.CreateChildWithGrowthRecordAsync(accountId, createDTO);
                return CreatedAtAction(nameof(GetChildById), new { childId = result.Child.ChildId }, result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating child with growth record");
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
