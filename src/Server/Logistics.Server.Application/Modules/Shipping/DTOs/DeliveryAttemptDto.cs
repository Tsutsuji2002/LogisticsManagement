namespace Logistics.Server.Application.Modules.Shipping.DTOs;

public record DeliveryAttemptDto
{
    public long Id { get; init; }
    public long ShipmentId { get; init; }
    public string TrackingNumber { get; init; } = string.Empty;
    public int AttemptNumber { get; init; }
    public long AttemptedByEmployeeId { get; init; }
    public string AttemptedByEmployeeName { get; init; } = string.Empty;
    public DateTime AttemptedAt { get; init; }
    /// <summary>"Delivered", "Failed", "Rescheduled"</summary>
    public string Status { get; init; } = string.Empty;
    public string? FailureReasonCode { get; init; }
    public string? FailureNotes { get; init; }
    public DateOnly? NextScheduledAttemptDate { get; init; }
    public bool RecipientContacted { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public ProofOfDeliveryDto? ProofOfDelivery { get; init; }
}

public record RecordDeliveryAttemptRequest
{
    public long AttemptedByEmployeeId { get; init; }
    public DateTime AttemptedAt { get; init; }
    /// <summary>"Delivered", "Failed", "Rescheduled"</summary>
    public string Status { get; init; } = string.Empty;
    public string? FailureReasonCode { get; init; }
    public string? FailureNotes { get; init; }
    public DateOnly? NextScheduledAttemptDate { get; init; }
    public bool RecipientContacted { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
}
