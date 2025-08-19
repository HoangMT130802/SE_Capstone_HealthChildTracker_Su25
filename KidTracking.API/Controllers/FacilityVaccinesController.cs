using AutoMapper;
using Contracts.DTOs.FacilityVaccine;
using KidTracking.API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repositories.Entities;
using Repositories.Interfaces;
using Services.Interfaces;
using System.Linq.Expressions;
using System.Security.Claims;

namespace KidTracking.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FacilityVaccinesController : ControllerBase
    {
        private readonly IFacilityVaccineService _facilityVaccineService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<FacilityVaccinesController> _logger;

        public FacilityVaccinesController(IFacilityVaccineService facilityVaccineService, IUnitOfWork unitOfWork, IMapper mapper, ILogger<FacilityVaccinesController> logger)
        {
            _facilityVaccineService = facilityVaccineService ?? throw new ArgumentNullException(nameof(facilityVaccineService));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private async Task<bool> IsManager()
        {
            var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountIdClaim) || !int.TryParse(accountIdClaim, out var accountId))
            {
                return false;
            }

            var staffRepository = _unitOfWork.GetRepository<FacilityStaff>();
            var staff = await staffRepository.GetAsync(s => s.AccountId == accountId && s.Position == "Manager,Doctor,Staff");
            return staff != null;
        }

        private int GetAccountId()
        {
            var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountIdClaim) || !int.TryParse(accountIdClaim, out var accountId))
            {
                throw new UnauthorizedAccessException("Không thể xác định AccountId từ token");
            }
            return accountId;
        }

        [HttpGet("{facilityVaccineId}")]
        public async Task<IActionResult> GetFacilityVaccineById(int facilityVaccineId)
        {
            try
            {
                var facilityVaccine = await _facilityVaccineService.GetFacilityVaccineByIdAsync(facilityVaccineId);
                return Ok(facilityVaccine);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting facility vaccine with ID {facilityVaccineId}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllFacilityVaccines(
            [FromQuery] int? facilityId = null,
            [FromQuery] string? status = null,
            [FromQuery] int? pageIndex = null,
            [FromQuery] int? pageSize = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] string include = "")
        {
            try
            {
                // Build filter expression
                Expression<Func<FacilityVaccine, bool>>? filter = null;
                if (facilityId.HasValue || !string.IsNullOrEmpty(status))
                {
                    if (facilityId.HasValue)
                    {
                        filter = fv => fv.FacilityId == facilityId.Value;
                    }
                    if (!string.IsNullOrEmpty(status))
                    {
                        Expression<Func<FacilityVaccine, bool>> statusFilter = fv => fv.Status == status;
                        filter = filter == null ? statusFilter : filter.And(statusFilter);
                    }
                }

                // Build orderBy expression
                Func<IQueryable<FacilityVaccine>, IOrderedQueryable<FacilityVaccine>>? orderBy = null;
                if (!string.IsNullOrEmpty(sortBy))
                {
                    switch (sortBy.ToLower())
                    {
                        case "importdate":
                            orderBy = q => q.OrderBy(fv => fv.ImportDate);
                            break;
                        case "importdate_desc":
                            orderBy = q => q.OrderByDescending(fv => fv.ImportDate);
                            break;
                        case "price":
                            orderBy = q => q.OrderBy(fv => fv.Price);
                            break;
                        case "price_desc":
                            orderBy = q => q.OrderByDescending(fv => fv.Price);
                            break;
                        default:
                            orderBy = q => q.OrderBy(fv => fv.FacilityVaccineId);
                            break;
                    }
                }

                var result = await _facilityVaccineService.GetAllFacilityVaccinesAsync(
                    filter: filter,
                    orderBy: orderBy,
                    include: include,
                    pageIndex: pageIndex,
                    pageSize: pageSize);

                return Ok(new
                {
                    TotalCount = result.TotalCount,
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all facility vaccines");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateFacilityVaccine([FromBody] CreateFacilityVaccineDTO facilityVaccineDto)
        {
            try
            {
                if (!await IsManager())
                {
                    return StatusCode(403, new { message = "Chỉ Manager mới có quyền thực hiện hành động này" });
                }

                var accountId = GetAccountId();
                var facilityVaccine = await _facilityVaccineService.CreateFacilityVaccineAsync(facilityVaccineDto, accountId);
                return CreatedAtAction(nameof(GetFacilityVaccineById), new { facilityVaccineId = facilityVaccine.FacilityVaccineId }, facilityVaccine);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating facility vaccine for FacilityId {facilityVaccineDto.FacilityId} and VaccineId {facilityVaccineDto.VaccineId}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPut("{facilityVaccineId}")]
        public async Task<IActionResult> UpdateFacilityVaccine(int facilityVaccineId, [FromBody] UpdateFacilityVaccineDTO facilityVaccineDto)
        {
            try
            {
                if (!await IsManager())
                {
                    return StatusCode(403, new { message = "Chỉ Manager mới có quyền thực hiện hành động này" });
                }

                var accountId = GetAccountId();
                var facilityVaccine = await _facilityVaccineService.UpdateFacilityVaccineAsync(facilityVaccineId, facilityVaccineDto, accountId);
                return Ok(facilityVaccine);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
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
                _logger.LogError(ex, $"Error updating facility vaccine with ID {facilityVaccineId}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpDelete("{facilityVaccineId}")]
        public async Task<IActionResult> DeleteFacilityVaccine(int facilityVaccineId)
        {
            try
            {
                if (!await IsManager())
                {
                    return StatusCode(403, new { message = "Chỉ Manager mới có quyền thực hiện hành động này" });
                }

                var accountId = GetAccountId();
                var result = await _facilityVaccineService.DeleteFacilityVaccineAsync(facilityVaccineId, accountId);
                return Ok(new { success = result });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting facility vaccine with ID {facilityVaccineId}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}
