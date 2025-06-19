using Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Contracts.DTOs.GrowthStandard;
namespace KidTracking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GrowthStandardController : ControllerBase
    {
        private readonly IGrowthStandardService _growthStandardService;
        private readonly ILogger<GrowthStandardController> _logger;

        public GrowthStandardController(
            IGrowthStandardService growthStandardService,
            ILogger<GrowthStandardController> logger)
        {
            _growthStandardService = growthStandardService ?? throw new ArgumentNullException(nameof(growthStandardService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        
        [HttpGet("height")]
        [ProducesResponseType(typeof(IEnumerable<GrowthStandardDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<GrowthStandardDTO>>> GetHeightStandards(
            [FromQuery] string gender,
            [FromQuery] int? ageInMonths)
        {
            try
            {
                if (string.IsNullOrEmpty(gender))
                {
                    return BadRequest("Giới tính không được để trống");
                }

                var standards = await _growthStandardService.GetHeightStandardsAsync(gender, ageInMonths);
                return Ok(standards);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy dữ liệu chuẩn chiều cao");
                return StatusCode(500, "Đã xảy ra lỗi khi xử lý yêu cầu");
            }
        }

      
        [HttpGet("weight")]
        [ProducesResponseType(typeof(IEnumerable<GrowthStandardDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<GrowthStandardDTO>>> GetWeightStandards(
            [FromQuery] string gender,
            [FromQuery] int? ageInMonths)
        {
            try
            {
                if (string.IsNullOrEmpty(gender))
                {
                    return BadRequest("Giới tính không được để trống");
                }

                var standards = await _growthStandardService.GetWeightStandardsAsync(gender, ageInMonths);
                return Ok(standards);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy dữ liệu chuẩn cân nặng");
                return StatusCode(500, "Đã xảy ra lỗi khi xử lý yêu cầu");
            }
        }

        [HttpGet("bmi")]
        [ProducesResponseType(typeof(IEnumerable<GrowthStandardDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<GrowthStandardDTO>>> GetBMIStandards(
            [FromQuery] string gender,
            [FromQuery] int? ageInMonths)
        {
            try
            {
                if (string.IsNullOrEmpty(gender))
                {
                    return BadRequest("Giới tính không được để trống");
                }

                var standards = await _growthStandardService.GetBMIStandardsAsync(gender, ageInMonths);
                return Ok(standards);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy dữ liệu chuẩn BMI");
                return StatusCode(500, "Đã xảy ra lỗi khi xử lý yêu cầu");
            }
        }

       
        [HttpGet("head-circumference")]
        [ProducesResponseType(typeof(IEnumerable<GrowthStandardDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<GrowthStandardDTO>>> GetHeadCircumferenceStandards(
            [FromQuery] string gender,
            [FromQuery] int? ageInMonths)
        {
            try
            {
                if (string.IsNullOrEmpty(gender))
                {
                    return BadRequest("Giới tính không được để trống");
                }

                var standards = await _growthStandardService.GetHeadCircumferenceStandardsAsync(gender, ageInMonths);
                return Ok(standards);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy dữ liệu chuẩn vòng đầu");
                return StatusCode(500, "Đã xảy ra lỗi khi xử lý yêu cầu");
            }
        }
    }
}



