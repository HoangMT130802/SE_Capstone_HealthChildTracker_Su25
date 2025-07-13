namespace Contracts.DTOs.Appointment
{
    /// <summary>
    /// DTO cho kết quả validation trước khi đặt lịch
    /// </summary>
    public class AppointmentValidationDTO
    {
        /// <summary>
        /// Có thể đặt lịch không
        /// </summary>
        public bool CanBook { get; set; }

        /// <summary>
        /// Danh sách lỗi validation
        /// </summary>
        public List<ValidationErrorDTO> Errors { get; set; } = new List<ValidationErrorDTO>();

        /// <summary>
        /// Danh sách cảnh báo
        /// </summary>
        public List<ValidationWarningDTO> Warnings { get; set; } = new List<ValidationWarningDTO>();

        /// <summary>
        /// Thông tin chi phí dự kiến
        /// </summary>
        public CostBreakdownDTO? CostBreakdown { get; set; }

        /// <summary>
        /// Thông tin lịch sử tiêm của trẻ
        /// </summary>
        public ChildVaccinationHistoryDTO? VaccinationHistory { get; set; }
    }

    /// <summary>
    /// DTO cho lỗi validation
    /// </summary>
    public class ValidationErrorDTO
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public string Field { get; set; }
        public ValidationSeverity Severity { get; set; }
    }

    /// <summary>
    /// DTO cho cảnh báo validation
    /// </summary>
    public class ValidationWarningDTO
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public string Field { get; set; }
        public bool CanProceed { get; set; }
    }

    /// <summary>
    /// DTO cho chi tiết chi phí
    /// </summary>
    public class CostBreakdownDTO
    {
        /// <summary>
        /// Chi phí vaccine/gói vaccine
        /// </summary>
        public decimal VaccineCost { get; set; }

        /// <summary>
        /// Phí dịch vụ
        /// </summary>
        public decimal ServiceFee { get; set; }

        /// <summary>
        /// Phí đặt lịch
        /// </summary>
        public decimal BookingFee { get; set; }

        /// <summary>
        /// Thuế
        /// </summary>
        public decimal Tax { get; set; }

        /// <summary>
        /// Giảm giá (nếu có)
        /// </summary>
        public decimal Discount { get; set; }

        /// <summary>
        /// Tổng chi phí
        /// </summary>
        public decimal TotalCost { get; set; }

        /// <summary>
        /// Chi tiết các item
        /// </summary>
        public List<CostItemDTO> Items { get; set; } = new List<CostItemDTO>();
    }

    /// <summary>
    /// DTO cho item chi phí
    /// </summary>
    public class CostItemDTO
    {
        public string Name { get; set; }
        public string Type { get; set; } // "Vaccine", "Package", "Service", "Fee"
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    /// <summary>
    /// DTO cho lịch sử tiêm của trẻ
    /// </summary>
    public class ChildVaccinationHistoryDTO
    {
        public int ChildId { get; set; }
        public string ChildName { get; set; }
        
        /// <summary>
        /// Ngày tiêm gần nhất
        /// </summary>
        public DateTime? LastVaccinationDate { get; set; }

        /// <summary>
        /// Vaccine đã tiêm liên quan đến bệnh này
        /// </summary>
        public List<string> RelatedVaccinesReceived { get; set; } = new List<string>();

        /// <summary>
        /// Có dị ứng vaccine không
        /// </summary>
        public bool HasVaccineAllergies { get; set; }

        /// <summary>
        /// Danh sách dị ứng
        /// </summary>
        public List<string> Allergies { get; set; } = new List<string>();

        /// <summary>
        /// Cần kiểm tra bác sĩ trước khi tiêm
        /// </summary>
        public bool RequiresDoctorConsultation { get; set; }
    }

    /// <summary>
    /// Mức độ nghiêm trọng của validation
    /// </summary>
    public enum ValidationSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }
} 