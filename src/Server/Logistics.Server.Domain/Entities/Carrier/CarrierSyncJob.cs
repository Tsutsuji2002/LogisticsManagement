using Logistics.Server.Domain.Common;

namespace Logistics.Server.Domain.Entities.Carrier;

public class CarrierSyncJob : BaseHistoricalEntity
{
    public long CarrierId { get; set; }
    public string JobType { get; set; } = string.Empty; // TrackingPoll, FeeReconciliation, ManifestSync
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime? EndTime { get; set; }
    public string Status { get; set; } = "Running"; // Running, Completed, Failed
    public int TotalRecordsProcessed { get; set; }
    public int TotalSuccessCount { get; set; }
    public int TotalFailureCount { get; set; }
    public string? ErrorLog { get; set; }

    // Navigation
    public virtual Carrier Carrier { get; set; } = null!;
}
