using Contracts.DTOs.ChildVaccineProfile;

namespace Contracts.DTOs.Appointment
{
    /// <summary>
    /// Response DTO cho việc cancel appointment
    /// </summary>
    public class CancelAppointmentResponseDTO
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// Thông tin ChildVaccineProfile đã được khôi phục về trạng thái trước khi book
        /// </summary>
        public List<ChildVaccineProfileDTO> RestoredProfiles { get; set; } = new List<ChildVaccineProfileDTO>();
        
        /// <summary>
        /// Số lần đã cancel cùng bệnh này trong ngày
        /// </summary>
        public int TodayCancelCount { get; set; }
        
        /// <summary>
        /// Số lần cancel tối đa cho phép trong ngày
        /// </summary>
        public int MaxCancelPerDay { get; set; } = 2;
        
        /// <summary>
        /// Có thể đặt lại lịch cho bệnh này không
        /// </summary>
        public bool CanRebookToday { get; set; }
        
        /// <summary>
        /// Thông tin appointment đã bị cancel
        /// </summary>
        public CancelledAppointmentInfo? CancelledAppointment { get; set; }
    }

    public class CancelledAppointmentInfo
    {
        public int AppointmentId { get; set; }
        public int ChildId { get; set; }
        public string ChildName { get; set; } = string.Empty;
        public int DiseaseId { get; set; }
        public string DiseaseName { get; set; } = string.Empty;
        public DateTime CancelledAt { get; set; }
        public string Reason { get; set; } = string.Empty;
        public List<string> VaccineNames { get; set; } = new List<string>();
    }
}
