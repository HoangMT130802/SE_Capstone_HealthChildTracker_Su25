using System.ComponentModel.DataAnnotations;

namespace Contracts.DTOs.ChildVaccineProfile
{
    public class CompleteVaccinationDTO
    {
        [Required]
        public int AppointmentId { get; set; }
        
        [Required]
        public int FacilityVaccineId { get; set; }
        
        /// <summary>
        /// ChildId sẽ được lấy từ appointment, không cần nhập
        /// </summary>
        
        /// <summary>
        /// ActualDate tự động = DateOnly.FromDateTime(DateTime.Today), không cần nhập
        /// </summary>
        
        /// <summary>
        /// Ghi chú của buổi tiêm (sẽ lưu vào Appointment.Note, không lưu vào ChildVaccineProfile.Note)
        /// </summary>
        public string? Note { get; set; }
        
        /// <summary>
        /// Số mũi hiện tại (phải validate <= NumberOfDoses của vaccine)
        /// </summary>
        [Required]
        public int DoseNumber { get; set; }
        
        /// <summary>
        /// Ngày dự kiến cho mũi tiếp theo (chỉ dùng khi tạo nextDose)
        /// </summary>
        [Required]
        public DateOnly ExpectedDateForNextDose { get; set; }
        
        /// <summary>
        /// ID của FacilityVaccine tiếp theo (tùy chọn) - dùng để tạo profile cho vaccine tiếp theo
        /// - Nếu null: Không tạo profile tiếp theo  
        /// - Nếu = facilityVaccineId: Tạo mũi tiếp theo của cùng vaccine tại cùng cơ sở
        /// - Nếu != facilityVaccineId: Tạo mũi đầu tiên của vaccine khác (có thể khác cơ sở, phải có trong order)
        /// </summary>
        public int? NextFacilityVaccineId { get; set; }

    }
} 