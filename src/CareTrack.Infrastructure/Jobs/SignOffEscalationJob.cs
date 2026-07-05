using CareTrack.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CareTrack.Infrastructure.Jobs;

public class SignOffEscalationJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<SignOffEscalationJob> _logger;

    public SignOffEscalationJob(IServiceProvider services, ILogger<SignOffEscalationJob> logger)
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
                var clinical = scope.ServiceProvider.GetRequiredService<IClinicalService>();
                await clinical.ProcessEscalationsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sign-off escalation job failed");
            }

            await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
        }
    }
}
