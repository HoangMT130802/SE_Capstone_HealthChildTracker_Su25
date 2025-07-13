using Contracts.DTOs.VaccinationFacility;

namespace Contracts.DTOs.Appointment
{
    /// <summary>
    /// DTO để tìm kiếm cơ sở tiêm chủng theo bệnh
    /// </summary>
    public class FacilitySearchByDiseaseDTO
    {
        public int DiseaseId { get; set; }
        public string DiseaseName { get; set; }
        public List<VaccinationFacilityWithVaccinesDTO> Facilities { get; set; } = new List<VaccinationFacilityWithVaccinesDTO>();
    }

    /// <summary>
    /// DTO cho cơ sở kèm thông tin vaccine có thể điều trị bệnh
    /// </summary>
    public class VaccinationFacilityWithVaccinesDTO : VaccinationFacilityDTO
    {
        /// <summary>
        /// Số lượng vaccine có thể điều trị bệnh này tại cơ sở
        /// </summary>
        public int AvailableVaccineCount { get; set; }

        /// <summary>
        /// Giá thấp nhất cho vaccine điều trị bệnh này
        /// </summary>
        public decimal MinPrice { get; set; }

        /// <summary>
        /// Giá cao nhất cho vaccine điều trị bệnh này
        /// </summary>
        public decimal MaxPrice { get; set; }

        /// <summary>
        /// Có gói vaccine không
        /// </summary>
        public bool HasPackages { get; set; }

        /// <summary>
        /// Khoảng cách từ vị trí người dùng (nếu có)
        /// </summary>
        public double? Distance { get; set; }
    }
} 