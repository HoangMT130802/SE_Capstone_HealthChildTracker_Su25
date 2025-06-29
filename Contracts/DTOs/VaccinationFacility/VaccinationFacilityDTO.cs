using System.ComponentModel.DataAnnotations;

namespace Contracts.DTOs.VaccinationFacility
{
    public class VaccinationFacilityDTO
    {
        public int FacilityId { get; set; }
        public string FacilityName { get; set; }
        public int LicenseNumber { get; set; }
        public string Address { get; set; }
        public int Phone { get; set; }
        public string Email { get; set; }
        public string Description { get; set; }
        public long Status { get; set; }
        public long CreatedAt { get; set; }
        public long UpdatedAt { get; set; }
    }
} 