using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Services.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Services.Implementations
{
    public class VaccineReminderBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<VaccineReminderBackgroundService> _logger;
        private readonly TimeSpan _period = TimeSpan.FromHours(24); // Chạy mỗi 24 giờ

        public VaccineReminderBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<VaccineReminderBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Vaccine Reminder Background Service started");

            // Tính toán thời gian bắt đầu (8:00 AM mỗi ngày)
            var now = DateTime.Now;
            var scheduledTime = DateTime.Today.AddHours(8);
            
            // Nếu đã qua 8:00 AM hôm nay, lên lịch cho 8:00 AM ngày mai
            if (now > scheduledTime)
            {
                scheduledTime = scheduledTime.AddDays(1);
            }

            var initialDelay = scheduledTime - now;
            _logger.LogInformation("Vaccine reminders will start at {ScheduledTime} (in {InitialDelay})", 
                scheduledTime, initialDelay);

            // Chờ đến thời gian bắt đầu
            if (initialDelay > TimeSpan.Zero)
            {
                await Task.Delay(initialDelay, stoppingToken);
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Starting daily vaccine and appointment reminders at {Time}", DateTime.Now);

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var vaccineReminderService = scope.ServiceProvider.GetRequiredService<IVaccineReminderService>();

                        // Gửi vaccine reminders
                        await vaccineReminderService.SendDailyVaccineRemindersAsync();

                        // Gửi appointment reminders
                        await vaccineReminderService.SendDailyAppointmentRemindersAsync();
                    }

                    _logger.LogInformation("Daily reminders completed successfully at {Time}", DateTime.Now);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while sending daily reminders");
                }

                // Chờ 24 giờ cho lần chạy tiếp theo
                await Task.Delay(_period, stoppingToken);
            }

            _logger.LogInformation("Vaccine Reminder Background Service stopped");
        }
    }
}
