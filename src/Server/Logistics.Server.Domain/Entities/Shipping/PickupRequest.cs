using Logistics.Server.Domain.Common;
using Logistics.Server.Domain.Entities.Crm;
using Logistics.Server.Domain.Entities.Organization;

namespace Logistics.Server.Domain.Entities.Shipping;

public class PickupRequest : BaseEntity
{
    public string PickupCode { get; set; } = string.Empty;
    public long CustomerId { get; set; }
    public long RequestedAddressId { get; set; }
    public DateOnly ScheduledDate { get; set; }
    public string TimeSlot { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending, Assigned, DriverArrived, Completed, Cancelled
    public long? AssignedEmployeeId { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public int EstimatedParcels { get; set; } = 1;
    public int ActualParcelsPickedUp { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public virtual Customer Customer { get; set; } = null!;
    public virtual Address RequestedAddress { get; set; } = null!;
    public virtual Employee? AssignedEmployee { get; set; }
}
