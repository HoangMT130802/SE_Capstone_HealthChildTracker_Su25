using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using Contracts.DTOs.GrowthAssessment;
using Repositories.Entities;
using System.Security.Claims;

namespace KidTracking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AITestController : ControllerBase
    {
        private readonly IAIRecommendationService _aiService;
        private readonly ILogger<AITestController> _logger;

        public AITestController(
            IAIRecommendationService aiService,
            ILogger<AITestController> logger)
        {
            _aiService = aiService;
            _logger = logger;
        }

        /// <summary>
        /// Test AI service trực tiếp với dữ liệu mẫu
        /// </summary>
        [HttpPost("test-ai")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> TestAI()
        {
            try
            {
                _logger.LogInformation("🧪 Bắt đầu test AI service...");

                // Tạo dữ liệu test
                var testContext = new GrowthAssessmentContext
                {
                    Child = new ChildInfo
                    {
                        ChildId = 1,
                        FullName = "Test Child",
                        BirthDate = DateTime.Now.AddMonths(-24), // 2 tuổi
                        Gender = "Male",
                        AgeInMonths = 24
                    },
                    RecentRecords = new List<GrowthRecord>
                    {
                        new GrowthRecord
                        {
                            RecordId = 1,
                            ChildId = 1,
                            Height = 85.5m,
                            Weight = 12.3m,
                            Bmi = 16.8m,
                            HeadCircumference = 48.2m,
                            CreatedAt = DateTime.Now.AddDays(-30)
                        },
                        new GrowthRecord
                        {
                            RecordId = 2,
                            ChildId = 1,
                            Height = 87.2m,
                            Weight = 12.8m,
                            Bmi = 16.9m,
                            HeadCircumference = 48.5m,
                            CreatedAt = DateTime.Now
                        }
                    },
                    CurrentAssessment = new GrowthAssessmentsDTO
                    {
                        HeightStatus = "Bình thường",
                        WeightStatus = "Bình thường",
                        BMIStatus = "Bình thường",
                        HeadCircumferenceStatus = "Bình thường"
                    },
                    Predictions = new List<PredictionDataPointDTO>
                    {
                        new PredictionDataPointDTO
                        {
                            PredictedDate = DateTime.Now.AddDays(90),
                            AgeInDays = 24 * 30 + 90,
                            PredictedHeight = 89.5m,
                            PredictedWeight = 13.2m,
                            PredictedBMI = 16.5m,
                            PredictedHeadCircumference = 48.8m,
                            TimeLabel = "3 tháng"
                        }
                    },
                    Quality = new PredictionQualityDTO
                    {
                        OverallConfidence = 85.5,
                        ConfidenceLevel = "Cao",
                        DataQualityDescription = "Dữ liệu tốt",
                        DataPointsUsed = 2,
                        TrendConsistency = 90.0,
                        QualityWarnings = new List<string>()
                    },
                    HeightTrend = 0.056, // Tăng trưởng tốt
                    WeightTrend = 0.016  // Tăng cân ổn định
                };

                _logger.LogInformation("📊 Test context đã tạo: Child={ChildName}, Records={RecordCount}", 
                    testContext.Child.FullName, testContext.RecentRecords.Count);

                // Gọi AI
                var aiResult = await _aiService.GenerateGrowthRecommendationsAsync(testContext);

                _logger.LogInformation("✅ AI test thành công! Độ dài response: {Length}", aiResult?.Length ?? 0);

                return Ok(new
                {
                    success = true,
                    message = "AI test thành công",
                    responseLength = aiResult?.Length ?? 0,
                    response = aiResult,
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ AI test thất bại: {ErrorMessage}", ex.Message);
                
                return StatusCode(500, new
                {
                    success = false,
                    message = "AI test thất bại",
                    error = ex.Message,
                    stackTrace = ex.StackTrace,
                    timestamp = DateTime.Now
                });
            }
        }

        /// <summary>
        /// Test AI basic assessment
        /// </summary>
        [HttpPost("test-ai-basic")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> TestAIBasic()
        {
            try
            {
                _logger.LogInformation("🧪 Bắt đầu test AI basic assessment...");

                var testContext = new BasicAssessmentContext
                {
                    Child = new ChildInfo
                    {
                        ChildId = 1,
                        FullName = "Test Child Basic",
                        BirthDate = DateTime.Now.AddMonths(-18), // 18 tháng
                        Gender = "Female",
                        AgeInMonths = 18
                    },
                    CurrentRecord = new GrowthRecord
                    {
                        RecordId = 1,
                        ChildId = 1,
                        Height = 82.3m,
                        Weight = 11.5m,
                        Bmi = 17.0m,
                        HeadCircumference = 47.8m,
                        CreatedAt = DateTime.Now
                    },
                    Assessment = new GrowthAssessmentsDTO
                    {
                        HeightStatus = "Bình thường",
                        WeightStatus = "Bình thường",
                        BMIStatus = "Bình thường",
                        HeadCircumferenceStatus = "Bình thường"
                    },
                    IsUsingClosestAge = false,
                    StandardAgeInMonths = 18,
                    RequestedAgeInMonths = 18
                };

                _logger.LogInformation("📊 Basic test context đã tạo: Child={ChildName}, Age={Age}", 
                    testContext.Child.FullName, testContext.RequestedAgeInMonths);

                // Gọi AI
                var aiResult = await _aiService.GenerateBasicAssessmentRecommendationsAsync(testContext);

                _logger.LogInformation("✅ AI basic test thành công! Độ dài response: {Length}", aiResult?.Length ?? 0);

                return Ok(new
                {
                    success = true,
                    message = "AI basic test thành công",
                    responseLength = aiResult?.Length ?? 0,
                    response = aiResult,
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ AI basic test thất bại: {ErrorMessage}", ex.Message);
                
                return StatusCode(500, new
                {
                    success = false,
                    message = "AI basic test thất bại",
                    error = ex.Message,
                    stackTrace = ex.StackTrace,
                    timestamp = DateTime.Now
                });
            }
        }
    }
}
