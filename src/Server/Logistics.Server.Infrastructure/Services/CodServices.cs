using Logistics.Server.Application.Common.Events;
using Logistics.Server.Application.Common.Exceptions;
using Logistics.Server.Application.Common.Interfaces;
using Logistics.Server.Application.Common.Models;
using Logistics.Server.Application.Modules.Cod.DTOs;
using Logistics.Server.Application.Modules.Cod.Services;
using Logistics.Server.Domain.Entities.Finance;
using Logistics.Server.Infrastructure.Persistence;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Logistics.Server.Infrastructure.Services;

// ═══════════════════════════════════════════════════════════════
//  1. COD COLLECTION SERVICE
// ═══════════════════════════════════════════════════════════════

public class CodCollectionService : ICodCollectionService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;
    private readonly ILogger<CodCollectionService> _logger;
    private readonly IOutboxService? _outbox;

    public CodCollectionService(
        ApplicationDbContext context,
        ICurrentUserService currentUser,
        IAuditService audit,
        ILogger<CodCollectionService> logger,
        IOutboxService? outbox = null)
    {
        _context = context;
        _currentUser = currentUser;
        _audit = audit;
        _logger = logger;
        _outbox = outbox;
    }

    public async Task<PagedResult<CodCollectionDto>> GetCollectionsAsync(
        long? branchId,
        long? employeeId,
        long? customerId,
        string? status,
        bool? isRemitted,
        DateOnly? fromDate,
        DateOnly? toDate,
        PagedRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _context.CODCollections
            .Include(c => c.Shipment).ThenInclude(s => s.Customer)
            .Include(c => c.Shipment).ThenInclude(s => s.DestinationBranch)
            .Include(c => c.Currency)
            .Include(c => c.CollectedByEmployee)
            .Include(c => c.VerifiedByUser)
            .Include(c => c.RemittanceItem).ThenInclude(ri => ri!.CODRemittance)
            .AsNoTracking();

        if (branchId.HasValue)
        {
            if (!_currentUser.HasBranchAccess(branchId.Value))
                throw new ForbiddenAccessException();
            query = query.Where(c => c.Shipment.DestinationBranchId == branchId.Value);
        }
        else if (!_currentUser.IsInRole("SUPER_ADMIN"))
        {
            var accessible = _currentUser.AccessibleBranchIds;
            query = query.Where(c => accessible.Contains(c.Shipment.DestinationBranchId));
        }

        if (employeeId.HasValue)
            query = query.Where(c => c.CollectedByEmployeeId == employeeId.Value);

        if (customerId.HasValue)
            query = query.Where(c => c.Shipment.CustomerId == customerId.Value);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(c => c.Status == status);

        if (isRemitted.HasValue)
        {
            query = isRemitted.Value
                ? query.Where(c => c.RemittanceItem != null)
                : query.Where(c => c.RemittanceItem == null);
        }

        if (fromDate.HasValue)
        {
            var dtFrom = fromDate.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(c => c.CollectionDate >= dtFrom);
        }

        if (toDate.HasValue)
        {
            var dtTo = toDate.Value.ToDateTime(TimeOnly.MaxValue);
            query = query.Where(c => c.CollectionDate <= dtTo);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var dbItems = await query
            .OrderByDescending(c => c.CollectionDate)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = dbItems.Select(MapToDto).ToList();
        return new PagedResult<CodCollectionDto>(items, totalCount, request.Page, request.PageSize);
    }

    public async Task<CodCollectionDto?> GetCollectionByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var col = await _context.CODCollections
            .Include(c => c.Shipment).ThenInclude(s => s.Customer)
            .Include(c => c.Shipment).ThenInclude(s => s.DestinationBranch)
            .Include(c => c.Currency)
            .Include(c => c.CollectedByEmployee)
            .Include(c => c.VerifiedByUser)
            .Include(c => c.RemittanceItem).ThenInclude(ri => ri!.CODRemittance)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (col == null) return null;

        if (!_currentUser.HasBranchAccess(col.Shipment.DestinationBranchId))
            throw new ForbiddenAccessException();

        return MapToDto(col);
    }

    public async Task<CodCollectionDto?> GetCollectionByShipmentIdAsync(long shipmentId, CancellationToken cancellationToken = default)
    {
        var col = await _context.CODCollections
            .Include(c => c.Shipment).ThenInclude(s => s.Customer)
            .Include(c => c.Shipment).ThenInclude(s => s.DestinationBranch)
            .Include(c => c.Currency)
            .Include(c => c.CollectedByEmployee)
            .Include(c => c.VerifiedByUser)
            .Include(c => c.RemittanceItem).ThenInclude(ri => ri!.CODRemittance)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ShipmentId == shipmentId, cancellationToken);

        if (col == null) return null;

        if (!_currentUser.HasBranchAccess(col.Shipment.DestinationBranchId))
            throw new ForbiddenAccessException();

        return MapToDto(col);
    }

    public async Task<CodCollectionDto> CreateCollectionAsync(CreateCodCollectionRequest request, CancellationToken cancellationToken = default)
    {
        var shipment = await _context.Shipments
            .Include(s => s.Customer)
            .Include(s => s.DestinationBranch)
            .FirstOrDefaultAsync(s => s.Id == request.ShipmentId, cancellationToken)
            ?? throw new NotFoundException("Shipment", request.ShipmentId);

        if (!_currentUser.HasBranchAccess(shipment.DestinationBranchId))
            throw new ForbiddenAccessException();

        if (shipment.CodAmount <= 0)
            throw new ConflictException($"Shipment '{shipment.TrackingNumber}' does not require Cash on Delivery (COD amount is 0).");

        var existing = await _context.CODCollections
            .AnyAsync(c => c.ShipmentId == request.ShipmentId, cancellationToken);

        if (existing)
            throw new ConflictException($"A COD collection record already exists for shipment '{shipment.TrackingNumber}'.");

        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == request.CollectedByEmployeeId, cancellationToken)
            ?? throw new NotFoundException("Employee", request.CollectedByEmployeeId);

        if (request.CollectedAmount > shipment.CodAmount)
            throw new ConflictException($"Collected amount ({request.CollectedAmount:N0}) cannot exceed expected COD amount ({shipment.CodAmount:N0}).");

        var collection = new CODCollection
        {
            ShipmentId = shipment.Id,
            CollectedAmount = request.CollectedAmount,
            CurrencyId = shipment.CurrencyId,
            CollectionDate = DateTime.UtcNow,
            CollectedByEmployeeId = employee.Id,
            Status = "CollectedByDriver",
            DriverReceiptNumber = request.DriverReceiptNumber,
            Notes = request.Notes
        };

        await _context.CODCollections.AddAsync(collection, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync("CreateCodCollection", "finance", "CODCollections", collection.Id.ToString(),
            null, new { collection.ShipmentId, collection.CollectedAmount, collection.CollectedByEmployeeId },
            notes: $"Recorded driver COD collection for shipment {shipment.TrackingNumber}", cancellationToken: cancellationToken);

        return (await GetCollectionByIdAsync(collection.Id, cancellationToken))!;
    }

    public async Task<CodCollectionDto> RecordCollectionAsync(long id, RecordCodCollectionRequest request, CancellationToken cancellationToken = default)
    {
        var collection = await _context.CODCollections
            .Include(c => c.Shipment)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException("CODCollection", id);

        if (!_currentUser.HasBranchAccess(collection.Shipment.DestinationBranchId))
            throw new ForbiddenAccessException();

        if (collection.Status == "RemittedToCustomer")
            throw new ConflictException("Cannot modify a COD collection that has already been remitted to customer.");

        var oldAmount = collection.CollectedAmount;
        collection.CollectedAmount = request.CollectedAmount;

        if (request.CollectedByEmployeeId.HasValue)
        {
            var empExists = await _context.Employees.AnyAsync(e => e.Id == request.CollectedByEmployeeId.Value, cancellationToken);
            if (!empExists) throw new NotFoundException("Employee", request.CollectedByEmployeeId.Value);
            collection.CollectedByEmployeeId = request.CollectedByEmployeeId.Value;
        }

        if (!string.IsNullOrWhiteSpace(request.DriverReceiptNumber))
            collection.DriverReceiptNumber = request.DriverReceiptNumber;

        if (request.Notes != null)
            collection.Notes = request.Notes;

        // Adjust customer balance difference
        var diff = request.CollectedAmount - oldAmount;
        if (diff != 0)
        {
            var balance = await _context.CustomerBalances
                .FirstOrDefaultAsync(b => b.CustomerId == collection.Shipment.CustomerId, cancellationToken);
            if (balance != null)
            {
                balance.TotalCodHoldingAmount += diff;
                balance.LastUpdatedDate = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync("UpdateCodCollection", "finance", "CODCollections", collection.Id.ToString(),
            new { CollectedAmount = oldAmount }, new { collection.CollectedAmount },
            notes: "Updated COD collection details", cancellationToken: cancellationToken);

        return (await GetCollectionByIdAsync(collection.Id, cancellationToken))!;
    }

    public async Task<CodCollectionDto> DepositToBranchAsync(long id, string? notes, CancellationToken cancellationToken = default)
    {
        var collection = await _context.CODCollections
            .Include(c => c.Shipment)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException("CODCollection", id);

        if (!_currentUser.HasBranchAccess(collection.Shipment.DestinationBranchId))
            throw new ForbiddenAccessException();

        if (collection.Status != "CollectedByDriver")
            throw new ConflictException($"Cannot deposit to branch from status '{collection.Status}'. Expected 'CollectedByDriver'.");

        collection.Status = "DepositedToBranch";
        if (!string.IsNullOrWhiteSpace(notes))
            collection.Notes = string.IsNullOrWhiteSpace(collection.Notes) ? notes : $"{collection.Notes}; {notes}";

        await _context.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync("DepositCodToBranch", "finance", "CODCollections", collection.Id.ToString(),
            new { Status = "CollectedByDriver" }, new { Status = "DepositedToBranch" },
            notes: notes, cancellationToken: cancellationToken);

        return (await GetCollectionByIdAsync(collection.Id, cancellationToken))!;
    }

    public async Task<CodCollectionDto> VerifyCollectionAsync(long id, VerifyCodCollectionRequest request, CancellationToken cancellationToken = default)
    {
        var collection = await _context.CODCollections
            .Include(c => c.Shipment)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException("CODCollection", id);

        if (!_currentUser.HasBranchAccess(collection.Shipment.DestinationBranchId))
            throw new ForbiddenAccessException();

        if (collection.Status != "DepositedToBranch" && collection.Status != "CollectedByDriver")
            throw new ConflictException($"Cannot verify COD collection from status '{collection.Status}'.");

        var oldStatus = collection.Status;
        collection.Status = "VerifiedByCashier";
        collection.VerifiedByUserId = _currentUser.UserId;
        collection.VerifiedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Notes))
            collection.Notes = string.IsNullOrWhiteSpace(collection.Notes) ? request.Notes : $"{collection.Notes}; Verified: {request.Notes}";

        // Update CustomerBalance.TotalCodHoldingAmount upon cashier verification
        var balance = await _context.CustomerBalances
            .FirstOrDefaultAsync(b => b.CustomerId == collection.Shipment.CustomerId, cancellationToken);
        if (balance == null)
        {
            balance = new CustomerBalance
            {
                CustomerId = collection.Shipment.CustomerId,
                TotalCodHoldingAmount = collection.CollectedAmount,
                LastUpdatedDate = DateTime.UtcNow
            };
            await _context.CustomerBalances.AddAsync(balance, cancellationToken);
        }
        else
        {
            balance.TotalCodHoldingAmount += collection.CollectedAmount;
            balance.LastUpdatedDate = DateTime.UtcNow;
        }

        _outbox?.Enqueue(
            new CodCollectedIntegrationEvent(collection.Id, collection.ShipmentId, collection.Shipment.TrackingNumber,
                collection.Shipment.CustomerId, collection.Shipment.DestinationBranchId, collection.CollectedAmount),
            "CODCollection", collection.Id.ToString());

        await _context.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync("VerifyCodCollection", "finance", "CODCollections", collection.Id.ToString(),
            new { Status = oldStatus }, new { Status = "VerifiedByCashier", collection.VerifiedByUserId, collection.VerifiedAt },
            notes: request.Notes, cancellationToken: cancellationToken);

        return (await GetCollectionByIdAsync(collection.Id, cancellationToken))!;
    }

    public async Task<CodCollectionDto> RecordFailureAsync(long id, RecordCodFailureRequest request, CancellationToken cancellationToken = default)
    {
        var collection = await _context.CODCollections
            .Include(c => c.Shipment)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException("CODCollection", id);

        if (!_currentUser.HasBranchAccess(collection.Shipment.DestinationBranchId))
            throw new ForbiddenAccessException();

        var failureNote = $"Failure: [{request.ReasonCode}] {request.Notes}";
        collection.Notes = string.IsNullOrWhiteSpace(collection.Notes) ? failureNote : $"{collection.Notes}; {failureNote}";

        await _context.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync("RecordCodFailure", "finance", "CODCollections", collection.Id.ToString(),
            null, new { request.ReasonCode, request.Notes },
            notes: failureNote, cancellationToken: cancellationToken);

        return (await GetCollectionByIdAsync(collection.Id, cancellationToken))!;
    }

    public async Task<CodCollectionSummaryDto> GetSummaryAsync(long? branchId, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken = default)
    {
        var query = _context.CODCollections
            .Include(c => c.Shipment)
            .AsNoTracking();

        if (branchId.HasValue)
        {
            if (!_currentUser.HasBranchAccess(branchId.Value))
                throw new ForbiddenAccessException();
            query = query.Where(c => c.Shipment.DestinationBranchId == branchId.Value);
        }
        else if (!_currentUser.IsInRole("SUPER_ADMIN"))
        {
            var accessible = _currentUser.AccessibleBranchIds;
            query = query.Where(c => accessible.Contains(c.Shipment.DestinationBranchId));
        }

        if (fromDate.HasValue)
        {
            var dtFrom = fromDate.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(c => c.CollectionDate >= dtFrom);
        }

        if (toDate.HasValue)
        {
            var dtTo = toDate.Value.ToDateTime(TimeOnly.MaxValue);
            query = query.Where(c => c.CollectionDate <= dtTo);
        }

        var collections = await query.ToListAsync(cancellationToken);

        return new CodCollectionSummaryDto
        {
            TotalExpectedAmount = collections.Sum(c => c.Shipment.CodAmount),
            TotalCollectedAmount = collections.Sum(c => c.CollectedAmount),
            TotalVerifiedAmount = collections.Where(c => c.Status == "VerifiedByCashier" || c.Status == "RemittedToCustomer").Sum(c => c.CollectedAmount),
            TotalRemittedAmount = collections.Where(c => c.Status == "RemittedToCustomer").Sum(c => c.CollectedAmount),
            PendingCollectionCount = 0,
            CollectedByDriverCount = collections.Count(c => c.Status == "CollectedByDriver"),
            DepositedToBranchCount = collections.Count(c => c.Status == "DepositedToBranch"),
            VerifiedByCashierCount = collections.Count(c => c.Status == "VerifiedByCashier"),
            RemittedToCustomerCount = collections.Count(c => c.Status == "RemittedToCustomer")
        };
    }

    private static CodCollectionDto MapToDto(CODCollection c) => new()
    {
        Id = c.Id,
        PublicId = c.PublicId,
        ShipmentId = c.ShipmentId,
        ShipmentTrackingNumber = c.Shipment.TrackingNumber,
        CustomerId = c.Shipment.CustomerId,
        CustomerName = c.Shipment.Customer.Name,
        BranchId = c.Shipment.DestinationBranchId,
        BranchName = c.Shipment.DestinationBranch.Name,
        ExpectedCodAmount = c.Shipment.CodAmount,
        CollectedAmount = c.CollectedAmount,
        CurrencyId = c.CurrencyId,
        CurrencyCode = c.Currency.Code,
        CollectionDate = c.CollectionDate,
        CollectedByEmployeeId = c.CollectedByEmployeeId,
        CollectedByEmployeeName = c.CollectedByEmployee.FullName,
        Status = c.Status,
        DriverReceiptNumber = c.DriverReceiptNumber,
        VerifiedByUserId = c.VerifiedByUserId,
        VerifiedByUsername = c.VerifiedByUser?.Username,
        VerifiedAt = c.VerifiedAt,
        Notes = c.Notes,
        IsRemitted = c.RemittanceItem != null,
        RemittanceId = c.RemittanceItem?.CODRemittanceId,
        RemittanceNumber = c.RemittanceItem?.CODRemittance.RemittanceNumber
    };
}

// ═══════════════════════════════════════════════════════════════
//  2. COD EMPLOYEE SERVICE
// ═══════════════════════════════════════════════════════════════

public class CodEmployeeService : ICodEmployeeService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CodEmployeeService(ApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<CodCollectionDto>> GetMyCollectionsAsync(string? status, PagedRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.EmployeeId.HasValue)
        {
            return new PagedResult<CodCollectionDto>([], 0, request.Page, request.PageSize);
        }

        return await GetEmployeeCollectionsInternalAsync(_currentUser.EmployeeId.Value, status, request, cancellationToken);
    }

    public async Task<EmployeeCodSummaryDto> GetMySummaryAsync(CancellationToken cancellationToken = default)
    {
        if (!_currentUser.EmployeeId.HasValue)
            throw new NotFoundException("Employee", _currentUser.UserId ?? 0);

        return await GetEmployeeSummaryInternalAsync(_currentUser.EmployeeId.Value, cancellationToken);
    }

    public async Task<PagedResult<CodCollectionDto>> GetEmployeeCollectionsAsync(long employeeId, string? status, PagedRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees.FindAsync([employeeId], cancellationToken)
            ?? throw new NotFoundException("Employee", employeeId);

        if (!_currentUser.HasBranchAccess(employee.BranchId) && !_currentUser.IsInRole("SUPER_ADMIN"))
            throw new ForbiddenAccessException();

        return await GetEmployeeCollectionsInternalAsync(employeeId, status, request, cancellationToken);
    }

    public async Task<EmployeeCodSummaryDto> GetEmployeeSummaryAsync(long employeeId, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees.FindAsync([employeeId], cancellationToken)
            ?? throw new NotFoundException("Employee", employeeId);

        if (!_currentUser.HasBranchAccess(employee.BranchId) && !_currentUser.IsInRole("SUPER_ADMIN"))
            throw new ForbiddenAccessException();

        return await GetEmployeeSummaryInternalAsync(employeeId, cancellationToken);
    }

    private async Task<PagedResult<CodCollectionDto>> GetEmployeeCollectionsInternalAsync(long employeeId, string? status, PagedRequest request, CancellationToken cancellationToken)
    {
        var query = _context.CODCollections
            .Include(c => c.Shipment).ThenInclude(s => s.Customer)
            .Include(c => c.Shipment).ThenInclude(s => s.DestinationBranch)
            .Include(c => c.Currency)
            .Include(c => c.CollectedByEmployee)
            .Include(c => c.VerifiedByUser)
            .Include(c => c.RemittanceItem).ThenInclude(ri => ri!.CODRemittance)
            .Where(c => c.CollectedByEmployeeId == employeeId)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(c => c.Status == status);

        var totalCount = await query.CountAsync(cancellationToken);
        var dbItems = await query
            .OrderByDescending(c => c.CollectionDate)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = dbItems.Select(c => new CodCollectionDto
        {
            Id = c.Id,
            PublicId = c.PublicId,
            ShipmentId = c.ShipmentId,
            ShipmentTrackingNumber = c.Shipment.TrackingNumber,
            CustomerId = c.Shipment.CustomerId,
            CustomerName = c.Shipment.Customer.Name,
            BranchId = c.Shipment.DestinationBranchId,
            BranchName = c.Shipment.DestinationBranch.Name,
            ExpectedCodAmount = c.Shipment.CodAmount,
            CollectedAmount = c.CollectedAmount,
            CurrencyId = c.CurrencyId,
            CurrencyCode = c.Currency.Code,
            CollectionDate = c.CollectionDate,
            CollectedByEmployeeId = c.CollectedByEmployeeId,
            CollectedByEmployeeName = c.CollectedByEmployee.FullName,
            Status = c.Status,
            DriverReceiptNumber = c.DriverReceiptNumber,
            VerifiedByUserId = c.VerifiedByUserId,
            VerifiedByUsername = c.VerifiedByUser?.Username,
            VerifiedAt = c.VerifiedAt,
            Notes = c.Notes,
            IsRemitted = c.RemittanceItem != null,
            RemittanceId = c.RemittanceItem?.CODRemittanceId,
            RemittanceNumber = c.RemittanceItem?.CODRemittance.RemittanceNumber
        }).ToList();

        return new PagedResult<CodCollectionDto>(items, totalCount, request.Page, request.PageSize);
    }

    private async Task<EmployeeCodSummaryDto> GetEmployeeSummaryInternalAsync(long employeeId, CancellationToken cancellationToken)
    {
        var employee = await _context.Employees
            .Include(e => e.Branch)
            .FirstOrDefaultAsync(e => e.Id == employeeId, cancellationToken)
            ?? throw new NotFoundException("Employee", employeeId);

        var collections = await _context.CODCollections
            .Include(c => c.Shipment)
            .Where(c => c.CollectedByEmployeeId == employeeId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return new EmployeeCodSummaryDto
        {
            EmployeeId = employee.Id,
            EmployeeCode = employee.Code,
            EmployeeName = employee.FullName,
            BranchId = employee.BranchId,
            BranchName = employee.Branch.Name,
            TotalExpectedAmount = collections.Sum(c => c.Shipment.CodAmount),
            TotalCollectedAmount = collections.Sum(c => c.CollectedAmount),
            TotalDepositedAmount = collections.Where(c => c.Status == "DepositedToBranch" || c.Status == "VerifiedByCashier" || c.Status == "RemittedToCustomer").Sum(c => c.CollectedAmount),
            TotalVerifiedAmount = collections.Where(c => c.Status == "VerifiedByCashier" || c.Status == "RemittedToCustomer").Sum(c => c.CollectedAmount),
            AssignedCount = collections.Count,
            CollectedCount = collections.Count(c => c.Status == "CollectedByDriver"),
            DepositedCount = collections.Count(c => c.Status == "DepositedToBranch"),
            VerifiedCount = collections.Count(c => c.Status == "VerifiedByCashier"),
            DisputedCount = collections.Count(c => c.Notes != null && c.Notes.Contains("Failure"))
        };
    }
}

// ═══════════════════════════════════════════════════════════════
//  3. COD REMITTANCE SERVICE
// ═══════════════════════════════════════════════════════════════

public class CodRemittanceService : ICodRemittanceService
{
    private readonly ApplicationDbContext _context;
    private readonly INumberSequenceService _numberSequence;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;
    private readonly ILogger<CodRemittanceService> _logger;
    private readonly IOutboxService? _outbox;

    public CodRemittanceService(
        ApplicationDbContext context,
        INumberSequenceService numberSequence,
        ICurrentUserService currentUser,
        IAuditService audit,
        ILogger<CodRemittanceService> logger,
        IOutboxService? outbox = null)
    {
        _context = context;
        _numberSequence = numberSequence;
        _currentUser = currentUser;
        _audit = audit;
        _logger = logger;
        _outbox = outbox;
    }

    public async Task<PagedResult<CodRemittanceDto>> GetRemittancesAsync(
        long? customerId,
        string? status,
        DateOnly? fromDate,
        DateOnly? toDate,
        PagedRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _context.CODRemittances
            .Include(r => r.Customer)
            .Include(r => r.Currency)
            .Include(r => r.RemittedByUser)
            .Include(r => r.RemittanceItems).ThenInclude(ri => ri.Shipment)
            .AsNoTracking();

        if (customerId.HasValue)
        {
            var cust = await _context.Customers.FindAsync([customerId.Value], cancellationToken);
            if (cust != null && !_currentUser.HasBranchAccess(cust.BranchId) && !_currentUser.IsInRole("SUPER_ADMIN"))
                throw new ForbiddenAccessException();

            query = query.Where(r => r.CustomerId == customerId.Value);
        }
        else if (!_currentUser.IsInRole("SUPER_ADMIN"))
        {
            var accessible = _currentUser.AccessibleBranchIds;
            query = query.Where(r => accessible.Contains(r.Customer.BranchId));
        }

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(r => r.Status == status);

        if (fromDate.HasValue)
        {
            var dtFrom = fromDate.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(r => r.RemittanceDate >= dtFrom);
        }

        if (toDate.HasValue)
        {
            var dtTo = toDate.Value.ToDateTime(TimeOnly.MaxValue);
            query = query.Where(r => r.RemittanceDate <= dtTo);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var dbItems = await query
            .OrderByDescending(r => r.RemittanceDate)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = dbItems.Select(MapToDto).ToList();
        return new PagedResult<CodRemittanceDto>(items, totalCount, request.Page, request.PageSize);
    }

    public async Task<CodRemittanceDto?> GetRemittanceByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var r = await _context.CODRemittances
            .Include(r => r.Customer)
            .Include(r => r.Currency)
            .Include(r => r.RemittedByUser)
            .Include(r => r.RemittanceItems).ThenInclude(ri => ri.Shipment)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (r == null) return null;

        if (!_currentUser.HasBranchAccess(r.Customer.BranchId) && !_currentUser.IsInRole("SUPER_ADMIN"))
            throw new ForbiddenAccessException();

        return MapToDto(r);
    }

    public async Task<CodRemittanceDto> CreateRemittanceAsync(CreateCodRemittanceRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken)
            ?? throw new NotFoundException("Customer", request.CustomerId);

        if (!_currentUser.HasBranchAccess(customer.BranchId) && !_currentUser.IsInRole("SUPER_ADMIN"))
            throw new ForbiddenAccessException();

        var currency = await _context.Currencies
            .FirstOrDefaultAsync(c => c.Id == request.CurrencyId, cancellationToken)
            ?? throw new NotFoundException("Currency", request.CurrencyId);

        // Fetch selected collections
        var collections = await _context.CODCollections
            .Include(c => c.Shipment)
            .Include(c => c.RemittanceItem)
            .Where(c => request.CollectionIds.Contains(c.Id))
            .ToListAsync(cancellationToken);

        if (collections.Count != request.CollectionIds.Count)
            throw new ConflictException("One or more selected COD collections could not be found.");

        foreach (var col in collections)
        {
            if (col.Shipment.CustomerId != customer.Id)
                throw new ConflictException($"COD Collection #{col.Id} belongs to a different customer.");

            if (col.RemittanceItem != null || col.Status == "RemittedToCustomer")
                throw new ConflictException($"COD Collection #{col.Id} has already been included in a remittance.");

            if (col.CurrencyId != currency.Id)
                throw new ConflictException($"COD Collection #{col.Id} currency does not match remittance currency.");
        }

        var totalCod = collections.Sum(c => c.CollectedAmount);
        if (request.ServiceFeesDeducted > totalCod)
            throw new ConflictException("Service fees deducted cannot exceed total COD collected amount.");

        var netAmount = totalCod - request.ServiceFeesDeducted;
        var remittanceNumber = await _numberSequence.GetNextNumberAsync("REMITTANCE", cancellationToken);

        var remittance = new CODRemittance
        {
            RemittanceNumber = remittanceNumber,
            CustomerId = customer.Id,
            RemittanceDate = DateTime.UtcNow,
            TotalCodAmount = totalCod,
            TotalServiceFeesDeducted = request.ServiceFeesDeducted,
            NetRemittanceAmount = netAmount,
            CurrencyId = currency.Id,
            PaymentMethod = request.PaymentMethod,
            PaymentReference = request.PaymentReference,
            Status = "Draft",
            BankName = request.BankName,
            BankAccount = request.BankAccount,
            AccountHolderName = request.AccountHolderName,
            RemittedByUserId = _currentUser.UserId ?? 1,
            Notes = request.Notes
        };

        // Distribute fees proportionally
        var totalFees = request.ServiceFeesDeducted;
        foreach (var col in collections)
        {
            var feeShare = totalCod > 0 ? Math.Round(col.CollectedAmount / totalCod * totalFees, 2) : 0m;
            remittance.RemittanceItems.Add(new CODRemittanceItem
            {
                CODCollectionId = col.Id,
                ShipmentId = col.ShipmentId,
                CodAmount = col.CollectedAmount,
                DeductedShippingFee = feeShare,
                NetAmount = col.CollectedAmount - feeShare
            });
        }

        await _context.CODRemittances.AddAsync(remittance, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync("CreateCodRemittance", "finance", "CODRemittances", remittance.Id.ToString(),
            null, new { remittance.RemittanceNumber, remittance.CustomerId, remittance.TotalCodAmount, remittance.NetRemittanceAmount },
            notes: $"Created draft COD remittance with {collections.Count} items", cancellationToken: cancellationToken);

        return (await GetRemittanceByIdAsync(remittance.Id, cancellationToken))!;
    }

    public async Task<CodRemittanceDto> AddItemAsync(long remittanceId, AddCodRemittanceItemRequest request, CancellationToken cancellationToken = default)
    {
        var remittance = await _context.CODRemittances
            .Include(r => r.Customer)
            .Include(r => r.RemittanceItems)
            .FirstOrDefaultAsync(r => r.Id == remittanceId, cancellationToken)
            ?? throw new NotFoundException("CODRemittance", remittanceId);

        if (remittance.Status != "Draft")
            throw new ConflictException("Cannot add items to a remittance that is not in Draft status.");

        if (!_currentUser.HasBranchAccess(remittance.Customer.BranchId) && !_currentUser.IsInRole("SUPER_ADMIN"))
            throw new ForbiddenAccessException();

        var collection = await _context.CODCollections
            .Include(c => c.Shipment)
            .Include(c => c.RemittanceItem)
            .FirstOrDefaultAsync(c => c.Id == request.CODCollectionId, cancellationToken)
            ?? throw new NotFoundException("CODCollection", request.CODCollectionId);

        if (collection.Shipment.CustomerId != remittance.CustomerId)
            throw new ConflictException("Collection belongs to a different customer.");

        if (collection.RemittanceItem != null || collection.Status == "RemittedToCustomer")
            throw new ConflictException("Collection is already part of a remittance.");

        var netItem = collection.CollectedAmount - request.DeductedShippingFee;
        var item = new CODRemittanceItem
        {
            CODRemittanceId = remittance.Id,
            CODCollectionId = collection.Id,
            ShipmentId = collection.ShipmentId,
            CodAmount = collection.CollectedAmount,
            DeductedShippingFee = request.DeductedShippingFee,
            NetAmount = netItem
        };

        remittance.RemittanceItems.Add(item);
        remittance.TotalCodAmount += collection.CollectedAmount;
        remittance.TotalServiceFeesDeducted += request.DeductedShippingFee;
        remittance.NetRemittanceAmount += netItem;

        await _context.SaveChangesAsync(cancellationToken);

        return (await GetRemittanceByIdAsync(remittance.Id, cancellationToken))!;
    }

    public async Task<CodRemittanceDto> RemoveItemAsync(long remittanceId, long itemId, CancellationToken cancellationToken = default)
    {
        var remittance = await _context.CODRemittances
            .Include(r => r.Customer)
            .Include(r => r.RemittanceItems).ThenInclude(ri => ri.CODCollection)
            .FirstOrDefaultAsync(r => r.Id == remittanceId, cancellationToken)
            ?? throw new NotFoundException("CODRemittance", remittanceId);

        if (remittance.Status != "Draft")
            throw new ConflictException("Cannot remove items from a remittance that is not in Draft status.");

        if (!_currentUser.HasBranchAccess(remittance.Customer.BranchId) && !_currentUser.IsInRole("SUPER_ADMIN"))
            throw new ForbiddenAccessException();

        var item = remittance.RemittanceItems.FirstOrDefault(ri => ri.Id == itemId)
            ?? throw new NotFoundException("CODRemittanceItem", itemId);

        remittance.TotalCodAmount = Math.Max(0m, remittance.TotalCodAmount - item.CodAmount);
        remittance.TotalServiceFeesDeducted = Math.Max(0m, remittance.TotalServiceFeesDeducted - item.DeductedShippingFee);
        remittance.NetRemittanceAmount = Math.Max(0m, remittance.NetRemittanceAmount - item.NetAmount);

        if (item.CODCollection != null)
        {
            item.CODCollection.Status = "VerifiedByCashier";
        }

        _context.CODRemittanceItems.Remove(item);
        await _context.SaveChangesAsync(cancellationToken);

        return (await GetRemittanceByIdAsync(remittance.Id, cancellationToken))!;
    }

    public async Task<CodRemittanceDto> ConfirmRemittanceAsync(long id, ConfirmCodRemittanceRequest request, CancellationToken cancellationToken = default)
    {
        var remittance = await _context.CODRemittances
            .Include(r => r.Customer)
            .Include(r => r.RemittanceItems).ThenInclude(ri => ri.CODCollection)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new NotFoundException("CODRemittance", id);

        if (!_currentUser.HasBranchAccess(remittance.Customer.BranchId) && !_currentUser.IsInRole("SUPER_ADMIN"))
            throw new ForbiddenAccessException();

        if (remittance.Status == "Completed")
            throw new ConflictException("Remittance is already completed.");

        if (remittance.Status == "Cancelled")
            throw new ConflictException("Cannot confirm a cancelled remittance.");

        if (!remittance.RemittanceItems.Any())
            throw new ConflictException("Cannot confirm a remittance with no items.");

        remittance.Status = "Completed";
        remittance.RemittedAt = DateTime.UtcNow;
        remittance.RemittedByUserId = _currentUser.UserId ?? remittance.RemittedByUserId;

        if (!string.IsNullOrWhiteSpace(request.PaymentReference))
            remittance.PaymentReference = request.PaymentReference;

        if (request.ActualRemittedAmount.HasValue)
            remittance.NetRemittanceAmount = request.ActualRemittedAmount.Value;

        if (!string.IsNullOrWhiteSpace(request.Notes))
            remittance.Notes = string.IsNullOrWhiteSpace(remittance.Notes) ? request.Notes : $"{remittance.Notes}; {request.Notes}";

        // Mark all constituent collections as RemittedToCustomer
        foreach (var item in remittance.RemittanceItems)
        {
            if (item.CODCollection != null)
            {
                item.CODCollection.Status = "RemittedToCustomer";
            }
        }

        // Reduce CustomerBalance.TotalCodHoldingAmount
        var balance = await _context.CustomerBalances
            .FirstOrDefaultAsync(b => b.CustomerId == remittance.CustomerId, cancellationToken);

        if (balance != null)
        {
            balance.TotalCodHoldingAmount = Math.Max(0m, balance.TotalCodHoldingAmount - remittance.TotalCodAmount);
            balance.LastUpdatedDate = DateTime.UtcNow;
        }

        _outbox?.Enqueue(
            new CodRemittedIntegrationEvent(remittance.Id, remittance.RemittanceNumber, remittance.CustomerId,
                remittance.Customer.BranchId, remittance.NetRemittanceAmount),
            "CODRemittance", remittance.Id.ToString());

        await _context.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync("ConfirmCodRemittance", "finance", "CODRemittances", remittance.Id.ToString(),
            new { Status = "Draft" }, new { Status = "Completed", remittance.NetRemittanceAmount, remittance.RemittedAt },
            notes: $"Confirmed COD remittance {remittance.RemittanceNumber} to customer {remittance.Customer.Name}",
            cancellationToken: cancellationToken);

        return (await GetRemittanceByIdAsync(remittance.Id, cancellationToken))!;
    }

    public async Task<CodRemittanceDto> CancelRemittanceAsync(long id, string reason, CancellationToken cancellationToken = default)
    {
        var remittance = await _context.CODRemittances
            .Include(r => r.Customer)
            .Include(r => r.RemittanceItems).ThenInclude(ri => ri.CODCollection)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new NotFoundException("CODRemittance", id);

        if (!_currentUser.HasBranchAccess(remittance.Customer.BranchId) && !_currentUser.IsInRole("SUPER_ADMIN"))
            throw new ForbiddenAccessException();

        if (remittance.Status == "Completed")
            throw new ConflictException("Cannot cancel a completed remittance. Reversal requires supervisory accounting adjustment.");

        var oldStatus = remittance.Status;
        remittance.Status = "Cancelled";
        remittance.Notes = string.IsNullOrWhiteSpace(remittance.Notes) ? $"Cancelled: {reason}" : $"{remittance.Notes}; Cancelled: {reason}";

        // Restore collections and remove remittance items
        foreach (var item in remittance.RemittanceItems)
        {
            if (item.CODCollection != null)
            {
                item.CODCollection.Status = "VerifiedByCashier";
            }
        }

        _context.CODRemittanceItems.RemoveRange(remittance.RemittanceItems);
        await _context.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync("CancelCodRemittance", "finance", "CODRemittances", remittance.Id.ToString(),
            new { Status = oldStatus }, new { Status = "Cancelled" },
            notes: reason, cancellationToken: cancellationToken);

        return (await GetRemittanceByIdAsync(remittance.Id, cancellationToken))!;
    }

    private static CodRemittanceDto MapToDto(CODRemittance r) => new()
    {
        Id = r.Id,
        PublicId = r.PublicId,
        RemittanceNumber = r.RemittanceNumber,
        CustomerId = r.CustomerId,
        CustomerCode = r.Customer.CustomerCode,
        CustomerName = r.Customer.Name,
        RemittanceDate = r.RemittanceDate,
        TotalCodAmount = r.TotalCodAmount,
        TotalServiceFeesDeducted = r.TotalServiceFeesDeducted,
        NetRemittanceAmount = r.NetRemittanceAmount,
        CurrencyId = r.CurrencyId,
        CurrencyCode = r.Currency.Code,
        PaymentMethod = r.PaymentMethod,
        PaymentReference = r.PaymentReference,
        Status = r.Status,
        BankName = r.BankName,
        BankAccount = r.BankAccount,
        AccountHolderName = r.AccountHolderName,
        RemittedByUserId = r.RemittedByUserId,
        RemittedByUsername = r.RemittedByUser?.Username,
        RemittedAt = r.RemittedAt,
        Notes = r.Notes,
        ItemsCount = r.RemittanceItems.Count,
        Items = r.RemittanceItems.Select(ri => new CodRemittanceItemDto
        {
            Id = ri.Id,
            CODRemittanceId = ri.CODRemittanceId,
            CODCollectionId = ri.CODCollectionId,
            ShipmentId = ri.ShipmentId,
            ShipmentTrackingNumber = ri.Shipment.TrackingNumber,
            CodAmount = ri.CodAmount,
            DeductedShippingFee = ri.DeductedShippingFee,
            NetAmount = ri.NetAmount
        }).ToList()
    };
}

// ═══════════════════════════════════════════════════════════════
//  4. COD RECONCILIATION SERVICE
// ═══════════════════════════════════════════════════════════════

public class CodReconciliationService : ICodReconciliationService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CodReconciliationService(ApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<CodReconciliationSummaryDto> GetSummaryAsync(long? branchId, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken = default)
    {
        var colQuery = _context.CODCollections
            .Include(c => c.Shipment)
            .AsNoTracking();

        if (branchId.HasValue)
        {
            if (!_currentUser.HasBranchAccess(branchId.Value))
                throw new ForbiddenAccessException();
            colQuery = colQuery.Where(c => c.Shipment.DestinationBranchId == branchId.Value);
        }
        else if (!_currentUser.IsInRole("SUPER_ADMIN"))
        {
            var accessible = _currentUser.AccessibleBranchIds;
            colQuery = colQuery.Where(c => accessible.Contains(c.Shipment.DestinationBranchId));
        }

        if (fromDate.HasValue)
        {
            var dtFrom = fromDate.Value.ToDateTime(TimeOnly.MinValue);
            colQuery = colQuery.Where(c => c.CollectionDate >= dtFrom);
        }

        if (toDate.HasValue)
        {
            var dtTo = toDate.Value.ToDateTime(TimeOnly.MaxValue);
            colQuery = colQuery.Where(c => c.CollectionDate <= dtTo);
        }

        var collections = await colQuery.ToListAsync(cancellationToken);

        var remQuery = _context.CODRemittances
            .Include(r => r.Customer)
            .AsNoTracking();

        if (branchId.HasValue)
            remQuery = remQuery.Where(r => r.Customer.BranchId == branchId.Value);

        var remittances = await remQuery.ToListAsync(cancellationToken);

        var totalExpected = collections.Sum(c => c.Shipment.CodAmount);
        var totalCollected = collections.Sum(c => c.CollectedAmount);
        var totalDeposited = collections.Where(c => c.Status == "DepositedToBranch" || c.Status == "VerifiedByCashier" || c.Status == "RemittedToCustomer").Sum(c => c.CollectedAmount);
        var totalVerified = collections.Where(c => c.Status == "VerifiedByCashier" || c.Status == "RemittedToCustomer").Sum(c => c.CollectedAmount);
        var totalRemitted = remittances.Where(r => r.Status == "Completed").Sum(r => r.NetRemittanceAmount);

        return new CodReconciliationSummaryDto
        {
            TotalShipmentCodExpected = totalExpected,
            TotalCollectedByDrivers = totalCollected,
            TotalDepositedToBranches = totalDeposited,
            TotalVerifiedByCashiers = totalVerified,
            TotalRemittedToCustomers = totalRemitted,
            DiscrepancyAmount = Math.Abs(totalExpected - totalCollected),
            TotalCollectionsCount = collections.Count,
            TotalRemittancesCount = remittances.Count
        };
    }

    public async Task<EmployeeCodReconciliationDto> ReconcileEmployeeAsync(long employeeId, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == employeeId, cancellationToken)
            ?? throw new NotFoundException("Employee", employeeId);

        if (!_currentUser.HasBranchAccess(employee.BranchId) && !_currentUser.IsInRole("SUPER_ADMIN"))
            throw new ForbiddenAccessException();

        var query = _context.CODCollections
            .Include(c => c.Shipment)
            .Where(c => c.CollectedByEmployeeId == employeeId)
            .AsNoTracking();

        if (fromDate.HasValue)
        {
            var dtFrom = fromDate.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(c => c.CollectionDate >= dtFrom);
        }

        if (toDate.HasValue)
        {
            var dtTo = toDate.Value.ToDateTime(TimeOnly.MaxValue);
            query = query.Where(c => c.CollectionDate <= dtTo);
        }

        var collections = await query.ToListAsync(cancellationToken);

        var totalExpected = collections.Sum(c => c.Shipment.CodAmount);
        var totalCollected = collections.Sum(c => c.CollectedAmount);
        var totalDeposited = collections.Where(c => c.Status == "DepositedToBranch" || c.Status == "VerifiedByCashier" || c.Status == "RemittedToCustomer").Sum(c => c.CollectedAmount);

        return new EmployeeCodReconciliationDto
        {
            EmployeeId = employee.Id,
            EmployeeName = employee.FullName,
            BranchId = employee.BranchId,
            TotalExpected = totalExpected,
            TotalCollected = totalCollected,
            TotalDeposited = totalDeposited,
            TotalAssignments = collections.Count,
            TotalCompletedCollections = collections.Count(c => c.Status != "CollectedByDriver" || c.CollectedAmount > 0)
        };
    }

    public async Task<BranchCodReconciliationDto> ReconcileBranchAsync(long branchId, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken = default)
    {
        var branch = await _context.Branches
            .FirstOrDefaultAsync(b => b.Id == branchId, cancellationToken)
            ?? throw new NotFoundException("Branch", branchId);

        if (!_currentUser.HasBranchAccess(branchId) && !_currentUser.IsInRole("SUPER_ADMIN"))
            throw new ForbiddenAccessException();

        var colQuery = _context.CODCollections
            .Include(c => c.Shipment)
            .Where(c => c.Shipment.DestinationBranchId == branchId)
            .AsNoTracking();

        if (fromDate.HasValue)
        {
            var dtFrom = fromDate.Value.ToDateTime(TimeOnly.MinValue);
            colQuery = colQuery.Where(c => c.CollectionDate >= dtFrom);
        }

        if (toDate.HasValue)
        {
            var dtTo = toDate.Value.ToDateTime(TimeOnly.MaxValue);
            colQuery = colQuery.Where(c => c.CollectionDate <= dtTo);
        }

        var collections = await colQuery.ToListAsync(cancellationToken);

        var remQuery = _context.CODRemittances
            .Include(r => r.Customer)
            .Where(r => r.Customer.BranchId == branchId)
            .AsNoTracking();

        var remittances = await remQuery.ToListAsync(cancellationToken);

        var totalCollected = collections.Sum(c => c.CollectedAmount);
        var totalVerified = collections.Where(c => c.Status == "VerifiedByCashier" || c.Status == "RemittedToCustomer").Sum(c => c.CollectedAmount);
        var totalRemitted = remittances.Where(r => r.Status == "Completed").Sum(r => r.NetRemittanceAmount);

        return new BranchCodReconciliationDto
        {
            BranchId = branch.Id,
            BranchCode = branch.Code,
            BranchName = branch.Name,
            TotalCollectedAtBranch = totalCollected,
            TotalVerifiedAtBranch = totalVerified,
            TotalRemittedFromBranch = totalRemitted,
            CollectionsCount = collections.Count,
            RemittancesCount = remittances.Count
        };
    }

    public async Task<RemittanceReconciliationDto> ReconcileRemittanceAsync(long remittanceId, CancellationToken cancellationToken = default)
    {
        var remittance = await _context.CODRemittances
            .Include(r => r.Customer)
            .Include(r => r.RemittanceItems)
            .FirstOrDefaultAsync(r => r.Id == remittanceId, cancellationToken)
            ?? throw new NotFoundException("CODRemittance", remittanceId);

        if (!_currentUser.HasBranchAccess(remittance.Customer.BranchId) && !_currentUser.IsInRole("SUPER_ADMIN"))
            throw new ForbiddenAccessException();

        var expectedCodTotal = remittance.RemittanceItems.Sum(ri => ri.CodAmount);
        var deductedFees = remittance.RemittanceItems.Sum(ri => ri.DeductedShippingFee);
        var calculatedNet = expectedCodTotal - deductedFees;

        return new RemittanceReconciliationDto
        {
            RemittanceId = remittance.Id,
            RemittanceNumber = remittance.RemittanceNumber,
            CustomerId = remittance.CustomerId,
            CustomerName = remittance.Customer.Name,
            ExpectedCodTotal = expectedCodTotal,
            DeductedFeesTotal = deductedFees,
            CalculatedNetAmount = calculatedNet,
            ActualRemittedAmount = remittance.NetRemittanceAmount,
            Status = remittance.Status
        };
    }
}
