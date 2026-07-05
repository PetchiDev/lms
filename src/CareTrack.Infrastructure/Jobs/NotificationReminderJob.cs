using CareTrack.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CareTrack.Infrastructure.Jobs;

public class NotificationReminderJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<NotificationReminderJob> _logger;

    public NotificationReminderJob(IServiceProvider services, ILogger<NotificationReminderJob> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();
                await notifications.ProcessRemindersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Notification reminder job failed");
            }

            await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
        }
    }
}
