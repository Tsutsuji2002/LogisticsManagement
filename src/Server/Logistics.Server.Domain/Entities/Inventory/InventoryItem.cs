using Logistics.Server.Domain.Common;
using Logistics.Server.Domain.Entities.Catalog;
using Logistics.Server.Domain.Entities.Organization;

namespace Logistics.Server.Domain.Entities.Inventory;

public class InventoryItem : BaseEntity
{
    public long ProductId { get; set; }
    public long WarehouseId { get; set; }
    public int ReorderPoint { get; set; } = 10;
    public int SafetyStock { get; set; } = 5;
    public int TargetStock { get; set; } = 100;
    public int MinimumOrderQuantity { get; set; } = 1;
    public int MaximumStock { get; set; } = 500;
    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual Product Product { get; set; } = null!;
    public virtual Organization.Warehouse Warehouse { get; set; } = null!;
}
