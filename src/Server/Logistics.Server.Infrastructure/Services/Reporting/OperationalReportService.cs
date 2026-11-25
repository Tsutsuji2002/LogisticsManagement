using Logistics.Server.Application.Common.Interfaces;
using Logistics.Server.Application.Common.Models;
using Logistics.Server.Application.Modules.Reporting.DTOs;
using Logistics.Server.Application.Modules.Reporting.Services;
using Logistics.Server.Infrastructure.Persistence;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Logistics.Server.Infrastructure.Services.Reporting;

public sealed class OperationalReportService : IOperationalReportService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public OperationalReportService(ApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<OrderReportRowDto>> GetOrderReportAsync(
        ReportFilterRequest filter, PagedRequest pagedRequest, CancellationToken cancellationToken = default)
    {
        ReportingQueryHelpers.ConfigureReadOnlyTimeout(_context);
        var (isGlobal, branches) = ReportingQueryHelpers.ResolveBranchScope(_currentUser, filter.BranchId);
        var window = ReportingQueryHelpers.ResolveTimeWindow(filter);

        var query = _context.Orders.AsNoTracking().AsQueryable();
        if (!isGlobal) query = query.Where(o => branches.Contains(o.BranchId));
        if (filter.CustomerId.HasValue) query = query.Where(o => o.CustomerId == filter.CustomerId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Status)) query = query.Where(o => o.OrderStatus == filter.Status);
        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(o => o.OrderNumber.Contains(filter.Search) || o.Customer.Name.Contains(filter.Search));
        if (window.UtcFromInclusive.HasValue) query = query.Where(o => o.OrderDate >= window.UtcFromInclusive.Value);
        if (window.UtcToExclusive.HasValue) query = query.Where(o => o.OrderDate < window.UtcToExclusive.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(o => o.OrderDate).ThenByDescending(o => o.Id)
            .Skip((pagedRequest.Page - 1) * pagedRequest.PageSize)
            .Take(pagedRequest.PageSize)
            .Select(o => new OrderReportRowDto
            {
                OrderId = o.Id,
                OrderNumber = o.OrderNumber,
                OrderDate = o.OrderDate,
                OrderStatus = o.OrderStatus,
                PaymentStatus = o.PaymentStatus,
                BranchId = o.BranchId,
                BranchCode = o.Branch.Code,
                CustomerId = o.CustomerId,
                CustomerName = o.Customer.Name,
                ItemsCount = o.OrderItems.Count,
                PackagesCount = o.OrderPackages.Count,
                TotalWeightKg = o.TotalWeightKg,
                DeclaredValue = o.DeclaredValue,
                CodAmount = o.CodAmount
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<OrderReportRowDto>(items, total, pagedRequest.Page, pagedRequest.PageSize);
    }

    public async Task<PagedResult<ShipmentReportRowDto>> GetShipmentReportAsync(
        ReportFilterRequest filter, PagedRequest pagedRequest, CancellationToken cancellationToken = default)
    {
        ReportingQueryHelpers.ConfigureReadOnlyTimeout(_context);
        var (isGlobal, branches) = ReportingQueryHelpers.ResolveBranchScope(_currentUser, filter.BranchId);
        var window = ReportingQueryHelpers.ResolveTimeWindow(filter);

        var query = _context.Shipments.AsNoTracking().AsQueryable();
        if (!isGlobal)
            query = query.Where(s => branches.Contains(s.OriginBranchId) || branches.Contains(s.DestinationBranchId));
        if (filter.CustomerId.HasValue) query = query.Where(s => s.CustomerId == filter.CustomerId.Value);
        if (filter.CarrierId.HasValue) query = query.Where(s => s.AssignedCarrierId == filter.CarrierId.Value);
        if (filter.CarrierServiceId.HasValue) query = query.Where(s => s.CarrierServiceId == filter.CarrierServiceId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Status)) query = query.Where(s => s.ShipmentStatus == filter.Status);
        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(s => s.TrackingNumber.Contains(filter.Search) || s.Customer.Name.Contains(filter.Search));
        if (window.UtcFromInclusive.HasValue) query = query.Where(s => s.CreatedAt >= window.UtcFromInclusive.Value);
        if (window.UtcToExclusive.HasValue) query = query.Where(s => s.CreatedAt < window.UtcToExclusive.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(s => s.CreatedAt).ThenByDescending(s => s.Id)
            .Skip((pagedRequest.Page - 1) * pagedRequest.PageSize)
            .Take(pagedRequest.PageSize)
            .Select(s => new ShipmentReportRowDto
            {
                ShipmentId = s.Id,
                TrackingNumber = s.TrackingNumber,
                OrderNumber = s.Order != null ? s.Order.OrderNumber : null,
                ShipmentStatus = s.ShipmentStatus,
                ServiceType = s.ServiceType,
                OriginBranchCode = s.OriginBranch.Code,
                DestinationBranchCode = s.DestinationBranch.Code,
                CustomerName = s.Customer.Name,
                CarrierCode = s.AssignedCarrier != null ? s.AssignedCarrier.Code : null,
                ChargeableWeightKg = s.ChargeableWeightKg,
                CodAmount = s.CodAmount,
                TotalAmount = s.TotalAmount,
                CreatedAt = s.CreatedAt,
                EstimatedDeliveryDate = s.EstimatedDeliveryDate,
                ActualDeliveryDate = s.ActualDeliveryDate
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<ShipmentReportRowDto>(items, total, pagedRequest.Page, pagedRequest.PageSize);
    }

    public async Task<PagedResult<DeliveryPerformanceRowDto>> GetDeliveryPerformanceReportAsync(
        ReportFilterRequest filter, PagedRequest pagedRequest, CancellationToken cancellationToken = default)
    {
        ReportingQueryHelpers.ConfigureReadOnlyTimeout(_context);
        var (isGlobal, branches) = ReportingQueryHelpers.ResolveBranchScope(_currentUser, filter.BranchId);
        var window = ReportingQueryHelpers.ResolveTimeWindow(filter);

        var query = _context.Shipments.AsNoTracking()
            .Where(s => s.ShipmentStatus == "Delivered" || s.ShipmentStatus == "DeliveryFailed" || s.ShipmentStatus == "Returning" || s.ShipmentStatus == "Returned");

        if (!isGlobal) query = query.Where(s => branches.Contains(s.DestinationBranchId));
        if (filter.CarrierId.HasValue) query = query.Where(s => s.AssignedCarrierId == filter.CarrierId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Status)) query = query.Where(s => s.ShipmentStatus == filter.Status);
        if (!string.IsNullOrWhiteSpace(filter.Search)) query = query.Where(s => s.TrackingNumber.Contains(filter.Search));
        if (window.UtcFromInclusive.HasValue) query = query.Where(s => s.CreatedAt >= window.UtcFromInclusive.Value);
        if (window.UtcToExclusive.HasValue) query = query.Where(s => s.CreatedAt < window.UtcToExclusive.Value);

        var total = await query.CountAsync(cancellationToken);
        var raw = await query
            .OrderByDescending(s => s.ActualDeliveryDate ?? s.CreatedAt).ThenByDescending(s => s.Id)
            .Skip((pagedRequest.Page - 1) * pagedRequest.PageSize)
            .Take(pagedRequest.PageSize)
            .Select(s => new
            {
                s.Id,
                s.TrackingNumber,
                BranchCode = s.DestinationBranch.Code,
                CarrierCode = s.AssignedCarrier != null ? s.AssignedCarrier.Code : null,
                s.ShipmentStatus,
                AttemptCount = s.DeliveryAttempts.Count,
                s.PickupDate,
                s.EstimatedDeliveryDate,
                s.ActualDeliveryDate
            })
            .ToListAsync(cancellationToken);

        var items = raw.Select(r => new DeliveryPerformanceRowDto
        {
            ShipmentId = r.Id,
            TrackingNumber = r.TrackingNumber,
            DestinationBranchCode = r.BranchCode,
            CarrierCode = r.CarrierCode,
            ShipmentStatus = r.ShipmentStatus,
            AttemptCount = r.AttemptCount,
            PickupDate = r.PickupDate,
            EstimatedDeliveryDate = r.EstimatedDeliveryDate,
            ActualDeliveryDate = r.ActualDeliveryDate,
            DeliveryDurationHours = r.PickupDate.HasValue && r.ActualDeliveryDate.HasValue
                ? Math.Round(Math.Max(0, (r.ActualDeliveryDate.Value - r.PickupDate.Value).TotalHours), 2)
                : null,
            IsOnTime = r.EstimatedDeliveryDate.HasValue && r.ActualDeliveryDate.HasValue
                ? r.ActualDeliveryDate.Value <= r.EstimatedDeliveryDate.Value
                : null
        }).ToList();

        return new PagedResult<DeliveryPerformanceRowDto>(items, total, pagedRequest.Page, pagedRequest.PageSize);
    }

    public async Task<PagedResult<CarrierPerformanceRowDto>> GetCarrierPerformanceReportAsync(
        ReportFilterRequest filter, PagedRequest pagedRequest, CancellationToken cancellationToken = default)
    {
        ReportingQueryHelpers.ConfigureReadOnlyTimeout(_context);
        var (isGlobal, branches) = ReportingQueryHelpers.ResolveBranchScope(_currentUser, filter.BranchId);
        var window = ReportingQueryHelpers.ResolveTimeWindow(filter);

        var carriersQuery = _context.Carriers.AsNoTracking().AsQueryable();
        if (filter.CarrierId.HasValue) carriersQuery = carriersQuery.Where(c => c.Id == filter.CarrierId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Search))
            carriersQuery = carriersQuery.Where(c => c.Code.Contains(filter.Search) || c.Name.Contains(filter.Search));

        var total = await carriersQuery.CountAsync(cancellationToken);
        var carriers = await carriersQuery
            .OrderBy(c => c.Code)
            .Skip((pagedRequest.Page - 1) * pagedRequest.PageSize)
            .Take(pagedRequest.PageSize)
            .Select(c => new { c.Id, c.Code, c.Name })
            .ToListAsync(cancellationToken);

        var carrierIds = carriers.Select(c => c.Id).ToList();
        if (carrierIds.Count == 0)
            return new PagedResult<CarrierPerformanceRowDto>([], total, pagedRequest.Page, pagedRequest.PageSize);

        var shipQ = _context.Shipments.AsNoTracking().Where(s => s.AssignedCarrierId != null && carrierIds.Contains(s.AssignedCarrierId.Value));
        if (!isGlobal) shipQ = shipQ.Where(s => branches.Contains(s.OriginBranchId) || branches.Contains(s.DestinationBranchId));
        if (window.UtcFromInclusive.HasValue) shipQ = shipQ.Where(s => s.CreatedAt >= window.UtcFromInclusive.Value);
        if (window.UtcToExclusive.HasValue) shipQ = shipQ.Where(s => s.CreatedAt < window.UtcToExclusive.Value);

        var shipStats = (await shipQ.GroupBy(s => s.AssignedCarrierId!.Value)
            .Select(g => new
            {
                CarrierId = g.Key,
                Total = g.Count(),
                Delivered = g.Count(x => x.ShipmentStatus == "Delivered"),
                Failed = g.Count(x => x.ShipmentStatus == "DeliveryFailed" || x.ShipmentStatus == "Returning" || x.ShipmentStatus == "Returned")
            })
            .ToListAsync(cancellationToken)).ToDictionary(x => x.CarrierId);

        // Operational carrier logs (aggregated counts only — no credentials or payloads exposed)
        var apiQ = _context.CarrierApiLogs.AsNoTracking().Where(l => carrierIds.Contains(l.CarrierId));
        var whQ = _context.CarrierWebhookLogs.AsNoTracking().Where(w => carrierIds.Contains(w.CarrierId));
        var syncQ = _context.CarrierSyncJobs.AsNoTracking().Where(j => carrierIds.Contains(j.CarrierId));
        var evtQ = _context.CarrierTrackingEvents.AsNoTracking().Where(e => carrierIds.Contains(e.CarrierId));

        if (window.UtcFromInclusive.HasValue)
        {
            apiQ = apiQ.Where(l => l.Timestamp >= window.UtcFromInclusive.Value);
            whQ = whQ.Where(w => w.ProcessedAt >= window.UtcFromInclusive.Value);
            syncQ = syncQ.Where(j => j.StartTime >= window.UtcFromInclusive.Value);
            evtQ = evtQ.Where(e => e.EventTimestamp >= window.UtcFromInclusive.Value);
        }
        if (window.UtcToExclusive.HasValue)
        {
            apiQ = apiQ.Where(l => l.Timestamp < window.UtcToExclusive.Value);
            whQ = whQ.Where(w => w.ProcessedAt < window.UtcToExclusive.Value);
            syncQ = syncQ.Where(j => j.StartTime < window.UtcToExclusive.Value);
            evtQ = evtQ.Where(e => e.EventTimestamp < window.UtcToExclusive.Value);
        }

        var apiStats = (await apiQ.GroupBy(l => l.CarrierId).Select(g => new { CarrierId = g.Key, Total = g.Count(), Ok = g.Count(x => x.IsSuccess) }).ToListAsync(cancellationToken)).ToDictionary(x => x.CarrierId);
        var whStats = (await whQ.GroupBy(w => w.CarrierId).Select(g => new { CarrierId = g.Key, Total = g.Count(), Ok = g.Count(x => x.ProcessedSuccessfully) }).ToListAsync(cancellationToken)).ToDictionary(x => x.CarrierId);
        var syncStats = (await syncQ.GroupBy(j => j.CarrierId).Select(g => new { CarrierId = g.Key, Total = g.Count(), Fail = g.Count(x => x.Status == "Failed") }).ToListAsync(cancellationToken)).ToDictionary(x => x.CarrierId);
        var evtStats = (await evtQ.GroupBy(e => e.CarrierId).Select(g => new { CarrierId = g.Key, Total = g.Count(), Fail = g.Count(x => x.ProcessedStatus == "Failed") }).ToListAsync(cancellationToken)).ToDictionary(x => x.CarrierId);

        var items = carriers.Select(c =>
        {
            shipStats.TryGetValue(c.Id, out var ss);
            apiStats.TryGetValue(c.Id, out var a);
            whStats.TryGetValue(c.Id, out var w);
            syncStats.TryGetValue(c.Id, out var sy);
            evtStats.TryGetValue(c.Id, out var ev);

            return new CarrierPerformanceRowDto
            {
                CarrierId = c.Id,
                CarrierCode = c.Code,
                CarrierName = c.Name,
                TotalShipments = ss?.Total ?? 0,
                DeliveredShipments = ss?.Delivered ?? 0,
                FailedOrReturnedShipments = ss?.Failed ?? 0,
                ApiCallsTotal = a?.Total ?? 0,
                ApiCallsSucceeded = a?.Ok ?? 0,
                ApiCallsFailed = (a?.Total ?? 0) - (a?.Ok ?? 0),
                WebhooksTotal = w?.Total ?? 0,
                WebhooksSucceeded = w?.Ok ?? 0,
                SyncJobsTotal = sy?.Total ?? 0,
                SyncJobsFailed = sy?.Fail ?? 0,
                TrackingEventsTotal = ev?.Total ?? 0,
                TrackingEventsFailed = ev?.Fail ?? 0
            };
        }).ToList();

        return new PagedResult<CarrierPerformanceRowDto>(items, total, pagedRequest.Page, pagedRequest.PageSize);
    }

    public async Task<PagedResult<WarehouseReceiptReportRowDto>> GetWarehouseReceiptsReportAsync(
        ReportFilterRequest filter, PagedRequest pagedRequest, CancellationToken cancellationToken = default)
    {
        ReportingQueryHelpers.ConfigureReadOnlyTimeout(_context);
        var (isGlobal, branches) = ReportingQueryHelpers.ResolveBranchScope(_currentUser, filter.BranchId);
        var window = ReportingQueryHelpers.ResolveTimeWindow(filter);

        var query = _context.WarehouseReceipts.AsNoTracking().AsQueryable();
        if (!isGlobal) query = query.Where(r => branches.Contains(r.Warehouse.BranchId));
        if (filter.WarehouseId.HasValue) query = query.Where(r => r.WarehouseId == filter.WarehouseId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Status)) query = query.Where(r => r.Status == filter.Status);
        if (!string.IsNullOrWhiteSpace(filter.Search)) query = query.Where(r => r.ReceiptNumber.Contains(filter.Search));
        if (window.UtcFromInclusive.HasValue) query = query.Where(r => r.ActualArrivalDate >= window.UtcFromInclusive.Value);
        if (window.UtcToExclusive.HasValue) query = query.Where(r => r.ActualArrivalDate < window.UtcToExclusive.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(r => r.ActualArrivalDate).ThenByDescending(r => r.Id)
            .Skip((pagedRequest.Page - 1) * pagedRequest.PageSize)
            .Take(pagedRequest.PageSize)
            .Select(r => new WarehouseReceiptReportRowDto
            {
                ReceiptId = r.Id,
                ReceiptNumber = r.ReceiptNumber,
                WarehouseCode = r.Warehouse.Code,
                ReceiptType = r.ReceiptType,
                Status = r.Status,
                ActualArrivalDate = r.ActualArrivalDate,
                LineItemsCount = r.ReceiptItems.Count,
                TotalReceivedQuantity = r.ReceiptItems.Sum(i => i.ReceivedQuantity),
                TotalDamagedQuantity = r.ReceiptItems.Sum(i => i.DamagedQuantity)
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<WarehouseReceiptReportRowDto>(items, total, pagedRequest.Page, pagedRequest.PageSize);
    }

    public async Task<PagedResult<WarehouseDispatchReportRowDto>> GetWarehouseDispatchesReportAsync(
        ReportFilterRequest filter, PagedRequest pagedRequest, CancellationToken cancellationToken = default)
    {
        ReportingQueryHelpers.ConfigureReadOnlyTimeout(_context);
        var (isGlobal, branches) = ReportingQueryHelpers.ResolveBranchScope(_currentUser, filter.BranchId);
        var window = ReportingQueryHelpers.ResolveTimeWindow(filter);

        var query = _context.WarehouseDispatches.AsNoTracking().AsQueryable();
        if (!isGlobal) query = query.Where(d => branches.Contains(d.Warehouse.BranchId));
        if (filter.WarehouseId.HasValue) query = query.Where(d => d.WarehouseId == filter.WarehouseId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Status)) query = query.Where(d => d.Status == filter.Status);
        if (!string.IsNullOrWhiteSpace(filter.Search)) query = query.Where(d => d.DispatchNumber.Contains(filter.Search));
        if (window.UtcFromInclusive.HasValue) query = query.Where(d => (d.DispatchedAt ?? d.CreatedAt) >= window.UtcFromInclusive.Value);
        if (window.UtcToExclusive.HasValue) query = query.Where(d => (d.DispatchedAt ?? d.CreatedAt) < window.UtcToExclusive.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(d => d.DispatchedAt ?? d.CreatedAt).ThenByDescending(d => d.Id)
            .Skip((pagedRequest.Page - 1) * pagedRequest.PageSize)
            .Take(pagedRequest.PageSize)
            .Select(d => new WarehouseDispatchReportRowDto
            {
                DispatchId = d.Id,
                DispatchNumber = d.DispatchNumber,
                WarehouseCode = d.Warehouse.Code,
                DispatchType = d.DispatchType,
                Status = d.Status,
                DispatchedAt = d.DispatchedAt,
                LineItemsCount = d.DispatchItems.Count,
                PackagesCount = d.DispatchPackages.Count
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<WarehouseDispatchReportRowDto>(items, total, pagedRequest.Page, pagedRequest.PageSize);
    }

    public async Task<PagedResult<ParcelHandlingReportRowDto>> GetParcelHandlingReportAsync(
        ReportFilterRequest filter, PagedRequest pagedRequest, CancellationToken cancellationToken = default)
    {
        ReportingQueryHelpers.ConfigureReadOnlyTimeout(_context);
        var (isGlobal, branches) = ReportingQueryHelpers.ResolveBranchScope(_currentUser, filter.BranchId);
        var window = ReportingQueryHelpers.ResolveTimeWindow(filter);

        var query = _context.ParcelHandlingEvents.AsNoTracking().AsQueryable();
        if (!isGlobal) query = query.Where(e => branches.Contains(e.Warehouse.BranchId));
        if (filter.WarehouseId.HasValue) query = query.Where(e => e.WarehouseId == filter.WarehouseId.Value);
        if (filter.EmployeeId.HasValue) query = query.Where(e => e.EmployeeId == filter.EmployeeId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Status)) query = query.Where(e => e.EventType == filter.Status);
        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(e => e.Shipment.TrackingNumber.Contains(filter.Search) || e.ShipmentPackage.Barcode.Contains(filter.Search));
        if (window.UtcFromInclusive.HasValue) query = query.Where(e => e.ScannedAt >= window.UtcFromInclusive.Value);
        if (window.UtcToExclusive.HasValue) query = query.Where(e => e.ScannedAt < window.UtcToExclusive.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(e => e.ScannedAt).ThenByDescending(e => e.Id)
            .Skip((pagedRequest.Page - 1) * pagedRequest.PageSize)
            .Take(pagedRequest.PageSize)
            .Select(e => new ParcelHandlingReportRowDto
            {
                EventId = e.Id,
                TrackingNumber = e.Shipment.TrackingNumber,
                PackageBarcode = e.ShipmentPackage.Barcode,
                ShipmentStatus = e.Shipment.ShipmentStatus,
                WarehouseCode = e.Warehouse.Code,
                EventType = e.EventType,
                EmployeeName = e.Employee.FullName,
                ScannedAt = e.ScannedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<ParcelHandlingReportRowDto>(items, total, pagedRequest.Page, pagedRequest.PageSize);
    }

    public async Task<PagedResult<InventoryBalanceReportRowDto>> GetInventoryBalancesReportAsync(
        ReportFilterRequest filter, bool onlyLowStock, PagedRequest pagedRequest, CancellationToken cancellationToken = default)
    {
        ReportingQueryHelpers.ConfigureReadOnlyTimeout(_context);
        var (isGlobal, branches) = ReportingQueryHelpers.ResolveBranchScope(_currentUser, filter.BranchId);

        var query = from b in _context.InventoryBalances.AsNoTracking()
                    join item in _context.InventoryItems.AsNoTracking().Where(ii => ii.IsActive)
                        on new { b.WarehouseId, b.ProductId } equals new { item.WarehouseId, item.ProductId } into itemGroup
                    from ii in itemGroup.DefaultIfEmpty()
                    select new { Balance = b, Item = ii };

        if (!isGlobal) query = query.Where(x => branches.Contains(x.Balance.Warehouse.BranchId));
        if (filter.WarehouseId.HasValue) query = query.Where(x => x.Balance.WarehouseId == filter.WarehouseId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(x => x.Balance.Product.Sku.Contains(filter.Search) || x.Balance.Product.Name.Contains(filter.Search));

        if (onlyLowStock)
        {
            query = query.Where(x => x.Item != null && (x.Balance.QuantityOnHand - x.Balance.QuantityAllocated) <= x.Item.ReorderPoint);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.Balance.Warehouse.Code).ThenBy(x => x.Balance.Product.Sku)
            .Skip((pagedRequest.Page - 1) * pagedRequest.PageSize)
            .Take(pagedRequest.PageSize)
            .Select(x => new InventoryBalanceReportRowDto
            {
                BalanceId = x.Balance.Id,
                WarehouseCode = x.Balance.Warehouse.Code,
                LocationCode = x.Balance.WarehouseLocation.Code,
                ProductId = x.Balance.ProductId,
                ProductSku = x.Balance.Product.Sku,
                ProductName = x.Balance.Product.Name,
                LotNumber = x.Balance.Lot != null ? x.Balance.Lot.LotNumber : null,
                LotExpiryDate = x.Balance.Lot != null ? x.Balance.Lot.ExpiryDate : null,
                QuantityOnHand = x.Balance.QuantityOnHand,
                QuantityAllocated = x.Balance.QuantityAllocated,
                QuantityAvailable = x.Balance.QuantityOnHand - x.Balance.QuantityAllocated,
                QuantityDamaged = x.Balance.QuantityDamaged,
                ReorderPoint = x.Item != null ? x.Item.ReorderPoint : null,
                IsBelowReorderPoint = x.Item != null && (x.Balance.QuantityOnHand - x.Balance.QuantityAllocated) <= x.Item.ReorderPoint
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<InventoryBalanceReportRowDto>(items, total, pagedRequest.Page, pagedRequest.PageSize);
    }

    public async Task<PagedResult<InventoryTransactionReportRowDto>> GetInventoryTransactionsReportAsync(
        ReportFilterRequest filter, PagedRequest pagedRequest, CancellationToken cancellationToken = default)
    {
        ReportingQueryHelpers.ConfigureReadOnlyTimeout(_context);
        var (isGlobal, branches) = ReportingQueryHelpers.ResolveBranchScope(_currentUser, filter.BranchId);
        var window = ReportingQueryHelpers.ResolveTimeWindow(filter);

        var query = _context.InventoryTransactions.AsNoTracking().AsQueryable();
        if (!isGlobal) query = query.Where(t => branches.Contains(t.Warehouse.BranchId));
        if (filter.WarehouseId.HasValue) query = query.Where(t => t.WarehouseId == filter.WarehouseId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Status)) query = query.Where(t => t.TransactionType == filter.Status);
        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(t => t.TransactionNumber.Contains(filter.Search) || t.ReferenceNumber.Contains(filter.Search));
        if (window.UtcFromInclusive.HasValue) query = query.Where(t => t.TransactionDate >= window.UtcFromInclusive.Value);
        if (window.UtcToExclusive.HasValue) query = query.Where(t => t.TransactionDate < window.UtcToExclusive.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(t => t.TransactionDate).ThenByDescending(t => t.Id)
            .Skip((pagedRequest.Page - 1) * pagedRequest.PageSize)
            .Take(pagedRequest.PageSize)
            .Select(t => new InventoryTransactionReportRowDto
            {
                TransactionId = t.Id,
                TransactionNumber = t.TransactionNumber,
                WarehouseCode = t.Warehouse.Code,
                TransactionType = t.TransactionType,
                ReferenceType = t.ReferenceType,
                ReferenceNumber = t.ReferenceNumber,
                TotalQuantity = t.TransactionItems.Sum(i => i.Quantity),
                TransactionDate = t.TransactionDate
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<InventoryTransactionReportRowDto>(items, total, pagedRequest.Page, pagedRequest.PageSize);
    }
}
