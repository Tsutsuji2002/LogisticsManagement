using Logistics.Server.Domain.Entities.Catalog;
using Logistics.Server.Domain.Entities.Configuration;

namespace Logistics.Server.Domain.Entities.Warehouse;

public class WarehouseReceiptItem
{
    public long Id { get; set; }
    public long WarehouseReceiptId { get; set; }
    public long ProductId { get; set; }
    public int ExpectedQuantity { get; set; }
    public int ReceivedQuantity { get; set; }
    public int DamagedQuantity { get; set; }
    public long UnitOfMeasureId { get; set; }
    public long? TargetLocationId { get; set; }
    public string? LotNumber { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public virtual WarehouseReceipt WarehouseReceipt { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
    public virtual UnitOfMeasure UnitOfMeasure { get; set; } = null!;
    public virtual WarehouseLocation? TargetLocation { get; set; }
}
