namespace Logistics.Server.Application.Modules.Warehouse.DTOs;

// ───────────── Parcel Scanning & Operations ─────────────

public class ScanPackageRequest
{
    public string TrackingNumberOrBarcode { get; set; } = string.Empty;
    public long WarehouseId { get; set; }
    public long EmployeeId { get; set; }
    public string? DeviceId { get; set; }
}

public class ScanPackageResultDto
{
    public long PackageId { get; set; }
    public string PackageCode { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string PackageStatus { get; set; } = string.Empty;
    public long ShipmentId { get; set; }
    public string ShipmentTrackingNumber { get; set; } = string.Empty;
    public string ShipmentStatus { get; set; } = string.Empty;
    public long OriginBranchId { get; set; }
    public long DestinationBranchId { get; set; }
    public decimal WeightKg { get; set; }
    public string PermittedNextOperation { get; set; } = string.Empty; // Receive, Sort, Stage, Dispatch, None
    public string? Message { get; set; }
}

public class ParcelReceiveRequest
{
    public string TrackingNumberOrBarcode { get; set; } = string.Empty;
    public long WarehouseId { get; set; }
    public long EmployeeId { get; set; }
    public string? Notes { get; set; }
    public string? DeviceId { get; set; }
}

public class ParcelSortRequest
{
    public string TrackingNumberOrBarcode { get; set; } = string.Empty;
    public long WarehouseId { get; set; }
    public long ZoneId { get; set; }
    public long EmployeeId { get; set; }
    public string? TargetContainerCode { get; set; }
    public string? Notes { get; set; }
}

public class ParcelStageRequest
{
    public string TrackingNumberOrBarcode { get; set; } = string.Empty;
    public long WarehouseId { get; set; }
    public long LocationId { get; set; }
    public long EmployeeId { get; set; }
    public string? BagNumber { get; set; }
    public string? Notes { get; set; }
}

public class ParcelExceptionRequest
{
    public string TrackingNumberOrBarcode { get; set; } = string.Empty;
    public long WarehouseId { get; set; }
    public long EmployeeId { get; set; }
    public string ExceptionType { get; set; } = string.Empty; // Damaged, Missing, IncorrectDestination, OperationalHold
    public string Reason { get; set; } = string.Empty;
    public string? DeviceId { get; set; }
}

// ───────────── Parcel Handling Events ─────────────

public class ParcelHandlingEventDto
{
    public long Id { get; set; }
    public long ShipmentPackageId { get; set; }
    public string PackageCode { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public long ShipmentId { get; set; }
    public string ShipmentTrackingNumber { get; set; } = string.Empty;
    public long WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty; // Unloaded, ScannedAtHub, Sorted, Bagged, Staged, LoadedToVehicle, Dispatched, ReceivedAtDeliveryBranch
    public string? BagNumber { get; set; }
    public long? NextDestinationWarehouseId { get; set; }
    public long EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public DateTime ScannedAt { get; set; }
    public string? DeviceId { get; set; }
    public string? Notes { get; set; }
}

// ───────────── Warehouse Dispatches ─────────────

public class WarehouseDispatchDto
{
    public long Id { get; set; }
    public Guid PublicId { get; set; }
    public string DispatchNumber { get; set; } = string.Empty;
    public long WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string DispatchType { get; set; } = string.Empty; // InventoryOutbound, HubTransfer, CourierDispatch, ReturnOutbound
    public long? DestinationWarehouseId { get; set; }
    public string? DestinationWarehouseCode { get; set; }
    public long? DestinationBranchId { get; set; }
    public long? AssignedCarrierId { get; set; }
    public string? AssignedCarrierCode { get; set; }
    public string Status { get; set; } = string.Empty; // Draft, Picking, Packed, Staged, Dispatched, Cancelled
    public long DispatchedByEmployeeId { get; set; }
    public string? DispatchedByEmployeeName { get; set; }
    public DateTime? DispatchedAt { get; set; }
    public string? VehicleNumber { get; set; }
    public string? Notes { get; set; }
    public IReadOnlyList<WarehouseDispatchItemDto> Items { get; set; } = [];
    public IReadOnlyList<WarehouseDispatchPackageDto> Packages { get; set; } = [];
}

public class WarehouseDispatchItemDto
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int RequestedQuantity { get; set; }
    public int PickedQuantity { get; set; }
    public long UnitOfMeasureId { get; set; }
    public long SourceLocationId { get; set; }
    public string SourceLocationCode { get; set; } = string.Empty;
    public string? LotNumber { get; set; }
    public string? Notes { get; set; }
}

public class WarehouseDispatchPackageDto
{
    public long Id { get; set; }
    public long ShipmentPackageId { get; set; }
    public string PackageCode { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public DateTime ScannedAt { get; set; }
    public long ScannedByEmployeeId { get; set; }
    public string? ScannedByEmployeeName { get; set; }
    public string? TargetContainerCode { get; set; }
}

public class CreateWarehouseDispatchDto
{
    public long WarehouseId { get; set; }
    public string DispatchType { get; set; } = "HubTransfer";
    public long? DestinationWarehouseId { get; set; }
    public long? DestinationBranchId { get; set; }
    public long? AssignedCarrierId { get; set; }
    public long DispatchedByEmployeeId { get; set; }
    public string? VehicleNumber { get; set; }
    public string? Notes { get; set; }
    public List<CreateWarehouseDispatchItemRequest>? Items { get; set; }
    public List<CreateWarehouseDispatchPackageRequest>? Packages { get; set; }
}

public class CreateWarehouseDispatchItemRequest
{
    public long ProductId { get; set; }
    public int RequestedQuantity { get; set; }
    public int PickedQuantity { get; set; }
    public long UnitOfMeasureId { get; set; }
    public long SourceLocationId { get; set; }
    public string? LotNumber { get; set; }
    public string? Notes { get; set; }
}

public class CreateWarehouseDispatchPackageRequest
{
    public long ShipmentPackageId { get; set; }
    public string? TargetContainerCode { get; set; }
}
