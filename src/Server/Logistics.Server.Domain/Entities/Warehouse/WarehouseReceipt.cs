using Logistics.Server.Domain.Common;
using Logistics.Server.Domain.Entities.Organization;

namespace Logistics.Server.Domain.Entities.Warehouse;

public class WarehouseReceipt : BaseEntity
{
    public string ReceiptNumber { get; set; } = string.Empty;
    public long WarehouseId { get; set; }
    public string ReceiptType { get; set; } = "InventoryInbound"; // InventoryInbound, ParcelInbound, CustomerReturn, TransferIn
    public long? SourceBranchId { get; set; }
    public string? SourceVendor { get; set; }
    public DateTime? ExpectedArrivalDate { get; set; }
    public DateTime ActualArrivalDate { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Draft"; // Draft, Pending, Receiving, Completed, Cancelled
    public long ReceivedByEmployeeId { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public virtual Organization.Warehouse Warehouse { get; set; } = null!;
    public virtual Branch? SourceBranch { get; set; }
    public virtual Employee ReceivedByEmployee { get; set; } = null!;
    public virtual ICollection<WarehouseReceiptItem> ReceiptItems { get; set; } = [];
}
