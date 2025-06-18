using BusinessLogic.DTOs.Children;
using BusinessLogic.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace HealthChildTracker_API.Controllers
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

        private bool ValidateUserAccess(int userId)
        {
            var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserIdClaim) || !int.TryParse(currentUserIdClaim, out int currentUserId))
            {
                return false;
            }
            return currentUserId == userId || User.IsInRole("Admin") || User.IsInRole("Doctor");
        }

        [HttpGet("{userId}/Get children by userId")]
        public async Task<IActionResult> GetAllChildrenByUserId(int userId)
        {
            try
            {
                if (!ValidateUserAccess(userId))
                {
                    return Forbid("Bạn không có quyền xem thông tin này");
                }

                var children = await _childService.GetAllChildrenByUserIdAsync(userId);
                return Ok(children);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting children for user {userId}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("{childId}/user/{userId}/Get child by childId")]
        public async Task<IActionResult> GetChildById(int childId, int userId)
        {
            try
            {
                if (!ValidateUserAccess(userId))
                {
                    return Forbid("Bạn không có quyền xem thông tin này");
                }

                var child = await _childService.GetChildByIdAsync(childId, userId);
                return Ok(child);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting child {childId} for user {userId}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("user/{userId}/Create new child")]
        public async Task<IActionResult> CreateChild(int userId, [FromBody] CreateChildDTO childDTO)
        {
            try
            {
                if (!ValidateUserAccess(userId))
                {
                    return Forbid("Bạn không có quyền thực hiện hành động này");
                }

                var child = await _childService.CreateChildAsync(userId, childDTO);
                return CreatedAtAction(nameof(GetChildById), new { childId = child.ChildId, userId }, child);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating child for user {userId}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPut("{childId}/user/{userId}/Update child")]
        public async Task<IActionResult> UpdateChild(int childId, int userId, [FromBody] UpdateChildDTO childDTO)
        {
            try
            {
                if (!ValidateUserAccess(userId))
                {
                    return Forbid("Bạn không có quyền thực hiện hành động này");
                }

                var child = await _childService.UpdateChildAsync(childId, userId, childDTO);
                return Ok(child);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating child {childId} for user {userId}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpDelete("SoftDelete{childId}/user/{userId}")]
        public async Task<IActionResult> DeleteChild(int childId, int userId)
        {
            try
            {
                if (!ValidateUserAccess(userId))
                {
                    return Forbid("Bạn không có quyền thực hiện hành động này");
                }

                var result = await _childService.SoftDeleteChildAsync(childId, userId);
                return Ok(new { success = result });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting child {childId} for user {userId}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpDelete("harddelete/{childId}/user/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> HardDeleteChild(int childId, int userId)
        {
            try
            {
                if (!ValidateUserAccess(userId))
                {
                    return Forbid("Bạn không có quyền thực hiện hành động này");
                }

                var result = await _childService.HardDeleteChildAsync(childId, userId);
                return Ok(new { success = result, message = "Child record has been permanently deleted" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error hard deleting child {childId} for user {userId}");
                return StatusCode(500, new { message = "Internal server error occurred while trying to delete the child record" });
            }
        }
    }
}
