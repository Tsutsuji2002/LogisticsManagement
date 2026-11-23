namespace Logistics.Server.Application.Modules.Crm.DTOs;

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
    DateTime? UpdatedAt
);

public record CustomerDetailDto(
    CustomerDto Customer,
    IReadOnlyList<CustomerContactDto> Contacts,
    IReadOnlyList<CustomerAddressDto> Addresses,
    IReadOnlyList<CustomerContractDto> Contracts,
    IReadOnlyList<CustomerPricingRuleDto> PricingRules
);

public record CustomerOverviewDto(
    int TotalCustomers,
    int ActiveCustomers,
    int InactiveCustomers,
    int IndividualCustomers,
    int CorporateCustomers,
    int CustomersWithActiveContracts,
    string ScopeType = "BranchScoped"
);

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
    long? AccountManagerEmployeeId = null
);

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
    long? AccountManagerEmployeeId
);

public record UpdateCustomerStatusRequest(
    bool IsActive,
    string? Reason = null
);
