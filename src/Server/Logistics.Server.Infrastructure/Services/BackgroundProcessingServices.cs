using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Logistics.Server.Application.Common.Events;
using Logistics.Server.Application.Common.Exceptions;
using Logistics.Server.Application.Common.Interfaces;
using Logistics.Server.Application.Common.Models;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.BackgroundProcessing.DTOs;
using Logistics.Server.Application.Modules.BackgroundProcessing.Services;
using Logistics.Server.Application.Modules.Notification.Services;
using Logistics.Server.Domain.Entities.Integration;
using Logistics.Server.Domain.Entities.Notification;
using Logistics.Server.Infrastructure.Persistence;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Logistics.Server.Infrastructure.Services;

// ═══════════════════════════════════════════════════════════════
//  1. TRANSACTIONAL OUTBOX ENQUEUE SERVICE
// ═══════════════════════════════════════════════════════════════

public sealed class OutboxService : IOutboxService
{
    public const int MaxPayloadBytes = 65536;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public OutboxService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public void Enqueue<TEvent>(TEvent integrationEvent, string aggregateType, string aggregateId, string? correlationId = null)
        where TEvent : IIntegrationEvent
    {
        var payload = JsonSerializer.Serialize(integrationEvent, SerializerOptions);
        if (System.Text.Encoding.UTF8.GetByteCount(payload) > MaxPayloadBytes)
            throw new InvalidOperationException($"Outbox payload for event '{integrationEvent.EventType}' exceeds maximum size ({MaxPayloadBytes} bytes).");

        var corrId = correlationId
                     ?? _httpContextAccessor.HttpContext?.Request.Headers["X-Correlation-Id"].FirstOrDefault();

        _context.OutboxMessages.Add(new OutboxMessage
        {
            EventType = integrationEvent.EventType,
            AggregateType = aggregateType,
            AggregateId = aggregateId,
            PayloadJson = payload,
            Status = "Pending",
            RetryCount = 0,
            CorrelationId = corrId,
            CreatedAt = DateTime.UtcNow
        });
    }
}

// ═══════════════════════════════════════════════════════════════
//  2. OUTBOX PROCESSOR & MULTI-WORKER CLAIMING SERVICE
// ═══════════════════════════════════════════════════════════════

public sealed class OutboxProcessorService : IOutboxProcessorService
{
    public const int MaxRetryCount = 5;

    private readonly ApplicationDbContext _context;
    private readonly IEnumerable<IOutboxEventHandler> _handlers;
    private readonly IAuditService _audit;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<OutboxProcessorService> _logger;

    public OutboxProcessorService(
        ApplicationDbContext context,
        IEnumerable<IOutboxEventHandler> handlers,
        IAuditService audit,
        ICurrentUserService currentUser,
        ILogger<OutboxProcessorService> logger)
    {
        _context = context;
        _handlers = handlers;
        _audit = audit;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<int> ProcessBatchAsync(string workerId, int batchSize = 50, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var claimed = await ClaimPendingMessagesAsync(workerId, batchSize, now, cancellationToken);
        if (claimed.Count == 0) return 0;

        var handlerLookup = _handlers
            .GroupBy(h => h.EventType, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var claimedSet = new HashSet<object>(claimed, ReferenceEqualityComparer.Instance);
        int processedCount = 0;

        foreach (var msg in claimed)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                if (!handlerLookup.TryGetValue(msg.EventType, out var matchedHandlers) || matchedHandlers.Count == 0)
                {
                    _logger.LogWarning("No outbox handler registered for unknown EventType '{EventType}' (MessageId: {MessageId}). Marking as Published with warning.",
                        msg.EventType, msg.Id);

                    msg.Status = "Published";
                    msg.ProcessedAt = DateTime.UtcNow;
                    msg.LockedAt = null;
                    msg.LockedBy = null;
                    msg.ErrorMessage = $"No handler registered for EventType '{msg.EventType}'.";
                }
                else
                {
                    var eventContext = new OutboxEventContext(
                        msg.Id, msg.EventType, msg.AggregateType, msg.AggregateId,
                        msg.PayloadJson, msg.CorrelationId, msg.CreatedAt);

                    foreach (var handler in matchedHandlers)
                    {
                        await handler.HandleAsync(eventContext, cancellationToken);
                    }

                    msg.Status = "Published";
                    msg.ProcessedAt = DateTime.UtcNow;
                    msg.LockedAt = null;
                    msg.LockedBy = null;
                    msg.ErrorMessage = null;
                }

                await _context.SaveChangesAsync(cancellationToken);
                processedCount++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Release lease gracefully on shutdown
                msg.Status = "Pending";
                msg.LockedAt = null;
                msg.LockedBy = null;
                await _context.SaveChangesAsync(CancellationToken.None);
                break;
            }
            catch (Exception ex)
            {
                // Detach any dirty entities staged by the failing handler while keeping claimed batch messages tracked
                foreach (var entry in _context.ChangeTracker.Entries().ToList())
                {
                    if (!claimedSet.Contains(entry.Entity))
                        entry.State = EntityState.Detached;
                }

                msg.RetryCount += 1;
                msg.LockedAt = null;
                msg.LockedBy = null;
                msg.ErrorMessage = SanitizeError(ex.Message);

                if (msg.RetryCount >= MaxRetryCount)
                {
                    msg.Status = "Failed";
                    msg.NextRetryAt = null;
                    _logger.LogError(ex, "OutboxMessage {MessageId} ({EventType}) permanently failed after {RetryCount} attempts.",
                        msg.Id, msg.EventType, msg.RetryCount);
                }
                else
                {
                    msg.Status = "Pending";
                    msg.NextRetryAt = DateTime.UtcNow.Add(ComputeBackoffWithJitter(msg.RetryCount));
                    _logger.LogWarning(ex, "OutboxMessage {MessageId} ({EventType}) failed (attempt {Attempt}/{Max}). Scheduled retry at {NextRetryAt}.",
                        msg.Id, msg.EventType, msg.RetryCount, MaxRetryCount, msg.NextRetryAt);
                }

                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        return processedCount;
    }

    public async Task<int> RecoverStaleInProgressAsync(TimeSpan leaseTimeout, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.Subtract(leaseTimeout);

        var stale = await _context.OutboxMessages
            .Where(m => m.Status == "InProgress" && ((m.LockedAt != null && m.LockedAt < cutoff) || (m.LockedAt == null && m.CreatedAt < cutoff)))
            .ToListAsync(cancellationToken);

        if (stale.Count == 0) return 0;

        foreach (var msg in stale)
        {
            msg.Status = "Pending";
            msg.LockedAt = null;
            msg.LockedBy = null;
            msg.NextRetryAt = DateTime.UtcNow;
            msg.ErrorMessage = "Recovered after worker lease expiration.";
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogWarning("Recovered {Count} stale InProgress OutboxMessages.", stale.Count);
        return stale.Count;
    }

    public async Task<BackgroundProcessingHealthDto> GetHealthSummaryAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var pendingOutbox = await _context.OutboxMessages.CountAsync(m => m.Status == "Pending", cancellationToken);
        var inProgressOutbox = await _context.OutboxMessages.CountAsync(m => m.Status == "InProgress", cancellationToken);
        var failedOutbox = await _context.OutboxMessages.CountAsync(m => m.Status == "Failed", cancellationToken);
        var lastProcessed = await _context.OutboxMessages
            .Where(m => m.ProcessedAt != null)
            .OrderByDescending(m => m.ProcessedAt)
            .Select(m => m.ProcessedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var pendingJobs = await _context.BackgroundJobLogs.CountAsync(j => j.Status == "Pending", cancellationToken);
        var runningJobs = await _context.BackgroundJobLogs.CountAsync(j => j.Status == "Running", cancellationToken);
        var failedJobs = await _context.BackgroundJobLogs.CountAsync(j => j.Status == "Failed", cancellationToken);

        var pendingNotif = await _context.Notifications.CountAsync(n => n.Status == "Pending" || n.Status == "Queued", cancellationToken);
        var failedNotif = await _context.Notifications.CountAsync(n => n.Status == "Failed", cancellationToken);

        var dueReports = await _context.ScheduledReports.CountAsync(r => r.IsActive && r.NextRunAt <= now, cancellationToken);

        var status = failedOutbox > 0 || failedJobs > 0 ? "Degraded" : "Healthy";

        return new BackgroundProcessingHealthDto
        {
            TimestampUtc = now,
            Status = status,
            PendingOutboxCount = pendingOutbox,
            InProgressOutboxCount = inProgressOutbox,
            FailedOutboxCount = failedOutbox,
            PendingJobsCount = pendingJobs,
            RunningJobsCount = runningJobs,
            FailedJobsCount = failedJobs,
            PendingNotificationsCount = pendingNotif,
            FailedNotificationsCount = failedNotif,
            DueScheduledReportsCount = dueReports,
            LastOutboxProcessedAt = lastProcessed
        };
    }

    public async Task<PagedResult<OutboxMessageDto>> GetOutboxMessagesAsync(
        string? status, string? eventType, PagedRequest pagedRequest, CancellationToken cancellationToken = default)
    {
        var query = _context.OutboxMessages.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(m => m.Status == status);
        if (!string.IsNullOrWhiteSpace(eventType))
            query = query.Where(m => m.EventType.Contains(eventType));

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.Id)
            .Skip((pagedRequest.Page - 1) * pagedRequest.PageSize)
            .Take(pagedRequest.PageSize)
            .Select(m => new OutboxMessageDto
            {
                Id = m.Id,
                EventType = m.EventType,
                AggregateType = m.AggregateType,
                AggregateId = m.AggregateId,
                Status = m.Status,
                RetryCount = m.RetryCount,
                NextRetryAt = m.NextRetryAt,
                ProcessedAt = m.ProcessedAt,
                ErrorMessage = m.ErrorMessage,
                CreatedAt = m.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<OutboxMessageDto>(items, totalCount, pagedRequest.Page, pagedRequest.PageSize);
    }

    public async Task<bool> RetryOutboxMessageAsync(long id, CancellationToken cancellationToken = default)
    {
        var msg = await _context.OutboxMessages.FirstOrDefaultAsync(m => m.Id == id, cancellationToken)
            ?? throw new NotFoundException("OutboxMessage", id);

        if (msg.Status == "Published")
            throw new ConflictException("Outbox message has already been published.");

        msg.Status = "Pending";
        msg.NextRetryAt = DateTime.UtcNow;
        msg.LockedAt = null;
        msg.LockedBy = null;
        msg.RetryCount = 0;

        await _context.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync("RetryOutboxMessage", "integration", "OutboxMessages", msg.Id.ToString(),
            null, new { msg.EventType, Status = "Pending" }, cancellationToken: cancellationToken);

        return true;
    }

    private async Task<List<OutboxMessage>> ClaimPendingMessagesAsync(string workerId, int batchSize, DateTime utcNow, CancellationToken ct)
    {
        if (_context.Database.IsSqlServer())
        {
            // SQL Server atomic lock-free queue claim using UPDLOCK + READPAST + ROWLOCK and OUTPUT INSERTED.*
            var sql = @"
;WITH NextBatch AS (
    SELECT TOP ({0}) Id
    FROM [integration].[OutboxMessages] WITH (UPDLOCK, READPAST, ROWLOCK)
    WHERE [Status] = N'Pending'
      AND ([NextRetryAt] IS NULL OR [NextRetryAt] <= {1})
    ORDER BY [CreatedAt] ASC, [Id] ASC
)
UPDATE target
SET [Status] = N'InProgress',
    [LockedAt] = {1},
    [LockedBy] = {2}
OUTPUT INSERTED.[Id], INSERTED.[AggregateId], INSERTED.[AggregateType], INSERTED.[CorrelationId],
       INSERTED.[CreatedAt], INSERTED.[ErrorMessage], INSERTED.[EventType], INSERTED.[LockedAt],
       INSERTED.[LockedBy], INSERTED.[NextRetryAt], INSERTED.[PayloadJson], INSERTED.[ProcessedAt],
       INSERTED.[RetryCount], INSERTED.[Status]
FROM [integration].[OutboxMessages] target
INNER JOIN NextBatch nb ON target.[Id] = nb.[Id];";

            var claimedRows = await _context.OutboxMessages
                .FromSqlRaw(sql, batchSize, utcNow, workerId)
                .AsNoTracking()
                .ToListAsync(ct);

            foreach (var row in claimedRows)
            {
                var tracked = _context.ChangeTracker.Entries<OutboxMessage>()
                    .FirstOrDefault(e => e.Entity.Id == row.Id);
                if (tracked != null)
                {
                    tracked.State = EntityState.Detached;
                }
                _context.OutboxMessages.Attach(row);
            }

            return claimedRows;
        }

        // Non-SQL Server (e.g., EF Core InMemory provider in unit tests) fallback
        var candidates = await _context.OutboxMessages
            .Where(m => m.Status == "Pending" && (m.NextRetryAt == null || m.NextRetryAt <= utcNow))
            .OrderBy(m => m.CreatedAt)
            .ThenBy(m => m.Id)
            .Take(batchSize)
            .ToListAsync(ct);

        if (candidates.Count == 0) return candidates;

        foreach (var c in candidates)
        {
            c.Status = "InProgress";
            c.LockedAt = utcNow;
            c.LockedBy = workerId;
        }

        await _context.SaveChangesAsync(ct);
        return candidates;
    }

    public static TimeSpan ComputeBackoffWithJitter(int retryCount)
    {
        var baseSeconds = Math.Pow(2, Math.Clamp(retryCount, 1, 8)) * 5; // 10s, 20s, 40s, 80s...
        var jitterMs = RandomNumberGenerator.GetInt32(250, 5000);
        return TimeSpan.FromSeconds(baseSeconds) + TimeSpan.FromMilliseconds(jitterMs);
    }

    private static string SanitizeError(string raw)
    {
        var sanitized = raw.Replace("Password=", "Password=[REDACTED]", StringComparison.OrdinalIgnoreCase)
            .Replace("Bearer ", "Bearer [REDACTED] ", StringComparison.OrdinalIgnoreCase);
        return sanitized.Length > 950 ? sanitized[..950] : sanitized;
    }
}

// ═══════════════════════════════════════════════════════════════
//  3. BACKGROUND JOB SERVICE
// ═══════════════════════════════════════════════════════════════

public sealed class BackgroundJobService : IBackgroundJobService
{
    private readonly ApplicationDbContext _context;
    private readonly IEnumerable<IBackgroundJobHandler> _handlers;
    private readonly IAuditService _audit;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<BackgroundJobService> _logger;

    public BackgroundJobService(
        ApplicationDbContext context,
        IEnumerable<IBackgroundJobHandler> handlers,
        IAuditService audit,
        ICurrentUserService currentUser,
        ILogger<BackgroundJobService> logger)
    {
        _context = context;
        _handlers = handlers;
        _audit = audit;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<PagedResult<BackgroundJobLogDto>> GetJobsAsync(
        string? jobName, string? status, string? triggerType, DateTime? fromDate, DateTime? toDate,
        PagedRequest pagedRequest, CancellationToken cancellationToken = default)
    {
        var query = _context.BackgroundJobLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(jobName))
            query = query.Where(j => j.JobName.Contains(jobName));
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(j => j.Status == status);
        if (!string.IsNullOrWhiteSpace(triggerType))
            query = query.Where(j => j.TriggerType == triggerType);
        if (fromDate.HasValue)
            query = query.Where(j => j.StartedAt >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(j => j.StartedAt <= toDate.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(j => j.StartedAt)
            .ThenByDescending(j => j.Id)
            .Skip((pagedRequest.Page - 1) * pagedRequest.PageSize)
            .Take(pagedRequest.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<BackgroundJobLogDto>(
            items.Select(MapToDto).ToList(), totalCount, pagedRequest.Page, pagedRequest.PageSize);
    }

    public async Task<BackgroundJobLogDto?> GetJobByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var job = await _context.BackgroundJobLogs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
        return job == null ? null : MapToDto(job);
    }

    public async Task<PagedResult<BackgroundJobLogDto>> GetFailedJobsAsync(PagedRequest pagedRequest, CancellationToken cancellationToken = default)
    {
        return await GetJobsAsync(null, "Failed", null, null, null, pagedRequest, cancellationToken);
    }

    public async Task<BackgroundJobLogDto> EnqueueJobAsync(EnqueueBackgroundJobRequest request, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var job = new BackgroundJobLog
        {
            JobName = request.JobName.Trim(),
            ExecutionId = Guid.NewGuid().ToString("N"),
            JobReference = request.JobReference?.Trim(),
            TriggerType = "Event",
            StartedAt = now,
            Status = "Pending",
            AttemptCount = 0,
            MaxAttempts = Math.Clamp(request.MaxAttempts, 1, 10),
            NextRetryAt = request.RunAt ?? now,
            PayloadJson = request.PayloadJson,
            CreatedAt = now
        };

        await _context.BackgroundJobLogs.AddAsync(job, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(job);
    }

    public async Task<BackgroundJobLogDto> RetryJobAsync(long id, CancellationToken cancellationToken = default)
    {
        var job = await _context.BackgroundJobLogs.FirstOrDefaultAsync(j => j.Id == id, cancellationToken)
            ?? throw new NotFoundException("BackgroundJobLog", id);

        if (job.Status is "Running" or "Pending")
            throw new ConflictException($"Job {job.Id} is currently in '{job.Status}' state and cannot be manually retried.");

        if (job.Status == "Success")
            throw new ConflictException($"Job {job.Id} has already succeeded.");

        var oldStatus = job.Status;
        job.Status = "Pending";
        job.NextRetryAt = DateTime.UtcNow;
        job.CompletedAt = null;
        job.LockedAt = null;
        job.LockedBy = null;
        job.ExceptionDetails = null;
        job.LogDetails = AppendLog(job.LogDetails, $"Manual retry triggered by {_currentUser.Username ?? "System"}.");

        await _context.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync("RetryBackgroundJob", "integration", "BackgroundJobLogs", job.Id.ToString(),
            new { Status = oldStatus }, new { Status = "Pending" }, cancellationToken: cancellationToken);

        return MapToDto(job);
    }

    public async Task<BackgroundJobLogDto> CancelJobAsync(long id, string? reason = null, CancellationToken cancellationToken = default)
    {
        var job = await _context.BackgroundJobLogs.FirstOrDefaultAsync(j => j.Id == id, cancellationToken)
            ?? throw new NotFoundException("BackgroundJobLog", id);

        if (job.Status != "Pending")
            throw new ConflictException($"Only 'Pending' background jobs can be safely cancelled. Current status: '{job.Status}'.");

        job.Status = "Cancelled";
        job.CompletedAt = DateTime.UtcNow;
        job.NextRetryAt = null;
        job.LockedAt = null;
        job.LockedBy = null;
        job.LogDetails = AppendLog(job.LogDetails, $"Cancelled by {_currentUser.Username ?? "System"}. Reason: {reason ?? "None"}");

        await _context.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync("CancelBackgroundJob", "integration", "BackgroundJobLogs", job.Id.ToString(),
            new { Status = "Pending" }, new { Status = "Cancelled", Reason = reason }, cancellationToken: cancellationToken);

        return MapToDto(job);
    }

    public async Task<int> ProcessPendingJobsAsync(string workerId, int batchSize = 20, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var claimed = await ClaimPendingJobsAsync(workerId, batchSize, now, cancellationToken);
        if (claimed.Count == 0) return 0;

        var handlers = _handlers.ToDictionary(h => h.JobName, StringComparer.OrdinalIgnoreCase);
        var claimedSet = new HashSet<object>(claimed, ReferenceEqualityComparer.Instance);
        int processed = 0;

        foreach (var job in claimed)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                job.Status = "Pending";
                job.LockedAt = null;
                job.LockedBy = null;
                await _context.SaveChangesAsync(CancellationToken.None);
                break;
            }

            try
            {
                if (!handlers.TryGetValue(job.JobName, out var handler))
                {
                    job.Status = "Failed";
                    job.CompletedAt = DateTime.UtcNow;
                    job.LockedAt = null;
                    job.LockedBy = null;
                    job.ExceptionDetails = $"No handler registered for job '{job.JobName}'.";
                    await _context.SaveChangesAsync(cancellationToken);
                    continue;
                }

                var result = await handler.ExecuteAsync(job.JobReference, job.PayloadJson, cancellationToken);
                job.RecordsProcessed = result.RecordsProcessed;
                job.RecordsSucceeded = result.RecordsSucceeded;
                job.RecordsFailed = result.RecordsFailed;
                job.LogDetails = AppendLog(job.LogDetails, result.LogDetails);
                job.LockedAt = null;
                job.LockedBy = null;

                if (result.Succeeded)
                {
                    job.Status = result.IsWarning ? "Warning" : "Success";
                    job.CompletedAt = DateTime.UtcNow;
                    job.NextRetryAt = null;
                    job.ExceptionDetails = null;
                }
                else if (!result.IsTransientFailure || job.AttemptCount >= job.MaxAttempts)
                {
                    job.Status = "Failed";
                    job.CompletedAt = DateTime.UtcNow;
                    job.NextRetryAt = null;
                    job.ExceptionDetails = result.ErrorDetails;
                }
                else
                {
                    job.Status = "Pending";
                    job.NextRetryAt = DateTime.UtcNow.Add(OutboxProcessorService.ComputeBackoffWithJitter(job.AttemptCount));
                    job.ExceptionDetails = result.ErrorDetails;
                }

                await _context.SaveChangesAsync(cancellationToken);
                processed++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                job.Status = "Pending";
                job.LockedAt = null;
                job.LockedBy = null;
                await _context.SaveChangesAsync(CancellationToken.None);
                break;
            }
            catch (Exception ex)
            {
                // Detach any dirty entities staged by failing job while keeping claimed batch jobs tracked
                foreach (var entry in _context.ChangeTracker.Entries().ToList())
                {
                    if (!claimedSet.Contains(entry.Entity))
                        entry.State = EntityState.Detached;
                }

                job.LockedAt = null;
                job.LockedBy = null;
                job.ExceptionDetails = ex.Message;

                if (job.AttemptCount >= job.MaxAttempts)
                {
                    job.Status = "Failed";
                    job.CompletedAt = DateTime.UtcNow;
                    job.NextRetryAt = null;
                }
                else
                {
                    job.Status = "Pending";
                    job.NextRetryAt = DateTime.UtcNow.Add(OutboxProcessorService.ComputeBackoffWithJitter(job.AttemptCount));
                }

                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        return processed;
    }

    public async Task<int> RecoverStaleJobsAsync(TimeSpan leaseTimeout, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.Subtract(leaseTimeout);
        var stale = await _context.BackgroundJobLogs
            .Where(j => j.Status == "Running" && j.LockedAt != null && j.LockedAt < cutoff)
            .ToListAsync(cancellationToken);

        if (stale.Count == 0) return 0;

        foreach (var j in stale)
        {
            j.Status = j.AttemptCount < j.MaxAttempts ? "Pending" : "Failed";
            j.LockedAt = null;
            j.LockedBy = null;
            j.NextRetryAt = j.Status == "Pending" ? DateTime.UtcNow : null;
            j.CompletedAt = j.Status == "Failed" ? DateTime.UtcNow : null;
            j.LogDetails = AppendLog(j.LogDetails, "Recovered from stale worker execution lock.");
        }

        await _context.SaveChangesAsync(cancellationToken);
        return stale.Count;
    }

    private async Task<List<BackgroundJobLog>> ClaimPendingJobsAsync(string workerId, int batchSize, DateTime utcNow, CancellationToken ct)
    {
        if (_context.Database.IsSqlServer())
        {
            var sql = @"
;WITH NextBatch AS (
    SELECT TOP ({0}) Id
    FROM [integration].[BackgroundJobLogs] WITH (UPDLOCK, READPAST, ROWLOCK)
    WHERE [Status] = N'Pending'
      AND ([NextRetryAt] IS NULL OR [NextRetryAt] <= {1})
    ORDER BY [CreatedAt] ASC, [Id] ASC
)
UPDATE target
SET [Status] = N'Running',
    [AttemptCount] = target.[AttemptCount] + 1,
    [StartedAt] = {1},
    [LockedAt] = {1},
    [LockedBy] = {2}
OUTPUT INSERTED.[Id], INSERTED.[AttemptCount], INSERTED.[CompletedAt], INSERTED.[CreatedAt],
       INSERTED.[ExceptionDetails], INSERTED.[ExecutionId], INSERTED.[JobName], INSERTED.[JobReference],
       INSERTED.[LockedAt], INSERTED.[LockedBy], INSERTED.[LogDetails], INSERTED.[MaxAttempts],
       INSERTED.[NextRetryAt], INSERTED.[PayloadJson], INSERTED.[RecordsFailed], INSERTED.[RecordsProcessed],
       INSERTED.[RecordsSucceeded], INSERTED.[StartedAt], INSERTED.[Status], INSERTED.[TriggerType]
FROM [integration].[BackgroundJobLogs] target
INNER JOIN NextBatch nb ON target.[Id] = nb.[Id];";

            var claimedJobs = await _context.BackgroundJobLogs
                .FromSqlRaw(sql, batchSize, utcNow, workerId)
                .AsNoTracking()
                .ToListAsync(ct);

            foreach (var row in claimedJobs)
            {
                var tracked = _context.ChangeTracker.Entries<BackgroundJobLog>()
                    .FirstOrDefault(e => e.Entity.Id == row.Id);
                if (tracked != null)
                {
                    tracked.State = EntityState.Detached;
                }
                _context.BackgroundJobLogs.Attach(row);
            }

            return claimedJobs;
        }

        var candidates = await _context.BackgroundJobLogs
            .Where(j => j.Status == "Pending" && (j.NextRetryAt == null || j.NextRetryAt <= utcNow))
            .OrderBy(j => j.CreatedAt)
            .ThenBy(j => j.Id)
            .Take(batchSize)
            .ToListAsync(ct);

        if (candidates.Count == 0) return candidates;

        foreach (var j in candidates)
        {
            j.Status = "Running";
            j.AttemptCount += 1;
            j.StartedAt = utcNow;
            j.LockedAt = utcNow;
            j.LockedBy = workerId;
        }

        await _context.SaveChangesAsync(ct);
        return candidates;
    }

    private static string AppendLog(string? existing, string? message)
    {
        if (string.IsNullOrWhiteSpace(message)) return existing ?? string.Empty;
        var entry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}Z] {message}";
        return string.IsNullOrWhiteSpace(existing) ? entry : $"{existing}\n{entry}";
    }

    private static BackgroundJobLogDto MapToDto(BackgroundJobLog j) => new()
    {
        Id = j.Id,
        JobName = j.JobName,
        ExecutionId = j.ExecutionId,
        JobReference = j.JobReference,
        TriggerType = j.TriggerType,
        StartedAt = j.StartedAt,
        CompletedAt = j.CompletedAt,
        Status = j.Status,
        AttemptCount = j.AttemptCount,
        MaxAttempts = j.MaxAttempts,
        NextRetryAt = j.NextRetryAt,
        RecordsProcessed = j.RecordsProcessed,
        RecordsSucceeded = j.RecordsSucceeded,
        RecordsFailed = j.RecordsFailed,
        LogDetails = j.LogDetails,
        ErrorSummary = j.ExceptionDetails
            ?.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(),
        CreatedAt = j.CreatedAt
    };
}

// ═══════════════════════════════════════════════════════════════
//  4. SCHEDULED REPORT SERVICE + CRON ENGINE + NULL REPORT ENGINE
// ═══════════════════════════════════════════════════════════════

public sealed class UnsupportedReportExecutionEngine : IReportExecutionEngine
{
    public Task<ReportGenerationResult> GenerateAsync(
        string reportCode,
        string queryDefinition,
        string outputFormat,
        long? branchId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ReportGenerationResult
        {
            IsSupported = false,
            Succeeded = false,
            Message = $"Report generation engine is not yet implemented for Report '{reportCode}' ({outputFormat}). No synthetic or placeholder report file was generated."
        });
    }
}

public static class CronScheduleHelper
{
    public static DateTime CalculateNextOccurrenceUtc(string cronExpression, string timeZoneId, DateTime fromUtc)
    {
        var tz = ResolveTimeZone(timeZoneId);
        var parts = cronExpression.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 5)
            throw new ValidationException("ScheduleCron", "Cron expression must consist of 5 space-separated fields.");

        var minutes = ParseField(parts[0], 0, 59);
        var hours = ParseField(parts[1], 0, 23);
        var days = ParseField(parts[2], 1, 31);
        var months = ParseField(parts[3], 1, 12);
        var daysOfWeek = ParseField(parts[4], 0, 6);

        var localFrom = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(fromUtc, DateTimeKind.Utc), tz);
        var candidate = new DateTime(localFrom.Year, localFrom.Month, localFrom.Day, localFrom.Hour, localFrom.Minute, 0, DateTimeKind.Unspecified)
            .AddMinutes(1);

        // Search up to 366 days ahead
        var maxIterations = 366 * 24 * 60;
        for (int i = 0; i < maxIterations; i++)
        {
            if (months.Contains(candidate.Month)
                && days.Contains(candidate.Day)
                && daysOfWeek.Contains((int)candidate.DayOfWeek)
                && hours.Contains(candidate.Hour)
                && minutes.Contains(candidate.Minute))
            {
                if (tz.IsInvalidTime(candidate))
                {
                    candidate = candidate.AddMinutes(1);
                    continue;
                }

                return TimeZoneInfo.ConvertTimeToUtc(candidate, tz);
            }

            candidate = candidate.AddMinutes(1);
        }

        return fromUtc.AddDays(1);
    }

    public static TimeZoneInfo ResolveTimeZone(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId)) return TimeZoneInfo.Utc;
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim());
        }
        catch (TimeZoneNotFoundException)
        {
            throw new ValidationException("TimeZoneId", $"Unrecognized time zone identifier '{timeZoneId}'.");
        }
    }

    private static HashSet<int> ParseField(string field, int min, int max)
    {
        var result = new HashSet<int>();
        foreach (var segment in field.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var step = 1;
            var rangePart = segment;

            if (segment.Contains('/'))
            {
                var stepSplit = segment.Split('/');
                if (stepSplit.Length != 2 || !int.TryParse(stepSplit[1], out step) || step <= 0)
                    throw new ValidationException("ScheduleCron", $"Invalid cron step segment '{segment}'.");
                rangePart = stepSplit[0];
            }

            int start, end;
            if (rangePart == "*")
            {
                start = min;
                end = max;
            }
            else if (rangePart.Contains('-'))
            {
                var dashSplit = rangePart.Split('-');
                if (dashSplit.Length != 2 || !int.TryParse(dashSplit[0], out start) || !int.TryParse(dashSplit[1], out end))
                    throw new ValidationException("ScheduleCron", $"Invalid cron range segment '{segment}'.");
            }
            else if (int.TryParse(rangePart, out var single))
            {
                start = single;
                end = rangePart == segment && !segment.Contains('/') ? single : max;
            }
            else
            {
                throw new ValidationException("ScheduleCron", $"Invalid cron segment '{segment}'.");
            }

            if (start < min || end > max || start > end)
                throw new ValidationException("ScheduleCron", $"Cron values out of bounds [{min}-{max}] in '{segment}'.");

            for (int v = start; v <= end; v += step)
                result.Add(v);
        }

        if (result.Count == 0)
            throw new ValidationException("ScheduleCron", $"No valid values parsed in cron field '{field}'.");

        return result;
    }
}

public sealed class ScheduledReportService : IScheduledReportService
{
    private readonly ApplicationDbContext _context;
    private readonly IReportExecutionEngine _reportEngine;
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;
    private readonly ILogger<ScheduledReportService> _logger;

    public ScheduledReportService(
        ApplicationDbContext context,
        IReportExecutionEngine reportEngine,
        INotificationService notificationService,
        ICurrentUserService currentUser,
        IAuditService audit,
        ILogger<ScheduledReportService> logger)
    {
        _context = context;
        _reportEngine = reportEngine;
        _notificationService = notificationService;
        _currentUser = currentUser;
        _audit = audit;
        _logger = logger;
    }

    public async Task<PagedResult<ReportDefinitionDto>> GetDefinitionsAsync(
        string? category, bool? isActive, PagedRequest pagedRequest, CancellationToken cancellationToken = default)
    {
        var query = _context.ReportDefinitions.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(d => d.Category == category);
        if (isActive.HasValue)
            query = query.Where(d => d.IsActive == isActive.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(d => d.Category).ThenBy(d => d.Code)
            .Skip((pagedRequest.Page - 1) * pagedRequest.PageSize)
            .Take(pagedRequest.PageSize)
            .Select(d => new ReportDefinitionDto
            {
                Id = d.Id,
                Code = d.Code,
                Name = d.Name,
                Category = d.Category,
                Description = d.Description,
                ParametersSchemaJson = d.ParametersSchemaJson,
                IsActive = d.IsActive
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<ReportDefinitionDto>(items, totalCount, pagedRequest.Page, pagedRequest.PageSize);
    }

    public async Task<ReportDefinitionDto?> GetDefinitionByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.ReportDefinitions
            .AsNoTracking()
            .Where(d => d.Id == id)
            .Select(d => new ReportDefinitionDto
            {
                Id = d.Id,
                Code = d.Code,
                Name = d.Name,
                Category = d.Category,
                Description = d.Description,
                ParametersSchemaJson = d.ParametersSchemaJson,
                IsActive = d.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedResult<ScheduledReportDto>> GetScheduledReportsAsync(
        long? branchId, bool? isActive, PagedRequest pagedRequest, CancellationToken cancellationToken = default)
    {
        var query = _context.ScheduledReports
            .Include(r => r.ReportDefinition)
            .Include(r => r.Branch)
            .AsNoTracking()
            .AsQueryable();

        if (!_currentUser.IsInRole(AppRoles.SuperAdmin))
        {
            var accessible = _currentUser.AccessibleBranchIds;
            query = query.Where(r => r.BranchId.HasValue && accessible.Contains(r.BranchId.Value));
        }

        if (branchId.HasValue)
        {
            EnsureBranchAccess(branchId.Value);
            query = query.Where(r => r.BranchId == branchId.Value);
        }

        if (isActive.HasValue)
            query = query.Where(r => r.IsActive == isActive.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((pagedRequest.Page - 1) * pagedRequest.PageSize)
            .Take(pagedRequest.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ScheduledReportDto>(
            items.Select(MapToDto).ToList(), totalCount, pagedRequest.Page, pagedRequest.PageSize);
    }

    public async Task<ScheduledReportDto?> GetScheduledReportByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var report = await _context.ScheduledReports
            .Include(r => r.ReportDefinition)
            .Include(r => r.Branch)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (report == null) return null;
        EnsureReportAccess(report.BranchId);
        return MapToDto(report);
    }

    public async Task<ScheduledReportDto> CreateAsync(CreateScheduledReportRequest request, CancellationToken cancellationToken = default)
    {
        var def = await _context.ReportDefinitions.FirstOrDefaultAsync(d => d.Id == request.ReportDefinitionId, cancellationToken)
            ?? throw new NotFoundException("ReportDefinition", request.ReportDefinitionId);

        if (!def.IsActive)
            throw new ConflictException($"Report definition '{def.Code}' is inactive.");

        EnsureReportAccess(request.BranchId);

        if (request.BranchId.HasValue)
        {
            var branchExists = await _context.Branches.AnyAsync(b => b.Id == request.BranchId.Value, cancellationToken);
            if (!branchExists) throw new NotFoundException("Branch", request.BranchId.Value);
        }

        var validatedEmails = ValidateRecipientEmails(request.RecipientEmails);
        var nextRunUtc = CronScheduleHelper.CalculateNextOccurrenceUtc(request.ScheduleCron, request.TimeZoneId, DateTime.UtcNow);

        var scheduled = new ScheduledReport
        {
            ReportDefinitionId = def.Id,
            BranchId = request.BranchId,
            ScheduleCron = request.ScheduleCron.Trim(),
            TimeZoneId = CronScheduleHelper.ResolveTimeZone(request.TimeZoneId).Id,
            OutputFormat = request.OutputFormat,
            RecipientEmails = validatedEmails,
            NextRunAt = nextRunUtc,
            IsActive = request.IsActive,
            CreatedBy = _currentUser.Username ?? "System"
        };

        await _context.ScheduledReports.AddAsync(scheduled, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync("CreateScheduledReport", "notification", "ScheduledReports", scheduled.Id.ToString(),
            null, new { def.Code, scheduled.ScheduleCron, scheduled.BranchId }, cancellationToken: cancellationToken);

        return (await GetScheduledReportByIdAsync(scheduled.Id, cancellationToken))!;
    }

    public async Task<ScheduledReportDto> UpdateAsync(long id, UpdateScheduledReportRequest request, CancellationToken cancellationToken = default)
    {
        var report = await _context.ScheduledReports
            .Include(r => r.ReportDefinition)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new NotFoundException("ScheduledReport", id);

        EnsureReportAccess(report.BranchId);
        EnsureReportAccess(request.BranchId);

        var validatedEmails = ValidateRecipientEmails(request.RecipientEmails);
        var nextRunUtc = CronScheduleHelper.CalculateNextOccurrenceUtc(request.ScheduleCron, request.TimeZoneId, DateTime.UtcNow);

        report.BranchId = request.BranchId;
        report.ScheduleCron = request.ScheduleCron.Trim();
        report.TimeZoneId = CronScheduleHelper.ResolveTimeZone(request.TimeZoneId).Id;
        report.OutputFormat = request.OutputFormat;
        report.RecipientEmails = validatedEmails;
        report.NextRunAt = nextRunUtc;
        report.UpdatedAt = DateTime.UtcNow;
        report.UpdatedBy = _currentUser.Username ?? "System";

        await _context.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync("UpdateScheduledReport", "notification", "ScheduledReports", report.Id.ToString(),
            null, new { report.ScheduleCron, report.TimeZoneId, report.BranchId }, cancellationToken: cancellationToken);

        return (await GetScheduledReportByIdAsync(report.Id, cancellationToken))!;
    }

    public async Task<ScheduledReportDto> SetActiveStatusAsync(long id, bool isActive, CancellationToken cancellationToken = default)
    {
        var report = await _context.ScheduledReports.FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new NotFoundException("ScheduledReport", id);

        EnsureReportAccess(report.BranchId);

        report.IsActive = isActive;
        if (isActive && report.NextRunAt <= DateTime.UtcNow)
        {
            report.NextRunAt = CronScheduleHelper.CalculateNextOccurrenceUtc(report.ScheduleCron, report.TimeZoneId, DateTime.UtcNow);
        }
        report.UpdatedAt = DateTime.UtcNow;
        report.UpdatedBy = _currentUser.Username ?? "System";

        await _context.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync(isActive ? "ActivateScheduledReport" : "DeactivateScheduledReport",
            "notification", "ScheduledReports", report.Id.ToString(), null, new { report.IsActive }, cancellationToken: cancellationToken);

        return (await GetScheduledReportByIdAsync(report.Id, cancellationToken))!;
    }

    public async Task<ScheduledReportRunResultDto> TriggerRunAsync(long id, CancellationToken cancellationToken = default)
    {
        var report = await _context.ScheduledReports
            .Include(r => r.ReportDefinition)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new NotFoundException("ScheduledReport", id);

        EnsureReportAccess(report.BranchId);

        return await ExecuteScheduledReportAsync(report, "Manual", cancellationToken);
    }

    public async Task<int> ProcessDueReportsAsync(string workerId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var dueReports = await _context.ScheduledReports
            .Include(r => r.ReportDefinition)
            .Where(r => r.IsActive && r.ReportDefinition.IsActive && r.NextRunAt <= now)
            .OrderBy(r => r.NextRunAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        int count = 0;
        foreach (var report in dueReports)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                await ExecuteScheduledReportAsync(report, "Scheduled", cancellationToken);
                count++;
            }
            catch (ConflictException)
            {
                // Already running — skip overlapping execution safely
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing due scheduled report {ReportId}.", report.Id);
            }
        }

        return count;
    }

    public async Task<PagedResult<BackgroundJobLogDto>> GetExecutionHistoryAsync(
        long scheduledReportId, PagedRequest pagedRequest, CancellationToken cancellationToken = default)
    {
        var report = await _context.ScheduledReports.AsNoTracking().FirstOrDefaultAsync(r => r.Id == scheduledReportId, cancellationToken)
            ?? throw new NotFoundException("ScheduledReport", scheduledReportId);

        EnsureReportAccess(report.BranchId);

        var jobRef = $"ScheduledReport:{scheduledReportId}";
        var query = _context.BackgroundJobLogs.AsNoTracking()
            .Where(j => j.JobName == "ScheduledReportExecution" && j.JobReference == jobRef);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(j => j.StartedAt)
            .Skip((pagedRequest.Page - 1) * pagedRequest.PageSize)
            .Take(pagedRequest.PageSize)
            .Select(j => new BackgroundJobLogDto
            {
                Id = j.Id,
                JobName = j.JobName,
                ExecutionId = j.ExecutionId,
                JobReference = j.JobReference,
                TriggerType = j.TriggerType,
                StartedAt = j.StartedAt,
                CompletedAt = j.CompletedAt,
                Status = j.Status,
                AttemptCount = j.AttemptCount,
                MaxAttempts = j.MaxAttempts,
                NextRetryAt = j.NextRetryAt,
                RecordsProcessed = j.RecordsProcessed,
                RecordsSucceeded = j.RecordsSucceeded,
                RecordsFailed = j.RecordsFailed,
                LogDetails = j.LogDetails,
                ErrorSummary = j.ExceptionDetails,
                CreatedAt = j.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<BackgroundJobLogDto>(items, totalCount, pagedRequest.Page, pagedRequest.PageSize);
    }

    private async Task<ScheduledReportRunResultDto> ExecuteScheduledReportAsync(
        ScheduledReport report, string triggerType, CancellationToken cancellationToken)
    {
        var jobRef = $"ScheduledReport:{report.Id}";
        var alreadyRunning = await _context.BackgroundJobLogs.AnyAsync(
            j => j.JobName == "ScheduledReportExecution" && j.JobReference == jobRef && j.Status == "Running",
            cancellationToken);

        if (alreadyRunning)
            throw new ConflictException($"Scheduled report {report.Id} is already executing. Overlapping run prevented.");

        var executionId = Guid.NewGuid().ToString("N");
        var startedAt = DateTime.UtcNow;

        var jobLog = new BackgroundJobLog
        {
            JobName = "ScheduledReportExecution",
            ExecutionId = executionId,
            JobReference = jobRef,
            TriggerType = triggerType,
            StartedAt = startedAt,
            Status = "Running",
            AttemptCount = 1,
            MaxAttempts = 1
        };

        await _context.BackgroundJobLogs.AddAsync(jobLog, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var genResult = await _reportEngine.GenerateAsync(
            report.ReportDefinition.Code,
            report.ReportDefinition.QueryDefinition,
            report.OutputFormat,
            report.BranchId,
            cancellationToken);

        jobLog.CompletedAt = DateTime.UtcNow;
        jobLog.RecordsProcessed = 1;

        if (!genResult.IsSupported)
        {
            jobLog.Status = "Warning";
            jobLog.RecordsFailed = 0;
            jobLog.LogDetails = genResult.Message;
        }
        else if (genResult.Succeeded)
        {
            jobLog.Status = "Success";
            jobLog.RecordsSucceeded = 1;
            jobLog.LogDetails = $"Report generated ({genResult.FileName}) and queued for recipient delivery.";

            var emails = report.RecipientEmails.Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var email in emails)
            {
                await _notificationService.CreateAsync(new Application.Modules.Notification.DTOs.CreateNotificationRequest
                {
                    RecipientEmail = email,
                    Channel = "Email",
                    Title = $"Scheduled Report: {report.ReportDefinition.Name}",
                    Content = $"Your scheduled report '{report.ReportDefinition.Name}' ({report.OutputFormat}) generated at {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC is ready.",
                    NotificationType = "ScheduledReportReady",
                    ReferenceType = "ScheduledReport",
                    ReferenceId = report.Id,
                    BranchId = report.BranchId,
                    EventKey = $"report:{report.Id}:{executionId}:{email}"
                }, cancellationToken);
            }
        }
        else
        {
            jobLog.Status = "Failed";
            jobLog.RecordsFailed = 1;
            jobLog.ExceptionDetails = genResult.Message;
        }

        // Advance NextRunAt strictly forward from UtcNow to avoid missed-execution catch-up storms
        report.LastRunAt = DateTime.UtcNow;
        report.NextRunAt = CronScheduleHelper.CalculateNextOccurrenceUtc(report.ScheduleCron, report.TimeZoneId, DateTime.UtcNow);
        await _context.SaveChangesAsync(cancellationToken);

        if (triggerType == "Manual")
        {
            await _audit.RecordAsync("TriggerScheduledReport", "notification", "ScheduledReports", report.Id.ToString(),
                null, new { ExecutionId = executionId, jobLog.Status }, cancellationToken: cancellationToken);
        }

        return new ScheduledReportRunResultDto
        {
            ScheduledReportId = report.Id,
            ExecutionId = executionId,
            Status = jobLog.Status,
            IsReportGenerationSupported = genResult.IsSupported,
            Message = genResult.Message,
            ExecutedAt = jobLog.CompletedAt.Value
        };
    }

    private void EnsureBranchAccess(long branchId)
    {
        if (_currentUser.IsInRole(AppRoles.SuperAdmin)) return;
        if (!_currentUser.HasBranchAccess(branchId))
            throw new ForbiddenAccessException($"You do not have access to branch '{branchId}'.");
    }

    private void EnsureReportAccess(long? branchId)
    {
        if (_currentUser.IsInRole(AppRoles.SuperAdmin)) return;
        if (!branchId.HasValue)
            throw new ForbiddenAccessException("Global (unbranched) scheduled reports require SuperAdmin permission.");
        EnsureBranchAccess(branchId.Value);
    }

    private static string ValidateRecipientEmails(string raw)
    {
        var emails = raw.Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (emails.Count == 0)
            throw new ValidationException("RecipientEmails", "At least one valid recipient email is required.");

        foreach (var email in emails)
        {
            try
            {
                _ = new System.Net.Mail.MailAddress(email);
            }
            catch
            {
                throw new ValidationException("RecipientEmails", $"Invalid recipient email address '{email}'.");
            }
        }

        var joined = string.Join(";", emails);
        if (joined.Length > 500)
            throw new ValidationException("RecipientEmails", "Total recipient email list length cannot exceed 500 characters.");

        return joined;
    }

    private static ScheduledReportDto MapToDto(ScheduledReport r) => new()
    {
        Id = r.Id,
        PublicId = r.PublicId,
        ReportDefinitionId = r.ReportDefinitionId,
        ReportCode = r.ReportDefinition?.Code ?? string.Empty,
        ReportName = r.ReportDefinition?.Name ?? string.Empty,
        BranchId = r.BranchId,
        BranchName = r.Branch?.Name,
        ScheduleCron = r.ScheduleCron,
        TimeZoneId = r.TimeZoneId,
        OutputFormat = r.OutputFormat,
        RecipientEmails = r.RecipientEmails,
        LastRunAt = r.LastRunAt,
        NextRunAt = r.NextRunAt,
        IsActive = r.IsActive,
        CreatedAt = r.CreatedAt
    };
}

// ═══════════════════════════════════════════════════════════════
//  5. SCHEDULED MAINTENANCE SERVICE (SESSIONS & EXPIRED CLEANUP)
// ═══════════════════════════════════════════════════════════════

public sealed class ScheduledMaintenanceService : IScheduledMaintenanceService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ScheduledMaintenanceService> _logger;

    public ScheduledMaintenanceService(ApplicationDbContext context, ILogger<ScheduledMaintenanceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<int> CleanupExpiredSessionsAndTokensAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-7);

        var expiredSessions = await _context.UserSessions
            .Where(s => s.ExpiresAt < cutoff || (s.IsTerminated && s.LastActivityAt < cutoff))
            .Take(500)
            .ToListAsync(cancellationToken);

        var expiredTokens = await _context.RefreshTokens
            .Where(t => t.ExpiresAt < cutoff || (t.RevokedAt != null && t.RevokedAt < cutoff))
            .Take(500)
            .ToListAsync(cancellationToken);

        var total = expiredSessions.Count + expiredTokens.Count;
        if (total == 0) return 0;

        if (expiredSessions.Count > 0)
            _context.UserSessions.RemoveRange(expiredSessions);

        if (expiredTokens.Count > 0)
            _context.RefreshTokens.RemoveRange(expiredTokens);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Maintenance cleanup removed {SessionCount} expired sessions and {TokenCount} expired refresh tokens.",
            expiredSessions.Count, expiredTokens.Count);

        return total;
    }

    public async Task<int> CleanupExpiredNotificationsAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-30);

        var expired = await _context.Notifications
            .Where(n => n.ExpiresAt != null && n.ExpiresAt < cutoff)
            .Take(500)
            .ToListAsync(cancellationToken);

        if (expired.Count == 0) return 0;

        _context.Notifications.RemoveRange(expired);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Maintenance cleanup removed {Count} expired notifications past retention.", expired.Count);
        return expired.Count;
    }
}

