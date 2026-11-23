namespace Logistics.Server.Application.Modules.Crm.DTOs;

public record CustomerAddressDto(
    long Id,
    Guid PublicId,
    long CustomerId,
    long AddressId,
    Guid AddressPublicId,
    string AddressType,
    bool IsDefault,
    string? AddressLabel,
    string ContactName,
    string PhoneNumber,
    string? Email,
    string AddressLine1,
    string? AddressLine2,
    long CountryId,
    string? CountryName,
    long? ProvinceId,
    string? ProvinceName,
    long? DistrictId,
    string? DistrictName,
    long? WardId,
    string? WardName,
    string? PostalCode,
    decimal? Latitude,
    decimal? Longitude,
    string FormattedAddress,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CreateCustomerAddressRequest(
    string AddressType,
    string ContactName,
    string PhoneNumber,
    string AddressLine1,
    string? FormattedAddress = null,
    string? AddressLine2 = null,
    string? Email = null,
    bool IsDefault = false,
    string? AddressLabel = null,
    long? CountryId = null,
    long? ProvinceId = null,
    long? DistrictId = null,
    long? WardId = null,
    string? PostalCode = null,
    decimal? Latitude = null,
    decimal? Longitude = null
);

public record UpdateCustomerAddressRequest(
    string AddressType,
    string ContactName,
    string PhoneNumber,
    string AddressLine1,
    string? FormattedAddress = null,
    string? AddressLine2 = null,
    string? Email = null,
    bool IsDefault = false,
    string? AddressLabel = null,
    long? CountryId = null,
    long? ProvinceId = null,
    long? DistrictId = null,
    long? WardId = null,
    string? PostalCode = null,
    decimal? Latitude = null,
    decimal? Longitude = null
);

public record SetDefaultAddressRequest(
    bool IsDefault
);

public record UpdateCustomerAddressStatusRequest(
    bool IsActive,
    string? Reason = null
);
