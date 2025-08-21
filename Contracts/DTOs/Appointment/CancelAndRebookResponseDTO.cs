namespace Contracts.DTOs.Appointment
{
    public class CancelAndRebookResponseDTO
    {
        public int ChildVaccineProfileId { get; set; }
        public int CancelledAppointmentId { get; set; }
        public int NewAppointmentId { get; set; }
        public string Status { get; set; }
        public string CancelReason { get; set; }
        public DateTime CancelledAt { get; set; }
        public DateTime NewAppointmentDate { get; set; }
        public string FacilityName { get; set; }
        public string Message { get; set; }
    }
}

