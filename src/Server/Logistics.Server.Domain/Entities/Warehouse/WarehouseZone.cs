using Logistics.Server.Domain.Common;
using Logistics.Server.Domain.Entities.Organization;

namespace Logistics.Server.Domain.Entities.Warehouse;

public class WarehouseZone : BaseEntity
{
    public long WarehouseId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ZoneType { get; set; } = "BulkStorage"; // Receiving, BulkStorage, Picking, Staging, ParcelSorting, CrossDock, Return, Packing
    public string TemperatureType { get; set; } = "Ambient"; // Ambient, Chilled, Frozen, AirConditioned
    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual Organization.Warehouse Warehouse { get; set; } = null!;
    public virtual ICollection<WarehouseLocation> Locations { get; set; } = [];
}
