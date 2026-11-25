namespace Logistics.Client.Application.Contracts.Warehouse;

public class WarehouseDto
{
    public long Id { get; set; }
    public Guid PublicId { get; set; }
    public long BranchId { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal TotalAreaSqM { get; set; }
    public long? AddressId { get; set; }
    public long? ManagerEmployeeId { get; set; }
    public string? ManagerEmployeeName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ZoneCount { get; set; }
    public int LocationCount { get; set; }
}

public class WarehouseDetailDto : WarehouseDto
{
    public IReadOnlyList<WarehouseZoneDto> Zones { get; set; } = [];
}

public class CreateWarehouseRequest
{
    public long BranchId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Hybrid";
    public decimal TotalAreaSqM { get; set; }
    public long? ManagerEmployeeId { get; set; }
}

public class UpdateWarehouseRequest
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Hybrid";
    public decimal TotalAreaSqM { get; set; }
    public long? ManagerEmployeeId { get; set; }
}

public class WarehouseZoneDto
{
    public long Id { get; set; }
    public Guid PublicId { get; set; }
    public long WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ZoneType { get; set; } = string.Empty;
    public string TemperatureType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int LocationCount { get; set; }
}

public class CreateWarehouseZoneRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ZoneType { get; set; } = "BulkStorage";
    public string TemperatureType { get; set; } = "Ambient";
}

public class WarehouseLocationDto
{
    public long Id { get; set; }
    public Guid PublicId { get; set; }
    public long WarehouseZoneId { get; set; }
    public string ZoneCode { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Aisle { get; set; }
    public string? Rack { get; set; }
    public string? Shelf { get; set; }
    public string? Bin { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public decimal MaxWeightKg { get; set; }
    public decimal MaxVolumeCbm { get; set; }
    public bool IsOccupied { get; set; }
    public bool IsBlocked { get; set; }
}

public class CreateWarehouseLocationRequest
{
    public string Code { get; set; } = string.Empty;
    public string? Aisle { get; set; }
    public string? Rack { get; set; }
    public string? Shelf { get; set; }
    public string? Bin { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public decimal MaxWeightKg { get; set; } = 1000m;
    public decimal MaxVolumeCbm { get; set; } = 5m;
}

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
    public decimal WeightKg { get; set; }
    public string PermittedNextOperation { get; set; } = string.Empty;
    public string? Message { get; set; }
}

public class ParcelReceiveRequest
{
    public string TrackingNumberOrBarcode { get; set; } = string.Empty;
    public long WarehouseId { get; set; }
    public long EmployeeId { get; set; }
    public string? Notes { get; set; }
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
    public string ExceptionType { get; set; } = "Damaged";
    public string Reason { get; set; } = string.Empty;
}

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
    public string EventType { get; set; } = string.Empty;
    public string? BagNumber { get; set; }
    public string? EmployeeName { get; set; }
    public DateTime ScannedAt { get; set; }
    public string? Notes { get; set; }
}

public class WarehouseDispatchDto
{
    public long Id { get; set; }
    public Guid PublicId { get; set; }
    public string DispatchNumber { get; set; } = string.Empty;
    public long WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string DispatchType { get; set; } = string.Empty;
    public long? DestinationWarehouseId { get; set; }
    public string? DestinationWarehouseCode { get; set; }
    public long? DestinationBranchId { get; set; }
    public long? AssignedCarrierId { get; set; }
    public string? AssignedCarrierCode { get; set; }
    public string Status { get; set; } = string.Empty;
    public long DispatchedByEmployeeId { get; set; }
    public string? DispatchedByEmployeeName { get; set; }
    public DateTime? DispatchedAt { get; set; }
    public string? VehicleNumber { get; set; }
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
}

public class InventoryItemDto
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? ProductSku { get; set; }
    public long WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public int ReorderPoint { get; set; }
    public int SafetyStock { get; set; }
    public int TargetStock { get; set; }
    public int MinimumOrderQuantity { get; set; }
    public int MaximumStock { get; set; }
    public bool IsActive { get; set; }
}

public class CreateInventoryItemRequest
{
    public long ProductId { get; set; }
    public long WarehouseId { get; set; }
    public int ReorderPoint { get; set; } = 10;
    public int SafetyStock { get; set; } = 5;
    public int TargetStock { get; set; } = 100;
    public int MinimumOrderQuantity { get; set; } = 1;
    public int MaximumStock { get; set; } = 500;
}

public class InventoryBalanceDto
{
    public long Id { get; set; }
    public long WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public long WarehouseLocationId { get; set; }
    public string LocationCode { get; set; } = string.Empty;
    public long ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public long? LotId { get; set; }
    public string? LotNumber { get; set; }
    public int QuantityOnHand { get; set; }
    public int QuantityAllocated { get; set; }
    public int QuantityAvailable { get; set; }
    public int QuantityDamaged { get; set; }
    public DateTime? LastCountDate { get; set; }
}

public class InventoryTransactionDto
{
    public long Id { get; set; }
    public string TransactionNumber { get; set; } = string.Empty;
    public long WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty;
    public string ReferenceType { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
    public string? PerformedByEmployeeName { get; set; }
    public DateTime TransactionDate { get; set; }
    public string? Notes { get; set; }
}

public class WarehouseReceiptDto
{
    public long Id { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public long WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string ReceiptType { get; set; } = string.Empty;
    public string? SourceVendor { get; set; }
    public DateTime ActualArrivalDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public long ReceivedByEmployeeId { get; set; }
    public string? ReceivedByEmployeeName { get; set; }
    public List<WarehouseReceiptItemDto> Items { get; set; } = [];
}

public class WarehouseReceiptItemDto
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int ExpectedQuantity { get; set; }
    public int ReceivedQuantity { get; set; }
    public int DamagedQuantity { get; set; }
    public long UnitOfMeasureId { get; set; }
    public long? TargetLocationId { get; set; }
}

public class CreateWarehouseReceiptRequest
{
    public long WarehouseId { get; set; }
    public string ReceiptType { get; set; } = "InventoryInbound";
    public string? SourceVendor { get; set; }
    public long ReceivedByEmployeeId { get; set; }
    public string? Notes { get; set; }
    public List<CreateWarehouseReceiptItemRequest> Items { get; set; } = [];
}

public class CreateWarehouseReceiptItemRequest
{
    public long ProductId { get; set; }
    public int ExpectedQuantity { get; set; }
    public long UnitOfMeasureId { get; set; }
    public long? TargetLocationId { get; set; }
    public string? LotNumber { get; set; }
}

public class ConfirmReceiptItemRequest
{
    public long ItemId { get; set; }
    public int ReceivedQuantity { get; set; }
    public int DamagedQuantity { get; set; }
    public long TargetLocationId { get; set; }
    public string? LotNumber { get; set; }
    public decimal UnitCost { get; set; }
}

public class ConfirmReceiptRequest
{
    public List<ConfirmReceiptItemRequest>? Items { get; set; }
}

public class StockAdjustmentDto
{
    public long Id { get; set; }
    public string AdjustmentNumber { get; set; } = string.Empty;
    public long WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string ReasonCode { get; set; } = string.Empty;
    public string ReasonDescription { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ApprovedByEmployeeName { get; set; }
    public DateTime? ApprovedAt { get; set; }
}

public class CreateStockAdjustmentRequest
{
    public long WarehouseId { get; set; }
    public string ReasonCode { get; set; } = "CountingVariance";
    public string ReasonDescription { get; set; } = string.Empty;
    public long ProductId { get; set; }
    public long LocationId { get; set; }
    public long? LotId { get; set; }
    public int QuantityChange { get; set; }
    public decimal UnitCost { get; set; }
    public string? Notes { get; set; }
}

public class StockTransferDto
{
    public long Id { get; set; }
    public string TransferNumber { get; set; } = string.Empty;
    public long SourceWarehouseId { get; set; }
    public string SourceWarehouseCode { get; set; } = string.Empty;
    public long DestinationWarehouseId { get; set; }
    public string DestinationWarehouseCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? ShippedAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
}

public class CreateStockTransferRequest
{
    public long SourceWarehouseId { get; set; }
    public long DestinationWarehouseId { get; set; }
    public long RequestedByEmployeeId { get; set; }
    public long ProductId { get; set; }
    public long SourceLocationId { get; set; }
    public int Quantity { get; set; }
    public string? Notes { get; set; }
}

public class ReceiveStockTransferRequest
{
    public long DestinationLocationId { get; set; }
    public string? Notes { get; set; }
}

public class InventoryReservationDto
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string WarehouseCode { get; set; } = string.Empty;
    public string LocationCode { get; set; } = string.Empty;
    public long OrderId { get; set; }
    public int ReservedQuantity { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ReservedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public class CreateInventoryReservationRequest
{
    public long ProductId { get; set; }
    public long WarehouseId { get; set; }
    public long LocationId { get; set; }
    public long OrderId { get; set; }
    public int ReservedQuantity { get; set; }
    public int DurationMinutes { get; set; } = 1440;
}

public class InventoryLotDto
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string LotNumber { get; set; } = string.Empty;
    public DateOnly? ManufactureDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public string? VendorBatchCode { get; set; }
}

public class CreateInventoryLotRequest
{
    public long ProductId { get; set; }
    public string LotNumber { get; set; } = string.Empty;
    public DateOnly? ManufactureDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public string? VendorBatchCode { get; set; }
}

public class InventorySerialNumberDto
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string WarehouseCode { get; set; } = string.Empty;
    public string? LocationCode { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; }
}

public class CreateInventorySerialNumberRequest
{
    public long ProductId { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public long WarehouseId { get; set; }
    public long? LocationId { get; set; }
}
