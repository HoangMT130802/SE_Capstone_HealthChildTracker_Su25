using Contracts.DTOs.ChildVaccineProfile;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IChildVaccineProfileService {
        Task<IEnumerable<ChildVaccineProfileDTO>> GetAllChildVaccineProfilesByChildIdAsync(int childId);
        Task<ChildVaccineProfileDTO> GetChildVaccineProfileByIdAsync(int profileId);
        Task<ChildVaccineProfileDTO> CreateChildVaccineProfileAsync(CreateChildVaccineProfileDTO profileDTO);
        Task<ChildVaccineProfileDTO> UpdateChildVaccineProfileAsync(int profileId, UpdateChildVaccineProfileDTO profileDTO);
        Task<bool> DeleteChildVaccineProfileAsync(int profileId); }
}
