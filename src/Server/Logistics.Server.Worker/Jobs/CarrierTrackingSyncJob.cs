using Logistics.Server.Application.Modules.Carrier.Services;
using Logistics.Server.Domain.Entities.Integration;
using Logistics.Server.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;

namespace Logistics.Server.Worker.Jobs;

/// <summary>
/// Background job that periodically synchronizes in-transit package tracking with external carriers
/// and records execution telemetry in BackgroundJobLogs.
/// </summary>
public sealed class CarrierTrackingSyncJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CarrierTrackingSyncJob> _logger;

    public CarrierTrackingSyncJob(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<CarrierTrackingSyncJob> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = Math.Clamp(_configuration.GetValue("Worker:CarrierSyncMinutes", 5), 1, 60);
        _logger.LogInformation("CarrierTrackingSyncJob started (Interval: {Minutes}m).", intervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var orchestrator = scope.ServiceProvider.GetService<ICarrierTrackingSyncOrchestrator>();
                var db = scope.ServiceProvider.GetService<ApplicationDbContext>();

                if (orchestrator != null)
                {
                    BackgroundJobLog? jobLog = null;
                    if (db != null)
                    {
                        jobLog = new BackgroundJobLog
                        {
                            JobName = "CarrierTrackingSync",
                            ExecutionId = Guid.NewGuid().ToString("N"),
                            TriggerType = "Scheduled",
                            StartedAt = DateTime.UtcNow,
                            Status = "Running",
                            AttemptCount = 1,
                            MaxAttempts = 1
                        };
                        db.BackgroundJobLogs.Add(jobLog);
                        await db.SaveChangesAsync(stoppingToken);
                    }

                    try
                    {
                        await orchestrator.SyncAllActiveCarriersAsync(stoppingToken);
                        if (jobLog != null && db != null)
                        {
                            jobLog.Status = "Success";
                            jobLog.CompletedAt = DateTime.UtcNow;
                            jobLog.LogDetails = "Carrier tracking sync cycle completed.";
                            await db.SaveChangesAsync(stoppingToken);
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        if (jobLog != null && db != null)
                        {
                            jobLog.Status = "Failed";
                            jobLog.CompletedAt = DateTime.UtcNow;
                            jobLog.ExceptionDetails = ex.Message.Length > 1000 ? ex.Message[..1000] : ex.Message;
                            await db.SaveChangesAsync(CancellationToken.None);
                        }
                        throw;
                    }
                }

                await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
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

        _logger.LogInformation("CarrierTrackingSyncJob stopped cleanly.");
    }
}
