using Logistics.Server.Application.Modules.Notification.Services;
using Microsoft.Extensions.Configuration;

namespace Logistics.Server.Worker.Jobs;

/// <summary>
/// Background hosted service dispatching pending Email notifications with bounded exponential backoff retries.
/// </summary>
public sealed class NotificationDeliveryWorkerJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<NotificationDeliveryWorkerJob> _logger;

    public NotificationDeliveryWorkerJob(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<NotificationDeliveryWorkerJob> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pollingSeconds = Math.Clamp(_configuration.GetValue("Worker:NotificationPollingSeconds", 15), 5, 300);
        var batchSize = Math.Clamp(_configuration.GetValue("Worker:NotificationBatchSize", 50), 1, 200);

        _logger.LogInformation("NotificationDeliveryWorkerJob started (Poll: {Poll}s).", pollingSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var deliveryService = scope.ServiceProvider.GetRequiredService<INotificationDeliveryService>();
                var delivered = await deliveryService.ProcessPendingDeliveriesAsync(batchSize, stoppingToken);

                if (delivered > 0)
                {
                    _logger.LogInformation("NotificationDeliveryWorkerJob processed {Count} outbound notifications.", delivered);
                }

                await Task.Delay(TimeSpan.FromSeconds(pollingSeconds), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in NotificationDeliveryWorkerJob.");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        _logger.LogInformation("NotificationDeliveryWorkerJob stopped cleanly.");
    }
}
