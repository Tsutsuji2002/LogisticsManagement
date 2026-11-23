namespace Logistics.Server.Application.Modules.Shipping.DTOs;

public record ProofOfDeliveryDto
{
    public long Id { get; init; }
    public long ShipmentId { get; init; }
    public long DeliveryAttemptId { get; init; }
    public string RecipientName { get; init; } = string.Empty;
    /// <summary>"Self", "FamilyMember", "Receptionist", "SecurityGuard", "Colleague"</summary>
    public string RecipientRelation { get; init; } = string.Empty;
    public string? RecipientNationalId { get; init; }
    public DateTime DeliveredAt { get; init; }
    public long? SignatureAttachmentId { get; init; }
    public long? PhotoAttachmentId { get; init; }
    public string? Notes { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public string? VerifiedByUserId { get; init; }
    public DateTime? VerifiedAt { get; init; }
}

public record CreateProofOfDeliveryRequest
{
    public string RecipientName { get; init; } = string.Empty;
    /// <summary>"Self", "FamilyMember", "Receptionist", "SecurityGuard", "Colleague"</summary>
    public string RecipientRelation { get; init; } = string.Empty;
    public string? RecipientNationalId { get; init; }
    public DateTime DeliveredAt { get; init; }
    public long? SignatureAttachmentId { get; init; }
    public long? PhotoAttachmentId { get; init; }
    public string? Notes { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
}
