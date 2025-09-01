using Contracts.DTOs.Disease;
using Contracts.DTOs.Vaccine;
using Contracts.DTOs.VaccinationFacility;

namespace Contracts.DTOs.ChildVaccineProfile
{
    public class VaccinationCompletionResponseDTO
    {
        /// <summary>
        /// Profile hiện tại đã được đánh dấu completed
        /// </summary>
        public ChildVaccineProfileDTO CompletedDose { get; set; }
        
        /// <summary>
        /// Mũi tiếp theo của CÙNG vaccine hiện tại (nếu chưa đủ liều)
        /// </summary>
        public ChildVaccineProfileDTO? NextDoseOfCurrentVaccine { get; set; }
        
        /// <summary>
        /// Có mũi tiếp theo của vaccine hiện tại không?
        /// </summary>
        public bool HasNextDoseOfCurrentVaccine { get; set; }
        
        /// <summary>
        /// Vaccine hiện tại đã hoàn thành đủ liều chưa?
        /// </summary>
        public bool IsCurrentVaccineCourseCompleted { get; set; }
        
        /// <summary>
        /// Tổng số mũi của vaccine hiện tại
        /// </summary>
        public int TotalDoses { get; set; }
        
        /// <summary>
        /// Số mũi đã hoàn thành của vaccine hiện tại
        /// </summary>
        public int CompletedDoses { get; set; }
        
        /// <summary>
        /// Ngày dự kiến cho mũi tiếp theo của vaccine hiện tại
        /// </summary>
        public DateOnly? NextExpectedDateOfCurrentVaccine { get; set; }
        
        /// <summary>
        /// Profile mới được tạo cho VACCINE MỚI (nếu có nextVaccineId)
        /// </summary>
        public ChildVaccineProfileDTO? NewVaccineProfile { get; set; }
        
        /// <summary>
        /// Thông tin vaccine mới được tạo profile (nếu có)
        /// </summary>
        public VaccineDTO? NewVaccine { get; set; }
        
        /// <summary>
        /// Thông tin bệnh mới được tạo profile (nếu có)
        /// </summary>
        public DiseaseDTO? NewDisease { get; set; }
        
        /// <summary>
        /// Thông tin cơ sở tiêm chủng cho vaccine mới (nếu có)
        /// </summary>
        public VaccinationFacilityDTO? NewFacility { get; set; }
        
        /// <summary>
        /// Message tổng hợp
        /// </summary>
        public string Message { get; set; }
        
        /// <summary>
        /// Danh sách các vaccine còn lại trong order (để UI hiển thị cho lựa chọn)
        /// </summary>
        public List<VaccineDTO>? RemainingVaccinesInOrder { get; set; }
    }
} 