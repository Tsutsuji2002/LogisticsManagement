namespace Logistics.Client.Application.Contracts.Crm;

public record CustomerDto(
    long Id,
    Guid PublicId,
    long BranchId,
    string? BranchCode,
    string? BranchName,
    string CustomerCode,
    string CustomerType,
    string Name,
    string? TradeName,
    string? TaxCode,
    string Email,
    string PhoneNumber,
    decimal CreditLimit,
    int CreditTermDays,
    string PaymentType,
    long? AccountManagerEmployeeId,
    string? AccountManagerName,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record CustomerDetailDto(
    CustomerDto Customer,
    IReadOnlyList<CustomerContactDto> Contacts,
    IReadOnlyList<CustomerAddressDto> Addresses,
    IReadOnlyList<CustomerContractDto> Contracts,
    IReadOnlyList<CustomerPricingRuleDto> PricingRules);

public record CustomerOverviewDto(
    int TotalCustomers,
    int ActiveCustomers,
    int InactiveCustomers,
    int IndividualCustomers,
    int CorporateCustomers,
    int CustomersWithActiveContracts,
    string ScopeType = "BranchScoped");

public record CreateCustomerRequest(
    long? BranchId,
    string CustomerCode,
    string CustomerType,
    string Name,
    string? TradeName,
    string? TaxCode,
    string Email,
    string PhoneNumber,
    decimal CreditLimit = 0,
    int CreditTermDays = 0,
    string PaymentType = "Prepaid",
    long? AccountManagerEmployeeId = null);

public record UpdateCustomerRequest(
    string CustomerType,
    string Name,
    string? TradeName,
    string? TaxCode,
    string Email,
    string PhoneNumber,
    decimal CreditLimit,
    int CreditTermDays,
    string PaymentType,
    long? AccountManagerEmployeeId);

public record UpdateCustomerStatusRequest(bool IsActive, string? Reason = null);

public record CustomerContactDto(
    long Id,
    Guid PublicId,
    long CustomerId,
    string ContactName,
    string? JobTitle,
    string PhoneNumber,
    string? Email,
    bool IsPrimary,
    string? Notes,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record CreateCustomerContactRequest(
    string ContactName,
    string PhoneNumber,
    string? JobTitle = null,
    string? Email = null,
    bool IsPrimary = false,
    string? Notes = null);

public record UpdateCustomerContactRequest(
    string ContactName,
    string PhoneNumber,
    string? JobTitle = null,
    string? Email = null,
    bool IsPrimary = false,
    string? Notes = null);

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
    DateTime? UpdatedAt);

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
    decimal? Longitude = null);

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
    decimal? Longitude = null);

public record SetDefaultAddressRequest(bool IsDefault);

public record CustomerContractDto(
    long Id,
    Guid PublicId,
    long CustomerId,
    string? CustomerCode,
    string ContractNumber,
    string Title,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    decimal DiscountRate,
    int PaymentTermDays,
    DateOnly? SignedAt,
    long? FileAttachmentId,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record CreateCustomerContractRequest(
    string ContractNumber,
    string Title,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status = "Draft",
    decimal DiscountRate = 0,
    int PaymentTermDays = 30,
    DateOnly? SignedAt = null,
    long? FileAttachmentId = null);

public record UpdateCustomerContractRequest(
    string Title,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    decimal DiscountRate,
    int PaymentTermDays,
    DateOnly? SignedAt,
    long? FileAttachmentId);

public record UpdateCustomerContractStatusRequest(string Status, string? Reason = null);

public record CustomerPricingRuleDto(
    long Id,
    Guid PublicId,
    long CustomerId,
    string? CustomerCode,
    long? CustomerContractId,
    string? ContractNumber,
    long? ShippingRateCardId,
    string? ShippingRateCardName,
    string Name,
    decimal DiscountPercent,
    decimal FlatDiscountAmount,
    int Priority,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record CreateCustomerPricingRuleRequest(
    string Name,
    DateTime EffectiveFrom,
    long? CustomerContractId = null,
    long? ShippingRateCardId = null,
    decimal DiscountPercent = 0,
    decimal FlatDiscountAmount = 0,
    int Priority = 1,
    DateTime? EffectiveTo = null,
    bool IsActive = true);

public record UpdateCustomerPricingRuleRequest(
    string Name,
    DateTime EffectiveFrom,
    long? CustomerContractId = null,
    long? ShippingRateCardId = null,
    decimal DiscountPercent = 0,
    decimal FlatDiscountAmount = 0,
    int Priority = 1,
    DateTime? EffectiveTo = null,
    bool IsActive = true);

public record UpdateCustomerPricingRuleStatusRequest(bool IsActive);
