using Contracts.DTOs.Dashboard;
using Contracts.DTOs.FacilityStaff;
using Repositories.Models.QueryModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IFacilityStaffService
    {
        Task<FacilityStaffDTO> GetFacilityStaffByIdAsync(int staffId);
        Task<QueryResultModel<IEnumerable<FacilityStaffDTO>>> GetAllFacilityStaffAsync(int? facilityId = null, string position = null, int? pageIndex = null, int? pageSize = null);
        Task<StaffCountsDTO> GetStaffCountsByFacilityAsync(int facilityId);
        Task<FacilityStaffDTO> UpdateFacilityStaffAsync(int staffId, UpdateFacilityStaffDTO staffDto);
    }
}
