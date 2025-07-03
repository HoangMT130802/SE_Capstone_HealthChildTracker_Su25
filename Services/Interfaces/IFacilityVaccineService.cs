using Contracts.DTOs.FacilityVaccine;
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
    public interface IFacilityVaccineService
    {
        Task<FacilityVaccineDTO> CreateFacilityVaccineAsync(CreateFacilityVaccineDTO facilityVaccineDto, int accountId);
        Task<FacilityVaccineDTO> GetFacilityVaccineByIdAsync(int facilityVaccineId);
        Task<QueryResultModel<IEnumerable<FacilityVaccineDTO>>> GetAllFacilityVaccinesAsync(
            Expression<Func<FacilityVaccine, bool>>? filter = null,
            Func<IQueryable<FacilityVaccine>, IOrderedQueryable<FacilityVaccine>>? orderBy = null,
            string include = "",
            int? pageIndex = null,
            int? pageSize = null);
        Task<FacilityVaccineDTO> UpdateFacilityVaccineAsync(int facilityVaccineId, UpdateFacilityVaccineDTO facilityVaccineDto, int accountId);
        Task<bool> DeleteFacilityVaccineAsync(int facilityVaccineId, int accountId);
    }
}
