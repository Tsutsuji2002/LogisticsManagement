namespace Logistics.Server.Application.Modules.Shipping.DTOs;

public record ShipmentRouteDto
{
    public long Id { get; init; }
    public long ShipmentId { get; init; }
    public int SequenceOrder { get; init; }
    public long FromWarehouseId { get; init; }
    public string FromWarehouseName { get; init; } = string.Empty;
    public long ToWarehouseId { get; init; }
    public string ToWarehouseName { get; init; } = string.Empty;
    /// <summary>"Road", "Air", "Rail", "Sea"</summary>
    public string TransportType { get; init; } = string.Empty;
    public long? CarrierId { get; init; }
    public string? CarrierName { get; init; }
    public string? VehicleNumber { get; init; }
    public DateTime? EstimatedDeparture { get; init; }
    public DateTime? ActualDeparture { get; init; }
    public DateTime? EstimatedArrival { get; init; }
    public DateTime? ActualArrival { get; init; }
    /// <summary>"Planned", "Dispatched", "Arrived", "Delayed", "Cancelled"</summary>
    public string Status { get; init; } = "Planned";
}

public record AddShipmentRouteRequest
{
    public int SequenceOrder { get; init; }
    public long FromWarehouseId { get; init; }
    public long ToWarehouseId { get; init; }
    /// <summary>"Road", "Air", "Rail", "Sea"</summary>
    public string TransportType { get; init; } = string.Empty;
    public long? CarrierId { get; init; }
    public string? VehicleNumber { get; init; }
    public DateTime? EstimatedDeparture { get; init; }
    public DateTime? EstimatedArrival { get; init; }
}

public record UpdateShipmentRouteRequest
{
    public string? VehicleNumber { get; init; }
    public DateTime? EstimatedDeparture { get; init; }
    public DateTime? ActualDeparture { get; init; }
    public DateTime? EstimatedArrival { get; init; }
    public DateTime? ActualArrival { get; init; }
    /// <summary>"Planned", "Dispatched", "Arrived", "Delayed", "Cancelled"</summary>
    public string Status { get; init; } = string.Empty;
}
