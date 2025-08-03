using Contracts.DTOs.Child;
using Contracts.DTOs.Order;
using Contracts.DTOs.FacilityVaccine;

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

        // Thông tin Order (package đã được user custom)
        public int? OrderId { get; set; }
        public OrderDTO? Order { get; set; }

        // Danh sách FacilityVaccines (vaccines của cơ sở)
        public List<FacilityVaccineDTO> FacilityVaccines { get; set; } = new List<FacilityVaccineDTO>();

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
        public int RefundingCount { get; set; }     // ✅ Đang chờ Manager duyệt hoàn tiền
        public int RefundedCount { get; set; }      // ✅ Đã được duyệt hoàn tiền  
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

    /// <summary>
    /// DTO cho Manager duyệt hoàn tiền
    /// </summary>
    public class ApproveRefundDTO
    {
        public string? Note { get; set; }
    }
} 