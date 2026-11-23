using Logistics.Server.Domain.Common;
using Logistics.Server.Domain.Entities.Organization;

namespace Logistics.Server.Domain.Entities.Inventory;

public class InventoryTransaction : BaseEntity
{
    public string TransactionNumber { get; set; } = string.Empty;
    public long WarehouseId { get; set; }
    public string TransactionType { get; set; } = string.Empty; // Receipt, Dispatch, TransferIn, TransferOut, PositiveAdjustment, NegativeAdjustment, CycleCount
    public string ReferenceType { get; set; } = string.Empty; // WarehouseReceipt, WarehouseDispatch, StockTransfer, StockAdjustment
    public long ReferenceId { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public long PerformedByEmployeeId { get; set; }
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }

    // Navigation
    public virtual Organization.Warehouse Warehouse { get; set; } = null!;
    public virtual Employee PerformedByEmployee { get; set; } = null!;
    public virtual ICollection<InventoryTransactionItem> TransactionItems { get; set; } = [];
}
