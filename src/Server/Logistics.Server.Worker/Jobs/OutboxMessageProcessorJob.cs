using Logistics.Server.Application.Modules.BackgroundProcessing.Services;
using Microsoft.Extensions.Configuration;

namespace Logistics.Server.Worker.Jobs;

/// <summary>
/// Background hosted service claiming and dispatching transactional Outbox messages
/// with multi-instance safe leasing, stale recovery, and graceful shutdown.
/// </summary>
public sealed class OutboxMessageProcessorJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OutboxMessageProcessorJob> _logger;
    private readonly string _workerId = $"{Environment.MachineName}-outbox-{Guid.NewGuid().ToString("N")[..8]}";

    public OutboxMessageProcessorJob(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<OutboxMessageProcessorJob> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pollingSeconds = Math.Clamp(_configuration.GetValue("Worker:OutboxPollingSeconds", 10), 2, 300);
        var batchSize = Math.Clamp(_configuration.GetValue("Worker:OutboxBatchSize", 50), 1, 200);
        var leaseMinutes = Math.Clamp(_configuration.GetValue("Worker:OutboxLeaseMinutes", 5), 1, 60);
        var cycle = 0;

        _logger.LogInformation("OutboxMessageProcessorJob started (WorkerId: {WorkerId}, Poll: {Poll}s).", _workerId, pollingSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<IOutboxProcessorService>();

                if (cycle % 6 == 0)
                {
                    await processor.RecoverStaleInProgressAsync(TimeSpan.FromMinutes(leaseMinutes), stoppingToken);
                }

                var processed = await processor.ProcessBatchAsync(_workerId, batchSize, stoppingToken);
                if (processed > 0)
                {
                    _logger.LogInformation("OutboxMessageProcessorJob ({WorkerId}) processed {Count} messages.", _workerId, processed);
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
                _logger.LogError(ex, "Error occurred during Outbox processing cycle ({WorkerId}).", _workerId);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("OutboxMessageProcessorJob stopped cleanly ({WorkerId}).", _workerId);
    }
}
