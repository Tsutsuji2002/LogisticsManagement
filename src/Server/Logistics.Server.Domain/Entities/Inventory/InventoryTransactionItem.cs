using Logistics.Server.Domain.Entities.Catalog;
using Logistics.Server.Domain.Entities.Warehouse;

namespace Logistics.Server.Domain.Entities.Inventory;

public class InventoryTransactionItem
{
    public long Id { get; set; }
    public long InventoryTransactionId { get; set; }
    public long ProductId { get; set; }
    public long LocationId { get; set; }
    public long? LotId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public int BalanceBefore { get; set; }
    public int BalanceAfter { get; set; }

    // Navigation
    public virtual InventoryTransaction InventoryTransaction { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
    public virtual WarehouseLocation Location { get; set; } = null!;
    public virtual InventoryLot? Lot { get; set; }
}
