using System.ComponentModel.DataAnnotations;

namespace Contracts.DTOs.Appointment
{
    public class AppointmentRebookingRequestDTO
    {
        [Required(ErrorMessage = "ChildVaccineProfileId là bắt buộc")]
        public int ChildVaccineProfileId { get; set; }
        
        [Required(ErrorMessage = "ScheduleId là bắt buộc")]
        public int ScheduleId { get; set; }
        
        /// <summary>
        /// ID Order đã mua (nếu có) - để sử dụng vaccine từ gói đã mua
        /// </summary>
        public int? OrderId { get; set; }
        
        /// <summary>
        /// ID OrderDetail cụ thể nếu muốn chọn vaccine khác từ order
        /// Nếu không có, sẽ tự động tìm OrderDetail phù hợp với ChildVaccineProfile hiện tại
        /// </summary>
        public int? OrderDetailId { get; set; }
        
        public string? Note { get; set; }
    }
}