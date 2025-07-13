using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Contracts.DTOs.FacilitySchedule;
using Services.Interfaces;
using System.Security.Claims;

namespace KidTracking.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // ✅ Yêu cầu authentication cho tất cả endpoints
    public class ScheduleSlotsController : ControllerBase
    {
        private readonly IScheduleSlotService _scheduleSlotService;
        private readonly ILogger<ScheduleSlotsController> _logger;

        public ScheduleSlotsController(IScheduleSlotService scheduleSlotService, ILogger<ScheduleSlotsController> logger)
        {
            _scheduleSlotService = scheduleSlotService;
            _logger = logger;
        }

        // ✅ Helper method để lấy FacilityId từ JWT token
        private int? GetFacilityIdFromToken()
        {
            var facilityIdClaim = User.FindFirst("FacilityId")?.Value;
            if (int.TryParse(facilityIdClaim, out int facilityId))
            {
                return facilityId;
            }
            return null;
        }

        // ✅ Helper method để lấy AccountId từ JWT token
        private int GetAccountIdFromToken()
        {
            var accountIdClaim = User.FindFirst("AccountId")?.Value;
            if (int.TryParse(accountIdClaim, out int accountId))
            {
                return accountId;
            }
            throw new UnauthorizedAccessException("Không thể xác định thông tin người dùng");
        }

        // ✅ Helper method để kiểm tra role
        private string GetUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value ?? "";
        }

        // ✅ GET tất cả slots (chỉ Admin)
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
                _logger.LogError(ex, "Lỗi khi lấy danh sách slots");
                return StatusCode(500, "Lỗi server");
            }
        }

        // ✅ GET slots của facility hiện tại (Manager/Staff/Member)
        [HttpGet("my-facility")]
        [Authorize(Roles = "Manager,FacilityStaff,Doctor,Member")]
        public async Task<ActionResult<List<ScheduleSlotDTO>>> GetMyFacilitySlots()
        {
            try
            {
                var facilityId = GetFacilityIdFromToken();
                if (!facilityId.HasValue)
                {
                    return BadRequest("Bạn chưa được gán vào facility nào");
                }

                var slots = await _scheduleSlotService.GetSlotsByFacilityAsync(facilityId.Value);
                return Ok(slots);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách slots của facility");
                return StatusCode(500, "Lỗi server");
            }
        }

        // ✅ GET slots theo facility ID (Admin/Member)
        [HttpGet("facility/{facilityId}")]
        [Authorize(Roles = "Admin,Member")]
        public async Task<ActionResult<List<ScheduleSlotDTO>>> GetSlotsByFacility(int facilityId)
        {
            try
            {
                var slots = await _scheduleSlotService.GetSlotsByFacilityAsync(facilityId);
                return Ok(slots);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách slots theo facility");
                return StatusCode(500, "Lỗi server");
            }
        }

        // ✅ GET slot theo ID với authorization
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Manager,FacilityStaff,Doctor,Member")]
        public async Task<ActionResult<ScheduleSlotDTO>> GetSlotById(int id)
        {
            try
            {
                var role = GetUserRole();
                
                if (role == "Admin" || role == "Member")
                {
                    // Admin và Member có thể xem tất cả slots
                    var slot = await _scheduleSlotService.GetSlotByIdAsync(id);
                    return Ok(slot);
                }
                else
                {
                    // Manager/Staff chỉ xem slots của facility mình
                    var facilityId = GetFacilityIdFromToken();
                    if (!facilityId.HasValue)
                    {
                        return BadRequest("Bạn chưa được gán vào facility nào");
                    }

                    var slot = await _scheduleSlotService.GetSlotByIdWithFacilityCheckAsync(id, facilityId.Value);
                    return Ok(slot);
                }
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy slot theo ID");
                return StatusCode(500, "Lỗi server");
            }
        }

        // ✅ POST tạo slot mới (Manager only)
        [HttpPost]
        [Authorize(Roles = "Manager")]
        public async Task<ActionResult<List<ScheduleSlotDTO>>> CreateSlot([FromBody] CreateScheduleSlotDTO createDto)
        {
            try
            {
                var facilityId = GetFacilityIdFromToken();
                if (!facilityId.HasValue)
                {
                    return BadRequest("Bạn chưa được gán vào facility nào");
                }

                var slots = await _scheduleSlotService.CreateSlotAsync(createDto, facilityId.Value);
                
                if (createDto.IsWorkingHours)
                {
                    // Working hours tạo nhiều slots
                    return Ok(new { 
                        message = $"Tạo thành công {slots.Count} slots cho working hours",
                        data = slots 
                    });
                }
                else
                {
                    // Single slot
                    var slot = slots.First();
                    return CreatedAtAction(nameof(GetSlotById), new { id = slot.SlotId }, new {
                        message = "Tạo slot thành công",
                        data = slots
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo slot");
                return StatusCode(500, "Lỗi server");
            }
        }

        // ✅ PUT cập nhật slot (Manager only, chỉ slots của facility mình)
        [HttpPut("{id}")]
        [Authorize(Roles = "Manager")]
        public async Task<ActionResult<ScheduleSlotDTO>> UpdateSlot(int id, [FromBody] UpdateScheduleSlotDTO updateDto)
        {
            try
            {
                var facilityId = GetFacilityIdFromToken();
                if (!facilityId.HasValue)
                {
                    return BadRequest("Bạn chưa được gán vào facility nào");
                }

                // Kiểm tra slot có thuộc facility không trước khi update
                await _scheduleSlotService.GetSlotByIdWithFacilityCheckAsync(id, facilityId.Value);
                
                var slot = await _scheduleSlotService.UpdateSlotAsync(id, updateDto);
                return Ok(new {
                    message = "Cập nhật slot thành công",
                    data = slot
                });
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật slot");
                return StatusCode(500, "Lỗi server");
            }
        }

        // ✅ DELETE xóa slot (Manager only, chỉ slots của facility mình)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Manager")]
        public async Task<ActionResult> DeleteSlot(int id)
        {
            try
            {
                var facilityId = GetFacilityIdFromToken();
                if (!facilityId.HasValue)
                {
                    return BadRequest("Bạn chưa được gán vào facility nào");
                }

                // Kiểm tra slot có thuộc facility không trước khi delete
                await _scheduleSlotService.GetSlotByIdWithFacilityCheckAsync(id, facilityId.Value);
                
                var result = await _scheduleSlotService.DeleteSlotAsync(id);
                if (result)
                {
                    return Ok(new { message = "Xóa slot thành công" });
                }
                return NotFound("Slot không tồn tại");
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa slot");
                return StatusCode(500, "Lỗi server");
            }
        }

        // ✅ GET working hours slots của facility (Manager/Staff/Member)
        [HttpGet("working-hours")]
        [Authorize(Roles = "Manager,FacilityStaff,Doctor,Member")]
        public async Task<ActionResult<List<ScheduleSlotDTO>>> GetWorkingHoursSlots([FromQuery] TimeOnly startTime, [FromQuery] TimeOnly endTime)
        {
            try
            {
                var slots = await _scheduleSlotService.GetWorkingHoursSlotsAsync(startTime, endTime);
                
                // Filter theo facility của user (nếu không phải Admin)
                var role = GetUserRole();
                if (role != "Admin")
                {
                    var facilityId = GetFacilityIdFromToken();
                    if (facilityId.HasValue)
                    {
                        slots = slots.Where(s => s.FacilityId == facilityId.Value).ToList();
                    }
                }
                
                return Ok(slots);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy working hours slots");
                return StatusCode(500, "Lỗi server");
            }
        }

        // ✅ DELETE working hours (Manager only)
        [HttpDelete("working-hours")]
        [Authorize(Roles = "Manager")]
        public async Task<ActionResult> DeleteWorkingHours([FromQuery] TimeOnly startTime, [FromQuery] TimeOnly endTime)
        {
            try
            {
                var facilityId = GetFacilityIdFromToken();
                if (!facilityId.HasValue)
                {
                    return BadRequest("Bạn chưa được gán vào facility nào");
                }

                var result = await _scheduleSlotService.DeleteWorkingHoursAsync(startTime, endTime);
                if (result)
                {
                    return Ok(new { message = "Xóa working hours thành công" });
                }
                return NotFound("Không tìm thấy working hours để xóa");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa working hours");
                return StatusCode(500, "Lỗi server");
            }
        }

        // ✅ PUT cập nhật working hours (Manager only)
        [HttpPut("working-hours")]
        [Authorize(Roles = "Manager")]
        public async Task<ActionResult<List<ScheduleSlotDTO>>> UpdateWorkingHours(
            [FromQuery] TimeOnly oldStartTime, 
            [FromQuery] TimeOnly oldEndTime, 
            [FromBody] CreateScheduleSlotDTO newConfig)
        {
            try
            {
                var facilityId = GetFacilityIdFromToken();
                if (!facilityId.HasValue)
                {
                    return BadRequest("Bạn chưa được gán vào facility nào");
                }

                var slots = await _scheduleSlotService.UpdateWorkingHoursAsync(oldStartTime, oldEndTime, newConfig, facilityId.Value);
                return Ok(new {
                    message = $"Cập nhật working hours thành công với {slots.Count} slots",
                    data = slots
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật working hours");
                return StatusCode(500, "Lỗi server");
            }
        }

        // ✅ PATCH cập nhật trạng thái slot (Manager only, chỉ slots của facility mình)
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Manager")]
        public async Task<ActionResult> UpdateSlotStatus(int id, [FromBody] UpdateStatusRequest request)
        {
            try
            {
                var facilityId = GetFacilityIdFromToken();
                if (!facilityId.HasValue)
                {
                    return BadRequest("Bạn chưa được gán vào facility nào");
                }

                // Kiểm tra slot có thuộc facility không
                await _scheduleSlotService.GetSlotByIdWithFacilityCheckAsync(id, facilityId.Value);
                
                var result = await _scheduleSlotService.UpdateSlotStatusAsync(id, request.Status);
                if (result)
                {
                    return Ok(new { message = $"Cập nhật trạng thái slot thành {request.Status}" });
                }
                return NotFound("Slot không tồn tại");
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật trạng thái slot");
                return StatusCode(500, "Lỗi server");
            }
        }

        // ✅ DELETE nhiều slots (Manager only, chỉ slots của facility mình)
        [HttpDelete("multiple")]
        [Authorize(Roles = "Manager")]
        public async Task<ActionResult> DeleteMultipleSlots([FromBody] DeleteMultipleSlotsRequest request)
        {
            try
            {
                var facilityId = GetFacilityIdFromToken();
                if (!facilityId.HasValue)
                {
                    return BadRequest("Bạn chưa được gán vào facility nào");
                }

                // Kiểm tra tất cả slots có thuộc facility không
                foreach (var slotId in request.SlotIds)
                {
                    await _scheduleSlotService.GetSlotByIdWithFacilityCheckAsync(slotId, facilityId.Value);
                }
                
                var result = await _scheduleSlotService.DeleteMultipleSlotsAsync(request.SlotIds);
                if (result)
                {
                    return Ok(new { message = $"Xóa thành công {request.SlotIds.Count} slots" });
                }
                return BadRequest("Không thể xóa slots");
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa nhiều slots");
                return StatusCode(500, "Lỗi server");
            }
        }
    }

    // ✅ Request DTOs
    public class UpdateStatusRequest
    {
        public string Status { get; set; }
    }

    public class DeleteMultipleSlotsRequest
    {
        public List<int> SlotIds { get; set; }
    }
} 