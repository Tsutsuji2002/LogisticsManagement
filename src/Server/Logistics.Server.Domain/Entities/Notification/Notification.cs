using Logistics.Server.Domain.Common;
using Logistics.Server.Domain.Entities.Identity;
using Logistics.Server.Domain.Entities.Organization;

namespace Logistics.Server.Domain.Entities.Notification;

public class Notification : BaseEntity
{
    public long? TemplateId { get; set; }
    public long? RecipientUserId { get; set; }
    public string? RecipientEmail { get; set; }
    public string? RecipientPhone { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Channel { get; set; } = "InApp"; // InApp, Email, SMS, Webhook
    public string Priority { get; set; } = "Normal"; // Low, Normal, High, Urgent
    public string Status { get; set; } = "Pending"; // Pending, Queued, Sent, Delivered, Failed, Cancelled
    public string? NotificationType { get; set; }
    public string? ReferenceType { get; set; }
    public long? ReferenceId { get; set; }
    public long? BranchId { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? EventKey { get; set; }

    // Navigation
    public virtual NotificationTemplate? Template { get; set; }
    public virtual UserAccount? RecipientUser { get; set; }
    public virtual Branch? Branch { get; set; }
    public virtual ICollection<NotificationDelivery> Deliveries { get; set; } = [];
}
