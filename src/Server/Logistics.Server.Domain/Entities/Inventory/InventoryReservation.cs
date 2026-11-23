using Logistics.Server.Domain.Common;
using Logistics.Server.Domain.Entities.Catalog;
using Logistics.Server.Domain.Entities.Organization;
using Logistics.Server.Domain.Entities.Sales;
using Logistics.Server.Domain.Entities.Warehouse;

namespace Logistics.Server.Domain.Entities.Inventory;

public class InventoryReservation : BaseEntity
{
    public long ProductId { get; set; }
    public long WarehouseId { get; set; }
    public long LocationId { get; set; }
    public long? LotId { get; set; }
    public long OrderId { get; set; }
    public int ReservedQuantity { get; set; }
    public string Status { get; set; } = "Active"; // Active, Released, Fulfilled, Expired
    public DateTime ReservedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? FulfilledAt { get; set; }

    // Navigation
    public virtual Product Product { get; set; } = null!;
    public virtual Organization.Warehouse Warehouse { get; set; } = null!;
    public virtual WarehouseLocation Location { get; set; } = null!;
    public virtual InventoryLot? Lot { get; set; }
    public virtual Order Order { get; set; } = null!;
}
