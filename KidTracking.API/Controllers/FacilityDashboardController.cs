using AutoMapper;
using Contracts.DTOs.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace KidTracking.API.Controllers
{
    [Route("api/facility/{facilityId}/dashboard")]
    [ApiController]
    [Authorize] 
    public class FacilityDashboardController : ControllerBase
    {
        private readonly IFacilityVaccineService _facilityVaccineService;
        private readonly IOrderService _orderService;
        private readonly IVaccinePackageService _vaccinePackageService;
        private readonly IFacilityRatingService _facilityRatingService;
        private readonly IFacilityStaffService _facilityStaffService;
        private readonly IMapper _mapper;
        private readonly ILogger<FacilityDashboardController> _logger;

        public FacilityDashboardController(
            IFacilityVaccineService facilityVaccineService,
            IOrderService orderService,
            IVaccinePackageService vaccinePackageService,
            IFacilityRatingService facilityRatingService,
            IFacilityStaffService facilityStaffService,
            IMapper mapper,
            ILogger<FacilityDashboardController> logger)
        {
            _facilityVaccineService = facilityVaccineService ?? throw new ArgumentNullException(nameof(facilityVaccineService));
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _vaccinePackageService = vaccinePackageService ?? throw new ArgumentNullException(nameof(vaccinePackageService));
            _facilityRatingService = facilityRatingService ?? throw new ArgumentNullException(nameof(facilityRatingService));
            _facilityStaffService = facilityStaffService ?? throw new ArgumentNullException(nameof(facilityStaffService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        // Cập nhật FacilityDashboardController.cs
        [HttpGet]
        public async Task<ActionResult<FacilityDashboardDTO>> GetDashboardInfo(int facilityId)
        {
            try
            {
                _logger.LogInformation($"Retrieving dashboard info for FacilityId: {facilityId}");

                // 1. Số lượng FacilityVaccine
                var totalFacilityVaccines = await _facilityVaccineService.GetCountByFacilityAsync(facilityId);

                // 2. Số Order (tổng số đơn hàng, bất kể status - nếu muốn riêng thì điều chỉnh)
                var totalOrders = await _orderService.GetCountByFacilityAsync(facilityId);

                // 3. Số PackageVaccine
                var totalPackageVaccines = await _vaccinePackageService.GetCountByFacilityAsync(facilityId);

                // 4. Rating trung bình
                var averageRating = await _facilityRatingService.GetAverageRatingByFacilityAsync(facilityId);

                // 5. Thống kê revenue: Paid riêng, Pending count riêng
                var revenueStats = await _orderService.GetRevenueStatsByFacilityAsync(facilityId);

                // 6. Thống kê staff
                var staffCounts = await _facilityStaffService.GetStaffCountsByFacilityAsync(facilityId);

                // Tạo DTO response
                var dashboardDto = new FacilityDashboardDTO
                {
                    FacilityId = facilityId,
                    TotalFacilityVaccines = totalFacilityVaccines,
                    TotalOrders = totalOrders,
                    TotalPackageVaccines = totalPackageVaccines,
                    AverageRating = averageRating,
                    RevenueStats = revenueStats, 
                    StaffCounts = staffCounts
                };

                return Ok(dashboardDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving dashboard info for FacilityId {facilityId}");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
