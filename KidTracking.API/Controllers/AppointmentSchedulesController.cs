using Contracts.DTOs.Appointment;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace KidTracking.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppointmentSchedulesController : ControllerBase
    {
        private readonly IAppointmentScheduleService _appointmentScheduleService;
        private readonly ILogger<AppointmentSchedulesController> _logger;

        public AppointmentSchedulesController(IAppointmentScheduleService appointmentScheduleService, ILogger<AppointmentSchedulesController> logger)
        {
            _appointmentScheduleService = appointmentScheduleService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<List<AppointmentScheduleDTO>>> GetAllSchedules()
        {
            try
            {
                var schedules = await _appointmentScheduleService.GetAllSchedulesAsync();
                return Ok(schedules);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy tất cả lịch hẹn");
                return StatusCode(500, "Lỗi server");
            }
        }

        [HttpGet("week")]
        public async Task<ActionResult<List<AppointmentScheduleDTO>>> GetSchedulesByWeek([FromQuery] DateTime startOfWeek)
        {
            try
            {
                var schedules = await _appointmentScheduleService.GetSchedulesByWeekAsync(startOfWeek);
                return Ok(schedules);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy lịch hẹn theo tuần");
                return StatusCode(500, "Lỗi server");
            }
        }

        [HttpGet("month")]
        public async Task<ActionResult<List<AppointmentScheduleDTO>>> GetSchedulesByMonth([FromQuery] DateTime month)
        {
            try
            {
                var schedules = await _appointmentScheduleService.GetSchedulesByMonthAsync(month);
                return Ok(schedules);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy lịch hẹn theo tháng");
                return StatusCode(500, "Lỗi server");
            }
        }

        [HttpGet("date")]
        public async Task<ActionResult<List<AppointmentScheduleDTO>>> GetSchedulesByDate([FromQuery] DateTime date)
        {
            try
            {
                var schedules = await _appointmentScheduleService.GetSchedulesByDateAsync(date);
                return Ok(schedules);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy lịch hẹn theo ngày");
                return StatusCode(500, "Lỗi server");
            }
        }

        [HttpPost]
        public async Task<ActionResult<AppointmentScheduleDTO>> CreateSchedule([FromBody] CreateAppointmentScheduleDTO createDto)
        {
            try
            {
                var schedule = await _appointmentScheduleService.CreateScheduleAsync(createDto);
                return CreatedAtAction(nameof(GetSchedulesByDate), new { date = schedule.Date }, schedule);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo lịch hẹn");
                return StatusCode(500, "Lỗi server");
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<AppointmentScheduleDTO>> UpdateSchedule(int id, [FromBody] UpdateAppointmentScheduleDTO updateDto)
        {
            try
            {
                var schedule = await _appointmentScheduleService.UpdateScheduleAsync(id, updateDto);
                return Ok(schedule);
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật lịch hẹn");
                return StatusCode(500, "Lỗi server");
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteSchedule(int id)
        {
            try
            {
                await _appointmentScheduleService.DeleteScheduleAsync(id);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa lịch hẹn");
                return StatusCode(500, "Lỗi server");
            }
        }

        [HttpDelete("date")]
        public async Task<ActionResult> DeleteSchedulesByDate([FromQuery] DateTime date)
        {
            try
            {
                await _appointmentScheduleService.DeleteSchedulesByDateAsync(date);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa lịch hẹn theo ngày");
                return StatusCode(500, "Lỗi server");
            }
        }

        [HttpPatch("date/status")]
        public async Task<ActionResult> UpdateDayStatus([FromQuery] DateTime date, [FromBody] UpdateDayStatusRequest request)
        {
            try
            {
                await _appointmentScheduleService.UpdateDayStatusAsync(date, request.Status);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật trạng thái ngày");
                return StatusCode(500, "Lỗi server");
            }
        }

        [HttpPost("date/slots")]
        public async Task<ActionResult<List<AppointmentScheduleDTO>>> AddSlotsToSchedule([FromQuery] DateTime date, [FromBody] AddSlotsToScheduleRequest request)
        {
            try
            {
                var schedules = await _appointmentScheduleService.AddSlotsToScheduleAsync(date, request.SlotIds);
                return Ok(schedules);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thêm slots vào lịch");
                return StatusCode(500, "Lỗi server");
            }
        }

        [HttpGet("date/slots")]
        public async Task<ActionResult<List<AppointmentScheduleDTO>>> GetDayScheduleWithSlots([FromQuery] DateTime date)
        {
            try
            {
                var schedules = await _appointmentScheduleService.GetDayScheduleWithSlotsAsync(date);
                return Ok(schedules);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy lịch hẹn với slots trong ngày");
                return StatusCode(500, "Lỗi server");
            }
        }
    }

    public class UpdateDayStatusRequest
    {
        public string Status { get; set; }
    }

    public class AddSlotsToScheduleRequest
    {
        public List<int> SlotIds { get; set; }
    }
} 