using AutoMapper;
using Microsoft.Extensions.Logging;
using Repositories.Entities;
using Repositories.Interfaces;
using Services.Interfaces;
using Contracts.DTOs.ChildVaccineProfile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Services.Implementations
{
    public class VaccineReminderService : IVaccineReminderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly IPushNotificationService _pushNotificationService;
        private readonly IDeviceTokenService _deviceTokenService;
        private readonly IMapper _mapper;
        private readonly ILogger<VaccineReminderService> _logger;

        public VaccineReminderService(
            IUnitOfWork unitOfWork,
            IEmailService emailService,
            IPushNotificationService pushNotificationService,
            IDeviceTokenService deviceTokenService,
            IMapper mapper,
            ILogger<VaccineReminderService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _pushNotificationService = pushNotificationService ?? throw new ArgumentNullException(nameof(pushNotificationService));
            _deviceTokenService = deviceTokenService ?? throw new ArgumentNullException(nameof(deviceTokenService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task SendDailyVaccineRemindersAsync()
        {
            try
            {
                _logger.LogInformation("Starting daily vaccine reminders process");

                var upcomingVaccines = await GetUpcomingVaccineRemindersAsync(7);
                var processedCount = 0;
                var errorCount = 0;

                foreach (var vaccine in upcomingVaccines)
                {
                    try
                    {
                        if (!vaccine.ReminderSent)
                        {
                            // Gửi email reminder
                            await _emailService.SendVaccineReminderEmailAsync(
                                vaccine.ParentEmail,
                                vaccine.ParentName,
                                vaccine.ChildName,
                                vaccine.VaccineName,
                                vaccine.DoseNum,
                                vaccine.ExpectedDate,
                                vaccine.FacilityName
                            );

                            // Gửi push notification
                            await SendPushNotificationForVaccineAsync(vaccine);

                            // Cập nhật flag reminder đã gửi (có thể thêm bảng EmailHistory sau)
                            processedCount++;
                            _logger.LogInformation("Vaccine reminder sent (email + push) for child {ChildName}, vaccine {VaccineName}", 
                                vaccine.ChildName, vaccine.VaccineName);
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        _logger.LogError(ex, "Failed to send vaccine reminder for child {ChildName}, vaccine {VaccineName}", 
                            vaccine.ChildName, vaccine.VaccineName);
                    }
                }

                _logger.LogInformation("Daily vaccine reminders completed. Processed: {ProcessedCount}, Errors: {ErrorCount}", 
                    processedCount, errorCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in daily vaccine reminders process");
                throw;
            }
        }

        public async Task SendVaccineReminderForChildAsync(int childId, int vaccineProfileId)
        {
            try
            {
                _logger.LogInformation("Sending vaccine reminder for child {ChildId}, profile {VaccineProfileId}", 
                    childId, vaccineProfileId);

                var profileRepo = _unitOfWork.GetRepository<ChildVaccineProfile>();
                var profile = await profileRepo.GetAsync(
                    p => p.VaccineProfileId == vaccineProfileId && p.ChildId == childId,
                    includeProperties: "Child,Child.Member,Child.Member.Account,Vaccine,Disease,Appointment,Appointment.Schedule,Appointment.Schedule.Facility"
                );

                if (profile == null)
                {
                    _logger.LogWarning("Vaccine profile not found: {VaccineProfileId}", vaccineProfileId);
                    return;
                }

                // Lấy thông tin parent từ Member.Account thay vì Child.Account
                var parentAccount = profile.Child?.Member?.Account;
                if (parentAccount == null)
                {
                    _logger.LogWarning("Parent account not found for profile: {VaccineProfileId}", vaccineProfileId);
                    return;
                }

                // Lấy tên facility từ appointment nếu có
                var facilityName = profile.Appointment?.Schedule?.Facility?.FacilityName;

                await _emailService.SendVaccineReminderEmailAsync(
                    parentAccount.Email,
                    profile.Child.Member.FullName, // Lấy FullName từ Member, không phải Account
                    profile.Child.FullName,
                    profile.Vaccine.Name,
                    profile.DoseNum,
                    profile.ExpectedDate ?? DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
                    facilityName
                );

                _logger.LogInformation("Vaccine reminder sent successfully for child {ChildName}", profile.Child.FullName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending vaccine reminder for child {ChildId}, profile {VaccineProfileId}", 
                    childId, vaccineProfileId);
                throw;
            }
        }

        public async Task SendDailyAppointmentRemindersAsync()
        {
            try
            {
                _logger.LogInformation("Starting daily appointment reminders process");

                var upcomingAppointments = await GetUpcomingAppointmentRemindersAsync(3);
                var processedCount = 0;
                var errorCount = 0;

                foreach (var appointment in upcomingAppointments)
                {
                    try
                    {
                        if (!appointment.ReminderSent)
                        {
                            // Gửi email reminder
                            await _emailService.SendAppointmentReminderEmailAsync(
                                appointment.ParentEmail,
                                appointment.ParentName,
                                appointment.ChildName,
                                appointment.AppointmentDate,
                                appointment.TimeSlot,
                                appointment.FacilityName,
                                appointment.FacilityAddress,
                                appointment.VaccineName
                            );

                            // Gửi push notification
                            await SendPushNotificationForAppointmentAsync(appointment);

                            processedCount++;
                            _logger.LogInformation("Appointment reminder sent (email + push) for child {ChildName} on {AppointmentDate}", 
                                appointment.ChildName, appointment.AppointmentDate);
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        _logger.LogError(ex, "Failed to send appointment reminder for appointment {AppointmentId}", 
                            appointment.AppointmentId);
                    }
                }

                _logger.LogInformation("Daily appointment reminders completed. Processed: {ProcessedCount}, Errors: {ErrorCount}", 
                    processedCount, errorCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in daily appointment reminders process");
                throw;
            }
        }

        public async Task SendAppointmentReminderAsync(int appointmentId)
        {
            try
            {
                _logger.LogInformation("Sending appointment reminder for appointment {AppointmentId}", appointmentId);

                var appointmentRepo = _unitOfWork.GetRepository<VaccinationAppointment>();
                var appointment = await appointmentRepo.GetAsync(
                    a => a.AppointmentId == appointmentId,
                    includeProperties: "Child,Child.Member,Child.Member.Account,Schedule,Schedule.Facility,Schedule.Slot"
                );

                if (appointment == null)
                {
                    _logger.LogWarning("Appointment not found: {AppointmentId}", appointmentId);
                    return;
                }

                var parentAccount = appointment.Child?.Member?.Account;
                if (parentAccount == null)
                {
                    _logger.LogWarning("Parent account not found for appointment: {AppointmentId}", appointmentId);
                    return;
                }

                // Lấy thông tin vaccine từ appointment details
                var detailRepo = _unitOfWork.GetRepository<VaccinationAppointmentDetail>();
                var details = await detailRepo.FindAsync(d => d.AppointmentId == appointmentId, includeProperties: "Vaccine");
                var vaccineName = details.FirstOrDefault()?.Vaccine?.Name ?? "Vaccine";

                var timeSlot = appointment.Schedule?.Slot != null 
                    ? $"{appointment.Schedule.Slot.StartTime:HH:mm} - {appointment.Schedule.Slot.EndTime:HH:mm}"
                    : "Cả ngày";

                await _emailService.SendAppointmentReminderEmailAsync(
                    parentAccount.Email,
                    appointment.Child.Member.FullName, // Lấy FullName từ Member
                    appointment.Child.FullName,
                    appointment.Schedule.Date,
                    timeSlot,
                    appointment.Schedule.Facility.FacilityName,
                    appointment.Schedule.Facility.Address,
                    vaccineName
                );

                _logger.LogInformation("Appointment reminder sent successfully for appointment {AppointmentId}", appointmentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending appointment reminder for appointment {AppointmentId}", appointmentId);
                throw;
            }
        }

        public async Task SendVaccinationCompletionAsync(int childId, int vaccineProfileId)
        {
            try
            {
                _logger.LogInformation("Sending vaccination completion notification for child {ChildId}, profile {VaccineProfileId}", 
                    childId, vaccineProfileId);

                var profileRepo = _unitOfWork.GetRepository<ChildVaccineProfile>();
                var profile = await profileRepo.GetAsync(
                    p => p.VaccineProfileId == vaccineProfileId && p.ChildId == childId,
                    includeProperties: "Child,Child.Member,Child.Member.Account,Vaccine,Disease"
                );

                if (profile == null)
                {
                    _logger.LogWarning("Profile not found for vaccination completion notification");
                    return;
                }

                var parentAccount = profile.Child?.Member?.Account;
                if (parentAccount == null)
                {
                    _logger.LogWarning("Parent account not found for vaccination completion notification");
                    return;
                }

                // Tìm mũi tiêm tiếp theo nếu có
                var nextProfile = await profileRepo.GetAsync(
                    p => p.ChildId == childId && 
                         p.VaccineId == profile.VaccineId && 
                         p.DiseaseId == profile.DiseaseId &&
                         p.DoseNum == profile.DoseNum + 1 &&
                         p.Status == "Scheduled"
                );

                DateOnly? nextDoseDate = nextProfile?.ExpectedDate;

                // Gửi email completion notification
                await _emailService.SendVaccinationCompletionEmailAsync(
                    parentAccount.Email,
                    profile.Child.Member.FullName, // Lấy FullName từ Member
                    profile.Child.FullName,
                    profile.Vaccine.Name,
                    profile.DoseNum,
                    profile.ActualDate ?? DateOnly.FromDateTime(DateTime.Today),
                    nextDoseDate
                );

                // Gửi push notification completion
                await SendPushNotificationForVaccinationCompletionAsync(profile, nextDoseDate);

                _logger.LogInformation("Vaccination completion notification sent (email + push) for child {ChildName}", profile.Child.FullName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending vaccination completion notification for child {ChildId}, profile {VaccineProfileId}", 
                    childId, vaccineProfileId);
                throw;
            }
        }

        public async Task<IEnumerable<VaccineReminderInfo>> GetUpcomingVaccineRemindersAsync(int daysAhead = 7)
        {
            try
            {
                var profileRepo = _unitOfWork.GetRepository<ChildVaccineProfile>();
                var fromDate = DateOnly.FromDateTime(DateTime.Today);
                var toDate = DateOnly.FromDateTime(DateTime.Today.AddDays(daysAhead));

                var profiles = await profileRepo.FindAsync(
                    p => p.ExpectedDate >= fromDate && 
                         p.ExpectedDate <= toDate &&
                         p.Status == "Scheduled" &&
                         p.ActualDate == null, // Chưa tiêm
                    includeProperties: "Child,Child.Member,Child.Member.Account,Vaccine,Disease,Appointment,Appointment.Schedule,Appointment.Schedule.Facility"
                );

                var result = new List<VaccineReminderInfo>();

                foreach (var profile in profiles)
                {
                    var parentAccount = profile.Child?.Member?.Account;
                    if (parentAccount != null && !string.IsNullOrEmpty(parentAccount.Email))
                    {
                        // Sử dụng mapper để convert entity sang DTO trước
                        var baseDto = _mapper.Map<ChildVaccineProfileDTO>(profile);
                        
                        // Tạo VaccineReminderInfo từ baseDTO
                        var reminderInfo = new VaccineReminderInfo
                        {
                            // Copy properties từ base DTO
                            VaccineProfileId = baseDto.VaccineProfileId,
                            ChildId = baseDto.ChildId,
                            DiseaseId = baseDto.DiseaseId,
                            AppointmentId = baseDto.AppointmentId,
                            FacilityId = baseDto.FacilityId,
                            VaccineId = baseDto.VaccineId,
                            DoseNum = baseDto.DoseNum,
                            ExpectedDate = baseDto.ExpectedDate,
                            ActualDate = baseDto.ActualDate,
                            Status = baseDto.Status,
                            IsRequired = baseDto.IsRequired,
                            Priority = baseDto.Priority,
                            Note = baseDto.Note,
                            CreatedAt = baseDto.CreatedAt,
                            UpdatedAt = baseDto.UpdatedAt,
                            
                            // Additional properties cho reminder
                            ChildName = profile.Child?.FullName ?? "Unknown",
                            ParentName = profile.Child?.Member?.FullName ?? "Unknown", // Lấy từ Member
                            ParentEmail = parentAccount.Email,
                            VaccineName = profile.Vaccine?.Name ?? "Unknown Vaccine",
                            FacilityName = profile.Appointment?.Schedule?.Facility?.FacilityName,
                            ReminderSent = false // TODO: Implement reminder tracking
                        };

                        result.Add(reminderInfo);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting upcoming vaccine reminders");
                throw;
            }
        }

        public async Task<IEnumerable<AppointmentReminderInfo>> GetUpcomingAppointmentRemindersAsync(int daysAhead = 3)
        {
            try
            {
                var appointmentRepo = _unitOfWork.GetRepository<VaccinationAppointment>();
                var fromDate = DateOnly.FromDateTime(DateTime.Today);
                var toDate = DateOnly.FromDateTime(DateTime.Today.AddDays(daysAhead));

                var appointments = await appointmentRepo.FindAsync(
                    a => a.Schedule.Date >= fromDate && 
                         a.Schedule.Date <= toDate &&
                         (a.Status == "Confirmed" || a.Status == "Paid"),
                    includeProperties: "Child,Child.Member,Child.Member.Account,Schedule,Schedule.Facility,Schedule.Slot"
                );

                var result = new List<AppointmentReminderInfo>();

                foreach (var appointment in appointments)
                {
                    var parentAccount = appointment.Child?.Member?.Account;
                    if (parentAccount != null && !string.IsNullOrEmpty(parentAccount.Email))
                    {
                        // Lấy thông tin vaccine từ appointment details
                        var detailRepo = _unitOfWork.GetRepository<VaccinationAppointmentDetail>();
                        var details = await detailRepo.FindAsync(d => d.AppointmentId == appointment.AppointmentId, 
                            includeProperties: "Vaccine");
                        
                        var vaccineName = details.FirstOrDefault()?.Vaccine?.Name ?? "Vaccine";
                        var timeSlot = appointment.Schedule?.Slot != null 
                            ? $"{appointment.Schedule.Slot.StartTime:HH:mm} - {appointment.Schedule.Slot.EndTime:HH:mm}"
                            : "Cả ngày";

                        result.Add(new AppointmentReminderInfo
                        {
                            AppointmentId = appointment.AppointmentId,
                            ChildId = appointment.ChildId,
                            ChildName = appointment.Child?.FullName ?? "Unknown",
                            ParentName = appointment.Child?.Member?.FullName ?? "Unknown", // Lấy từ Member
                            ParentEmail = parentAccount.Email,
                            AppointmentDate = appointment.Schedule.Date,
                            TimeSlot = timeSlot,
                            FacilityName = appointment.Schedule.Facility?.FacilityName ?? "Unknown Facility",
                            FacilityAddress = appointment.Schedule.Facility?.Address ?? "",
                            VaccineName = vaccineName,
                            ReminderSent = false // TODO: Implement reminder tracking
                        });
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting upcoming appointment reminders");
                throw;
            }
        }

        /// <summary>
        /// Gửi push notification cho vaccine reminder
        /// </summary>
        private async Task SendPushNotificationForVaccineAsync(VaccineReminderInfo vaccine)
        {
            try
            {
                // Lấy account ID từ email (có thể cần optimize bằng cách lưu trong VaccineReminderInfo)
                var accountRepo = _unitOfWork.GetRepository<Account>();
                var account = await accountRepo.GetAsync(a => a.Email == vaccine.ParentEmail);
                
                if (account == null)
                {
                    _logger.LogWarning("Account not found for email {Email}", vaccine.ParentEmail);
                    return;
                }

                // Lấy tất cả device tokens của user
                var deviceTokens = await _deviceTokenService.GetUserActiveTokensAsync(account.AccountId);
                
                if (!deviceTokens.Any())
                {
                    _logger.LogDebug("No active device tokens found for account {AccountId}", account.AccountId);
                    return;
                }

                // Gửi push notification đến tất cả devices
                foreach (var token in deviceTokens)
                {
                    try
                    {
                        await _pushNotificationService.SendVaccineReminderPushAsync(
                            token,
                            vaccine.ChildName,
                            vaccine.VaccineName,
                            vaccine.DoseNum,
                            vaccine.ExpectedDate.ToString("dd/MM/yyyy"),
                            vaccine.FacilityName,
                            account.AccountId,
                            vaccine.ChildId,
                            vaccine.VaccineId
                        );

                        // Cập nhật last used time cho token
                        await _deviceTokenService.UpdateTokenLastUsedAsync(token);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send push notification to token for account {AccountId}", account.AccountId);
                        
                        // Nếu token invalid, deactivate nó
                        if (ex.Message.Contains("invalid") || ex.Message.Contains("not-registered"))
                        {
                            await _deviceTokenService.DeactivateDeviceTokenAsync(token);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending push notification for vaccine reminder");
                // Không throw để không ảnh hưởng đến email sending
            }
        }

        /// <summary>
        /// Gửi push notification cho appointment reminder
        /// </summary>
        private async Task SendPushNotificationForAppointmentAsync(AppointmentReminderInfo appointment)
        {
            try
            {
                var accountRepo = _unitOfWork.GetRepository<Account>();
                var account = await accountRepo.GetAsync(a => a.Email == appointment.ParentEmail);
                
                if (account == null)
                {
                    _logger.LogWarning("Account not found for email {Email}", appointment.ParentEmail);
                    return;
                }

                var deviceTokens = await _deviceTokenService.GetUserActiveTokensAsync(account.AccountId);
                
                if (!deviceTokens.Any())
                {
                    _logger.LogDebug("No active device tokens found for account {AccountId}", account.AccountId);
                    return;
                }

                foreach (var token in deviceTokens)
                {
                    try
                    {
                        await _pushNotificationService.SendAppointmentReminderPushAsync(
                            token,
                            appointment.ChildName,
                            appointment.AppointmentDate.ToString("dd/MM/yyyy"),
                            appointment.TimeSlot,
                            appointment.FacilityName,
                            appointment.FacilityAddress
                        );

                        await _deviceTokenService.UpdateTokenLastUsedAsync(token);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send push notification to token for account {AccountId}", account.AccountId);
                        
                        if (ex.Message.Contains("invalid") || ex.Message.Contains("not-registered"))
                        {
                            await _deviceTokenService.DeactivateDeviceTokenAsync(token);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending push notification for appointment reminder");
            }
        }

        /// <summary>
        /// Gửi push notification cho vaccination completion
        /// </summary>
        private async Task SendPushNotificationForVaccinationCompletionAsync(ChildVaccineProfile profile, DateOnly? nextDoseDate)
        {
            try
            {
                var accountRepo = _unitOfWork.GetRepository<Account>();
                var account = await accountRepo.GetAsync(a => a.AccountId == profile.Child.Member.AccountId);
                
                if (account == null)
                {
                    _logger.LogWarning("Account not found for child {ChildId}", profile.ChildId);
                    return;
                }

                var deviceTokens = await _deviceTokenService.GetUserActiveTokensAsync(account.AccountId);
                
                if (!deviceTokens.Any())
                {
                    _logger.LogDebug("No active device tokens found for account {AccountId}", account.AccountId);
                    return;
                }

                foreach (var token in deviceTokens)
                {
                    try
                    {
                        await _pushNotificationService.SendVaccinationCompletionPushAsync(
                            token,
                            profile.Child.FullName,
                            profile.Vaccine.Name,
                            profile.DoseNum,
                            nextDoseDate?.ToString("dd/MM/yyyy")
                        );

                        await _deviceTokenService.UpdateTokenLastUsedAsync(token);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send push notification to token for account {AccountId}", account.AccountId);
                        
                        if (ex.Message.Contains("invalid") || ex.Message.Contains("not-registered"))
                        {
                            await _deviceTokenService.DeactivateDeviceTokenAsync(token);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending push notification for vaccination completion");
            }
        }
    }
}
