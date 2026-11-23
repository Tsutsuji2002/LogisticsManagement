using Logistics.Server.Domain.Entities.Catalog;
using Logistics.Server.Domain.Entities.Configuration;

namespace Logistics.Server.Domain.Entities.Warehouse;

public class WarehouseDispatchItem
{
    public long Id { get; set; }
    public long WarehouseDispatchId { get; set; }
    public long ProductId { get; set; }
    public int RequestedQuantity { get; set; }
    public int PickedQuantity { get; set; }
    public long UnitOfMeasureId { get; set; }
    public long SourceLocationId { get; set; }
    public string? LotNumber { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public virtual WarehouseDispatch WarehouseDispatch { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
    public virtual UnitOfMeasure UnitOfMeasure { get; set; } = null!;
    public virtual WarehouseLocation SourceLocation { get; set; } = null!;
}
