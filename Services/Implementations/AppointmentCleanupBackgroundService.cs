using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Services.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Services.Implementations
{
    /// <summary>
    /// Background service để tự động dọn dẹp các appointment đã quá hạn
    /// Chạy mỗi 6 tiếng để kiểm tra và xử lý appointment expired
    /// </summary>
    public class AppointmentCleanupBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AppointmentCleanupBackgroundService> _logger;
        private readonly TimeSpan _period = TimeSpan.FromHours(6); // Chạy mỗi 6 tiếng

        public AppointmentCleanupBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<AppointmentCleanupBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Appointment Cleanup Background Service started - will run every {Period}", _period);

            // Chờ 1 phút trước khi bắt đầu để đảm bảo hệ thống đã khởi động hoàn toàn
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Starting appointment cleanup process at {Time}", DateTime.Now);

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var appointmentBookingService = scope.ServiceProvider.GetRequiredService<IAppointmentBookingService>();

                        // Dọn dẹp các appointment đã quá hạn
                        var cleanupResult = await appointmentBookingService.CleanupExpiredAppointmentsAsync();
                        
                        if (cleanupResult.TotalProcessed > 0)
                        {
                            _logger.LogInformation("Appointment cleanup completed: {ExpiredCount} expired, {CancelledCount} cancelled, {TotalProcessed} total processed", 
                                cleanupResult.ExpiredAppointmentsCount, 
                                cleanupResult.CancelledAppointmentsCount,
                                cleanupResult.TotalProcessed);
                        }
                        else
                        {
                            _logger.LogDebug("No expired appointments found to cleanup");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during appointment cleanup process");
                }

                // Chờ 6 tiếng cho lần chạy tiếp theo
                await Task.Delay(_period, stoppingToken);
            }

            _logger.LogInformation("Appointment Cleanup Background Service stopped");
        }
    }
}

