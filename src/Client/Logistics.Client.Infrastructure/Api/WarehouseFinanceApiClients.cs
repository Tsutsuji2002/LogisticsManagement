using Logistics.Client.Application.Contracts.Finance;
using Logistics.Client.Application.Contracts.Warehouse;
using Logistics.Client.Application.Interfaces;
using Logistics.Shared.Contracts;
using Logistics.Shared.Contracts.Pagination;

namespace Logistics.Client.Infrastructure.Api;

public sealed class WarehouseApiClient(IApiClient api) : IWarehouseApiClient
{
    public async Task<PagedResponse<WarehouseDto>> GetWarehousesAsync(string? search, long? branchId, string? type, bool? isActive, int page, int pageSize, CancellationToken ct = default)
    {
        var url = ApiClientHelpers.BuildUrl("api/v1/warehouses", ("search", search), ("branchId", branchId), ("type", type), ("isActive", isActive), ("page", page), ("pageSize", pageSize));
        return ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<WarehouseDto>>>(url, ct), "GET warehouses");
    }

    public async Task<WarehouseDetailDto> GetWarehouseDetailAsync(string id, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<WarehouseDetailDto>>($"api/v1/warehouses/{Uri.EscapeDataString(id)}", ct), "GET warehouse detail");

    public async Task<WarehouseDetailDto> CreateWarehouseAsync(CreateWarehouseRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CreateWarehouseRequest, ApiResponse<WarehouseDetailDto>>("api/v1/warehouses", request, ct), "POST warehouse");

    public async Task<WarehouseDetailDto> ToggleWarehouseStatusAsync(string id, bool isActive, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PatchAsync<object?, ApiResponse<WarehouseDetailDto>>($"api/v1/warehouses/{Uri.EscapeDataString(id)}/status?isActive={isActive.ToString().ToLowerInvariant()}", null, ct), "PATCH warehouse status");

    public async Task<IReadOnlyList<WarehouseZoneDto>> GetZonesAsync(string warehouseId, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<IReadOnlyList<WarehouseZoneDto>>>($"api/v1/warehouses/{Uri.EscapeDataString(warehouseId)}/zones", ct), "GET zones");

    public async Task<WarehouseZoneDto> CreateZoneAsync(string warehouseId, CreateWarehouseZoneRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CreateWarehouseZoneRequest, ApiResponse<WarehouseZoneDto>>($"api/v1/warehouses/{Uri.EscapeDataString(warehouseId)}/zones", request, ct), "POST zone");

    public async Task<PagedResponse<WarehouseLocationDto>> GetLocationsAsync(string warehouseId, long? zoneId, int page, int pageSize, CancellationToken ct = default)
    {
        var url = ApiClientHelpers.BuildUrl($"api/v1/warehouses/{Uri.EscapeDataString(warehouseId)}/locations", ("zoneId", zoneId), ("page", page), ("pageSize", pageSize));
        return ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<WarehouseLocationDto>>>(url, ct), "GET locations");
    }

    public async Task<WarehouseLocationDto> CreateLocationAsync(string warehouseId, long zoneId, CreateWarehouseLocationRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CreateWarehouseLocationRequest, ApiResponse<WarehouseLocationDto>>($"api/v1/warehouses/{Uri.EscapeDataString(warehouseId)}/zones/{zoneId}/locations", request, ct), "POST location");

    public async Task<WarehouseLocationDto> ToggleLocationBlockAsync(string warehouseId, long locationId, bool isBlocked, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PatchAsync<object?, ApiResponse<WarehouseLocationDto>>($"api/v1/warehouses/{Uri.EscapeDataString(warehouseId)}/locations/{locationId}/block?isBlocked={isBlocked.ToString().ToLowerInvariant()}", null, ct), "PATCH location block");

    public async Task<ScanPackageResultDto> ScanParcelAsync(ScanPackageRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<ScanPackageRequest, ApiResponse<ScanPackageResultDto>>("api/v1/parcel-handling/scan", request, ct), "POST parcel scan");

    public async Task<ParcelHandlingEventDto> ReceiveParcelAsync(ParcelReceiveRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<ParcelReceiveRequest, ApiResponse<ParcelHandlingEventDto>>("api/v1/parcel-handling/receive", request, ct), "POST parcel receive");

    public async Task<ParcelHandlingEventDto> SortParcelAsync(ParcelSortRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<ParcelSortRequest, ApiResponse<ParcelHandlingEventDto>>("api/v1/parcel-handling/sort", request, ct), "POST parcel sort");

    public async Task<ParcelHandlingEventDto> StageParcelAsync(ParcelStageRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<ParcelStageRequest, ApiResponse<ParcelHandlingEventDto>>("api/v1/parcel-handling/stage", request, ct), "POST parcel stage");

    public async Task<ParcelHandlingEventDto> RecordParcelExceptionAsync(ParcelExceptionRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<ParcelExceptionRequest, ApiResponse<ParcelHandlingEventDto>>("api/v1/parcel-handling/exception", request, ct), "POST parcel exception");

    public async Task<PagedResponse<ParcelHandlingEventDto>> GetParcelEventsAsync(long? warehouseId, long? shipmentId, string? eventType, int page, int pageSize, CancellationToken ct = default)
    {
        var url = ApiClientHelpers.BuildUrl("api/v1/parcel-handling/events", ("warehouseId", warehouseId), ("shipmentId", shipmentId), ("eventType", eventType), ("page", page), ("pageSize", pageSize));
        return ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<ParcelHandlingEventDto>>>(url, ct), "GET parcel events");
    }

    public async Task<PagedResponse<WarehouseDispatchDto>> GetDispatchesAsync(long? warehouseId, string? status, int page, int pageSize, CancellationToken ct = default)
    {
        var url = ApiClientHelpers.BuildUrl("api/v1/warehouse-dispatches", ("warehouseId", warehouseId), ("status", status), ("page", page), ("pageSize", pageSize));
        return ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<WarehouseDispatchDto>>>(url, ct), "GET dispatches");
    }

    public async Task<WarehouseDispatchDto> CreateDispatchAsync(CreateWarehouseDispatchDto request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CreateWarehouseDispatchDto, ApiResponse<WarehouseDispatchDto>>("api/v1/warehouse-dispatches", request, ct), "POST dispatch");

    public async Task<WarehouseDispatchDto> ConfirmDispatchAsync(long id, long employeeId, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostEmptyAsync<ApiResponse<WarehouseDispatchDto>>($"api/v1/warehouse-dispatches/{id}/confirm?employeeId={employeeId}", ct), "POST confirm dispatch");
}

public sealed class InventoryApiClient(IApiClient api) : IInventoryApiClient
{
    public async Task<PagedResponse<InventoryItemDto>> GetItemsAsync(string? search, long? warehouseId, int page, int pageSize, CancellationToken ct = default)
    {
        var url = ApiClientHelpers.BuildUrl("api/v1/inventory/items", ("search", search), ("warehouseId", warehouseId), ("page", page), ("pageSize", pageSize));
        return ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<InventoryItemDto>>>(url, ct), "GET inventory items");
    }

    public async Task<InventoryItemDto> CreateItemAsync(CreateInventoryItemRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CreateInventoryItemRequest, ApiResponse<InventoryItemDto>>("api/v1/inventory/items", request, ct), "POST inventory item");

    public async Task<PagedResponse<InventoryBalanceDto>> GetBalancesAsync(long? warehouseId, long? productId, long? locationId, int page, int pageSize, CancellationToken ct = default)
    {
        var url = ApiClientHelpers.BuildUrl("api/v1/inventory/balances", ("warehouseId", warehouseId), ("productId", productId), ("locationId", locationId), ("page", page), ("pageSize", pageSize));
        return ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<InventoryBalanceDto>>>(url, ct), "GET inventory balances");
    }

    public async Task<PagedResponse<InventoryTransactionDto>> GetTransactionsAsync(long? warehouseId, string? transactionType, int page, int pageSize, CancellationToken ct = default)
    {
        var url = ApiClientHelpers.BuildUrl("api/v1/inventory/transactions", ("warehouseId", warehouseId), ("transactionType", transactionType), ("page", page), ("pageSize", pageSize));
        return ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<InventoryTransactionDto>>>(url, ct), "GET inventory transactions");
    }

    public async Task<PagedResponse<WarehouseReceiptDto>> GetReceiptsAsync(long? warehouseId, string? status, int page, int pageSize, CancellationToken ct = default)
    {
        var url = ApiClientHelpers.BuildUrl("api/v1/inventory/receipts", ("warehouseId", warehouseId), ("status", status), ("page", page), ("pageSize", pageSize));
        return ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<WarehouseReceiptDto>>>(url, ct), "GET receipts");
    }

    public async Task<WarehouseReceiptDto> CreateReceiptAsync(CreateWarehouseReceiptRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CreateWarehouseReceiptRequest, ApiResponse<WarehouseReceiptDto>>("api/v1/inventory/receipts", request, ct), "POST receipt");

    public async Task<WarehouseReceiptDto> ConfirmReceiptAsync(long id, ConfirmReceiptRequest? request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<ConfirmReceiptRequest?, ApiResponse<WarehouseReceiptDto>>($"api/v1/inventory/receipts/{id}/confirm", request, ct), "POST confirm receipt");

    public async Task<PagedResponse<StockAdjustmentDto>> GetAdjustmentsAsync(long? warehouseId, string? status, int page, int pageSize, CancellationToken ct = default)
    {
        var url = ApiClientHelpers.BuildUrl("api/v1/inventory/adjustments", ("warehouseId", warehouseId), ("status", status), ("page", page), ("pageSize", pageSize));
        return ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<StockAdjustmentDto>>>(url, ct), "GET adjustments");
    }

    public async Task<StockAdjustmentDto> CreateAdjustmentAsync(CreateStockAdjustmentRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CreateStockAdjustmentRequest, ApiResponse<StockAdjustmentDto>>("api/v1/inventory/adjustments", request, ct), "POST adjustment");

    public async Task<StockAdjustmentDto> ApproveAdjustmentAsync(long id, long employeeId, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostEmptyAsync<ApiResponse<StockAdjustmentDto>>($"api/v1/inventory/adjustments/{id}/approve?employeeId={employeeId}", ct), "POST approve adjustment");

    public async Task<PagedResponse<StockTransferDto>> GetTransfersAsync(long? sourceWarehouseId, long? destinationWarehouseId, string? status, int page, int pageSize, CancellationToken ct = default)
    {
        var url = ApiClientHelpers.BuildUrl("api/v1/inventory/transfers", ("sourceWarehouseId", sourceWarehouseId), ("destinationWarehouseId", destinationWarehouseId), ("status", status), ("page", page), ("pageSize", pageSize));
        return ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<StockTransferDto>>>(url, ct), "GET transfers");
    }

    public async Task<StockTransferDto> CreateTransferAsync(CreateStockTransferRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CreateStockTransferRequest, ApiResponse<StockTransferDto>>("api/v1/inventory/transfers", request, ct), "POST transfer");

    public async Task<StockTransferDto> ConfirmTransferAsync(long id, long employeeId, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostEmptyAsync<ApiResponse<StockTransferDto>>($"api/v1/inventory/transfers/{id}/confirm?employeeId={employeeId}", ct), "POST confirm transfer");

    public async Task<StockTransferDto> ReceiveTransferAsync(long id, ReceiveStockTransferRequest request, long employeeId, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<ReceiveStockTransferRequest, ApiResponse<StockTransferDto>>($"api/v1/inventory/transfers/{id}/receive?employeeId={employeeId}", request, ct), "POST receive transfer");

    public async Task<PagedResponse<InventoryReservationDto>> GetReservationsAsync(long? warehouseId, string? status, int page, int pageSize, CancellationToken ct = default)
    {
        var url = ApiClientHelpers.BuildUrl("api/v1/inventory/reservations", ("warehouseId", warehouseId), ("status", status), ("page", page), ("pageSize", pageSize));
        return ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<InventoryReservationDto>>>(url, ct), "GET reservations");
    }

    public async Task<InventoryReservationDto> CreateReservationAsync(CreateInventoryReservationRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CreateInventoryReservationRequest, ApiResponse<InventoryReservationDto>>("api/v1/inventory/reservations", request, ct), "POST reservation");

    public async Task<InventoryReservationDto> ReleaseReservationAsync(long id, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostEmptyAsync<ApiResponse<InventoryReservationDto>>($"api/v1/inventory/reservations/{id}/release", ct), "POST release reservation");

    public async Task<InventoryReservationDto> ConsumeReservationAsync(long id, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostEmptyAsync<ApiResponse<InventoryReservationDto>>($"api/v1/inventory/reservations/{id}/consume", ct), "POST consume reservation");

    public async Task<PagedResponse<InventoryLotDto>> GetLotsAsync(long? productId, int page, int pageSize, CancellationToken ct = default)
    {
        var url = ApiClientHelpers.BuildUrl("api/v1/inventory/lots", ("productId", productId), ("page", page), ("pageSize", pageSize));
        return ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<InventoryLotDto>>>(url, ct), "GET lots");
    }

    public async Task<InventoryLotDto> CreateLotAsync(CreateInventoryLotRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CreateInventoryLotRequest, ApiResponse<InventoryLotDto>>("api/v1/inventory/lots", request, ct), "POST lot");

    public async Task<PagedResponse<InventorySerialNumberDto>> GetSerialNumbersAsync(long? warehouseId, long? productId, int page, int pageSize, CancellationToken ct = default)
    {
        var url = ApiClientHelpers.BuildUrl("api/v1/inventory/serial-numbers", ("warehouseId", warehouseId), ("productId", productId), ("page", page), ("pageSize", pageSize));
        return ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<InventorySerialNumberDto>>>(url, ct), "GET serial-numbers");
    }

    public async Task<InventorySerialNumberDto> CreateSerialNumberAsync(CreateInventorySerialNumberRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CreateInventorySerialNumberRequest, ApiResponse<InventorySerialNumberDto>>("api/v1/inventory/serial-numbers", request, ct), "POST serial-number");
}

public sealed class FinanceApiClient(IApiClient api) : IFinanceApiClient
{
    public async Task<PagedResponse<ShippingRateCardDto>> GetRateCardsAsync(string? search, bool? isActive, int page, int pageSize, CancellationToken ct = default)
    {
        var url = ApiClientHelpers.BuildUrl("api/v1/finance/rate-cards", ("search", search), ("isActive", isActive), ("page", page), ("pageSize", pageSize));
        return ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<ShippingRateCardDto>>>(url, ct), "GET rate-cards");
    }

    public async Task<ShippingRateCardDto> CreateRateCardAsync(CreateShippingRateCardRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CreateShippingRateCardRequest, ApiResponse<ShippingRateCardDto>>("api/v1/finance/rate-cards", request, ct), "POST rate-card");

    public async Task<ShippingRateCardDto> UpdateRateCardAsync(long id, UpdateShippingRateCardRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PutAsync<UpdateShippingRateCardRequest, ApiResponse<ShippingRateCardDto>>($"api/v1/finance/rate-cards/{id}", request, ct), "PUT rate-card");

    public async Task<ShippingRateCardDto> ToggleRateCardStatusAsync(long id, bool isActive, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PatchAsync<object?, ApiResponse<ShippingRateCardDto>>($"api/v1/finance/rate-cards/{id}/status?isActive={isActive.ToString().ToLowerInvariant()}", null, ct), "PATCH rate-card status");

    public async Task<IReadOnlyList<ShippingRateRuleDto>> GetRateRulesAsync(long rateCardId, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<IReadOnlyList<ShippingRateRuleDto>>>($"api/v1/finance/rate-cards/{rateCardId}/rules", ct), "GET rate-rules");

    public async Task<ShippingRateRuleDto> CreateRateRuleAsync(long rateCardId, CreateShippingRateRuleRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CreateShippingRateRuleRequest, ApiResponse<ShippingRateRuleDto>>($"api/v1/finance/rate-cards/{rateCardId}/rules", request, ct), "POST rate-rule");

    public async Task<ShippingFeeCalculationResultDto> CalculateShippingFeeAsync(CalculateShippingFeeRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CalculateShippingFeeRequest, ApiResponse<ShippingFeeCalculationResultDto>>("api/v1/finance/shipping-fees/calculate", request, ct), "POST calculate shipping fee");

    public async Task<PagedResponse<ShipmentChargeDto>> GetChargesAsync(long? shipmentId, long? customerId, bool? isInvoiced, int page, int pageSize, CancellationToken ct = default)
    {
        var url = ApiClientHelpers.BuildUrl("api/v1/finance/shipping-fees/charges", ("shipmentId", shipmentId), ("customerId", customerId), ("isInvoiced", isInvoiced), ("page", page), ("pageSize", pageSize));
        return ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<ShipmentChargeDto>>>(url, ct), "GET charges");
    }

    public async Task<ShipmentChargeDto> CreateChargeAdjustmentAsync(long shipmentId, CreateChargeAdjustmentRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CreateChargeAdjustmentRequest, ApiResponse<ShipmentChargeDto>>($"api/v1/finance/shipping-fees/charges/{shipmentId}/adjustments", request, ct), "POST charge adjustment");

    public async Task<PagedResponse<InvoiceDto>> GetInvoicesAsync(long? customerId, string? status, int page, int pageSize, CancellationToken ct = default)
    {
        var url = ApiClientHelpers.BuildUrl("api/v1/finance/invoices", ("customerId", customerId), ("status", status), ("page", page), ("pageSize", pageSize));
        return ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<InvoiceDto>>>(url, ct), "GET invoices");
    }

    public async Task<InvoiceDto> GetInvoiceByIdAsync(long id, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<InvoiceDto>>($"api/v1/finance/invoices/{id}", ct), "GET invoice");

    public async Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CreateInvoiceRequest, ApiResponse<InvoiceDto>>("api/v1/finance/invoices", request, ct), "POST invoice");

    public async Task<InvoiceDto> IssueInvoiceAsync(long id, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostEmptyAsync<ApiResponse<InvoiceDto>>($"api/v1/finance/invoices/{id}/issue", ct), "POST issue invoice");

    public async Task<InvoiceDto> CancelInvoiceAsync(long id, CancelInvoiceRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CancelInvoiceRequest, ApiResponse<InvoiceDto>>($"api/v1/finance/invoices/{id}/cancel", request, ct), "POST cancel invoice");

    public async Task<PagedResponse<PaymentDto>> GetPaymentsAsync(long? customerId, string? status, int page, int pageSize, CancellationToken ct = default)
    {
        var url = ApiClientHelpers.BuildUrl("api/v1/finance/payments", ("customerId", customerId), ("status", status), ("page", page), ("pageSize", pageSize));
        return ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<PaymentDto>>>(url, ct), "GET payments");
    }

    public async Task<PaymentDto> RegisterPaymentAsync(RegisterPaymentRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<RegisterPaymentRequest, ApiResponse<PaymentDto>>("api/v1/finance/payments", request, ct), "POST payment");

    public async Task<PaymentDto> AllocatePaymentAsync(long paymentId, CreatePaymentAllocationRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CreatePaymentAllocationRequest, ApiResponse<PaymentDto>>($"api/v1/finance/payments/{paymentId}/allocations", request, ct), "POST payment allocation");

    public async Task<PaymentDto> ReverseAllocationAsync(long paymentId, long allocationId, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostEmptyAsync<ApiResponse<PaymentDto>>($"api/v1/finance/payments/{paymentId}/allocations/{allocationId}/reverse", ct), "POST reverse allocation");

    public async Task<PagedResponse<CustomerBalanceDto>> GetCustomerBalancesAsync(long? branchId, int page, int pageSize, CancellationToken ct = default)
    {
        var url = ApiClientHelpers.BuildUrl("api/v1/finance/customer-balances", ("branchId", branchId), ("page", page), ("pageSize", pageSize));
        return ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<CustomerBalanceDto>>>(url, ct), "GET customer-balances");
    }

    public async Task<CustomerBalanceReconciliationDto> ReconcileCustomerBalanceAsync(long customerId, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostEmptyAsync<ApiResponse<CustomerBalanceReconciliationDto>>($"api/v1/finance/customer-balances/{customerId}/reconcile", ct), "POST reconcile balance");

    public async Task<PagedResponse<FinancialLedgerEntryDto>> GetLedgerEntriesAsync(long? customerId, string? entryType, int page, int pageSize, CancellationToken ct = default)
    {
        var url = ApiClientHelpers.BuildUrl("api/v1/finance/ledger/entries", ("customerId", customerId), ("entryType", entryType), ("page", page), ("pageSize", pageSize));
        return ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<FinancialLedgerEntryDto>>>(url, ct), "GET ledger entries");
    }
}
