using Contracts.DTOs.Vaccine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IVaccineService
    {
        Task<VaccineDTO> CreateVaccineAsync(CreateVaccineDTO vaccineDto);
        Task<VaccineDTO> GetVaccineByIdAsync(int vaccineId);
        Task<IEnumerable<VaccineDTO>> GetAllVaccinesAsync();
        Task<VaccineDTO> UpdateVaccineAsync(int vaccineId, UpdateVaccineDTO vaccineDto);
        Task<bool> DeleteVaccineAsync(int vaccineId);
    }
}
