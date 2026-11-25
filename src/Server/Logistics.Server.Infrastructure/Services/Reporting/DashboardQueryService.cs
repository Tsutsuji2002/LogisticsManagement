using Logistics.Server.Application.Common.Exceptions;
using Logistics.Server.Application.Common.Interfaces;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Identity.Services;
using Logistics.Server.Application.Modules.Reporting.DTOs;
using Logistics.Server.Application.Modules.Reporting.Services;
using Logistics.Server.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Logistics.Server.Infrastructure.Services.Reporting;

public sealed class DashboardQueryService : IDashboardQueryService
{
    private static readonly TimeSpan DashboardCacheTtl = TimeSpan.FromSeconds(45);

    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IPermissionService _permissionService;
    private readonly ICacheService _cache;

    public DashboardQueryService(
        ApplicationDbContext context,
        ICurrentUserService currentUser,
        IPermissionService permissionService,
        ICacheService cache)
    {
        _context = context;
        _currentUser = currentUser;
        _permissionService = permissionService;
        _cache = cache;
    }

    public async Task<CompanyOverviewDashboardDto> GetCompanyOverviewAsync(ReportFilterRequest filter, CancellationToken cancellationToken = default)
    {
        ReportingQueryHelpers.ConfigureReadOnlyTimeout(_context);
        var window = ReportingQueryHelpers.ResolveTimeWindow(filter);

        var cacheKey = ReportingQueryHelpers.BuildBranchCacheKey("company-overview", _currentUser, filter);
        var cached = await _cache.GetAsync<CompanyOverviewDashboardDto>(cacheKey, cancellationToken);
        if (cached != null) return cached;

        var totalBranches = await _context.Branches.AsNoTracking().CountAsync(b => b.IsActive, cancellationToken);
        var totalEmployees = await _context.Employees.AsNoTracking().CountAsync(e => e.Status == "Active", cancellationToken);
        var totalCustomers = await _context.Customers.AsNoTracking().CountAsync(c => c.IsActive, cancellationToken);

        var ordersQuery = _context.Orders.AsNoTracking().AsQueryable();
        var shipmentsQuery = _context.Shipments.AsNoTracking().AsQueryable();
        var attemptsQuery = _context.DeliveryAttempts.AsNoTracking().AsQueryable();

        // Live operational snapshot (always current operational queue state)
        var inTransitCount = await shipmentsQuery.CountAsync(s => s.ShipmentStatus == "InTransit" || s.ShipmentStatus == "InSortingHub", cancellationToken);
        var awaitingPickupCount = await shipmentsQuery.CountAsync(s => s.ShipmentStatus == "ReadyForPickup", cancellationToken);
        var awaitingDeliveryCount = await shipmentsQuery.CountAsync(s => s.ShipmentStatus == "OutForDelivery", cancellationToken);
        var pendingReceipts = await _context.WarehouseReceipts.AsNoTracking().CountAsync(r => r.Status == "Draft" || r.Status == "Pending" || r.Status == "Receiving", cancellationToken);
        var pendingDispatches = await _context.WarehouseDispatches.AsNoTracking().CountAsync(d => d.Status == "Draft" || d.Status == "Picking" || d.Status == "Packed" || d.Status == "Staged", cancellationToken);

        // Date-filtered historical/activity metrics
        if (window.UtcFromInclusive.HasValue)
        {
            ordersQuery = ordersQuery.Where(o => o.OrderDate >= window.UtcFromInclusive.Value);
            shipmentsQuery = shipmentsQuery.Where(s => s.CreatedAt >= window.UtcFromInclusive.Value);
            attemptsQuery = attemptsQuery.Where(a => a.AttemptedAt >= window.UtcFromInclusive.Value);
        }
        if (window.UtcToExclusive.HasValue)
        {
            ordersQuery = ordersQuery.Where(o => o.OrderDate < window.UtcToExclusive.Value);
            shipmentsQuery = shipmentsQuery.Where(s => s.CreatedAt < window.UtcToExclusive.Value);
            attemptsQuery = attemptsQuery.Where(a => a.AttemptedAt < window.UtcToExclusive.Value);
        }

        var totalOrders = await ordersQuery.CountAsync(cancellationToken);
        var totalShipments = await shipmentsQuery.CountAsync(cancellationToken);
        var deliveredShipments = await shipmentsQuery.CountAsync(s => s.ShipmentStatus == "Delivered", cancellationToken);
        var returnedShipments = await shipmentsQuery.CountAsync(s => s.ShipmentStatus == "Returned" || s.ShipmentStatus == "Returning", cancellationToken);
        var failedAttempts = await attemptsQuery.CountAsync(a => a.Status == "Failed", cancellationToken);

        var inventorySummary = await BuildInventorySnapshotAsync(true, Array.Empty<long>(), null, cancellationToken);
        var financeSummary = await BuildFinanceSnapshotAsync(true, Array.Empty<long>(), window, cancellationToken);
        var codSummary = await BuildCodSnapshotAsync(true, Array.Empty<long>(), window, cancellationToken);

        var dto = new CompanyOverviewDashboardDto
        {
            StartDate = window.StartDate,
            EndDate = window.EndDate,
            TimeZoneId = window.TimeZoneId,
            TotalActiveBranches = totalBranches,
            TotalActiveEmployees = totalEmployees,
            TotalActiveCustomers = totalCustomers,
            ShipmentsCurrentlyInTransit = inTransitCount,
            ShipmentsAwaitingPickup = awaitingPickupCount,
            ShipmentsAwaitingDelivery = awaitingDeliveryCount,
            PendingWarehouseReceipts = pendingReceipts,
            PendingWarehouseDispatches = pendingDispatches,
            TotalOrders = totalOrders,
            TotalShipments = totalShipments,
            DeliveredShipments = deliveredShipments,
            FailedDeliveryAttempts = failedAttempts,
            ReturnedShipments = returnedShipments,
            InventorySummary = inventorySummary,
            FinancialSummary = financeSummary,
            CodSummary = codSummary
        };

        await _cache.SetAsync(cacheKey, dto, DashboardCacheTtl, cancellationToken);
        return dto;
    }

    public async Task<BranchDashboardDto> GetBranchDashboardAsync(long branchId, ReportFilterRequest filter, CancellationToken cancellationToken = default)
    {
        ReportingQueryHelpers.ConfigureReadOnlyTimeout(_context);
        var branch = await _context.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == branchId, cancellationToken)
            ?? throw new NotFoundException("Branch", branchId);

        if (!_currentUser.IsInRole(AppRoles.SuperAdmin) && !_currentUser.HasBranchAccess(branchId))
            throw new ForbiddenAccessException($"You do not have access to branch '{branchId}'.");

        var window = ReportingQueryHelpers.ResolveTimeWindow(filter);
        IReadOnlyList<long> singleBranch = [branchId];

        var ordersQuery = _context.Orders.AsNoTracking().Where(o => o.BranchId == branchId);
        var originShipments = _context.Shipments.AsNoTracking().Where(s => s.OriginBranchId == branchId);
        var destShipments = _context.Shipments.AsNoTracking().Where(s => s.DestinationBranchId == branchId);
        var whDispatches = _context.WarehouseDispatches.AsNoTracking().Where(d => d.Warehouse.BranchId == branchId && d.Status == "Dispatched");

        if (window.UtcFromInclusive.HasValue)
        {
            ordersQuery = ordersQuery.Where(o => o.OrderDate >= window.UtcFromInclusive.Value);
            originShipments = originShipments.Where(s => s.CreatedAt >= window.UtcFromInclusive.Value);
            destShipments = destShipments.Where(s => s.CreatedAt >= window.UtcFromInclusive.Value);
            whDispatches = whDispatches.Where(d => d.DispatchedAt >= window.UtcFromInclusive.Value);
        }
        if (window.UtcToExclusive.HasValue)
        {
            ordersQuery = ordersQuery.Where(o => o.OrderDate < window.UtcToExclusive.Value);
            originShipments = originShipments.Where(s => s.CreatedAt < window.UtcToExclusive.Value);
            destShipments = destShipments.Where(s => s.CreatedAt < window.UtcToExclusive.Value);
            whDispatches = whDispatches.Where(d => d.DispatchedAt < window.UtcToExclusive.Value);
        }

        var ordersCreated = await ordersQuery.CountAsync(cancellationToken);
        var createdAtOrigin = await originShipments.CountAsync(cancellationToken);
        var receivedAtDest = await destShipments.CountAsync(cancellationToken);
        var deliveredCount = await destShipments.CountAsync(s => s.ShipmentStatus == "Delivered", cancellationToken);
        var terminalFailed = await destShipments.CountAsync(s => s.ShipmentStatus == "DeliveryFailed" || s.ShipmentStatus == "Returning" || s.ShipmentStatus == "Returned", cancellationToken);
        var terminalTotal = deliveredCount + terminalFailed;
        var successRate = terminalTotal > 0 ? Math.Round((decimal)deliveredCount / terminalTotal * 100m, 2) : 0m;

        var pendingReceipts = await _context.WarehouseReceipts.AsNoTracking()
            .CountAsync(r => r.Warehouse.BranchId == branchId && (r.Status == "Draft" || r.Status == "Pending" || r.Status == "Receiving"), cancellationToken);
        var pendingDispatches = await _context.WarehouseDispatches.AsNoTracking()
            .CountAsync(d => d.Warehouse.BranchId == branchId && (d.Status == "Draft" || d.Status == "Picking" || d.Status == "Packed" || d.Status == "Staged"), cancellationToken);

        var invSummary = await BuildInventorySnapshotAsync(false, singleBranch, null, cancellationToken);
        var codSummary = await BuildCodSnapshotAsync(false, singleBranch, window, cancellationToken);

        FinanceSnapshotDto? financeSummary = null;
        var canReadFinance = _currentUser.IsInRole(AppRoles.SuperAdmin)
            || (_currentUser.UserId.HasValue && await _permissionService.HasPermissionAsync(_currentUser.UserId.Value, AppPermissions.Reporting.FinanceRead, cancellationToken));

        if (canReadFinance)
        {
            financeSummary = await BuildFinanceSnapshotAsync(false, singleBranch, window, cancellationToken);
        }

        return new BranchDashboardDto
        {
            BranchId = branch.Id,
            BranchCode = branch.Code,
            BranchName = branch.Name,
            StartDate = window.StartDate,
            EndDate = window.EndDate,
            OrdersCreated = ordersCreated,
            ShipmentsCreatedAtOrigin = createdAtOrigin,
            ShipmentsReceivedAtDestination = receivedAtDest,
            ShipmentsDelivered = deliveredCount,
            ShipmentsDispatchedFromBranchWarehouses = await whDispatches.CountAsync(cancellationToken),
            DeliverySuccessRatePercent = successRate,
            PendingReceiptsCount = pendingReceipts,
            PendingDispatchesCount = pendingDispatches,
            InventorySummary = invSummary,
            CodOperations = codSummary,
            FinancialSummary = financeSummary
        };
    }

    public async Task<OperationsDashboardDto> GetOperationsDashboardAsync(ReportFilterRequest filter, CancellationToken cancellationToken = default)
    {
        ReportingQueryHelpers.ConfigureReadOnlyTimeout(_context);
        var (isGlobal, branches) = ReportingQueryHelpers.ResolveBranchScope(_currentUser, filter.BranchId);
        var window = ReportingQueryHelpers.ResolveTimeWindow(filter);

        var orders = _context.Orders.AsNoTracking().AsQueryable();
        var shipments = _context.Shipments.AsNoTracking().AsQueryable();
        var attempts = _context.DeliveryAttempts.AsNoTracking().AsQueryable();
        var returns = _context.ReturnRequests.AsNoTracking().AsQueryable();
        var receipts = _context.WarehouseReceipts.AsNoTracking().AsQueryable();
        var dispatches = _context.WarehouseDispatches.AsNoTracking().AsQueryable();
        var parcelEvents = _context.ParcelHandlingEvents.AsNoTracking().AsQueryable();

        if (!isGlobal)
        {
            orders = orders.Where(o => branches.Contains(o.BranchId));
            shipments = shipments.Where(s => branches.Contains(s.OriginBranchId) || branches.Contains(s.DestinationBranchId));
            attempts = attempts.Where(a => branches.Contains(a.Shipment.DestinationBranchId));
            returns = returns.Where(r => branches.Contains(r.OriginalShipment.DestinationBranchId));
            receipts = receipts.Where(r => branches.Contains(r.Warehouse.BranchId));
            dispatches = dispatches.Where(d => branches.Contains(d.Warehouse.BranchId));
            parcelEvents = parcelEvents.Where(e => branches.Contains(e.Warehouse.BranchId));
        }

        if (window.UtcFromInclusive.HasValue)
            parcelEvents = parcelEvents.Where(e => e.ScannedAt >= window.UtcFromInclusive.Value);
        if (window.UtcToExclusive.HasValue)
            parcelEvents = parcelEvents.Where(e => e.ScannedAt < window.UtcToExclusive.Value);

        return new OperationsDashboardDto
        {
            OrdersAwaitingProcessing = await orders.CountAsync(o => o.OrderStatus == "Confirmed" || o.OrderStatus == "Processing", cancellationToken),
            ShipmentsAwaitingPickup = await shipments.CountAsync(s => s.ShipmentStatus == "ReadyForPickup", cancellationToken),
            ShipmentsInSortingHub = await shipments.CountAsync(s => s.ShipmentStatus == "InSortingHub", cancellationToken),
            ShipmentsInTransit = await shipments.CountAsync(s => s.ShipmentStatus == "InTransit", cancellationToken),
            ShipmentsAwaitingDelivery = await shipments.CountAsync(s => s.ShipmentStatus == "OutForDelivery", cancellationToken),
            ShipmentsDelivered = await shipments.CountAsync(s => s.ShipmentStatus == "Delivered", cancellationToken),
            FailedDeliveryAttempts = await attempts.CountAsync(a => a.Status == "Failed", cancellationToken),
            ActiveReturnRequests = await returns.CountAsync(r => r.Status == "Requested" || r.Status == "Approved" || r.Status == "InTransit", cancellationToken),
            WarehouseReceiptsPending = await receipts.CountAsync(r => r.Status == "Draft" || r.Status == "Pending" || r.Status == "Receiving", cancellationToken),
            WarehouseDispatchesPending = await dispatches.CountAsync(d => d.Status == "Draft" || d.Status == "Picking" || d.Status == "Packed" || d.Status == "Staged", cancellationToken),
            ParcelHandlingEventsCount = await parcelEvents.CountAsync(cancellationToken)
        };
    }

    public async Task<ShipmentDashboardDto> GetShipmentDashboardAsync(ReportFilterRequest filter, CancellationToken cancellationToken = default)
    {
        ReportingQueryHelpers.ConfigureReadOnlyTimeout(_context);
        var (isGlobal, branches) = ReportingQueryHelpers.ResolveBranchScope(_currentUser, filter.BranchId);
        var window = ReportingQueryHelpers.ResolveTimeWindow(filter);

        var query = _context.Shipments.AsNoTracking().AsQueryable();
        if (!isGlobal)
            query = query.Where(s => branches.Contains(s.OriginBranchId) || branches.Contains(s.DestinationBranchId));
        if (filter.CustomerId.HasValue)
            query = query.Where(s => s.CustomerId == filter.CustomerId.Value);
        if (filter.CarrierId.HasValue)
            query = query.Where(s => s.AssignedCarrierId == filter.CarrierId.Value);
        if (window.UtcFromInclusive.HasValue)
            query = query.Where(s => s.CreatedAt >= window.UtcFromInclusive.Value);
        if (window.UtcToExclusive.HasValue)
            query = query.Where(s => s.CreatedAt < window.UtcToExclusive.Value);

        var totalShipments = await query.CountAsync(cancellationToken);
        var distinctOrders = await query.Where(s => s.OrderId.HasValue).Select(s => s.OrderId!.Value).Distinct().CountAsync(cancellationToken);

        var byStatus = await query.GroupBy(s => s.ShipmentStatus)
            .Select(g => new StatusBreakdownDto(g.Key, g.Count(), g.Sum(x => x.TotalAmount)))
            .ToListAsync(cancellationToken);

        var byService = await query.GroupBy(s => s.ServiceType)
            .Select(g => new StatusBreakdownDto(g.Key, g.Count(), g.Sum(x => x.TotalAmount)))
            .ToListAsync(cancellationToken);

        var carrierGrouped = await query
            .GroupBy(s => s.AssignedCarrierId)
            .Select(g => new { CarrierId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(20)
            .ToListAsync(cancellationToken);

        var carrierIds = carrierGrouped.Where(x => x.CarrierId.HasValue).Select(x => x.CarrierId!.Value).ToList();
        var carriersLookup = carrierIds.Count > 0
            ? await _context.Carriers.AsNoTracking()
                .Where(c => carrierIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => new { c.Code, c.Name }, cancellationToken)
            : [];

        var byCarrier = carrierGrouped.Select(x =>
        {
            if (x.CarrierId.HasValue && carriersLookup.TryGetValue(x.CarrierId.Value, out var info))
                return new CarrierCountDto(x.CarrierId, info.Code, info.Name, x.Count);
            return new CarrierCountDto(x.CarrierId, "INTERNAL", "Internal Fleet", x.Count);
        }).ToList();

        var byBranchRaw = await query
            .GroupBy(s => new { s.OriginBranchId, s.OriginBranch.Code, s.OriginBranch.Name })
            .Select(g => new { g.Key.OriginBranchId, g.Key.Code, g.Key.Name, Count = g.Count(), Amount = g.Sum(x => x.TotalAmount) })
            .OrderByDescending(x => x.Count)
            .Take(20)
            .ToListAsync(cancellationToken);

        var byBranch = byBranchRaw
            .Select(x => new BranchBreakdownDto(x.OriginBranchId, x.Code, x.Name, x.Count, x.Amount))
            .ToList();

        var creationDates = await query.Select(s => s.CreatedAt).ToListAsync(cancellationToken);
        var deliveryDates = await query.Where(s => s.ActualDeliveryDate != null).Select(s => s.ActualDeliveryDate!.Value).ToListAsync(cancellationToken);
        var cancelDates = await query.Where(s => s.ShipmentStatus == "Cancelled").Select(s => s.UpdatedAt ?? s.CreatedAt).ToListAsync(cancellationToken);

        var returnsQuery = _context.ReturnRequests.AsNoTracking().AsQueryable();
        if (!isGlobal)
            returnsQuery = returnsQuery.Where(r => branches.Contains(r.OriginalShipment.DestinationBranchId));
        if (window.UtcFromInclusive.HasValue)
            returnsQuery = returnsQuery.Where(r => r.RequestedAt >= window.UtcFromInclusive.Value);
        if (window.UtcToExclusive.HasValue)
            returnsQuery = returnsQuery.Where(r => r.RequestedAt < window.UtcToExclusive.Value);
        var returnDates = await returnsQuery.Select(r => r.RequestedAt).ToListAsync(cancellationToken);

        return new ShipmentDashboardDto
        {
            TotalShipments = totalShipments,
            DistinctOrdersCovered = distinctOrders,
            ByStatus = byStatus,
            ByServiceType = byService,
            ByCarrier = byCarrier,
            ByOriginBranch = byBranch,
            CreationTrend = AggregateDailyTrend(creationDates, window.TimeZoneId),
            DeliveryTrend = AggregateDailyTrend(deliveryDates, window.TimeZoneId),
            CancellationTrend = AggregateDailyTrend(cancelDates, window.TimeZoneId),
            ReturnTrend = AggregateDailyTrend(returnDates, window.TimeZoneId)
        };
    }

    public async Task<DeliveryPerformanceDashboardDto> GetDeliveryPerformanceDashboardAsync(ReportFilterRequest filter, CancellationToken cancellationToken = default)
    {
        ReportingQueryHelpers.ConfigureReadOnlyTimeout(_context);
        var (isGlobal, branches) = ReportingQueryHelpers.ResolveBranchScope(_currentUser, filter.BranchId);
        var window = ReportingQueryHelpers.ResolveTimeWindow(filter);

        var shipments = _context.Shipments.AsNoTracking().AsQueryable();
        if (!isGlobal)
            shipments = shipments.Where(s => branches.Contains(s.DestinationBranchId));
        if (filter.CarrierId.HasValue)
            shipments = shipments.Where(s => s.AssignedCarrierId == filter.CarrierId.Value);
        if (window.UtcFromInclusive.HasValue)
            shipments = shipments.Where(s => s.CreatedAt >= window.UtcFromInclusive.Value);
        if (window.UtcToExclusive.HasValue)
            shipments = shipments.Where(s => s.CreatedAt < window.UtcToExclusive.Value);

        // Explicit rule: active/in-transit/cancelled shipments must NOT be treated as failed deliveries
        var deliveredCount = await shipments.CountAsync(s => s.ShipmentStatus == "Delivered", cancellationToken);
        var failedOutcomeCount = await shipments.CountAsync(s =>
            s.ShipmentStatus == "DeliveryFailed" || s.ShipmentStatus == "Returning" || s.ShipmentStatus == "Returned", cancellationToken);
        var inTransitActiveCount = await shipments.CountAsync(s =>
            s.ShipmentStatus == "ReadyForPickup" || s.ShipmentStatus == "PickedUp" || s.ShipmentStatus == "InSortingHub" ||
            s.ShipmentStatus == "InTransit" || s.ShipmentStatus == "OutForDelivery", cancellationToken);

        var terminalCount = deliveredCount + failedOutcomeCount;
        decimal? successRate = terminalCount > 0 ? Math.Round((decimal)deliveredCount / terminalCount * 100m, 2) : null;
        decimal? failedRate = terminalCount > 0 ? Math.Round((decimal)failedOutcomeCount / terminalCount * 100m, 2) : null;

        var durationRows = await shipments
            .Where(s => s.ShipmentStatus == "Delivered" && s.PickupDate != null && s.ActualDeliveryDate != null)
            .Select(s => new { Pickup = s.PickupDate!.Value, Delivered = s.ActualDeliveryDate!.Value })
            .ToListAsync(cancellationToken);

        double? avgHours = durationRows.Count > 0
            ? Math.Round(durationRows.Average(x => Math.Max(0, (x.Delivered - x.Pickup).TotalHours)), 2)
            : null;

        // SLA evaluation only when reliable EstimatedDeliveryDate exists
        var withSla = await shipments
            .Where(s => s.ShipmentStatus == "Delivered" && s.ActualDeliveryDate != null && s.EstimatedDeliveryDate != null)
            .Select(s => new { Est = s.EstimatedDeliveryDate!.Value, Actual = s.ActualDeliveryDate!.Value })
            .ToListAsync(cancellationToken);

        var deliveredWithoutSla = await shipments.CountAsync(
            s => s.ShipmentStatus == "Delivered" && (s.EstimatedDeliveryDate == null || s.ActualDeliveryDate == null), cancellationToken);

        var onTime = withSla.Count(x => x.Actual <= x.Est);
        var late = withSla.Count(x => x.Actual > x.Est);
        decimal? onTimeRate = withSla.Count > 0 ? Math.Round((decimal)onTime / withSla.Count * 100m, 2) : null;

        var attemptsQuery = _context.DeliveryAttempts.AsNoTracking().AsQueryable();
        if (!isGlobal)
            attemptsQuery = attemptsQuery.Where(a => branches.Contains(a.Shipment.DestinationBranchId));
        if (filter.EmployeeId.HasValue)
            attemptsQuery = attemptsQuery.Where(a => a.AttemptedByEmployeeId == filter.EmployeeId.Value);
        if (window.UtcFromInclusive.HasValue)
            attemptsQuery = attemptsQuery.Where(a => a.AttemptedAt >= window.UtcFromInclusive.Value);
        if (window.UtcToExclusive.HasValue)
            attemptsQuery = attemptsQuery.Where(a => a.AttemptedAt < window.UtcToExclusive.Value);

        var totalAttempts = await attemptsQuery.CountAsync(cancellationToken);
        var attemptedShipments = await attemptsQuery.Select(a => a.ShipmentId).Distinct().CountAsync(cancellationToken);
        double? avgAttempts = attemptedShipments > 0 ? Math.Round((double)totalAttempts / attemptedShipments, 2) : null;

        var byBranchRaw = await attemptsQuery
            .GroupBy(a => new { a.Shipment.DestinationBranchId, a.Shipment.DestinationBranch.Code, a.Shipment.DestinationBranch.Name })
            .Select(g => new
            {
                g.Key.DestinationBranchId,
                g.Key.Code,
                g.Key.Name,
                Total = g.Count(),
                Delivered = g.Count(x => x.Status == "Delivered"),
                Failed = g.Count(x => x.Status == "Failed")
            })
            .ToListAsync(cancellationToken);

        var byEmployeeRaw = await attemptsQuery
            .GroupBy(a => new { a.AttemptedByEmployeeId, a.AttemptedByEmployee.Code, a.AttemptedByEmployee.FullName })
            .Select(g => new
            {
                g.Key.AttemptedByEmployeeId,
                g.Key.Code,
                g.Key.FullName,
                Total = g.Count(),
                Delivered = g.Count(x => x.Status == "Delivered"),
                Failed = g.Count(x => x.Status == "Failed")
            })
            .OrderByDescending(x => x.Total)
            .Take(25)
            .ToListAsync(cancellationToken);

        var byCarrierRaw = await shipments
            .Where(s => s.AssignedCarrierId != null && (s.ShipmentStatus == "Delivered" || s.ShipmentStatus == "DeliveryFailed" || s.ShipmentStatus == "Returning" || s.ShipmentStatus == "Returned"))
            .GroupBy(s => new { CarrierId = s.AssignedCarrierId!.Value, s.AssignedCarrier!.Code, s.AssignedCarrier.Name })
            .Select(g => new
            {
                g.Key.CarrierId,
                g.Key.Code,
                g.Key.Name,
                Total = g.Count(),
                Delivered = g.Count(x => x.ShipmentStatus == "Delivered"),
                Failed = g.Count(x => x.ShipmentStatus != "Delivered")
            })
            .ToListAsync(cancellationToken);

        return new DeliveryPerformanceDashboardDto
        {
            CompletedTerminalShipmentsCount = terminalCount,
            DeliveredShipmentsCount = deliveredCount,
            FailedOutcomeShipmentsCount = failedOutcomeCount,
            InTransitActiveExcludedCount = inTransitActiveCount,
            SuccessfulDeliveryRatePercent = successRate,
            FailedDeliveryRatePercent = failedRate,
            AverageDeliveryDurationHours = avgHours,
            AverageAttemptsPerAttemptedShipment = avgAttempts,
            DeliveredWithSlaCount = withSla.Count,
            DeliveredWithoutSlaCount = deliveredWithoutSla,
            OnTimeDeliveredCount = onTime,
            LateDeliveredCount = late,
            OnTimeDeliveryRatePercent = onTimeRate,
            ByBranch = byBranchRaw.Select(x => new DeliveryGroupPerformanceDto
            {
                KeyId = x.DestinationBranchId,
                KeyCode = x.Code,
                KeyName = x.Name,
                TotalAttempts = x.Total,
                DeliveredCount = x.Delivered,
                FailedCount = x.Failed,
                SuccessRatePercent = x.Total > 0 ? Math.Round((decimal)x.Delivered / x.Total * 100m, 2) : null
            }).ToList(),
            ByEmployee = byEmployeeRaw.Select(x => new DeliveryGroupPerformanceDto
            {
                KeyId = x.AttemptedByEmployeeId,
                KeyCode = x.Code,
                KeyName = x.FullName,
                TotalAttempts = x.Total,
                DeliveredCount = x.Delivered,
                FailedCount = x.Failed,
                SuccessRatePercent = x.Total > 0 ? Math.Round((decimal)x.Delivered / x.Total * 100m, 2) : null
            }).ToList(),
            ByCarrier = byCarrierRaw.Select(x => new DeliveryGroupPerformanceDto
            {
                KeyId = x.CarrierId,
                KeyCode = x.Code,
                KeyName = x.Name,
                TotalAttempts = x.Total,
                DeliveredCount = x.Delivered,
                FailedCount = x.Failed,
                SuccessRatePercent = x.Total > 0 ? Math.Round((decimal)x.Delivered / x.Total * 100m, 2) : null
            }).ToList()
        };
    }

    public async Task<OrdersDashboardDto> GetOrdersDashboardAsync(ReportFilterRequest filter, CancellationToken cancellationToken = default)
    {
        ReportingQueryHelpers.ConfigureReadOnlyTimeout(_context);
        var (isGlobal, branches) = ReportingQueryHelpers.ResolveBranchScope(_currentUser, filter.BranchId);
        var window = ReportingQueryHelpers.ResolveTimeWindow(filter);

        var query = _context.Orders.AsNoTracking().AsQueryable();
        if (!isGlobal) query = query.Where(o => branches.Contains(o.BranchId));
        if (filter.CustomerId.HasValue) query = query.Where(o => o.CustomerId == filter.CustomerId.Value);
        if (window.UtcFromInclusive.HasValue) query = query.Where(o => o.OrderDate >= window.UtcFromInclusive.Value);
        if (window.UtcToExclusive.HasValue) query = query.Where(o => o.OrderDate < window.UtcToExclusive.Value);

        var totalOrders = await query.CountAsync(cancellationToken);
        var totalDeclared = await query.SumAsync(o => (decimal?)o.DeclaredValue, cancellationToken) ?? 0m;
        var totalCod = await query.SumAsync(o => (decimal?)o.CodAmount, cancellationToken) ?? 0m;
        var totalItems = await query.SelectMany(o => o.OrderItems).CountAsync(cancellationToken);
        var totalPackages = await query.SelectMany(o => o.OrderPackages).CountAsync(cancellationToken);

        var byStatus = await query.GroupBy(o => o.OrderStatus)
            .Select(g => new StatusBreakdownDto(g.Key, g.Count(), g.Sum(x => x.DeclaredValue)))
            .ToListAsync(cancellationToken);

        var byBranchRaw = await query.GroupBy(o => new { o.BranchId, o.Branch.Code, o.Branch.Name })
            .Select(g => new { g.Key.BranchId, g.Key.Code, g.Key.Name, Count = g.Count(), Amount = g.Sum(x => x.DeclaredValue) })
            .OrderByDescending(x => x.Count)
            .ToListAsync(cancellationToken);

        var byBranch = byBranchRaw
            .Select(x => new BranchBreakdownDto(x.BranchId, x.Code, x.Name, x.Count, x.Amount))
            .ToList();

        var topCustomersRaw = await query.GroupBy(o => new { o.CustomerId, o.Customer.CustomerCode, o.Customer.Name })
            .Select(g => new { g.Key.CustomerId, g.Key.CustomerCode, g.Key.Name, Count = g.Count(), Total = g.Sum(x => x.DeclaredValue) })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToListAsync(cancellationToken);

        var topCustomers = topCustomersRaw
            .Select(x => new CustomerSummaryBreakdownDto(x.CustomerId, x.CustomerCode, x.Name, x.Count, x.Total))
            .ToList();

        var orderDates = await query.Select(o => o.OrderDate).ToListAsync(cancellationToken);
        var cancelDates = await query.Where(o => o.OrderStatus == "Cancelled" && o.CancelledAt != null).Select(o => o.CancelledAt!.Value).ToListAsync(cancellationToken);

        return new OrdersDashboardDto
        {
            TotalOrders = totalOrders,
            TotalDeclaredValue = totalDeclared,
            TotalCodAmount = totalCod,
            TotalOrderItems = totalItems,
            TotalOrderPackages = totalPackages,
            ServiceBreakdownNote = "The sales.Orders schema does not store a ServiceType column; service breakdowns apply to shipping.Shipments.",
            ByStatus = byStatus,
            ByBranch = byBranch,
            TopCustomers = topCustomers,
            CreationTrend = AggregateDailyTrend(orderDates, window.TimeZoneId),
            CancellationTrend = AggregateDailyTrend(cancelDates, window.TimeZoneId)
        };
    }

    public async Task<WarehouseDashboardDto> GetWarehouseDashboardAsync(ReportFilterRequest filter, CancellationToken cancellationToken = default)
    {
        ReportingQueryHelpers.ConfigureReadOnlyTimeout(_context);
        var (isGlobal, branches) = ReportingQueryHelpers.ResolveBranchScope(_currentUser, filter.BranchId);
        var window = ReportingQueryHelpers.ResolveTimeWindow(filter);

        var receipts = _context.WarehouseReceipts.AsNoTracking().AsQueryable();
        var dispatches = _context.WarehouseDispatches.AsNoTracking().AsQueryable();
        var parcelEvents = _context.ParcelHandlingEvents.AsNoTracking().AsQueryable();

        if (!isGlobal)
        {
            receipts = receipts.Where(r => branches.Contains(r.Warehouse.BranchId));
            dispatches = dispatches.Where(d => branches.Contains(d.Warehouse.BranchId));
            parcelEvents = parcelEvents.Where(e => branches.Contains(e.Warehouse.BranchId));
        }

        if (filter.WarehouseId.HasValue)
        {
            receipts = receipts.Where(r => r.WarehouseId == filter.WarehouseId.Value);
            dispatches = dispatches.Where(d => d.WarehouseId == filter.WarehouseId.Value);
            parcelEvents = parcelEvents.Where(e => e.WarehouseId == filter.WarehouseId.Value);
        }

        if (window.UtcFromInclusive.HasValue)
        {
            receipts = receipts.Where(r => r.ActualArrivalDate >= window.UtcFromInclusive.Value);
            dispatches = dispatches.Where(d => (d.DispatchedAt ?? d.CreatedAt) >= window.UtcFromInclusive.Value);
            parcelEvents = parcelEvents.Where(e => e.ScannedAt >= window.UtcFromInclusive.Value);
        }
        if (window.UtcToExclusive.HasValue)
        {
            receipts = receipts.Where(r => r.ActualArrivalDate < window.UtcToExclusive.Value);
            dispatches = dispatches.Where(d => (d.DispatchedAt ?? d.CreatedAt) < window.UtcToExclusive.Value);
            parcelEvents = parcelEvents.Where(e => e.ScannedAt < window.UtcToExclusive.Value);
        }

        var eventsByType = await parcelEvents.GroupBy(e => e.EventType)
            .Select(g => new StatusBreakdownDto(g.Key, g.Count(), 0m))
            .ToListAsync(cancellationToken);

        var packagesQuery = _context.ShipmentPackages.AsNoTracking().AsQueryable();
        if (!isGlobal)
            packagesQuery = packagesQuery.Where(p => branches.Contains(p.Shipment.OriginBranchId) || branches.Contains(p.Shipment.DestinationBranchId));

        return new WarehouseDashboardDto
        {
            TotalReceipts = await receipts.CountAsync(cancellationToken),
            PendingReceipts = await receipts.CountAsync(r => r.Status == "Draft" || r.Status == "Pending" || r.Status == "Receiving", cancellationToken),
            CompletedReceipts = await receipts.CountAsync(r => r.Status == "Completed", cancellationToken),
            TotalDispatches = await dispatches.CountAsync(cancellationToken),
            PendingDispatches = await dispatches.CountAsync(d => d.Status == "Draft" || d.Status == "Picking" || d.Status == "Packed" || d.Status == "Staged", cancellationToken),
            CompletedDispatches = await dispatches.CountAsync(d => d.Status == "Dispatched", cancellationToken),
            ParcelEventsTotal = eventsByType.Sum(e => e.Count),
            ParcelsUnloadedAtHub = eventsByType.FirstOrDefault(e => e.Status == "Unloaded")?.Count ?? 0,
            ParcelsSortedAtHub = eventsByType.FirstOrDefault(e => e.Status == "Sorted")?.Count ?? 0,
            ParcelsDispatchedFromHub = eventsByType.FirstOrDefault(e => e.Status == "Dispatched")?.Count ?? 0,
            ParcelsAwaitingSorting = await packagesQuery.CountAsync(p => p.PackageStatus == "Received", cancellationToken),
            ParcelsAwaitingDispatch = await packagesQuery.CountAsync(p => p.PackageStatus == "Sorted", cancellationToken),
            ParcelEventsByType = eventsByType
        };
    }

    public async Task<InventoryDashboardDto> GetInventoryDashboardAsync(ReportFilterRequest filter, CancellationToken cancellationToken = default)
    {
        ReportingQueryHelpers.ConfigureReadOnlyTimeout(_context);
        var (isGlobal, branches) = ReportingQueryHelpers.ResolveBranchScope(_currentUser, filter.BranchId);
        var window = ReportingQueryHelpers.ResolveTimeWindow(filter);

        var snapshot = await BuildInventorySnapshotAsync(isGlobal, branches, filter.WarehouseId, cancellationToken);

        var balancesQuery = _context.InventoryBalances.AsNoTracking().AsQueryable();
        var resQuery = _context.InventoryReservations.AsNoTracking().Where(r => r.Status == "Active");
        var serialQuery = _context.InventorySerialNumbers.AsNoTracking().Where(s => s.Status == "Available");
        var txnQuery = _context.InventoryTransactions.AsNoTracking().AsQueryable();

        if (!isGlobal)
        {
            balancesQuery = balancesQuery.Where(b => branches.Contains(b.Warehouse.BranchId));
            resQuery = resQuery.Where(r => branches.Contains(r.Warehouse.BranchId));
            serialQuery = serialQuery.Where(s => branches.Contains(s.Warehouse.BranchId));
            txnQuery = txnQuery.Where(t => branches.Contains(t.Warehouse.BranchId));
        }
        if (filter.WarehouseId.HasValue)
        {
            balancesQuery = balancesQuery.Where(b => b.WarehouseId == filter.WarehouseId.Value);
            resQuery = resQuery.Where(r => r.WarehouseId == filter.WarehouseId.Value);
            serialQuery = serialQuery.Where(s => s.WarehouseId == filter.WarehouseId.Value);
            txnQuery = txnQuery.Where(t => t.WarehouseId == filter.WarehouseId.Value);
        }
        if (window.UtcFromInclusive.HasValue) txnQuery = txnQuery.Where(t => t.TransactionDate >= window.UtcFromInclusive.Value);
        if (window.UtcToExclusive.HasValue) txnQuery = txnQuery.Where(t => t.TransactionDate < window.UtcToExclusive.Value);

        var stockByWh = await balancesQuery
            .GroupBy(b => new { b.WarehouseId, b.Warehouse.Code, b.Warehouse.Name })
            .Select(g => new WarehouseStockBreakdownDto(
                g.Key.WarehouseId, g.Key.Code, g.Key.Name,
                g.Sum(x => (long)x.QuantityOnHand),
                g.Sum(x => (long)x.QuantityAllocated),
                g.Sum(x => (long)(x.QuantityOnHand - x.QuantityAllocated)),
                g.Sum(x => (long)x.QuantityDamaged)))
            .ToListAsync(cancellationToken);

        // Low-stock calculation comparing authoritative InventoryBalances against InventoryItem.ReorderPoint
        var prodWhStocks = await balancesQuery
            .GroupBy(b => new { b.WarehouseId, b.ProductId })
            .Select(g => new { g.Key.WarehouseId, g.Key.ProductId, Avail = g.Sum(x => x.QuantityOnHand - x.QuantityAllocated) })
            .ToListAsync(cancellationToken);

        var invItemsQuery = _context.InventoryItems.AsNoTracking().Where(i => i.IsActive && i.ReorderPoint > 0);
        if (!isGlobal) invItemsQuery = invItemsQuery.Where(i => branches.Contains(i.Warehouse.BranchId));
        if (filter.WarehouseId.HasValue) invItemsQuery = invItemsQuery.Where(i => i.WarehouseId == filter.WarehouseId.Value);
        var rules = await invItemsQuery.Select(i => new { i.WarehouseId, i.ProductId, i.ReorderPoint }).ToListAsync(cancellationToken);
        var stockLookup = prodWhStocks.ToDictionary(x => (x.WarehouseId, x.ProductId), x => x.Avail);
        var lowStockCount = rules.Count(r => stockLookup.GetValueOrDefault((r.WarehouseId, r.ProductId), 0) <= r.ReorderPoint);

        var txnByType = await txnQuery.GroupBy(t => t.TransactionType)
            .Select(g => new StatusBreakdownDto(g.Key, g.Count(), 0m))
            .ToListAsync(cancellationToken);

        return new InventoryDashboardDto
        {
            Summary = snapshot,
            ActiveReservationsCount = await resQuery.CountAsync(cancellationToken),
            ReservedUnitsTotal = await resQuery.SumAsync(r => (long?)r.ReservedQuantity, cancellationToken) ?? 0L,
            LowStockProductWarehouseCount = lowStockCount,
            DistinctLotsCount = await balancesQuery.Where(b => b.LotId != null && b.QuantityOnHand > 0).Select(b => b.LotId!.Value).Distinct().CountAsync(cancellationToken),
            SerialNumbersAvailableCount = await serialQuery.CountAsync(cancellationToken),
            TransactionsCountInPeriod = txnByType.Sum(x => x.Count),
            TransactionsByType = txnByType,
            StockByWarehouse = stockByWh
        };
    }

    public async Task<FinanceDashboardDto> GetFinanceDashboardAsync(ReportFilterRequest filter, CancellationToken cancellationToken = default)
    {
        ReportingQueryHelpers.ConfigureReadOnlyTimeout(_context);
        var (isGlobal, branches) = ReportingQueryHelpers.ResolveBranchScope(_currentUser, filter.BranchId);
        var window = ReportingQueryHelpers.ResolveTimeWindow(filter);

        var charges = _context.ShipmentCharges.AsNoTracking().AsQueryable();
        var invoices = _context.Invoices.AsNoTracking().AsQueryable();
        var payments = _context.Payments.AsNoTracking().AsQueryable();
        var balances = _context.CustomerBalances.AsNoTracking().AsQueryable();

        if (!isGlobal)
        {
            charges = charges.Where(c => branches.Contains(c.Shipment.OriginBranchId) || branches.Contains(c.Shipment.DestinationBranchId));
            invoices = invoices.Where(i => branches.Contains(i.Customer.BranchId));
            payments = payments.Where(p => branches.Contains(p.Customer.BranchId));
            balances = balances.Where(b => branches.Contains(b.Customer.BranchId));
        }

        if (filter.CustomerId.HasValue)
        {
            charges = charges.Where(c => c.Shipment.CustomerId == filter.CustomerId.Value);
            invoices = invoices.Where(i => i.CustomerId == filter.CustomerId.Value);
            payments = payments.Where(p => p.CustomerId == filter.CustomerId.Value);
            balances = balances.Where(b => b.CustomerId == filter.CustomerId.Value);
        }

        if (window.UtcFromInclusive.HasValue)
        {
            charges = charges.Where(c => c.CreatedAt >= window.UtcFromInclusive.Value);
            payments = payments.Where(p => p.PaymentDate >= window.UtcFromInclusive.Value);
        }
        if (window.UtcToExclusive.HasValue)
        {
            charges = charges.Where(c => c.CreatedAt < window.UtcToExclusive.Value);
            payments = payments.Where(p => p.PaymentDate < window.UtcToExclusive.Value);
        }
        if (window.StartDate.HasValue) invoices = invoices.Where(i => i.InvoiceDate >= window.StartDate.Value);
        if (window.EndDate.HasValue) invoices = invoices.Where(i => i.InvoiceDate <= window.EndDate.Value);

        var totalCharges = await charges.SumAsync(c => (decimal?)c.Amount, cancellationToken) ?? 0m;
        var unbilledCharges = await charges.Where(c => !c.IsInvoiced).SumAsync(c => (decimal?)c.Amount, cancellationToken) ?? 0m;

        var invGroups = await invoices.GroupBy(i => i.Status)
            .Select(g => new { Status = g.Key, Count = g.Count(), Total = g.Sum(x => x.TotalAmount) })
            .ToListAsync(cancellationToken);

        var draftCount = invGroups.Where(g => g.Status == "Draft").Sum(g => g.Count);
        var draftTotal = invGroups.Where(g => g.Status == "Draft").Sum(g => g.Total);
        var activeStatuses = new[] { "Issued", "PartiallyPaid", "Paid", "Overdue" };
        var issuedCount = invGroups.Where(g => activeStatuses.Contains(g.Status)).Sum(g => g.Count);
        var issuedTotal = invGroups.Where(g => activeStatuses.Contains(g.Status)).Sum(g => g.Total);
        var cancelledCount = invGroups.Where(g => g.Status == "Cancelled").Sum(g => g.Count);
        var cancelledTotal = invGroups.Where(g => g.Status == "Cancelled").Sum(g => g.Total);

        var confirmedPayments = payments.Where(p => p.Status == "Confirmed");
        var totalPayments = await confirmedPayments.SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;
        var totalAllocated = await confirmedPayments.SelectMany(p => p.Allocations).SumAsync(a => (decimal?)a.AllocatedAmount, cancellationToken) ?? 0m;

        var invTrendDates = await invoices.Where(i => activeStatuses.Contains(i.Status))
            .Select(i => new { i.InvoiceDate, i.TotalAmount })
            .ToListAsync(cancellationToken);

        var payTrendDates = await confirmedPayments
            .Select(p => new { p.PaymentDate, p.Amount })
            .ToListAsync(cancellationToken);

        return new FinanceDashboardDto
        {
            TotalShipmentChargesAmount = totalCharges,
            UnbilledShipmentChargesAmount = unbilledCharges,
            DraftInvoicesCount = draftCount,
            DraftInvoicesTotal = draftTotal,
            IssuedAndActiveInvoicesCount = issuedCount,
            IssuedInvoicesTotal = issuedTotal,
            CancelledInvoicesCount = cancelledCount,
            CancelledInvoicesTotal = cancelledTotal,
            TotalPaymentsConfirmed = totalPayments,
            TotalPaymentsAllocated = totalAllocated,
            TotalUnallocatedPaymentCredit = Math.Max(0m, totalPayments - totalAllocated),
            AuthoritativeCustomerReceivables = await balances.SumAsync(b => (decimal?)b.CurrentOutstandingBalance, cancellationToken) ?? 0m,
            AuthoritativeUnbilledBalance = await balances.SumAsync(b => (decimal?)b.UnbilledChargesAmount, cancellationToken) ?? 0m,
            InvoicingTrend = invTrendDates.GroupBy(x => x.InvoiceDate)
                .OrderBy(g => g.Key)
                .Select(g => new DateTrendPointDto(g.Key, g.Count(), g.Sum(x => x.TotalAmount)))
                .ToList(),
            PaymentCollectionTrend = AggregateDailyTrendWithValue(
                payTrendDates.Select(x => (x.PaymentDate, x.Amount)), window.TimeZoneId)
        };
    }

    public async Task<CodDashboardDto> GetCodDashboardAsync(ReportFilterRequest filter, CancellationToken cancellationToken = default)
    {
        ReportingQueryHelpers.ConfigureReadOnlyTimeout(_context);
        var (isGlobal, branches) = ReportingQueryHelpers.ResolveBranchScope(_currentUser, filter.BranchId);
        var window = ReportingQueryHelpers.ResolveTimeWindow(filter);

        var snapshot = await BuildCodSnapshotAsync(isGlobal, branches, window, cancellationToken);

        var colQuery = _context.CODCollections.AsNoTracking().AsQueryable();
        var remQuery = _context.CODRemittances.AsNoTracking().Where(r => r.Status == "Completed");

        if (!isGlobal)
        {
            colQuery = colQuery.Where(c => branches.Contains(c.Shipment.DestinationBranchId));
            remQuery = remQuery.Where(r => branches.Contains(r.Customer.BranchId));
        }
        if (filter.CustomerId.HasValue)
        {
            colQuery = colQuery.Where(c => c.Shipment.CustomerId == filter.CustomerId.Value);
            remQuery = remQuery.Where(r => r.CustomerId == filter.CustomerId.Value);
        }
        if (window.UtcFromInclusive.HasValue)
        {
            colQuery = colQuery.Where(c => c.CollectionDate >= window.UtcFromInclusive.Value);
            remQuery = remQuery.Where(r => (r.RemittedAt ?? r.RemittanceDate) >= window.UtcFromInclusive.Value);
        }
        if (window.UtcToExclusive.HasValue)
        {
            colQuery = colQuery.Where(c => c.CollectionDate < window.UtcToExclusive.Value);
            remQuery = remQuery.Where(r => (r.RemittedAt ?? r.RemittanceDate) < window.UtcToExclusive.Value);
        }

        var byStatus = await colQuery.GroupBy(c => c.Status)
            .Select(g => new StatusBreakdownDto(g.Key, g.Count(), g.Sum(x => x.CollectedAmount)))
            .ToListAsync(cancellationToken);

        var byBranchRaw = await colQuery
            .GroupBy(c => new { c.Shipment.DestinationBranchId, c.Shipment.DestinationBranch.Code, c.Shipment.DestinationBranch.Name })
            .Select(g => new { g.Key.DestinationBranchId, g.Key.Code, g.Key.Name, Count = g.Count(), Amount = g.Sum(x => x.CollectedAmount) })
            .OrderByDescending(x => x.Amount)
            .ToListAsync(cancellationToken);

        var byBranch = byBranchRaw
            .Select(x => new BranchBreakdownDto(x.DestinationBranchId, x.Code, x.Name, x.Count, x.Amount))
            .ToList();

        var colDates = await colQuery.Select(c => new { c.CollectionDate, c.CollectedAmount }).ToListAsync(cancellationToken);

        return new CodDashboardDto
        {
            ExpectedCodOnShipments = snapshot.TotalExpectedCodOnShipments,
            CollectedByDrivers = snapshot.TotalCollectedByDrivers,
            DepositedToBranches = snapshot.TotalDepositedToBranches,
            VerifiedByCashiers = snapshot.TotalVerifiedByCashiers,
            RemittedToCustomersNet = snapshot.TotalRemittedToCustomers,
            OutstandingDriverCashInHand = snapshot.OutstandingDriverCashInHand,
            UnremittedVerifiedHolding = snapshot.TotalCustomerCodHoldingBalance,
            ExpectedVsCollectedDiscrepancy = snapshot.TotalExpectedCodOnShipments - snapshot.TotalCollectedByDrivers,
            CollectionsCount = byStatus.Sum(x => x.Count),
            CompletedRemittancesCount = await remQuery.CountAsync(cancellationToken),
            CollectionsByStatus = byStatus,
            ByBranch = byBranch,
            CollectionTrend = AggregateDailyTrendWithValue(colDates.Select(x => (x.CollectionDate, x.CollectedAmount)), window.TimeZoneId)
        };
    }

    private async Task<InventorySnapshotDto> BuildInventorySnapshotAsync(
        bool isGlobal, IReadOnlyList<long> branches, long? warehouseId, CancellationToken ct)
    {
        var q = _context.InventoryBalances.AsNoTracking().AsQueryable();
        if (!isGlobal) q = q.Where(b => branches.Contains(b.Warehouse.BranchId));
        if (warehouseId.HasValue) q = q.Where(b => b.WarehouseId == warehouseId.Value);

        var distinctProducts = await q.Where(b => b.QuantityOnHand > 0).Select(b => b.ProductId).Distinct().CountAsync(ct);
        var onHand = await q.SumAsync(b => (long?)b.QuantityOnHand, ct) ?? 0L;
        var allocated = await q.SumAsync(b => (long?)b.QuantityAllocated, ct) ?? 0L;
        var damaged = await q.SumAsync(b => (long?)b.QuantityDamaged, ct) ?? 0L;

        return new InventorySnapshotDto
        {
            DistinctProductsInStock = distinctProducts,
            TotalQuantityOnHand = onHand,
            TotalQuantityAllocated = allocated,
            TotalQuantityAvailable = onHand - allocated,
            TotalQuantityDamaged = damaged
        };
    }

    private async Task<FinanceSnapshotDto> BuildFinanceSnapshotAsync(
        bool isGlobal, IReadOnlyList<long> branches, ReportTimeWindow window, CancellationToken ct)
    {
        var charges = _context.ShipmentCharges.AsNoTracking().AsQueryable();
        var invoices = _context.Invoices.AsNoTracking().Where(i => i.Status != "Draft" && i.Status != "Cancelled");
        var payments = _context.Payments.AsNoTracking().Where(p => p.Status == "Confirmed");
        var balances = _context.CustomerBalances.AsNoTracking().AsQueryable();

        if (!isGlobal)
        {
            charges = charges.Where(c => branches.Contains(c.Shipment.OriginBranchId) || branches.Contains(c.Shipment.DestinationBranchId));
            invoices = invoices.Where(i => branches.Contains(i.Customer.BranchId));
            payments = payments.Where(p => branches.Contains(p.Customer.BranchId));
            balances = balances.Where(b => branches.Contains(b.Customer.BranchId));
        }

        if (window.UtcFromInclusive.HasValue)
        {
            charges = charges.Where(c => c.CreatedAt >= window.UtcFromInclusive.Value);
            payments = payments.Where(p => p.PaymentDate >= window.UtcFromInclusive.Value);
        }
        if (window.UtcToExclusive.HasValue)
        {
            charges = charges.Where(c => c.CreatedAt < window.UtcToExclusive.Value);
            payments = payments.Where(p => p.PaymentDate < window.UtcToExclusive.Value);
        }
        if (window.StartDate.HasValue) invoices = invoices.Where(i => i.InvoiceDate >= window.StartDate.Value);
        if (window.EndDate.HasValue) invoices = invoices.Where(i => i.InvoiceDate <= window.EndDate.Value);

        var totalPayments = await payments.SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;
        var allocated = await payments.SelectMany(p => p.Allocations).SumAsync(a => (decimal?)a.AllocatedAmount, ct) ?? 0m;

        return new FinanceSnapshotDto
        {
            TotalShipmentCharges = await charges.SumAsync(c => (decimal?)c.Amount, ct) ?? 0m,
            TotalIssuedInvoiceAmount = await invoices.SumAsync(i => (decimal?)i.TotalAmount, ct) ?? 0m,
            TotalPaymentsConfirmed = totalPayments,
            TotalPaymentsAllocated = allocated,
            TotalCustomerOutstandingReceivables = await balances.SumAsync(b => (decimal?)b.CurrentOutstandingBalance, ct) ?? 0m,
            TotalUnbilledCharges = await balances.SumAsync(b => (decimal?)b.UnbilledChargesAmount, ct) ?? 0m
        };
    }

    private async Task<CodSnapshotDto> BuildCodSnapshotAsync(
        bool isGlobal, IReadOnlyList<long> branches, ReportTimeWindow window, CancellationToken ct)
    {
        // Expected COD from authoritative COD shipments (excluding Cancelled)
        var shipQuery = _context.Shipments.AsNoTracking().Where(s => s.CodAmount > 0 && s.ShipmentStatus != "Cancelled");
        var colQuery = _context.CODCollections.AsNoTracking().AsQueryable();
        var remQuery = _context.CODRemittances.AsNoTracking().Where(r => r.Status == "Completed");
        var balQuery = _context.CustomerBalances.AsNoTracking().AsQueryable();

        if (!isGlobal)
        {
            shipQuery = shipQuery.Where(s => branches.Contains(s.DestinationBranchId));
            colQuery = colQuery.Where(c => branches.Contains(c.Shipment.DestinationBranchId));
            remQuery = remQuery.Where(r => branches.Contains(r.Customer.BranchId));
            balQuery = balQuery.Where(b => branches.Contains(b.Customer.BranchId));
        }

        if (window.UtcFromInclusive.HasValue)
        {
            shipQuery = shipQuery.Where(s => s.CreatedAt >= window.UtcFromInclusive.Value);
            colQuery = colQuery.Where(c => c.CollectionDate >= window.UtcFromInclusive.Value);
            remQuery = remQuery.Where(r => (r.RemittedAt ?? r.RemittanceDate) >= window.UtcFromInclusive.Value);
        }
        if (window.UtcToExclusive.HasValue)
        {
            shipQuery = shipQuery.Where(s => s.CreatedAt < window.UtcToExclusive.Value);
            colQuery = colQuery.Where(c => c.CollectionDate < window.UtcToExclusive.Value);
            remQuery = remQuery.Where(r => (r.RemittedAt ?? r.RemittanceDate) < window.UtcToExclusive.Value);
        }

        var expectedCod = await shipQuery.SumAsync(s => (decimal?)s.CodAmount, ct) ?? 0m;
        var collected = await colQuery.SumAsync(c => (decimal?)c.CollectedAmount, ct) ?? 0m;
        var deposited = await colQuery.Where(c => c.Status == "DepositedToBranch" || c.Status == "VerifiedByCashier" || c.Status == "RemittedToCustomer")
            .SumAsync(c => (decimal?)c.CollectedAmount, ct) ?? 0m;
        var verified = await colQuery.Where(c => c.Status == "VerifiedByCashier" || c.Status == "RemittedToCustomer")
            .SumAsync(c => (decimal?)c.CollectedAmount, ct) ?? 0m;
        var remitted = await remQuery.SumAsync(r => (decimal?)r.NetRemittanceAmount, ct) ?? 0m;
        var holding = await balQuery.SumAsync(b => (decimal?)b.TotalCodHoldingAmount, ct) ?? 0m;

        return new CodSnapshotDto
        {
            TotalExpectedCodOnShipments = expectedCod,
            TotalCollectedByDrivers = collected,
            TotalDepositedToBranches = deposited,
            TotalVerifiedByCashiers = verified,
            TotalRemittedToCustomers = remitted,
            OutstandingDriverCashInHand = Math.Max(0m, collected - deposited),
            TotalCustomerCodHoldingBalance = holding
        };
    }

    private static List<DateTrendPointDto> AggregateDailyTrend(IEnumerable<DateTime> utcDates, string timeZoneId)
    {
        var tz = CronScheduleHelper.ResolveTimeZone(timeZoneId);
        return utcDates
            .Select(d => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(d, DateTimeKind.Utc), tz)))
            .GroupBy(d => d)
            .OrderBy(g => g.Key)
            .Select(g => new DateTrendPointDto(g.Key, g.Count(), 0m))
            .ToList();
    }

    private static List<DateTrendPointDto> AggregateDailyTrendWithValue(IEnumerable<(DateTime Date, decimal Value)> items, string timeZoneId)
    {
        var tz = CronScheduleHelper.ResolveTimeZone(timeZoneId);
        return items
            .Select(x => (Date: DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(x.Date, DateTimeKind.Utc), tz)), x.Value))
            .GroupBy(x => x.Date)
            .OrderBy(g => g.Key)
            .Select(g => new DateTrendPointDto(g.Key, g.Count(), g.Sum(i => i.Value)))
            .ToList();
    }
}
