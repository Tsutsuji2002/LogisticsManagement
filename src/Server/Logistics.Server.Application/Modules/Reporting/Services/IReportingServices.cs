using Logistics.Server.Application.Common.Models;
using Logistics.Server.Application.Modules.Reporting.DTOs;
using Logistics.Shared.Contracts.Pagination;

namespace Logistics.Server.Application.Modules.Reporting.Services;

public interface IDashboardQueryService
{
    Task<CompanyOverviewDashboardDto> GetCompanyOverviewAsync(ReportFilterRequest filter, CancellationToken cancellationToken = default);

    Task<BranchDashboardDto> GetBranchDashboardAsync(long branchId, ReportFilterRequest filter, CancellationToken cancellationToken = default);

    Task<OperationsDashboardDto> GetOperationsDashboardAsync(ReportFilterRequest filter, CancellationToken cancellationToken = default);

    Task<ShipmentDashboardDto> GetShipmentDashboardAsync(ReportFilterRequest filter, CancellationToken cancellationToken = default);

    Task<DeliveryPerformanceDashboardDto> GetDeliveryPerformanceDashboardAsync(ReportFilterRequest filter, CancellationToken cancellationToken = default);

    Task<OrdersDashboardDto> GetOrdersDashboardAsync(ReportFilterRequest filter, CancellationToken cancellationToken = default);

    Task<WarehouseDashboardDto> GetWarehouseDashboardAsync(ReportFilterRequest filter, CancellationToken cancellationToken = default);

    Task<InventoryDashboardDto> GetInventoryDashboardAsync(ReportFilterRequest filter, CancellationToken cancellationToken = default);

    Task<FinanceDashboardDto> GetFinanceDashboardAsync(ReportFilterRequest filter, CancellationToken cancellationToken = default);

    Task<CodDashboardDto> GetCodDashboardAsync(ReportFilterRequest filter, CancellationToken cancellationToken = default);
}

public interface IOperationalReportService
{
    Task<PagedResult<OrderReportRowDto>> GetOrderReportAsync(ReportFilterRequest filter, PagedRequest pagedRequest, CancellationToken cancellationToken = default);

    Task<PagedResult<ShipmentReportRowDto>> GetShipmentReportAsync(ReportFilterRequest filter, PagedRequest pagedRequest, CancellationToken cancellationToken = default);

    Task<PagedResult<DeliveryPerformanceRowDto>> GetDeliveryPerformanceReportAsync(ReportFilterRequest filter, PagedRequest pagedRequest, CancellationToken cancellationToken = default);

    Task<PagedResult<CarrierPerformanceRowDto>> GetCarrierPerformanceReportAsync(ReportFilterRequest filter, PagedRequest pagedRequest, CancellationToken cancellationToken = default);

    Task<PagedResult<WarehouseReceiptReportRowDto>> GetWarehouseReceiptsReportAsync(ReportFilterRequest filter, PagedRequest pagedRequest, CancellationToken cancellationToken = default);

    Task<PagedResult<WarehouseDispatchReportRowDto>> GetWarehouseDispatchesReportAsync(ReportFilterRequest filter, PagedRequest pagedRequest, CancellationToken cancellationToken = default);

    Task<PagedResult<ParcelHandlingReportRowDto>> GetParcelHandlingReportAsync(ReportFilterRequest filter, PagedRequest pagedRequest, CancellationToken cancellationToken = default);

    Task<PagedResult<InventoryBalanceReportRowDto>> GetInventoryBalancesReportAsync(ReportFilterRequest filter, bool onlyLowStock, PagedRequest pagedRequest, CancellationToken cancellationToken = default);

    Task<PagedResult<InventoryTransactionReportRowDto>> GetInventoryTransactionsReportAsync(ReportFilterRequest filter, PagedRequest pagedRequest, CancellationToken cancellationToken = default);
}

public interface IFinancialReportService
{
    Task<PagedResult<ShippingChargeReportRowDto>> GetShippingChargesReportAsync(ReportFilterRequest filter, bool? isInvoiced, PagedRequest pagedRequest, CancellationToken cancellationToken = default);

    Task<PagedResult<InvoiceReportRowDto>> GetInvoicesReportAsync(ReportFilterRequest filter, PagedRequest pagedRequest, CancellationToken cancellationToken = default);

    Task<PagedResult<PaymentReportRowDto>> GetPaymentsReportAsync(ReportFilterRequest filter, PagedRequest pagedRequest, CancellationToken cancellationToken = default);

    Task<PagedResult<ReceivableReportRowDto>> GetReceivablesReportAsync(ReportFilterRequest filter, PagedRequest pagedRequest, CancellationToken cancellationToken = default);

    Task<PagedResult<CodCollectionReportRowDto>> GetCodCollectionsReportAsync(ReportFilterRequest filter, PagedRequest pagedRequest, CancellationToken cancellationToken = default);

    Task<PagedResult<CodRemittanceReportRowDto>> GetCodRemittancesReportAsync(ReportFilterRequest filter, PagedRequest pagedRequest, CancellationToken cancellationToken = default);

    Task<PagedResult<CodReconciliationReportRowDto>> GetCodReconciliationReportAsync(ReportFilterRequest filter, PagedRequest pagedRequest, CancellationToken cancellationToken = default);
}

public interface IReportExportService
{
    Task<ReportExportResult> ExportAsync(string reportType, string format, ReportFilterRequest filter, CancellationToken cancellationToken = default);
}
