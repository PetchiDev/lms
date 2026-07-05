using CareTrack.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CareTrack.Infrastructure.Jobs;

public class SisRosterSyncJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<SisRosterSyncJob> _logger;

    public SisRosterSyncJob(IServiceProvider services, ILogger<SisRosterSyncJob> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var integration = scope.ServiceProvider.GetRequiredService<IIntegrationService>();
                await integration.RunSisRosterSyncAsync(cancellationToken: stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SIS roster sync job failed");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
