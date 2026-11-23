using Logistics.Server.Domain.Entities.Catalog;
using Logistics.Server.Domain.Entities.Organization;
using Logistics.Server.Domain.Entities.Warehouse;

namespace Logistics.Server.Domain.Entities.Inventory;

public class InventoryBalance
{
    public long Id { get; set; }
    public long WarehouseId { get; set; }
    public long WarehouseLocationId { get; set; }
    public long ProductId { get; set; }
    public long? LotId { get; set; }
    public int QuantityOnHand { get; set; }
    public int QuantityAllocated { get; set; }
    public int QuantityAvailable { get; private set; } // Computed in SQL Server
    public int QuantityDamaged { get; set; }
    public DateTime? LastCountDate { get; set; }
    public byte[]? RowVersion { get; set; }

    // Navigation
    public virtual Organization.Warehouse Warehouse { get; set; } = null!;
    public virtual WarehouseLocation WarehouseLocation { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
    public virtual InventoryLot? Lot { get; set; }
}
