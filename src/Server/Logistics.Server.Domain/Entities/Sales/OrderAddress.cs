using Logistics.Server.Domain.Entities.Configuration;

namespace Logistics.Server.Domain.Entities.Sales;

public class OrderAddress
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public string AddressType { get; set; } = "Sender"; // Sender, Recipient, Pickup
    public string ContactName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public long CountryId { get; set; }
    public long? ProvinceId { get; set; }
    public long? DistrictId { get; set; }
    public long? WardId { get; set; }
    public string? PostalCode { get; set; }
    public string FormattedAddress { get; set; } = string.Empty;

    // Navigation
    public virtual Order Order { get; set; } = null!;
    public virtual Country Country { get; set; } = null!;
    public virtual Province? Province { get; set; }
    public virtual District? District { get; set; }
    public virtual Ward? Ward { get; set; }
}
