using System.ComponentModel.DataAnnotations;

namespace Contracts.DTOs.Email
{
    /// <summary>
    /// DTO cho thông tin lịch tiêm sắp tới của member
    /// </summary>
    public class UpcomingVaccinationEmailDTO
    {
        public int MemberId { get; set; }
        public string MemberName { get; set; } = "";
        public string Email { get; set; } = "";
        public List<UpcomingVaccinationItemDTO> UpcomingVaccinations { get; set; } = new List<UpcomingVaccinationItemDTO>();
        public DateTime SentAt { get; set; }
    }

    /// <summary>
    /// Thông tin từng lịch tiêm sắp tới
    /// </summary>
    public class UpcomingVaccinationItemDTO
    {
        public int AppointmentId { get; set; }
        public string ChildName { get; set; } = "";
        public int ChildAge { get; set; } // Tuổi tính theo tháng
        public string VaccineName { get; set; } = "";
        public int DoseNumber { get; set; }
        public DateOnly AppointmentDate { get; set; }
        public string AppointmentTime { get; set; } = "";
        public string FacilityName { get; set; } = "";
        public string FacilityAddress { get; set; } = "";
        public string Status { get; set; } = "";
        public int DaysUntilAppointment { get; set; }
    }
}
