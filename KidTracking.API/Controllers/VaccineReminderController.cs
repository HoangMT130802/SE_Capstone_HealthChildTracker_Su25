using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using System.Security.Claims;

namespace KidTracking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class VaccineReminderController : ControllerBase
    {
        private readonly IVaccineReminderService _vaccineReminderService;
        private readonly ILogger<VaccineReminderController> _logger;

        public VaccineReminderController(
            IVaccineReminderService vaccineReminderService,
            ILogger<VaccineReminderController> logger)
        {
            _vaccineReminderService = vaccineReminderService;
            _logger = logger;
        }

        /// <summary>
        /// Gửi vaccine reminder cho một trẻ cụ thể (Manual trigger)
        /// </summary>
        [HttpPost("send-vaccine-reminder/{childId}/{vaccineProfileId}")]
        public async Task<IActionResult> SendVaccineReminder(int childId, int vaccineProfileId)
        {
            try
            {
                var accountId = GetCurrentAccountId();
                _logger.LogInformation("Manual vaccine reminder requested by account {AccountId} for child {ChildId}", 
                    accountId, childId);

                await _vaccineReminderService.SendVaccineReminderForChildAsync(childId, vaccineProfileId);

                return Ok(new { message = "Vaccine reminder sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending vaccine reminder for child {ChildId}, profile {VaccineProfileId}", 
                    childId, vaccineProfileId);
                return StatusCode(500, new { message = "Error sending vaccine reminder", error = ex.Message });
            }
        }

        /// <summary>
        /// Gửi appointment reminder cho một appointment cụ thể (Manual trigger)
        /// </summary>
        [HttpPost("send-appointment-reminder/{appointmentId}")]
        public async Task<IActionResult> SendAppointmentReminder(int appointmentId)
        {
            try
            {
                var accountId = GetCurrentAccountId();
                _logger.LogInformation("Manual appointment reminder requested by account {AccountId} for appointment {AppointmentId}", 
                    accountId, appointmentId);

                await _vaccineReminderService.SendAppointmentReminderAsync(appointmentId);

                return Ok(new { message = "Appointment reminder sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending appointment reminder for appointment {AppointmentId}", appointmentId);
                return StatusCode(500, new { message = "Error sending appointment reminder", error = ex.Message });
            }
        }

        /// <summary>
        /// Gửi vaccination completion notification (Manual trigger)
        /// </summary>
        [HttpPost("send-completion-notification/{childId}/{vaccineProfileId}")]
        public async Task<IActionResult> SendVaccinationCompletion(int childId, int vaccineProfileId)
        {
            try
            {
                var accountId = GetCurrentAccountId();
                _logger.LogInformation("Manual vaccination completion notification requested by account {AccountId} for child {ChildId}", 
                    accountId, childId);

                await _vaccineReminderService.SendVaccinationCompletionAsync(childId, vaccineProfileId);

                return Ok(new { message = "Vaccination completion notification sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending vaccination completion notification for child {ChildId}, profile {VaccineProfileId}", 
                    childId, vaccineProfileId);
                return StatusCode(500, new { message = "Error sending vaccination completion notification", error = ex.Message });
            }
        }

        /// <summary>
        /// Lấy danh sách vaccine reminders sắp tới
        /// </summary>
        [HttpGet("upcoming-vaccine-reminders")]
        public async Task<IActionResult> GetUpcomingVaccineReminders([FromQuery] int daysAhead = 7)
        {
            try
            {
                var reminders = await _vaccineReminderService.GetUpcomingVaccineRemindersAsync(daysAhead);
                return Ok(reminders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting upcoming vaccine reminders");
                return StatusCode(500, new { message = "Error getting upcoming vaccine reminders", error = ex.Message });
            }
        }

        /// <summary>
        /// Lấy danh sách appointment reminders sắp tới
        /// </summary>
        [HttpGet("upcoming-appointment-reminders")]
        public async Task<IActionResult> GetUpcomingAppointmentReminders([FromQuery] int daysAhead = 3)
        {
            try
            {
                var reminders = await _vaccineReminderService.GetUpcomingAppointmentRemindersAsync(daysAhead);
                return Ok(reminders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting upcoming appointment reminders");
                return StatusCode(500, new { message = "Error getting upcoming appointment reminders", error = ex.Message });
            }
        }

        /// <summary>
        /// Trigger manual daily reminders (Admin only)
        /// </summary>
        [HttpPost("trigger-daily-reminders")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> TriggerDailyReminders()
        {
            try
            {
                var accountId = GetCurrentAccountId();
                _logger.LogInformation("Manual daily reminders triggered by admin account {AccountId}", accountId);

                // Chạy trong background task để không block request
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _vaccineReminderService.SendDailyVaccineRemindersAsync();
                        await _vaccineReminderService.SendDailyAppointmentRemindersAsync();
                        _logger.LogInformation("Manual daily reminders completed successfully");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in manual daily reminders");
                    }
                });

                return Ok(new { message = "Daily reminders triggered successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering daily reminders");
                return StatusCode(500, new { message = "Error triggering daily reminders", error = ex.Message });
            }
        }

        /// <summary>
        /// Test email service (Admin only)
        /// </summary>
        [HttpPost("test-email")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> TestEmail([FromBody] TestEmailRequest request)
        {
            try
            {
                var emailService = HttpContext.RequestServices.GetRequiredService<IEmailService>();
                
                switch (request.EmailType.ToLower())
                {
                    case "vaccine":
                        await emailService.SendVaccineReminderEmailAsync(
                            request.Email,
                            "Test Parent",
                            "Test Child",
                            "Test Vaccine",
                            1,
                            DateOnly.FromDateTime(DateTime.Today.AddDays(3)),
                            "Test Facility"
                        );
                        break;
                    
                    case "appointment":
                        await emailService.SendAppointmentReminderEmailAsync(
                            request.Email,
                            "Test Parent",
                            "Test Child",
                            DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                            "08:00 - 09:00",
                            "Test Facility",
                            "123 Test Street",
                            "Test Vaccine"
                        );
                        break;
                    
                    case "completion":
                        await emailService.SendVaccinationCompletionEmailAsync(
                            request.Email,
                            "Test Parent",
                            "Test Child",
                            "Test Vaccine",
                            1,
                            DateOnly.FromDateTime(DateTime.Today),
                            DateOnly.FromDateTime(DateTime.Today.AddDays(30))
                        );
                        break;
                    
                    default:
                        return BadRequest(new { message = "Invalid email type. Use: vaccine, appointment, or completion" });
                }

                return Ok(new { message = $"Test {request.EmailType} email sent successfully to {request.Email}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending test email");
                return StatusCode(500, new { message = "Error sending test email", error = ex.Message });
            }
        }

        private int GetCurrentAccountId()
        {
            var accountIdClaim = User.FindFirst("AccountId")?.Value;
            return int.TryParse(accountIdClaim, out var accountId) ? accountId : 0;
        }
    }

    public class TestEmailRequest
    {
        public string Email { get; set; } = "";
        public string EmailType { get; set; } = ""; // vaccine, appointment, completion
    }
}
