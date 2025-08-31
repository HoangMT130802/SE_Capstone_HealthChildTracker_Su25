using Contracts.DTOs.FacilityVaccine;

namespace Contracts.DTOs.Appointment
{
    /// <summary>
    /// Response DTO cho việc thay đổi vaccine
    /// </summary>
    public class UpdateVaccineResponseDTO
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Thông tin vaccine cũ đã được thay thế
        /// </summary>
        public VaccineChangeInfo? OldVaccine { get; set; }

        /// <summary>
        /// Thông tin vaccine mới
        /// </summary>
        public VaccineChangeInfo? NewVaccine { get; set; }

        /// <summary>
        /// Thông tin appointment detail đã được cập nhật
        /// </summary>
        public UpdatedAppointmentDetailInfo? UpdatedDetail { get; set; }

        /// <summary>
        /// Thông tin về các bệnh bị ảnh hưởng khi thay đổi vaccine
        /// </summary>
        public DiseaseCompatibilityInfo? DiseaseCompatibility { get; set; }
    }

    public class VaccineChangeInfo
    {
        public int VaccineId { get; set; }
        public string VaccineName { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int AvailableQuantity { get; set; }
        public decimal Price { get; set; }
        public List<string> TreatedDiseases { get; set; } = new List<string>();
    }

    public class UpdatedAppointmentDetailInfo
    {
        public int AppointmentDetailId { get; set; }
        public int AppointmentId { get; set; }
        public int OldVaccineId { get; set; }
        public int NewVaccineId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
        
        /// <summary>
        /// Nguồn vaccine mới (Order hoặc Individual)
        /// </summary>
        public string SourceType { get; set; } = string.Empty;
        
        /// <summary>
        /// ID của OrderDetail nếu sử dụng từ order
        /// </summary>
        public int? OrderDetailId { get; set; }
        
        /// <summary>
        /// Tên gói vaccine nếu sử dụng từ order
        /// </summary>
        public string? PackageName { get; set; }
        
        /// <summary>
        /// Chi phí phát sinh (0 nếu từ order đã trả tiền)
        /// </summary>
        public decimal AdditionalCost { get; set; }
    }

    public class DiseaseCompatibilityInfo
    {
        /// <summary>
        /// Số lượng bệnh vẫn được chữa bởi vaccine mới
        /// </summary>
        public int CompatibleDiseaseCount { get; set; }

        /// <summary>
        /// Số lượng bệnh không còn được chữa bởi vaccine mới
        /// </summary>
        public int IncompatibleDiseaseCount { get; set; }

        /// <summary>
        /// Danh sách tên bệnh vẫn được chữa
        /// </summary>
        public List<string> CompatibleDiseases { get; set; } = new List<string>();

        /// <summary>
        /// Danh sách tên bệnh không còn được chữa (cần đặt lịch lại)
        /// </summary>
        public List<string> IncompatibleDiseases { get; set; } = new List<string>();
    }
}
