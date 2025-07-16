using System.ComponentModel.DataAnnotations;

namespace Contracts.DTOs.Appointment
{
    /// <summary>
    /// DTO cho lịch sử đặt lịch của user
    /// </summary>
    public class AppointmentHistoryDTO
    {
        public int AppointmentId { get; set; }
        public int ChildId { get; set; }
        public string ChildName { get; set; }
        public int? OrderId { get; set; }
        public string Status { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Thông tin lịch hẹn
        public DateOnly AppointmentDate { get; set; }
        public string AppointmentTime { get; set; }
        public string FacilityName { get; set; }
        public string FacilityAddress { get; set; }

        // Thông tin vaccine/package
        public string? PackageName { get; set; }
        public List<string> VaccineNames { get; set; } = new List<string>();
        
        // Chi phí
        public decimal EstimatedCost { get; set; }
        
        // Trạng thái
        public bool CanCancel { get; set; }
        public bool CanReschedule { get; set; }
        public bool IsUpcoming { get; set; }
        public bool IsPast { get; set; }
        
        // Countdown
        public string TimeUntilAppointment { get; set; }
    }

    /// <summary>
    /// Response DTO cho lịch sử đặt lịch
    /// </summary>
    public class AppointmentHistoryResponseDTO
    {
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        
        public List<AppointmentHistoryDTO> Appointments { get; set; } = new List<AppointmentHistoryDTO>();
        
        // Thống kê
        public int UpcomingCount { get; set; }
        public int CompletedCount { get; set; }
        public int CancelledCount { get; set; }
    }

    /// <summary>
    /// DTO cho filters lịch sử đặt lịch
    /// </summary>
    public class AppointmentHistoryFiltersDTO
    {
        /// <summary>
        /// ID trẻ (nếu muốn filter theo trẻ cụ thể)
        /// </summary>
        public int? ChildId { get; set; }

        /// <summary>
        /// Trạng thái appointment
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Ngày từ
        /// </summary>
        public DateOnly? FromDate { get; set; }

        /// <summary>
        /// Ngày đến
        /// </summary>
        public DateOnly? ToDate { get; set; }

        /// <summary>
        /// Chỉ lấy appointments sắp tới
        /// </summary>
        public bool? UpcomingOnly { get; set; }

        /// <summary>
        /// Chỉ lấy appointments đã hoàn thành
        /// </summary>
        public bool? CompletedOnly { get; set; }

        /// <summary>
        /// Sắp xếp theo (CreatedAt, AppointmentDate)
        /// </summary>
        public string? SortBy { get; set; } = "AppointmentDate";

        /// <summary>
        /// Sắp xếp tăng dần (true) hay giảm dần (false)
        /// </summary>
        public bool? SortAscending { get; set; } = false;

        /// <summary>
        /// Số trang
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Page phải >= 1")]
        public int Page { get; set; } = 1;

        /// <summary>
        /// Kích thước trang
        /// </summary>
        [Range(1, 100, ErrorMessage = "PageSize từ 1-100")]
        public int PageSize { get; set; } = 20;
    }
} 