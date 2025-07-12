using System.ComponentModel.DataAnnotations;

namespace Contracts.DTOs.Appointment
{
    /// <summary>
    /// DTO cho đặt lịch nhanh với thông tin tối thiểu
    /// </summary>
    public class AppointmentQuickBookingDTO
    {
        [Required(ErrorMessage = "ID trẻ là bắt buộc")]
        public int ChildId { get; set; }

        [Required(ErrorMessage = "ID bệnh là bắt buộc")]
        public int DiseaseId { get; set; }

        /// <summary>
        /// Vị trí người dùng để tìm cơ sở gần nhất
        /// </summary>
        public UserLocationDTO? UserLocation { get; set; }

        /// <summary>
        /// Ngày ưu tiên (nếu null sẽ tìm ngày gần nhất có lịch)
        /// </summary>
        public DateOnly? PreferredDate { get; set; }

        /// <summary>
        /// Khung giờ ưu tiên
        /// </summary>
        public List<string>? PreferredTimeSlots { get; set; }

        /// <summary>
        /// Ngân sách tối đa
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Ngân sách phải >= 0")]
        public decimal? MaxBudget { get; set; }

        /// <summary>
        /// Ưu tiên gói vaccine
        /// </summary>
        public bool PreferPackages { get; set; } = false;

        /// <summary>
        /// Ghi chú
        /// </summary>
        [StringLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự")]
        public string? Note { get; set; }
    }

    /// <summary>
    /// Response cho đặt lịch nhanh
    /// </summary>
    public class AppointmentQuickBookingResponseDTO
    {
        /// <summary>
        /// Đã đặt lịch thành công hay chưa
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Thông tin cuộc hẹn (nếu đặt thành công)
        /// </summary>
        public AppointmentBookingResponseDTO? Appointment { get; set; }

        /// <summary>
        /// Danh sách gợi ý khác (nếu không đặt được lịch ưu tiên)
        /// </summary>
        public List<AppointmentSuggestionDTO> Suggestions { get; set; } = new List<AppointmentSuggestionDTO>();

        /// <summary>
        /// Lý do không đặt được lịch (nếu có)
        /// </summary>
        public string? FailureReason { get; set; }
    }

    /// <summary>
    /// DTO cho gợi ý đặt lịch khác
    /// </summary>
    public class AppointmentSuggestionDTO
    {
        public int FacilityId { get; set; }
        public string FacilityName { get; set; }
        public string FacilityAddress { get; set; }
        public double? Distance { get; set; }
        
        public DateOnly AvailableDate { get; set; }
        public string TimeSlot { get; set; }
        
        public decimal EstimatedCost { get; set; }
        public bool HasPackageOption { get; set; }
        
        /// <summary>
        /// Lý do gợi ý
        /// </summary>
        public string Reason { get; set; }
        
        /// <summary>
        /// Điểm ưu tiên (cao hơn = tốt hơn)
        /// </summary>
        public int Priority { get; set; }
    }
} 