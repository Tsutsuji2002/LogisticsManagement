using Logistics.Server.Domain.Entities.Catalog;
using Logistics.Server.Domain.Entities.Organization;
using Logistics.Server.Domain.Entities.Warehouse;

namespace Logistics.Server.Domain.Entities.Inventory;

public class InventorySerialNumber
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public long WarehouseId { get; set; }
    public long? LocationId { get; set; }
    public string Status { get; set; } = "Available"; // Available, Reserved, Dispatched, Defective
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DispatchedAt { get; set; }

    // Navigation
    public virtual Product Product { get; set; } = null!;
    public virtual Organization.Warehouse Warehouse { get; set; } = null!;
    public virtual WarehouseLocation? Location { get; set; }
}
