using Logistics.Server.Domain.Common;
using Logistics.Server.Domain.Entities.Identity;

namespace Logistics.Server.Domain.Entities.Shipping;

public class ReturnRequest : BaseEntity
{
    public string ReturnCode { get; set; } = string.Empty;
    public long OriginalShipmentId { get; set; }
    public long? NewReturnShipmentId { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public string ReasonDescription { get; set; } = string.Empty;
    public string Status { get; set; } = "Requested"; // Requested, Approved, InTransit, ReturnedToSender, Rejected
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public long? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public virtual Shipment OriginalShipment { get; set; } = null!;
    public virtual Shipment? NewReturnShipment { get; set; }
    public virtual UserAccount? ApprovedByUser { get; set; }
}
