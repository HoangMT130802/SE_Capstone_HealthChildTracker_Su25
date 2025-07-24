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
    }
} 