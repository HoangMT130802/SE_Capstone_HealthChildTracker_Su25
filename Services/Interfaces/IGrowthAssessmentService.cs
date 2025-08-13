using Contracts.DTOs.GrowthAssessment;
using Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IGrowthAssessmentService
    {
        Task<GrowthAssessmentDTO> AssessGrowthAsync(GrowthRecord record);
        Task<GrowthPredictionDTO> PredictGrowthAsync(int childId, int days = 90);
    }
}
