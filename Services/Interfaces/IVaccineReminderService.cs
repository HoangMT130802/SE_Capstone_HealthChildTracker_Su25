using Contracts.DTOs.ChildVaccineProfile;
using Contracts.DTOs.Appointment;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IVaccineReminderService
    {
        /// <summary>
        /// Gửi email nhắc nhở vaccine cho tất cả trẻ em có vaccine sắp đến hạn
        /// </summary>
        Task SendDailyVaccineRemindersAsync();
        
        /// <summary>
        /// Gửi email nhắc nhở vaccine cho một trẻ em cụ thể
        /// </summary>
        Task SendVaccineReminderForChildAsync(int childId, int vaccineProfileId);
        
        /// <summary>
        /// Gửi email nhắc nhở appointment cho tất cả appointment sắp tới
        /// </summary>
        Task SendDailyAppointmentRemindersAsync();
        
        /// <summary>
        /// Gửi email nhắc nhở appointment cho một appointment cụ thể
        /// </summary>
        Task SendAppointmentReminderAsync(int appointmentId);
        
        /// <summary>
        /// Gửi email thông báo hoàn thành tiêm vaccine
        /// </summary>
        Task SendVaccinationCompletionAsync(int childId, int vaccineProfileId);
        
        /// <summary>
        /// Lấy danh sách vaccine profiles cần nhắc nhở (1-7 ngày tới)
        /// </summary>
        Task<IEnumerable<VaccineReminderInfo>> GetUpcomingVaccineRemindersAsync(int daysAhead = 7);
        
        /// <summary>
        /// Lấy danh sách appointments cần nhắc nhở (1-3 ngày tới)
        /// </summary>
        Task<IEnumerable<AppointmentReminderInfo>> GetUpcomingAppointmentRemindersAsync(int daysAhead = 3);
    }
    
    /// <summary>
    /// Thông tin vaccine reminder - mở rộng từ ChildVaccineProfileDTO
    /// </summary>
    public class VaccineReminderInfo : ChildVaccineProfileDTO
    {
        public string ChildName { get; set; }
        public string ParentName { get; set; }
        public string ParentEmail { get; set; }
        public string VaccineName { get; set; }
        public string FacilityName { get; set; }
        public bool ReminderSent { get; set; }
    }
    
    /// <summary>
    /// Thông tin appointment reminder - mở rộng từ FacilityAppointmentDTO
    /// </summary>
    public class AppointmentReminderInfo
    {
        public int AppointmentId { get; set; }
        public int ChildId { get; set; }
        public string ChildName { get; set; }
        public string ParentName { get; set; }
        public string ParentEmail { get; set; }
        public DateOnly AppointmentDate { get; set; }
        public string TimeSlot { get; set; }
        public string FacilityName { get; set; }
        public string FacilityAddress { get; set; }
        public string VaccineName { get; set; }
        public bool ReminderSent { get; set; }
    }
}
