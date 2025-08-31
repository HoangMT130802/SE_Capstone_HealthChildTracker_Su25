using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs.Appointment
{
    /// <summary>
    /// DTO cho kết quả cleanup appointment đã quá hạn
    /// </summary>
    public class AppointmentCleanupResultDTO
    {
        /// <summary>
        /// Số lượng appointment đã quá hạn được xử lý
        /// </summary>
        public int ExpiredAppointmentsCount { get; set; }

        /// <summary>
        /// Số lượng appointment đã bị hủy được xử lý
        /// </summary>
        public int CancelledAppointmentsCount { get; set; }

        /// <summary>
        /// Tổng số appointment được xử lý
        /// </summary>
        public int TotalProcessed { get; set; }

        /// <summary>
        /// Số lượng ChildVaccineProfile được cập nhật (xóa AppointmentId)
        /// </summary>
        public int ChildVaccineProfilesUpdated { get; set; }

        /// <summary>
        /// Thời gian thực hiện cleanup
        /// </summary>
        public DateTime ProcessedAt { get; set; }

        /// <summary>
        /// Danh sách ID của các appointment đã được xử lý
        /// </summary>
        public List<int> ProcessedAppointmentIds { get; set; } = new List<int>();

        /// <summary>
        /// Thông báo chi tiết về quá trình cleanup
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Có lỗi xảy ra trong quá trình cleanup không
        /// </summary>
        public bool HasErrors { get; set; }

        /// <summary>
        /// Danh sách lỗi nếu có
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();
    }
}

