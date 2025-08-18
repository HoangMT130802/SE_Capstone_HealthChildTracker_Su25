namespace Contracts.DTOs.VaccinationFacilityPaymentAccount
{
    public class VaccinationFacilityPaymentAccountDto
    {
        public int Id { get; set; }
        public int FacilityId { get; set; }
        public string BankName { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string ChecksumKey { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateOnly CreatedAt { get; set; }
        public DateOnly UpdatedAt { get; set; }
    }
}
