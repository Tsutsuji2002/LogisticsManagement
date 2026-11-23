using Logistics.Server.Domain.Entities.Audit;
using Logistics.Server.Domain.Entities.Identity;

namespace Logistics.Server.Domain.Entities.Shipping;

public class ProofOfDelivery
{
    public long Id { get; set; }
    public long ShipmentId { get; set; }
    public long DeliveryAttemptId { get; set; }
    public string RecipientName { get; set; } = string.Empty;
    public string RecipientRelation { get; set; } = "Self"; // Self, FamilyMember, Receptionist, SecurityGuard, Colleague
    public string? RecipientNationalId { get; set; }
    public DateTime DeliveredAt { get; set; } = DateTime.UtcNow;
    public long? SignatureAttachmentId { get; set; }
    public long? PhotoAttachmentId { get; set; }
    public string? Notes { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public long? VerifiedByUserId { get; set; }
    public DateTime? VerifiedAt { get; set; }

    // Navigation
    public virtual Shipment Shipment { get; set; } = null!;
    public virtual DeliveryAttempt DeliveryAttempt { get; set; } = null!;
    public virtual FileAttachment? SignatureAttachment { get; set; }
    public virtual FileAttachment? PhotoAttachment { get; set; }
    public virtual UserAccount? VerifiedByUser { get; set; }
}
