using Logistics.Server.Domain.Common;
using Logistics.Server.Domain.Entities.Catalog;

namespace Logistics.Server.Domain.Entities.Inventory;

public class InventoryLot : BaseEntity
{
    public long ProductId { get; set; }
    public string LotNumber { get; set; } = string.Empty;
    public DateOnly? ManufactureDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public string? VendorBatchCode { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public virtual Product Product { get; set; } = null!;
}
