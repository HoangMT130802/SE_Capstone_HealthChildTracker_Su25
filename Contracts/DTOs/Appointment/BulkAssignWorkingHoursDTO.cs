using System.ComponentModel.DataAnnotations;

namespace Contracts.DTOs.Appointment
{
    /// <summary>
    /// DTO cho việc bulk assign working hours group vào một ngày cụ thể
    /// </summary>
    public class BulkAssignWorkingHoursDTO
    {
        [Required(ErrorMessage = "FacilityId là bắt buộc")]
        public int FacilityId { get; set; }

        [Required(ErrorMessage = "WorkingHoursGroupId là bắt buộc")]
        public string WorkingHoursGroupId { get; set; }

        [Required(ErrorMessage = "Date là bắt buộc")]
        public DateOnly Date { get; set; }

        [Required(ErrorMessage = "Status là bắt buộc")]
        [RegularExpression("^(Available|Unavailable|Holiday|Maintenance)$", 
            ErrorMessage = "Status phải là: Available, Unavailable, Holiday, hoặc Maintenance")]
        public string Status { get; set; } = "Available";
    }

    /// <summary>
    /// Response DTO cho việc bulk assign working hours group
    /// </summary>
    public class BulkAssignWorkingHoursResponseDTO
    {
        /// <summary>
        /// Có thành công hay không
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Thông báo kết quả
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Tổng số slots đã được assign
        /// </summary>
        public int TotalSlotsAssigned { get; set; }

        /// <summary>
        /// Số slots đã tồn tại trước đó
        /// </summary>
        public int ExistingSlotsSkipped { get; set; }

        /// <summary>
        /// Danh sách AppointmentSchedule đã được tạo
        /// </summary>
        public List<AppointmentScheduleDTO> CreatedSchedules { get; set; } = new List<AppointmentScheduleDTO>();

        /// <summary>
        /// Thông tin working hours group
        /// </summary>
        public WorkingHoursGroupInfoDTO WorkingHoursGroup { get; set; }
    }

    /// <summary>
    /// DTO cho thông tin working hours group trong response
    /// </summary>
    public class WorkingHoursGroupInfoDTO
    {
        public string WorkingHoursGroupId { get; set; }
        public string Description { get; set; }
        public int TotalSlots { get; set; }
        public string TimeRange { get; set; }
        public DateOnly AssignedDate { get; set; }
    }
} 