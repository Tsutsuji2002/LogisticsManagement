namespace Logistics.Server.Application.Modules.Shipping.DTOs;

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
    public string? CreatedBy { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? UpdatedBy { get; init; }
}

public record ShipmentDetailDto : ShipmentDto
{
    public IReadOnlyList<ShipmentPackageDto> Packages { get; init; } = [];
    public IReadOnlyList<ShipmentAddressDto> Addresses { get; init; } = [];
    public IReadOnlyList<ShipmentStatusHistoryDto> StatusHistory { get; init; } = [];
    public IReadOnlyList<ShipmentAssignmentDto> Assignments { get; init; } = [];
    public IReadOnlyList<ShipmentRouteDto> Routes { get; init; } = [];

    public ShipmentAddressDto? SenderAddress =>
        Addresses.FirstOrDefault(a => a.AddressType.Equals("Sender", StringComparison.OrdinalIgnoreCase));
    public ShipmentAddressDto? RecipientAddress =>
        Addresses.FirstOrDefault(a => a.AddressType.Equals("Recipient", StringComparison.OrdinalIgnoreCase));
    public ShipmentAddressDto? ReturnAddress =>
        Addresses.FirstOrDefault(a => a.AddressType.Equals("ReturnAddress", StringComparison.OrdinalIgnoreCase));
}

public record ShipmentStatusHistoryDto
{
    public long Id { get; init; }
    public long ShipmentId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? SubStatus { get; init; }
    public long? LocationWarehouseId { get; init; }
    public string? LocationWarehouseName { get; init; }
    public long? LocationBranchId { get; init; }
    public string? LocationBranchName { get; init; }
    public string? Notes { get; init; }
    public string? ChangedByUserId { get; init; }
    public long? ChangedByEmployeeId { get; init; }
    public DateTime ChangedAt { get; init; }
    public string Source { get; init; } = "Internal";
    public DateTime CreatedAt { get; init; }
}

public record CreateShipmentFromOrderRequest
{
    public long OrderId { get; init; }
    public long OriginBranchId { get; init; }
    public long DestinationBranchId { get; init; }
    public string ServiceType { get; init; } = string.Empty;
    public long? AssignedCarrierId { get; init; }
    public long? CarrierServiceId { get; init; }
    public DateTime? PickupDate { get; init; }
    public DateTime? EstimatedDeliveryDate { get; init; }
    public decimal DeclaredValue { get; init; }
    public decimal CodAmount { get; init; }
    public long? CurrencyId { get; init; }
    public string? Notes { get; init; }
}

public record UpdateShipmentRequest
{
    public long? AssignedCarrierId { get; init; }
    public long? CarrierServiceId { get; init; }
    public string? CarrierTrackingNumber { get; init; }
    public string ServiceType { get; init; } = string.Empty;
    public DateTime? PickupDate { get; init; }
    public DateTime? EstimatedDeliveryDate { get; init; }
    public decimal DeclaredValue { get; init; }
    public decimal CodAmount { get; init; }
    public decimal ShippingFee { get; init; }
    public decimal InsuranceFee { get; init; }
    public decimal SurchargesTotal { get; init; }
    public string? Notes { get; init; }
}

public record CancelShipmentRequest
{
    public string Reason { get; init; } = string.Empty;
}

public record ShipmentStatusTransitionRequest
{
    /// <summary>
    /// Target status. Valid transitions:
    /// Draft → ReadyForPickup | Cancelled
    /// ReadyForPickup → PickedUp | Cancelled
    /// PickedUp → InSortingHub | InTransit | Cancelled
    /// InSortingHub → InTransit
    /// InTransit → OutForDelivery | InSortingHub (re-sort at hub)
    /// OutForDelivery → Delivered | DeliveryFailed
    /// DeliveryFailed → OutForDelivery | Returning | Cancelled
    /// Returning → Returned
    /// Delivered, Returned, Cancelled are terminal states.
    /// </summary>
    public string TargetStatus { get; init; } = string.Empty;
    public string? SubStatus { get; init; }
    public long? LocationWarehouseId { get; init; }
    public long? LocationBranchId { get; init; }
    public string? Notes { get; init; }
    public string Source { get; init; } = "Internal";
}
