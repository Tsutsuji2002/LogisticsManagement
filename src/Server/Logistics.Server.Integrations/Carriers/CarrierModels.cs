namespace Logistics.Server.Integrations.Carriers;

public record CarrierRateRequest(
    string OriginPostalCode,
    string DestinationPostalCode,
    long OriginProvinceId,
    long DestinationProvinceId,
    decimal WeightKg,
    decimal LengthCm,
    decimal WidthCm,
    decimal HeightCm,
    decimal DeclaredValue,
    decimal CodAmount
);

public record CarrierRateOption(
    string ServiceCode,
    string ServiceName,
    decimal TotalFee,
    decimal ShippingFee,
    decimal InsuranceFee,
    int EstimatedDays
);

public record CarrierRateResponse(
    bool Success,
    IReadOnlyList<CarrierRateOption> Options,
    string? ErrorMessage = null
);

public record CarrierCreateShipmentRequest(
    string ReferenceCode,
    string ServiceCode,
    string SenderName,
    string SenderPhone,
    string SenderAddress,
    string RecipientName,
    string RecipientPhone,
    string RecipientAddress,
    decimal WeightKg,
    decimal CodAmount,
    decimal DeclaredValue,
    string? Note
);

public record CarrierCreateShipmentResponse(
    bool Success,
    string? CarrierTrackingNumber,
    string? CarrierOrderCode,
    decimal? TotalFee,
    string? LabelUrl,
    string? ErrorMessage = null
);

public record CarrierTrackingEventInfo(
    string StatusCode,
    string StatusDescription,
    string? Location,
    DateTime Timestamp
);

public record CarrierTrackingInfo(
    string TrackingNumber,
    string CarrierCode,
    string CurrentStatus,
    IReadOnlyList<CarrierTrackingEventInfo> Events
);
