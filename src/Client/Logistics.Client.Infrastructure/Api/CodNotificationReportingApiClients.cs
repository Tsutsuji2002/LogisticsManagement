using Logistics.Client.Application.Contracts.Cod;
using Logistics.Client.Application.Contracts.Notification;
using Logistics.Client.Application.Contracts.Reporting;
using Logistics.Client.Application.Interfaces;
using Logistics.Shared.Contracts;
using Logistics.Shared.Contracts.Pagination;

namespace Logistics.Client.Infrastructure.Api;

public sealed class CodApiClient(IApiClient api) : ICodApiClient
{
    public async Task<PagedResponse<CodCollectionDto>> GetCollectionsAsync(long? branchId, long? employeeId, long? customerId, string? status, int page, int pageSize, CancellationToken ct = default)
    {
        var url = ApiClientHelpers.BuildUrl("api/v1/cod/collections", ("branchId", branchId), ("employeeId", employeeId), ("customerId", customerId), ("status", status), ("page", page), ("pageSize", pageSize));
        return ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<CodCollectionDto>>>(url, ct), "GET cod/collections");
    }

    public async Task<CodCollectionSummaryDto> GetCollectionSummaryAsync(long? branchId, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<CodCollectionSummaryDto>>(ApiClientHelpers.BuildUrl("api/v1/cod/collections/summary", ("branchId", branchId)), ct), "GET cod summary");

    public async Task<CodCollectionDto> CreateCollectionAsync(CreateCodCollectionRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CreateCodCollectionRequest, ApiResponse<CodCollectionDto>>("api/v1/cod/collections", request, ct), "POST cod collection");

    public async Task<CodCollectionDto> CollectAsync(long id, RecordCodCollectionRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<RecordCodCollectionRequest, ApiResponse<CodCollectionDto>>($"api/v1/cod/collections/{id}/collect", request, ct), "POST cod collect");

    public async Task<CodCollectionDto> DepositAsync(long id, string? notes, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostEmptyAsync<ApiResponse<CodCollectionDto>>(ApiClientHelpers.BuildUrl($"api/v1/cod/collections/{id}/deposit", ("notes", notes)), ct), "POST cod deposit");

    public async Task<CodCollectionDto> VerifyAsync(long id, VerifyCodCollectionRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<VerifyCodCollectionRequest, ApiResponse<CodCollectionDto>>($"api/v1/cod/collections/{id}/verify", request, ct), "POST cod verify");

    public async Task<CodCollectionDto> RecordFailureAsync(long id, RecordCodFailureRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<RecordCodFailureRequest, ApiResponse<CodCollectionDto>>($"api/v1/cod/collections/{id}/record-failure", request, ct), "POST cod failure");

    public async Task<PagedResponse<CodRemittanceDto>> GetRemittancesAsync(long? customerId, string? status, int page, int pageSize, CancellationToken ct = default)
    {
        var url = ApiClientHelpers.BuildUrl("api/v1/cod/remittances", ("customerId", customerId), ("status", status), ("page", page), ("pageSize", pageSize));
        return ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<CodRemittanceDto>>>(url, ct), "GET cod remittances");
    }

    public async Task<CodRemittanceDto> GetRemittanceByIdAsync(long id, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<CodRemittanceDto>>($"api/v1/cod/remittances/{id}", ct), "GET cod remittance");

    public async Task<CodRemittanceDto> CreateRemittanceAsync(CreateCodRemittanceRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CreateCodRemittanceRequest, ApiResponse<CodRemittanceDto>>("api/v1/cod/remittances", request, ct), "POST cod remittance");

    public async Task<CodRemittanceDto> AddRemittanceItemAsync(long remittanceId, AddCodRemittanceItemRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<AddCodRemittanceItemRequest, ApiResponse<CodRemittanceDto>>($"api/v1/cod/remittances/{remittanceId}/items", request, ct), "POST remittance item");

    public async Task<CodRemittanceDto> ConfirmRemittanceAsync(long id, ConfirmCodRemittanceRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<ConfirmCodRemittanceRequest, ApiResponse<CodRemittanceDto>>($"api/v1/cod/remittances/{id}/confirm", request, ct), "POST confirm remittance");

    public async Task<CodRemittanceDto> CancelRemittanceAsync(long id, CancelCodRemittanceRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CancelCodRemittanceRequest, ApiResponse<CodRemittanceDto>>($"api/v1/cod/remittances/{id}/cancel", request, ct), "POST cancel remittance");

    public async Task<CodReconciliationSummaryDto> GetReconciliationSummaryAsync(long? branchId, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<CodReconciliationSummaryDto>>(ApiClientHelpers.BuildUrl("api/v1/cod/reconciliation/summary", ("branchId", branchId)), ct), "GET cod reconciliation summary");

    public async Task<EmployeeCodReconciliationDto> ReconcileEmployeeAsync(long employeeId, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<EmployeeCodReconciliationDto>>($"api/v1/cod/reconciliation/employees/{employeeId}", ct), "GET employee reconciliation");

    public async Task<BranchCodReconciliationDto> ReconcileBranchAsync(long branchId, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<BranchCodReconciliationDto>>($"api/v1/cod/reconciliation/branches/{branchId}", ct), "GET branch reconciliation");
}

public sealed class NotificationApiClient(IApiClient api) : INotificationApiClient
{
    public async Task<PagedResponse<NotificationDto>> GetMyNotificationsAsync(bool? isRead, int page, int pageSize, CancellationToken ct = default)
    {
        var url = ApiClientHelpers.BuildUrl("api/v1/notifications", ("isRead", isRead), ("page", page), ("pageSize", pageSize));
        return ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<NotificationDto>>>(url, ct), "GET notifications");
    }

    public async Task<PagedResponse<NotificationDto>> GetAllNotificationsAsync(long? branchId, bool? isRead, int page, int pageSize, CancellationToken ct = default)
    {
        var url = ApiClientHelpers.BuildUrl("api/v1/notifications/all", ("branchId", branchId), ("isRead", isRead), ("page", page), ("pageSize", pageSize));
        return ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<NotificationDto>>>(url, ct), "GET notifications/all");
    }

    public async Task<UnreadCountDto> GetUnreadCountAsync(CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<UnreadCountDto>>("api/v1/notifications/unread-count", ct), "GET unread-count");

    public async Task<NotificationDto> MarkAsReadAsync(long id, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PatchAsync<object?, ApiResponse<NotificationDto>>($"api/v1/notifications/{id}/read", null, ct), "PATCH mark-read");

    public async Task MarkAllReadAsync(CancellationToken ct = default)
        => await api.PostEmptyAsync<ApiResponse>("api/v1/notifications/mark-all-read", ct);

    public async Task<NotificationDto> SendNotificationAsync(CreateNotificationRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CreateNotificationRequest, ApiResponse<NotificationDto>>("api/v1/notifications", request, ct), "POST notification");

    public async Task<PagedResponse<NotificationTemplateDto>> GetTemplatesAsync(string? search, string? channel, int page, int pageSize, CancellationToken ct = default)
    {
        var url = ApiClientHelpers.BuildUrl("api/v1/notification-templates", ("search", search), ("channel", channel), ("page", page), ("pageSize", pageSize));
        return ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<NotificationTemplateDto>>>(url, ct), "GET templates");
    }

    public async Task<NotificationTemplateDto> CreateTemplateAsync(CreateNotificationTemplateRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CreateNotificationTemplateRequest, ApiResponse<NotificationTemplateDto>>("api/v1/notification-templates", request, ct), "POST template");

    public async Task<NotificationTemplateDto> UpdateTemplateAsync(long id, UpdateNotificationTemplateRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PutAsync<UpdateNotificationTemplateRequest, ApiResponse<NotificationTemplateDto>>($"api/v1/notification-templates/{id}", request, ct), "PUT template");

    public async Task<NotificationTemplateDto> ToggleTemplateStatusAsync(long id, UpdateTemplateStatusRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PatchAsync<UpdateTemplateStatusRequest, ApiResponse<NotificationTemplateDto>>($"api/v1/notification-templates/{id}/status", request, ct), "PATCH template status");
}

public sealed class ReportingApiClient(IApiClient api) : IReportingApiClient
{
    private static string BuildFilterUrl(string path, ReportFilterRequest f, params (string Key, object? Value)[] extra)
    {
        var list = new List<(string Key, object? Value)>
        {
            ("StartDate", f.StartDate),
            ("EndDate", f.EndDate),
            ("TimeZoneId", f.TimeZoneId),
            ("BranchId", f.BranchId),
            ("CustomerId", f.CustomerId),
            ("EmployeeId", f.EmployeeId),
            ("WarehouseId", f.WarehouseId),
            ("CarrierId", f.CarrierId),
            ("Status", f.Status),
            ("Search", f.Search)
        };
        list.AddRange(extra);
        return ApiClientHelpers.BuildUrl(path, [.. list]);
    }

    public async Task<CompanyOverviewDashboardDto> GetCompanyOverviewAsync(ReportFilterRequest filter, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<CompanyOverviewDashboardDto>>(BuildFilterUrl("api/v1/dashboard/overview", filter), ct), "GET dashboard overview");

    public async Task<BranchDashboardDto> GetBranchDashboardAsync(long branchId, ReportFilterRequest filter, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<BranchDashboardDto>>(BuildFilterUrl($"api/v1/dashboard/branches/{branchId}", filter), ct), "GET dashboard branch");

    public async Task<OperationsDashboardDto> GetOperationsDashboardAsync(ReportFilterRequest filter, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<OperationsDashboardDto>>(BuildFilterUrl("api/v1/dashboard/operations", filter), ct), "GET dashboard operations");

    public async Task<ShipmentDashboardDto> GetShipmentDashboardAsync(ReportFilterRequest filter, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<ShipmentDashboardDto>>(BuildFilterUrl("api/v1/dashboard/shipments", filter), ct), "GET dashboard shipments");

    public async Task<DeliveryPerformanceDashboardDto> GetDeliveryPerformanceDashboardAsync(ReportFilterRequest filter, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<DeliveryPerformanceDashboardDto>>(BuildFilterUrl("api/v1/dashboard/delivery-performance", filter), ct), "GET dashboard delivery");

    public async Task<OrdersDashboardDto> GetOrdersDashboardAsync(ReportFilterRequest filter, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<OrdersDashboardDto>>(BuildFilterUrl("api/v1/dashboard/orders", filter), ct), "GET dashboard orders");

    public async Task<WarehouseDashboardDto> GetWarehouseDashboardAsync(ReportFilterRequest filter, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<WarehouseDashboardDto>>(BuildFilterUrl("api/v1/dashboard/warehouse", filter), ct), "GET dashboard warehouse");

    public async Task<InventoryDashboardDto> GetInventoryDashboardAsync(ReportFilterRequest filter, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<InventoryDashboardDto>>(BuildFilterUrl("api/v1/dashboard/inventory", filter), ct), "GET dashboard inventory");

    public async Task<FinanceDashboardDto> GetFinanceDashboardAsync(ReportFilterRequest filter, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<FinanceDashboardDto>>(BuildFilterUrl("api/v1/dashboard/finance", filter), ct), "GET dashboard finance");

    public async Task<CodDashboardDto> GetCodDashboardAsync(ReportFilterRequest filter, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<CodDashboardDto>>(BuildFilterUrl("api/v1/dashboard/cod", filter), ct), "GET dashboard cod");

    public async Task<PagedResponse<OrderReportRowDto>> GetOrderReportAsync(ReportFilterRequest filter, int page, int pageSize, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<OrderReportRowDto>>>(BuildFilterUrl("api/v1/reports/orders", filter, ("Page", page), ("PageSize", pageSize)), ct), "GET reports/orders");

    public async Task<PagedResponse<ShipmentReportRowDto>> GetShipmentReportAsync(ReportFilterRequest filter, int page, int pageSize, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<ShipmentReportRowDto>>>(BuildFilterUrl("api/v1/reports/shipments", filter, ("Page", page), ("PageSize", pageSize)), ct), "GET reports/shipments");

    public async Task<PagedResponse<DeliveryPerformanceRowDto>> GetDeliveryPerformanceReportAsync(ReportFilterRequest filter, int page, int pageSize, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<DeliveryPerformanceRowDto>>>(BuildFilterUrl("api/v1/reports/delivery-performance", filter, ("Page", page), ("PageSize", pageSize)), ct), "GET reports/delivery-performance");

    public async Task<PagedResponse<CarrierPerformanceRowDto>> GetCarrierPerformanceReportAsync(ReportFilterRequest filter, int page, int pageSize, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<CarrierPerformanceRowDto>>>(BuildFilterUrl("api/v1/reports/carrier-performance", filter, ("Page", page), ("PageSize", pageSize)), ct), "GET reports/carrier-performance");

    public async Task<PagedResponse<WarehouseReceiptReportRowDto>> GetWarehouseReceiptsReportAsync(ReportFilterRequest filter, int page, int pageSize, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<WarehouseReceiptReportRowDto>>>(BuildFilterUrl("api/v1/reports/warehouse-receipts", filter, ("Page", page), ("PageSize", pageSize)), ct), "GET reports/warehouse-receipts");

    public async Task<PagedResponse<WarehouseDispatchReportRowDto>> GetWarehouseDispatchesReportAsync(ReportFilterRequest filter, int page, int pageSize, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<WarehouseDispatchReportRowDto>>>(BuildFilterUrl("api/v1/reports/warehouse-dispatches", filter, ("Page", page), ("PageSize", pageSize)), ct), "GET reports/warehouse-dispatches");

    public async Task<PagedResponse<ParcelHandlingReportRowDto>> GetParcelHandlingReportAsync(ReportFilterRequest filter, int page, int pageSize, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<ParcelHandlingReportRowDto>>>(BuildFilterUrl("api/v1/reports/parcel-handling", filter, ("Page", page), ("PageSize", pageSize)), ct), "GET reports/parcel-handling");

    public async Task<PagedResponse<InventoryBalanceReportRowDto>> GetInventoryBalancesReportAsync(ReportFilterRequest filter, bool onlyLowStock, int page, int pageSize, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<InventoryBalanceReportRowDto>>>(BuildFilterUrl("api/v1/reports/inventory-balances", filter, ("onlyLowStock", onlyLowStock), ("Page", page), ("PageSize", pageSize)), ct), "GET reports/inventory-balances");

    public async Task<PagedResponse<InventoryTransactionReportRowDto>> GetInventoryTransactionsReportAsync(ReportFilterRequest filter, int page, int pageSize, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<InventoryTransactionReportRowDto>>>(BuildFilterUrl("api/v1/reports/inventory-transactions", filter, ("Page", page), ("PageSize", pageSize)), ct), "GET reports/inventory-transactions");

    public async Task<PagedResponse<ShippingChargeReportRowDto>> GetShippingChargesReportAsync(ReportFilterRequest filter, bool? isInvoiced, int page, int pageSize, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<ShippingChargeReportRowDto>>>(BuildFilterUrl("api/v1/reports/shipping-charges", filter, ("isInvoiced", isInvoiced), ("Page", page), ("PageSize", pageSize)), ct), "GET reports/shipping-charges");

    public async Task<PagedResponse<InvoiceReportRowDto>> GetInvoicesReportAsync(ReportFilterRequest filter, int page, int pageSize, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<InvoiceReportRowDto>>>(BuildFilterUrl("api/v1/reports/invoices", filter, ("Page", page), ("PageSize", pageSize)), ct), "GET reports/invoices");

    public async Task<PagedResponse<PaymentReportRowDto>> GetPaymentsReportAsync(ReportFilterRequest filter, int page, int pageSize, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<PaymentReportRowDto>>>(BuildFilterUrl("api/v1/reports/payments", filter, ("Page", page), ("PageSize", pageSize)), ct), "GET reports/payments");

    public async Task<PagedResponse<ReceivableReportRowDto>> GetReceivablesReportAsync(ReportFilterRequest filter, int page, int pageSize, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<ReceivableReportRowDto>>>(BuildFilterUrl("api/v1/reports/receivables", filter, ("Page", page), ("PageSize", pageSize)), ct), "GET reports/receivables");

    public async Task<PagedResponse<CodCollectionReportRowDto>> GetCodCollectionsReportAsync(ReportFilterRequest filter, int page, int pageSize, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<CodCollectionReportRowDto>>>(BuildFilterUrl("api/v1/reports/cod-collections", filter, ("Page", page), ("PageSize", pageSize)), ct), "GET reports/cod-collections");

    public async Task<PagedResponse<CodRemittanceReportRowDto>> GetCodRemittancesReportAsync(ReportFilterRequest filter, int page, int pageSize, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<CodRemittanceReportRowDto>>>(BuildFilterUrl("api/v1/reports/cod-remittances", filter, ("Page", page), ("PageSize", pageSize)), ct), "GET reports/cod-remittances");

    public async Task<PagedResponse<CodReconciliationReportRowDto>> GetCodReconciliationReportAsync(ReportFilterRequest filter, int page, int pageSize, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<CodReconciliationReportRowDto>>>(BuildFilterUrl("api/v1/reports/cod-reconciliation", filter, ("Page", page), ("PageSize", pageSize)), ct), "GET reports/cod-reconciliation");

    public async Task<DownloadedFileDto> ExportCsvAsync(string reportType, ReportFilterRequest filter, CancellationToken ct = default)
    {
        var url = BuildFilterUrl($"api/v1/reports/export/{Uri.EscapeDataString(reportType)}", filter, ("format", "csv"));
        var (bytes, contentType, fileName) = await api.GetFileAsync(url, ct);
        return new DownloadedFileDto(bytes, contentType, fileName);
    }

    public async Task<PagedResponse<ReportDefinitionDto>> GetReportDefinitionsAsync(int page, int pageSize, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<ReportDefinitionDto>>>($"api/v1/reports/definitions?page={page}&pageSize={pageSize}", ct), "GET reports/definitions");

    public async Task<PagedResponse<ScheduledReportDto>> GetScheduledReportsAsync(long? branchId, int page, int pageSize, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<ScheduledReportDto>>>(ApiClientHelpers.BuildUrl("api/v1/reports/scheduled", ("branchId", branchId), ("page", page), ("pageSize", pageSize)), ct), "GET reports/scheduled");

    public async Task<ScheduledReportDto> CreateScheduledReportAsync(CreateScheduledReportRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CreateScheduledReportRequest, ApiResponse<ScheduledReportDto>>("api/v1/reports/scheduled", request, ct), "POST reports/scheduled");

    public async Task<ScheduledReportRunResultDto> RunScheduledReportAsync(long id, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostEmptyAsync<ApiResponse<ScheduledReportRunResultDto>>($"api/v1/scheduled-reports/{id}/run", ct), "POST scheduled-reports run");

    public async Task<BackgroundProcessingHealthDto> GetBackgroundHealthAsync(CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<BackgroundProcessingHealthDto>>("api/v1/background-jobs/health", ct), "GET background health");

    public async Task<PagedResponse<BackgroundJobLogDto>> GetBackgroundJobsAsync(string? status, int page, int pageSize, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<BackgroundJobLogDto>>>(ApiClientHelpers.BuildUrl("api/v1/background-jobs", ("status", status), ("page", page), ("pageSize", pageSize)), ct), "GET background jobs");

    public async Task<BackgroundJobLogDto> RetryBackgroundJobAsync(long id, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostEmptyAsync<ApiResponse<BackgroundJobLogDto>>($"api/v1/background-jobs/{id}/retry", ct), "POST retry job");

    public async Task<PagedResponse<OutboxMessageDto>> GetOutboxMessagesAsync(string? status, int page, int pageSize, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<OutboxMessageDto>>>(ApiClientHelpers.BuildUrl("api/v1/background-jobs/outbox", ("status", status), ("page", page), ("pageSize", pageSize)), ct), "GET outbox");

    public async Task RetryOutboxMessageAsync(long id, CancellationToken ct = default)
        => await api.PostEmptyAsync<ApiResponse>($"api/v1/background-jobs/outbox/{id}/retry", ct);
}
