using Logistics.Server.Domain.Common;
using Logistics.Server.Domain.Entities.Crm;
using Logistics.Server.Domain.Entities.Warehouse;

namespace Logistics.Server.Domain.Entities.Organization;

public class Warehouse : BaseEntity
{
    public long BranchId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Hybrid"; // InventoryWarehouse, ParcelHub, Hybrid
    public decimal TotalAreaSqM { get; set; }
    public long? AddressId { get; set; }
    public long? ManagerEmployeeId { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual Branch Branch { get; set; } = null!;
    public virtual Address? Address { get; set; }
    public virtual Employee? ManagerEmployee { get; set; }
    public virtual ICollection<WarehouseZone> Zones { get; set; } = [];
}
