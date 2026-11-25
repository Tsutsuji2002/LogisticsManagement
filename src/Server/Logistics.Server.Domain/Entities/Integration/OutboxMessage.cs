using Logistics.Server.Domain.Common;

namespace Logistics.Server.Domain.Entities.Integration;

public class OutboxMessage : BaseHistoricalEntity
{
    public string EventType { get; set; } = string.Empty;
    public string AggregateType { get; set; } = string.Empty;
    public string AggregateId { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public DateTime? ProcessedAt { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, InProgress, Published, Failed
    public int RetryCount { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public DateTime? LockedAt { get; set; }
    public string? LockedBy { get; set; }
    public string? CorrelationId { get; set; }
    public string? ErrorMessage { get; set; }
}
