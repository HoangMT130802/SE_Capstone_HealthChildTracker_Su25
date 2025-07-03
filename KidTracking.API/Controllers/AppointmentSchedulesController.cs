using Contracts.DTOs.Appointment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using System.Security.Claims;

namespace KidTracking.API.Controllers
{
    [Route("api/appointment-schedules")]
    [ApiController]
    public class AppointmentSchedulesController : ControllerBase
    {
        private readonly IAppointmentScheduleService _appointmentScheduleService;
        private readonly ILogger<AppointmentSchedulesController> _logger;

        public AppointmentSchedulesController(
            IAppointmentScheduleService appointmentScheduleService,
            ILogger<AppointmentSchedulesController> logger)
        {
            _appointmentScheduleService = appointmentScheduleService;
            _logger = logger;
        }

        [HttpPost]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> CreateSchedule([FromBody] CreateAppointmentScheduleDTO createDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _appointmentScheduleService.CreateScheduleAsync(createDto);
                return CreatedAtAction(nameof(GetScheduleById), new { id = result.ScheduleId }, new
                {
                    success = true,
                    message = "Tạo lịch hẹn thành công",
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
                _logger.LogError(ex, "Error creating appointment schedule");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi tạo lịch hẹn" });
            }
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetScheduleById(int id)
        {
            try
            {
                var result = await _appointmentScheduleService.GetScheduleByIdAsync(id);
                return Ok(new
                {
                    success = true,
                    message = "Lấy thông tin lịch hẹn thành công",
                    data = result
                });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting appointment schedule by ID");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi lấy thông tin lịch hẹn" });
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetSchedules(
            [FromQuery] int page = 1,
            [FromQuery] int size = 10,
            [FromQuery] int? facilityId = null,
            [FromQuery] string? status = null)
        {
            try
            {
                if (page < 1 || size < 1)
                {
                    return BadRequest(new { success = false, message = "Page và Size phải lớn hơn 0" });
                }

                var result = await _appointmentScheduleService.GetSchedulesAsync(page, size, facilityId, status);
                return Ok(new
                {
                    success = true,
                    message = "Lấy danh sách lịch hẹn thành công",
                    data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting appointment schedules");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi lấy danh sách lịch hẹn" });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> UpdateSchedule(int id, [FromBody] UpdateAppointmentScheduleDTO updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _appointmentScheduleService.UpdateScheduleAsync(id, updateDto);
                return Ok(new
                {
                    success = true,
                    message = "Cập nhật lịch hẹn thành công",
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
                _logger.LogError(ex, "Error updating appointment schedule");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi cập nhật lịch hẹn" });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> DeleteSchedule(int id)
        {
            try
            {
                var result = await _appointmentScheduleService.DeleteScheduleAsync(id);
                return Ok(new
                {
                    success = true,
                    message = "Xóa lịch hẹn thành công"
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
                _logger.LogError(ex, "Error deleting appointment schedule");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi xóa lịch hẹn" });
            }
        }

        [HttpGet("facility/{facilityId}")]
        [Authorize]
        public async Task<IActionResult> GetSchedulesByFacility(int facilityId)
        {
            try
            {
                var result = await _appointmentScheduleService.GetSchedulesByFacilityAsync(facilityId);
                return Ok(new
                {
                    success = true,
                    message = "Lấy lịch hẹn theo cơ sở thành công",
                    data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting schedules by facility");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi lấy lịch hẹn theo cơ sở" });
            }
        }

        [HttpGet("date/{date}")]
        [Authorize]
        public async Task<IActionResult> GetSchedulesByDate(DateTime date)
        {
            try
            {
                var result = await _appointmentScheduleService.GetSchedulesByDateAsync(date);
                return Ok(new
                {
                    success = true,
                    message = "Lấy lịch hẹn theo ngày thành công",
                    data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting schedules by date");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi lấy lịch hẹn theo ngày" });
            }
        }

        [HttpGet("available")]
        [Authorize]
        public async Task<IActionResult> GetAvailableSchedules([FromQuery] DateTime date, [FromQuery] int? facilityId = null)
        {
            try
            {
                var result = await _appointmentScheduleService.GetAvailableSchedulesAsync(date, facilityId);
                return Ok(new
                {
                    success = true,
                    message = "Lấy lịch hẹn khả dụng thành công",
                    data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available schedules");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi lấy lịch hẹn khả dụng" });
            }
        }

        [HttpGet("{id}/available")]
        [Authorize]
        public async Task<IActionResult> IsScheduleAvailable(int id)
        {
            try
            {
                var result = await _appointmentScheduleService.IsScheduleAvailableAsync(id);
                return Ok(new
                {
                    success = true,
                    message = "Kiểm tra tình trạng lịch hẹn thành công",
                    data = new { available = result }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking schedule availability");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi kiểm tra tình trạng lịch hẹn" });
            }
        }

        [HttpPost("{id}/book")]
        [Authorize(Roles = "Manager,Member")]
        public async Task<IActionResult> BookSchedule(int id, [FromBody] BookScheduleRequestDTO bookingRequest)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _appointmentScheduleService.BookScheduleAsync(id, bookingRequest.MemberId);
                return Ok(new
                {
                    success = true,
                    message = "Đặt lịch hẹn thành công"
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
                _logger.LogError(ex, "Error booking schedule");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi đặt lịch hẹn" });
            }
        }

        [HttpPost("{id}/cancel")]
        [Authorize(Roles = "Manager,Member")]
        public async Task<IActionResult> CancelSchedule(int id)
        {
            try
            {
                var result = await _appointmentScheduleService.CancelScheduleAsync(id);
                return Ok(new
                {
                    success = true,
                    message = "Hủy lịch hẹn thành công"
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
                _logger.LogError(ex, "Error canceling schedule");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi hủy lịch hẹn" });
            }
        }

        [HttpPost("facility/{facilityId}/holiday")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> SetHolidaySchedule(int facilityId, [FromBody] SetHolidayRequestDTO holidayRequest)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _appointmentScheduleService.SetHolidayAsync(facilityId, holidayRequest.Date, holidayRequest.Reason);
                return Ok(new
                {
                    success = true,
                    message = "Thiết lập lịch nghỉ lễ thành công"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting holiday schedule");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi thiết lập lịch nghỉ lễ" });
            }
        }

        [HttpPost("facility/{facilityId}/maintenance")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> SetMaintenanceSchedule(int facilityId, [FromBody] SetMaintenanceRequestDTO maintenanceRequest)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _appointmentScheduleService.SetMaintenanceAsync(facilityId, maintenanceRequest.Date, maintenanceRequest.Reason);
                return Ok(new
                {
                    success = true,
                    message = "Thiết lập lịch bảo trì thành công"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting maintenance schedule");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi thiết lập lịch bảo trì" });
            }
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> UpdateScheduleStatus(int id, [FromBody] UpdateStatusRequestDTO statusRequest)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _appointmentScheduleService.UpdateScheduleStatusAsync(id, statusRequest.Status);
                return Ok(new
                {
                    success = true,
                    message = "Cập nhật trạng thái lịch hẹn thành công"
                });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating schedule status");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi cập nhật trạng thái lịch hẹn" });
            }
        }

        [HttpPost("batch/create")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> CreateSchedulesForDateRange([FromBody] CreateScheduleBatchRequestDTO batchRequest)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _appointmentScheduleService.CreateSchedulesForDateRangeAsync(
                    batchRequest.FacilityId, 
                    batchRequest.StartDate, 
                    batchRequest.EndDate);

                return Ok(new
                {
                    success = true,
                    message = "Tạo lịch hẹn hàng loạt thành công",
                    data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating schedules for date range");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi tạo lịch hẹn hàng loạt" });
            }
        }

        [HttpPost("multiple")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> CreateMultipleSchedules([FromBody] List<CreateAppointmentScheduleDTO> createDtos)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _appointmentScheduleService.CreateMultipleSchedulesAsync(createDtos);
                return Ok(new
                {
                    success = true,
                    message = "Tạo nhiều lịch hẹn thành công",
                    data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating multiple schedules");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi tạo nhiều lịch hẹn" });
            }
        }

        [HttpPut("batch/status")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> UpdateMultipleSchedulesStatus([FromBody] UpdateScheduleStatusBatchRequestDTO batchRequest)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _appointmentScheduleService.UpdateMultipleSchedulesStatusAsync(batchRequest.ScheduleIds, batchRequest.Status);
                return Ok(new
                {
                    success = true,
                    message = "Cập nhật trạng thái lịch hẹn hàng loạt thành công"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating multiple schedules status");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi cập nhật trạng thái lịch hẹn hàng loạt" });
            }
        }

        [HttpGet("manager/{managerId}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> GetSchedulesByManager(int managerId)
        {
            try
            {
                var result = await _appointmentScheduleService.GetSchedulesByManagerAsync(managerId);
                return Ok(new
                {
                    success = true,
                    message = "Lấy lịch hẹn theo manager thành công",
                    data = result
                });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting schedules by manager");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi lấy lịch hẹn theo manager" });
            }
        }
    }

    // Request DTOs
    public class BookScheduleRequestDTO
    {
        public int MemberId { get; set; }
    }

    public class SetHolidayRequestDTO
    {
        public DateTime Date { get; set; }
        public string Reason { get; set; } = "Holiday";
    }

    public class SetMaintenanceRequestDTO
    {
        public DateTime Date { get; set; }
        public string Reason { get; set; } = "Maintenance";
    }

    public class UpdateStatusRequestDTO
    {
        public string Status { get; set; }
    }

    public class CreateScheduleBatchRequestDTO
    {
        public int FacilityId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class UpdateScheduleStatusBatchRequestDTO
    {
        public List<int> ScheduleIds { get; set; } = new List<int>();
        public string Status { get; set; }
    }
} 