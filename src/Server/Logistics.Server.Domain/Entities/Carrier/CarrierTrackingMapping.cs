namespace Logistics.Server.Domain.Entities.Carrier;

public class CarrierTrackingMapping
{
    public long Id { get; set; }
    public long CarrierId { get; set; }
    public string ExternalStatusCode { get; set; } = string.Empty;
    public string? ExternalStatusName { get; set; }
    public string InternalShipmentStatus { get; set; } = string.Empty;
    public string? InternalSubStatus { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual Carrier Carrier { get; set; } = null!;
}
