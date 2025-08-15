using Contracts.DTOs.FacilityStaff;
using Contracts.DTOs.VaccinationFacility;

namespace Contracts.DTOs.Authentication
{
    public class ManagerWithFacilityResponseDTO
    {
        public StaffResponseDTO Manager { get; set; }
        public VaccinationFacilityDTO Facility { get; set; }
    }
}

