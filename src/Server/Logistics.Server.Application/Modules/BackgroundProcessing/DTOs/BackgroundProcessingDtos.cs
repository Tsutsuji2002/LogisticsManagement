namespace Logistics.Server.Application.Modules.BackgroundProcessing.DTOs;

// ═══════════════════════════════════════════════════════════════
//  1. BACKGROUND JOB LOGS & HEALTH DTOs
// ═══════════════════════════════════════════════════════════════

public record BackgroundJobLogDto
{
    public long Id { get; init; }
    public string JobName { get; init; } = string.Empty;
    public string ExecutionId { get; init; } = string.Empty;
    public string? JobReference { get; init; }
    public string TriggerType { get; init; } = "Scheduled";
    public DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public string Status { get; init; } = "Running";
    public int AttemptCount { get; init; }
    public int MaxAttempts { get; init; }
    public DateTime? NextRetryAt { get; init; }
    public int RecordsProcessed { get; init; }
    public int RecordsSucceeded { get; init; }
    public int RecordsFailed { get; init; }
    public string? LogDetails { get; init; }
    public string? ErrorSummary { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record EnqueueBackgroundJobRequest
{
    public string JobName { get; init; } = string.Empty;
    public string? JobReference { get; init; }
    public string? PayloadJson { get; init; }
    public int MaxAttempts { get; init; } = 3;
    public DateTime? RunAt { get; init; }
}

public record BackgroundJobExecutionResult
{
    public bool Succeeded { get; init; }
    public bool IsWarning { get; init; }
    public int RecordsProcessed { get; init; }
    public int RecordsSucceeded { get; init; }
    public int RecordsFailed { get; init; }
    public string? LogDetails { get; init; }
    public string? ErrorDetails { get; init; }
    public bool IsTransientFailure { get; init; } = true;
}

public record OutboxMessageDto
{
    public long Id { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string AggregateType { get; init; } = string.Empty;
    public string AggregateId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int RetryCount { get; init; }
    public DateTime? NextRetryAt { get; init; }
    public DateTime? ProcessedAt { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record BackgroundProcessingHealthDto
{
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
    public string Status { get; init; } = "Healthy";
    public int PendingOutboxCount { get; init; }
    public int InProgressOutboxCount { get; init; }
    public int FailedOutboxCount { get; init; }
    public int PendingJobsCount { get; init; }
    public int RunningJobsCount { get; init; }
    public int FailedJobsCount { get; init; }
    public int PendingNotificationsCount { get; init; }
    public int FailedNotificationsCount { get; init; }
    public int DueScheduledReportsCount { get; init; }
    public DateTime? LastOutboxProcessedAt { get; init; }
}

// ═══════════════════════════════════════════════════════════════
//  2. SCHEDULED REPORTS & REPORT DEFINITIONS DTOs
// ═══════════════════════════════════════════════════════════════

public record ReportDefinitionDto
{
    public long Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? ParametersSchemaJson { get; init; }
    public bool IsActive { get; init; }
}

public record ScheduledReportDto
{
    public long Id { get; init; }
    public Guid PublicId { get; init; }
    public long ReportDefinitionId { get; init; }
    public string ReportCode { get; init; } = string.Empty;
    public string ReportName { get; init; } = string.Empty;
    public long? BranchId { get; init; }
    public string? BranchName { get; init; }
    public string ScheduleCron { get; init; } = string.Empty;
    public string TimeZoneId { get; init; } = "UTC";
    public string OutputFormat { get; init; } = "Pdf";
    public string RecipientEmails { get; init; } = string.Empty;
    public DateTime? LastRunAt { get; init; }
    public DateTime NextRunAt { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateScheduledReportRequest
{
    public long ReportDefinitionId { get; init; }
    public long? BranchId { get; init; }
    public string ScheduleCron { get; init; } = string.Empty;
    public string TimeZoneId { get; init; } = "UTC";
    public string OutputFormat { get; init; } = "Pdf";
    public string RecipientEmails { get; init; } = string.Empty;
    public bool IsActive { get; init; } = true;
}

public record UpdateScheduledReportRequest
{
    public long? BranchId { get; init; }
    public string ScheduleCron { get; init; } = string.Empty;
    public string TimeZoneId { get; init; } = "UTC";
    public string OutputFormat { get; init; } = "Pdf";
    public string RecipientEmails { get; init; } = string.Empty;
}

public record UpdateScheduledReportStatusRequest
{
    public bool IsActive { get; init; }
}

public record ScheduledReportRunResultDto
{
    public long ScheduledReportId { get; init; }
    public string ExecutionId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public bool IsReportGenerationSupported { get; init; }
    public string Message { get; init; } = string.Empty;
    public DateTime ExecutedAt { get; init; }
}

public record ReportGenerationResult
{
    public bool IsSupported { get; init; }
    public bool Succeeded { get; init; }
    public byte[]? FileBytes { get; init; }
    public string? FileName { get; init; }
    public string? ContentType { get; init; }
    public string Message { get; init; } = string.Empty;
}

