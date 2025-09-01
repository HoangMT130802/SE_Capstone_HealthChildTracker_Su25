using Contracts.DTOs.Disease;
using Contracts.DTOs.Vaccine;

namespace Contracts.DTOs.ChildVaccineProfile
{
    public class VaccinationCompletionResponseDTO
    {
        public ChildVaccineProfileDTO CompletedDose { get; set; }
        public ChildVaccineProfileDTO? NextDose { get; set; }
        public bool HasNextDose { get; set; }
        public bool IsVaccineCourseCompleted { get; set; }
        public int TotalDoses { get; set; }
        public int CompletedDoses { get; set; }
        public DateOnly? NextExpectedDate { get; set; }
        public string Message { get; set; }
        
        /// <summary>
        /// Thông tin vaccine kế tiếp được tạo profile (nếu có)
        /// </summary>
        public VaccineDTO? NextVaccine { get; set; }
        
        /// <summary>
        /// Thông tin bệnh kế tiếp được tạo profile (nếu có)
        /// </summary>
        public DiseaseDTO? NextDisease { get; set; }
        
        /// <summary>
        /// Danh sách các vaccine còn lại trong order (để UI hiển thị cho lựa chọn)
        /// </summary>
        public List<VaccineDTO>? RemainingVaccinesInOrder { get; set; }
    }
} 