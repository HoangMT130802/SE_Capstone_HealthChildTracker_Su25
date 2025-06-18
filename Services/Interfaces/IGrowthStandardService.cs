using BusinessLogic.DTOs.GrowthStandard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Services.Interfaces
{
    public interface IGrowthStandardService
    {
        Task<IEnumerable<GrowthStandardDTO>> GetHeightStandardsAsync(string gender, int? ageInMonths = null);
        Task<IEnumerable<GrowthStandardDTO>> GetWeightStandardsAsync(string gender, int? ageInMonths = null);
        Task<IEnumerable<GrowthStandardDTO>> GetBMIStandardsAsync(string gender, int? ageInMonths = null);
        Task<IEnumerable<GrowthStandardDTO>> GetHeadCircumferenceStandardsAsync(string gender, int? ageInMonths = null);
    }
}
