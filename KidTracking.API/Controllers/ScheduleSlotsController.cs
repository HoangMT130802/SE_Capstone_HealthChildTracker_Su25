using Contracts.DTOs.FacilitySchedule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace KidTracking.API.Controllers
{
    [Route("api/schedule-slots")]
    [ApiController]
    public class ScheduleSlotsController : ControllerBase
    {
        private readonly IScheduleSlotService _scheduleSlotService;
        private readonly ILogger<ScheduleSlotsController> _logger;

        public ScheduleSlotsController(
            IScheduleSlotService scheduleSlotService,
            ILogger<ScheduleSlotsController> logger)
        {
            _scheduleSlotService = scheduleSlotService;
            _logger = logger;
        }

        [HttpPost]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> CreateSlot([FromBody] CreateScheduleSlotDTO createDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _scheduleSlotService.CreateSlotAsync(createDto);
                return CreatedAtAction(nameof(GetSlotById), new { id = result.SlotId }, new
                {
                    success = true,
                    message = "Tạo slot thời gian thành công",
                    data = result
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating schedule slot");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi tạo slot thời gian" });
            }
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetSlotById(int id)
        {
            try
            {
                var result = await _scheduleSlotService.GetSlotByIdAsync(id);
                return Ok(new
                {
                    success = true,
                    message = "Lấy thông tin slot thành công",
                    data = result
                });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting schedule slot by ID");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi lấy thông tin slot" });
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetSlots(
            [FromQuery] int page = 1,
            [FromQuery] int size = 10,
            [FromQuery] string? status = null)
        {
            try
            {
                if (page < 1 || size < 1)
                {
                    return BadRequest(new { success = false, message = "Page và Size phải lớn hơn 0" });
                }

                var result = await _scheduleSlotService.GetSlotsAsync(page, size, status);
                return Ok(new
                {
                    success = true,
                    message = "Lấy danh sách slot thành công",
                    data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting schedule slots");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi lấy danh sách slot" });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> UpdateSlot(int id, [FromBody] UpdateScheduleSlotDTO updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _scheduleSlotService.UpdateSlotAsync(id, updateDto);
                return Ok(new
                {
                    success = true,
                    message = "Cập nhật slot thành công",
                    data = result
                });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating schedule slot");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi cập nhật slot" });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> DeleteSlot(int id)
        {
            try
            {
                var result = await _scheduleSlotService.DeleteSlotAsync(id);
                return Ok(new
                {
                    success = true,
                    message = "Xóa slot thành công"
                });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting schedule slot");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi xóa slot" });
            }
        }

        [HttpGet("active")]
        [Authorize]
        public async Task<IActionResult> GetActiveSlots()
        {
            try
            {
                var result = await _scheduleSlotService.GetActiveSlotsAsync();
                return Ok(new
                {
                    success = true,
                    message = "Lấy danh sách slot hoạt động thành công",
                    data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active slots");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi lấy danh sách slot hoạt động" });
            }
        }

        [HttpGet("available")]
        [Authorize]
        public async Task<IActionResult> GetAvailableSlots()
        {
            try
            {
                var result = await _scheduleSlotService.GetAvailableSlotsAsync();
                return Ok(new
                {
                    success = true,
                    message = "Lấy danh sách slot khả dụng thành công",
                    data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available slots");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi lấy danh sách slot khả dụng" });
            }
        }

        [HttpGet("{id}/available")]
        [Authorize]
        public async Task<IActionResult> IsSlotAvailable(int id)
        {
            try
            {
                var result = await _scheduleSlotService.IsSlotAvailableAsync(id);
                return Ok(new
                {
                    success = true,
                    message = "Kiểm tra tình trạng slot thành công",
                    data = new { isAvailable = result }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking slot availability");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi kiểm tra tình trạng slot" });
            }
        }

        [HttpPost("{id}/activate")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> ActivateSlot(int id)
        {
            try
            {
                var result = await _scheduleSlotService.ActivateSlotAsync(id);
                return Ok(new
                {
                    success = true,
                    message = "Kích hoạt slot thành công"
                });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating slot");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi kích hoạt slot" });
            }
        }

        [HttpPost("{id}/deactivate")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> DeactivateSlot(int id)
        {
            try
            {
                var result = await _scheduleSlotService.DeactivateSlotAsync(id);
                return Ok(new
                {
                    success = true,
                    message = "Vô hiệu hóa slot thành công"
                });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating slot");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi vô hiệu hóa slot" });
            }
        }

        [HttpPut("{id}/capacity")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> UpdateSlotCapacity(int id, [FromBody] UpdateCapacityRequestDTO capacityRequest)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _scheduleSlotService.UpdateSlotCapacityAsync(id, capacityRequest.NewCapacity);
                return Ok(new
                {
                    success = true,
                    message = "Cập nhật sức chứa slot thành công"
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating slot capacity");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi cập nhật sức chứa slot" });
            }
        }

        [HttpPost("default")]
        [Authorize(Roles = "Manager,Admin")]
        public async Task<IActionResult> CreateDefaultSlots()
        {
            try
            {
                var result = await _scheduleSlotService.CreateDefaultSlotsAsync();
                return Ok(new
                {
                    success = true,
                    message = $"Tạo thành công {result.Count} slot mặc định",
                    data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating default slots");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi tạo slot mặc định" });
            }
        }

        [HttpPost("batch/create")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> CreateMultipleSlots([FromBody] List<CreateScheduleSlotDTO> createDtos)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _scheduleSlotService.CreateMultipleSlotsAsync(createDtos);
                return Ok(new
                {
                    success = true,
                    message = $"Tạo thành công {result.Count} slot",
                    data = result
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating multiple slots");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi tạo nhiều slot" });
            }
        }

        [HttpPut("batch/status")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> UpdateMultipleSlotsStatus([FromBody] UpdateSlotsStatusBatchRequestDTO batchRequest)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _scheduleSlotService.UpdateMultipleSlotsStatusAsync(batchRequest.SlotIds, batchRequest.Status);
                return Ok(new
                {
                    success = true,
                    message = "Cập nhật trạng thái slot thành công"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating multiple slots status");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi cập nhật trạng thái slot" });
            }
        }

        [HttpPost("{id}/validate-time")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> ValidateSlotTime(int id, [FromBody] ValidateSlotTimeRequestDTO validateRequest)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var isValid = await _scheduleSlotService.ValidateSlotTimeAsync(validateRequest.SlotTime);
                var hasConflict = await _scheduleSlotService.CheckSlotTimeConflictAsync(validateRequest.SlotTime, id);

                return Ok(new
                {
                    success = true,
                    message = "Kiểm tra thời gian slot thành công",
                    data = new 
                    { 
                        isValid = isValid,
                        hasConflict = hasConflict,
                        canUse = isValid && !hasConflict
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating slot time");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi kiểm tra thời gian slot" });
            }
        }
    }

    // Request DTOs for controller actions
    public class UpdateCapacityRequestDTO
    {
        public int NewCapacity { get; set; }
    }

    public class UpdateSlotsStatusBatchRequestDTO
    {
        public List<int> SlotIds { get; set; } = new List<int>();
        public string Status { get; set; }
    }

    public class ValidateSlotTimeRequestDTO
    {
        public string SlotTime { get; set; }
    }
} 