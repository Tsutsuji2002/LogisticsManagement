using Logistics.Server.Domain.Common;

namespace Logistics.Server.Domain.Entities.Integration;

public class BackgroundJobLog : BaseHistoricalEntity
{
    public string JobName { get; set; } = string.Empty;
    public string ExecutionId { get; set; } = string.Empty;
    public string? JobReference { get; set; }
    public string TriggerType { get; set; } = "Scheduled"; // Scheduled, Manual, Event
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string Status { get; set; } = "Running"; // Pending, Running, Success, Warning, Failed, Cancelled
    public int AttemptCount { get; set; } = 1;
    public int MaxAttempts { get; set; } = 3;
    public DateTime? NextRetryAt { get; set; }
    public DateTime? LockedAt { get; set; }
    public string? LockedBy { get; set; }
    public string? PayloadJson { get; set; }
    public int RecordsProcessed { get; set; }
    public int RecordsSucceeded { get; set; }
    public int RecordsFailed { get; set; }
    public string? LogDetails { get; set; }
    public string? ExceptionDetails { get; set; }
}
