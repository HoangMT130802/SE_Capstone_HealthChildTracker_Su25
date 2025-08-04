using Contracts.DTOs.VaccineTemplate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IVaccineTemplateService
    {
        Task<VaccineTemplateDTO> CreateVaccineTemplateAsync(CreateVaccineTemplateDTO vaccineTemplateDto);
        Task<VaccineTemplateDTO> UpdateVaccineTemplateAsync(int vaccineTemplateId, UpdateVaccineTemplateDTO vaccineTemplateDto);
        Task<VaccineTemplateDTO> GetVaccineTemplateByIdAsync(int vaccineTemplateId);
        Task<IEnumerable<VaccineTemplateDTO>> GetAllVaccineTemplatesAsync(string? diseaseName = null, int? diseaseId = null, int pageNumber = 1, int pageSize = 10);
    }
}
