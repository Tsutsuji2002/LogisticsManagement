using Logistics.Server.Domain.Entities.Identity;
using Logistics.Server.Domain.Entities.Organization;

namespace Logistics.Server.Domain.Entities.Shipping;

public class ShipmentAssignment
{
    public long Id { get; set; }
    public long ShipmentId { get; set; }
    public string AssignmentType { get; set; } = "Pickup"; // Pickup, Linehaul, LastMileDelivery
    public long AssignedToEmployeeId { get; set; }
    public long AssignedBranchId { get; set; }
    public string? AssignedVehicleId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public long? AssignedByUserId { get; set; }
    public string Status { get; set; } = "Assigned"; // Assigned, Accepted, InProgress, Completed, Failed
    public DateTime? CompletedAt { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public virtual Shipment Shipment { get; set; } = null!;
    public virtual Employee AssignedToEmployee { get; set; } = null!;
    public virtual Branch AssignedBranch { get; set; } = null!;
    public virtual UserAccount? AssignedByUser { get; set; }
}
