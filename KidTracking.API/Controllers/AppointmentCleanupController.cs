using Contracts.DTOs.Appointment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace KidTracking.API.Controllers
{
    /// <summary>
    /// Controller để quản lý cleanup appointment đã quá hạn
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AppointmentCleanupController : ControllerBase
    {
        private readonly IAppointmentBookingService _appointmentBookingService;
        private readonly ILogger<AppointmentCleanupController> _logger;

        public AppointmentCleanupController(
            IAppointmentBookingService appointmentBookingService,
            ILogger<AppointmentCleanupController> logger)
        {
            _appointmentBookingService = appointmentBookingService;
            _logger = logger;
        }

        /// <summary>
        /// Thực hiện cleanup appointment đã quá hạn (Manual trigger)
        /// </summary>
        /// <returns>Kết quả cleanup</returns>
        [HttpPost("cleanup-expired")]
        [Authorize] // Yêu cầu authentication
        public async Task<ActionResult<AppointmentCleanupResultDTO>> CleanupExpiredAppointments()
        {
            try
            {
                _logger.LogInformation("Manual cleanup expired appointments được trigger bởi user");

                var result = await _appointmentBookingService.CleanupExpiredAppointmentsAsync();

                if (result.HasErrors)
                {
                    return BadRequest(new
                    {
                        message = "Có lỗi xảy ra trong quá trình cleanup",
                        result = result
                    });
                }

                return Ok(new
                {
                    message = "Cleanup thành công",
                    result = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thực hiện manual cleanup expired appointments");
                return StatusCode(500, new
                {
                    message = "Lỗi server khi thực hiện cleanup",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Lấy thông tin về số lượng appointment có thể cleanup
        /// </summary>
        /// <returns>Thống kê appointment có thể cleanup</returns>
        [HttpGet("cleanup-preview")]
        [Authorize] // Yêu cầu authentication
        public async Task<ActionResult> GetCleanupPreview()
        {
            try
            {
                _logger.LogInformation("Lấy preview cleanup expired appointments");

                // Logic tương tự như CleanupExpiredAppointmentsAsync nhưng chỉ đếm, không thực hiện cleanup
                // Có thể tạo method riêng trong service để preview
                
                return Ok(new
                {
                    message = "Preview cleanup - chức năng này có thể được implement sau",
                    note = "Hiện tại hãy sử dụng endpoint /cleanup-expired để thực hiện cleanup trực tiếp"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy preview cleanup");
                return StatusCode(500, new
                {
                    message = "Lỗi server khi lấy preview cleanup",
                    error = ex.Message
                });
            }
        }
    }
}

