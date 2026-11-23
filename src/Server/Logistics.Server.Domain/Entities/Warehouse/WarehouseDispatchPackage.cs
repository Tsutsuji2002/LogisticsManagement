using Logistics.Server.Domain.Entities.Organization;
using Logistics.Server.Domain.Entities.Shipping;

namespace Logistics.Server.Domain.Entities.Warehouse;

public class WarehouseDispatchPackage
{
    public long Id { get; set; }
    public long WarehouseDispatchId { get; set; }
    public long ShipmentPackageId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public DateTime ScannedAt { get; set; } = DateTime.UtcNow;
    public long ScannedByEmployeeId { get; set; }
    public string? TargetContainerCode { get; set; }

    // Navigation
    public virtual WarehouseDispatch WarehouseDispatch { get; set; } = null!;
    public virtual ShipmentPackage ShipmentPackage { get; set; } = null!;
    public virtual Employee ScannedByEmployee { get; set; } = null!;
}
