using Contracts.DTOs.Disease;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IDiseaseService
    {
        Task<DiseaseDTO> CreateDiseaseAsync(CreateDiseaseDTO diseaseDto);
        Task<DiseaseDTO> GetDiseaseByIdAsync(int diseaseId);
        Task<IEnumerable<DiseaseDTO>> GetAllDiseasesAsync();
        Task<DiseaseDTO> UpdateDiseaseAsync(int diseaseId, UpdateDiseaseDTO diseaseDto);
        Task<bool> DeleteDiseaseAsync(int diseaseId);
    }
}
