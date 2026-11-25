using Logistics.Server.Application.Common.Interfaces;
using Logistics.Server.Application.Common.Models;
using Logistics.Server.Application.Modules.Reporting.DTOs;
using Logistics.Server.Application.Modules.Reporting.Services;
using Logistics.Server.Infrastructure.Persistence;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Logistics.Server.Infrastructure.Services.Reporting;

public sealed class FinancialReportService : IFinancialReportService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public FinancialReportService(ApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<ShippingChargeReportRowDto>> GetShippingChargesReportAsync(
        ReportFilterRequest filter,
        bool? isInvoiced,
        PagedRequest pagedRequest,
        CancellationToken cancellationToken = default)
    {
        ReportingQueryHelpers.ConfigureReadOnlyTimeout(_context);
        var (isGlobal, branches) = ReportingQueryHelpers.ResolveBranchScope(_currentUser, filter.BranchId);
        var window = ReportingQueryHelpers.ResolveTimeWindow(filter);

        var query = _context.ShipmentCharges.AsNoTracking().AsQueryable();
        if (!isGlobal)
            query = query.Where(c => branches.Contains(c.Shipment.OriginBranchId) || branches.Contains(c.Shipment.DestinationBranchId));
        if (filter.CustomerId.HasValue)
            query = query.Where(c => c.Shipment.CustomerId == filter.CustomerId.Value);
        if (isInvoiced.HasValue)
            query = query.Where(c => c.IsInvoiced == isInvoiced.Value);
        if (!string.IsNullOrWhiteSpace(filter.Status))
            query = query.Where(c => c.ChargeType == filter.Status);
        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(c =>
                c.Shipment.TrackingNumber.Contains(filter.Search) ||
                c.Shipment.Customer.Name.Contains(filter.Search) ||
                (c.Invoice != null && c.Invoice.InvoiceNumber.Contains(filter.Search)));
        if (window.UtcFromInclusive.HasValue)
            query = query.Where(c => c.CreatedAt >= window.UtcFromInclusive.Value);
        if (window.UtcToExclusive.HasValue)
            query = query.Where(c => c.CreatedAt < window.UtcToExclusive.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(c => c.CreatedAt).ThenByDescending(c => c.Id)
            .Skip((pagedRequest.Page - 1) * pagedRequest.PageSize)
            .Take(pagedRequest.PageSize)
            .Select(c => new ShippingChargeReportRowDto
            {
                ChargeId = c.Id,
                ShipmentId = c.ShipmentId,
                TrackingNumber = c.Shipment.TrackingNumber,
                CustomerName = c.Shipment.Customer.Name,
                ChargeType = c.ChargeType,
                Amount = c.Amount,
                CurrencyCode = c.Currency.Code,
                IsInvoiced = c.IsInvoiced,
                InvoiceNumber = c.Invoice != null ? c.Invoice.InvoiceNumber : null,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<ShippingChargeReportRowDto>(items, total, pagedRequest.Page, pagedRequest.PageSize);
    }

    public async Task<PagedResult<InvoiceReportRowDto>> GetInvoicesReportAsync(
        ReportFilterRequest filter,
        PagedRequest pagedRequest,
        CancellationToken cancellationToken = default)
    {
        ReportingQueryHelpers.ConfigureReadOnlyTimeout(_context);
        var (isGlobal, branches) = ReportingQueryHelpers.ResolveBranchScope(_currentUser, filter.BranchId);
        var window = ReportingQueryHelpers.ResolveTimeWindow(filter);

        var query = _context.Invoices.AsNoTracking().AsQueryable();
        if (!isGlobal)
            query = query.Where(i => branches.Contains(i.Customer.BranchId));
        if (filter.CustomerId.HasValue)
            query = query.Where(i => i.CustomerId == filter.CustomerId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Status))
            query = query.Where(i => i.Status == filter.Status);
        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(i => i.InvoiceNumber.Contains(filter.Search) || i.Customer.Name.Contains(filter.Search));
        if (window.StartDate.HasValue)
            query = query.Where(i => i.InvoiceDate >= window.StartDate.Value);
        if (window.EndDate.HasValue)
            query = query.Where(i => i.InvoiceDate <= window.EndDate.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(i => i.InvoiceDate).ThenByDescending(i => i.Id)
            .Skip((pagedRequest.Page - 1) * pagedRequest.PageSize)
            .Take(pagedRequest.PageSize)
            .Select(i => new InvoiceReportRowDto
            {
                InvoiceId = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                CustomerId = i.CustomerId,
                CustomerName = i.Customer.Name,
                BranchCode = i.Customer.Branch.Code,
                InvoiceDate = i.InvoiceDate,
                DueDate = i.DueDate,
                Status = i.Status,
                SubTotal = i.SubTotal,
                TaxAmount = i.TaxAmount,
                TotalAmount = i.TotalAmount,
                PaidAmount = i.PaidAmount,
                // Compute directly in LINQ so EF Core InMemory and SQL Server agree without computed-column gaps
                OutstandingAmount = i.TotalAmount - i.PaidAmount
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<InvoiceReportRowDto>(items, total, pagedRequest.Page, pagedRequest.PageSize);
    }

    public async Task<PagedResult<PaymentReportRowDto>> GetPaymentsReportAsync(
        ReportFilterRequest filter,
        PagedRequest pagedRequest,
        CancellationToken cancellationToken = default)
    {
        ReportingQueryHelpers.ConfigureReadOnlyTimeout(_context);
        var (isGlobal, branches) = ReportingQueryHelpers.ResolveBranchScope(_currentUser, filter.BranchId);
        var window = ReportingQueryHelpers.ResolveTimeWindow(filter);

        var query = _context.Payments.AsNoTracking().AsQueryable();
        if (!isGlobal)
            query = query.Where(p => branches.Contains(p.Customer.BranchId));
        if (filter.CustomerId.HasValue)
            query = query.Where(p => p.CustomerId == filter.CustomerId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Status))
            query = query.Where(p => p.Status == filter.Status);
        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(p => p.PaymentNumber.Contains(filter.Search) || p.Customer.Name.Contains(filter.Search));
        if (window.UtcFromInclusive.HasValue)
            query = query.Where(p => p.PaymentDate >= window.UtcFromInclusive.Value);
        if (window.UtcToExclusive.HasValue)
            query = query.Where(p => p.PaymentDate < window.UtcToExclusive.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(p => p.PaymentDate).ThenByDescending(p => p.Id)
            .Skip((pagedRequest.Page - 1) * pagedRequest.PageSize)
            .Take(pagedRequest.PageSize)
            .Select(p => new PaymentReportRowDto
            {
                PaymentId = p.Id,
                PaymentNumber = p.PaymentNumber,
                CustomerName = p.Customer.Name,
                BranchCode = p.Customer.Branch.Code,
                PaymentDate = p.PaymentDate,
                PaymentMethod = p.PaymentMethod,
                Status = p.Status,
                Amount = p.Amount,
                AllocatedAmount = p.Allocations.Sum(a => (decimal?)a.AllocatedAmount) ?? 0m,
                UnallocatedAmount = p.Amount - (p.Allocations.Sum(a => (decimal?)a.AllocatedAmount) ?? 0m)
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<PaymentReportRowDto>(items, total, pagedRequest.Page, pagedRequest.PageSize);
    }

    public async Task<PagedResult<ReceivableReportRowDto>> GetReceivablesReportAsync(
        ReportFilterRequest filter,
        PagedRequest pagedRequest,
        CancellationToken cancellationToken = default)
    {
        ReportingQueryHelpers.ConfigureReadOnlyTimeout(_context);
        var (isGlobal, branches) = ReportingQueryHelpers.ResolveBranchScope(_currentUser, filter.BranchId);

        var query = _context.CustomerBalances.AsNoTracking().AsQueryable();
        if (!isGlobal)
            query = query.Where(b => branches.Contains(b.Customer.BranchId));
        if (filter.CustomerId.HasValue)
            query = query.Where(b => b.CustomerId == filter.CustomerId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(b => b.Customer.CustomerCode.Contains(filter.Search) || b.Customer.Name.Contains(filter.Search));

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(b => b.CurrentOutstandingBalance)
            .ThenByDescending(b => b.UnbilledChargesAmount)
            .ThenBy(b => b.CustomerId)
            .Skip((pagedRequest.Page - 1) * pagedRequest.PageSize)
            .Take(pagedRequest.PageSize)
            .Select(b => new ReceivableReportRowDto
            {
                CustomerId = b.CustomerId,
                CustomerCode = b.Customer.CustomerCode,
                CustomerName = b.Customer.Name,
                BranchCode = b.Customer.Branch.Code,
                CurrentOutstandingBalance = b.CurrentOutstandingBalance,
                UnbilledChargesAmount = b.UnbilledChargesAmount,
                TotalCodHoldingAmount = b.TotalCodHoldingAmount,
                CreditLimit = b.Customer.CreditLimit,
                LastUpdatedDate = b.LastUpdatedDate
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<ReceivableReportRowDto>(items, total, pagedRequest.Page, pagedRequest.PageSize);
    }

    public async Task<PagedResult<CodCollectionReportRowDto>> GetCodCollectionsReportAsync(
        ReportFilterRequest filter,
        PagedRequest pagedRequest,
        CancellationToken cancellationToken = default)
    {
        ReportingQueryHelpers.ConfigureReadOnlyTimeout(_context);
        var (isGlobal, branches) = ReportingQueryHelpers.ResolveBranchScope(_currentUser, filter.BranchId);
        var window = ReportingQueryHelpers.ResolveTimeWindow(filter);

        var query = _context.CODCollections.AsNoTracking().AsQueryable();
        if (!isGlobal)
            query = query.Where(c => branches.Contains(c.Shipment.DestinationBranchId));
        if (filter.CustomerId.HasValue)
            query = query.Where(c => c.Shipment.CustomerId == filter.CustomerId.Value);
        if (filter.EmployeeId.HasValue)
            query = query.Where(c => c.CollectedByEmployeeId == filter.EmployeeId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Status))
            query = query.Where(c => c.Status == filter.Status);
        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(c =>
                c.Shipment.TrackingNumber.Contains(filter.Search) ||
                c.Shipment.Customer.Name.Contains(filter.Search) ||
                c.CollectedByEmployee.FullName.Contains(filter.Search));
        if (window.UtcFromInclusive.HasValue)
            query = query.Where(c => c.CollectionDate >= window.UtcFromInclusive.Value);
        if (window.UtcToExclusive.HasValue)
            query = query.Where(c => c.CollectionDate < window.UtcToExclusive.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(c => c.CollectionDate).ThenByDescending(c => c.Id)
            .Skip((pagedRequest.Page - 1) * pagedRequest.PageSize)
            .Take(pagedRequest.PageSize)
            .Select(c => new CodCollectionReportRowDto
            {
                CollectionId = c.Id,
                TrackingNumber = c.Shipment.TrackingNumber,
                DestinationBranchCode = c.Shipment.DestinationBranch.Code,
                CustomerName = c.Shipment.Customer.Name,
                CollectedByEmployeeName = c.CollectedByEmployee.FullName,
                ExpectedCodAmount = c.Shipment.CodAmount,
                CollectedAmount = c.CollectedAmount,
                VarianceAmount = c.CollectedAmount - c.Shipment.CodAmount,
                Status = c.Status,
                CollectionDate = c.CollectionDate,
                VerifiedAt = c.VerifiedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<CodCollectionReportRowDto>(items, total, pagedRequest.Page, pagedRequest.PageSize);
    }

    public async Task<PagedResult<CodRemittanceReportRowDto>> GetCodRemittancesReportAsync(
        ReportFilterRequest filter,
        PagedRequest pagedRequest,
        CancellationToken cancellationToken = default)
    {
        ReportingQueryHelpers.ConfigureReadOnlyTimeout(_context);
        var (isGlobal, branches) = ReportingQueryHelpers.ResolveBranchScope(_currentUser, filter.BranchId);
        var window = ReportingQueryHelpers.ResolveTimeWindow(filter);

        var query = _context.CODRemittances.AsNoTracking().AsQueryable();
        if (!isGlobal)
            query = query.Where(r => branches.Contains(r.Customer.BranchId));
        if (filter.CustomerId.HasValue)
            query = query.Where(r => r.CustomerId == filter.CustomerId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Status))
            query = query.Where(r => r.Status == filter.Status);
        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(r => r.RemittanceNumber.Contains(filter.Search) || r.Customer.Name.Contains(filter.Search));
        if (window.UtcFromInclusive.HasValue)
            query = query.Where(r => r.RemittanceDate >= window.UtcFromInclusive.Value);
        if (window.UtcToExclusive.HasValue)
            query = query.Where(r => r.RemittanceDate < window.UtcToExclusive.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(r => r.RemittanceDate).ThenByDescending(r => r.Id)
            .Skip((pagedRequest.Page - 1) * pagedRequest.PageSize)
            .Take(pagedRequest.PageSize)
            .Select(r => new CodRemittanceReportRowDto
            {
                RemittanceId = r.Id,
                RemittanceNumber = r.RemittanceNumber,
                CustomerName = r.Customer.Name,
                BranchCode = r.Customer.Branch.Code,
                RemittanceDate = r.RemittanceDate,
                Status = r.Status,
                TotalCodAmount = r.TotalCodAmount,
                TotalServiceFeesDeducted = r.TotalServiceFeesDeducted,
                NetRemittanceAmount = r.NetRemittanceAmount,
                ItemsCount = r.RemittanceItems.Count,
                RemittedAt = r.RemittedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<CodRemittanceReportRowDto>(items, total, pagedRequest.Page, pagedRequest.PageSize);
    }

    public async Task<PagedResult<CodReconciliationReportRowDto>> GetCodReconciliationReportAsync(
        ReportFilterRequest filter,
        PagedRequest pagedRequest,
        CancellationToken cancellationToken = default)
    {
        ReportingQueryHelpers.ConfigureReadOnlyTimeout(_context);
        var (isGlobal, branches) = ReportingQueryHelpers.ResolveBranchScope(_currentUser, filter.BranchId);
        var window = ReportingQueryHelpers.ResolveTimeWindow(filter);

        var branchQuery = _context.Branches.AsNoTracking().Where(b => b.IsActive);
        if (!isGlobal)
            branchQuery = branchQuery.Where(b => branches.Contains(b.Id));
        if (!string.IsNullOrWhiteSpace(filter.Search))
            branchQuery = branchQuery.Where(b => b.Code.Contains(filter.Search) || b.Name.Contains(filter.Search));

        var total = await branchQuery.CountAsync(cancellationToken);
        var pageBranches = await branchQuery
            .OrderBy(b => b.Code)
            .Skip((pagedRequest.Page - 1) * pagedRequest.PageSize)
            .Take(pagedRequest.PageSize)
            .Select(b => new { b.Id, b.Code, b.Name })
            .ToListAsync(cancellationToken);

        if (pageBranches.Count == 0)
            return new PagedResult<CodReconciliationReportRowDto>([], total, pagedRequest.Page, pagedRequest.PageSize);

        var pageBranchIds = pageBranches.Select(b => b.Id).ToList();

        var shipQuery = _context.Shipments.AsNoTracking()
            .Where(s => s.CodAmount > 0 && s.ShipmentStatus != "Cancelled" && pageBranchIds.Contains(s.DestinationBranchId));
        var colQuery = _context.CODCollections.AsNoTracking()
            .Where(c => pageBranchIds.Contains(c.Shipment.DestinationBranchId));
        var remQuery = _context.CODRemittances.AsNoTracking()
            .Where(r => r.Status == "Completed" && pageBranchIds.Contains(r.Customer.BranchId));

        if (filter.CustomerId.HasValue)
        {
            shipQuery = shipQuery.Where(s => s.CustomerId == filter.CustomerId.Value);
            colQuery = colQuery.Where(c => c.Shipment.CustomerId == filter.CustomerId.Value);
            remQuery = remQuery.Where(r => r.CustomerId == filter.CustomerId.Value);
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

        var expectedByBranch = await shipQuery
            .GroupBy(s => s.DestinationBranchId)
            .Select(g => new { BranchId = g.Key, Amount = g.Sum(s => s.CodAmount) })
            .ToDictionaryAsync(x => x.BranchId, x => x.Amount, cancellationToken);

        var colGroups = await colQuery
            .GroupBy(c => new { c.Shipment.DestinationBranchId, c.Status })
            .Select(g => new { g.Key.DestinationBranchId, g.Key.Status, Amount = g.Sum(c => c.CollectedAmount) })
            .ToListAsync(cancellationToken);

        var remittedByBranch = await remQuery
            .GroupBy(r => r.Customer.BranchId)
            .Select(g => new { BranchId = g.Key, Amount = g.Sum(r => r.NetRemittanceAmount) })
            .ToDictionaryAsync(x => x.BranchId, x => x.Amount, cancellationToken);

        var rows = pageBranches.Select(b =>
        {
            var expected = expectedByBranch.GetValueOrDefault(b.Id);
            var branchCols = colGroups.Where(x => x.DestinationBranchId == b.Id).ToList();
            var collected = branchCols.Sum(x => x.Amount);
            var deposited = branchCols
                .Where(x => x.Status is "DepositedToBranch" or "VerifiedByCashier" or "RemittedToCustomer")
                .Sum(x => x.Amount);
            var verified = branchCols
                .Where(x => x.Status is "VerifiedByCashier" or "RemittedToCustomer")
                .Sum(x => x.Amount);
            var remitted = remittedByBranch.GetValueOrDefault(b.Id);

            return new CodReconciliationReportRowDto
            {
                BranchId = b.Id,
                BranchCode = b.Code,
                BranchName = b.Name,
                ExpectedCod = expected,
                CollectedByDrivers = collected,
                DepositedToBranch = deposited,
                VerifiedByCashier = verified,
                RemittedToCustomers = remitted,
                DriverCashInHand = Math.Max(0m, collected - deposited),
                PendingCashierVerification = Math.Max(0m, deposited - verified),
                DiscrepancyAmount = expected - collected
            };
        }).ToList();

        return new PagedResult<CodReconciliationReportRowDto>(rows, total, pagedRequest.Page, pagedRequest.PageSize);
    }
}
