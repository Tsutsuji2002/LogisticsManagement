using Logistics.Server.Domain.Common;

namespace Logistics.Server.Domain.Entities.Integration;

public class BackgroundJobLog : BaseHistoricalEntity
{
    public string JobName { get; set; } = string.Empty;
    public string ExecutionId { get; set; } = string.Empty;
    public string TriggerType { get; set; } = "Scheduled"; // Scheduled, Manual, Event
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string Status { get; set; } = "Running"; // Running, Success, Warning, Failed
    public int RecordsProcessed { get; set; }
    public int RecordsSucceeded { get; set; }
    public int RecordsFailed { get; set; }
    public string? LogDetails { get; set; }
    public string? ExceptionDetails { get; set; }
}
