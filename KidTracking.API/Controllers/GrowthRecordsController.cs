using BusinessLogic.DTOs.GrowthRecord;
using BusinessLogic.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HealthChildTracker_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class GrowthRecordsController : ControllerBase
    {
        private readonly IGrowthRecordService _growthRecordService;
        private readonly IChildService _childService;
        private readonly ILogger<GrowthRecordsController> _logger;

        public GrowthRecordsController(
            IGrowthRecordService growthRecordService, 
            IChildService childService,
            ILogger<GrowthRecordsController> logger)
        {
            _growthRecordService = growthRecordService ?? throw new ArgumentNullException(nameof(growthRecordService));
            _childService = childService ?? throw new ArgumentNullException(nameof(childService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private async Task<bool> ValidateChildAccess(int childId)
        {
            try
            {
                var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserIdClaim) || !int.TryParse(currentUserIdClaim, out int currentUserId))
                {
                    return false;
                }

                // Admin và Doctor có quyền truy cập tất cả
                if (User.IsInRole("Admin") || User.IsInRole("Doctor"))
                {
                    return true;
                }

                // User thường chỉ xem được thông tin của con mình
                var child = await _childService.GetChildByIdAsync(childId, currentUserId);
                return child != null;
            }
            catch
            {
                return false;
            }
        }

        [HttpGet("child/{childId}")]
        public async Task<IActionResult> GetAllGrowthRecordsByChildId(int childId)
        {
            try
            {
                if (!await ValidateChildAccess(childId))
                {
                    return Forbid("Bạn không có quyền xem thông tin này");
                }

                var records = await _growthRecordService.GetAllGrowthRecordsByChildIdAsync(childId);
                return Ok(records);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting growth records for child {childId}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("Get {recordId}")]
        public async Task<IActionResult> GetGrowthRecordById(int recordId)
        {
            try
            {
                var record = await _growthRecordService.GetGrowthRecordByIdAsync(recordId);
                
                if (!await ValidateChildAccess(record.ChildId))
                {
                    return Forbid("Bạn không có quyền xem thông tin này");
                }

                return Ok(record);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting growth record {recordId}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("Create new record")]
        public async Task<IActionResult> CreateGrowthRecord([FromBody] CreateGrowthRecordDTO recordDTO)
        {
            try
            {
                if (!await ValidateChildAccess(recordDTO.ChildId))
                {
                    return Forbid("Bạn không có quyền thực hiện hành động này");
                }

                var record = await _growthRecordService.CreateGrowthRecordAsync(recordDTO);
                return CreatedAtAction(nameof(GetGrowthRecordById), new { recordId = record.RecordId }, record);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating growth record for child {recordDTO.ChildId}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPut("{recordId}/Update record")]
        public async Task<IActionResult> UpdateGrowthRecord(int recordId, [FromBody] UpdateGrowthRecordDTO recordDTO)
        {
            try
            {
                var existingRecord = await _growthRecordService.GetGrowthRecordByIdAsync(recordId);
                if (!await ValidateChildAccess(existingRecord.ChildId))
                {
                    return Forbid("Bạn không có quyền thực hiện hành động này");
                }

                var record = await _growthRecordService.UpdateGrowthRecordAsync(recordId, recordDTO);
                return Ok(record);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating growth record {recordId}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpDelete("{recordId}/Delete")]
        public async Task<IActionResult> DeleteGrowthRecord(int recordId)
        {
            try
            {
                var existingRecord = await _growthRecordService.GetGrowthRecordByIdAsync(recordId);
                if (!await ValidateChildAccess(existingRecord.ChildId))
                {
                    return Forbid("Bạn không có quyền thực hiện hành động này");
                }

                var result = await _growthRecordService.DeleteGrowthRecordAsync(recordId);
                return Ok(new { success = result });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting growth record {recordId}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}
