using Logistics.Server.Application.Common.Models;
using Logistics.Server.Application.Modules.Crm.DTOs;
using Logistics.Shared.Contracts.Pagination;

namespace Logistics.Server.Application.Modules.Crm.Services;

public interface ICustomerService
{
    Task<PagedResult<CustomerDto>> GetCustomersAsync(
        string? search,
        string? customerType,
        bool? isActive,
        long? branchId,
        DateTime? dateFrom,
        DateTime? dateTo,
        PagedRequest request,
        CancellationToken cancellationToken = default);

    Task<CustomerDto?> GetByIdOrPublicIdAsync(string identifier, CancellationToken cancellationToken = default);
    Task<CustomerDetailDto?> GetDetailByIdOrPublicIdAsync(string identifier, CancellationToken cancellationToken = default);
    Task<CustomerOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default);
    Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default);
    Task<CustomerDto> UpdateAsync(string identifier, UpdateCustomerRequest request, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(string identifier, UpdateCustomerStatusRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(string identifier, CancellationToken cancellationToken = default);
}

public interface ICustomerContactService
{
    Task<IReadOnlyList<CustomerContactDto>> GetContactsAsync(
        string customerIdentifier,
        string? search,
        bool? isPrimary,
        CancellationToken cancellationToken = default);

    Task<CustomerContactDto?> GetByIdOrPublicIdAsync(
        string customerIdentifier,
        string contactIdentifier,
        CancellationToken cancellationToken = default);

    Task<CustomerContactDto> CreateAsync(
        string customerIdentifier,
        CreateCustomerContactRequest request,
        CancellationToken cancellationToken = default);

    Task<CustomerContactDto> UpdateAsync(
        string customerIdentifier,
        string contactIdentifier,
        UpdateCustomerContactRequest request,
        CancellationToken cancellationToken = default);

    Task SetPrimaryAsync(
        string customerIdentifier,
        string contactIdentifier,
        CancellationToken cancellationToken = default);

    Task UpdateStatusAsync(
        string customerIdentifier,
        string contactIdentifier,
        UpdateContactStatusRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string customerIdentifier,
        string contactIdentifier,
        CancellationToken cancellationToken = default);
}

public interface ICustomerAddressService
{
    Task<IReadOnlyList<CustomerAddressDto>> GetAddressesAsync(
        string customerIdentifier,
        string? addressType,
        bool? isDefault,
        CancellationToken cancellationToken = default);

    Task<CustomerAddressDto?> GetByIdOrPublicIdAsync(
        string customerIdentifier,
        string addressIdentifier,
        CancellationToken cancellationToken = default);

    Task<CustomerAddressDto> CreateAsync(
        string customerIdentifier,
        CreateCustomerAddressRequest request,
        CancellationToken cancellationToken = default);

    Task<CustomerAddressDto> UpdateAsync(
        string customerIdentifier,
        string addressIdentifier,
        UpdateCustomerAddressRequest request,
        CancellationToken cancellationToken = default);

    Task SetDefaultAsync(
        string customerIdentifier,
        string addressIdentifier,
        SetDefaultAddressRequest request,
        CancellationToken cancellationToken = default);

    Task UpdateStatusAsync(
        string customerIdentifier,
        string addressIdentifier,
        UpdateCustomerAddressStatusRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string customerIdentifier,
        string addressIdentifier,
        CancellationToken cancellationToken = default);
}

public interface ICustomerContractService
{
    Task<IReadOnlyList<CustomerContractDto>> GetContractsAsync(
        string customerIdentifier,
        string? search,
        string? status,
        bool? activeOnly,
        CancellationToken cancellationToken = default);

    Task<CustomerContractDto?> GetByIdOrPublicIdAsync(
        string customerIdentifier,
        string contractIdentifier,
        CancellationToken cancellationToken = default);

    Task<CustomerContractDto> CreateAsync(
        string customerIdentifier,
        CreateCustomerContractRequest request,
        CancellationToken cancellationToken = default);

    Task<CustomerContractDto> UpdateAsync(
        string customerIdentifier,
        string contractIdentifier,
        UpdateCustomerContractRequest request,
        CancellationToken cancellationToken = default);

    Task UpdateStatusAsync(
        string customerIdentifier,
        string contractIdentifier,
        UpdateCustomerContractStatusRequest request,
        CancellationToken cancellationToken = default);
}

public interface ICustomerPricingRuleService
{
    Task<IReadOnlyList<CustomerPricingRuleDto>> GetPricingRulesAsync(
        string customerIdentifier,
        bool? isActive,
        long? rateCardId,
        CancellationToken cancellationToken = default);

    Task<CustomerPricingRuleDto?> GetByIdOrPublicIdAsync(
        string customerIdentifier,
        string ruleIdentifier,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerPricingRuleDto>> GetApplicablePricingRulesAsync(
        string customerIdentifier,
        DateTime? targetDate,
        long? shippingRateCardId,
        CancellationToken cancellationToken = default);

    Task<CustomerPricingRuleDto> CreateAsync(
        string customerIdentifier,
        CreateCustomerPricingRuleRequest request,
        CancellationToken cancellationToken = default);

    Task<CustomerPricingRuleDto> UpdateAsync(
        string customerIdentifier,
        string ruleIdentifier,
        UpdateCustomerPricingRuleRequest request,
        CancellationToken cancellationToken = default);

    Task UpdateStatusAsync(
        string customerIdentifier,
        string ruleIdentifier,
        UpdateCustomerPricingRuleStatusRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string customerIdentifier,
        string ruleIdentifier,
        CancellationToken cancellationToken = default);
}
