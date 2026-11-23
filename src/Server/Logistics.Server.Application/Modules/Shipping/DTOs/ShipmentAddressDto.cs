namespace Logistics.Server.Application.Modules.Shipping.DTOs;

public record ShipmentAddressDto
{
    public long Id { get; init; }
    public long ShipmentId { get; init; }
    /// <summary>"Sender", "Recipient", "ReturnAddress"</summary>
    public string AddressType { get; init; } = string.Empty;
    public string ContactName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string AddressLine1 { get; init; } = string.Empty;
    public string? AddressLine2 { get; init; }
    public long CountryId { get; init; }
    public string? CountryName { get; init; }
    public long? ProvinceId { get; init; }
    public string? ProvinceName { get; init; }
    public long? DistrictId { get; init; }
    public string? DistrictName { get; init; }
    public long? WardId { get; init; }
    public string? WardName { get; init; }
    public string? PostalCode { get; init; }
    public string FormattedAddress { get; init; } = string.Empty;
}

public record UpdateShipmentAddressRequest
{
    public string ContactName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string AddressLine1 { get; init; } = string.Empty;
    public string? AddressLine2 { get; init; }
    public long CountryId { get; init; }
    public long? ProvinceId { get; init; }
    public long? DistrictId { get; init; }
    public long? WardId { get; init; }
    public string? PostalCode { get; init; }
}
