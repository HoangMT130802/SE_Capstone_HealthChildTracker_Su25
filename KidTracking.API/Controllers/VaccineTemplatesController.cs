using Contracts.DTOs.VaccineTemplate;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace KidTracking.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VaccineTemplatesController : ControllerBase
    {
        private readonly IVaccineTemplateService _vaccineTemplateService;
        private readonly ILogger<VaccineTemplatesController> _logger;

        public VaccineTemplatesController(IVaccineTemplateService vaccineTemplateService, ILogger<VaccineTemplatesController> logger)
        {
            _vaccineTemplateService = vaccineTemplateService;
            _logger = logger;
        }

        private bool IsAdminOrDoctor()
        {
            return User.IsInRole("Admin") || User.IsInRole("Member") || User.IsInRole("FacilityStaff");
        }

        [HttpPost]
        public async Task<IActionResult> CreateVaccineTemplate([FromBody] CreateVaccineTemplateDTO vaccineTemplateDto)
        {
            try
            {
                if (!IsAdminOrDoctor())
                {
                    return StatusCode(403, new { message = "Bạn không có quyền thực hiện hành động này" });
                }

                var vaccineTemplate = await _vaccineTemplateService.CreateVaccineTemplateAsync(vaccineTemplateDto);
                return CreatedAtAction(nameof(GetVaccineTemplate), new { vaccineTemplateId = vaccineTemplate.Id }, vaccineTemplate);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating vaccine template");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPut("{vaccineTemplateId}")]
        public async Task<IActionResult> UpdateVaccineTemplate(int vaccineTemplateId, [FromBody] UpdateVaccineTemplateDTO vaccineTemplateDto)
        {
            try
            {
                if (!IsAdminOrDoctor())
                {
                    return StatusCode(403, new { message = "Bạn không có quyền thực hiện hành động này" });
                }

                var vaccineTemplate = await _vaccineTemplateService.UpdateVaccineTemplateAsync(vaccineTemplateId, vaccineTemplateDto);
                return Ok(vaccineTemplate);
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
                _logger.LogError(ex, $"Error updating vaccine template with ID {vaccineTemplateId}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("{vaccineTemplateId}")]
        public async Task<IActionResult> GetVaccineTemplate(int vaccineTemplateId)
        {
            try
            {
                var vaccineTemplate = await _vaccineTemplateService.GetVaccineTemplateByIdAsync(vaccineTemplateId);
                return Ok(vaccineTemplate);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving vaccine template with ID {vaccineTemplateId}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllVaccineTemplates([FromQuery] string? diseaseName = null, [FromQuery] int? diseaseId = null, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var vaccineTemplates = await _vaccineTemplateService.GetAllVaccineTemplatesAsync(diseaseName, diseaseId, pageNumber, pageSize);
                return Ok(vaccineTemplates);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all vaccine templates");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}
