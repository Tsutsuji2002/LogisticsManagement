using Logistics.Server.Domain.Common;

namespace Logistics.Server.Domain.Entities.Crm;

public class CustomerAddress : BaseEntity
{
    public long CustomerId { get; set; }
    public long AddressId { get; set; }
    public string AddressType { get; set; } = "Sender"; // Sender, Recipient, Billing, PickupWarehouse
    public bool IsDefault { get; set; }
    public string? AddressLabel { get; set; }

    // Navigation
    public virtual Customer Customer { get; set; } = null!;
    public virtual Address Address { get; set; } = null!;
}
