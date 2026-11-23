using Logistics.Server.Domain.Entities.Shipping;

namespace Logistics.Server.Domain.Entities.Carrier;

public class CarrierTrackingEvent
{
    public long Id { get; set; }
    public long CarrierId { get; set; }
    public string CarrierTrackingNumber { get; set; } = string.Empty;
    public string ExternalEventId { get; set; } = string.Empty;
    public string EventCode { get; set; } = string.Empty;
    public string EventDescription { get; set; } = string.Empty;
    public DateTime EventTimestamp { get; set; }
    public string? LocationName { get; set; }
    public string RawPayloadJson { get; set; } = string.Empty;
    public string ProcessedStatus { get; set; } = "Pending"; // Pending, Processed, Ignored, Failed
    public long? ProcessedShipmentId { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? ErrorMessage { get; set; }

    // Navigation
    public virtual Carrier Carrier { get; set; } = null!;
    public virtual Shipment? ProcessedShipment { get; set; }
}
