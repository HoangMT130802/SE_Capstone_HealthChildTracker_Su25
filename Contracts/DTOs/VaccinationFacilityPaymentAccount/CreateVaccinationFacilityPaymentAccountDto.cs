using System.ComponentModel.DataAnnotations;

namespace Contracts.DTOs.VaccinationFacilityPaymentAccount
{
    public class CreateVaccinationFacilityPaymentAccountDto
    {
        [Required(ErrorMessage = "Facility ID is required")]
        public int FacilityId { get; set; }

        [Required(ErrorMessage = "Bank name is required")]
        [StringLength(100, ErrorMessage = "Bank name cannot exceed 100 characters")]
        public string BankName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Client ID is required")]
        [StringLength(100, ErrorMessage = "Client ID cannot exceed 100 characters")]
        public string ClientId { get; set; } = string.Empty;

        [Required(ErrorMessage = "API Key is required")]
        [StringLength(200, ErrorMessage = "API Key cannot exceed 200 characters")]
        public string ApiKey { get; set; } = string.Empty;

        [Required(ErrorMessage = "Checksum Key is required")]
        [StringLength(200, ErrorMessage = "Checksum Key cannot exceed 200 characters")]
        public string ChecksumKey { get; set; } = string.Empty;

        [Required(ErrorMessage = "IsActive is required")]
        public bool IsActive { get; set; }
    }
}
