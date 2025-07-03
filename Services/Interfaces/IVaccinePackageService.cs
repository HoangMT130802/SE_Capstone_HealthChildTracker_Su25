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
        Task<VaccinePackageDTO> CreateVaccinePackageAsync(CreateVaccinePackageDTO vaccinePackageDto, int accountId);
        Task<VaccinePackageDTO> CreateVaccinePackageWithVaccinesAsync(CreateVaccinePackageWithVaccinesDTO vaccinePackageDto, int accountId);
        Task<PackageVaccineDTO> AddVaccineToPackageAsync(int packageId, CreatePackageVaccineDTO packageVaccineDto, int accountId);
        Task<VaccinePackageDTO> GetVaccinePackageByIdAsync(int packageId);
        Task<IEnumerable<VaccinePackageDTO>> GetAllVaccinePackagesAsync();
        Task<VaccinePackageDTO> UpdateVaccinePackageAsync(int packageId, UpdateVaccinePackageDTO vaccinePackageDto, int accountId);
        Task<PackageVaccineDTO> UpdateVaccineInPackageAsync(int packageId, int vaccineId, UpdatePackageVaccineDTO packageVaccineDto, int accountId);
        Task<bool> DeleteVaccinePackageAsync(int packageId, int accountId);
        Task<bool> DeleteVaccineFromPackageAsync(int packageId, int vaccineId, int accountId);
    }
}
