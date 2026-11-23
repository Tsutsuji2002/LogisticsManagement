using Logistics.Server.Domain.Common;
using Logistics.Server.Domain.Entities.Organization;
using Logistics.Server.Domain.Entities.Shipping;

namespace Logistics.Server.Domain.Entities.Warehouse;

public class ParcelHandlingEvent : BaseHistoricalEntity
{
    public long ShipmentPackageId { get; set; }
    public long ShipmentId { get; set; }
    public long WarehouseId { get; set; }
    public string EventType { get; set; } = string.Empty; // Unloaded, ScannedAtHub, Sorted, Bagged, Staged, LoadedToVehicle, Dispatched, ReceivedAtDeliveryBranch
    public string? BagNumber { get; set; }
    public long? NextDestinationWarehouseId { get; set; }
    public long EmployeeId { get; set; }
    public DateTime ScannedAt { get; set; } = DateTime.UtcNow;
    public string? DeviceId { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public virtual ShipmentPackage ShipmentPackage { get; set; } = null!;
    public virtual Shipment Shipment { get; set; } = null!;
    public virtual Organization.Warehouse Warehouse { get; set; } = null!;
    public virtual Organization.Warehouse? NextDestinationWarehouse { get; set; }
    public virtual Employee Employee { get; set; } = null!;
}
