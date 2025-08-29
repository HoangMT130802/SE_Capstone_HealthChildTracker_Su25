namespace Contracts.DTOs.Appointment
{
    /// <summary>
    /// DTO cho danh sách vaccine có thể thay thế
    /// </summary>
    public class AvailableVaccineDTO
    {
        public int VaccineId { get; set; }
        public string VaccineName { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int AvailableQuantity { get; set; }
        public decimal Price { get; set; }
        public List<string> TreatedDiseases { get; set; } = new List<string>();
        public bool CanTreatBookedDiseases { get; set; }
        public List<string> BookedDiseasesItCanTreat { get; set; } = new List<string>();
    }

    /// <summary>
    /// Response cho danh sách vaccine thay thế
    /// </summary>
    public class AvailableVaccinesResponseDTO
    {
        public int AppointmentId { get; set; }
        public int AppointmentDetailId { get; set; }
        public string CurrentVaccineName { get; set; } = string.Empty;
        public List<string> BookedDiseases { get; set; } = new List<string>();
        public List<AvailableVaccineDTO> AvailableVaccines { get; set; } = new List<AvailableVaccineDTO>();
        public int TotalAvailable { get; set; }
    }
}
