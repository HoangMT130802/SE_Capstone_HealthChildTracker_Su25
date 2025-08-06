using Contracts.DTOs.FacilityVaccine;
using Contracts.DTOs.VaccinePackage;

namespace Contracts.DTOs.Appointment
{
    /// <summary>
    /// DTO cho danh sách vaccine và gói vaccine của cơ sở theo bệnh
    /// </summary>
    public class FacilityVaccinesByDiseaseDTO
    {
        public int FacilityId { get; set; }
        public string FacilityName { get; set; }
        public int DiseaseId { get; set; }
        public string DiseaseName { get; set; }

        /// <summary>
        /// Danh sách vaccine lẻ có thể điều trị bệnh
        /// </summary>
        public List<FacilityVaccineForBookingDTO> IndividualVaccines { get; set; } = new List<FacilityVaccineForBookingDTO>();

        /// <summary>
        /// Danh sách gói vaccine có chứa vaccine điều trị bệnh
        /// </summary>
        public List<VaccinePackageForBookingDTO> VaccinePackages { get; set; } = new List<VaccinePackageForBookingDTO>();
    }

    /// <summary>
    /// DTO cho vaccine tại cơ sở với thông tin booking
    /// </summary>
    public class FacilityVaccineForBookingDTO : FacilityVaccineDTO
    {
        /// <summary>
        /// Có thể đặt lịch không (còn hàng, chưa hết hạn)
        /// </summary>
        public bool IsBookable => AvailableQuantity > 0 && ExpiryDate > DateOnly.FromDateTime(DateTime.Now) && Status == "active";

        /// <summary>
        /// Số ngày còn lại trước khi hết hạn
        /// </summary>
        public int DaysUntilExpiry => ExpiryDate.DayNumber - DateOnly.FromDateTime(DateTime.Now).DayNumber;
    }

    /// <summary>
    /// DTO cho gói vaccine với thông tin booking
    /// </summary>
    public class VaccinePackageForBookingDTO : VaccinePackageDTO
    {
        /// <summary>
        /// Có thể đặt lịch không
        /// </summary>
        public bool IsBookable => Status == "active" && PackageVaccines.All(pv => pv.FacilityVaccine.AvailableQuantity >= pv.Quantity);

        /// <summary>
        /// Số lượng vaccine trong gói có thể điều trị bệnh này
        /// </summary>
        public int RelevantVaccineCount { get; set; }

        /// <summary>
        /// Tổng số vaccine trong gói
        /// </summary>
        public int TotalVaccineCount => PackageVaccines?.Sum(pv => pv.Quantity) ?? 0;
    }
} 