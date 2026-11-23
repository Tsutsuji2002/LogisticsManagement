using Logistics.Server.Application.Common.Models;
using Logistics.Server.Application.Modules.Finance.DTOs;
using Logistics.Shared.Contracts.Pagination;

namespace Logistics.Server.Application.Modules.Finance.Services;

public interface IRateManagementService
{
    Task<PagedResult<ShippingRateCardDto>> GetRateCardsAsync(bool? isActive, string? search, PagedRequest request, CancellationToken cancellationToken = default);
    Task<ShippingRateCardDto?> GetRateCardByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<ShippingRateCardDto> CreateRateCardAsync(CreateShippingRateCardRequest request, CancellationToken cancellationToken = default);
    Task<ShippingRateCardDto> UpdateRateCardAsync(long id, UpdateShippingRateCardRequest request, CancellationToken cancellationToken = default);
    Task<ShippingRateCardDto> ToggleRateCardStatusAsync(long id, bool isActive, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ShippingRateRuleDto>> GetRateRulesAsync(long rateCardId, CancellationToken cancellationToken = default);
    Task<ShippingRateRuleDto> CreateRateRuleAsync(long rateCardId, CreateShippingRateRuleRequest request, CancellationToken cancellationToken = default);
    Task<ShippingRateRuleDto> UpdateRateRuleAsync(long rateCardId, long ruleId, UpdateShippingRateRuleRequest request, CancellationToken cancellationToken = default);
    Task DeleteRateRuleAsync(long rateCardId, long ruleId, CancellationToken cancellationToken = default);
}

public interface IShippingFeeCalculationService
{
    Task<ShippingFeeCalculationResultDto> CalculateFeeAsync(CalculateShippingFeeRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShipmentChargeDto>> CalculateAndSaveShipmentChargesAsync(long shipmentId, CancellationToken cancellationToken = default);
}

public interface IShipmentChargeService
{
    Task<PagedResult<ShipmentChargeDto>> GetChargesAsync(long? shipmentId, long? customerId, bool? isInvoiced, PagedRequest request, CancellationToken cancellationToken = default);
    Task<ShipmentChargeDto?> GetChargeByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<ShipmentChargeDto> AddAdjustmentChargeAsync(long shipmentId, CreateChargeAdjustmentRequest request, CancellationToken cancellationToken = default);
}

public interface IInvoiceService
{
    Task<PagedResult<InvoiceDto>> GetInvoicesAsync(long? customerId, string? status, DateOnly? fromDate, DateOnly? toDate, PagedRequest request, CancellationToken cancellationToken = default);
    Task<InvoiceDto?> GetInvoiceByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<InvoiceDto> CreateDraftInvoiceAsync(CreateInvoiceRequest request, CancellationToken cancellationToken = default);
    Task<InvoiceDto> AddInvoiceItemAsync(long invoiceId, CreateInvoiceItemRequest request, CancellationToken cancellationToken = default);
    Task<InvoiceDto> RemoveInvoiceItemAsync(long invoiceId, long itemId, CancellationToken cancellationToken = default);
    Task<InvoiceDto> IssueInvoiceAsync(long invoiceId, CancellationToken cancellationToken = default);
    Task<InvoiceDto> CancelInvoiceAsync(long invoiceId, CancelInvoiceRequest request, CancellationToken cancellationToken = default);
}

public interface IPaymentService
{
    Task<PagedResult<PaymentDto>> GetPaymentsAsync(long? customerId, string? status, string? paymentMethod, PagedRequest request, CancellationToken cancellationToken = default);
    Task<PaymentDto?> GetPaymentByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<PaymentDto> RegisterPaymentAsync(RegisterPaymentRequest request, CancellationToken cancellationToken = default);
    Task<PaymentDto> AllocatePaymentAsync(long paymentId, CreatePaymentAllocationRequest request, CancellationToken cancellationToken = default);
    Task<PaymentDto> ReverseAllocationAsync(long paymentId, long allocationId, CancellationToken cancellationToken = default);
}

public interface ICustomerBalanceService
{
    Task<CustomerBalanceDto?> GetCustomerBalanceAsync(long customerId, CancellationToken cancellationToken = default);
    Task<PagedResult<CustomerBalanceDto>> GetBalancesAsync(long? branchId, PagedRequest request, CancellationToken cancellationToken = default);
    Task<CustomerBalanceReconciliationDto> ReconcileCustomerBalanceAsync(long customerId, CancellationToken cancellationToken = default);
}

public interface IFinancialLedgerService
{
    Task<FinancialLedgerEntryDto> PostEntryAsync(
        long customerId,
        string entryType,
        string referenceType,
        long referenceId,
        string referenceNumber,
        decimal debitAmount,
        decimal creditAmount,
        long currencyId,
        string description,
        CancellationToken cancellationToken = default);

    Task<PagedResult<FinancialLedgerEntryDto>> GetLedgerEntriesAsync(
        long? customerId,
        string? entryType,
        string? referenceType,
        PagedRequest request,
        CancellationToken cancellationToken = default);
}
