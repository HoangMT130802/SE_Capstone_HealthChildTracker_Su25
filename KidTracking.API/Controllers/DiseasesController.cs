using Contracts.DTOs.Disease;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace KidTracking.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DiseasesController : ControllerBase
    {
        private readonly IDiseaseService _diseaseService;
        private readonly ILogger<DiseasesController> _logger;

        public DiseasesController(IDiseaseService diseaseService, ILogger<DiseasesController> logger)
        {
            _diseaseService = diseaseService ?? throw new ArgumentNullException(nameof(diseaseService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private bool IsAdmin()
        {
            return User.IsInRole("Admin") ;
        }

        [HttpPost]
        public async Task<IActionResult> CreateDisease([FromBody] CreateDiseaseDTO diseaseDto)
        {
            try
            {
                if (!IsAdmin())
                {
                    return StatusCode(403, new { message = "Bạn không có quyền thực hiện hành động này" });
                }

                var disease = await _diseaseService.CreateDiseaseAsync(diseaseDto);
                return CreatedAtAction(nameof(GetDiseaseById), new { diseaseId = disease.DiseaseId }, disease);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating disease with name {diseaseDto.Name}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("{diseaseId}")]
        public async Task<IActionResult> GetDiseaseById(int diseaseId)
        {
            try
            {
                var disease = await _diseaseService.GetDiseaseByIdAsync(diseaseId);
                return Ok(disease);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting disease with ID {diseaseId}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllDiseases()
        {
            try
            {
                var diseases = await _diseaseService.GetAllDiseasesAsync();
                return Ok(diseases);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all diseases");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPut("{diseaseId}")]
        public async Task<IActionResult> UpdateDisease(int diseaseId, [FromBody] UpdateDiseaseDTO diseaseDto)
        {
            try
            {
                if (!IsAdmin())
                {
                    return StatusCode(403, new { message = "Bạn không có quyền thực hiện hành động này" });
                }

                var disease = await _diseaseService.UpdateDiseaseAsync(diseaseId, diseaseDto);
                return Ok(disease);
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
                _logger.LogError(ex, $"Error updating disease with ID {diseaseId}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpDelete("{diseaseId}")]
        public async Task<IActionResult> DeleteDisease(int diseaseId)
        {
            try
            {
                if (!IsAdmin())
                {
                    return StatusCode(403, new { message = "Bạn không có quyền thực hiện hành động này" });
                }

                var result = await _diseaseService.DeleteDiseaseAsync(diseaseId);
                return Ok(new { success = result });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting disease with ID {diseaseId}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}
