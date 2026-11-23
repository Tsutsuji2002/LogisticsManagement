namespace Logistics.Server.Application.Modules.Shipping.DTOs;

public record ShipmentAssignmentDto
{
    public long Id { get; init; }
    public long ShipmentId { get; init; }
    /// <summary>"Pickup", "Linehaul", "LastMileDelivery"</summary>
    public string AssignmentType { get; init; } = string.Empty;
    public long AssignedToEmployeeId { get; init; }
    public string AssignedToEmployeeName { get; init; } = string.Empty;
    public long AssignedBranchId { get; init; }
    public string AssignedBranchName { get; init; } = string.Empty;
    public long? AssignedVehicleId { get; init; }
    public string? VehicleNumber { get; init; }
    public DateTime AssignedAt { get; init; }
    public string? AssignedByUserId { get; init; }
    /// <summary>"Assigned", "Accepted", "InProgress", "Completed", "Failed"</summary>
    public string Status { get; init; } = "Assigned";
    public DateTime? CompletedAt { get; init; }
    public string? Notes { get; init; }
}

public record CreateShipmentAssignmentRequest
{
    /// <summary>"Pickup", "Linehaul", "LastMileDelivery"</summary>
    public string AssignmentType { get; init; } = string.Empty;
    public long AssignedToEmployeeId { get; init; }
    public long AssignedBranchId { get; init; }
    public long? AssignedVehicleId { get; init; }
    public string? Notes { get; init; }
}

public record UpdateShipmentAssignmentRequest
{
    /// <summary>"Assigned", "Accepted", "InProgress", "Completed", "Failed"</summary>
    public string Status { get; init; } = string.Empty;
    public long? AssignedVehicleId { get; init; }
    public string? Notes { get; init; }
}
