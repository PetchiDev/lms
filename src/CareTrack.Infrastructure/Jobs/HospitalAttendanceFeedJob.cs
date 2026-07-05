using CareTrack.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CareTrack.Infrastructure.Jobs;

public class HospitalAttendanceFeedJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<HospitalAttendanceFeedJob> _logger;

    public HospitalAttendanceFeedJob(IServiceProvider services, ILogger<HospitalAttendanceFeedJob> logger)
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
                var integration = scope.ServiceProvider.GetRequiredService<IIntegrationService>();
                await integration.ProcessHospitalAttendanceFeedAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hospital attendance feed job failed");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
