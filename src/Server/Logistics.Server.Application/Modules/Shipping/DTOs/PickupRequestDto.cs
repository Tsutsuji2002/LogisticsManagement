namespace Logistics.Server.Application.Modules.Shipping.DTOs;

public record PickupRequestDto
{
    public long Id { get; init; }
    public Guid PublicId { get; init; }
    public string PickupCode { get; init; } = string.Empty;
    public long CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public long RequestedAddressId { get; init; }
    public string FormattedAddress { get; init; } = string.Empty;
    public DateOnly ScheduledDate { get; init; }
    public string TimeSlot { get; init; } = string.Empty;
    /// <summary>"Pending", "Assigned", "DriverArrived", "Completed", "Cancelled"</summary>
    public string Status { get; init; } = "Pending";
    public long? AssignedEmployeeId { get; init; }
    public string? AssignedEmployeeName { get; init; }
    public string ContactName { get; init; } = string.Empty;
    public string ContactPhone { get; init; } = string.Empty;
    public int EstimatedParcels { get; init; }
    public int ActualParcelsPickedUp { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? UpdatedBy { get; init; }
}

public record CreatePickupRequestRequest
{
    public long CustomerId { get; init; }
    public long RequestedAddressId { get; init; }
    public DateOnly ScheduledDate { get; init; }
    public string TimeSlot { get; init; } = string.Empty;
    public string ContactName { get; init; } = string.Empty;
    public string ContactPhone { get; init; } = string.Empty;
    public int EstimatedParcels { get; init; }
    public string? Notes { get; init; }
}

public record UpdatePickupRequestRequest
{
    public DateOnly ScheduledDate { get; init; }
    public string TimeSlot { get; init; } = string.Empty;
    public string ContactName { get; init; } = string.Empty;
    public string ContactPhone { get; init; } = string.Empty;
    public int EstimatedParcels { get; init; }
    public long? AssignedEmployeeId { get; init; }
    public string? Notes { get; init; }
}

public record CompletePickupRequest
{
    public int ActualParcelsPickedUp { get; init; }
    public string? Notes { get; init; }
}
