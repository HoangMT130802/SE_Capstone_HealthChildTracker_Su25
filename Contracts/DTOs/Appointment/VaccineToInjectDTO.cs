namespace Contracts.DTOs.Appointment
{
    /// <summary>
    /// DTO cho vaccine cần tiêm trong appointment - hiển thị cho staff/bác sĩ
    /// </summary>
    public class VaccineToInjectDTO
    {
        public int VaccineId { get; set; }
        public string VaccineName { get; set; }
        public string DiseaseName { get; set; }
        public string DoseNumber { get; set; }
        public string? Notes { get; set; }
        
        // Thông tin bổ sung
        public string? Manufacturer { get; set; }
        public string? SideEffects { get; set; }
        public string? Contraindications { get; set; }
    }
}
