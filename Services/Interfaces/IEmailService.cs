using System.Threading.Tasks;
using Services;

namespace Services.Interfaces
{
    public interface IEmailService
    {
        Task SendVerificationEmailAsync(string email, string otpCode, string fullName);
        Task SendForgotPasswordEmailAsync(string email, string otpCode, string fullName);
        Task<string> GenerateOtpCodeAsync();
        Task<bool> SaveEmailVerificationAsync(string email, string otpCode, string type, int? accountId = null);
        Task<bool> SaveRegistrationDataAsync(string email, string otpCode, string accountName, string password, string fullName, string phone, string address);
        Task<OtpInfo> GetRegistrationDataAsync(string email, string otpCode);
        Task<bool> VerifyOtpCodeAsync(string email, string otpCode, string type);
        Task CleanupExpiredOtpAsync();
        
        // Vaccine reminder methods
        Task SendVaccineReminderEmailAsync(string email, string parentName, string childName, string vaccineName, int doseNumber, DateOnly expectedDate, string facilityName = null);
        Task SendAppointmentReminderEmailAsync(string email, string parentName, string childName, DateOnly appointmentDate, string timeSlot, string facilityName, string facilityAddress, string vaccineName);
        Task SendVaccinationCompletionEmailAsync(string email, string parentName, string childName, string vaccineName, int doseNumber, DateOnly vaccinationDate, DateOnly? nextDoseDate = null);
        
        // Thank you email method
        Task SendThankYouEmailAsync(string email, string memberName, int totalChildren = 0, int totalAppointments = 0, int totalVaccinations = 0);
        
        // Upcoming vaccination email method
        Task SendUpcomingVaccinationEmailAsync(string email, string memberName, List<Contracts.DTOs.Email.UpcomingVaccinationItemDTO> upcomingVaccinations);
    }
}
