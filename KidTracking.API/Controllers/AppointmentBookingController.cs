using Contracts.DTOs.Appointment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace KidTracking.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AppointmentBookingController : ControllerBase
    {
        private readonly IAppointmentBookingService _appointmentBookingService;
        private readonly ILogger<AppointmentBookingController> _logger;

        public AppointmentBookingController(
            IAppointmentBookingService appointmentBookingService,
            ILogger<AppointmentBookingController> logger)
        {
            _appointmentBookingService = appointmentBookingService;
            _logger = logger;
        }

        #region Search & Filter APIs

        /// <summary>
        /// Tìm kiếm cơ sở tiêm chủng theo bệnh
        /// </summary>
        /// <param name="diseaseId">ID bệnh</param>
        /// <param name="filters">Bộ lọc tìm kiếm</param>
        /// <returns>Danh sách cơ sở phù hợp</returns>
        [HttpPost("search/facilities/{diseaseId}")]
        public async Task<ActionResult<FacilitySearchByDiseaseDTO>> SearchFacilitiesByDisease(
            int diseaseId,
            [FromBody] AppointmentSearchFiltersDTO? filters = null)
        {
            try
            {
                var result = await _appointmentBookingService.SearchFacilitiesByDiseaseAsync(diseaseId, filters);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tìm kiếm cơ sở theo bệnh {DiseaseId}", diseaseId);
                return StatusCode(500, "Có lỗi xảy ra khi tìm kiếm cơ sở");
            }
        }

        /// <summary>
        /// Lấy danh sách vaccine và gói của cơ sở cho bệnh cụ thể
        /// </summary>
        /// <param name="facilityId">ID cơ sở</param>
        /// <param name="diseaseId">ID bệnh</param>
        /// <returns>Danh sách vaccine và gói</returns>
        [HttpGet("facilities/{facilityId}/vaccines/{diseaseId}")]
        public async Task<ActionResult<FacilityVaccinesByDiseaseDTO>> GetFacilityVaccinesByDisease(
            int facilityId,
            int diseaseId)
        {
            try
            {
                var result = await _appointmentBookingService.GetFacilityVaccinesByDiseaseAsync(facilityId, diseaseId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy vaccine của cơ sở {FacilityId} cho bệnh {DiseaseId}", facilityId, diseaseId);
                return StatusCode(500, "Có lỗi xảy ra khi lấy danh sách vaccine");
            }
        }

        /// <summary>
        /// Lấy lịch trống có sẵn của cơ sở
        /// </summary>
        /// <param name="facilityId">ID cơ sở</param>
        /// <param name="fromDate">Ngày bắt đầu (yyyy-MM-dd)</param>
        /// <param name="toDate">Ngày kết thúc (yyyy-MM-dd)</param>
        /// <param name="preferredTimeSlots">Khung giờ ưu tiên (tùy chọn)</param>
        /// <returns>Lịch trống theo ngày</returns>
        [HttpGet("facilities/{facilityId}/schedules")]
        public async Task<ActionResult<AvailableSchedulesDTO>> GetAvailableSchedules(
            int facilityId,
            [FromQuery] string fromDate,
            [FromQuery] string toDate,
            [FromQuery] List<string>? preferredTimeSlots = null)
        {
            try
            {
                if (!DateOnly.TryParse(fromDate, out var fromDateOnly) || 
                    !DateOnly.TryParse(toDate, out var toDateOnly))
                {
                    return BadRequest("Định dạng ngày không hợp lệ. Sử dụng định dạng yyyy-MM-dd");
                }

                var result = await _appointmentBookingService.GetAvailableSchedulesAsync(
                    facilityId, fromDateOnly, toDateOnly, preferredTimeSlots);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy lịch trống của cơ sở {FacilityId}", facilityId);
                return StatusCode(500, "Có lỗi xảy ra khi lấy lịch trống");
            }
        }

        #endregion

        #region Validation APIs

        /// <summary>
        /// Validate thông tin đặt lịch trước khi book
        /// </summary>
        /// <param name="request">Thông tin đặt lịch</param>
        /// <returns>Kết quả validation</returns>
        [HttpPost("validate")]
        public async Task<ActionResult<AppointmentValidationDTO>> ValidateBookingRequest(
            [FromBody] AppointmentBookingRequestDTO request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _appointmentBookingService.ValidateBookingRequestAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi validate đặt lịch cho trẻ {ChildId}", request.ChildId);
                return StatusCode(500, "Có lỗi xảy ra khi kiểm tra thông tin đặt lịch");
            }
        }

        /// <summary>
        /// Kiểm tra lịch sử tiêm của trẻ
        /// </summary>
        /// <param name="childId">ID trẻ</param>
        /// <param name="diseaseId">ID bệnh</param>
        /// <returns>Lịch sử tiêm liên quan</returns>
        [HttpGet("children/{childId}/vaccination-history/{diseaseId}")]
        public async Task<ActionResult<ChildVaccinationHistoryDTO>> GetChildVaccinationHistory(
            int childId, 
            int diseaseId)
        {
            try
            {
                var result = await _appointmentBookingService.GetChildVaccinationHistoryAsync(childId, diseaseId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy lịch sử tiêm của trẻ {ChildId} cho bệnh {DiseaseId}", childId, diseaseId);
                return StatusCode(500, "Có lỗi xảy ra khi lấy lịch sử tiêm");
            }
        }

        #endregion

        #region Booking APIs

        /// <summary>
        /// Đặt lịch tiêm chủng
        /// </summary>
        /// <param name="request">Thông tin đặt lịch</param>
        /// <returns>Kết quả đặt lịch</returns>
        [HttpPost("book")]
        public async Task<ActionResult<AppointmentBookingResponseDTO>> BookAppointment(
            [FromBody] AppointmentBookingRequestDTO request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _appointmentBookingService.BookAppointmentAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đặt lịch cho trẻ {ChildId}", request.ChildId);
                return StatusCode(500, "Có lỗi xảy ra khi đặt lịch");
            }
        }

        /// <summary>
        /// Đặt lịch nhanh với thông tin tối thiểu
        /// </summary>
        /// <param name="request">Thông tin đặt lịch nhanh</param>
        /// <returns>Kết quả đặt lịch hoặc gợi ý</returns>
        [HttpPost("quick-book")]
        public async Task<ActionResult<AppointmentQuickBookingResponseDTO>> QuickBookAppointment(
            [FromBody] AppointmentQuickBookingDTO request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _appointmentBookingService.QuickBookAppointmentAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đặt lịch nhanh cho trẻ {ChildId}", request.ChildId);
                return StatusCode(500, "Có lỗi xảy ra khi đặt lịch nhanh");
            }
        }

        /// <summary>
        /// Hủy đặt lịch
        /// </summary>
        /// <param name="appointmentId">ID cuộc hẹn</param>
        /// <param name="reason">Lý do hủy</param>
        /// <returns>Kết quả hủy</returns>
        [HttpDelete("{appointmentId}/cancel")]
        public async Task<ActionResult<bool>> CancelAppointment(
            int appointmentId,
            [FromBody] string reason)
        {
            try
            {
                var result = await _appointmentBookingService.CancelAppointmentAsync(appointmentId, reason);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi hủy lịch hẹn {AppointmentId}", appointmentId);
                return StatusCode(500, "Có lỗi xảy ra khi hủy lịch hẹn");
            }
        }

        #endregion

        #region Cost Calculation APIs

        /// <summary>
        /// Tính toán chi phí dự kiến
        /// </summary>
        /// <param name="facilityId">ID cơ sở</param>
        /// <param name="packageId">ID gói vaccine (tùy chọn)</param>
        /// <param name="facilityVaccineIds">Danh sách ID vaccine (nếu không chọn gói)</param>
        /// <returns>Chi tiết chi phí</returns>
        [HttpPost("calculate-cost/{facilityId}")]
        public async Task<ActionResult<CostBreakdownDTO>> CalculateEstimatedCost(
            int facilityId,
            [FromQuery] int? packageId = null,
            [FromBody] List<int>? facilityVaccineIds = null)
        {
            try
            {
                var result = await _appointmentBookingService.CalculateEstimatedCostAsync(
                    facilityId, packageId, facilityVaccineIds);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tính chi phí cho cơ sở {FacilityId}", facilityId);
                return StatusCode(500, "Có lỗi xảy ra khi tính chi phí");
            }
        }

        #endregion

        #region Helper APIs

        /// <summary>
        /// Tạo gợi ý đặt lịch khác
        /// </summary>
        /// <param name="request">Yêu cầu đặt lịch ban đầu</param>
        /// <param name="maxSuggestions">Số lượng gợi ý tối đa</param>
        /// <returns>Danh sách gợi ý</returns>
        [HttpPost("suggestions")]
        public async Task<ActionResult<List<AppointmentSuggestionDTO>>> GenerateAppointmentSuggestions(
            [FromBody] AppointmentQuickBookingDTO request,
            [FromQuery] int maxSuggestions = 5)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _appointmentBookingService.GenerateAppointmentSuggestionsAsync(request, maxSuggestions);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo gợi ý đặt lịch cho trẻ {ChildId}", request.ChildId);
                return StatusCode(500, "Có lỗi xảy ra khi tạo gợi ý");
            }
        }

        #endregion
    }
} 