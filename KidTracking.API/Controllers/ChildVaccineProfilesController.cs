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
                var currentAccountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentAccountIdClaim) || !int.TryParse(currentAccountIdClaim, out int currentAccountId))
                {
                    _logger.LogWarning("Invalid token for ValidateChildAccess request");
                    return false;
                }

                if (User.IsInRole("Admin") || User.IsInRole("FacilityStaff"))
                {
                    return true;
                }

                // Tra cứu MemberId từ AccountId
                var memberRepository = _unitOfWork.GetRepository<Member>();
                var member = await memberRepository.GetAsync(m => m.AccountId == currentAccountId);
                if (member == null)
                {
                    _logger.LogWarning($"No Member found for AccountId {currentAccountId}");
                    return false;
                }

                var currentMemberId = member.MemberId;

                var childRepository = _unitOfWork.GetRepository<Child>();
                var child = await childRepository.GetAsync(c => c.ChildId == childId && c.MemberId == currentMemberId);
                return child != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error validating child access for childId {childId}");
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

        /// <summary>
        /// API public để lấy vaccine profiles của child mà không cần check account ownership
        /// </summary>
        [HttpGet("public/child/{childId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllChildVaccineProfilesByChildIdPublic(int childId)
        {
            try
            {
                var profiles = await _childVaccineProfileService.GetAllChildVaccineProfilesByChildIdPublicAsync(childId);
                return Ok(profiles);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy hồ sơ tiêm chủng public cho trẻ {childId}");
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

        /// <summary>
        /// Doctor ghi nhận hoàn thành tiêm vaccine và tạo mũi tiếp theo nếu cần
        /// </summary>
        [HttpPost("complete-vaccination")]
        [Authorize(Roles = "FacilityStaff")]
        public async Task<IActionResult> CompleteVaccination([FromBody] CompleteVaccinationDTO completeDto)
        {
            try
            {
                _logger.LogInformation("Doctor completing vaccination for Appointment {AppointmentId}, FacilityVaccine {FacilityVaccineId}, Dose {DoseNumber}", 
                    completeDto.AppointmentId, completeDto.FacilityVaccineId, completeDto.DoseNumber);

                // Lấy current user ID từ token
                var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserIdClaim) || !int.TryParse(currentUserIdClaim, out int currentUserId))
                {
                    return Unauthorized("Không thể xác định tài khoản người dùng");
                }

                var result = await _childVaccineProfileService.CompleteVaccinationAsync(completeDto, currentUserId);
                
                _logger.LogInformation("Successfully completed vaccination for Appointment {AppointmentId}, FacilityVaccine {FacilityVaccineId}", 
                    completeDto.AppointmentId, completeDto.FacilityVaccineId);
                    
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Not found when completing vaccination");
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation when completing vaccination");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi complete vaccination cho Appointment {AppointmentId}, FacilityVaccine {FacilityVaccineId}", 
                    completeDto.AppointmentId, completeDto.FacilityVaccineId);
                return StatusCode(500, new { message = "Lỗi server nội bộ" });
            }
        }
        [HttpGet("record/{childId}")]
        public async Task<IActionResult> GetVaccineRecord(int childId, [FromQuery] int? diseaseId = null)
        {
            try
            {
                var vaccineRecords = await _childVaccineProfileService.GetVaccineRecordAsync(childId, diseaseId);
                return Ok(vaccineRecords);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving vaccine record for ChildId {childId}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }

}
