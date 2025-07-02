using Contracts.DTOs.VaccinePackage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace KidTracking.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class VaccinePackagesController : ControllerBase
    {
        private readonly IVaccinePackageService _vaccinePackageService;
        private readonly ILogger<VaccinePackagesController> _logger;

        public VaccinePackagesController(IVaccinePackageService vaccinePackageService, ILogger<VaccinePackagesController> logger)
        {
            _vaccinePackageService = vaccinePackageService ?? throw new ArgumentNullException(nameof(vaccinePackageService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private bool IsAdminOrDoctor()
        {
            return User.IsInRole("Admin") || User.IsInRole("Manager");
        }

        [HttpPost]
        public async Task<IActionResult> CreateVaccinePackage([FromBody] CreateVaccinePackageDTO vaccinePackageDto)
        {
            try
            {
                if (!IsAdminOrDoctor())
                {
                    return StatusCode(403, new { message = "Bạn không có quyền thực hiện hành động này" });
                }

                var vaccinePackage = await _vaccinePackageService.CreateVaccinePackageAsync(vaccinePackageDto);
                return CreatedAtAction(nameof(GetVaccinePackageById), new { packageId = vaccinePackage.PackageId }, vaccinePackage);
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
                if (!IsAdminOrDoctor())
                {
                    return StatusCode(403, new { message = "Bạn không có quyền thực hiện hành động này" });
                }

                var vaccinePackage = await _vaccinePackageService.CreateVaccinePackageWithVaccinesAsync(vaccinePackageDto);
                return CreatedAtAction(nameof(GetVaccinePackageById), new { packageId = vaccinePackage.PackageId }, vaccinePackage);
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
                if (!IsAdminOrDoctor())
                {
                    return StatusCode(403, new { message = "Bạn không có quyền thực hiện hành động này" });
                }

                var packageVaccine = await _vaccinePackageService.AddVaccineToPackageAsync(packageId, packageVaccineDto);
                return CreatedAtAction(nameof(GetVaccinePackageById), new { packageId = packageVaccine.PackageId }, packageVaccine);
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

        [HttpPut("{packageId}")]
        public async Task<IActionResult> UpdateVaccinePackage(int packageId, [FromBody] UpdateVaccinePackageDTO vaccinePackageDto)
        {
            try
            {
                if (!IsAdminOrDoctor())
                {
                    return StatusCode(403, new { message = "Bạn không có quyền thực hiện hành động này" });
                }

                var vaccinePackage = await _vaccinePackageService.UpdateVaccinePackageAsync(packageId, vaccinePackageDto);
                return Ok(vaccinePackage);
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
                if (!IsAdminOrDoctor())
                {
                    return StatusCode(403, new { message = "Bạn không có quyền thực hiện hành động này" });
                }

                var packageVaccine = await _vaccinePackageService.UpdateVaccineInPackageAsync(packageId, vaccineId, packageVaccineDto);
                return Ok(packageVaccine);
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
                if (!IsAdminOrDoctor())
                {
                    return StatusCode(403, new { message = "Bạn không có quyền thực hiện hành động này" });
                }

                var result = await _vaccinePackageService.DeleteVaccinePackageAsync(packageId);
                return Ok(new { success = result });
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
                if (!IsAdminOrDoctor())
                {
                    return StatusCode(403, new { message = "Bạn không có quyền thực hiện hành động này" });
                }

                var result = await _vaccinePackageService.DeleteVaccineFromPackageAsync(packageId, vaccineId);
                return Ok(new { success = result });
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
