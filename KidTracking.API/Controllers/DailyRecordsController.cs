using Contracts.DTOs.DailyRecord;
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
    public class DailyRecordsController : ControllerBase
    {
        private readonly IDailyRecordService _dailyRecordService;
        private readonly IChildService _childService;
        private readonly ILogger<DailyRecordsController> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public DailyRecordsController(
            IDailyRecordService dailyRecordService,
            IUnitOfWork unitOfWork,
            IChildService childService,
            ILogger<DailyRecordsController> logger)
        {
            _dailyRecordService = dailyRecordService ?? throw new ArgumentNullException(nameof(dailyRecordService));
            _childService = childService ?? throw new ArgumentNullException(nameof(childService));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private async Task<bool> ValidateChildAccess(int childId)
        {
            try
            {
                var currentAccountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentAccountIdClaim) || !int.TryParse(currentAccountIdClaim, out int currentAccountId))
                {
                    _logger.LogWarning("Invalid token for ValidateChildAccess request");
                    return false;
                }

                // Admin và FacilityStaff có quyền truy cập tất cả
                if (User.IsInRole("Admin") || User.IsInRole("FacilityStaff"))
                {
                    return true;
                }

                // Tra cứu MemberId từ AccountId
                var memberRepository = _unitOfWork.GetRepository<Member>();
                var member = await memberRepository.GetAsync(m => m.AccountId == currentAccountId);
                if (member == null)
                {
                    _logger.LogWarning($"No Member found for AccountId {currentAccountId}");
                    return false;
                }

                var currentMemberId = member.MemberId;

                // Kiểm tra nếu người dùng là phụ huynh (Member) của trẻ
                var childRepository = _unitOfWork.GetRepository<Child>();
                var child = await childRepository.GetAsync(c => c.ChildId == childId && c.MemberId == currentMemberId);
                return child != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error validating child access for childId {childId}");
                return false;
            }
        }

        [HttpGet("child/{childId}")]
        public async Task<IActionResult> GetAllDailyRecordsByChildId(int childId)
        {
            try
            {
                if (!await ValidateChildAccess(childId))
                {
                    return Forbid("Bạn không có quyền xem thông tin này");
                }

                var records = await _dailyRecordService.GetAllDailyRecordsByChildIdAsync(childId);
                return Ok(records);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting daily records for child {childId}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("{recordId}")]
        public async Task<IActionResult> GetDailyRecordById(int recordId)
        {
            try
            {
                var record = await _dailyRecordService.GetDailyRecordByIdAsync(recordId);

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
                _logger.LogError(ex, $"Error getting daily record {recordId}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateDailyRecord([FromBody] CreateDailyRecordDTO recordDTO)
        {
            try
            {
                if (!await ValidateChildAccess(recordDTO.ChildId))
                {
                    return Forbid("Bạn không có quyền thực hiện hành động này");
                }

                var record = await _dailyRecordService.CreateDailyRecordAsync(recordDTO);
                return CreatedAtAction(nameof(GetDailyRecordById), new { recordId = record.DailyRecordId }, record);
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
                _logger.LogError(ex, $"Error creating daily record for child {recordDTO.ChildId}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPut("{recordId}")]
        public async Task<IActionResult> UpdateDailyRecord(int recordId, [FromBody] UpdateDailyRecordDTO recordDTO)
        {
            try
            {
                var existingRecord = await _dailyRecordService.GetDailyRecordByIdAsync(recordId);
                if (!await ValidateChildAccess(existingRecord.ChildId))
                {
                    return Forbid("Bạn không có quyền thực hiện hành động này");
                }

                var record = await _dailyRecordService.UpdateDailyRecordAsync(recordId, recordDTO);
                return Ok(record);
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
                _logger.LogError(ex, $"Error updating daily record {recordId}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpDelete("{recordId}")]
        public async Task<IActionResult> DeleteDailyRecord(int recordId)
        {
            try
            {
                var existingRecord = await _dailyRecordService.GetDailyRecordByIdAsync(recordId);
                if (!await ValidateChildAccess(existingRecord.ChildId))
                {
                    return Forbid("Bạn không có quyền thực hiện hành động này");
                }

                var result = await _dailyRecordService.DeleteDailyRecordAsync(recordId);
                return Ok(new { success = result });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting daily record {recordId}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}