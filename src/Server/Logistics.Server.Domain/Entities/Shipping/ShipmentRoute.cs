using WarehouseEntity = Logistics.Server.Domain.Entities.Organization.Warehouse;

namespace Logistics.Server.Domain.Entities.Shipping;

public class ShipmentRoute
{
    public long Id { get; set; }
    public long ShipmentId { get; set; }
    public int SequenceOrder { get; set; }
    public long FromWarehouseId { get; set; }
    public long ToWarehouseId { get; set; }
    public string TransportType { get; set; } = "Road"; // Road, Air, Rail, Sea
    public long? CarrierId { get; set; }
    public string? VehicleNumber { get; set; }
    public DateTime? EstimatedDeparture { get; set; }
    public DateTime? ActualDeparture { get; set; }
    public DateTime? EstimatedArrival { get; set; }
    public DateTime? ActualArrival { get; set; }
    public string Status { get; set; } = "Planned"; // Planned, Dispatched, Arrived, Delayed, Cancelled

    // Navigation
    public virtual Shipment Shipment { get; set; } = null!;
    public virtual WarehouseEntity FromWarehouse { get; set; } = null!;
    public virtual WarehouseEntity ToWarehouse { get; set; } = null!;
    public virtual Carrier.Carrier? Carrier { get; set; }
}
