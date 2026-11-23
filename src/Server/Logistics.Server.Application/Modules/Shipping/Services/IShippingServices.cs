using Logistics.Server.Application.Common.Models;
using Logistics.Server.Application.Modules.Shipping.DTOs;
using Logistics.Shared.Contracts.Pagination;

namespace Logistics.Server.Application.Modules.Shipping.Services;

/// <summary>
/// Core shipment CRUD, status management, and order-linked lookup.
/// </summary>
public interface IShipmentService
{
    Task<PagedResult<ShipmentDto>> GetShipmentsAsync(
        string? search,
        string? shipmentStatus,
        string? serviceType,
        long? customerId,
        long? originBranchId,
        long? destinationBranchId,
        long? carrierId,
        DateTime? dateFrom,
        DateTime? dateTo,
        PagedRequest request,
        CancellationToken cancellationToken = default);

    Task<ShipmentDetailDto?> GetByIdOrTrackingAsync(string identifier, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShipmentDto>> GetByOrderIdAsync(long orderId, CancellationToken cancellationToken = default);

    Task<ShipmentDetailDto> CreateFromOrderAsync(CreateShipmentFromOrderRequest request, CancellationToken cancellationToken = default);
    Task<ShipmentDetailDto> UpdateAsync(string identifier, UpdateShipmentRequest request, CancellationToken cancellationToken = default);
    Task<ShipmentDetailDto> CancelAsync(string identifier, CancelShipmentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Transitions the shipment status according to the valid state machine:
    /// Draft → ReadyForPickup | Cancelled
    /// ReadyForPickup → PickedUp | Cancelled
    /// PickedUp → InSortingHub | InTransit | Cancelled
    /// InSortingHub → InTransit
    /// InTransit → OutForDelivery | InSortingHub (re-sort at hub)
    /// OutForDelivery → Delivered | DeliveryFailed
    /// DeliveryFailed → OutForDelivery | Returning | Cancelled
    /// Returning → Returned
    /// Delivered, Returned, Cancelled are terminal states.
    /// Throws <see cref="Logistics.Server.Application.Common.Exceptions.ValidationException"/> for invalid transitions.
    /// </summary>
    Task<ShipmentDetailDto> TransitionStatusAsync(string identifier, ShipmentStatusTransitionRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ShipmentStatusHistoryDto>> GetStatusHistoryAsync(string identifier, CancellationToken cancellationToken = default);
}

/// <summary>
/// Manages physical packages belonging to a shipment.
/// </summary>
public interface IShipmentPackageService
{
    Task<IReadOnlyList<ShipmentPackageDto>> GetPackagesAsync(string shipmentIdentifier, CancellationToken cancellationToken = default);
    Task<ShipmentPackageDto?> GetPackageByIdAsync(string shipmentIdentifier, string packageIdentifier, CancellationToken cancellationToken = default);
    Task<ShipmentPackageDto> AddPackageAsync(string shipmentIdentifier, AddShipmentPackageRequest request, CancellationToken cancellationToken = default);
    Task<ShipmentPackageDto> UpdatePackageAsync(string shipmentIdentifier, string packageIdentifier, UpdateShipmentPackageRequest request, CancellationToken cancellationToken = default);
    Task RemovePackageAsync(string shipmentIdentifier, string packageIdentifier, CancellationToken cancellationToken = default);
}

/// <summary>
/// Manages sender, recipient, and return addresses for a shipment.
/// </summary>
public interface IShipmentAddressService
{
    Task<IReadOnlyList<ShipmentAddressDto>> GetAddressesAsync(string shipmentIdentifier, CancellationToken cancellationToken = default);
    Task<ShipmentAddressDto?> GetAddressByTypeAsync(string shipmentIdentifier, string addressType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or replaces the address for the given <paramref name="addressType"/> ("Sender", "Recipient", "ReturnAddress").
    /// </summary>
    Task<ShipmentAddressDto> SetAddressAsync(string shipmentIdentifier, string addressType, UpdateShipmentAddressRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Manages the full pickup-request lifecycle: create → assign driver → arrive → complete/cancel.
/// </summary>
public interface IPickupRequestService
{
    Task<PagedResult<PickupRequestDto>> GetPickupRequestsAsync(
        string? search,
        string? status,
        long? customerId,
        long? assignedEmployeeId,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        PagedRequest request,
        CancellationToken cancellationToken = default);

    Task<PickupRequestDto?> GetByIdOrCodeAsync(string identifier, CancellationToken cancellationToken = default);
    Task<PickupRequestDto> CreateAsync(CreatePickupRequestRequest request, CancellationToken cancellationToken = default);
    Task<PickupRequestDto> UpdateAsync(string identifier, UpdatePickupRequestRequest request, CancellationToken cancellationToken = default);
    Task<PickupRequestDto> AssignDriverAsync(string identifier, long employeeId, CancellationToken cancellationToken = default);
    Task<PickupRequestDto> MarkDriverArrivedAsync(string identifier, CancellationToken cancellationToken = default);
    Task<PickupRequestDto> CompleteAsync(string identifier, CompletePickupRequest request, CancellationToken cancellationToken = default);
    Task<PickupRequestDto> CancelAsync(string identifier, string reason, CancellationToken cancellationToken = default);
}

/// <summary>
/// Creates and tracks courier/linehaul/last-mile assignment records for a shipment.
/// </summary>
public interface IShipmentAssignmentService
{
    Task<IReadOnlyList<ShipmentAssignmentDto>> GetAssignmentsAsync(string shipmentIdentifier, CancellationToken cancellationToken = default);
    Task<ShipmentAssignmentDto?> GetAssignmentByIdAsync(string shipmentIdentifier, long assignmentId, CancellationToken cancellationToken = default);
    Task<ShipmentAssignmentDto> CreateAssignmentAsync(string shipmentIdentifier, CreateShipmentAssignmentRequest request, CancellationToken cancellationToken = default);
    Task<ShipmentAssignmentDto> UpdateAssignmentAsync(string shipmentIdentifier, long assignmentId, UpdateShipmentAssignmentRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Records each physical delivery attempt for a shipment.
/// </summary>
public interface IDeliveryAttemptService
{
    Task<IReadOnlyList<DeliveryAttemptDto>> GetAttemptsAsync(string shipmentIdentifier, CancellationToken cancellationToken = default);
    Task<DeliveryAttemptDto?> GetAttemptByIdAsync(string shipmentIdentifier, long attemptId, CancellationToken cancellationToken = default);
    Task<DeliveryAttemptDto> RecordAttemptAsync(string shipmentIdentifier, RecordDeliveryAttemptRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Manages proof-of-delivery records (signature, photo, recipient info).
/// </summary>
public interface IProofOfDeliveryService
{
    Task<ProofOfDeliveryDto?> GetByShipmentAsync(string shipmentIdentifier, CancellationToken cancellationToken = default);
    Task<ProofOfDeliveryDto?> GetByAttemptIdAsync(string shipmentIdentifier, long attemptId, CancellationToken cancellationToken = default);
    Task<ProofOfDeliveryDto> CreateAsync(string shipmentIdentifier, long attemptId, CreateProofOfDeliveryRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Manages the return-request lifecycle: request → approve/reject → track return shipment.
/// </summary>
public interface IReturnRequestService
{
    Task<PagedResult<ReturnRequestDto>> GetReturnRequestsAsync(
        string? search,
        string? status,
        long? customerId,
        DateTime? dateFrom,
        DateTime? dateTo,
        PagedRequest request,
        CancellationToken cancellationToken = default);

    Task<ReturnRequestDto?> GetByIdOrCodeAsync(string identifier, CancellationToken cancellationToken = default);
    Task<ReturnRequestDto> CreateAsync(CreateReturnRequestRequest request, CancellationToken cancellationToken = default);
    Task<ReturnRequestDto> ProcessAsync(string identifier, ProcessReturnRequestRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Manages the ordered leg-by-leg routing plan for a shipment across warehouses.
/// </summary>
public interface IShipmentRouteService
{
    Task<IReadOnlyList<ShipmentRouteDto>> GetRoutesAsync(string shipmentIdentifier, CancellationToken cancellationToken = default);
    Task<ShipmentRouteDto?> GetRouteByIdAsync(string shipmentIdentifier, long routeId, CancellationToken cancellationToken = default);
    Task<ShipmentRouteDto> AddRouteAsync(string shipmentIdentifier, AddShipmentRouteRequest request, CancellationToken cancellationToken = default);
    Task<ShipmentRouteDto> UpdateRouteAsync(string shipmentIdentifier, long routeId, UpdateShipmentRouteRequest request, CancellationToken cancellationToken = default);
    Task RemoveRouteAsync(string shipmentIdentifier, long routeId, CancellationToken cancellationToken = default);
}
