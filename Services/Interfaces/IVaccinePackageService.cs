using Contracts.DTOs.VaccinePackage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IVaccinePackageService
    {
        Task<VaccinePackageDTO> CreateVaccinePackageAsync(CreateVaccinePackageDTO vaccinePackageDto);
        Task<VaccinePackageDTO> CreateVaccinePackageWithVaccinesAsync(CreateVaccinePackageWithVaccinesDTO vaccinePackageDto);
        Task<PackageVaccineDTO> AddVaccineToPackageAsync(int packageId, CreatePackageVaccineDTO packageVaccineDto);
        Task<VaccinePackageDTO> GetVaccinePackageByIdAsync(int packageId);
        Task<IEnumerable<VaccinePackageDTO>> GetAllVaccinePackagesAsync();
        Task<VaccinePackageDTO> UpdateVaccinePackageAsync(int packageId, UpdateVaccinePackageDTO vaccinePackageDto);
        Task<PackageVaccineDTO> UpdateVaccineInPackageAsync(int packageId, int vaccineId, UpdatePackageVaccineDTO packageVaccineDto);
        Task<bool> DeleteVaccinePackageAsync(int packageId);
        Task<bool> DeleteVaccineFromPackageAsync(int packageId, int vaccineId);
    }
}
