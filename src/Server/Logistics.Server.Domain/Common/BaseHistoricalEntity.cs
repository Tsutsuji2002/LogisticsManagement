namespace Logistics.Server.Domain.Common;

/// <summary>
/// Base class for append-only, immutable historical and telemetry log records.
/// Avoids mutable update and concurrency tracking overhead.
/// </summary>
public abstract class BaseHistoricalEntity
{
    /// <summary>
    /// Internal surrogate primary key (BIGINT IDENTITY).
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// UTC timestamp when this event or log entry was recorded.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
