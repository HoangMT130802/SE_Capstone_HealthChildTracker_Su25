using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Repositories.Entities;
using Repositories.Interfaces;
using Services.Interfaces;

namespace KidTracking.API.Controllers
{
    /// <summary>
    /// Controller để gửi email cảm ơn cho member đã sử dụng hệ thống
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ThankYouEmailController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly ILogger<ThankYouEmailController> _logger;

        public ThankYouEmailController(
            IUnitOfWork unitOfWork,
            IEmailService emailService,
            ILogger<ThankYouEmailController> logger)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _logger = logger;
        }

        /// <summary>
        /// Gửi email cảm ơn cho member cụ thể
        /// </summary>
        /// <param name="memberId">ID của member</param>
        /// <param name="includeStatistics">Có bao gồm thống kê cá nhân không (mặc định: true)</param>
        /// <returns>Kết quả gửi email</returns>
        [HttpPost("send/{memberId}")]
        [Authorize] // Yêu cầu authentication
        public async Task<ActionResult> SendThankYouEmail(int memberId, [FromQuery] bool includeStatistics = true)
        {
            try
            {
                _logger.LogInformation("Bắt đầu gửi email cảm ơn cho Member {MemberId}", memberId);

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

                // 2. Lấy thống kê cá nhân nếu được yêu cầu
                int totalChildren = 0;
                int totalAppointments = 0;
                int totalVaccinations = 0;

                if (includeStatistics)
                {
                    var statistics = await GetMemberStatisticsAsync(memberId);
                    totalChildren = statistics.TotalChildren;
                    totalAppointments = statistics.TotalAppointments;
                    totalVaccinations = statistics.TotalVaccinations;
                }

                // 3. Gửi email
                await _emailService.SendThankYouEmailAsync(
                    email: member.Account.Email,
                    memberName: member.FullName,
                    totalChildren: totalChildren,
                    totalAppointments: totalAppointments,
                    totalVaccinations: totalVaccinations
                );

                _logger.LogInformation("Đã gửi email cảm ơn thành công cho Member {MemberId} ({Email})", 
                    memberId, member.Account.Email);

                return Ok(new
                {
                    success = true,
                    message = "Email cảm ơn đã được gửi thành công",
                    memberInfo = new
                    {
                        memberId = member.MemberId,
                        memberName = member.FullName,
                        email = member.Account.Email
                    },
                    statistics = includeStatistics ? new
                    {
                        totalChildren,
                        totalAppointments,
                        totalVaccinations
                    } : null,
                    sentAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi email cảm ơn cho Member {MemberId}", memberId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Có lỗi xảy ra khi gửi email",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Gửi email cảm ơn hàng loạt cho danh sách member
        /// </summary>
        /// <param name="memberIds">Danh sách ID của các member</param>
        /// <param name="includeStatistics">Có bao gồm thống kê cá nhân không (mặc định: true)</param>
        /// <returns>Kết quả gửi email hàng loạt</returns>
        [HttpPost("send-bulk")]
        [Authorize] // Yêu cầu authentication
        public async Task<ActionResult> SendBulkThankYouEmails([FromBody] List<int> memberIds, [FromQuery] bool includeStatistics = true)
        {
            try
            {
                _logger.LogInformation("Bắt đầu gửi email cảm ơn hàng loạt cho {Count} members", memberIds.Count);

                if (!memberIds.Any())
                {
                    return BadRequest(new { message = "Danh sách member ID không được để trống" });
                }

                if (memberIds.Count > 100)
                {
                    return BadRequest(new { message = "Không thể gửi quá 100 email cùng lúc" });
                }

                var results = new List<object>();
                var successCount = 0;
                var failureCount = 0;

                foreach (var memberId in memberIds)
                {
                    try
                    {
                        // Lấy thông tin member
                        var memberRepo = _unitOfWork.GetRepository<Member>();
                        var member = await memberRepo.GetAsync(
                            m => m.MemberId == memberId,
                            includeProperties: "Account"
                        );

                        if (member?.Account?.Email == null)
                        {
                            results.Add(new
                            {
                                memberId,
                                success = false,
                                error = "Member không tồn tại hoặc không có email"
                            });
                            failureCount++;
                            continue;
                        }

                        // Lấy thống kê
                        int totalChildren = 0, totalAppointments = 0, totalVaccinations = 0;
                        if (includeStatistics)
                        {
                            var stats = await GetMemberStatisticsAsync(memberId);
                            totalChildren = stats.TotalChildren;
                            totalAppointments = stats.TotalAppointments;
                            totalVaccinations = stats.TotalVaccinations;
                        }

                        // Gửi email
                        await _emailService.SendThankYouEmailAsync(
                            member.Account.Email,
                            member.FullName,
                            totalChildren,
                            totalAppointments,
                            totalVaccinations
                        );

                        results.Add(new
                        {
                            memberId,
                            memberName = member.FullName,
                            email = member.Account.Email,
                            success = true,
                            statistics = includeStatistics ? new { totalChildren, totalAppointments, totalVaccinations } : null
                        });
                        successCount++;

                        // Delay nhỏ để tránh spam
                        await Task.Delay(200);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi khi gửi email cho Member {MemberId}", memberId);
                        results.Add(new
                        {
                            memberId,
                            success = false,
                            error = ex.Message
                        });
                        failureCount++;
                    }
                }

                _logger.LogInformation("Hoàn thành gửi email hàng loạt: {SuccessCount} thành công, {FailureCount} thất bại", 
                    successCount, failureCount);

                return Ok(new
                {
                    message = $"Đã gửi email cho {successCount}/{memberIds.Count} members",
                    summary = new
                    {
                        totalRequested = memberIds.Count,
                        successCount,
                        failureCount
                    },
                    results,
                    processedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi trong quá trình gửi email hàng loạt");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Có lỗi xảy ra trong quá trình gửi email hàng loạt",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Lấy danh sách members để chọn gửi email
        /// </summary>
        /// <param name="pageIndex">Trang (mặc định: 1)</param>
        /// <param name="pageSize">Số lượng per trang (mặc định: 20)</param>
        /// <param name="search">Tìm kiếm theo tên hoặc email</param>
        /// <returns>Danh sách members</returns>
        [HttpGet("members")]
        [Authorize]
        public async Task<ActionResult> GetMembersForEmail([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
        {
            try
            {
                var memberRepo = _unitOfWork.GetRepository<Member>();
                
                // Query members với account và email
                var query = (await memberRepo.FindAsync(
                    m => m.Account != null && !string.IsNullOrEmpty(m.Account.Email),
                    includeProperties: "Account"
                )).ToList();

                // Filter theo search nếu có
                if (!string.IsNullOrEmpty(search))
                {
                    var searchLower = search.ToLower();
                    query = query.Where(m => 
                        m.FullName.ToLower().Contains(searchLower) ||
                        m.Account.Email.ToLower().Contains(searchLower)
                    ).ToList();
                }

                var totalCount = query.Count();
                var members = query
                    .OrderBy(m => m.FullName)
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .Select(m => new
                    {
                        memberId = m.MemberId,
                        fullName = m.FullName,
                        email = m.Account.Email,
                        phoneNumber = m.PhoneNumber,
                        createdAt = m.CreatedAt,
                        hasChildren = m.Children.Any()
                    })
                    .ToList();

                return Ok(new
                {
                    members,
                    pagination = new
                    {
                        currentPage = pageIndex,
                        pageSize,
                        totalCount,
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách members");
                return StatusCode(500, new { message = "Lỗi server", error = ex.Message });
            }
        }

        /// <summary>
        /// Preview email cảm ơn (không gửi thật)
        /// </summary>
        /// <param name="memberId">ID của member</param>
        /// <returns>Nội dung email sẽ được gửi</returns>
        [HttpGet("preview/{memberId}")]
        [Authorize]
        public async Task<ActionResult> PreviewThankYouEmail(int memberId)
        {
            try
            {
                var memberRepo = _unitOfWork.GetRepository<Member>();
                var member = await memberRepo.GetAsync(
                    m => m.MemberId == memberId,
                    includeProperties: "Account"
                );

                if (member?.Account?.Email == null)
                {
                    return NotFound(new { message = "Member không tồn tại hoặc không có email" });
                }

                var statistics = await GetMemberStatisticsAsync(memberId);

                return Ok(new
                {
                    recipient = new
                    {
                        memberId = member.MemberId,
                        memberName = member.FullName,
                        email = member.Account.Email
                    },
                    emailPreview = new
                    {
                        subject = "🙏 Cảm ơn bạn đã tin tướng và sử dụng Health Child Tracker",
                        statistics = statistics,
                        features = new[]
                        {
                            "📱 Quản lý tiêm chủng dễ dàng",
                            "🏥 Kết nối cơ sở y tế uy tín", 
                            "📈 Theo dõi tăng trưởng toàn diện",
                            "🛡️ An toàn và bảo mật"
                        }
                    },
                    note = "Đây là preview, email chưa được gửi. Sử dụng endpoint /send/{memberId} để gửi thật."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi preview email cho Member {MemberId}", memberId);
                return StatusCode(500, new { message = "Lỗi server", error = ex.Message });
            }
        }

        /// <summary>
        /// Lấy thống kê cá nhân của member
        /// </summary>
        private async Task<(int TotalChildren, int TotalAppointments, int TotalVaccinations)> GetMemberStatisticsAsync(int memberId)
        {
            try
            {
                var childRepo = _unitOfWork.GetRepository<Child>();
                var appointmentRepo = _unitOfWork.GetRepository<VaccinationAppointment>();
                var cvpRepo = _unitOfWork.GetRepository<ChildVaccineProfile>();

                // Số lượng trẻ em
                var children = (await childRepo.FindAsync(c => c.MemberId == memberId)).ToList();
                var totalChildren = children.Count();

                if (totalChildren == 0)
                    return (0, 0, 0);

                var childIds = children.Select(c => c.ChildId).ToList();

                // Số lượng appointment
                var appointments = await appointmentRepo.FindAsync(a => childIds.Contains(a.ChildId));
                var totalAppointments = appointments.Count();

                // Số lượng vaccination hoàn thành
                var completedVaccinations = await cvpRepo.FindAsync(
                    cvp => childIds.Contains(cvp.ChildId) && cvp.Status == "Completed"
                );
                var totalVaccinations = completedVaccinations.Count();

                return (totalChildren, totalAppointments, totalVaccinations);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể lấy thống kê cho Member {MemberId}", memberId);
                return (0, 0, 0);
            }
        }
    }
}
