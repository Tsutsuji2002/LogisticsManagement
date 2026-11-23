using Logistics.Server.Domain.Common;

namespace Logistics.Server.Domain.Entities.Warehouse;

public class WarehouseLocation : BaseEntity
{
    public long WarehouseZoneId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Aisle { get; set; }
    public string? Rack { get; set; }
    public string? Shelf { get; set; }
    public string? Bin { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public decimal MaxWeightKg { get; set; } = 1000m;
    public decimal MaxVolumeCbm { get; set; } = 5m;
    public bool IsOccupied { get; set; }
    public bool IsBlocked { get; set; }

    // Navigation
    public virtual WarehouseZone WarehouseZone { get; set; } = null!;
}
