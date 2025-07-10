using Contracts.DTOs.FacilitySchedule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace KidTracking.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ScheduleSlotsController : ControllerBase
    {
        private readonly IScheduleSlotService _scheduleSlotService;
        private readonly ILogger<ScheduleSlotsController> _logger;

        public ScheduleSlotsController(IScheduleSlotService scheduleSlotService, ILogger<ScheduleSlotsController> logger)
        {
            _scheduleSlotService = scheduleSlotService;
            _logger = logger;
        }

        [HttpGet]
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

        [HttpGet("{id}")]
        public async Task<ActionResult<ScheduleSlotDTO>> GetSlotById(int id)
        {
            try
            {
                var slot = await _scheduleSlotService.GetSlotByIdAsync(id);
                return Ok(slot);
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

        [HttpPost]
        public async Task<ActionResult<List<ScheduleSlotDTO>>> CreateSlot([FromBody] CreateScheduleSlotDTO createDto)
        {
            try
            {
                var slots = await _scheduleSlotService.CreateSlotAsync(createDto);
                
                if (createDto.IsWorkingHours)
                {
                    // Working hours tạo nhiều slots
                    return Ok(slots);
                }
                else
                {
                    // Single slot
                    var slot = slots.First();
                    return CreatedAtAction(nameof(GetSlotById), new { id = slot.SlotId }, slots);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo slot");
                return StatusCode(500, "Lỗi server");
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ScheduleSlotDTO>> UpdateSlot(int id, [FromBody] UpdateScheduleSlotDTO updateDto)
        {
            try
            {
                var slot = await _scheduleSlotService.UpdateSlotAsync(id, updateDto);
                return Ok(slot);
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

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteSlot(int id)
        {
            try
            {
                await _scheduleSlotService.DeleteSlotAsync(id);
                return NoContent();
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

        // ✅ Working Hours Management theo entity mới
        [HttpGet("working-hours")]
        public async Task<ActionResult<List<ScheduleSlotDTO>>> GetWorkingHoursSlots([FromQuery] TimeOnly startTime, [FromQuery] TimeOnly endTime)
        {
            try
            {
                var slots = await _scheduleSlotService.GetWorkingHoursSlotsAsync(startTime, endTime);
                return Ok(slots);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy working hours slots");
                return StatusCode(500, "Lỗi server");
            }
        }

        [HttpDelete("working-hours")]
        public async Task<ActionResult> DeleteWorkingHours([FromQuery] TimeOnly startTime, [FromQuery] TimeOnly endTime)
        {
            try
            {
                await _scheduleSlotService.DeleteWorkingHoursAsync(startTime, endTime);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa working hours");
                return StatusCode(500, "Lỗi server");
            }
        }

        [HttpPut("working-hours")]
        public async Task<ActionResult<List<ScheduleSlotDTO>>> UpdateWorkingHours(
            [FromQuery] TimeOnly oldStartTime, 
            [FromQuery] TimeOnly oldEndTime, 
            [FromBody] CreateScheduleSlotDTO newConfig)
        {
            try
            {
                var slots = await _scheduleSlotService.UpdateWorkingHoursAsync(oldStartTime, oldEndTime, newConfig);
                return Ok(slots);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật working hours");
                return StatusCode(500, "Lỗi server");
            }
        }

        [HttpPatch("{id}/status")]
        public async Task<ActionResult> UpdateSlotStatus(int id, [FromBody] UpdateStatusRequest request)
        {
            try
            {
                await _scheduleSlotService.UpdateSlotStatusAsync(id, request.Status);
                return Ok();
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

        [HttpDelete("multiple")]
        public async Task<ActionResult> DeleteMultipleSlots([FromBody] DeleteMultipleSlotsRequest request)
        {
            try
            {
                await _scheduleSlotService.DeleteMultipleSlotsAsync(request.SlotIds);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa nhiều slots");
                return StatusCode(500, "Lỗi server");
            }
        }
    }

    // ❌ XÓA: CreateWorkingHoursRequest - không còn dùng vì đã gộp vào CreateScheduleSlotDTO

    public class UpdateStatusRequest
    {
        public string Status { get; set; }
    }

    public class DeleteMultipleSlotsRequest
    {
        public List<int> SlotIds { get; set; }
    }
} 