using Contracts.DTOs.GrowthStandard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IGrowthStandardService
    {
        // ✅ Growth Standards (chỉ số tuyệt đối)
        Task<IEnumerable<GrowthStandardDTO>> GetHeightStandardsAsync(string gender, int? ageInDays = null);
        Task<IEnumerable<GrowthStandardDTO>> GetWeightStandardsAsync(string gender, int? ageInDays = null);
        Task<IEnumerable<GrowthStandardDTO>> GetBMIStandardsAsync(string gender, int? ageInDays = null);
        Task<IEnumerable<GrowthStandardDTO>> GetHeadCircumferenceStandardsAsync(string gender, int? ageInDays = null);
        
        // ✅ Growth Velocity (tốc độ tăng trưởng)
        Task<IEnumerable<GrowthVelocityDTO>> GetHeightVelocityStandardsAsync(string gender, int? ageInMonths = null);
        Task<IEnumerable<GrowthVelocityDTO>> GetWeightVelocityStandardsAsync(string gender, int? ageInMonths = null);
        Task<IEnumerable<GrowthVelocityDTO>> GetHeadCircumferenceVelocityStandardsAsync(string gender, int? ageInMonths = null);
        
        // ✅ Combined Assessment
        Task<GrowthVelocityAssessmentDTO> AssessGrowthVelocityAsync(string gender, int ageInMonths, decimal actualVelocity, string measurement);
    }
}
