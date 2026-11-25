using Logistics.Server.Application.Modules.BackgroundProcessing.Services;
using Microsoft.Extensions.Configuration;

namespace Logistics.Server.Worker.Jobs;

/// <summary>
/// Hosted daemon coordinating queued background job execution, stale job recovery,
/// due scheduled report execution, and periodic expired session/notification maintenance.
/// </summary>
public sealed class BackgroundJobQueueWorkerJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BackgroundJobQueueWorkerJob> _logger;
    private readonly string _workerId = $"{Environment.MachineName}-jobs-{Guid.NewGuid().ToString("N")[..8]}";
    private DateTime _lastMaintenanceUtc = DateTime.MinValue;

    public BackgroundJobQueueWorkerJob(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<BackgroundJobQueueWorkerJob> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pollingSeconds = Math.Clamp(_configuration.GetValue("Worker:JobPollingSeconds", 15), 5, 300);
        var leaseMinutes = Math.Clamp(_configuration.GetValue("Worker:JobLeaseMinutes", 10), 2, 120);
        var maintenanceHours = Math.Clamp(_configuration.GetValue("Worker:MaintenanceIntervalHours", 6), 1, 24);
        var cycle = 0;

        _logger.LogInformation("BackgroundJobQueueWorkerJob started (WorkerId: {WorkerId}, Poll: {Poll}s).", _workerId, pollingSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var jobService = scope.ServiceProvider.GetRequiredService<IBackgroundJobService>();
                var reportService = scope.ServiceProvider.GetRequiredService<IScheduledReportService>();

                if (cycle % 4 == 0)
                {
                    await jobService.RecoverStaleJobsAsync(TimeSpan.FromMinutes(leaseMinutes), stoppingToken);
                }

                await jobService.ProcessPendingJobsAsync(_workerId, batchSize: 20, stoppingToken);

                // Check due scheduled reports every ~1 minute (every 4 cycles of 15s)
                if (cycle % 4 == 0)
                {
                    await reportService.ProcessDueReportsAsync(_workerId, stoppingToken);
                }

                // Periodic cleanup of expired sessions, refresh tokens, and expired notifications
                if (DateTime.UtcNow - _lastMaintenanceUtc >= TimeSpan.FromHours(maintenanceHours))
                {
                    var maintenance = scope.ServiceProvider.GetRequiredService<IScheduledMaintenanceService>();
                    await maintenance.CleanupExpiredSessionsAndTokensAsync(stoppingToken);
                    await maintenance.CleanupExpiredNotificationsAsync(stoppingToken);
                    _lastMaintenanceUtc = DateTime.UtcNow;
                }

                cycle++;
                await Task.Delay(TimeSpan.FromSeconds(pollingSeconds), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in BackgroundJobQueueWorkerJob ({WorkerId}).", _workerId);
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        _logger.LogInformation("BackgroundJobQueueWorkerJob stopped cleanly ({WorkerId}).", _workerId);
    }
}
