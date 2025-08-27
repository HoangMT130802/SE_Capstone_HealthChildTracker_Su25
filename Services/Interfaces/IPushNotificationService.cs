using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IPushNotificationService
    {
        /// <summary>
        /// Gửi push notification về vaccine reminder
        /// </summary>
        Task SendVaccineReminderPushAsync(string deviceToken, string childName, string vaccineName, 
            int doseNumber, string expectedDate, string facilityName = null);

        /// <summary>
        /// Gửi push notification về appointment reminder
        /// </summary>
        Task SendAppointmentReminderPushAsync(string deviceToken, string childName, string appointmentDate,
            string appointmentTime, string facilityName, string facilityAddress = null);

        /// <summary>
        /// Gửi push notification về vaccination completion
        /// </summary>
        Task SendVaccinationCompletionPushAsync(string deviceToken, string childName, string vaccineName,
            int doseNumber, string nextVaccineDate = null);

        /// <summary>
        /// Gửi push notification tùy chỉnh
        /// </summary>
        Task SendCustomPushAsync(string deviceToken, string title, string body, 
            Dictionary<string, string> data = null);

        /// <summary>
        /// Gửi push notification cho nhiều devices
        /// </summary>
        Task SendMulticastPushAsync(List<string> deviceTokens, string title, string body,
            Dictionary<string, string> data = null);
    }
}
