using Contracts.DTOs.VaccinationFacility;
using Repositories.Models.QueryModels;

namespace Services.Interfaces
{
    public interface IVaccinationFacilityService
    {
        Task<QueryResultModel<List<VaccinationFacilityDTO>>> GetAllFacilitiesAsync(int pageIndex = 1, int pageSize = 10);
        Task<VaccinationFacilityDTO?> GetFacilityByIdAsync(int facilityId);
        Task<VaccinationFacilityDTO?> GetFacilityByManagerIdAsync(int accountId);
        Task<VaccinationFacilityDTO> CreateFacilityAsync(CreateVaccinationFacilityDTO createDto, int managerAccountId);
        Task<VaccinationFacilityDTO?> UpdateFacilityAsync(UpdateVaccinationFacilityDTO updateDto, int managerAccountId);
        Task<bool> DeleteFacilityAsync(int facilityId, int managerAccountId);
        Task<bool> CheckManagerHasFacilityAsync(int managerAccountId);
        Task<int> GetTotalCountAsync();
    }
} 