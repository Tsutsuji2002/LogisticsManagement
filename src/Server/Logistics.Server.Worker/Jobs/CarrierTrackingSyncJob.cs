using Logistics.Server.Application.Modules.Carrier.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Logistics.Server.Worker.Jobs;

/// <summary>
/// Background job that periodically synchronizes in-transit package tracking with external carriers.
/// </summary>
public class CarrierTrackingSyncJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CarrierTrackingSyncJob> _logger;

    public CarrierTrackingSyncJob(IServiceProvider serviceProvider, ILogger<CarrierTrackingSyncJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CarrierTrackingSyncJob started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var orchestrator = scope.ServiceProvider.GetService<ICarrierTrackingSyncOrchestrator>();
                if (orchestrator != null)
                {
                    await orchestrator.SyncAllActiveCarriersAsync(stoppingToken);
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during Carrier Tracking Sync cycle.");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        _logger.LogInformation("CarrierTrackingSyncJob stopped.");
    }
}
