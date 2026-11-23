using Logistics.Server.Domain.Common;

namespace Logistics.Server.Domain.Entities.Notification;

public class NotificationDelivery : BaseHistoricalEntity
{
    public long NotificationId { get; set; }
    public string Channel { get; set; } = string.Empty;
    public int AttemptNumber { get; set; } = 1;
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Success"; // Success, Failed
    public string? GatewayResponse { get; set; }
    public string? ErrorDetails { get; set; }

    // Navigation
    public virtual Notification Notification { get; set; } = null!;
}
