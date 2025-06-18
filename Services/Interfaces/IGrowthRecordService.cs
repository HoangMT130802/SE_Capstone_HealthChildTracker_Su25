using BusinessLogic.DTOs.GrowthRecord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Services.Interfaces
{
    public interface IGrowthRecordService
    {
        Task<IEnumerable<GrowthRecordDTO>> GetAllGrowthRecordsByChildIdAsync(int childId);
        Task<GrowthRecordDTO> GetGrowthRecordByIdAsync(int recordId);
        Task<GrowthRecordDTO> CreateGrowthRecordAsync(CreateGrowthRecordDTO recordDTO);
        Task<GrowthRecordDTO> UpdateGrowthRecordAsync(int recordId, UpdateGrowthRecordDTO recordDTO);
        Task<bool> DeleteGrowthRecordAsync(int recordId);
    }
}
