using Logistics.Server.Domain.Common;
using Logistics.Server.Domain.Entities.Identity;
using Logistics.Server.Domain.Entities.Organization;
using WarehouseEntity = Logistics.Server.Domain.Entities.Organization.Warehouse;

namespace Logistics.Server.Domain.Entities.Shipping;

public class ShipmentStatusHistory : BaseHistoricalEntity
{
    public long ShipmentId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? SubStatus { get; set; }
    public long? LocationWarehouseId { get; set; }
    public long? LocationBranchId { get; set; }
    public string? Notes { get; set; }
    public long? ChangedByUserId { get; set; }
    public long? ChangedByEmployeeId { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string Source { get; set; } = "Internal"; // Internal, MobileApp, CarrierWebhook, BackgroundSync

    // Navigation
    public virtual Shipment Shipment { get; set; } = null!;
    public virtual WarehouseEntity? LocationWarehouse { get; set; }
    public virtual Branch? LocationBranch { get; set; }
    public virtual UserAccount? ChangedByUser { get; set; }
    public virtual Employee? ChangedByEmployee { get; set; }
}
