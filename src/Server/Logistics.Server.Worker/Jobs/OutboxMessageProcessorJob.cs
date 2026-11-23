using Logistics.Server.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Logistics.Server.Worker.Jobs;

/// <summary>
/// Background job that processes and dispatches pending transactional Outbox messages.
/// </summary>
public class OutboxMessageProcessorJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxMessageProcessorJob> _logger;

    public OutboxMessageProcessorJob(IServiceProvider serviceProvider, ILogger<OutboxMessageProcessorJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxMessageProcessorJob started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var pendingMessages = await dbContext.OutboxMessages
                    .Where(m => m.Status == "Pending")
                    .OrderBy(m => m.CreatedAt)
                    .Take(50)
                    .ToListAsync(stoppingToken);

                if (pendingMessages.Count > 0)
                {
                    _logger.LogInformation("Processing {Count} pending outbox messages.", pendingMessages.Count);
                    // Dispatch logic will be invoked as domain event handlers are wired
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during Outbox processing cycle.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("OutboxMessageProcessorJob stopped.");
    }
}
