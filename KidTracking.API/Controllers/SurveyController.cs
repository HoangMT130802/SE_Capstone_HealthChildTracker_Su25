using Contracts.DTOs.Survey;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace KidTracking.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SurveyController : ControllerBase
    {
        private readonly ISurveyService _surveyService;
        private readonly ILogger<SurveyController> _logger;

        public SurveyController(ISurveyService surveyService, ILogger<SurveyController> logger)
        {
            _surveyService = surveyService ?? throw new ArgumentNullException(nameof(surveyService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost]
        public async Task<IActionResult> CreateSurvey([FromBody] CreateSurveyDto surveyDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var accountId = GetAccountId();
                var surveyId = await _surveyService.CreateSurveyAsync(surveyDto, accountId);
                return Ok(new { surveyId, message = "Survey created successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating survey");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
        [HttpPost("{surveyId}/questions")]
        public async Task<IActionResult> CreateSurveyQuestion(int surveyId, [FromBody] CreateSurveyQuestionDto questionDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var accountId = GetAccountId();
                var questionId = await _surveyService.CreateSurveyQuestionAsync(surveyId, questionDto, accountId);
                return Ok(new { questionId, message = "Survey question created successfully" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating survey question for SurveyId {surveyId}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("{surveyId}/questions")]
        public async Task<IActionResult> GetQuestionsBySurveyId(int surveyId, [FromQuery] int? pageIndex = 1, [FromQuery] int? pageSize = 10)
        {
            try
            {
                if (pageIndex <= 0 || pageSize <= 0)
                    return BadRequest(new { message = "PageIndex and PageSize must be positive" });

                var questions = await _surveyService.GetQuestionsBySurveyIdAsync(surveyId, pageIndex, pageSize);
                return Ok(new
                {
                    totalCount = questions.TotalCount,
                    pageIndex,
                    pageSize,
                    data = questions.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting questions for survey ID {surveyId}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("{appointmentId}/submit")]
        public async Task<IActionResult> SubmitAppointmentSurvey(int appointmentId, [FromBody] IEnumerable<AppointmentSurveyDto> answers)
        {
            if (answers == null || !answers.Any())
            {
                return BadRequest(new { message = "At least one answer is required" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var accountId = GetAccountId();
                await _surveyService.SubmitAppointmentSurveyAsync(appointmentId, answers, accountId);
                return Ok(new { success = true, message = "Survey submitted successfully" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error submitting survey for appointment ID {appointmentId}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("{appointmentId}/responses")]
        public async Task<IActionResult> GetSurveyResponsesByAppointmentId(int appointmentId, [FromQuery] int? pageIndex = 1, [FromQuery] int? pageSize = 10)
        {
            try
            {
                if (pageIndex <= 0 || pageSize <= 0)
                    return BadRequest(new { message = "PageIndex and PageSize must be positive" });

                var responses = await _surveyService.GetSurveyResponsesByAppointmentIdAsync(appointmentId, pageIndex, pageSize);
                return Ok(new
                {
                    totalCount = responses.TotalCount,
                    pageIndex,
                    pageSize,
                    data = responses.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting survey responses for appointment ID {appointmentId}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpDelete("{surveyId}")]
        public async Task<IActionResult> DeleteSurvey(int surveyId)
        {
            try
            {
                var accountId = GetAccountId();
                await _surveyService.DeleteSurveyAsync(surveyId, accountId);
                return Ok(new { message = "Survey deleted successfully" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message }); 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting survey with ID {surveyId}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPut("{surveyId}")]
        public async Task<IActionResult> UpdateSurvey(int surveyId, [FromBody] CreateSurveyDto surveyDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var accountId = GetAccountId();
                await _surveyService.UpdateSurveyAsync(surveyId, surveyDto, accountId);
                return Ok(new { message = "Survey updated successfully" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message }); 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating survey with ID {surveyId}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        private int GetAccountId()
        {
            var accountId = int.Parse(User.FindFirst("AccountId")?.Value ?? "0");
            if (accountId == 0) throw new UnauthorizedAccessException("AccountId not found in token");
            return accountId;
        }
    }
}