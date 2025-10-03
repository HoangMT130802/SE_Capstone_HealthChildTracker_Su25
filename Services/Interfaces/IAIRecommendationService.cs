using Contracts.DTOs.GrowthAssessment;
using Repositories.Entities;

namespace Services.Interfaces
{
    public interface IAIRecommendationService
    {
        Task<string> GenerateGrowthRecommendationsAsync(
            GrowthAssessmentContext context,
            CancellationToken cancellationToken = default);
        
        Task<string> GenerateBasicAssessmentRecommendationsAsync(
            BasicAssessmentContext context,
            CancellationToken cancellationToken = default);
    }

    public class GrowthAssessmentContext
    {
        public ChildInfo Child { get; set; }
        public List<GrowthRecord> RecentRecords { get; set; }
        public GrowthAssessmentsDTO CurrentAssessment { get; set; }
        public List<PredictionDataPointDTO> Predictions { get; set; }
        public PredictionQualityDTO Quality { get; set; }
        public double HeightTrend { get; set; }
        public double WeightTrend { get; set; }
    }

    public class BasicAssessmentContext
    {
        public ChildInfo Child { get; set; }
        public GrowthRecord CurrentRecord { get; set; }
        public GrowthAssessmentsDTO Assessment { get; set; }
        public bool IsUsingClosestAge { get; set; }
        public int? StandardAgeInMonths { get; set; }
        public int? RequestedAgeInMonths { get; set; }
    }

    public class ChildInfo
    {
        public int ChildId { get; set; }
        public string FullName { get; set; }
        public DateTime BirthDate { get; set; }
        public string Gender { get; set; }
        public int AgeInMonths { get; set; }
    }
}

