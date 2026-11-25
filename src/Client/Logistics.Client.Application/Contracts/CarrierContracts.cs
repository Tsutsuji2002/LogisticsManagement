namespace Logistics.Client.Application.Contracts.Carrier;

public class CarrierDto
{
    public long Id { get; set; }
    public Guid PublicId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public string? WebsiteUrl { get; set; }
    public string ApiBaseUrl { get; set; } = string.Empty;
    public string? TrackingUrlTemplate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ServiceCount { get; set; }
}

public class CarrierDetailDto : CarrierDto
{
    public IReadOnlyList<CarrierServiceDto> Services { get; set; } = [];
    public IReadOnlyList<CarrierTrackingMappingDto> TrackingMappings { get; set; } = [];
}

public class CreateCarrierRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public string? WebsiteUrl { get; set; }
    public string ApiBaseUrl { get; set; } = string.Empty;
    public string? TrackingUrlTemplate { get; set; }
}

public class UpdateCarrierRequest
{
    public string Name { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public string? WebsiteUrl { get; set; }
    public string ApiBaseUrl { get; set; } = string.Empty;
    public string? TrackingUrlTemplate { get; set; }
}

public class CarrierServiceDto
{
    public long Id { get; set; }
    public Guid PublicId { get; set; }
    public long CarrierId { get; set; }
    public string CarrierCode { get; set; } = string.Empty;
    public string ServiceCode { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? EstimatedTransitHours { get; set; }
    public decimal? MaxWeightKg { get; set; }
    public bool IsCodSupported { get; set; }
    public bool IsInsuranceSupported { get; set; }
    public bool IsActive { get; set; }
}

public class CreateCarrierServiceRequest
{
    public string ServiceCode { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? EstimatedTransitHours { get; set; }
    public decimal? MaxWeightKg { get; set; }
    public bool IsCodSupported { get; set; } = true;
    public bool IsInsuranceSupported { get; set; } = true;
}

public class CarrierConfigurationDto
{
    public long Id { get; set; }
    public long CarrierId { get; set; }
    public string CarrierCode { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public bool HasApiKey { get; set; }
    public bool HasApiSecret { get; set; }
    public bool HasClientId { get; set; }
    public bool HasUsername { get; set; }
    public bool HasPassword { get; set; }
    public bool HasWebhookSecret { get; set; }
    public string? SettingsJson { get; set; }
    public bool IsActive { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UpsertCarrierConfigurationRequest
{
    public string Environment { get; set; } = "Production";
    public string? ApiKey { get; set; }
    public string? ApiSecret { get; set; }
    public string? ClientId { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? WebhookSecret { get; set; }
    public string? SettingsJson { get; set; }
}

public class CarrierTrackingMappingDto
{
    public long Id { get; set; }
    public long CarrierId { get; set; }
    public string CarrierCode { get; set; } = string.Empty;
    public string ExternalStatusCode { get; set; } = string.Empty;
    public string? ExternalStatusName { get; set; }
    public string InternalShipmentStatus { get; set; } = string.Empty;
    public string? InternalSubStatus { get; set; }
    public bool IsActive { get; set; }
}

public class CreateTrackingMappingRequest
{
    public string ExternalStatusCode { get; set; } = string.Empty;
    public string? ExternalStatusName { get; set; }
    public string InternalShipmentStatus { get; set; } = string.Empty;
    public string? InternalSubStatus { get; set; }
    public string? Description { get; set; }
}

public class CarrierTrackingEventDto
{
    public long Id { get; set; }
    public long CarrierId { get; set; }
    public string CarrierCode { get; set; } = string.Empty;
    public string CarrierTrackingNumber { get; set; } = string.Empty;
    public string ExternalEventId { get; set; } = string.Empty;
    public string EventCode { get; set; } = string.Empty;
    public string EventDescription { get; set; } = string.Empty;
    public DateTime EventTimestamp { get; set; }
    public string? LocationName { get; set; }
    public string ProcessedStatus { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}

public class CarrierWebhookLogDto
{
    public long Id { get; set; }
    public long CarrierId { get; set; }
    public string CarrierCode { get; set; } = string.Empty;
    public string? ExternalEventId { get; set; }
    public string WebhookUrl { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public int ResponseStatusCode { get; set; }
    public bool ProcessedSuccessfully { get; set; }
    public DateTime ProcessedAt { get; set; }
    public string? ErrorDetails { get; set; }
}

public class CarrierApiLogDto
{
    public long Id { get; set; }
    public long CarrierId { get; set; }
    public string CarrierCode { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public int ResponseStatusCode { get; set; }
    public int DurationMs { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}

public class CarrierSyncJobDto
{
    public long Id { get; set; }
    public long CarrierId { get; set; }
    public string CarrierCode { get; set; } = string.Empty;
    public string JobType { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TotalRecordsProcessed { get; set; }
    public int TotalSuccessCount { get; set; }
    public int TotalFailureCount { get; set; }
    public string? ErrorLog { get; set; }
}

public class CarrierBookShipmentRequest
{
    public string ShipmentIdentifier { get; set; } = string.Empty;
    public string? CarrierServiceCode { get; set; }
}

public class CarrierBookingResultDto
{
    public bool Success { get; set; }
    public string? CarrierTrackingNumber { get; set; }
    public string? CarrierOrderCode { get; set; }
    public decimal? TotalFee { get; set; }
    public string? LabelUrl { get; set; }
    public string? ErrorMessage { get; set; }
}

public class CarrierCancelShipmentRequest
{
    public string ShipmentIdentifier { get; set; } = string.Empty;
    public string? Reason { get; set; }
}

public class CarrierCancelResultDto
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

public class CarrierRateQueryRequest
{
    public long CarrierId { get; set; }
    public decimal WeightKg { get; set; }
    public decimal LengthCm { get; set; }
    public decimal WidthCm { get; set; }
    public decimal HeightCm { get; set; }
    public decimal DeclaredValue { get; set; }
    public decimal CodAmount { get; set; }
}

public class CarrierRateOptionDto
{
    public string ServiceCode { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public decimal TotalFee { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal InsuranceFee { get; set; }
    public int EstimatedDays { get; set; }
}

public class CarrierRateResultDto
{
    public bool Success { get; set; }
    public IReadOnlyList<CarrierRateOptionDto> Options { get; set; } = [];
    public string? ErrorMessage { get; set; }
}

public class CarrierTrackingQueryDto
{
    public string TrackingNumber { get; set; } = string.Empty;
    public string CarrierCode { get; set; } = string.Empty;
    public string CurrentStatus { get; set; } = string.Empty;
    public IReadOnlyList<CarrierTrackingEventDto> Events { get; set; } = [];
}
