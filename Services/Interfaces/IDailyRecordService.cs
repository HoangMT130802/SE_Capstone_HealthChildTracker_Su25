using Contracts.DTOs.DailyRecord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IDailyRecordService
    {
        Task<IEnumerable<DailyRecordDTO>> GetAllDailyRecordsByChildIdAsync(int childId);
        Task<DailyRecordDTO> GetDailyRecordByIdAsync(int recordId);
        Task<DailyRecordDTO> CreateDailyRecordAsync(CreateDailyRecordDTO recordDTO);
        Task<DailyRecordDTO> UpdateDailyRecordAsync(int recordId, UpdateDailyRecordDTO recordDTO);
        Task<bool> DeleteDailyRecordAsync(int recordId);
    }
}
