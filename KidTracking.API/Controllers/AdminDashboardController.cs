using AutoMapper;
using Contracts.DTOs.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace KidTracking.API.Controllers
{
    [Route("api/admin/dashboard")]
    [ApiController]
    [Authorize(Roles = "Admin")] 
    public class AdminDashboardController : ControllerBase
    {
        private readonly IVaccinationFacilityService _facilityService;
        private readonly IOrderService _orderService;
        private readonly IChildService _childService;
        private readonly IMembershipService _membershipService;
        private readonly IUserMembershipService _userMembershipService;
        private readonly ITransactionService _transactionService;
        private readonly IAppointmentBookingService _appointmentService;
        private readonly IGrowthRecordService _growthRecordService;
        private readonly IMapper _mapper;
        private readonly ILogger<AdminDashboardController> _logger;

        public AdminDashboardController(
            IVaccinationFacilityService facilityService,
            IOrderService orderService,
            IChildService childService,
            IMembershipService membershipService,
            IUserMembershipService userMembershipService,
            ITransactionService transactionService,
            IGrowthRecordService growthRecordService,
            IAppointmentBookingService appointmentService,
            IMapper mapper,
            ILogger<AdminDashboardController> logger)
        {
            _facilityService = facilityService ?? throw new ArgumentNullException(nameof(facilityService));
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _childService = childService ?? throw new ArgumentNullException(nameof(childService));
            _membershipService = membershipService ?? throw new ArgumentNullException(nameof(membershipService));
            _userMembershipService = userMembershipService ?? throw new ArgumentNullException(nameof(userMembershipService));
            _transactionService = transactionService ?? throw new ArgumentNullException(nameof(transactionService));
            _growthRecordService = growthRecordService ?? throw new ArgumentNullException(nameof(growthRecordService));
            _appointmentService = appointmentService ?? throw new ArgumentNullException(nameof(appointmentService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        [HttpGet]
        public async Task<ActionResult<AdminDashboardDTO>> GetDashboardInfo()
        {
            try
            {
                _logger.LogInformation("Retrieving admin dashboard info");


                var totalFacilities = await _facilityService.GetTotalCountAsync();

                var totalRevenueFromOrders = await _orderService.GetTotalRevenueAsync();

                var totalChildren = await _childService.GetTotalCountAsync();

                var totalMembershipPackages = await _membershipService.GetTotalCountAsync(); 

                var totalUserMemberships = await _userMembershipService.GetActiveCountAsync(); 

                var totalRevenueFromMemberships = await _transactionService.GetTotalRevenueFromMembershipsAsync(); 

                var totalGrowthRecords = await _growthRecordService.GetTotalCountAsync();

                var appointmentStats = await _appointmentService.GetAppointmentStatsAsync();

                var dashboardDto = new AdminDashboardDTO
                {
                    TotalFacilities = totalFacilities,
                    TotalRevenue = totalRevenueFromOrders, 
                    TotalChildren = totalChildren,
                    TotalMembershipPackages = totalMembershipPackages,
                    TotalUserMemberships = totalUserMemberships,
                    TotalRevenueFromMemberships = totalRevenueFromMemberships,
                    TotalGrowthRecords = totalGrowthRecords,
                    AppointmentStats = appointmentStats
                };

                return Ok(dashboardDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving admin dashboard info");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
