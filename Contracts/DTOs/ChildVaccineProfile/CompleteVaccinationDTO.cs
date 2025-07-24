using System.ComponentModel.DataAnnotations;

namespace Contracts.DTOs.ChildVaccineProfile
{
    public class CompleteVaccinationDTO
    {
        [Required]
        public int AppointmentId { get; set; }
        
        [Required]
        public int VaccineId { get; set; }
        
        [Required]
        public int ChildId { get; set; }
        
        [Required]
        public DateOnly ActualDate { get; set; }
        
        public string? Note { get; set; }
        
        public string? ReactionNotes { get; set; }
        
        [Required]
        public int DoseNumber { get; set; }
    }
} 