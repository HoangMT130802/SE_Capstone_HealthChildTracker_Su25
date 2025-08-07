using Contracts.DTOs.FacilityRating;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace KidTracking.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FacilityRatingController : ControllerBase
    {
        private readonly IFacilityRatingService _ratingService;
        private readonly ILogger<FacilityRatingController> _logger;

        public FacilityRatingController(IFacilityRatingService ratingService, ILogger<FacilityRatingController> logger)
        {
            _ratingService = ratingService ?? throw new ArgumentNullException(nameof(ratingService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateFacilityRating([FromBody] CreateFacilityRatingDTO ratingDto)
        {
            if (ratingDto == null)
            {
                return BadRequest("Rating data is required");
            }

            try
            {
                var rating = await _ratingService.CreateFacilityRatingAsync(ratingDto);
                return Ok(rating);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating facility rating");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetFacilityRatingById(int id)
        {
            try
            {
                var rating = await _ratingService.GetFacilityRatingByIdAsync(id);
                return Ok(rating);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting rating {id}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetFacilityRatings([FromQuery] int? facilityId = null, [FromQuery] int? memberId = null, [FromQuery] int? pageIndex = null, [FromQuery] int? pageSize = null)
        {
            try
            {
                var ratings = await _ratingService.GetFacilityRatingsAsync(facilityId, memberId, pageIndex, pageSize);
                return Ok(ratings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting ratings for FacilityId {facilityId}, MemberId {memberId}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateFacilityRating(int id, [FromBody] UpdateFacilityRatingDTO ratingDto)
        {
            if (ratingDto == null)
            {
                return BadRequest("Rating data is required");
            }

            try
            {
                var rating = await _ratingService.UpdateFacilityRatingAsync(id, ratingDto);
                return Ok(rating);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating rating {id}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteFacilityRating(int id)
        {
            try
            {
                await _ratingService.DeleteFacilityRatingAsync(id);
                return Ok(new { message = $"Rating with ID {id} has been successfully deleted." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting rating {id}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("average/{facilityId}")]
        public async Task<IActionResult> GetAverageRatingByFacility(int facilityId)
        {
            try
            {
                var averageRating = await _ratingService.GetAverageRatingByFacilityAsync(facilityId);
                return Ok(new { facilityId = facilityId, averageRating = averageRating });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting average rating for FacilityId {facilityId}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}