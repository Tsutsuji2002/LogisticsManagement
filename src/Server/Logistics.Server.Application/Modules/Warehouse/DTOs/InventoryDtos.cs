namespace Logistics.Server.Application.Modules.Warehouse.DTOs;

// ───────────── Inventory Items ─────────────

public class InventoryItemDto
{
    public long Id { get; set; }
    public Guid PublicId { get; set; }
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

public class UpdateInventoryItemRequest
{
    public int ReorderPoint { get; set; }
    public int SafetyStock { get; set; }
    public int TargetStock { get; set; }
    public int MinimumOrderQuantity { get; set; }
    public int MaximumStock { get; set; }
    public bool IsActive { get; set; }
}

// ───────────── Inventory Balances ─────────────

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

// ───────────── Inventory Transactions ─────────────

public class InventoryTransactionDto
{
    public long Id { get; set; }
    public Guid PublicId { get; set; }
    public string TransactionNumber { get; set; } = string.Empty;
    public long WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty; // Receipt, Dispatch, TransferIn, TransferOut, PositiveAdjustment, NegativeAdjustment, CycleCount
    public string ReferenceType { get; set; } = string.Empty; // WarehouseReceipt, WarehouseDispatch, StockTransfer, StockAdjustment
    public long ReferenceId { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public long PerformedByEmployeeId { get; set; }
    public string? PerformedByEmployeeName { get; set; }
    public DateTime TransactionDate { get; set; }
    public string? Notes { get; set; }
    public IReadOnlyList<InventoryTransactionItemDto> Items { get; set; } = [];
}

public class InventoryTransactionItemDto
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public long LocationId { get; set; }
    public string LocationCode { get; set; } = string.Empty;
    public long? LotId { get; set; }
    public string? LotNumber { get; set; }
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public int BalanceBefore { get; set; }
    public int BalanceAfter { get; set; }
}

// ───────────── Warehouse Receipts ─────────────

public class WarehouseReceiptDto
{
    public long Id { get; set; }
    public Guid PublicId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public long WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string ReceiptType { get; set; } = string.Empty; // InventoryInbound, CustomerReturn, TransferIn
    public long? SourceBranchId { get; set; }
    public string? SourceVendor { get; set; }
    public DateTime? ExpectedArrivalDate { get; set; }
    public DateTime ActualArrivalDate { get; set; }
    public string Status { get; set; } = string.Empty; // Draft, Pending, Receiving, Completed, Cancelled
    public long ReceivedByEmployeeId { get; set; }
    public string? ReceivedByEmployeeName { get; set; }
    public string? Notes { get; set; }
    public IReadOnlyList<WarehouseReceiptItemDto> Items { get; set; } = [];
}

public class WarehouseReceiptItemDto
{
    public long Id { get; set; }
    public long WarehouseReceiptId { get; set; }
    public long ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int ExpectedQuantity { get; set; }
    public int ReceivedQuantity { get; set; }
    public int DamagedQuantity { get; set; }
    public long UnitOfMeasureId { get; set; }
    public string? UnitName { get; set; }
    public long? TargetLocationId { get; set; }
    public string? TargetLocationCode { get; set; }
    public string? LotNumber { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public string? Notes { get; set; }
}

public class CreateWarehouseReceiptRequest
{
    public long WarehouseId { get; set; }
    public string ReceiptType { get; set; } = "InventoryInbound";
    public long? SourceBranchId { get; set; }
    public string? SourceVendor { get; set; }
    public DateTime? ExpectedArrivalDate { get; set; }
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
    public DateOnly? ExpiryDate { get; set; }
    public string? Notes { get; set; }
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

// ───────────── Stock Adjustments ─────────────

public class StockAdjustmentDto
{
    public long Id { get; set; }
    public Guid PublicId { get; set; }
    public string AdjustmentNumber { get; set; } = string.Empty;
    public long WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string ReasonCode { get; set; } = string.Empty; // Damage, Shrinkage, CountingVariance, Expired, FoundGoods
    public string ReasonDescription { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // Draft, Approved, Rejected, Applied
    public long? ApprovedByEmployeeId { get; set; }
    public string? ApprovedByEmployeeName { get; set; }
    public DateTime? ApprovedAt { get; set; }
}

public class CreateStockAdjustmentRequest
{
    public long WarehouseId { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public string ReasonDescription { get; set; } = string.Empty;
    public long ProductId { get; set; }
    public long LocationId { get; set; }
    public long? LotId { get; set; }
    public int QuantityChange { get; set; } // Positive (found) or negative (shrinkage/damage)
    public decimal UnitCost { get; set; }
    public string? Notes { get; set; }
}

// ───────────── Stock Transfers ─────────────

public class StockTransferDto
{
    public long Id { get; set; }
    public Guid PublicId { get; set; }
    public string TransferNumber { get; set; } = string.Empty;
    public long SourceWarehouseId { get; set; }
    public string SourceWarehouseCode { get; set; } = string.Empty;
    public long DestinationWarehouseId { get; set; }
    public string DestinationWarehouseCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // Draft, Approved, InTransit, Received, Cancelled
    public long RequestedByEmployeeId { get; set; }
    public string? RequestedByEmployeeName { get; set; }
    public long? ApprovedByEmployeeId { get; set; }
    public string? ApprovedByEmployeeName { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public string? Notes { get; set; }
}

public class CreateStockTransferRequest
{
    public long SourceWarehouseId { get; set; }
    public long DestinationWarehouseId { get; set; }
    public long RequestedByEmployeeId { get; set; }
    public long ProductId { get; set; }
    public long SourceLocationId { get; set; }
    public long? LotId { get; set; }
    public int Quantity { get; set; }
    public string? Notes { get; set; }
}

public class ReceiveStockTransferRequest
{
    public long DestinationLocationId { get; set; }
    public string? Notes { get; set; }
}

// ───────────── Inventory Reservations ─────────────

public class InventoryReservationDto
{
    public long Id { get; set; }
    public Guid PublicId { get; set; }
    public long ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public long WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public long LocationId { get; set; }
    public string LocationCode { get; set; } = string.Empty;
    public long? LotId { get; set; }
    public string? LotNumber { get; set; }
    public long OrderId { get; set; }
    public int ReservedQuantity { get; set; }
    public string Status { get; set; } = string.Empty; // Active, Released, Fulfilled, Expired
    public DateTime ReservedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? FulfilledAt { get; set; }
}

public class CreateInventoryReservationRequest
{
    public long ProductId { get; set; }
    public long WarehouseId { get; set; }
    public long LocationId { get; set; }
    public long? LotId { get; set; }
    public long OrderId { get; set; }
    public int ReservedQuantity { get; set; }
    public int DurationMinutes { get; set; } = 1440; // Default 24 hours
}

// ───────────── Lots & Serial Numbers ─────────────

public class InventoryLotDto
{
    public long Id { get; set; }
    public Guid PublicId { get; set; }
    public long ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string LotNumber { get; set; } = string.Empty;
    public DateOnly? ManufactureDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public string? VendorBatchCode { get; set; }
    public string? Notes { get; set; }
}

public class CreateInventoryLotRequest
{
    public long ProductId { get; set; }
    public string LotNumber { get; set; } = string.Empty;
    public DateOnly? ManufactureDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public string? VendorBatchCode { get; set; }
    public string? Notes { get; set; }
}

public class InventorySerialNumberDto
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public long WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public long? LocationId { get; set; }
    public string? LocationCode { get; set; }
    public string Status { get; set; } = string.Empty; // Available, Reserved, Dispatched, Defective
    public DateTime ReceivedAt { get; set; }
    public DateTime? DispatchedAt { get; set; }
}

public class CreateInventorySerialNumberRequest
{
    public long ProductId { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public long WarehouseId { get; set; }
    public long? LocationId { get; set; }
}
