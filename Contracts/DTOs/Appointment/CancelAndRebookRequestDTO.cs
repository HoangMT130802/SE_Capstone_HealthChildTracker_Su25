using System.ComponentModel.DataAnnotations;

namespace Contracts.DTOs.Appointment
{
    public class CancelAndRebookRequestDTO
    {
        [Required]
        public int CurrentAppointmentId { get; set; }

        [Required]
        public int NewScheduleId { get; set; }

        [Required]
        public int ChildVaccineProfileId { get; set; }

        [Required]
        [MaxLength(500)]
        public string CancelReason { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }
    }
}

