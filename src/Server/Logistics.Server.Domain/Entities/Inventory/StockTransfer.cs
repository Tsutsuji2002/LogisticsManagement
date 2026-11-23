using Logistics.Server.Domain.Common;
using Logistics.Server.Domain.Entities.Organization;

namespace Logistics.Server.Domain.Entities.Inventory;

public class StockTransfer : BaseEntity
{
    public string TransferNumber { get; set; } = string.Empty;
    public long SourceWarehouseId { get; set; }
    public long DestinationWarehouseId { get; set; }
    public string Status { get; set; } = "Draft"; // Draft, Approved, InTransit, Received, Cancelled
    public long RequestedByEmployeeId { get; set; }
    public long? ApprovedByEmployeeId { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public virtual Organization.Warehouse SourceWarehouse { get; set; } = null!;
    public virtual Organization.Warehouse DestinationWarehouse { get; set; } = null!;
    public virtual Employee RequestedByEmployee { get; set; } = null!;
    public virtual Employee? ApprovedByEmployee { get; set; }
}
