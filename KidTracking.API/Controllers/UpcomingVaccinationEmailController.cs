using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Repositories.Entities;
using Repositories.Interfaces;
using Services.Interfaces;
using Contracts.DTOs.Email;

namespace KidTracking.API.Controllers
{
    /// <summary>
    /// Controller để gửi email lịch tiêm chủng sắp tới cho member
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class UpcomingVaccinationEmailController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly ILogger<UpcomingVaccinationEmailController> _logger;

        public UpcomingVaccinationEmailController(
            IUnitOfWork unitOfWork,
            IEmailService emailService,
            ILogger<UpcomingVaccinationEmailController> logger)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _logger = logger;
        }

        /// <summary>
        /// Gửi email lịch tiêm chủng sắp tới gần nhất cho member cụ thể
        /// </summary>
        /// <param name="memberId">ID của member</param>
        /// <returns>Kết quả gửi email</returns>
        [HttpPost("send/{memberId}")]
        public async Task<ActionResult> SendUpcomingVaccinationEmail(int memberId)
        {
            try
            {
                _logger.LogInformation("Bắt đầu gửi email lịch tiêm sắp tới cho Member {MemberId}", memberId);

                // 1. Lấy thông tin member và account
                var memberRepo = _unitOfWork.GetRepository<Member>();
                var member = await memberRepo.GetAsync(
                    m => m.MemberId == memberId,
                    includeProperties: "Account"
                );

                if (member == null)
                {
                    return NotFound(new { message = $"Không tìm thấy member với ID {memberId}" });
                }

                if (member.Account == null)
                {
                    return BadRequest(new { message = "Member không có account liên kết" });
                }

                if (string.IsNullOrEmpty(member.Account.Email))
                {
                    return BadRequest(new { message = "Member không có email để gửi" });
                }

                // 2. Lấy lịch tiêm sắp tới gần nhất của member
                var upcomingVaccinations = await GetNearestUpcomingVaccinationsForMemberAsync(memberId);

                // 3. Gửi email
                await _emailService.SendUpcomingVaccinationEmailAsync(
                    email: member.Account.Email,
                    memberName: member.FullName,
                    upcomingVaccinations: upcomingVaccinations
                );

                _logger.LogInformation("Đã gửi email lịch tiêm sắp tới thành công cho Member {MemberId} ({Email}) với {Count} lịch hẹn", 
                    memberId, member.Account.Email, upcomingVaccinations.Count);

                return Ok(new
                {
                    success = true,
                    message = "Email lịch tiêm sắp tới đã được gửi thành công",
                    memberInfo = new
                    {
                        memberId = member.MemberId,
                        memberName = member.FullName,
                        email = member.Account.Email
                    },
                    vaccinationInfo = new
                    {
                        totalUpcomingAppointments = upcomingVaccinations.Count,
                        appointments = upcomingVaccinations.Select(v => new
                        {
                            childName = v.ChildName,
                            vaccineName = v.VaccineName,
                            appointmentDate = v.AppointmentDate,
                            facilityName = v.FacilityName,
                            daysUntil = v.DaysUntilAppointment
                        })
                    },
                    sentAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi email lịch tiêm sắp tới cho Member {MemberId}", memberId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Có lỗi xảy ra khi gửi email",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Lấy lịch tiêm sắp tới gần nhất của member
        /// </summary>
        private async Task<List<UpcomingVaccinationItemDTO>> GetNearestUpcomingVaccinationsForMemberAsync(int memberId)
        {
            var result = new List<UpcomingVaccinationItemDTO>();

            try
            {
                var fromDate = DateOnly.FromDateTime(DateTime.Today);

                // Lấy tất cả appointment sắp tới của member (không giới hạn ngày)
                var appointmentRepo = _unitOfWork.GetRepository<VaccinationAppointment>();
                var allUpcomingAppointments = await appointmentRepo.FindAsync(
                    a => a.Child.MemberId == memberId &&
                         a.Schedule.Date >= fromDate &&
                         (a.Status == "Pending" || a.Status == "Confirmed" || a.Status == "Paid"),
                    includeProperties: "Child,Schedule,Schedule.Facility,Schedule.Slot"
                );

                // Tìm ngày gần nhất
                if (!allUpcomingAppointments.Any())
                {
                    _logger.LogInformation("Không tìm thấy lịch tiêm sắp tới nào cho Member {MemberId}", memberId);
                    return result;
                }

                var nearestDate = allUpcomingAppointments.Min(a => a.Schedule.Date);
                _logger.LogInformation("Tìm thấy lịch tiêm gần nhất vào ngày {NearestDate} cho Member {MemberId}", nearestDate, memberId);

                // Lấy tất cả appointments trong ngày gần nhất đó
                var appointments = allUpcomingAppointments.Where(a => a.Schedule.Date == nearestDate).ToList();

                // Lấy tất cả VaccinationAppointmentDetail cùng lúc để tránh N+1 queries
                var appointmentIds = appointments.Select(a => a.AppointmentId).ToList();
                var detailRepo = _unitOfWork.GetRepository<VaccinationAppointmentDetail>();
                var details = await detailRepo.FindAsync(
                    d => appointmentIds.Contains(d.AppointmentId),
                    includeProperties: "Vaccine"
                );
                var detailLookup = details.ToLookup(d => d.AppointmentId);

                foreach (var appointment in appointments)
                {
                    var detail = detailLookup[appointment.AppointmentId].FirstOrDefault();

                    if (detail?.Vaccine != null)
                    {
                        var daysUntil = (appointment.Schedule.Date.ToDateTime(TimeOnly.MinValue) - DateTime.Today).Days;
                        var childAge = (DateTime.Today.Year - appointment.Child.BirthDate.Year) * 12 + (DateTime.Today.Month - appointment.Child.BirthDate.Month);

                        // Parse DoseNumber từ string sang int
                        int doseNumber = 1;
                        if (!string.IsNullOrEmpty(detail.DoseNumber) && int.TryParse(detail.DoseNumber, out int parsedDose))
                        {
                            doseNumber = parsedDose;
                        }

                        result.Add(new UpcomingVaccinationItemDTO
                        {
                            AppointmentId = appointment.AppointmentId,
                            ChildName = appointment.Child?.FullName ?? "Unknown",
                            ChildAge = childAge,
                            VaccineName = detail.Vaccine.Name,
                            DoseNumber = doseNumber,
                            AppointmentDate = appointment.Schedule.Date,
                            AppointmentTime = appointment.Schedule.Slot?.SlotTime ?? "Cả ngày",
                            FacilityName = appointment.Schedule.Facility?.FacilityName ?? "Unknown",
                            FacilityAddress = appointment.Schedule.Facility?.Address ?? "",
                            Status = appointment.Status,
                            DaysUntilAppointment = daysUntil
                        });
                    }
                }

                // Sắp xếp theo thời gian hẹn
                result = result.OrderBy(v => v.AppointmentDate).ThenBy(v => v.AppointmentTime).ToList();

                _logger.LogInformation("Tìm thấy {Count} lịch tiêm gần nhất vào ngày {NearestDate} cho Member {MemberId}", 
                    result.Count, nearestDate, memberId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy lịch tiêm sắp tới cho Member {MemberId}", memberId);
                return result;
            }
        }
    }
}
