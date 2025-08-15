using Contracts.DTOs.VaccinationFacility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using System.Security.Claims;

namespace KidTracking.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VaccinationFacilitiesController : ControllerBase
    {
        private readonly IVaccinationFacilityService _facilityService;

        public VaccinationFacilitiesController(IVaccinationFacilityService facilityService)
        {
            _facilityService = facilityService ?? throw new ArgumentNullException(nameof(facilityService));
        }

        [HttpGet]
        public async Task<IActionResult> GetAllFacilities([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var result = await _facilityService.GetAllFacilitiesAsync(pageIndex, pageSize);
                return Ok(new
                {
                    Success = true,
                    Message = "Lấy danh sách cơ sở thành công",
                    Data = result.Data,
                    TotalCount = result.TotalCount,
                    PageIndex = pageIndex,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetFacilityById(int id)
        {
            try
            {
                var facility = await _facilityService.GetFacilityByIdAsync(id);
                if (facility == null)
                {
                    return NotFound(new
                    {
                        Success = false,
                        Message = "Không tìm thấy cơ sở"
                    });
                }

                return Ok(new
                {
                    Success = true,
                    Message = "Lấy thông tin cơ sở thành công",
                    Data = facility
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        [HttpGet("my-facility")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> GetMyFacility()
        {
            try
            {
                var accountIdClaim = User.FindFirst("AccountId")?.Value;
                if (string.IsNullOrEmpty(accountIdClaim) || !int.TryParse(accountIdClaim, out int accountId))
                {
                    return Unauthorized(new
                    {
                        Success = false,
                        Message = "Không thể xác thực người dùng"
                    });
                }

                var facility = await _facilityService.GetFacilityByManagerIdAsync(accountId);
                if (facility == null)
                {
                    return NotFound(new
                    {
                        Success = false,
                        Message = "Bạn chưa có cơ sở nào"
                    });
                }

                return Ok(new
                {
                    Success = true,
                    Message = "Lấy thông tin cơ sở của bạn thành công",
                    Data = facility
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        [HttpPost]
        // Manager có role FacilityStaff
        public async Task<IActionResult> CreateFacility([FromBody] CreateVaccinationFacilityDTO createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "Dữ liệu không hợp lệ",
                    Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });
            }

            try
            {
                var accountIdClaim = User.FindFirst("AccountId")?.Value;
                if (string.IsNullOrEmpty(accountIdClaim) || !int.TryParse(accountIdClaim, out int accountId))
                {
                    return Unauthorized(new
                    {
                        Success = false,
                        Message = "Không thể xác thực người dùng"
                    });
                }

                var facility = await _facilityService.CreateFacilityAsync(createDto, accountId);
                return CreatedAtAction(nameof(GetFacilityById), new { id = facility.FacilityId }, new
                {
                    Success = true,
                    Message = "Tạo cơ sở thành công",
                    Data = facility
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> UpdateFacility(int id, [FromBody] UpdateVaccinationFacilityDTO updateDto)
        {
            if (id != updateDto.FacilityId)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "ID trong URL không khớp với ID trong dữ liệu"
                });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "Dữ liệu không hợp lệ",
                    Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });
            }

            try
            {
                var accountIdClaim = User.FindFirst("AccountId")?.Value;
                if (string.IsNullOrEmpty(accountIdClaim) || !int.TryParse(accountIdClaim, out int accountId))
                {
                    return Unauthorized(new
                    {
                        Success = false,
                        Message = "Không thể xác thực người dùng"
                    });
                }

                var facility = await _facilityService.UpdateFacilityAsync(updateDto, accountId);
                if (facility == null)
                {
                    return NotFound(new
                    {
                        Success = false,
                        Message = "Không tìm thấy cơ sở"
                    });
                }

                return Ok(new
                {
                    Success = true,
                    Message = "Cập nhật cơ sở thành công",
                    Data = facility
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> DeleteFacility(int id)
        {
            try
            {
                var accountIdClaim = User.FindFirst("AccountId")?.Value;
                if (string.IsNullOrEmpty(accountIdClaim) || !int.TryParse(accountIdClaim, out int accountId))
                {
                    return Unauthorized(new
                    {
                        Success = false,
                        Message = "Không thể xác thực người dùng"
                    });
                }

                var result = await _facilityService.DeleteFacilityAsync(id, accountId);
                if (!result)
                {
                    return NotFound(new
                    {
                        Success = false,
                        Message = "Không tìm thấy cơ sở"
                    });
                }

                return Ok(new
                {
                    Success = true,
                    Message = "Xóa cơ sở thành công"
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        [HttpGet("check-manager-facility")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> CheckManagerHasFacility()
        {
            try
            {
                var accountIdClaim = User.FindFirst("AccountId")?.Value;
                if (string.IsNullOrEmpty(accountIdClaim) || !int.TryParse(accountIdClaim, out int accountId))
                {
                    return Unauthorized(new
                    {
                        Success = false,
                        Message = "Không thể xác thực người dùng"
                    });
                }

                var hasFacility = await _facilityService.CheckManagerHasFacilityAsync(accountId);
                return Ok(new
                {
                    Success = true,
                    Message = "Kiểm tra thành công",
                    Data = new { HasFacility = hasFacility }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }
    }
} 