using Logistics.Server.Application.Common.Models;
using Logistics.Server.Application.Modules.BackgroundProcessing.DTOs;
using Logistics.Shared.Contracts.Pagination;

namespace Logistics.Server.Application.Modules.BackgroundProcessing.Services;

public interface IBackgroundJobService
{
    Task<PagedResult<BackgroundJobLogDto>> GetJobsAsync(
        string? jobName,
        string? status,
        string? triggerType,
        DateTime? fromDate,
        DateTime? toDate,
        PagedRequest pagedRequest,
        CancellationToken cancellationToken = default);

    Task<BackgroundJobLogDto?> GetJobByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<PagedResult<BackgroundJobLogDto>> GetFailedJobsAsync(PagedRequest pagedRequest, CancellationToken cancellationToken = default);

    Task<BackgroundJobLogDto> EnqueueJobAsync(EnqueueBackgroundJobRequest request, CancellationToken cancellationToken = default);

    Task<BackgroundJobLogDto> RetryJobAsync(long id, CancellationToken cancellationToken = default);

    Task<BackgroundJobLogDto> CancelJobAsync(long id, string? reason = null, CancellationToken cancellationToken = default);

    Task<int> ProcessPendingJobsAsync(string workerId, int batchSize = 20, CancellationToken cancellationToken = default);

    Task<int> RecoverStaleJobsAsync(TimeSpan leaseTimeout, CancellationToken cancellationToken = default);
}

public interface IBackgroundJobHandler
{
    string JobName { get; }
    Task<BackgroundJobExecutionResult> ExecuteAsync(string? jobReference, string? payloadJson, CancellationToken cancellationToken = default);
}

public interface IOutboxProcessorService
{
    Task<int> ProcessBatchAsync(string workerId, int batchSize = 50, CancellationToken cancellationToken = default);

    Task<int> RecoverStaleInProgressAsync(TimeSpan leaseTimeout, CancellationToken cancellationToken = default);

    Task<BackgroundProcessingHealthDto> GetHealthSummaryAsync(CancellationToken cancellationToken = default);

    Task<PagedResult<OutboxMessageDto>> GetOutboxMessagesAsync(string? status, string? eventType, PagedRequest pagedRequest, CancellationToken cancellationToken = default);

    Task<bool> RetryOutboxMessageAsync(long id, CancellationToken cancellationToken = default);
}

public interface IScheduledReportService
{
    Task<PagedResult<ReportDefinitionDto>> GetDefinitionsAsync(string? category, bool? isActive, PagedRequest pagedRequest, CancellationToken cancellationToken = default);

    Task<ReportDefinitionDto?> GetDefinitionByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<PagedResult<ScheduledReportDto>> GetScheduledReportsAsync(long? branchId, bool? isActive, PagedRequest pagedRequest, CancellationToken cancellationToken = default);

    Task<ScheduledReportDto?> GetScheduledReportByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<ScheduledReportDto> CreateAsync(CreateScheduledReportRequest request, CancellationToken cancellationToken = default);

    Task<ScheduledReportDto> UpdateAsync(long id, UpdateScheduledReportRequest request, CancellationToken cancellationToken = default);

    Task<ScheduledReportDto> SetActiveStatusAsync(long id, bool isActive, CancellationToken cancellationToken = default);

    Task<ScheduledReportRunResultDto> TriggerRunAsync(long id, CancellationToken cancellationToken = default);

    Task<int> ProcessDueReportsAsync(string workerId, CancellationToken cancellationToken = default);

    Task<PagedResult<BackgroundJobLogDto>> GetExecutionHistoryAsync(long scheduledReportId, PagedRequest pagedRequest, CancellationToken cancellationToken = default);
}

public interface IReportExecutionEngine
{
    Task<ReportGenerationResult> GenerateAsync(
        string reportCode,
        string queryDefinition,
        string outputFormat,
        long? branchId,
        CancellationToken cancellationToken = default);
}

public interface IScheduledMaintenanceService
{
    Task<int> CleanupExpiredSessionsAndTokensAsync(CancellationToken cancellationToken = default);

    Task<int> CleanupExpiredNotificationsAsync(CancellationToken cancellationToken = default);
}

