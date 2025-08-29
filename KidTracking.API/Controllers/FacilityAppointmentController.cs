using Contracts.DTOs.Appointment;
using Contracts.DTOs.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using System.Security.Claims;
using Repositories.Interfaces;
using Repositories.Entities;

namespace KidTracking.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] 
    public class FacilityAppointmentController : ControllerBase
    {
        private readonly IAppointmentBookingService _appointmentBookingService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<FacilityAppointmentController> _logger;

        public FacilityAppointmentController(
            IAppointmentBookingService appointmentBookingService,
            IUnitOfWork unitOfWork,
            ILogger<FacilityAppointmentController> logger)
        {
            _appointmentBookingService = appointmentBookingService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        #region Helper Methods
        private async Task<int> GetFacilityIdAsync()
        {
            // Thử lấy FacilityId từ token trước
            var facilityIdClaim = User.FindFirst("FacilityId")?.Value;
            if (!string.IsNullOrEmpty(facilityIdClaim) && int.TryParse(facilityIdClaim, out int facilityId))
            {
                return facilityId;
            }

            // Nếu không có trong token, lấy từ database dựa trên AccountId
            var accountIdClaim = User.FindFirst("AccountId")?.Value;
            if (string.IsNullOrEmpty(accountIdClaim) || !int.TryParse(accountIdClaim, out int accountId))
            {
                throw new UnauthorizedAccessException("Không tìm thấy AccountId trong token");
            }

            // Kiểm tra xem user có phải là FacilityStaff không
            var facilityStaffRepo = _unitOfWork.GetRepository<FacilityStaff>();
            var staff = await facilityStaffRepo.GetAsync(s => s.AccountId == accountId);
            
            if (staff != null)
            {
                return staff.FacilityId;
            }

            // Nếu không phải FacilityStaff, có thể là Admin - cho phép truy cập tất cả facility
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole == "Admin")
            {
                // Admin có thể truy cập facility nào đó, cần parameter facilityId
                throw new ArgumentException("Admin cần cung cấp FacilityId trong query parameter");
            }

            throw new UnauthorizedAccessException("User không thuộc về facility nào");
        }

        private string GetUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value ?? "";
        }

        private async Task ValidateManagerPermissionAsync()
        {
            // Lấy AccountId từ token
            var accountIdClaim = User.FindFirst("AccountId")?.Value;
            if (string.IsNullOrEmpty(accountIdClaim) || !int.TryParse(accountIdClaim, out int accountId))
            {
                throw new UnauthorizedAccessException("Không thể xác định AccountId từ token");
            }

            // Kiểm tra FacilityStaff có position Manager không
            var facilityStaffRepo = _unitOfWork.GetRepository<FacilityStaff>();
            var staff = await facilityStaffRepo.GetAsync(s => s.AccountId == accountId);
            
            if (staff == null)
            {
                throw new UnauthorizedAccessException("User không phải là staff của facility");
            }

            if (staff.Position != "Manager")
            {
                throw new UnauthorizedAccessException("Chỉ có Manager mới có quyền duyệt hoàn tiền");
            }
        }
        #endregion

        /// <summary>
        /// Lấy tất cả lịch đặt của facility với phân trang và search theo tên trẻ
        /// </summary>
        /// <param name="pageIndex">Số trang (mặc định: 1)</param>
        /// <param name="pageSize">Kích thước trang (mặc định: 50)</param>
        /// <param name="childName">Tên trẻ để search (tùy chọn)</param>
        /// <returns>Danh sách lịch đặt có phân trang</returns>
        [HttpGet]
        public async Task<ActionResult<FacilityAppointmentResponseDTO>> GetAllFacilityAppointments(
            [FromQuery] int pageIndex = 1, 
            [FromQuery] int pageSize = 50,
            [FromQuery] string? childName = null)
        {
            try
            {
                // ✅ Validate phân trang
                if (pageIndex < 1) pageIndex = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 50; // Giới hạn max 100 items/page
                
                var facilityId = await GetFacilityIdAsync();
                var result = await _appointmentBookingService.GetAllFacilityAppointmentsAsync(facilityId, pageIndex, pageSize, childName);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access to facility appointments");
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Facility not found for user");
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy tất cả lịch đặt cho facility");
                return StatusCode(500, "Có lỗi xảy ra khi lấy danh sách lịch đặt");
            }
        }

        /// <summary>
        /// Lấy tất cả lịch đặt của facility cụ thể (dành cho Admin) với phân trang
        /// </summary>
        /// <param name="facilityId">ID cơ sở</param>
        /// <param name="pageIndex">Số trang (mặc định: 1)</param>
        /// <param name="pageSize">Kích thước trang (mặc định: 50)</param>
        /// <returns>Danh sách lịch đặt của facility có phân trang</returns>
        [HttpGet("admin/{facilityId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<FacilityAppointmentResponseDTO>> GetAllFacilityAppointmentsByAdmin(
            int facilityId,
            [FromQuery] int pageIndex = 1, 
            [FromQuery] int pageSize = 50)
        {
            try
            {
                // ✅ Validate phân trang
                if (pageIndex < 1) pageIndex = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 50;
                
                var result = await _appointmentBookingService.GetAllFacilityAppointmentsAsync(facilityId, pageIndex, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy tất cả lịch đặt cho facility {FacilityId}", facilityId);
                return StatusCode(500, "Có lỗi xảy ra khi lấy danh sách lịch đặt");
            }
        }

        /// <summary>
        /// Lấy lịch đặt theo ngày với phân trang
        /// </summary>
        /// <param name="date">Ngày (yyyy-MM-dd)</param>
        /// <param name="pageIndex">Số trang (mặc định: 1)</param>
        /// <param name="pageSize">Kích thước trang (mặc định: 50)</param>
        /// <returns>Lịch đặt trong ngày có phân trang</returns>
        [HttpGet("date")]
        public async Task<ActionResult<FacilityAppointmentResponseDTO>> GetFacilityAppointmentsByDate(
            [FromQuery] DateTime date,
            [FromQuery] int pageIndex = 1, 
            [FromQuery] int pageSize = 50)
        {
            try
            {
                // ✅ Validate phân trang
                if (pageIndex < 1) pageIndex = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 50;
                
                var facilityId = await GetFacilityIdAsync();
                var result = await _appointmentBookingService.GetFacilityAppointmentsByDateAsync(facilityId, date, pageIndex, pageSize);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access to facility appointments");
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Facility not found for user");
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy lịch đặt theo ngày cho facility");
                return StatusCode(500, "Có lỗi xảy ra khi lấy lịch đặt theo ngày");
            }
        }

        /// <summary>
        /// Lấy lịch đặt theo tuần với phân trang
        /// </summary>
        /// <param name="startOfWeek">Ngày đầu tuần (yyyy-MM-dd)</param>
        /// <param name="pageIndex">Số trang (mặc định: 1)</param>
        /// <param name="pageSize">Kích thước trang (mặc định: 50)</param>
        /// <returns>Lịch đặt trong tuần có phân trang</returns>
        [HttpGet("week")]
        public async Task<ActionResult<FacilityAppointmentResponseDTO>> GetFacilityAppointmentsByWeek(
            [FromQuery] DateTime startOfWeek,
            [FromQuery] int pageIndex = 1, 
            [FromQuery] int pageSize = 50)
        {
            try
            {
                // ✅ Validate phân trang
                if (pageIndex < 1) pageIndex = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 50;
                
                var facilityId = await GetFacilityIdAsync();
                var result = await _appointmentBookingService.GetFacilityAppointmentsByWeekAsync(facilityId, startOfWeek, pageIndex, pageSize);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access to facility appointments");
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Facility not found for user");
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy lịch đặt theo tuần cho facility");
                return StatusCode(500, "Có lỗi xảy ra khi lấy lịch đặt theo tuần");
            }
        }

        /// <summary>
        /// Lấy lịch đặt theo tháng với phân trang
        /// </summary>
        /// <param name="month">Tháng (yyyy-MM-dd)</param>
        /// <param name="pageIndex">Số trang (mặc định: 1)</param>
        /// <param name="pageSize">Kích thước trang (mặc định: 50)</param>
        /// <returns>Lịch đặt trong tháng có phân trang</returns>
        [HttpGet("month")]
        public async Task<ActionResult<FacilityAppointmentResponseDTO>> GetFacilityAppointmentsByMonth(
            [FromQuery] DateTime month,
            [FromQuery] int pageIndex = 1, 
            [FromQuery] int pageSize = 50)
        {
            try
            {
                // ✅ Validate phân trang
                if (pageIndex < 1) pageIndex = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 50;
                
                var facilityId = await GetFacilityIdAsync();
                var result = await _appointmentBookingService.GetFacilityAppointmentsByMonthAsync(facilityId, month, pageIndex, pageSize);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access to facility appointments");
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Facility not found for user");
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy lịch đặt theo tháng cho facility");
                return StatusCode(500, "Có lỗi xảy ra khi lấy lịch đặt theo tháng");
            }
        }

        /// <summary>
        /// Lấy chi tiết lịch đặt theo ID
        /// </summary>
        /// <param name="appointmentId">ID lịch đặt</param>
        /// <returns>Chi tiết lịch đặt</returns>
        [HttpGet("{appointmentId}")]
        public async Task<ActionResult<FacilityAppointmentDTO>> GetFacilityAppointmentById(int appointmentId)
        {
            try
            {
                var facilityId = await GetFacilityIdAsync();
                var result = await _appointmentBookingService.GetFacilityAppointmentByIdAsync(appointmentId, facilityId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access to facility appointment {AppointmentId}", appointmentId);
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Appointment not found: {AppointmentId}", appointmentId);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy chi tiết lịch đặt {AppointmentId}", appointmentId);
                return StatusCode(500, "Có lỗi xảy ra khi lấy chi tiết lịch đặt");
            }
        }

        /// <summary>
        /// Cập nhật trạng thái lịch đặt (Duyệt/Từ chối/Hoàn thành/Hoàn tiền)
        /// </summary>
        /// <param name="appointmentId">ID lịch đặt</param>
        /// <param name="updateDto">Thông tin cập nhật</param>
        /// <returns>Kết quả cập nhật</returns>
        [HttpPut("{appointmentId}/status")]
        public async Task<ActionResult<bool>> UpdateAppointmentStatus(
            int appointmentId,
            [FromBody] UpdateAppointmentStatusDTO updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var facilityId = await GetFacilityIdAsync();
                var result = await _appointmentBookingService.UpdateAppointmentStatusAsync(appointmentId, facilityId, updateDto);
                
                _logger.LogInformation("Cập nhật trạng thái lịch đặt {AppointmentId} thành {Status} thành công", 
                    appointmentId, updateDto.Status);
                
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access to update appointment {AppointmentId}", appointmentId);
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Appointment not found: {AppointmentId}", appointmentId);
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid status transition for appointment {AppointmentId}", appointmentId);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật trạng thái lịch đặt {AppointmentId}", appointmentId);
                return StatusCode(500, "Có lỗi xảy ra khi cập nhật trạng thái lịch đặt");
            }
        }

        /// <summary>
        /// Thay đổi vaccine trong appointment (dành cho bác sĩ/staff khi vaccine hết)
        /// </summary>
        /// <param name="request">Thông tin thay đổi vaccine</param>
        /// <returns>Kết quả thay đổi</returns>
        [HttpPut("vaccine/update")]
        [Authorize(Roles = "FacilityStaff")]
        public async Task<ActionResult<ResponseDataModel<UpdateVaccineResponseDTO>>> UpdateAppointmentVaccine(
            [FromBody] UpdateVaccineRequestDTO request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ResponseDataModel<UpdateVaccineResponseDTO>
                    {
                        Status = false,
                        Message = "Dữ liệu đầu vào không hợp lệ",
                        Data = new UpdateVaccineResponseDTO { IsSuccess = false, Message = "Dữ liệu đầu vào không hợp lệ" }
                    });
                }

                var facilityId = await GetFacilityIdAsync();
                
                // Lấy AccountId từ JWT claims
                var accountIdClaim = User.FindFirst("AccountId")?.Value;
                if (string.IsNullOrEmpty(accountIdClaim) || !int.TryParse(accountIdClaim, out int staffAccountId))
                {
                    return Unauthorized("Không tìm thấy thông tin người dùng");
                }
                
                var result = await _appointmentBookingService.UpdateAppointmentVaccineAsync(request, facilityId, staffAccountId);
                
                if (result.Status)
                {
                    _logger.LogInformation("Staff {StaffAccountId} đã thay đổi vaccine thành công cho AppointmentDetail {DetailId}", 
                        staffAccountId, request.AppointmentDetailId);
                }
                
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access to update vaccine for AppointmentDetail {DetailId}", request.AppointmentDetailId);
                return Forbid("Bạn không có quyền thực hiện thao tác này");
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid request for updating vaccine: {Message}", ex.Message);
                return BadRequest(new ResponseDataModel<UpdateVaccineResponseDTO>
                {
                    Status = false,
                    Message = ex.Message,
                    Data = new UpdateVaccineResponseDTO { IsSuccess = false, Message = ex.Message }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thay đổi vaccine cho AppointmentDetail {DetailId}", request.AppointmentDetailId);
                return StatusCode(500, new ResponseDataModel<UpdateVaccineResponseDTO>
                {
                    Status = false,
                    Message = "Có lỗi xảy ra khi thay đổi vaccine",
                    Data = new UpdateVaccineResponseDTO { IsSuccess = false, Message = "Có lỗi xảy ra khi thay đổi vaccine" }
                });
            }
        }

        /// <summary>
        /// Lấy danh sách vaccine có thể thay thế cho appointment detail (khi vaccine hiện tại hết)
        /// </summary>
        /// <param name="appointmentDetailId">ID của VaccinationAppointmentDetail</param>
        /// <returns>Danh sách vaccine thay thế</returns>
        [HttpGet("appointment-detail/{appointmentDetailId}/available-vaccines")]
        [Authorize(Roles = "FacilityStaff")]
        public async Task<ActionResult<ResponseDataModel<AvailableVaccinesResponseDTO>>> GetAvailableVaccinesForReplacement(
            int appointmentDetailId)
        {
            try
            {
                var facilityId = await GetFacilityIdAsync();
                var result = await _appointmentBookingService.GetAvailableVaccinesForReplacementAsync(appointmentDetailId, facilityId);
                
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access to get available vaccines for AppointmentDetail {DetailId}", appointmentDetailId);
                return Forbid("Bạn không có quyền thực hiện thao tác này");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách vaccine thay thế cho AppointmentDetail {DetailId}", appointmentDetailId);
                return StatusCode(500, new ResponseDataModel<AvailableVaccinesResponseDTO>
                {
                    Status = false,
                    Message = "Có lỗi xảy ra khi lấy danh sách vaccine thay thế",
                    Data = new AvailableVaccinesResponseDTO { AppointmentDetailId = appointmentDetailId, TotalAvailable = 0 }
                });
            }
        }

        /// <summary>
        /// Manager duyệt hoàn tiền (Refunding -> Accepted) - Chỉ Manager được phép
        /// </summary>
        /// <param name="appointmentId">ID lịch đặt</param>
        /// <param name="refundDto">Thông tin duyệt hoàn tiền</param>
        /// <returns>Kết quả duyệt</returns>
        [HttpPut("{appointmentId}/approve-refund")]
        public async Task<ActionResult<bool>> ApproveRefund(
            int appointmentId,
            [FromBody] ApproveRefundDTO refundDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Kiểm tra quyền Manager
                await ValidateManagerPermissionAsync();

                var facilityId = await GetFacilityIdAsync();
                var result = await _appointmentBookingService.ApproveRefundAsync(appointmentId, facilityId, refundDto.Note);
                
                _logger.LogInformation("Manager duyệt hoàn tiền cho appointment {AppointmentId} thành công", appointmentId);
                
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access to approve refund for appointment {AppointmentId}", appointmentId);
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Appointment not found for refund approval: {AppointmentId}", appointmentId);
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid refund approval for appointment {AppointmentId}", appointmentId);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi duyệt hoàn tiền cho appointment {AppointmentId}", appointmentId);
                return StatusCode(500, "Có lỗi xảy ra khi duyệt hoàn tiền");
            }
        }
    }
} 