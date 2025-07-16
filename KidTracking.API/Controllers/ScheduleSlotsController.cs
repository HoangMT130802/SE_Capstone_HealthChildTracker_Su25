using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Contracts.DTOs.FacilitySchedule;
using Services.Interfaces;
using System.Security.Claims;


namespace KidTracking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ScheduleSlotsController : ControllerBase
    {
        private readonly IScheduleSlotService _scheduleSlotService;
        private readonly ILogger<ScheduleSlotsController> _logger;

        public ScheduleSlotsController(IScheduleSlotService scheduleSlotService, ILogger<ScheduleSlotsController> logger)
        {
            _scheduleSlotService = scheduleSlotService;
            _logger = logger;
        }

        #region Helper Methods
        private int GetFacilityIdFromToken()
        {
            var facilityIdClaim = User.FindFirst("FacilityId")?.Value;
            if (string.IsNullOrEmpty(facilityIdClaim) || !int.TryParse(facilityIdClaim, out int facilityId))
            {
                throw new UnauthorizedAccessException("Không tìm thấy FacilityId trong token");
            }
            return facilityId;
        }

        private string GetUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value ?? "";
        }
        #endregion

        // ✅ GET: api/scheduleslots (Admin only)
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<ScheduleSlotDTO>>> GetAllSlots()
        {
            try
            {
                var slots = await _scheduleSlotService.GetAllSlotsAsync();
                return Ok(slots);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy tất cả slots");
                return StatusCode(500, "Có lỗi xảy ra khi lấy danh sách slots");
            }
        }

        // ✅ GET: api/scheduleslots/my-facility (Manager,Staff,Doctor,Member)
        [HttpGet("my-facility")]
        [Authorize(Roles = "Manager,FacilityStaff,Doctor,Member")]
        public async Task<ActionResult<List<ScheduleSlotDTO>>> GetMyFacilitySlots()
        {
            try
            {
                var facilityId = GetFacilityIdFromToken();
                var slots = await _scheduleSlotService.GetSlotsByFacilityAsync(facilityId);
                return Ok(slots);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access to facility slots");
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy slots theo facility");
                return StatusCode(500, "Có lỗi xảy ra khi lấy danh sách slots");
            }
        }

        // ✅ GET: api/scheduleslots/facility/{id} (Admin,Member)
        [HttpGet("facility/{id}")]
        [Authorize(Roles = "Admin,Member")]
        public async Task<ActionResult<List<ScheduleSlotDTO>>> GetSlotsByFacility(int id)
        {
            try
            {
                var slots = await _scheduleSlotService.GetSlotsByFacilityAsync(id);
                return Ok(slots);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy slots theo facility: {FacilityId}", id);
                return StatusCode(500, "Có lỗi xảy ra khi lấy danh sách slots");
            }
        }

        // ✅ GET: api/scheduleslots/{id} (All roles với filtering)
        [HttpGet("{id}")]
        public async Task<ActionResult<ScheduleSlotDTO>> GetSlotById(int id)
        {
            try
            {
                var userRole = GetUserRole();
                
                if (userRole == "Admin" || userRole == "Member")
                {
                    // Admin và Member có thể xem tất cả slots
                    var slot = await _scheduleSlotService.GetSlotByIdAsync(id);
                    return Ok(slot);
                }
                else
                {
                    // Manager, FacilityStaff, Doctor chỉ xem slots của facility mình
                    var facilityId = GetFacilityIdFromToken();
                    var slot = await _scheduleSlotService.GetSlotByIdWithFacilityCheckAsync(id, facilityId);
                    return Ok(slot);
                }
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Slot not found: {SlotId}", id);
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access to slot: {SlotId}", id);
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy slot theo ID: {SlotId}", id);
                return StatusCode(500, "Có lỗi xảy ra khi lấy slot");
            }
        }

        // ✅ POST: api/scheduleslots (Manager only)
        [HttpPost]
        [Authorize(Roles = "Manager")]
        public async Task<ActionResult<List<ScheduleSlotDTO>>> CreateSlot([FromBody] CreateScheduleSlotDTO createDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // ✅ Validation
                if (!createDto.IsValid())
                {
                    return BadRequest("Dữ liệu đầu vào không hợp lệ");
                }

                var facilityId = GetFacilityIdFromToken();
                var createdSlots = await _scheduleSlotService.CreateSlotAsync(createDto, facilityId);
                
                _logger.LogInformation("Tạo working hours thành công với {Count} slots", createdSlots.Count);
                return CreatedAtAction(nameof(GetSlotById), new { id = createdSlots.First().SlotId }, createdSlots);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid data for creating slot");
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access to create slot");
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo slot");
                return StatusCode(500, "Có lỗi xảy ra khi tạo slot");
            }
        }

        // ✅ PUT: api/scheduleslots/{id} (Manager only với facility check)
        [HttpPut("{id}")]
        [Authorize(Roles = "Manager")]
        public async Task<ActionResult<ScheduleSlotDTO>> UpdateSlot(int id, [FromBody] UpdateScheduleSlotDTO updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var facilityId = GetFacilityIdFromToken();
                var updatedSlot = await _scheduleSlotService.UpdateSlotAsync(id, updateDto, facilityId);
                
                _logger.LogInformation("Cập nhật slot thành công với ID: {SlotId}", id);
                return Ok(updatedSlot);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Slot not found for update: {SlotId}", id);
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access to update slot: {SlotId}", id);
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật slot: {SlotId}", id);
                return StatusCode(500, "Có lỗi xảy ra khi cập nhật slot");
            }
        }

        // ✅ DELETE: api/scheduleslots/{id} (Manager only với facility check)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Manager")]
        public async Task<ActionResult> DeleteSlot(int id)
        {
            try
            {
                var facilityId = GetFacilityIdFromToken();
                var result = await _scheduleSlotService.DeleteSlotAsync(id, facilityId);
                
                if (result)
                {
                    _logger.LogInformation("Xóa slot thành công với ID: {SlotId}", id);
                    return NoContent();
                }
                else
                {
                    return NotFound($"Slot với ID {id} không tồn tại");
                }
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Slot not found for deletion: {SlotId}", id);
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access to delete slot: {SlotId}", id);
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa slot: {SlotId}", id);
                return StatusCode(500, "Có lỗi xảy ra khi xóa slot");
            }
        }

        // ✅ PUT: api/scheduleslots/{id}/status (Manager only)
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Manager")]
        public async Task<ActionResult> UpdateSlotStatus(int id, [FromBody] string status)
        {
            try
            {
                var result = await _scheduleSlotService.UpdateSlotStatusAsync(id, status);
                
                if (result)
                {
                    _logger.LogInformation("Cập nhật trạng thái slot thành công với ID: {SlotId}", id);
                    return Ok(new { message = "Cập nhật trạng thái thành công" });
                }
                else
                {
                    return NotFound($"Slot với ID {id} không tồn tại");
                }
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Slot not found for status update: {SlotId}", id);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật trạng thái slot: {SlotId}", id);
                return StatusCode(500, "Có lỗi xảy ra khi cập nhật trạng thái");
            }
        }

        // ✅ DELETE: api/scheduleslots/batch (Manager only)
        [HttpDelete("batch")]
        [Authorize(Roles = "Manager")]
        public async Task<ActionResult> DeleteMultipleSlots([FromBody] List<int> slotIds)
        {
            try
            {
                var facilityId = GetFacilityIdFromToken();
                var result = await _scheduleSlotService.DeleteMultipleSlotsAsync(slotIds, facilityId);
                
                if (result)
                {
                    _logger.LogInformation("Xóa {Count} slots thành công", slotIds.Count);
                    return Ok(new { message = "Xóa slots thành công" });
                }
                else
                {
                    return BadRequest("Không thể xóa slots");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa multiple slots");
                return StatusCode(500, "Lỗi hệ thống");
            }
        }

        // ✅ Working Hours Group Management
        [HttpGet("working-hours-groups")]
        [Authorize(Roles = "Manager,FacilityStaff,Doctor")]
        public async Task<ActionResult<List<WorkingHoursGroupDTO>>> GetWorkingHoursGroups()
        {
            try
            {
                var facilityId = GetFacilityIdFromToken();
                var groups = await _scheduleSlotService.GetWorkingHoursGroupsByFacilityAsync(facilityId);
                
                _logger.LogInformation("Lấy {Count} working hours groups thành công cho facility: {FacilityId}", 
                    groups.Count, facilityId);
                
                return Ok(groups);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy working hours groups");
                return StatusCode(500, "Lỗi hệ thống");
            }
        }

        [HttpGet("working-hours-groups/{groupId}/slots")]
        [Authorize(Roles = "Manager,FacilityStaff,Doctor")]
        public async Task<ActionResult<List<ScheduleSlotDTO>>> GetSlotsByWorkingHoursGroup(string groupId)
        {
            try
            {
                var slots = await _scheduleSlotService.GetSlotsByWorkingHoursGroupIdAsync(groupId);
                
                _logger.LogInformation("Lấy {Count} slots cho working hours group: {GroupId}", 
                    slots.Count, groupId);
                
                return Ok(slots);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy slots cho working hours group: {GroupId}", groupId);
                return StatusCode(500, "Lỗi hệ thống");
            }
        }
    }
} 