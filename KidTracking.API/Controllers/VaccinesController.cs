using Contracts.DTOs.Vaccine;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace KidTracking.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class VaccinesController : ControllerBase
    {
        private readonly IVaccineService _vaccineService;
        private readonly ILogger<VaccinesController> _logger;

        public VaccinesController(IVaccineService vaccineService, ILogger<VaccinesController> logger)
        {
            _vaccineService = vaccineService ?? throw new ArgumentNullException(nameof(vaccineService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private bool IsAdminOrDoctor()
        {
            return User.IsInRole("Admin") || User.IsInRole("Doctor");
        }

        [HttpPost]
        public async Task<IActionResult> CreateVaccine([FromBody] CreateVaccineDTO vaccineDto)
        {
            try
            {
                if (!IsAdminOrDoctor())
                {
                    return StatusCode(403, new { message = "Bạn không có quyền thực hiện hành động này" });
                }

                var vaccine = await _vaccineService.CreateVaccineAsync(vaccineDto);
                return CreatedAtAction(nameof(GetVaccineById), new { vaccineId = vaccine.VaccineId }, vaccine);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating vaccine with name {vaccineDto.Name}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("{vaccineId}")]
        public async Task<IActionResult> GetVaccineById(int vaccineId)
        {
            try
            {
                var vaccine = await _vaccineService.GetVaccineByIdAsync(vaccineId);
                return Ok(vaccine);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting vaccine with ID {vaccineId}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetAllVaccines(int? diseaseId = null)
        {
            try
            {
                var vaccines = await _vaccineService.GetAllVaccinesAsync(diseaseId);
                return Ok(vaccines);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all vaccines");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
        //[HttpGet]
        //public async Task<IActionResult> GetAllVaccines()
        //{
        //    try
        //    {
        //        var vaccines = await _vaccineService.GetAllVaccinesAsync();
        //        return Ok(vaccines);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error getting all vaccines");
        //        return StatusCode(500, new { message = "Internal server error" });
        //    }
        //}

        [HttpPut("{vaccineId}")]
        public async Task<IActionResult> UpdateVaccine(int vaccineId, [FromBody] UpdateVaccineDTO vaccineDto)
        {
            try
            {
                if (!IsAdminOrDoctor())
                {
                    return StatusCode(403, new { message = "Bạn không có quyền thực hiện hành động này" });
                }

                var vaccine = await _vaccineService.UpdateVaccineAsync(vaccineId, vaccineDto);
                return Ok(vaccine);
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
                _logger.LogError(ex, $"Error updating vaccine with ID {vaccineId}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpDelete("{vaccineId}")]
        public async Task<IActionResult> DeleteVaccine(int vaccineId)
        {
            try
            {
                if (!IsAdminOrDoctor())
                {
                    return StatusCode(403, new { message = "Bạn không có quyền thực hiện hành động này" });
                }

                var result = await _vaccineService.DeleteVaccineAsync(vaccineId);
                return Ok(new { success = result });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting vaccine with ID {vaccineId}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}
