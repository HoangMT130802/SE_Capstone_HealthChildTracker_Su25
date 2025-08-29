using Contracts.DTOs.VaccinePackage;
using Repositories.Entities;
using Repositories.Models.QueryModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
        Task<QueryResultModel<IEnumerable<VaccinePackageDTO>>> GetAllVaccinePackagesAsync(
            Expression<Func<VaccinePackage, bool>>? filter = null,
            Func<IQueryable<VaccinePackage>, IOrderedQueryable<VaccinePackage>>? orderBy = null,
            string include = "",
            int? pageIndex = null,
            int? pageSize = null);
        Task<VaccinePackageDTO> UpdateVaccinePackageAsync(int packageId, UpdateVaccinePackageDTO vaccinePackageDto, int accountId);
        Task<VaccinePackageDTO> UpdateVaccineInPackageAsync(int packageId, UpdatePackageVaccineDTO packageVaccineDto, int accountId);
        Task<bool> DeleteVaccinePackageAsync(int packageId, int accountId);
        Task<bool> DeleteVaccineFromPackageAsync(int packageId, int facilityVaccineId, int accountId);
        Task<VaccinePackageDTO> UpdateVaccinePackageWithNewVaccineAsync(int packageId, AddPackageVaccineDTO packageVaccineDto, int accountId);
        Task<int> GetCountByFacilityAsync(int facilityId);
    }
}
