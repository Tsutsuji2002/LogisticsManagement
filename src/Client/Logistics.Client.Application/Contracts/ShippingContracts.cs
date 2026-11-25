namespace Logistics.Client.Application.Contracts.Shipping;

public record ShipmentDto
{
    public long Id { get; init; }
    public Guid PublicId { get; init; }
    public string TrackingNumber { get; init; } = string.Empty;
    public long? OrderId { get; init; }
    public string? OrderNumber { get; init; }
    public long CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public long OriginBranchId { get; init; }
    public string OriginBranchName { get; init; } = string.Empty;
    public long DestinationBranchId { get; init; }
    public string DestinationBranchName { get; init; } = string.Empty;
    public long? AssignedCarrierId { get; init; }
    public string? CarrierName { get; init; }
    public long? CarrierServiceId { get; init; }
    public string? CarrierServiceName { get; init; }
    public string? CarrierTrackingNumber { get; init; }
    public string ShipmentStatus { get; init; } = "Draft";
    public string ServiceType { get; init; } = string.Empty;
    public int TotalPackages { get; init; }
    public decimal ActualWeightKg { get; init; }
    public decimal ChargeableWeightKg { get; init; }
    public decimal DeclaredValue { get; init; }
    public decimal CodAmount { get; init; }
    public decimal ShippingFee { get; init; }
    public decimal InsuranceFee { get; init; }
    public decimal SurchargesTotal { get; init; }
    public decimal TotalAmount { get; init; }
    public long CurrencyId { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public DateTime? PickupDate { get; init; }
    public DateTime? EstimatedDeliveryDate { get; init; }
    public DateTime? ActualDeliveryDate { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record ShipmentDetailDto : ShipmentDto
{
    public IReadOnlyList<ShipmentPackageDto> Packages { get; init; } = [];
    public IReadOnlyList<ShipmentAddressDto> Addresses { get; init; } = [];
    public IReadOnlyList<ShipmentStatusHistoryDto> StatusHistory { get; init; } = [];
    public IReadOnlyList<ShipmentAssignmentDto> Assignments { get; init; } = [];
    public IReadOnlyList<ShipmentRouteDto> Routes { get; init; } = [];
}

public record ShipmentPackageDto
{
    public long Id { get; init; }
    public Guid PublicId { get; init; }
    public long ShipmentId { get; init; }
    public long? OrderPackageId { get; init; }
    public string PackageCode { get; init; } = string.Empty;
    public string Barcode { get; init; } = string.Empty;
    public decimal WeightKg { get; init; }
    public decimal LengthCm { get; init; }
    public decimal WidthCm { get; init; }
    public decimal HeightCm { get; init; }
    public decimal VolumetricWeightKg { get; init; }
    public string PackageStatus { get; init; } = "Created";
    public string? SealNumber { get; init; }
}

public record AddShipmentPackageRequest
{
    public long? OrderPackageId { get; init; }
    public decimal WeightKg { get; init; }
    public decimal LengthCm { get; init; }
    public decimal WidthCm { get; init; }
    public decimal HeightCm { get; init; }
    public string? SealNumber { get; init; }
}

public record ShipmentAddressDto
{
    public long Id { get; init; }
    public long ShipmentId { get; init; }
    public string AddressType { get; init; } = string.Empty;
    public string ContactName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string AddressLine1 { get; init; } = string.Empty;
    public string? AddressLine2 { get; init; }
    public long CountryId { get; init; }
    public string? CountryName { get; init; }
    public string FormattedAddress { get; init; } = string.Empty;
}

public record ShipmentStatusHistoryDto
{
    public long Id { get; init; }
    public long ShipmentId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? SubStatus { get; init; }
    public string? LocationWarehouseName { get; init; }
    public string? LocationBranchName { get; init; }
    public string? Notes { get; init; }
    public DateTime ChangedAt { get; init; }
    public string Source { get; init; } = "Internal";
}

public record ShipmentAssignmentDto
{
    public long Id { get; init; }
    public long ShipmentId { get; init; }
    public string AssignmentType { get; init; } = string.Empty;
    public long AssignedToEmployeeId { get; init; }
    public string AssignedToEmployeeName { get; init; } = string.Empty;
    public long AssignedBranchId { get; init; }
    public string AssignedBranchName { get; init; } = string.Empty;
    public long? AssignedVehicleId { get; init; }
    public string? VehicleNumber { get; init; }
    public DateTime AssignedAt { get; init; }
    public string Status { get; init; } = "Assigned";
    public DateTime? CompletedAt { get; init; }
    public string? Notes { get; init; }
}

public record CreateShipmentAssignmentRequest
{
    public string AssignmentType { get; init; } = "LastMileDelivery";
    public long AssignedToEmployeeId { get; init; }
    public long AssignedBranchId { get; init; }
    public long? AssignedVehicleId { get; init; }
    public string? Notes { get; init; }
}

public record ShipmentRouteDto
{
    public long Id { get; init; }
    public long ShipmentId { get; init; }
    public int SequenceOrder { get; init; }
    public long FromWarehouseId { get; init; }
    public string FromWarehouseName { get; init; } = string.Empty;
    public long ToWarehouseId { get; init; }
    public string ToWarehouseName { get; init; } = string.Empty;
    public string TransportType { get; init; } = string.Empty;
    public string? VehicleNumber { get; init; }
    public DateTime? EstimatedDeparture { get; init; }
    public DateTime? EstimatedArrival { get; init; }
    public string Status { get; init; } = "Planned";
}

public record AddShipmentRouteRequest
{
    public int SequenceOrder { get; init; }
    public long FromWarehouseId { get; init; }
    public long ToWarehouseId { get; init; }
    public string TransportType { get; init; } = "Road";
    public long? CarrierId { get; init; }
    public string? VehicleNumber { get; init; }
    public DateTime? EstimatedDeparture { get; init; }
    public DateTime? EstimatedArrival { get; init; }
}

public record CreateShipmentFromOrderRequest
{
    public long OrderId { get; init; }
    public long OriginBranchId { get; init; }
    public long DestinationBranchId { get; init; }
    public string ServiceType { get; init; } = "StandardGround";
    public long? AssignedCarrierId { get; init; }
    public long? CarrierServiceId { get; init; }
    public DateTime? PickupDate { get; init; }
    public DateTime? EstimatedDeliveryDate { get; init; }
    public decimal DeclaredValue { get; init; }
    public decimal CodAmount { get; init; }
    public long? CurrencyId { get; init; }
    public string? Notes { get; init; }
}

public record ShipmentStatusTransitionRequest
{
    public string TargetStatus { get; init; } = string.Empty;
    public string? SubStatus { get; init; }
    public long? LocationWarehouseId { get; init; }
    public long? LocationBranchId { get; init; }
    public string? Notes { get; init; }
    public string Source { get; init; } = "Internal";
}

public record CancelShipmentRequest
{
    public string Reason { get; init; } = string.Empty;
}

public record PickupRequestDto
{
    public long Id { get; init; }
    public Guid PublicId { get; init; }
    public string PickupCode { get; init; } = string.Empty;
    public long CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public long RequestedAddressId { get; init; }
    public string FormattedAddress { get; init; } = string.Empty;
    public DateOnly ScheduledDate { get; init; }
    public string TimeSlot { get; init; } = string.Empty;
    public string Status { get; init; } = "Pending";
    public long? AssignedEmployeeId { get; init; }
    public string? AssignedEmployeeName { get; init; }
    public string ContactName { get; init; } = string.Empty;
    public string ContactPhone { get; init; } = string.Empty;
    public int EstimatedParcels { get; init; }
    public int ActualParcelsPickedUp { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreatePickupRequestRequest
{
    public long CustomerId { get; init; }
    public long RequestedAddressId { get; init; }
    public DateOnly ScheduledDate { get; init; }
    public string TimeSlot { get; init; } = string.Empty;
    public string ContactName { get; init; } = string.Empty;
    public string ContactPhone { get; init; } = string.Empty;
    public int EstimatedParcels { get; init; }
    public string? Notes { get; init; }
}

public record CompletePickupRequest
{
    public int ActualParcelsPickedUp { get; init; }
    public string? Notes { get; init; }
}

public record DeliveryAttemptDto
{
    public long Id { get; init; }
    public long ShipmentId { get; init; }
    public string TrackingNumber { get; init; } = string.Empty;
    public int AttemptNumber { get; init; }
    public long AttemptedByEmployeeId { get; init; }
    public string AttemptedByEmployeeName { get; init; } = string.Empty;
    public DateTime AttemptedAt { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? FailureReasonCode { get; init; }
    public string? FailureNotes { get; init; }
    public DateOnly? NextScheduledAttemptDate { get; init; }
    public bool RecipientContacted { get; init; }
    public ProofOfDeliveryDto? ProofOfDelivery { get; init; }
}

public record RecordDeliveryAttemptRequest
{
    public long AttemptedByEmployeeId { get; init; }
    public DateTime AttemptedAt { get; init; }
    public string Status { get; init; } = "Delivered";
    public string? FailureReasonCode { get; init; }
    public string? FailureNotes { get; init; }
    public DateOnly? NextScheduledAttemptDate { get; init; }
    public bool RecipientContacted { get; init; }
}

public record ProofOfDeliveryDto
{
    public long Id { get; init; }
    public long ShipmentId { get; init; }
    public long DeliveryAttemptId { get; init; }
    public string RecipientName { get; init; } = string.Empty;
    public string RecipientRelation { get; init; } = string.Empty;
    public string? RecipientNationalId { get; init; }
    public DateTime DeliveredAt { get; init; }
    public long? SignatureAttachmentId { get; init; }
    public long? PhotoAttachmentId { get; init; }
    public string? Notes { get; init; }
    public string? VerifiedByUserId { get; init; }
    public DateTime? VerifiedAt { get; init; }
}

public record CreateProofOfDeliveryRequest
{
    public string RecipientName { get; init; } = string.Empty;
    public string RecipientRelation { get; init; } = "Self";
    public string? RecipientNationalId { get; init; }
    public DateTime DeliveredAt { get; init; }
    public long? SignatureAttachmentId { get; init; }
    public long? PhotoAttachmentId { get; init; }
    public string? Notes { get; init; }
}

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
    public string Status { get; init; } = "Requested";
    public DateTime RequestedAt { get; init; }
    public string? ApprovedByUserId { get; init; }
    public DateTime? ApprovedAt { get; init; }
    public string? Notes { get; init; }
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
    public string Decision { get; init; } = "Approved";
    public string? Notes { get; init; }
}
