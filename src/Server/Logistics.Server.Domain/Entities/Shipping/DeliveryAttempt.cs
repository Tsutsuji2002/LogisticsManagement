using Logistics.Server.Domain.Entities.Organization;

namespace Logistics.Server.Domain.Entities.Shipping;

public class DeliveryAttempt
{
    public long Id { get; set; }
    public long ShipmentId { get; set; }
    public int AttemptNumber { get; set; }
    public long AttemptedByEmployeeId { get; set; }
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Delivered"; // Delivered, Failed, Rescheduled
    public string? FailureReasonCode { get; set; }
    public string? FailureNotes { get; set; }
    public DateOnly? NextScheduledAttemptDate { get; set; }
    public bool RecipientContacted { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    // Navigation
    public virtual Shipment Shipment { get; set; } = null!;
    public virtual Employee AttemptedByEmployee { get; set; } = null!;
    public virtual ProofOfDelivery? ProofOfDelivery { get; set; }
}
