using Logistics.Server.Domain.Common;
using Logistics.Server.Domain.Entities.Carrier;
using Logistics.Server.Domain.Entities.Organization;

namespace Logistics.Server.Domain.Entities.Warehouse;

public class WarehouseDispatch : BaseEntity
{
    public string DispatchNumber { get; set; } = string.Empty;
    public long WarehouseId { get; set; }
    public string DispatchType { get; set; } = "InventoryOutbound"; // InventoryOutbound, HubTransfer, CourierDispatch, ReturnOutbound
    public long? DestinationWarehouseId { get; set; }
    public long? DestinationBranchId { get; set; }
    public long? AssignedCarrierId { get; set; }
    public string Status { get; set; } = "Draft"; // Draft, Picking, Packed, Staged, Dispatched, Cancelled
    public long DispatchedByEmployeeId { get; set; }
    public DateTime? DispatchedAt { get; set; }
    public string? VehicleNumber { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public virtual Organization.Warehouse Warehouse { get; set; } = null!;
    public virtual Organization.Warehouse? DestinationWarehouse { get; set; }
    public virtual Branch? DestinationBranch { get; set; }
    public virtual Carrier.Carrier? AssignedCarrier { get; set; }
    public virtual Employee DispatchedByEmployee { get; set; } = null!;
    public virtual ICollection<WarehouseDispatchItem> DispatchItems { get; set; } = [];
    public virtual ICollection<WarehouseDispatchPackage> DispatchPackages { get; set; } = [];
}
