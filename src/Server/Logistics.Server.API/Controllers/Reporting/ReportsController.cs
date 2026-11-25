using FluentValidation;
using Logistics.Server.API.Authorization;
using Logistics.Server.Application.Common.Exceptions;
using Logistics.Server.Application.Common.Models;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.BackgroundProcessing.DTOs;
using Logistics.Server.Application.Modules.BackgroundProcessing.Services;
using Logistics.Server.Application.Modules.Reporting.DTOs;
using Logistics.Server.Application.Modules.Reporting.Services;
using Logistics.Shared.Contracts;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.AspNetCore.Mvc;
using ValidationException = Logistics.Server.Application.Common.Exceptions.ValidationException;

namespace Logistics.Server.API.Controllers.Reporting;

/// <summary>
/// Detailed paginated reports, CSV exports, and scheduled report management endpoints.
/// </summary>
[Route("api/v1/reports")]
public class ReportsController : BaseApiController
{
    private readonly IOperationalReportService _operationalReports;
    private readonly IFinancialReportService _financialReports;
    private readonly IReportExportService _exportService;
    private readonly IScheduledReportService _scheduledReportService;
    private readonly IValidator<ReportFilterRequest> _filterValidator;
    private readonly IValidator<CreateScheduledReportRequest> _createScheduledValidator;
    private readonly IValidator<UpdateScheduledReportRequest> _updateScheduledValidator;

    public ReportsController(
        IOperationalReportService operationalReports,
        IFinancialReportService financialReports,
        IReportExportService exportService,
        IScheduledReportService scheduledReportService,
        IValidator<ReportFilterRequest> filterValidator,
        IValidator<CreateScheduledReportRequest> createScheduledValidator,
        IValidator<UpdateScheduledReportRequest> updateScheduledValidator)
    {
        _operationalReports = operationalReports;
        _financialReports = financialReports;
        _exportService = exportService;
        _scheduledReportService = scheduledReportService;
        _filterValidator = filterValidator;
        _createScheduledValidator = createScheduledValidator;
        _updateScheduledValidator = updateScheduledValidator;
    }

    // ─── Operational Reports ────────────────────────────────────────────────

    [HttpGet("orders")]
    [HasPermission(AppPermissions.Reporting.OrdersRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<OrderReportRowDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrdersReport(
        [FromQuery] PagedReportFilterRequest request,
        CancellationToken cancellationToken)
    {
        await ValidateFilterAsync(request, cancellationToken);
        var result = await _operationalReports.GetOrderReportAsync(request, request.ToPagedRequest(), cancellationToken);
        return Ok(ApiResponse<PagedResponse<OrderReportRowDto>>.Ok(result.ToPagedResponse()));
    }

    [HttpGet("shipments")]
    [HasPermission(AppPermissions.Reporting.ShipmentsRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<ShipmentReportRowDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetShipmentsReport(
        [FromQuery] PagedReportFilterRequest request,
        CancellationToken cancellationToken)
    {
        await ValidateFilterAsync(request, cancellationToken);
        var result = await _operationalReports.GetShipmentReportAsync(request, request.ToPagedRequest(), cancellationToken);
        return Ok(ApiResponse<PagedResponse<ShipmentReportRowDto>>.Ok(result.ToPagedResponse()));
    }

    [HttpGet("delivery-performance")]
    [HasPermission(AppPermissions.Reporting.DeliveryRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<DeliveryPerformanceRowDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDeliveryPerformanceReport(
        [FromQuery] PagedReportFilterRequest request,
        CancellationToken cancellationToken)
    {
        await ValidateFilterAsync(request, cancellationToken);
        var result = await _operationalReports.GetDeliveryPerformanceReportAsync(request, request.ToPagedRequest(), cancellationToken);
        return Ok(ApiResponse<PagedResponse<DeliveryPerformanceRowDto>>.Ok(result.ToPagedResponse()));
    }

    [HttpGet("carrier-performance")]
    [HasPermission(AppPermissions.Reporting.CarrierRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<CarrierPerformanceRowDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCarrierPerformanceReport(
        [FromQuery] PagedReportFilterRequest request,
        CancellationToken cancellationToken)
    {
        await ValidateFilterAsync(request, cancellationToken);
        var result = await _operationalReports.GetCarrierPerformanceReportAsync(request, request.ToPagedRequest(), cancellationToken);
        return Ok(ApiResponse<PagedResponse<CarrierPerformanceRowDto>>.Ok(result.ToPagedResponse()));
    }

    [HttpGet("warehouse-receipts")]
    [HasPermission(AppPermissions.Reporting.WarehouseRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<WarehouseReceiptReportRowDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWarehouseReceiptsReport(
        [FromQuery] PagedReportFilterRequest request,
        CancellationToken cancellationToken)
    {
        await ValidateFilterAsync(request, cancellationToken);
        var result = await _operationalReports.GetWarehouseReceiptsReportAsync(request, request.ToPagedRequest(), cancellationToken);
        return Ok(ApiResponse<PagedResponse<WarehouseReceiptReportRowDto>>.Ok(result.ToPagedResponse()));
    }

    [HttpGet("warehouse-dispatches")]
    [HasPermission(AppPermissions.Reporting.WarehouseRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<WarehouseDispatchReportRowDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWarehouseDispatchesReport(
        [FromQuery] PagedReportFilterRequest request,
        CancellationToken cancellationToken)
    {
        await ValidateFilterAsync(request, cancellationToken);
        var result = await _operationalReports.GetWarehouseDispatchesReportAsync(request, request.ToPagedRequest(), cancellationToken);
        return Ok(ApiResponse<PagedResponse<WarehouseDispatchReportRowDto>>.Ok(result.ToPagedResponse()));
    }

    [HttpGet("parcel-handling")]
    [HasPermission(AppPermissions.Reporting.WarehouseRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<ParcelHandlingReportRowDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetParcelHandlingReport(
        [FromQuery] PagedReportFilterRequest request,
        CancellationToken cancellationToken)
    {
        await ValidateFilterAsync(request, cancellationToken);
        var result = await _operationalReports.GetParcelHandlingReportAsync(request, request.ToPagedRequest(), cancellationToken);
        return Ok(ApiResponse<PagedResponse<ParcelHandlingReportRowDto>>.Ok(result.ToPagedResponse()));
    }

    [HttpGet("inventory-balances")]
    [HasPermission(AppPermissions.Reporting.InventoryRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<InventoryBalanceReportRowDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInventoryBalancesReport(
        [FromQuery] PagedReportFilterRequest request,
        [FromQuery] bool onlyLowStock = false,
        CancellationToken cancellationToken = default)
    {
        await ValidateFilterAsync(request, cancellationToken);
        var result = await _operationalReports.GetInventoryBalancesReportAsync(request, onlyLowStock, request.ToPagedRequest(), cancellationToken);
        return Ok(ApiResponse<PagedResponse<InventoryBalanceReportRowDto>>.Ok(result.ToPagedResponse()));
    }

    [HttpGet("inventory-transactions")]
    [HasPermission(AppPermissions.Reporting.InventoryRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<InventoryTransactionReportRowDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInventoryTransactionsReport(
        [FromQuery] PagedReportFilterRequest request,
        CancellationToken cancellationToken)
    {
        await ValidateFilterAsync(request, cancellationToken);
        var result = await _operationalReports.GetInventoryTransactionsReportAsync(request, request.ToPagedRequest(), cancellationToken);
        return Ok(ApiResponse<PagedResponse<InventoryTransactionReportRowDto>>.Ok(result.ToPagedResponse()));
    }

    // ─── Financial & COD Reports ────────────────────────────────────────────

    [HttpGet("shipping-charges")]
    [HasPermission(AppPermissions.Reporting.FinanceRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<ShippingChargeReportRowDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetShippingChargesReport(
        [FromQuery] PagedReportFilterRequest request,
        [FromQuery] bool? isInvoiced = null,
        CancellationToken cancellationToken = default)
    {
        await ValidateFilterAsync(request, cancellationToken);
        var result = await _financialReports.GetShippingChargesReportAsync(request, isInvoiced, request.ToPagedRequest(), cancellationToken);
        return Ok(ApiResponse<PagedResponse<ShippingChargeReportRowDto>>.Ok(result.ToPagedResponse()));
    }

    [HttpGet("invoices")]
    [HasPermission(AppPermissions.Reporting.FinanceRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<InvoiceReportRowDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInvoicesReport(
        [FromQuery] PagedReportFilterRequest request,
        CancellationToken cancellationToken)
    {
        await ValidateFilterAsync(request, cancellationToken);
        var result = await _financialReports.GetInvoicesReportAsync(request, request.ToPagedRequest(), cancellationToken);
        return Ok(ApiResponse<PagedResponse<InvoiceReportRowDto>>.Ok(result.ToPagedResponse()));
    }

    [HttpGet("payments")]
    [HasPermission(AppPermissions.Reporting.FinanceRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<PaymentReportRowDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaymentsReport(
        [FromQuery] PagedReportFilterRequest request,
        CancellationToken cancellationToken)
    {
        await ValidateFilterAsync(request, cancellationToken);
        var result = await _financialReports.GetPaymentsReportAsync(request, request.ToPagedRequest(), cancellationToken);
        return Ok(ApiResponse<PagedResponse<PaymentReportRowDto>>.Ok(result.ToPagedResponse()));
    }

    [HttpGet("receivables")]
    [HasPermission(AppPermissions.Reporting.FinanceRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<ReceivableReportRowDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReceivablesReport(
        [FromQuery] PagedReportFilterRequest request,
        CancellationToken cancellationToken)
    {
        await ValidateFilterAsync(request, cancellationToken);
        var result = await _financialReports.GetReceivablesReportAsync(request, request.ToPagedRequest(), cancellationToken);
        return Ok(ApiResponse<PagedResponse<ReceivableReportRowDto>>.Ok(result.ToPagedResponse()));
    }

    [HttpGet("cod-collections")]
    [HasPermission(AppPermissions.Reporting.CodRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<CodCollectionReportRowDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCodCollectionsReport(
        [FromQuery] PagedReportFilterRequest request,
        CancellationToken cancellationToken)
    {
        await ValidateFilterAsync(request, cancellationToken);
        var result = await _financialReports.GetCodCollectionsReportAsync(request, request.ToPagedRequest(), cancellationToken);
        return Ok(ApiResponse<PagedResponse<CodCollectionReportRowDto>>.Ok(result.ToPagedResponse()));
    }

    [HttpGet("cod-remittances")]
    [HasPermission(AppPermissions.Reporting.CodRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<CodRemittanceReportRowDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCodRemittancesReport(
        [FromQuery] PagedReportFilterRequest request,
        CancellationToken cancellationToken)
    {
        await ValidateFilterAsync(request, cancellationToken);
        var result = await _financialReports.GetCodRemittancesReportAsync(request, request.ToPagedRequest(), cancellationToken);
        return Ok(ApiResponse<PagedResponse<CodRemittanceReportRowDto>>.Ok(result.ToPagedResponse()));
    }

    [HttpGet("cod-reconciliation")]
    [HasPermission(AppPermissions.Reporting.CodRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<CodReconciliationReportRowDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCodReconciliationReport(
        [FromQuery] PagedReportFilterRequest request,
        CancellationToken cancellationToken)
    {
        await ValidateFilterAsync(request, cancellationToken);
        var result = await _financialReports.GetCodReconciliationReportAsync(request, request.ToPagedRequest(), cancellationToken);
        return Ok(ApiResponse<PagedResponse<CodReconciliationReportRowDto>>.Ok(result.ToPagedResponse()));
    }

    // ─── Export ─────────────────────────────────────────────────────────────

    [HttpGet("export/{reportType}")]
    [HasPermission(AppPermissions.Reporting.Export)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportReport(
        string reportType,
        [FromQuery] ReportFilterRequest filter,
        [FromQuery] string format = "csv",
        CancellationToken cancellationToken = default)
    {
        await ValidateFilterAsync(filter, cancellationToken);
        var exported = await _exportService.ExportAsync(reportType, format, filter, cancellationToken);
        return File(exported.Content, exported.ContentType, exported.SafeFileName);
    }

    // ─── Report Definitions & Scheduled Reports Management ──────────────────

    [HttpGet("definitions")]
    [HasPermission(AppPermissions.Reporting.ScheduleManage)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<ReportDefinitionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReportDefinitions(
        [FromQuery] string? category,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _scheduledReportService.GetDefinitionsAsync(
            category, isActive, new PagedRequest { Page = page, PageSize = pageSize }, cancellationToken);
        return Ok(ApiResponse<PagedResponse<ReportDefinitionDto>>.Ok(result.ToPagedResponse()));
    }

    [HttpGet("scheduled")]
    [HasPermission(AppPermissions.Reporting.ScheduleManage)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<ScheduledReportDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetScheduledReports(
        [FromQuery] long? branchId,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _scheduledReportService.GetScheduledReportsAsync(
            branchId, isActive, new PagedRequest { Page = page, PageSize = pageSize }, cancellationToken);
        return Ok(ApiResponse<PagedResponse<ScheduledReportDto>>.Ok(result.ToPagedResponse()));
    }

    [HttpGet("scheduled/{id:long}")]
    [HasPermission(AppPermissions.Reporting.ScheduleManage)]
    [ProducesResponseType(typeof(ApiResponse<ScheduledReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetScheduledReportById(long id, CancellationToken cancellationToken)
    {
        var report = await _scheduledReportService.GetScheduledReportByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("ScheduledReport", id);
        return Ok(ApiResponse<ScheduledReportDto>.Ok(report));
    }

    [HttpPost("scheduled")]
    [HasPermission(AppPermissions.Reporting.ScheduleManage)]
    [ProducesResponseType(typeof(ApiResponse<ScheduledReportDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateScheduledReport(
        [FromBody] CreateScheduledReportRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _createScheduledValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors.ToDictionary(e => e.PropertyName, e => new[] { e.ErrorMessage }));

        var created = await _scheduledReportService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetScheduledReportById), new { id = created.Id },
            ApiResponse<ScheduledReportDto>.Ok(created, "Scheduled report created."));
    }

    [HttpPut("scheduled/{id:long}")]
    [HasPermission(AppPermissions.Reporting.ScheduleManage)]
    [ProducesResponseType(typeof(ApiResponse<ScheduledReportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateScheduledReport(
        long id,
        [FromBody] UpdateScheduledReportRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _updateScheduledValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors.ToDictionary(e => e.PropertyName, e => new[] { e.ErrorMessage }));

        var updated = await _scheduledReportService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<ScheduledReportDto>.Ok(updated, "Scheduled report updated."));
    }

    [HttpDelete("scheduled/{id:long}")]
    [HasPermission(AppPermissions.Reporting.ScheduleManage)]
    [ProducesResponseType(typeof(ApiResponse<ScheduledReportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeactivateScheduledReport(long id, CancellationToken cancellationToken)
    {
        var updated = await _scheduledReportService.SetActiveStatusAsync(id, false, cancellationToken);
        return Ok(ApiResponse<ScheduledReportDto>.Ok(updated, "Scheduled report deactivated."));
    }

    private async Task ValidateFilterAsync(ReportFilterRequest filter, CancellationToken cancellationToken)
    {
        var result = await _filterValidator.ValidateAsync(filter, cancellationToken);
        if (!result.IsValid)
        {
            throw new ValidationException(result.Errors.ToDictionary(e => e.PropertyName, e => new[] { e.ErrorMessage }));
        }
    }
}
