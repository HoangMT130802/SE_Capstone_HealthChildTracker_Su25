using Contracts.DTOs.Child;

namespace Contracts.DTOs.Appointment
{
    /// <summary>
    /// DTO cho facility staff xem lịch đặt của cơ sở
    /// </summary>
    public class FacilityAppointmentDTO
    {
        public int AppointmentId { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? Note { get; set; }

        // Thông tin người đặt (Member)
        public int MemberId { get; set; }
        public string MemberName { get; set; }
        public string MemberPhone { get; set; }
        public string MemberEmail { get; set; }

        // Thông tin trẻ
        public ChildDTO Child { get; set; }

        // Thông tin gói vaccine (nếu có)
        public string? PackageName { get; set; }

        // Danh sách vaccines được chọn (nếu không chọn gói)
        public List<string> VaccineNames { get; set; } = new List<string>();

        // Thông tin lịch hẹn
        public DateOnly AppointmentDate { get; set; }
        public string AppointmentTime { get; set; }
        public string SlotTime { get; set; }

        // Chi phí
        public decimal EstimatedCost { get; set; }

        // Trạng thái
        public bool IsUpcoming { get; set; }
        public bool IsPast { get; set; }
        public bool CanApprove { get; set; }
        public bool CanReject { get; set; }
        public bool CanComplete { get; set; }
    }

    /// <summary>
    /// Response DTO cho danh sách lịch đặt của facility
    /// </summary>
    public class FacilityAppointmentResponseDTO
    {
        public List<FacilityAppointmentDTO> Appointments { get; set; } = new List<FacilityAppointmentDTO>();
        
        // Thống kê
        public int PendingCount { get; set; }
        public int ConfirmedCount { get; set; }
        public int CompletedCount { get; set; }
        public int CancelledCount { get; set; }
        public int TodayCount { get; set; }
    }

    /// <summary>
    /// DTO cho việc cập nhật trạng thái appointment
    /// </summary>
    public class UpdateAppointmentStatusDTO
    {
        public string Status { get; set; }
        public string? Note { get; set; }
    }
} 