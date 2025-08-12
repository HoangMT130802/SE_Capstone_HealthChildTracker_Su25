using Contracts.DTOs.FacilityStaff;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace KidTracking.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,FacilityStaff")] 
    public class FacilityStaffController : ControllerBase
    {
        private readonly IFacilityStaffService _facilityStaffService;

        public FacilityStaffController(IFacilityStaffService facilityStaffService)
        {
            _facilityStaffService = facilityStaffService ?? throw new ArgumentNullException(nameof(facilityStaffService));
        }

        [HttpGet("{staffId}")]
        public async Task<IActionResult> GetFacilityStaffById(int staffId)
        {
            try
            {
                var result = await _facilityStaffService.GetFacilityStaffByIdAsync(staffId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi lấy thông tin nhân viên: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllFacilityStaff([FromQuery] int? facilityId, [FromQuery] string position = null, [FromQuery] int? pageIndex = null, [FromQuery] int? pageSize = null)
        {
            try
            {
                var result = await _facilityStaffService.GetAllFacilityStaffAsync(facilityId, position, pageIndex, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi lấy danh sách nhân viên: {ex.Message}");
            }
        }
    }
}