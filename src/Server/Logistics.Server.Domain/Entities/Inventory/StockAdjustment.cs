using Logistics.Server.Domain.Common;
using Logistics.Server.Domain.Entities.Organization;

namespace Logistics.Server.Domain.Entities.Inventory;

public class StockAdjustment : BaseEntity
{
    public string AdjustmentNumber { get; set; } = string.Empty;
    public long WarehouseId { get; set; }
    public string ReasonCode { get; set; } = string.Empty; // Damage, Shrinkage, CountingVariance, Expired, FoundGoods
    public string ReasonDescription { get; set; } = string.Empty;
    public string Status { get; set; } = "Draft"; // Draft, Approved, Rejected, Applied
    public long? ApprovedByEmployeeId { get; set; }
    public DateTime? ApprovedAt { get; set; }

    // Navigation
    public virtual Organization.Warehouse Warehouse { get; set; } = null!;
    public virtual Employee? ApprovedByEmployee { get; set; }
}
