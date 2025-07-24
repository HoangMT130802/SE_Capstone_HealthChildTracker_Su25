using Contracts.DTOs.ChildVaccineProfile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace KidTracking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChildVaccineProfileController : ControllerBase
    {
        private readonly IChildVaccineProfileService _childVaccineProfileService;
        private readonly ILogger<ChildVaccineProfileController> _logger;

        public ChildVaccineProfileController(
            IChildVaccineProfileService childVaccineProfileService,
            ILogger<ChildVaccineProfileController> logger)
        {
            _childVaccineProfileService = childVaccineProfileService;
            _logger = logger;
        }

        /// <summary>
        /// Lấy tất cả vaccine profiles của child (cho parents)
        /// </summary>
        [HttpGet("child/{childId}")]
        [Authorize(Roles = "Member")]
        public async Task<ActionResult<IEnumerable<ChildVaccineProfileDTO>>> GetChildVaccineProfiles(int childId)
        {
            try
            {
                var profiles = await _childVaccineProfileService.GetAllChildVaccineProfilesByChildIdAsync(childId);
                return Ok(profiles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy vaccine profiles cho child {ChildId}", childId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Lấy tất cả vaccine profiles của child (public API)
        /// </summary>
        [HttpGet("child/{childId}/public")]
        public async Task<ActionResult<IEnumerable<ChildVaccineProfileDTO>>> GetChildVaccineProfilesPublic(int childId)
        {
            try
            {
                var profiles = await _childVaccineProfileService.GetAllChildVaccineProfilesByChildIdPublicAsync(childId);
                return Ok(profiles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy public vaccine profiles cho child {ChildId}", childId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Doctor ghi nhận hoàn thành tiêm vaccine và tạo mũi tiếp theo nếu cần
        /// </summary>
        [HttpPost("complete-vaccination")]
        [Authorize(Roles = "Facility Staff")]
        public async Task<ActionResult<VaccinationCompletionResponseDTO>> CompleteVaccination([FromBody] CompleteVaccinationDTO completeDto)
        {
            try
            {
                _logger.LogInformation("Doctor completing vaccination for Child {ChildId}, Vaccine {VaccineId}, Appointment {AppointmentId}", 
                    completeDto.ChildId, completeDto.VaccineId, completeDto.AppointmentId);

                var result = await _childVaccineProfileService.CompleteVaccinationAsync(completeDto);
                
                _logger.LogInformation("Successfully completed vaccination for Child {ChildId}, Vaccine {VaccineId}", 
                    completeDto.ChildId, completeDto.VaccineId);
                    
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Not found when completing vaccination");
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation when completing vaccination");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi complete vaccination cho Child {ChildId}, Vaccine {VaccineId}", 
                    completeDto.ChildId, completeDto.VaccineId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Tạo vaccine profile mới
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Member,Facility Staff")]
        public async Task<ActionResult<ChildVaccineProfileDTO>> CreateVaccineProfile([FromBody] CreateChildVaccineProfileDTO createDto)
        {
            try
            {
                var result = await _childVaccineProfileService.CreateChildVaccineProfileAsync(createDto);
                return CreatedAtAction(nameof(GetVaccineProfile), new { id = result.VaccineProfileId }, result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo vaccine profile");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Lấy vaccine profile theo ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Member,Facility Staff")]
        public async Task<ActionResult<ChildVaccineProfileDTO>> GetVaccineProfile(int id)
        {
            try
            {
                var profile = await _childVaccineProfileService.GetChildVaccineProfileByIdAsync(id);
                return Ok(profile);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy vaccine profile {ProfileId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Cập nhật vaccine profile
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Member,Facility Staff")]
        public async Task<ActionResult<ChildVaccineProfileDTO>> UpdateVaccineProfile(int id, [FromBody] UpdateChildVaccineProfileDTO updateDto)
        {
            try
            {
                var result = await _childVaccineProfileService.UpdateChildVaccineProfileAsync(id, updateDto);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật vaccine profile {ProfileId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Xóa vaccine profile
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Member,Facility Staff")]
        public async Task<ActionResult> DeleteVaccineProfile(int id)
        {
            try
            {
                var result = await _childVaccineProfileService.DeleteChildVaccineProfileAsync(id);
                if (result)
                {
                    return NoContent();
                }
                return NotFound("Vaccine profile not found");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa vaccine profile {ProfileId}", id);
                return StatusCode(500, "Internal server error");
            }
        }
    }
} 