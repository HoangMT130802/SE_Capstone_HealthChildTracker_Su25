using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IPushNotificationService
    {
        /// <summary>
        /// Gửi push notification về vaccine reminder
        /// </summary>
        Task<string?> SendVaccineReminderPushAsync(string deviceToken, string childName, string vaccineName, 
            int doseNumber, string expectedDate, string facilityName = null, int? accountId = null, int? childId = null, int? vaccineId = null);

        /// <summary>
        /// Gửi push notification về appointment reminder
        /// </summary>
        Task<string?> SendAppointmentReminderPushAsync(string deviceToken, string childName, string appointmentDate,
            string appointmentTime, string facilityName, string facilityAddress = null);

        /// <summary>
        /// Gửi push notification về vaccination completion
        /// </summary>
        Task<string?> SendVaccinationCompletionPushAsync(string deviceToken, string childName, string vaccineName,
            int doseNumber, string nextVaccineDate = null);

        /// <summary>
        /// Gửi push notification tùy chỉnh
        /// </summary>
        Task<string?> SendCustomPushAsync(string deviceToken, string title, string body, 
            Dictionary<string, string> data = null);

        /// <summary>
        /// Gửi push notification cho nhiều devices
        /// </summary>
        Task<List<string>> SendMulticastPushAsync(List<string> deviceTokens, string title, string body,
            Dictionary<string, string> data = null);
    }
}
