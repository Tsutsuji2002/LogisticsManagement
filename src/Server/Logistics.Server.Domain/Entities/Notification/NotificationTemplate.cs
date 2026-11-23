using Logistics.Server.Domain.Common;

namespace Logistics.Server.Domain.Entities.Notification;

public class NotificationTemplate : BaseEntity
{
    public string Code { get; set; } = string.Empty; // SHIPMENT_DISPATCHED, OUT_FOR_DELIVERY, INVOICE_ISSUED
    public string Name { get; set; } = string.Empty;
    public string Channel { get; set; } = "InApp"; // InApp, Email, SMS, Webhook
    public string? SubjectTemplate { get; set; }
    public string BodyTemplate { get; set; } = string.Empty;
    public string? ParametersJson { get; set; }
    public bool IsActive { get; set; } = true;
}
