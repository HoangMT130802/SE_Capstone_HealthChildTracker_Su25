using AutoMapper;
using Contracts.DTOs.VaccinePackage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repositories.Interfaces;
using Services.Interfaces;
using System.Security.Claims;

namespace KidTracking.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class VaccinePackagesController : ControllerBase
    {
        private readonly IVaccinePackageService _vaccinePackageService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<VaccinePackagesController> _logger;

        public VaccinePackagesController(IVaccinePackageService vaccinePackageService, IUnitOfWork unitOfWork, IMapper mapper, ILogger<VaccinePackagesController> logger)
        {
            _vaccinePackageService = vaccinePackageService ?? throw new ArgumentNullException(nameof(vaccinePackageService));
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

            var staffRepository = _unitOfWork.GetRepository<Repositories.Entities.FacilityStaff>();
            var staff = await staffRepository.GetAsync(s => s.AccountId == accountId && s.Position == "Manager");
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

        [HttpGet("{packageId}")]
        public async Task<IActionResult> GetVaccinePackageById(int packageId)
        {
            try
            {
                var vaccinePackage = await _vaccinePackageService.GetVaccinePackageByIdAsync(packageId);
                return Ok(vaccinePackage);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting vaccine package with ID {packageId}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllVaccinePackages()
        {
            try
            {
                var vaccinePackages = await _vaccinePackageService.GetAllVaccinePackagesAsync();
                return Ok(vaccinePackages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all vaccine packages");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateVaccinePackage([FromBody] CreateVaccinePackageDTO vaccinePackageDto)
        {
            try
            {
                if (!await IsManager())
                {
                    return StatusCode(403, new { message = "Chỉ Manager mới có quyền thực hiện hành động này" });
                }

                var accountId = GetAccountId();
                var vaccinePackage = await _vaccinePackageService.CreateVaccinePackageAsync(vaccinePackageDto, accountId);
                return CreatedAtAction(nameof(GetVaccinePackageById), new { packageId = vaccinePackage.PackageId }, vaccinePackage);
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
                _logger.LogError(ex, $"Error creating vaccine package with name {vaccinePackageDto.Name}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("with-vaccines")]
        public async Task<IActionResult> CreateVaccinePackageWithVaccines([FromBody] CreateVaccinePackageWithVaccinesDTO vaccinePackageDto)
        {
            try
            {
                if (!await IsManager())
                {
                    return StatusCode(403, new { message = "Chỉ Manager mới có quyền thực hiện hành động này" });
                }

                var accountId = GetAccountId();
                var vaccinePackage = await _vaccinePackageService.CreateVaccinePackageWithVaccinesAsync(vaccinePackageDto, accountId);
                return CreatedAtAction(nameof(GetVaccinePackageById), new { packageId = vaccinePackage.PackageId }, vaccinePackage);
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
                _logger.LogError(ex, $"Error creating vaccine package with name {vaccinePackageDto.Name} and vaccines");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("{packageId}/vaccines")]
        public async Task<IActionResult> AddVaccineToPackage(int packageId, [FromBody] CreatePackageVaccineDTO packageVaccineDto)
        {
            try
            {
                if (!await IsManager())
                {
                    return StatusCode(403, new { message = "Chỉ Manager mới có quyền thực hiện hành động này" });
                }

                var accountId = GetAccountId();
                var packageVaccine = await _vaccinePackageService.AddVaccineToPackageAsync(packageId, packageVaccineDto, accountId);
                return CreatedAtAction(nameof(GetVaccinePackageById), new { packageId = packageVaccine.PackageId }, packageVaccine);
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
                _logger.LogError(ex, $"Error adding vaccine to package with PackageId {packageId} and VaccineId {packageVaccineDto.VaccineId}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPut("{packageId}")]
        public async Task<IActionResult> UpdateVaccinePackage(int packageId, [FromBody] UpdateVaccinePackageDTO vaccinePackageDto)
        {
            try
            {
                if (!await IsManager())
                {
                    return StatusCode(403, new { message = "Chỉ Manager mới có quyền thực hiện hành động này" });
                }

                var accountId = GetAccountId();
                var vaccinePackage = await _vaccinePackageService.UpdateVaccinePackageAsync(packageId, vaccinePackageDto, accountId);
                return Ok(vaccinePackage);
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
                _logger.LogError(ex, $"Error updating vaccine package with ID {packageId}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPut("{packageId}/vaccines/{vaccineId}")]
        public async Task<IActionResult> UpdateVaccineInPackage(int packageId, int vaccineId, [FromBody] UpdatePackageVaccineDTO packageVaccineDto)
        {
            try
            {
                if (!await IsManager())
                {
                    return StatusCode(403, new { message = "Chỉ Manager mới có quyền thực hiện hành động này" });
                }

                var accountId = GetAccountId();
                var packageVaccine = await _vaccinePackageService.UpdateVaccineInPackageAsync(packageId, vaccineId, packageVaccineDto, accountId);
                return Ok(packageVaccine);
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
                _logger.LogError(ex, $"Error updating vaccine in package with PackageId {packageId} and VaccineId {vaccineId}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpDelete("{packageId}")]
        public async Task<IActionResult> DeleteVaccinePackage(int packageId)
        {
            try
            {
                if (!await IsManager())
                {
                    return StatusCode(403, new { message = "Chỉ Manager mới có quyền thực hiện hành động này" });
                }

                var accountId = GetAccountId();
                var result = await _vaccinePackageService.DeleteVaccinePackageAsync(packageId, accountId);
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
                _logger.LogError(ex, $"Error deleting vaccine package with ID {packageId}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpDelete("{packageId}/vaccines/{vaccineId}")]
        public async Task<IActionResult> DeleteVaccineFromPackage(int packageId, int vaccineId)
        {
            try
            {
                if (!await IsManager())
                {
                    return StatusCode(403, new { message = "Chỉ Manager mới có quyền thực hiện hành động này" });
                }

                var accountId = GetAccountId();
                var result = await _vaccinePackageService.DeleteVaccineFromPackageAsync(packageId, vaccineId, accountId);
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
                _logger.LogError(ex, $"Error deleting vaccine from package with PackageId {packageId} and VaccineId {vaccineId}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}