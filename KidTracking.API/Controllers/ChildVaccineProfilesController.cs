using Contracts.DTOs.ChildVaccineProfile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repositories.Entities;
using Repositories.Interfaces;
using Services.Interfaces;
using System.Security.Claims;

namespace KidTracking.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChildVaccineProfilesController : ControllerBase
    {
        private readonly IChildVaccineProfileService _childVaccineProfileService; 
        private readonly IChildService _childService; private readonly ILogger _logger; 
        private readonly IUnitOfWork _unitOfWork;
        public ChildVaccineProfilesController(
        IChildVaccineProfileService childVaccineProfileService,
        IChildService childService,
        IUnitOfWork unitOfWork,
        ILogger<ChildVaccineProfilesController> logger)
        {
            _childVaccineProfileService = childVaccineProfileService ?? throw new ArgumentNullException(nameof(childVaccineProfileService));
            _childService = childService ?? throw new ArgumentNullException(nameof(childService));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private async Task<bool> ValidateChildAccess(int childId)
        {
            try
            {
                var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserIdClaim) || !int.TryParse(currentUserIdClaim, out int currentUserId))
                {
                    return false;
                }

                if (User.IsInRole("Admin") || User.IsInRole("Doctor"))
                {
                    return true;
                }

                var childRepository = _unitOfWork.GetRepository<Child>();
                var child = await childRepository.GetAsync(c => c.ChildId == childId && c.MemberId == currentUserId);
                return child != null;
            }
            catch
            {
                return false;
            }
        }

        [HttpGet("child/{childId}")]
        public async Task<IActionResult> GetAllChildVaccineProfilesByChildId(int childId)
        {
            try
            {
                if (!await ValidateChildAccess(childId))
                {
                    return Forbid("Bạn không có quyền xem thông tin này");
                }

                var profiles = await _childVaccineProfileService.GetAllChildVaccineProfilesByChildIdAsync(childId);
                return Ok(profiles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy hồ sơ tiêm chủng cho trẻ {childId}");
                return StatusCode(500, new { message = "Lỗi server nội bộ" });
            }
        }

        [HttpGet("{profileId}")]
        public async Task<IActionResult> GetChildVaccineProfileById(int profileId)
        {
            try
            {
                var profile = await _childVaccineProfileService.GetChildVaccineProfileByIdAsync(profileId);
                if (!await ValidateChildAccess(profile.ChildId))
                {
                    return Forbid("Bạn không có quyền xem thông tin này");
                }

                return Ok(profile);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy hồ sơ tiêm chủng {profileId}");
                return StatusCode(500, new { message = "Lỗi server nội bộ" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateChildVaccineProfile([FromBody] CreateChildVaccineProfileDTO profileDTO)
        {
            try
            {
                if (!await ValidateChildAccess(profileDTO.ChildId))
                {
                    return Forbid("Bạn không có quyền thực hiện hành động này");
                }

                var profile = await _childVaccineProfileService.CreateChildVaccineProfileAsync(profileDTO);
                return CreatedAtAction(nameof(GetChildVaccineProfileById), new { profileId = profile.VaccineProfileId }, profile);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi tạo hồ sơ tiêm chủng cho trẻ {profileDTO.ChildId}");
                return StatusCode(500, new { message = "Lỗi server nội bộ" });
            }
        }

        [HttpPut("{profileId}")]
        public async Task<IActionResult> UpdateChildVaccineProfile(int profileId, [FromBody] UpdateChildVaccineProfileDTO profileDTO)
        {
            try
            {
                var existingProfile = await _childVaccineProfileService.GetChildVaccineProfileByIdAsync(profileId);
                if (!await ValidateChildAccess(existingProfile.ChildId))
                {
                    return Forbid("Bạn không có quyền thực hiện hành động này");
                }

                var profile = await _childVaccineProfileService.UpdateChildVaccineProfileAsync(profileId, profileDTO);
                return Ok(profile);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi cập nhật hồ sơ tiêm chủng {profileId}");
                return StatusCode(500, new { message = "Lỗi server nội bộ" });
            }
        }

        [HttpDelete("{profileId}")]
        public async Task<IActionResult> DeleteChildVaccineProfile(int profileId)
        {
            try
            {
                var existingProfile = await _childVaccineProfileService.GetChildVaccineProfileByIdAsync(profileId);
                if (!await ValidateChildAccess(existingProfile.ChildId))
                {
                    return Forbid("Bạn không có quyền thực hiện hành động này");
                }

                var result = await _childVaccineProfileService.DeleteChildVaccineProfileAsync(profileId);
                return Ok(new { success = result });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi xóa hồ sơ tiêm chủng {profileId}");
                return StatusCode(500, new { message = "Lỗi server nội bộ" });
            }
        }
    }

}
