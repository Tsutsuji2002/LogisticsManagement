using Logistics.Server.Application.Common.Exceptions;
using Logistics.Server.Application.Common.Interfaces;
using Logistics.Server.Application.Common.Models;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.BackgroundProcessing.DTOs;
using Logistics.Server.Application.Modules.BackgroundProcessing.Services;
using Logistics.Server.Application.Modules.Identity.Services;
using Logistics.Server.Application.Modules.Reporting.DTOs;
using Logistics.Server.Application.Modules.Reporting.Services;
using Logistics.Server.Infrastructure.Persistence;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Logistics.Server.Infrastructure.Services.Reporting;

public sealed class ReportExportService : IReportExportService
{
    private static readonly PagedRequest ExportPageRequest = new() { Page = 1, PageSize = ReportingQueryHelpers.MaxExportRows };

    private readonly IOperationalReportService _operationalReports;
    private readonly IFinancialReportService _financialReports;
    private readonly ICurrentUserService _currentUser;
    private readonly IPermissionService _permissionService;
    private readonly IAuditService _auditService;

    public ReportExportService(
        IOperationalReportService operationalReports,
        IFinancialReportService financialReports,
        ICurrentUserService currentUser,
        IPermissionService permissionService,
        IAuditService auditService)
    {
        _operationalReports = operationalReports;
        _financialReports = financialReports;
        _currentUser = currentUser;
        _permissionService = permissionService;
        _auditService = auditService;
    }

    public async Task<ReportExportResult> ExportAsync(
        string reportType,
        string format,
        ReportFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reportType))
            throw new ValidationException("ReportType", "Report type is required.");

        if (!string.Equals(format?.Trim(), "csv", StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException(
                "Format",
                $"Export format '{format}' is not supported. Only 'csv' is currently supported.");
        }

        if (!_currentUser.UserId.HasValue)
            throw new ForbiddenAccessException("User authentication is required to export reports.");

        var normalizedType = reportType.Trim().ToLowerInvariant();
        var requiredPermission = ResolveRequiredReadPermission(normalizedType);

        if (!await _permissionService.HasPermissionAsync(_currentUser.UserId.Value, AppPermissions.Reporting.Export, cancellationToken) ||
            !await _permissionService.HasPermissionAsync(_currentUser.UserId.Value, requiredPermission, cancellationToken))
        {
            throw new ForbiddenAccessException($"Missing permission '{AppPermissions.Reporting.Export}' or '{requiredPermission}'.");
        }

        var (bytes, rowCount) = await GenerateCsvPayloadAsync(normalizedType, filter, cancellationToken);
        var fileName = ReportingQueryHelpers.SanitizeFileName($"{normalizedType}-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");

        await _auditService.RecordAsync(
            "Export",
            "report",
            normalizedType,
            normalizedType,
            null,
            new
            {
                Format = "csv",
                RowCount = rowCount,
                filter.BranchId,
                filter.StartDate,
                filter.EndDate,
                filter.TimeZoneId
            },
            $"Exported {rowCount} row(s) of {normalizedType} as CSV.",
            cancellationToken);

        return new ReportExportResult(bytes, "text/csv; charset=utf-8", fileName);
    }

    private static string ResolveRequiredReadPermission(string normalizedType) => normalizedType switch
    {
        "orders" => AppPermissions.Reporting.OrdersRead,
        "shipments" => AppPermissions.Reporting.ShipmentsRead,
        "delivery-performance" => AppPermissions.Reporting.DeliveryRead,
        "carrier-performance" => AppPermissions.Reporting.CarrierRead,
        "warehouse-receipts" or "warehouse-dispatches" or "parcel-handling" => AppPermissions.Reporting.WarehouseRead,
        "inventory-balances" or "inventory-transactions" => AppPermissions.Reporting.InventoryRead,
        "shipping-charges" or "invoices" or "payments" or "receivables" => AppPermissions.Reporting.FinanceRead,
        "cod-collections" or "cod-remittances" or "cod-reconciliation" => AppPermissions.Reporting.CodRead,
        _ => throw new ValidationException("ReportType", $"Unknown report type '{normalizedType}'.")
    };

    private async Task<(byte[] Bytes, int RowCount)> GenerateCsvPayloadAsync(
        string normalizedType,
        ReportFilterRequest filter,
        CancellationToken ct)
    {
        switch (normalizedType)
        {
            case "orders":
            {
                var page = await _operationalReports.GetOrderReportAsync(filter, ExportPageRequest, ct);
                var bytes = ReportingQueryHelpers.BuildCsv(page.Items, [
                    ("OrderId", r => r.OrderId),
                    ("OrderNumber", r => r.OrderNumber),
                    ("OrderDateUtc", r => r.OrderDate),
                    ("OrderStatus", r => r.OrderStatus),
                    ("PaymentStatus", r => r.PaymentStatus),
                    ("BranchCode", r => r.BranchCode),
                    ("CustomerName", r => r.CustomerName),
                    ("ItemsCount", r => r.ItemsCount),
                    ("PackagesCount", r => r.PackagesCount),
                    ("TotalWeightKg", r => r.TotalWeightKg),
                    ("DeclaredValue", r => r.DeclaredValue),
                    ("CodAmount", r => r.CodAmount)
                ]);
                return (bytes, page.Items.Count);
            }
            case "shipments":
            {
                var page = await _operationalReports.GetShipmentReportAsync(filter, ExportPageRequest, ct);
                var bytes = ReportingQueryHelpers.BuildCsv(page.Items, [
                    ("ShipmentId", r => r.ShipmentId),
                    ("TrackingNumber", r => r.TrackingNumber),
                    ("OrderNumber", r => r.OrderNumber),
                    ("ShipmentStatus", r => r.ShipmentStatus),
                    ("ServiceType", r => r.ServiceType),
                    ("OriginBranchCode", r => r.OriginBranchCode),
                    ("DestinationBranchCode", r => r.DestinationBranchCode),
                    ("CustomerName", r => r.CustomerName),
                    ("CarrierCode", r => r.CarrierCode),
                    ("ChargeableWeightKg", r => r.ChargeableWeightKg),
                    ("CodAmount", r => r.CodAmount),
                    ("TotalAmount", r => r.TotalAmount),
                    ("CreatedAtUtc", r => r.CreatedAt),
                    ("EstimatedDeliveryDateUtc", r => r.EstimatedDeliveryDate),
                    ("ActualDeliveryDateUtc", r => r.ActualDeliveryDate)
                ]);
                return (bytes, page.Items.Count);
            }
            case "delivery-performance":
            {
                var page = await _operationalReports.GetDeliveryPerformanceReportAsync(filter, ExportPageRequest, ct);
                var bytes = ReportingQueryHelpers.BuildCsv(page.Items, [
                    ("ShipmentId", r => r.ShipmentId),
                    ("TrackingNumber", r => r.TrackingNumber),
                    ("DestinationBranchCode", r => r.DestinationBranchCode),
                    ("CarrierCode", r => r.CarrierCode),
                    ("ShipmentStatus", r => r.ShipmentStatus),
                    ("AttemptCount", r => r.AttemptCount),
                    ("PickupDateUtc", r => r.PickupDate),
                    ("EstimatedDeliveryDateUtc", r => r.EstimatedDeliveryDate),
                    ("ActualDeliveryDateUtc", r => r.ActualDeliveryDate),
                    ("DeliveryDurationHours", r => r.DeliveryDurationHours),
                    ("IsOnTime", r => r.IsOnTime)
                ]);
                return (bytes, page.Items.Count);
            }
            case "carrier-performance":
            {
                var page = await _operationalReports.GetCarrierPerformanceReportAsync(filter, ExportPageRequest, ct);
                var bytes = ReportingQueryHelpers.BuildCsv(page.Items, [
                    ("CarrierId", r => r.CarrierId),
                    ("CarrierCode", r => r.CarrierCode),
                    ("CarrierName", r => r.CarrierName),
                    ("TotalShipments", r => r.TotalShipments),
                    ("DeliveredShipments", r => r.DeliveredShipments),
                    ("FailedOrReturnedShipments", r => r.FailedOrReturnedShipments),
                    ("ApiCallsTotal", r => r.ApiCallsTotal),
                    ("ApiCallsSucceeded", r => r.ApiCallsSucceeded),
                    ("ApiCallsFailed", r => r.ApiCallsFailed),
                    ("WebhooksTotal", r => r.WebhooksTotal),
                    ("WebhooksSucceeded", r => r.WebhooksSucceeded),
                    ("SyncJobsTotal", r => r.SyncJobsTotal),
                    ("SyncJobsFailed", r => r.SyncJobsFailed),
                    ("TrackingEventsTotal", r => r.TrackingEventsTotal),
                    ("TrackingEventsFailed", r => r.TrackingEventsFailed)
                ]);
                return (bytes, page.Items.Count);
            }
            case "warehouse-receipts":
            {
                var page = await _operationalReports.GetWarehouseReceiptsReportAsync(filter, ExportPageRequest, ct);
                var bytes = ReportingQueryHelpers.BuildCsv(page.Items, [
                    ("ReceiptId", r => r.ReceiptId),
                    ("ReceiptNumber", r => r.ReceiptNumber),
                    ("WarehouseCode", r => r.WarehouseCode),
                    ("ReceiptType", r => r.ReceiptType),
                    ("Status", r => r.Status),
                    ("ActualArrivalDateUtc", r => r.ActualArrivalDate),
                    ("LineItemsCount", r => r.LineItemsCount),
                    ("TotalReceivedQuantity", r => r.TotalReceivedQuantity),
                    ("TotalDamagedQuantity", r => r.TotalDamagedQuantity)
                ]);
                return (bytes, page.Items.Count);
            }
            case "warehouse-dispatches":
            {
                var page = await _operationalReports.GetWarehouseDispatchesReportAsync(filter, ExportPageRequest, ct);
                var bytes = ReportingQueryHelpers.BuildCsv(page.Items, [
                    ("DispatchId", r => r.DispatchId),
                    ("DispatchNumber", r => r.DispatchNumber),
                    ("WarehouseCode", r => r.WarehouseCode),
                    ("DispatchType", r => r.DispatchType),
                    ("Status", r => r.Status),
                    ("DispatchedAtUtc", r => r.DispatchedAt),
                    ("LineItemsCount", r => r.LineItemsCount),
                    ("PackagesCount", r => r.PackagesCount)
                ]);
                return (bytes, page.Items.Count);
            }
            case "parcel-handling":
            {
                var page = await _operationalReports.GetParcelHandlingReportAsync(filter, ExportPageRequest, ct);
                var bytes = ReportingQueryHelpers.BuildCsv(page.Items, [
                    ("EventId", r => r.EventId),
                    ("TrackingNumber", r => r.TrackingNumber),
                    ("PackageBarcode", r => r.PackageBarcode),
                    ("ShipmentStatus", r => r.ShipmentStatus),
                    ("WarehouseCode", r => r.WarehouseCode),
                    ("EventType", r => r.EventType),
                    ("EmployeeName", r => r.EmployeeName),
                    ("ScannedAtUtc", r => r.ScannedAt)
                ]);
                return (bytes, page.Items.Count);
            }
            case "inventory-balances":
            {
                var page = await _operationalReports.GetInventoryBalancesReportAsync(filter, false, ExportPageRequest, ct);
                var bytes = ReportingQueryHelpers.BuildCsv(page.Items, [
                    ("BalanceId", r => r.BalanceId),
                    ("WarehouseCode", r => r.WarehouseCode),
                    ("LocationCode", r => r.LocationCode),
                    ("ProductSku", r => r.ProductSku),
                    ("ProductName", r => r.ProductName),
                    ("LotNumber", r => r.LotNumber),
                    ("LotExpiryDate", r => r.LotExpiryDate),
                    ("QuantityOnHand", r => r.QuantityOnHand),
                    ("QuantityAllocated", r => r.QuantityAllocated),
                    ("QuantityAvailable", r => r.QuantityAvailable),
                    ("QuantityDamaged", r => r.QuantityDamaged),
                    ("ReorderPoint", r => r.ReorderPoint),
                    ("IsBelowReorderPoint", r => r.IsBelowReorderPoint)
                ]);
                return (bytes, page.Items.Count);
            }
            case "inventory-transactions":
            {
                var page = await _operationalReports.GetInventoryTransactionsReportAsync(filter, ExportPageRequest, ct);
                var bytes = ReportingQueryHelpers.BuildCsv(page.Items, [
                    ("TransactionId", r => r.TransactionId),
                    ("TransactionNumber", r => r.TransactionNumber),
                    ("WarehouseCode", r => r.WarehouseCode),
                    ("TransactionType", r => r.TransactionType),
                    ("ReferenceType", r => r.ReferenceType),
                    ("ReferenceNumber", r => r.ReferenceNumber),
                    ("TotalQuantity", r => r.TotalQuantity),
                    ("TransactionDateUtc", r => r.TransactionDate)
                ]);
                return (bytes, page.Items.Count);
            }
            case "shipping-charges":
            {
                var page = await _financialReports.GetShippingChargesReportAsync(filter, null, ExportPageRequest, ct);
                var bytes = ReportingQueryHelpers.BuildCsv(page.Items, [
                    ("ChargeId", r => r.ChargeId),
                    ("ShipmentId", r => r.ShipmentId),
                    ("TrackingNumber", r => r.TrackingNumber),
                    ("CustomerName", r => r.CustomerName),
                    ("ChargeType", r => r.ChargeType),
                    ("Amount", r => r.Amount),
                    ("CurrencyCode", r => r.CurrencyCode),
                    ("IsInvoiced", r => r.IsInvoiced),
                    ("InvoiceNumber", r => r.InvoiceNumber),
                    ("CreatedAtUtc", r => r.CreatedAt)
                ]);
                return (bytes, page.Items.Count);
            }
            case "invoices":
            {
                var page = await _financialReports.GetInvoicesReportAsync(filter, ExportPageRequest, ct);
                var bytes = ReportingQueryHelpers.BuildCsv(page.Items, [
                    ("InvoiceId", r => r.InvoiceId),
                    ("InvoiceNumber", r => r.InvoiceNumber),
                    ("CustomerId", r => r.CustomerId),
                    ("CustomerName", r => r.CustomerName),
                    ("BranchCode", r => r.BranchCode),
                    ("InvoiceDate", r => r.InvoiceDate),
                    ("DueDate", r => r.DueDate),
                    ("Status", r => r.Status),
                    ("SubTotal", r => r.SubTotal),
                    ("TaxAmount", r => r.TaxAmount),
                    ("TotalAmount", r => r.TotalAmount),
                    ("PaidAmount", r => r.PaidAmount),
                    ("OutstandingAmount", r => r.OutstandingAmount)
                ]);
                return (bytes, page.Items.Count);
            }
            case "payments":
            {
                var page = await _financialReports.GetPaymentsReportAsync(filter, ExportPageRequest, ct);
                var bytes = ReportingQueryHelpers.BuildCsv(page.Items, [
                    ("PaymentId", r => r.PaymentId),
                    ("PaymentNumber", r => r.PaymentNumber),
                    ("CustomerName", r => r.CustomerName),
                    ("BranchCode", r => r.BranchCode),
                    ("PaymentDateUtc", r => r.PaymentDate),
                    ("PaymentMethod", r => r.PaymentMethod),
                    ("Status", r => r.Status),
                    ("Amount", r => r.Amount),
                    ("AllocatedAmount", r => r.AllocatedAmount),
                    ("UnallocatedAmount", r => r.UnallocatedAmount)
                ]);
                return (bytes, page.Items.Count);
            }
            case "receivables":
            {
                var page = await _financialReports.GetReceivablesReportAsync(filter, ExportPageRequest, ct);
                var bytes = ReportingQueryHelpers.BuildCsv(page.Items, [
                    ("CustomerId", r => r.CustomerId),
                    ("CustomerCode", r => r.CustomerCode),
                    ("CustomerName", r => r.CustomerName),
                    ("BranchCode", r => r.BranchCode),
                    ("CurrentOutstandingBalance", r => r.CurrentOutstandingBalance),
                    ("UnbilledChargesAmount", r => r.UnbilledChargesAmount),
                    ("TotalCodHoldingAmount", r => r.TotalCodHoldingAmount),
                    ("CreditLimit", r => r.CreditLimit),
                    ("LastUpdatedDateUtc", r => r.LastUpdatedDate)
                ]);
                return (bytes, page.Items.Count);
            }
            case "cod-collections":
            {
                var page = await _financialReports.GetCodCollectionsReportAsync(filter, ExportPageRequest, ct);
                var bytes = ReportingQueryHelpers.BuildCsv(page.Items, [
                    ("CollectionId", r => r.CollectionId),
                    ("TrackingNumber", r => r.TrackingNumber),
                    ("DestinationBranchCode", r => r.DestinationBranchCode),
                    ("CustomerName", r => r.CustomerName),
                    ("CollectedByEmployeeName", r => r.CollectedByEmployeeName),
                    ("ExpectedCodAmount", r => r.ExpectedCodAmount),
                    ("CollectedAmount", r => r.CollectedAmount),
                    ("VarianceAmount", r => r.VarianceAmount),
                    ("Status", r => r.Status),
                    ("CollectionDateUtc", r => r.CollectionDate),
                    ("VerifiedAtUtc", r => r.VerifiedAt)
                ]);
                return (bytes, page.Items.Count);
            }
            case "cod-remittances":
            {
                var page = await _financialReports.GetCodRemittancesReportAsync(filter, ExportPageRequest, ct);
                var bytes = ReportingQueryHelpers.BuildCsv(page.Items, [
                    ("RemittanceId", r => r.RemittanceId),
                    ("RemittanceNumber", r => r.RemittanceNumber),
                    ("CustomerName", r => r.CustomerName),
                    ("BranchCode", r => r.BranchCode),
                    ("RemittanceDateUtc", r => r.RemittanceDate),
                    ("Status", r => r.Status),
                    ("TotalCodAmount", r => r.TotalCodAmount),
                    ("TotalServiceFeesDeducted", r => r.TotalServiceFeesDeducted),
                    ("NetRemittanceAmount", r => r.NetRemittanceAmount),
                    ("ItemsCount", r => r.ItemsCount),
                    ("RemittedAtUtc", r => r.RemittedAt)
                ]);
                return (bytes, page.Items.Count);
            }
            case "cod-reconciliation":
            {
                var page = await _financialReports.GetCodReconciliationReportAsync(filter, ExportPageRequest, ct);
                var bytes = ReportingQueryHelpers.BuildCsv(page.Items, [
                    ("BranchId", r => r.BranchId),
                    ("BranchCode", r => r.BranchCode),
                    ("BranchName", r => r.BranchName),
                    ("ExpectedCod", r => r.ExpectedCod),
                    ("CollectedByDrivers", r => r.CollectedByDrivers),
                    ("DepositedToBranch", r => r.DepositedToBranch),
                    ("VerifiedByCashier", r => r.VerifiedByCashier),
                    ("RemittedToCustomers", r => r.RemittedToCustomers),
                    ("DriverCashInHand", r => r.DriverCashInHand),
                    ("PendingCashierVerification", r => r.PendingCashierVerification),
                    ("DiscrepancyAmount", r => r.DiscrepancyAmount)
                ]);
                return (bytes, page.Items.Count);
            }
            default:
                throw new ValidationException("ReportType", $"Unknown report type '{normalizedType}'.");
        }
    }
}

/// <summary>
/// Scheduled report execution engine supporting real CSV report generation and honest unsupported status for Pdf/Excel.
/// Designed to execute both in HTTP requests and background worker contexts without relying on Ambient HTTP user claims.
/// </summary>
public sealed class ReportingExecutionEngine : IReportExecutionEngine
{
    private readonly ApplicationDbContext _context;

    public ReportingExecutionEngine(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ReportGenerationResult> GenerateAsync(
        string reportCode,
        string queryDefinition,
        string outputFormat,
        long? branchId,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(outputFormat?.Trim(), "Csv", StringComparison.OrdinalIgnoreCase))
        {
            return new ReportGenerationResult
            {
                IsSupported = false,
                Succeeded = false,
                Message = $"Report output format '{outputFormat}' is not supported. Only 'Csv' generation is currently supported."
            };
        }

        ReportingQueryHelpers.ConfigureReadOnlyTimeout(_context);

        var code = reportCode.Trim().ToUpperInvariant();
        byte[] csvBytes;

        switch (code)
        {
            case "RPT_DAILY_DISPATCH":
            {
                var q = _context.WarehouseDispatches.AsNoTracking().AsQueryable();
                if (branchId.HasValue) q = q.Where(d => d.Warehouse.BranchId == branchId.Value);
                var rows = await q.OrderByDescending(d => d.CreatedAt).Take(ReportingQueryHelpers.MaxExportRows)
                    .Select(d => new
                    {
                        d.Id,
                        d.DispatchNumber,
                        WarehouseCode = d.Warehouse.Code,
                        d.DispatchType,
                        d.Status,
                        d.DispatchedAt,
                        LineItemsCount = d.DispatchItems.Count,
                        PackagesCount = d.DispatchPackages.Count
                    }).ToListAsync(cancellationToken);

                csvBytes = ReportingQueryHelpers.BuildCsv(rows, [
                    ("DispatchId", r => r.Id),
                    ("DispatchNumber", r => r.DispatchNumber),
                    ("WarehouseCode", r => r.WarehouseCode),
                    ("DispatchType", r => r.DispatchType),
                    ("Status", r => r.Status),
                    ("DispatchedAtUtc", r => r.DispatchedAt),
                    ("LineItemsCount", r => r.LineItemsCount),
                    ("PackagesCount", r => r.PackagesCount)
                ]);
                break;
            }
            case "RPT_ORDERS":
            {
                var q = _context.Orders.AsNoTracking().AsQueryable();
                if (branchId.HasValue) q = q.Where(o => o.BranchId == branchId.Value);
                var rows = await q.OrderByDescending(o => o.OrderDate).Take(ReportingQueryHelpers.MaxExportRows)
                    .Select(o => new
                    {
                        o.Id,
                        o.OrderNumber,
                        o.OrderDate,
                        o.OrderStatus,
                        o.PaymentStatus,
                        BranchCode = o.Branch.Code,
                        CustomerName = o.Customer.Name,
                        o.DeclaredValue,
                        o.CodAmount
                    }).ToListAsync(cancellationToken);

                csvBytes = ReportingQueryHelpers.BuildCsv(rows, [
                    ("OrderId", r => r.Id),
                    ("OrderNumber", r => r.OrderNumber),
                    ("OrderDateUtc", r => r.OrderDate),
                    ("OrderStatus", r => r.OrderStatus),
                    ("PaymentStatus", r => r.PaymentStatus),
                    ("BranchCode", r => r.BranchCode),
                    ("CustomerName", r => r.CustomerName),
                    ("DeclaredValue", r => r.DeclaredValue),
                    ("CodAmount", r => r.CodAmount)
                ]);
                break;
            }
            case "RPT_SHIPMENTS":
            {
                var q = _context.Shipments.AsNoTracking().AsQueryable();
                if (branchId.HasValue) q = q.Where(s => s.OriginBranchId == branchId.Value || s.DestinationBranchId == branchId.Value);
                var rows = await q.OrderByDescending(s => s.CreatedAt).Take(ReportingQueryHelpers.MaxExportRows)
                    .Select(s => new
                    {
                        s.Id,
                        s.TrackingNumber,
                        s.ShipmentStatus,
                        s.ServiceType,
                        OriginBranchCode = s.OriginBranch.Code,
                        DestinationBranchCode = s.DestinationBranch.Code,
                        CustomerName = s.Customer.Name,
                        s.TotalAmount,
                        s.CodAmount,
                        s.CreatedAt
                    }).ToListAsync(cancellationToken);

                csvBytes = ReportingQueryHelpers.BuildCsv(rows, [
                    ("ShipmentId", r => r.Id),
                    ("TrackingNumber", r => r.TrackingNumber),
                    ("ShipmentStatus", r => r.ShipmentStatus),
                    ("ServiceType", r => r.ServiceType),
                    ("OriginBranchCode", r => r.OriginBranchCode),
                    ("DestinationBranchCode", r => r.DestinationBranchCode),
                    ("CustomerName", r => r.CustomerName),
                    ("TotalAmount", r => r.TotalAmount),
                    ("CodAmount", r => r.CodAmount),
                    ("CreatedAtUtc", r => r.CreatedAt)
                ]);
                break;
            }
            case "RPT_INVENTORY_BALANCES":
            {
                var q = _context.InventoryBalances.AsNoTracking().AsQueryable();
                if (branchId.HasValue) q = q.Where(b => b.Warehouse.BranchId == branchId.Value);
                var rows = await q.OrderBy(b => b.Warehouse.Code).ThenBy(b => b.Product.Sku).Take(ReportingQueryHelpers.MaxExportRows)
                    .Select(b => new
                    {
                        b.Id,
                        WarehouseCode = b.Warehouse.Code,
                        LocationCode = b.WarehouseLocation.Code,
                        ProductSku = b.Product.Sku,
                        ProductName = b.Product.Name,
                        b.QuantityOnHand,
                        b.QuantityAllocated,
                        QuantityAvailable = b.QuantityOnHand - b.QuantityAllocated,
                        b.QuantityDamaged
                    }).ToListAsync(cancellationToken);

                csvBytes = ReportingQueryHelpers.BuildCsv(rows, [
                    ("BalanceId", r => r.Id),
                    ("WarehouseCode", r => r.WarehouseCode),
                    ("LocationCode", r => r.LocationCode),
                    ("ProductSku", r => r.ProductSku),
                    ("ProductName", r => r.ProductName),
                    ("QuantityOnHand", r => r.QuantityOnHand),
                    ("QuantityAllocated", r => r.QuantityAllocated),
                    ("QuantityAvailable", r => r.QuantityAvailable),
                    ("QuantityDamaged", r => r.QuantityDamaged)
                ]);
                break;
            }
            case "RPT_FINANCE_INVOICES":
            {
                var q = _context.Invoices.AsNoTracking().AsQueryable();
                if (branchId.HasValue) q = q.Where(i => i.Customer.BranchId == branchId.Value);
                var rows = await q.OrderByDescending(i => i.InvoiceDate).Take(ReportingQueryHelpers.MaxExportRows)
                    .Select(i => new
                    {
                        i.Id,
                        i.InvoiceNumber,
                        CustomerName = i.Customer.Name,
                        BranchCode = i.Customer.Branch.Code,
                        i.InvoiceDate,
                        i.DueDate,
                        i.Status,
                        i.TotalAmount,
                        i.PaidAmount,
                        OutstandingAmount = i.TotalAmount - i.PaidAmount
                    }).ToListAsync(cancellationToken);

                csvBytes = ReportingQueryHelpers.BuildCsv(rows, [
                    ("InvoiceId", r => r.Id),
                    ("InvoiceNumber", r => r.InvoiceNumber),
                    ("CustomerName", r => r.CustomerName),
                    ("BranchCode", r => r.BranchCode),
                    ("InvoiceDate", r => r.InvoiceDate),
                    ("DueDate", r => r.DueDate),
                    ("Status", r => r.Status),
                    ("TotalAmount", r => r.TotalAmount),
                    ("PaidAmount", r => r.PaidAmount),
                    ("OutstandingAmount", r => r.OutstandingAmount)
                ]);
                break;
            }
            case "RPT_COD_RECONCILIATION":
            {
                var q = _context.CODCollections.AsNoTracking().AsQueryable();
                if (branchId.HasValue) q = q.Where(c => c.Shipment.DestinationBranchId == branchId.Value);
                var rows = await q.OrderByDescending(c => c.CollectionDate).Take(ReportingQueryHelpers.MaxExportRows)
                    .Select(c => new
                    {
                        c.Id,
                        c.Shipment.TrackingNumber,
                        BranchCode = c.Shipment.DestinationBranch.Code,
                        ExpectedCod = c.Shipment.CodAmount,
                        c.CollectedAmount,
                        Variance = c.CollectedAmount - c.Shipment.CodAmount,
                        c.Status,
                        c.CollectionDate
                    }).ToListAsync(cancellationToken);

                csvBytes = ReportingQueryHelpers.BuildCsv(rows, [
                    ("CollectionId", r => r.Id),
                    ("TrackingNumber", r => r.TrackingNumber),
                    ("BranchCode", r => r.BranchCode),
                    ("ExpectedCod", r => r.ExpectedCod),
                    ("CollectedAmount", r => r.CollectedAmount),
                    ("Variance", r => r.Variance),
                    ("Status", r => r.Status),
                    ("CollectionDateUtc", r => r.CollectionDate)
                ]);
                break;
            }
            default:
                return new ReportGenerationResult
                {
                    IsSupported = false,
                    Succeeded = false,
                    Message = $"No CSV query handler is registered for report definition code '{reportCode}'."
                };
        }

        var safeFileName = ReportingQueryHelpers.SanitizeFileName($"{code.ToLowerInvariant()}-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
        return new ReportGenerationResult
        {
            IsSupported = true,
            Succeeded = true,
            FileBytes = csvBytes,
            FileName = safeFileName,
            ContentType = "text/csv; charset=utf-8",
            Message = $"CSV report generated for '{reportCode}'."
        };
    }
}
