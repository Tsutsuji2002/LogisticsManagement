namespace Logistics.Server.Application.Modules.Shipping.DTOs;

public record ReturnRequestDto
{
    public long Id { get; init; }
    public Guid PublicId { get; init; }
    public string ReturnCode { get; init; } = string.Empty;
    public long OriginalShipmentId { get; init; }
    public string OriginalTrackingNumber { get; init; } = string.Empty;
    public long? NewReturnShipmentId { get; init; }
    public string? NewReturnTrackingNumber { get; init; }
    public string ReasonCode { get; init; } = string.Empty;
    public string ReasonDescription { get; init; } = string.Empty;
    /// <summary>"Requested", "Approved", "InTransit", "ReturnedToSender", "Rejected"</summary>
    public string Status { get; init; } = "Requested";
    public DateTime RequestedAt { get; init; }
    public string? ApprovedByUserId { get; init; }
    public DateTime? ApprovedAt { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? UpdatedBy { get; init; }
}

public record CreateReturnRequestRequest
{
    public long OriginalShipmentId { get; init; }
    public string ReasonCode { get; init; } = string.Empty;
    public string ReasonDescription { get; init; } = string.Empty;
    public string? Notes { get; init; }
}

public record ProcessReturnRequestRequest
{
    /// <summary>"Approved" or "Rejected"</summary>
    public string Decision { get; init; } = string.Empty;
    public string? Notes { get; init; }
}
