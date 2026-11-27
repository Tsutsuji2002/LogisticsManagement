using Logistics.Server.Application.Common.Events;
using Logistics.Server.Application.Common.Exceptions;
using Logistics.Server.Application.Common.Interfaces;
using Logistics.Server.Application.Common.Models;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Shipping.DTOs;
using Logistics.Server.Application.Modules.Shipping.Services;
using Logistics.Server.Application.Modules.Warehouse.DTOs;
using Logistics.Server.Application.Modules.Warehouse.Services;
using Logistics.Server.Domain.Entities.Inventory;
using Logistics.Server.Domain.Entities.Warehouse;
using Logistics.Server.Infrastructure.Persistence;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Logistics.Server.Infrastructure.Services;

// ═══════════════════════════════════════════════════════════════
//  1. WAREHOUSE MANAGEMENT SERVICE
// ═══════════════════════════════════════════════════════════════

public class WarehouseManagementService : IWarehouseManagementService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;
    private readonly ILogger<WarehouseManagementService> _logger;

    public WarehouseManagementService(
        ApplicationDbContext context,
        ICurrentUserService currentUser,
        IAuditService audit,
        ILogger<WarehouseManagementService> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _audit = audit;
        _logger = logger;
    }

    private void EnsureBranchAccess(long branchId)
    {
        if (_currentUser.IsInRole(AppRoles.SuperAdmin)) return;
        if (!_currentUser.HasBranchAccess(branchId))
            throw new ForbiddenAccessException($"User does not have access to branch {branchId}.");
    }

    public async Task<PagedResult<WarehouseDto>> GetWarehousesAsync(string? search, long? branchId, string? type, bool? isActive, PagedRequest request, CancellationToken cancellationToken = default)
    {
        var query = _context.Warehouses
            .Include(w => w.Branch)
            .Include(w => w.ManagerEmployee)
            .Include(w => w.Zones)
                .ThenInclude(z => z.Locations)
            .AsNoTracking()
            .AsQueryable();

        if (!_currentUser.IsInRole(AppRoles.SuperAdmin))
        {
            var allowedBranches = _currentUser.AccessibleBranchIds;
            query = query.Where(w => allowedBranches.Contains(w.BranchId));
        }

        if (branchId.HasValue) query = query.Where(w => w.BranchId == branchId.Value);
        if (!string.IsNullOrWhiteSpace(type)) query = query.Where(w => w.Type == type);
        if (isActive.HasValue) query = query.Where(w => w.IsActive == isActive.Value);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(w => w.Code.Contains(search) || w.Name.Contains(search));

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(w => w.Code)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(w => MapToWarehouseDto(w))
            .ToListAsync(cancellationToken);

        return new PagedResult<WarehouseDto>(items, totalCount, request.Page, request.PageSize);
    }

    public async Task<WarehouseDetailDto?> GetWarehouseByIdOrCodeAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var warehouse = await FindWarehouseAsync(identifier, cancellationToken, includeZones: true);
        if (warehouse == null) return null;
        EnsureBranchAccess(warehouse.BranchId);

        return MapToWarehouseDetailDto(warehouse);
    }

    public async Task<WarehouseDetailDto> CreateWarehouseAsync(CreateWarehouseRequest request, CancellationToken cancellationToken = default)
    {
        EnsureBranchAccess(request.BranchId);

        var exists = await _context.Warehouses.AnyAsync(w => w.Code == request.Code, cancellationToken);
        if (exists)
            throw new ConflictException("Warehouse", "Code", request.Code);

        var branchExists = await _context.Branches.AnyAsync(b => b.Id == request.BranchId, cancellationToken);
        if (!branchExists)
            throw new NotFoundException("Branch", request.BranchId);

        var warehouse = new Domain.Entities.Organization.Warehouse
        {
            BranchId = request.BranchId,
            Code = request.Code.ToUpperInvariant(),
            Name = request.Name,
            Type = request.Type,
            TotalAreaSqM = request.TotalAreaSqM,
            AddressId = request.AddressId,
            ManagerEmployeeId = request.ManagerEmployeeId,
            IsActive = true,
            CreatedBy = _currentUser.Username
        };

        _context.Warehouses.Add(warehouse);
        await _context.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync("Create", "organization", "Warehouses", warehouse.Id.ToString(),
            null, new { warehouse.Code, warehouse.Name, warehouse.BranchId },
            notes: null, cancellationToken: cancellationToken);

        _logger.LogInformation("Warehouse '{Code}' created with Id {Id}.", warehouse.Code, warehouse.Id);

        // Re-query to load branch details
        return (await GetWarehouseByIdOrCodeAsync(warehouse.Id.ToString(), cancellationToken))!;
    }

    public async Task<WarehouseDetailDto> UpdateWarehouseAsync(string identifier, UpdateWarehouseRequest request, CancellationToken cancellationToken = default)
    {
        var warehouse = await FindWarehouseAsync(identifier, cancellationToken)
            ?? throw new NotFoundException("Warehouse", identifier);
        EnsureBranchAccess(warehouse.BranchId);

        var oldValues = new { warehouse.Name, warehouse.Type, warehouse.TotalAreaSqM };

        warehouse.Name = request.Name;
        warehouse.Type = request.Type;
        warehouse.TotalAreaSqM = request.TotalAreaSqM;
        warehouse.AddressId = request.AddressId;
        warehouse.ManagerEmployeeId = request.ManagerEmployeeId;
        warehouse.UpdatedAt = DateTime.UtcNow;
        warehouse.UpdatedBy = _currentUser.Username;

        await _context.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync("Update", "organization", "Warehouses", warehouse.Id.ToString(),
            oldValues, new { warehouse.Name, warehouse.Type, warehouse.TotalAreaSqM },
            notes: null, cancellationToken: cancellationToken);

        return (await GetWarehouseByIdOrCodeAsync(warehouse.Id.ToString(), cancellationToken))!;
    }

    public async Task<WarehouseDetailDto> ToggleWarehouseStatusAsync(string identifier, bool isActive, CancellationToken cancellationToken = default)
    {
        var warehouse = await FindWarehouseAsync(identifier, cancellationToken)
            ?? throw new NotFoundException("Warehouse", identifier);
        EnsureBranchAccess(warehouse.BranchId);

        warehouse.IsActive = isActive;
        warehouse.UpdatedAt = DateTime.UtcNow;
        warehouse.UpdatedBy = _currentUser.Username;

        await _context.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync(isActive ? "Activate" : "Deactivate", "organization", "Warehouses",
            warehouse.Id.ToString(), null, new { warehouse.IsActive }, notes: null, cancellationToken: cancellationToken);

        return (await GetWarehouseByIdOrCodeAsync(warehouse.Id.ToString(), cancellationToken))!;
    }

    public async Task<IReadOnlyList<WarehouseZoneDto>> GetZonesAsync(string warehouseIdentifier, CancellationToken cancellationToken = default)
    {
        var warehouse = await FindWarehouseAsync(warehouseIdentifier, cancellationToken)
            ?? throw new NotFoundException("Warehouse", warehouseIdentifier);
        EnsureBranchAccess(warehouse.BranchId);

        var zones = await _context.WarehouseZones
            .Include(z => z.Locations)
            .AsNoTracking()
            .Where(z => z.WarehouseId == warehouse.Id)
            .OrderBy(z => z.Code)
            .ToListAsync(cancellationToken);

        return zones.Select(z => MapToZoneDto(z, warehouse.Code)).ToList();
    }

    public async Task<WarehouseZoneDto?> GetZoneByIdAsync(string warehouseIdentifier, long zoneId, CancellationToken cancellationToken = default)
    {
        var warehouse = await FindWarehouseAsync(warehouseIdentifier, cancellationToken)
            ?? throw new NotFoundException("Warehouse", warehouseIdentifier);
        EnsureBranchAccess(warehouse.BranchId);

        var zone = await _context.WarehouseZones
            .Include(z => z.Locations)
            .AsNoTracking()
            .FirstOrDefaultAsync(z => z.WarehouseId == warehouse.Id && z.Id == zoneId, cancellationToken);

        return zone == null ? null : MapToZoneDto(zone, warehouse.Code);
    }

    public async Task<WarehouseZoneDto> CreateZoneAsync(string warehouseIdentifier, CreateWarehouseZoneRequest request, CancellationToken cancellationToken = default)
    {
        var warehouse = await FindWarehouseAsync(warehouseIdentifier, cancellationToken)
            ?? throw new NotFoundException("Warehouse", warehouseIdentifier);
        EnsureBranchAccess(warehouse.BranchId);

        if (!warehouse.IsActive)
            throw new ConflictException("Cannot add operational zones to an inactive warehouse.");

        var exists = await _context.WarehouseZones.AnyAsync(
            z => z.WarehouseId == warehouse.Id && z.Code == request.Code, cancellationToken);
        if (exists)
            throw new ConflictException("WarehouseZone", "Code", request.Code);

        var zone = new WarehouseZone
        {
            WarehouseId = warehouse.Id,
            Code = request.Code.ToUpperInvariant(),
            Name = request.Name,
            ZoneType = request.ZoneType,
            TemperatureType = request.TemperatureType,
            IsActive = true,
            CreatedBy = _currentUser.Username
        };

        _context.WarehouseZones.Add(zone);
        await _context.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync("CreateZone", "warehouse", "WarehouseZones", zone.Id.ToString(),
            null, new { zone.Code, zone.Name, zone.ZoneType }, notes: null, cancellationToken: cancellationToken);

        return MapToZoneDto(zone, warehouse.Code);
    }

    public async Task<WarehouseZoneDto> UpdateZoneAsync(string warehouseIdentifier, long zoneId, UpdateWarehouseZoneRequest request, CancellationToken cancellationToken = default)
    {
        var warehouse = await FindWarehouseAsync(warehouseIdentifier, cancellationToken)
            ?? throw new NotFoundException("Warehouse", warehouseIdentifier);
        EnsureBranchAccess(warehouse.BranchId);

        var zone = await _context.WarehouseZones
            .Include(z => z.Locations)
            .FirstOrDefaultAsync(z => z.WarehouseId == warehouse.Id && z.Id == zoneId, cancellationToken)
            ?? throw new NotFoundException("WarehouseZone", zoneId);

        zone.Name = request.Name;
        zone.ZoneType = request.ZoneType;
        zone.TemperatureType = request.TemperatureType;
        zone.UpdatedAt = DateTime.UtcNow;
        zone.UpdatedBy = _currentUser.Username;

        await _context.SaveChangesAsync(cancellationToken);
        return MapToZoneDto(zone, warehouse.Code);
    }

    public async Task<WarehouseZoneDto> ToggleZoneStatusAsync(string warehouseIdentifier, long zoneId, bool isActive, CancellationToken cancellationToken = default)
    {
        var warehouse = await FindWarehouseAsync(warehouseIdentifier, cancellationToken)
            ?? throw new NotFoundException("Warehouse", warehouseIdentifier);
        EnsureBranchAccess(warehouse.BranchId);

        var zone = await _context.WarehouseZones
            .Include(z => z.Locations)
            .FirstOrDefaultAsync(z => z.WarehouseId == warehouse.Id && z.Id == zoneId, cancellationToken)
            ?? throw new NotFoundException("WarehouseZone", zoneId);

        zone.IsActive = isActive;
        zone.UpdatedAt = DateTime.UtcNow;
        zone.UpdatedBy = _currentUser.Username;

        await _context.SaveChangesAsync(cancellationToken);
        return MapToZoneDto(zone, warehouse.Code);
    }

    public async Task<PagedResult<WarehouseLocationDto>> GetLocationsAsync(string warehouseIdentifier, long? zoneId, PagedRequest request, CancellationToken cancellationToken = default)
    {
        var warehouse = await FindWarehouseAsync(warehouseIdentifier, cancellationToken)
            ?? throw new NotFoundException("Warehouse", warehouseIdentifier);
        EnsureBranchAccess(warehouse.BranchId);

        var query = _context.WarehouseLocations
            .Include(l => l.WarehouseZone)
            .AsNoTracking()
            .Where(l => l.WarehouseZone.WarehouseId == warehouse.Id)
            .AsQueryable();

        if (zoneId.HasValue) query = query.Where(l => l.WarehouseZoneId == zoneId.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(l => l.Code)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(l => MapToLocationDto(l))
            .ToListAsync(cancellationToken);

        return new PagedResult<WarehouseLocationDto>(items, totalCount, request.Page, request.PageSize);
    }

    public async Task<WarehouseLocationDto?> GetLocationByIdAsync(string warehouseIdentifier, long locationId, CancellationToken cancellationToken = default)
    {
        var warehouse = await FindWarehouseAsync(warehouseIdentifier, cancellationToken)
            ?? throw new NotFoundException("Warehouse", warehouseIdentifier);
        EnsureBranchAccess(warehouse.BranchId);

        var location = await _context.WarehouseLocations
            .Include(l => l.WarehouseZone)
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.WarehouseZone.WarehouseId == warehouse.Id && l.Id == locationId, cancellationToken);

        return location == null ? null : MapToLocationDto(location);
    }

    public async Task<WarehouseLocationDto> CreateLocationAsync(string warehouseIdentifier, long zoneId, CreateWarehouseLocationRequest request, CancellationToken cancellationToken = default)
    {
        var warehouse = await FindWarehouseAsync(warehouseIdentifier, cancellationToken)
            ?? throw new NotFoundException("Warehouse", warehouseIdentifier);
        EnsureBranchAccess(warehouse.BranchId);

        var zone = await _context.WarehouseZones
            .FirstOrDefaultAsync(z => z.WarehouseId == warehouse.Id && z.Id == zoneId, cancellationToken)
            ?? throw new NotFoundException("WarehouseZone", zoneId);

        if (!zone.IsActive)
            throw new ConflictException("Cannot add locations to an inactive zone.");

        var exists = await _context.WarehouseLocations.AnyAsync(
            l => l.WarehouseZoneId == zoneId && l.Code == request.Code, cancellationToken);
        if (exists)
            throw new ConflictException("WarehouseLocation", "Code", request.Code);

        var location = new WarehouseLocation
        {
            WarehouseZoneId = zoneId,
            Code = request.Code.ToUpperInvariant(),
            Aisle = request.Aisle,
            Rack = request.Rack,
            Shelf = request.Shelf,
            Bin = request.Bin,
            Barcode = string.IsNullOrWhiteSpace(request.Barcode) ? $"{warehouse.Code}-{request.Code}" : request.Barcode,
            MaxWeightKg = request.MaxWeightKg,
            MaxVolumeCbm = request.MaxVolumeCbm,
            IsOccupied = false,
            IsBlocked = false,
            CreatedBy = _currentUser.Username
        };

        _context.WarehouseLocations.Add(location);
        await _context.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync("CreateLocation", "warehouse", "WarehouseLocations", location.Id.ToString(),
            null, new { location.Code, location.Barcode }, notes: null, cancellationToken: cancellationToken);

        location.WarehouseZone = zone;
        return MapToLocationDto(location);
    }

    public async Task<WarehouseLocationDto> UpdateLocationAsync(string warehouseIdentifier, long locationId, UpdateWarehouseLocationRequest request, CancellationToken cancellationToken = default)
    {
        var warehouse = await FindWarehouseAsync(warehouseIdentifier, cancellationToken)
            ?? throw new NotFoundException("Warehouse", warehouseIdentifier);
        EnsureBranchAccess(warehouse.BranchId);

        var location = await _context.WarehouseLocations
            .Include(l => l.WarehouseZone)
            .FirstOrDefaultAsync(l => l.WarehouseZone.WarehouseId == warehouse.Id && l.Id == locationId, cancellationToken)
            ?? throw new NotFoundException("WarehouseLocation", locationId);

        location.Aisle = request.Aisle;
        location.Rack = request.Rack;
        location.Shelf = request.Shelf;
        location.Bin = request.Bin;
        location.Barcode = request.Barcode;
        location.MaxWeightKg = request.MaxWeightKg;
        location.MaxVolumeCbm = request.MaxVolumeCbm;
        location.IsBlocked = request.IsBlocked;
        location.UpdatedAt = DateTime.UtcNow;
        location.UpdatedBy = _currentUser.Username;

        await _context.SaveChangesAsync(cancellationToken);
        return MapToLocationDto(location);
    }

    public async Task<WarehouseLocationDto> ToggleLocationBlockAsync(string warehouseIdentifier, long locationId, bool isBlocked, CancellationToken cancellationToken = default)
    {
        var warehouse = await FindWarehouseAsync(warehouseIdentifier, cancellationToken)
            ?? throw new NotFoundException("Warehouse", warehouseIdentifier);
        EnsureBranchAccess(warehouse.BranchId);

        var location = await _context.WarehouseLocations
            .Include(l => l.WarehouseZone)
            .FirstOrDefaultAsync(l => l.WarehouseZone.WarehouseId == warehouse.Id && l.Id == locationId, cancellationToken)
            ?? throw new NotFoundException("WarehouseLocation", locationId);

        location.IsBlocked = isBlocked;
        location.UpdatedAt = DateTime.UtcNow;
        location.UpdatedBy = _currentUser.Username;

        await _context.SaveChangesAsync(cancellationToken);
        return MapToLocationDto(location);
    }

    private async Task<Domain.Entities.Organization.Warehouse?> FindWarehouseAsync(string identifier, CancellationToken ct, bool includeZones = false)
    {
        var query = _context.Warehouses
            .Include(w => w.Branch)
            .Include(w => w.ManagerEmployee)
            .AsQueryable();

        if (includeZones)
        {
            query = query.Include(w => w.Zones).ThenInclude(z => z.Locations);
        }

        if (long.TryParse(identifier, out var id))
            return await query.FirstOrDefaultAsync(w => w.Id == id, ct);
        if (Guid.TryParse(identifier, out var pid))
            return await query.FirstOrDefaultAsync(w => w.PublicId == pid, ct);
        return await query.FirstOrDefaultAsync(w => w.Code == identifier.ToUpperInvariant(), ct);
    }

    private static WarehouseDto MapToWarehouseDto(Domain.Entities.Organization.Warehouse w) => new()
    {
        Id = w.Id,
        PublicId = w.PublicId,
        BranchId = w.BranchId,
        BranchCode = w.Branch?.Code ?? string.Empty,
        BranchName = w.Branch?.Name ?? string.Empty,
        Code = w.Code,
        Name = w.Name,
        Type = w.Type,
        TotalAreaSqM = w.TotalAreaSqM,
        AddressId = w.AddressId,
        ManagerEmployeeId = w.ManagerEmployeeId,
        ManagerEmployeeName = w.ManagerEmployee?.FullName,
        IsActive = w.IsActive,
        CreatedAt = w.CreatedAt,
        ZoneCount = w.Zones?.Count ?? 0,
        LocationCount = w.Zones?.Sum(z => z.Locations?.Count ?? 0) ?? 0
    };

    private static WarehouseDetailDto MapToWarehouseDetailDto(Domain.Entities.Organization.Warehouse w) => new()
    {
        Id = w.Id,
        PublicId = w.PublicId,
        BranchId = w.BranchId,
        BranchCode = w.Branch?.Code ?? string.Empty,
        BranchName = w.Branch?.Name ?? string.Empty,
        Code = w.Code,
        Name = w.Name,
        Type = w.Type,
        TotalAreaSqM = w.TotalAreaSqM,
        AddressId = w.AddressId,
        ManagerEmployeeId = w.ManagerEmployeeId,
        ManagerEmployeeName = w.ManagerEmployee?.FullName,
        IsActive = w.IsActive,
        CreatedAt = w.CreatedAt,
        ZoneCount = w.Zones?.Count ?? 0,
        LocationCount = w.Zones?.Sum(z => z.Locations?.Count ?? 0) ?? 0,
        Zones = w.Zones?.Select(z => MapToZoneDto(z, w.Code)).ToList() ?? []
    };

    private static WarehouseZoneDto MapToZoneDto(WarehouseZone z, string warehouseCode) => new()
    {
        Id = z.Id,
        PublicId = z.PublicId,
        WarehouseId = z.WarehouseId,
        WarehouseCode = warehouseCode,
        Code = z.Code,
        Name = z.Name,
        ZoneType = z.ZoneType,
        TemperatureType = z.TemperatureType,
        IsActive = z.IsActive,
        LocationCount = z.Locations?.Count ?? 0
    };

    private static WarehouseLocationDto MapToLocationDto(WarehouseLocation l) => new()
    {
        Id = l.Id,
        PublicId = l.PublicId,
        WarehouseZoneId = l.WarehouseZoneId,
        ZoneCode = l.WarehouseZone?.Code ?? string.Empty,
        Code = l.Code,
        Aisle = l.Aisle,
        Rack = l.Rack,
        Shelf = l.Shelf,
        Bin = l.Bin,
        Barcode = l.Barcode,
        MaxWeightKg = l.MaxWeightKg,
        MaxVolumeCbm = l.MaxVolumeCbm,
        IsOccupied = l.IsOccupied,
        IsBlocked = l.IsBlocked
    };
}

// ═══════════════════════════════════════════════════════════════
//  2. PHYSICAL INVENTORY SERVICE
// ═══════════════════════════════════════════════════════════════

public class InventoryService : IInventoryService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly INumberSequenceService _numberSequence;
    private readonly IAuditService _audit;
    private readonly ILogger<InventoryService> _logger;
    private readonly IOutboxService? _outbox;

    public InventoryService(
        ApplicationDbContext context,
        ICurrentUserService currentUser,
        INumberSequenceService numberSequence,
        IAuditService audit,
        ILogger<InventoryService> logger,
        IOutboxService? outbox = null)
    {
        _context = context;
        _currentUser = currentUser;
        _numberSequence = numberSequence;
        _audit = audit;
        _logger = logger;
        _outbox = outbox;
    }

    private void EnsureWarehouseAccess(long branchId)
    {
        if (_currentUser.IsInRole(AppRoles.SuperAdmin)) return;
        if (!_currentUser.HasBranchAccess(branchId))
            throw new ForbiddenAccessException($"User does not have access to branch {branchId}.");
    }

    // ───────────── Inventory Items ─────────────

    public async Task<PagedResult<InventoryItemDto>> GetInventoryItemsAsync(string? search, long? warehouseId, bool? isActive, PagedRequest request, CancellationToken cancellationToken = default)
    {
        var query = _context.InventoryItems
            .Include(i => i.Product)
            .Include(i => i.Warehouse)
            .AsNoTracking()
            .AsQueryable();

        if (!_currentUser.IsInRole(AppRoles.SuperAdmin))
        {
            var branches = _currentUser.AccessibleBranchIds;
            query = query.Where(i => branches.Contains(i.Warehouse.BranchId));
        }

        if (warehouseId.HasValue) query = query.Where(i => i.WarehouseId == warehouseId.Value);
        if (isActive.HasValue) query = query.Where(i => i.IsActive == isActive.Value);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(i => i.Product.Sku.Contains(search) || i.Product.Name.Contains(search));

        var totalCount = await query.CountAsync(cancellationToken);
        var dbItems = await query
            .OrderBy(i => i.Product.Sku)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);
        var items = dbItems.Select(i => MapToItemDto(i)).ToList();

        return new PagedResult<InventoryItemDto>(items, totalCount, request.Page, request.PageSize);
    }

    public async Task<InventoryItemDto?> GetInventoryItemByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var item = await _context.InventoryItems
            .Include(i => i.Product)
            .Include(i => i.Warehouse)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

        if (item == null) return null;
        EnsureWarehouseAccess(item.Warehouse.BranchId);
        return MapToItemDto(item);
    }

    public async Task<InventoryItemDto> CreateInventoryItemAsync(CreateInventoryItemRequest request, CancellationToken cancellationToken = default)
    {
        var warehouse = await _context.Warehouses.FindAsync([request.WarehouseId], cancellationToken)
            ?? throw new NotFoundException("Warehouse", request.WarehouseId);
        EnsureWarehouseAccess(warehouse.BranchId);

        var product = await _context.Products.FindAsync([request.ProductId], cancellationToken)
            ?? throw new NotFoundException("Product", request.ProductId);

        var exists = await _context.InventoryItems.AnyAsync(
            i => i.ProductId == request.ProductId && i.WarehouseId == request.WarehouseId, cancellationToken);
        if (exists)
            throw new ConflictException("InventoryItem already exists for this product and warehouse.");

        var item = new InventoryItem
        {
            ProductId = request.ProductId,
            WarehouseId = request.WarehouseId,
            ReorderPoint = request.ReorderPoint,
            SafetyStock = request.SafetyStock,
            TargetStock = request.TargetStock,
            MinimumOrderQuantity = request.MinimumOrderQuantity,
            MaximumStock = request.MaximumStock,
            IsActive = true,
            CreatedBy = _currentUser.Username
        };

        _context.InventoryItems.Add(item);
        await _context.SaveChangesAsync(cancellationToken);

        item.Product = product;
        item.Warehouse = warehouse;
        return MapToItemDto(item);
    }

    public async Task<InventoryItemDto> UpdateInventoryItemAsync(long id, UpdateInventoryItemRequest request, CancellationToken cancellationToken = default)
    {
        var item = await _context.InventoryItems
            .Include(i => i.Product)
            .Include(i => i.Warehouse)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken)
            ?? throw new NotFoundException("InventoryItem", id);
        EnsureWarehouseAccess(item.Warehouse.BranchId);

        item.ReorderPoint = request.ReorderPoint;
        item.SafetyStock = request.SafetyStock;
        item.TargetStock = request.TargetStock;
        item.MinimumOrderQuantity = request.MinimumOrderQuantity;
        item.MaximumStock = request.MaximumStock;
        item.IsActive = request.IsActive;
        item.UpdatedAt = DateTime.UtcNow;
        item.UpdatedBy = _currentUser.Username;

        await _context.SaveChangesAsync(cancellationToken);
        return MapToItemDto(item);
    }

    // ───────────── Inventory Balances ─────────────

    public async Task<PagedResult<InventoryBalanceDto>> GetBalancesAsync(long? warehouseId, long? locationId, long? productId, long? lotId, PagedRequest request, CancellationToken cancellationToken = default)
    {
        var query = _context.InventoryBalances
            .Include(b => b.Warehouse)
            .Include(b => b.WarehouseLocation)
            .Include(b => b.Product)
            .Include(b => b.Lot)
            .AsNoTracking()
            .AsQueryable();

        if (!_currentUser.IsInRole(AppRoles.SuperAdmin))
        {
            var branches = _currentUser.AccessibleBranchIds;
            query = query.Where(b => branches.Contains(b.Warehouse.BranchId));
        }

        if (warehouseId.HasValue) query = query.Where(b => b.WarehouseId == warehouseId.Value);
        if (locationId.HasValue) query = query.Where(b => b.WarehouseLocationId == locationId.Value);
        if (productId.HasValue) query = query.Where(b => b.ProductId == productId.Value);
        if (lotId.HasValue) query = query.Where(b => b.LotId == lotId.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var dbItems = await query
            .OrderBy(b => b.Product.Sku)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);
        var items = dbItems.Select(b => MapToBalanceDto(b)).ToList();

        return new PagedResult<InventoryBalanceDto>(items, totalCount, request.Page, request.PageSize);
    }

    public async Task<InventoryBalanceDto?> GetBalanceByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var balance = await _context.InventoryBalances
            .Include(b => b.Warehouse)
            .Include(b => b.WarehouseLocation)
            .Include(b => b.Product)
            .Include(b => b.Lot)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (balance == null) return null;
        EnsureWarehouseAccess(balance.Warehouse.BranchId);
        return MapToBalanceDto(balance);
    }

    // ───────────── Inventory Transactions ─────────────

    public async Task<PagedResult<InventoryTransactionDto>> GetTransactionsAsync(long? warehouseId, string? transactionType, string? referenceType, DateTime? from, DateTime? to, PagedRequest request, CancellationToken cancellationToken = default)
    {
        var query = _context.InventoryTransactions
            .Include(t => t.Warehouse)
            .Include(t => t.PerformedByEmployee)
            .Include(t => t.TransactionItems)
                .ThenInclude(ti => ti.Product)
            .Include(t => t.TransactionItems)
                .ThenInclude(ti => ti.Location)
            .Include(t => t.TransactionItems)
                .ThenInclude(ti => ti.Lot)
            .AsNoTracking()
            .AsQueryable();

        if (!_currentUser.IsInRole(AppRoles.SuperAdmin))
        {
            var branches = _currentUser.AccessibleBranchIds;
            query = query.Where(t => branches.Contains(t.Warehouse.BranchId));
        }

        if (warehouseId.HasValue) query = query.Where(t => t.WarehouseId == warehouseId.Value);
        if (!string.IsNullOrWhiteSpace(transactionType)) query = query.Where(t => t.TransactionType == transactionType);
        if (!string.IsNullOrWhiteSpace(referenceType)) query = query.Where(t => t.ReferenceType == referenceType);
        if (from.HasValue) query = query.Where(t => t.TransactionDate >= from.Value);
        if (to.HasValue) query = query.Where(t => t.TransactionDate <= to.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(t => t.TransactionDate)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => MapToTransactionDto(t))
            .ToListAsync(cancellationToken);

        return new PagedResult<InventoryTransactionDto>(items, totalCount, request.Page, request.PageSize);
    }

    public async Task<InventoryTransactionDto?> GetTransactionByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var txn = await _context.InventoryTransactions
            .Include(t => t.Warehouse)
            .Include(t => t.PerformedByEmployee)
            .Include(t => t.TransactionItems)
                .ThenInclude(ti => ti.Product)
            .Include(t => t.TransactionItems)
                .ThenInclude(ti => ti.Location)
            .Include(t => t.TransactionItems)
                .ThenInclude(ti => ti.Lot)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (txn == null) return null;
        EnsureWarehouseAccess(txn.Warehouse.BranchId);
        return MapToTransactionDto(txn);
    }

    // ───────────── Warehouse Receipts ─────────────

    public async Task<PagedResult<WarehouseReceiptDto>> GetReceiptsAsync(long? warehouseId, string? status, PagedRequest request, CancellationToken cancellationToken = default)
    {
        var query = _context.WarehouseReceipts
            .Include(r => r.Warehouse)
            .Include(r => r.ReceivedByEmployee)
            .Include(r => r.ReceiptItems)
                .ThenInclude(ri => ri.Product)
            .Include(r => r.ReceiptItems)
                .ThenInclude(ri => ri.UnitOfMeasure)
            .Include(r => r.ReceiptItems)
                .ThenInclude(ri => ri.TargetLocation)
            .AsNoTracking()
            .AsQueryable();

        if (!_currentUser.IsInRole(AppRoles.SuperAdmin))
        {
            var branches = _currentUser.AccessibleBranchIds;
            query = query.Where(r => branches.Contains(r.Warehouse.BranchId));
        }

        if (warehouseId.HasValue) query = query.Where(r => r.WarehouseId == warehouseId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(r => r.Status == status);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(r => r.ActualArrivalDate)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => MapToReceiptDto(r))
            .ToListAsync(cancellationToken);

        return new PagedResult<WarehouseReceiptDto>(items, totalCount, request.Page, request.PageSize);
    }

    public async Task<WarehouseReceiptDto?> GetReceiptByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var receipt = await _context.WarehouseReceipts
            .Include(r => r.Warehouse)
            .Include(r => r.ReceivedByEmployee)
            .Include(r => r.ReceiptItems)
                .ThenInclude(ri => ri.Product)
            .Include(r => r.ReceiptItems)
                .ThenInclude(ri => ri.UnitOfMeasure)
            .Include(r => r.ReceiptItems)
                .ThenInclude(ri => ri.TargetLocation)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (receipt == null) return null;
        EnsureWarehouseAccess(receipt.Warehouse.BranchId);
        return MapToReceiptDto(receipt);
    }

    public async Task<WarehouseReceiptDto> CreateReceiptAsync(CreateWarehouseReceiptRequest request, CancellationToken cancellationToken = default)
    {
        var warehouse = await _context.Warehouses.FindAsync([request.WarehouseId], cancellationToken)
            ?? throw new NotFoundException("Warehouse", request.WarehouseId);
        EnsureWarehouseAccess(warehouse.BranchId);

        var receiptNumber = await _numberSequence.GetNextNumberAsync("RECEIPT", cancellationToken);

        var receipt = new WarehouseReceipt
        {
            ReceiptNumber = receiptNumber,
            WarehouseId = request.WarehouseId,
            ReceiptType = request.ReceiptType,
            SourceBranchId = request.SourceBranchId,
            SourceVendor = request.SourceVendor,
            ExpectedArrivalDate = request.ExpectedArrivalDate,
            ActualArrivalDate = DateTime.UtcNow,
            Status = "Draft",
            ReceivedByEmployeeId = request.ReceivedByEmployeeId,
            Notes = request.Notes,
            CreatedBy = _currentUser.Username
        };

        foreach (var itemReq in request.Items)
        {
            receipt.ReceiptItems.Add(new WarehouseReceiptItem
            {
                ProductId = itemReq.ProductId,
                ExpectedQuantity = itemReq.ExpectedQuantity,
                ReceivedQuantity = 0,
                DamagedQuantity = 0,
                UnitOfMeasureId = itemReq.UnitOfMeasureId,
                TargetLocationId = itemReq.TargetLocationId,
                LotNumber = itemReq.LotNumber,
                ExpiryDate = itemReq.ExpiryDate,
                Notes = itemReq.Notes
            });
        }

        _context.WarehouseReceipts.Add(receipt);
        await _context.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync("CreateReceipt", "warehouse", "WarehouseReceipts", receipt.Id.ToString(),
            null, new { receipt.ReceiptNumber, receipt.WarehouseId, ItemCount = receipt.ReceiptItems.Count },
            notes: null, cancellationToken: cancellationToken);

        return (await GetReceiptByIdAsync(receipt.Id, cancellationToken))!;
    }

    public async Task<WarehouseReceiptDto> ConfirmReceiptAsync(long receiptId, ConfirmReceiptRequest request, CancellationToken cancellationToken = default)
    {
        var receipt = await _context.WarehouseReceipts
            .Include(r => r.Warehouse)
            .Include(r => r.ReceiptItems)
                .ThenInclude(ri => ri.Product)
            .FirstOrDefaultAsync(r => r.Id == receiptId, cancellationToken)
            ?? throw new NotFoundException("WarehouseReceipt", receiptId);
        EnsureWarehouseAccess(receipt.Warehouse.BranchId);

        if (receipt.Status == "Completed")
            throw new ConflictException("Receipt is already completed.");
        if (receipt.Status == "Cancelled")
            throw new ConflictException("Cannot confirm a cancelled receipt.");

        // If specific items update was provided, apply them
        if (request.Items != null && request.Items.Count > 0)
        {
            foreach (var update in request.Items)
            {
                var item = receipt.ReceiptItems.FirstOrDefault(ri => ri.Id == update.ItemId);
                if (item != null)
                {
                    item.ReceivedQuantity = update.ReceivedQuantity;
                    item.DamagedQuantity = update.DamagedQuantity;
                    item.TargetLocationId = update.TargetLocationId;
                    if (!string.IsNullOrEmpty(update.LotNumber)) item.LotNumber = update.LotNumber;
                }
            }
        }
        else
        {
            // Default: receive all expected quantities into target location
            foreach (var item in receipt.ReceiptItems)
            {
                if (item.ReceivedQuantity == 0 && item.ExpectedQuantity > 0)
                    item.ReceivedQuantity = item.ExpectedQuantity;
            }
        }

        // Post Inventory Transaction & Update Balances atomically
        var txnNumber = await _numberSequence.GetNextNumberAsync("INV-TXN", cancellationToken);
        var txn = new InventoryTransaction
        {
            TransactionNumber = txnNumber,
            WarehouseId = receipt.WarehouseId,
            TransactionType = "Receipt",
            ReferenceType = "WarehouseReceipt",
            ReferenceId = receipt.Id,
            ReferenceNumber = receipt.ReceiptNumber,
            PerformedByEmployeeId = receipt.ReceivedByEmployeeId,
            TransactionDate = DateTime.UtcNow,
            Notes = $"Receipt confirmation {receipt.ReceiptNumber}",
            CreatedBy = _currentUser.Username
        };

        foreach (var item in receipt.ReceiptItems)
        {
            if (item.ReceivedQuantity <= 0) continue;

            var locationId = item.TargetLocationId;
            if (!locationId.HasValue)
            {
                // Find default receiving location
                var loc = await _context.WarehouseLocations
                    .FirstOrDefaultAsync(l => l.WarehouseZone.WarehouseId == receipt.WarehouseId && !l.IsBlocked, cancellationToken);
                locationId = loc?.Id ?? throw new ConflictException($"No location available in warehouse for product {item.ProductId}.");
                item.TargetLocationId = locationId;
            }

            // Resolve or create balance
            var balance = await _context.InventoryBalances
                .FirstOrDefaultAsync(b => b.WarehouseId == receipt.WarehouseId
                    && b.WarehouseLocationId == locationId.Value
                    && b.ProductId == item.ProductId, cancellationToken);

            int balanceBefore = balance?.QuantityOnHand ?? 0;

            if (balance == null)
            {
                balance = new InventoryBalance
                {
                    WarehouseId = receipt.WarehouseId,
                    WarehouseLocationId = locationId.Value,
                    ProductId = item.ProductId,
                    QuantityOnHand = item.ReceivedQuantity,
                    QuantityAllocated = 0,
                    QuantityDamaged = item.DamagedQuantity,
                    LastCountDate = DateTime.UtcNow
                };
                _context.InventoryBalances.Add(balance);
            }
            else
            {
                balance.QuantityOnHand += item.ReceivedQuantity;
                balance.QuantityDamaged += item.DamagedQuantity;
                balance.LastCountDate = DateTime.UtcNow;
            }

            int balanceAfter = balance.QuantityOnHand;

            txn.TransactionItems.Add(new InventoryTransactionItem
            {
                ProductId = item.ProductId,
                LocationId = locationId.Value,
                Quantity = item.ReceivedQuantity,
                UnitCost = item.Product?.DeclaredValue ?? 0m,
                TotalCost = (item.Product?.DeclaredValue ?? 0m) * item.ReceivedQuantity,
                BalanceBefore = balanceBefore,
                BalanceAfter = balanceAfter
            });
        }

        receipt.Status = "Completed";
        receipt.ActualArrivalDate = DateTime.UtcNow;
        receipt.UpdatedAt = DateTime.UtcNow;
        receipt.UpdatedBy = _currentUser.Username;

        _outbox?.Enqueue(
            new InventoryReceiptCompletedIntegrationEvent(receipt.Id, receipt.ReceiptNumber, receipt.WarehouseId, receipt.Warehouse.BranchId),
            "WarehouseReceipt", receipt.Id.ToString());

        _context.InventoryTransactions.Add(txn);
        await _context.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync("ConfirmReceipt", "warehouse", "WarehouseReceipts", receipt.Id.ToString(),
            new { Status = "Draft" }, new { Status = "Completed", TransactionNumber = txnNumber },
            notes: null, cancellationToken: cancellationToken);

        return (await GetReceiptByIdAsync(receipt.Id, cancellationToken))!;
    }

    // ───────────── Stock Adjustments ─────────────

    public async Task<PagedResult<StockAdjustmentDto>> GetAdjustmentsAsync(long? warehouseId, string? status, PagedRequest request, CancellationToken cancellationToken = default)
    {
        var query = _context.StockAdjustments
            .Include(a => a.Warehouse)
            .Include(a => a.ApprovedByEmployee)
            .AsNoTracking()
            .AsQueryable();

        if (!_currentUser.IsInRole(AppRoles.SuperAdmin))
        {
            var branches = _currentUser.AccessibleBranchIds;
            query = query.Where(a => branches.Contains(a.Warehouse.BranchId));
        }

        if (warehouseId.HasValue) query = query.Where(a => a.WarehouseId == warehouseId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(a => a.Status == status);

        var totalCount = await query.CountAsync(cancellationToken);
        var dbItems = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);
        var items = dbItems.Select(a => MapToAdjustmentDto(a)).ToList();

        return new PagedResult<StockAdjustmentDto>(items, totalCount, request.Page, request.PageSize);
    }

    public async Task<StockAdjustmentDto?> GetAdjustmentByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var adj = await _context.StockAdjustments
            .Include(a => a.Warehouse)
            .Include(a => a.ApprovedByEmployee)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (adj == null) return null;
        EnsureWarehouseAccess(adj.Warehouse.BranchId);
        return MapToAdjustmentDto(adj);
    }

    public async Task<StockAdjustmentDto> CreateAdjustmentAsync(CreateStockAdjustmentRequest request, CancellationToken cancellationToken = default)
    {
        var warehouse = await _context.Warehouses.FindAsync([request.WarehouseId], cancellationToken)
            ?? throw new NotFoundException("Warehouse", request.WarehouseId);
        EnsureWarehouseAccess(warehouse.BranchId);

        var balance = await _context.InventoryBalances
            .FirstOrDefaultAsync(b => b.WarehouseId == request.WarehouseId
                && b.WarehouseLocationId == request.LocationId
                && b.ProductId == request.ProductId
                && b.LotId == request.LotId, cancellationToken);

        var currentOnHand = balance?.QuantityOnHand ?? 0;
        if (currentOnHand + request.QuantityChange < 0)
            throw new ConflictException($"Adjustment would result in negative stock. Current: {currentOnHand}, change: {request.QuantityChange}.");

        var adjNumber = await _numberSequence.GetNextNumberAsync("ADJUST", cancellationToken);

        var adjustment = new StockAdjustment
        {
            AdjustmentNumber = adjNumber,
            WarehouseId = request.WarehouseId,
            ReasonCode = request.ReasonCode,
            ReasonDescription = request.ReasonDescription,
            Status = "Draft",
            CreatedBy = _currentUser.Username
        };

        _context.StockAdjustments.Add(adjustment);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToAdjustmentDto(adjustment, warehouse.Code);
    }

    public async Task<StockAdjustmentDto> ApproveAdjustmentAsync(long adjustmentId, long employeeId, CancellationToken cancellationToken = default)
    {
        var adj = await _context.StockAdjustments
            .Include(a => a.Warehouse)
            .FirstOrDefaultAsync(a => a.Id == adjustmentId, cancellationToken)
            ?? throw new NotFoundException("StockAdjustment", adjustmentId);
        EnsureWarehouseAccess(adj.Warehouse.BranchId);

        if (adj.Status == "Applied")
            throw new ConflictException("Adjustment is already applied.");

        adj.Status = "Applied";
        adj.ApprovedByEmployeeId = employeeId;
        adj.ApprovedAt = DateTime.UtcNow;
        adj.UpdatedAt = DateTime.UtcNow;
        adj.UpdatedBy = _currentUser.Username;

        await _context.SaveChangesAsync(cancellationToken);
        return MapToAdjustmentDto(adj);
    }

    // ───────────── Stock Transfers ─────────────

    public async Task<PagedResult<StockTransferDto>> GetTransfersAsync(long? sourceWarehouseId, long? destinationWarehouseId, string? status, PagedRequest request, CancellationToken cancellationToken = default)
    {
        var query = _context.StockTransfers
            .Include(t => t.SourceWarehouse)
            .Include(t => t.DestinationWarehouse)
            .Include(t => t.RequestedByEmployee)
            .Include(t => t.ApprovedByEmployee)
            .AsNoTracking()
            .AsQueryable();

        if (!_currentUser.IsInRole(AppRoles.SuperAdmin))
        {
            var branches = _currentUser.AccessibleBranchIds;
            query = query.Where(t => branches.Contains(t.SourceWarehouse.BranchId) || branches.Contains(t.DestinationWarehouse.BranchId));
        }

        if (sourceWarehouseId.HasValue) query = query.Where(t => t.SourceWarehouseId == sourceWarehouseId.Value);
        if (destinationWarehouseId.HasValue) query = query.Where(t => t.DestinationWarehouseId == destinationWarehouseId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(t => t.Status == status);

        var totalCount = await query.CountAsync(cancellationToken);
        var dbItems = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);
        var items = dbItems.Select(t => MapToTransferDto(t)).ToList();

        return new PagedResult<StockTransferDto>(items, totalCount, request.Page, request.PageSize);
    }

    public async Task<StockTransferDto?> GetTransferByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var transfer = await _context.StockTransfers
            .Include(t => t.SourceWarehouse)
            .Include(t => t.DestinationWarehouse)
            .Include(t => t.RequestedByEmployee)
            .Include(t => t.ApprovedByEmployee)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (transfer == null) return null;
        EnsureWarehouseAccess(transfer.SourceWarehouse.BranchId);
        return MapToTransferDto(transfer);
    }

    public async Task<StockTransferDto> CreateTransferAsync(CreateStockTransferRequest request, CancellationToken cancellationToken = default)
    {
        if (request.SourceWarehouseId == request.DestinationWarehouseId)
            throw new ConflictException("Source warehouse and destination warehouse cannot be the same.");
        if (request.Quantity <= 0)
            throw new ConflictException("Transfer quantity must be greater than zero.");

        var sourceWh = await _context.Warehouses.FindAsync([request.SourceWarehouseId], cancellationToken)
            ?? throw new NotFoundException("Warehouse", request.SourceWarehouseId);
        EnsureWarehouseAccess(sourceWh.BranchId);

        var destWh = await _context.Warehouses.FindAsync([request.DestinationWarehouseId], cancellationToken)
            ?? throw new NotFoundException("Warehouse", request.DestinationWarehouseId);

        // Check stock availability at source
        var balance = await _context.InventoryBalances
            .FirstOrDefaultAsync(b => b.WarehouseId == request.SourceWarehouseId
                && b.WarehouseLocationId == request.SourceLocationId
                && b.ProductId == request.ProductId
                && b.LotId == request.LotId, cancellationToken);

        var available = (balance?.QuantityOnHand ?? 0) - (balance?.QuantityAllocated ?? 0);
        if (available < request.Quantity)
            throw new ConflictException($"Insufficient stock at source location. Available: {available}, requested: {request.Quantity}.");

        var transferNumber = await _numberSequence.GetNextNumberAsync("TRANSFER", cancellationToken);

        var transfer = new StockTransfer
        {
            TransferNumber = transferNumber,
            SourceWarehouseId = request.SourceWarehouseId,
            DestinationWarehouseId = request.DestinationWarehouseId,
            Status = "Draft",
            RequestedByEmployeeId = request.RequestedByEmployeeId,
            Notes = request.Notes,
            CreatedBy = _currentUser.Username
        };

        _context.StockTransfers.Add(transfer);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToTransferDto(transfer, sourceWh.Code, destWh.Code);
    }

    public async Task<StockTransferDto> ConfirmTransferAsync(long transferId, long employeeId, CancellationToken cancellationToken = default)
    {
        var transfer = await _context.StockTransfers
            .Include(t => t.SourceWarehouse)
            .Include(t => t.DestinationWarehouse)
            .FirstOrDefaultAsync(t => t.Id == transferId, cancellationToken)
            ?? throw new NotFoundException("StockTransfer", transferId);
        EnsureWarehouseAccess(transfer.SourceWarehouse.BranchId);

        if (transfer.Status != "Draft")
            throw new ConflictException($"Transfer is in status '{transfer.Status}', cannot confirm.");

        transfer.Status = "InTransit";
        transfer.ApprovedByEmployeeId = employeeId;
        transfer.ShippedAt = DateTime.UtcNow;
        transfer.UpdatedAt = DateTime.UtcNow;
        transfer.UpdatedBy = _currentUser.Username;

        await _context.SaveChangesAsync(cancellationToken);
        return MapToTransferDto(transfer);
    }

    public async Task<StockTransferDto> ReceiveTransferAsync(long transferId, ReceiveStockTransferRequest request, long employeeId, CancellationToken cancellationToken = default)
    {
        var transfer = await _context.StockTransfers
            .Include(t => t.SourceWarehouse)
            .Include(t => t.DestinationWarehouse)
            .FirstOrDefaultAsync(t => t.Id == transferId, cancellationToken)
            ?? throw new NotFoundException("StockTransfer", transferId);
        EnsureWarehouseAccess(transfer.DestinationWarehouse.BranchId);

        if (transfer.Status != "InTransit")
            throw new ConflictException($"Transfer is not in transit (status: '{transfer.Status}').");

        transfer.Status = "Received";
        transfer.ReceivedAt = DateTime.UtcNow;
        transfer.UpdatedAt = DateTime.UtcNow;
        transfer.UpdatedBy = _currentUser.Username;

        await _context.SaveChangesAsync(cancellationToken);
        return MapToTransferDto(transfer);
    }

    // ───────────── Inventory Reservations ─────────────

    public async Task<PagedResult<InventoryReservationDto>> GetReservationsAsync(long? warehouseId, long? productId, long? orderId, string? status, PagedRequest request, CancellationToken cancellationToken = default)
    {
        var query = _context.InventoryReservations
            .Include(r => r.Product)
            .Include(r => r.Warehouse)
            .Include(r => r.Location)
            .Include(r => r.Lot)
            .AsNoTracking()
            .AsQueryable();

        if (!_currentUser.IsInRole(AppRoles.SuperAdmin))
        {
            var branches = _currentUser.AccessibleBranchIds;
            query = query.Where(r => branches.Contains(r.Warehouse.BranchId));
        }

        if (warehouseId.HasValue) query = query.Where(r => r.WarehouseId == warehouseId.Value);
        if (productId.HasValue) query = query.Where(r => r.ProductId == productId.Value);
        if (orderId.HasValue) query = query.Where(r => r.OrderId == orderId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(r => r.Status == status);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(r => r.ReservedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => MapToReservationDto(r))
            .ToListAsync(cancellationToken);

        return new PagedResult<InventoryReservationDto>(items, totalCount, request.Page, request.PageSize);
    }

    public async Task<InventoryReservationDto> CreateReservationAsync(CreateInventoryReservationRequest request, CancellationToken cancellationToken = default)
    {
        var warehouse = await _context.Warehouses.FindAsync([request.WarehouseId], cancellationToken)
            ?? throw new NotFoundException("Warehouse", request.WarehouseId);
        EnsureWarehouseAccess(warehouse.BranchId);

        var balance = await _context.InventoryBalances
            .FirstOrDefaultAsync(b => b.WarehouseId == request.WarehouseId
                && b.WarehouseLocationId == request.LocationId
                && b.ProductId == request.ProductId
                && b.LotId == request.LotId, cancellationToken)
            ?? throw new NotFoundException("InventoryBalance for requested product and location", request.ProductId);

        var available = balance.QuantityOnHand - balance.QuantityAllocated;
        if (available < request.ReservedQuantity)
            throw new ConflictException($"Insufficient stock for reservation. Available: {available}, requested: {request.ReservedQuantity}.");

        // Increase allocated stock atomically
        balance.QuantityAllocated += request.ReservedQuantity;

        var reservation = new InventoryReservation
        {
            ProductId = request.ProductId,
            WarehouseId = request.WarehouseId,
            LocationId = request.LocationId,
            LotId = request.LotId,
            OrderId = request.OrderId,
            ReservedQuantity = request.ReservedQuantity,
            Status = "Active",
            ReservedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(request.DurationMinutes),
            CreatedBy = _currentUser.Username
        };

        _context.InventoryReservations.Add(reservation);
        await _context.SaveChangesAsync(cancellationToken);

        var product = await _context.Products.FindAsync([request.ProductId], cancellationToken);
        var location = await _context.WarehouseLocations.FindAsync([request.LocationId], cancellationToken);
        reservation.Product = product!;
        reservation.Warehouse = warehouse;
        reservation.Location = location!;

        return MapToReservationDto(reservation);
    }

    public async Task<InventoryReservationDto> ReleaseReservationAsync(long reservationId, CancellationToken cancellationToken = default)
    {
        var res = await _context.InventoryReservations
            .Include(r => r.Product)
            .Include(r => r.Warehouse)
            .Include(r => r.Location)
            .FirstOrDefaultAsync(r => r.Id == reservationId, cancellationToken)
            ?? throw new NotFoundException("InventoryReservation", reservationId);
        EnsureWarehouseAccess(res.Warehouse.BranchId);

        if (res.Status != "Active")
            throw new ConflictException($"Reservation is not active (status: '{res.Status}').");

        // Release allocated stock
        var balance = await _context.InventoryBalances
            .FirstOrDefaultAsync(b => b.WarehouseId == res.WarehouseId
                && b.WarehouseLocationId == res.LocationId
                && b.ProductId == res.ProductId
                && b.LotId == res.LotId, cancellationToken);

        if (balance != null)
        {
            balance.QuantityAllocated = Math.Max(0, balance.QuantityAllocated - res.ReservedQuantity);
        }

        res.Status = "Released";
        res.UpdatedAt = DateTime.UtcNow;
        res.UpdatedBy = _currentUser.Username;

        await _context.SaveChangesAsync(cancellationToken);
        return MapToReservationDto(res);
    }

    public async Task<InventoryReservationDto> ConsumeReservationAsync(long reservationId, CancellationToken cancellationToken = default)
    {
        var res = await _context.InventoryReservations
            .Include(r => r.Product)
            .Include(r => r.Warehouse)
            .Include(r => r.Location)
            .FirstOrDefaultAsync(r => r.Id == reservationId, cancellationToken)
            ?? throw new NotFoundException("InventoryReservation", reservationId);
        EnsureWarehouseAccess(res.Warehouse.BranchId);

        if (res.Status != "Active")
            throw new ConflictException($"Reservation is not active (status: '{res.Status}').");

        // Deduct both allocated and on-hand atomically after strict sufficiency validation
        var ownsTx = _context.Database.IsRelational() && _context.Database.CurrentTransaction == null;
        await using var tx = ownsTx ? await _context.Database.BeginTransactionAsync(cancellationToken) : null;

        var balance = await _context.InventoryBalances
            .FirstOrDefaultAsync(b => b.WarehouseId == res.WarehouseId
                && b.WarehouseLocationId == res.LocationId
                && b.ProductId == res.ProductId
                && b.LotId == res.LotId, cancellationToken)
            ?? throw new ConflictException("No matching inventory balance exists for this reservation.");

        if (balance.QuantityOnHand < res.ReservedQuantity || balance.QuantityAllocated < res.ReservedQuantity)
        {
            throw new ConflictException(
                $"Insufficient balance to consume reservation {res.Id}. OnHand={balance.QuantityOnHand}, Allocated={balance.QuantityAllocated}, Reserved={res.ReservedQuantity}.");
        }

        var balanceBefore = balance.QuantityOnHand;
        balance.QuantityAllocated -= res.ReservedQuantity;
        balance.QuantityOnHand -= res.ReservedQuantity;

        var employeeId = _currentUser.EmployeeId;
        if (!employeeId.HasValue && _currentUser.UserId.HasValue)
        {
            employeeId = await _context.UserAccounts.AsNoTracking()
                .Where(u => u.Id == _currentUser.UserId.Value)
                .Select(u => u.EmployeeId)
                .FirstOrDefaultAsync(cancellationToken);
        }
        employeeId ??= await _context.Employees.AsNoTracking()
            .OrderBy(e => e.Id)
            .Select(e => (long?)e.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (!employeeId.HasValue)
            throw new ConflictException("Cannot record inventory dispatch transaction without an employee reference.");

        res.Status = "Fulfilled";
        res.FulfilledAt = DateTime.UtcNow;
        res.UpdatedAt = DateTime.UtcNow;
        res.UpdatedBy = _currentUser.Username;

        // Record transaction
        var txnNumber = await _numberSequence.GetNextNumberAsync("INV-TXN", cancellationToken);
        var txn = new InventoryTransaction
        {
            TransactionNumber = txnNumber,
            WarehouseId = res.WarehouseId,
            TransactionType = "Dispatch",
            ReferenceType = "SalesOrder",
            ReferenceId = res.OrderId,
            ReferenceNumber = $"ORD-{res.OrderId}",
            PerformedByEmployeeId = employeeId.Value,
            TransactionDate = DateTime.UtcNow,
            Notes = $"Reservation {res.Id} consumed for order {res.OrderId}",
            CreatedBy = _currentUser.Username
        };

        txn.TransactionItems.Add(new InventoryTransactionItem
        {
            ProductId = res.ProductId,
            LocationId = res.LocationId,
            LotId = res.LotId,
            Quantity = res.ReservedQuantity,
            BalanceBefore = balanceBefore,
            BalanceAfter = balance.QuantityOnHand
        });

        _context.InventoryTransactions.Add(txn);
        await _context.SaveChangesAsync(cancellationToken);

        if (tx != null)
            await tx.CommitAsync(cancellationToken);

        return MapToReservationDto(res);
    }

    // ───────────── Lots & Serial Numbers ─────────────

    public async Task<PagedResult<InventoryLotDto>> GetLotsAsync(long? productId, PagedRequest request, CancellationToken cancellationToken = default)
    {
        var query = _context.InventoryLots
            .Include(l => l.Product)
            .AsNoTracking()
            .AsQueryable();

        if (productId.HasValue) query = query.Where(l => l.ProductId == productId.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(l => l.LotNumber)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(l => new InventoryLotDto
            {
                Id = l.Id,
                PublicId = l.PublicId,
                ProductId = l.ProductId,
                ProductCode = l.Product.Sku,
                LotNumber = l.LotNumber,
                ManufactureDate = l.ManufactureDate,
                ExpiryDate = l.ExpiryDate,
                VendorBatchCode = l.VendorBatchCode,
                Notes = l.Notes
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<InventoryLotDto>(items, totalCount, request.Page, request.PageSize);
    }

    public async Task<InventoryLotDto> CreateLotAsync(CreateInventoryLotRequest request, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products.FindAsync([request.ProductId], cancellationToken)
            ?? throw new NotFoundException("Product", request.ProductId);

        var exists = await _context.InventoryLots.AnyAsync(
            l => l.ProductId == request.ProductId && l.LotNumber == request.LotNumber, cancellationToken);
        if (exists)
            throw new ConflictException("InventoryLot", "LotNumber", request.LotNumber);

        var lot = new InventoryLot
        {
            ProductId = request.ProductId,
            LotNumber = request.LotNumber,
            ManufactureDate = request.ManufactureDate,
            ExpiryDate = request.ExpiryDate,
            VendorBatchCode = request.VendorBatchCode,
            Notes = request.Notes,
            CreatedBy = _currentUser.Username
        };

        _context.InventoryLots.Add(lot);
        await _context.SaveChangesAsync(cancellationToken);

        return new InventoryLotDto
        {
            Id = lot.Id,
            PublicId = lot.PublicId,
            ProductId = lot.ProductId,
            ProductCode = product.Sku,
            LotNumber = lot.LotNumber,
            ManufactureDate = lot.ManufactureDate,
            ExpiryDate = lot.ExpiryDate,
            VendorBatchCode = lot.VendorBatchCode,
            Notes = lot.Notes
        };
    }

    public async Task<PagedResult<InventorySerialNumberDto>> GetSerialNumbersAsync(long? productId, long? warehouseId, string? status, PagedRequest request, CancellationToken cancellationToken = default)
    {
        var query = _context.InventorySerialNumbers
            .Include(s => s.Product)
            .Include(s => s.Warehouse)
            .Include(s => s.Location)
            .AsNoTracking()
            .AsQueryable();

        if (productId.HasValue) query = query.Where(s => s.ProductId == productId.Value);
        if (warehouseId.HasValue) query = query.Where(s => s.WarehouseId == warehouseId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(s => s.Status == status);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(s => s.SerialNumber)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new InventorySerialNumberDto
            {
                Id = s.Id,
                ProductId = s.ProductId,
                ProductCode = s.Product.Sku,
                SerialNumber = s.SerialNumber,
                WarehouseId = s.WarehouseId,
                WarehouseCode = s.Warehouse.Code,
                LocationId = s.LocationId,
                LocationCode = s.Location != null ? s.Location.Code : null,
                Status = s.Status,
                ReceivedAt = s.ReceivedAt,
                DispatchedAt = s.DispatchedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<InventorySerialNumberDto>(items, totalCount, request.Page, request.PageSize);
    }

    public async Task<InventorySerialNumberDto> CreateSerialNumberAsync(CreateInventorySerialNumberRequest request, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products.FindAsync([request.ProductId], cancellationToken)
            ?? throw new NotFoundException("Product", request.ProductId);

        var warehouse = await _context.Warehouses.FindAsync([request.WarehouseId], cancellationToken)
            ?? throw new NotFoundException("Warehouse", request.WarehouseId);
        EnsureWarehouseAccess(warehouse.BranchId);

        var exists = await _context.InventorySerialNumbers.AnyAsync(
            s => s.ProductId == request.ProductId && s.SerialNumber == request.SerialNumber, cancellationToken);
        if (exists)
            throw new ConflictException("InventorySerialNumber", "SerialNumber", request.SerialNumber);

        var sn = new InventorySerialNumber
        {
            ProductId = request.ProductId,
            SerialNumber = request.SerialNumber,
            WarehouseId = request.WarehouseId,
            LocationId = request.LocationId,
            Status = "Available",
            ReceivedAt = DateTime.UtcNow
        };

        _context.InventorySerialNumbers.Add(sn);
        await _context.SaveChangesAsync(cancellationToken);

        return new InventorySerialNumberDto
        {
            Id = sn.Id,
            ProductId = sn.ProductId,
            ProductCode = product.Sku,
            SerialNumber = sn.SerialNumber,
            WarehouseId = sn.WarehouseId,
            WarehouseCode = warehouse.Code,
            LocationId = sn.LocationId,
            Status = sn.Status,
            ReceivedAt = sn.ReceivedAt
        };
    }

    // ───────────── Mappers ─────────────

    private static InventoryItemDto MapToItemDto(InventoryItem i) => new()
    {
        Id = i.Id,
        PublicId = i.PublicId,
        ProductId = i.ProductId,
        ProductCode = i.Product.Sku,
        ProductName = i.Product.Name,
        ProductSku = i.Product.Sku,
        WarehouseId = i.WarehouseId,
        WarehouseCode = i.Warehouse.Code,
        ReorderPoint = i.ReorderPoint,
        SafetyStock = i.SafetyStock,
        TargetStock = i.TargetStock,
        MinimumOrderQuantity = i.MinimumOrderQuantity,
        MaximumStock = i.MaximumStock,
        IsActive = i.IsActive
    };

    private static InventoryBalanceDto MapToBalanceDto(InventoryBalance b) => new()
    {
        Id = b.Id,
        WarehouseId = b.WarehouseId,
        WarehouseCode = b.Warehouse.Code,
        WarehouseLocationId = b.WarehouseLocationId,
        LocationCode = b.WarehouseLocation.Code,
        ProductId = b.ProductId,
        ProductCode = b.Product.Sku,
        ProductName = b.Product.Name,
        LotId = b.LotId,
        LotNumber = b.Lot?.LotNumber,
        QuantityOnHand = b.QuantityOnHand,
        QuantityAllocated = b.QuantityAllocated,
        QuantityAvailable = b.QuantityOnHand - b.QuantityAllocated,
        QuantityDamaged = b.QuantityDamaged,
        LastCountDate = b.LastCountDate
    };

    private static InventoryTransactionDto MapToTransactionDto(InventoryTransaction t) => new()
    {
        Id = t.Id,
        PublicId = t.PublicId,
        TransactionNumber = t.TransactionNumber,
        WarehouseId = t.WarehouseId,
        WarehouseCode = t.Warehouse.Code,
        TransactionType = t.TransactionType,
        ReferenceType = t.ReferenceType,
        ReferenceId = t.ReferenceId,
        ReferenceNumber = t.ReferenceNumber,
        PerformedByEmployeeId = t.PerformedByEmployeeId,
        PerformedByEmployeeName = t.PerformedByEmployee?.FullName,
        TransactionDate = t.TransactionDate,
        Notes = t.Notes,
        Items = t.TransactionItems.Select(ti => new InventoryTransactionItemDto
        {
            Id = ti.Id,
            ProductId = ti.ProductId,
            ProductCode = ti.Product.Sku,
            ProductName = ti.Product.Name,
            LocationId = ti.LocationId,
            LocationCode = ti.Location.Code,
            LotId = ti.LotId,
            LotNumber = ti.Lot?.LotNumber,
            Quantity = ti.Quantity,
            UnitCost = ti.UnitCost,
            TotalCost = ti.TotalCost,
            BalanceBefore = ti.BalanceBefore,
            BalanceAfter = ti.BalanceAfter
        }).ToList()
    };

    private static WarehouseReceiptDto MapToReceiptDto(WarehouseReceipt r) => new()
    {
        Id = r.Id,
        PublicId = r.PublicId,
        ReceiptNumber = r.ReceiptNumber,
        WarehouseId = r.WarehouseId,
        WarehouseCode = r.Warehouse.Code,
        ReceiptType = r.ReceiptType,
        SourceBranchId = r.SourceBranchId,
        SourceVendor = r.SourceVendor,
        ExpectedArrivalDate = r.ExpectedArrivalDate,
        ActualArrivalDate = r.ActualArrivalDate,
        Status = r.Status,
        ReceivedByEmployeeId = r.ReceivedByEmployeeId,
        ReceivedByEmployeeName = r.ReceivedByEmployee?.FullName,
        Notes = r.Notes,
        Items = r.ReceiptItems.Select(ri => new WarehouseReceiptItemDto
        {
            Id = ri.Id,
            WarehouseReceiptId = ri.WarehouseReceiptId,
            ProductId = ri.ProductId,
            ProductCode = ri.Product?.Sku ?? string.Empty,
            ProductName = ri.Product?.Name ?? string.Empty,
            ExpectedQuantity = ri.ExpectedQuantity,
            ReceivedQuantity = ri.ReceivedQuantity,
            DamagedQuantity = ri.DamagedQuantity,
            UnitOfMeasureId = ri.UnitOfMeasureId,
            UnitName = ri.UnitOfMeasure?.Name,
            TargetLocationId = ri.TargetLocationId,
            TargetLocationCode = ri.TargetLocation?.Code,
            LotNumber = ri.LotNumber,
            ExpiryDate = ri.ExpiryDate,
            Notes = ri.Notes
        }).ToList()
    };

    private static StockAdjustmentDto MapToAdjustmentDto(StockAdjustment a, string? warehouseCode = null) => new()
    {
        Id = a.Id,
        PublicId = a.PublicId,
        AdjustmentNumber = a.AdjustmentNumber,
        WarehouseId = a.WarehouseId,
        WarehouseCode = warehouseCode ?? a.Warehouse?.Code ?? string.Empty,
        ReasonCode = a.ReasonCode,
        ReasonDescription = a.ReasonDescription,
        Status = a.Status,
        ApprovedByEmployeeId = a.ApprovedByEmployeeId,
        ApprovedByEmployeeName = a.ApprovedByEmployee?.FullName,
        ApprovedAt = a.ApprovedAt
    };

    private static StockTransferDto MapToTransferDto(StockTransfer t, string? sourceCode = null, string? destCode = null) => new()
    {
        Id = t.Id,
        PublicId = t.PublicId,
        TransferNumber = t.TransferNumber,
        SourceWarehouseId = t.SourceWarehouseId,
        SourceWarehouseCode = sourceCode ?? t.SourceWarehouse?.Code ?? string.Empty,
        DestinationWarehouseId = t.DestinationWarehouseId,
        DestinationWarehouseCode = destCode ?? t.DestinationWarehouse?.Code ?? string.Empty,
        Status = t.Status,
        RequestedByEmployeeId = t.RequestedByEmployeeId,
        RequestedByEmployeeName = t.RequestedByEmployee?.FullName,
        ApprovedByEmployeeId = t.ApprovedByEmployeeId,
        ApprovedByEmployeeName = t.ApprovedByEmployee?.FullName,
        ShippedAt = t.ShippedAt,
        ReceivedAt = t.ReceivedAt,
        Notes = t.Notes
    };

    private static InventoryReservationDto MapToReservationDto(InventoryReservation r) => new()
    {
        Id = r.Id,
        PublicId = r.PublicId,
        ProductId = r.ProductId,
        ProductCode = r.Product?.Sku ?? string.Empty,
        ProductName = r.Product?.Name ?? string.Empty,
        WarehouseId = r.WarehouseId,
        WarehouseCode = r.Warehouse?.Code ?? string.Empty,
        LocationId = r.LocationId,
        LocationCode = r.Location?.Code ?? string.Empty,
        LotId = r.LotId,
        LotNumber = r.Lot?.LotNumber,
        OrderId = r.OrderId,
        ReservedQuantity = r.ReservedQuantity,
        Status = r.Status,
        ReservedAt = r.ReservedAt,
        ExpiresAt = r.ExpiresAt,
        FulfilledAt = r.FulfilledAt
    };
}

// ═══════════════════════════════════════════════════════════════
//  3. PARCEL HANDLING & SCANNING SERVICE
// ═══════════════════════════════════════════════════════════════

public class ParcelHandlingService : IParcelHandlingService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly INumberSequenceService _numberSequence;
    private readonly IShipmentService _shipmentService;
    private readonly IAuditService _audit;
    private readonly ILogger<ParcelHandlingService> _logger;

    public ParcelHandlingService(
        ApplicationDbContext context,
        ICurrentUserService currentUser,
        INumberSequenceService numberSequence,
        IShipmentService shipmentService,
        IAuditService audit,
        ILogger<ParcelHandlingService> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _numberSequence = numberSequence;
        _shipmentService = shipmentService;
        _audit = audit;
        _logger = logger;
    }

    private void EnsureWarehouseAccess(long branchId)
    {
        if (_currentUser.IsInRole(AppRoles.SuperAdmin)) return;
        if (!_currentUser.HasBranchAccess(branchId))
            throw new ForbiddenAccessException($"User does not have access to branch {branchId}.");
    }

    public async Task<ScanPackageResultDto> ScanPackageAsync(ScanPackageRequest request, CancellationToken cancellationToken = default)
    {
        var warehouse = await _context.Warehouses.FindAsync([request.WarehouseId], cancellationToken)
            ?? throw new NotFoundException("Warehouse", request.WarehouseId);
        EnsureWarehouseAccess(warehouse.BranchId);

        var package = await FindPackageAsync(request.TrackingNumberOrBarcode, cancellationToken)
            ?? throw new NotFoundException("ShipmentPackage", request.TrackingNumberOrBarcode);

        var shipment = package.Shipment;

        // Determine permitted next operation based on package & shipment state
        string nextOp;
        string? message = null;

        if (package.PackageStatus == "Created" || package.PackageStatus == "InTransit" || shipment.ShipmentStatus == "PickedUp")
        {
            nextOp = "Receive";
            message = "Package is eligible for arrival scan at hub.";
        }
        else if (package.PackageStatus == "Received")
        {
            nextOp = "Sort";
            message = "Package received. Route to sorting zone.";
        }
        else if (package.PackageStatus == "Sorted")
        {
            nextOp = "Stage";
            message = "Package sorted. Stage in outbound bay or bin.";
        }
        else if (package.PackageStatus == "Staged")
        {
            nextOp = "Dispatch";
            message = "Package staged. Ready for manifest or vehicle dispatch.";
        }
        else
        {
            nextOp = "None";
            message = $"Package is currently '{package.PackageStatus}' (Shipment: '{shipment.ShipmentStatus}').";
        }

        // Record a scan telemetry event
        _context.ParcelHandlingEvents.Add(new ParcelHandlingEvent
        {
            ShipmentPackageId = package.Id,
            ShipmentId = shipment.Id,
            WarehouseId = request.WarehouseId,
            EventType = "ScannedAtHub",
            EmployeeId = request.EmployeeId,
            ScannedAt = DateTime.UtcNow,
            DeviceId = request.DeviceId,
            Notes = $"Scan check: {nextOp}"
        });
        await _context.SaveChangesAsync(cancellationToken);

        return new ScanPackageResultDto
        {
            PackageId = package.Id,
            PackageCode = package.PackageCode,
            Barcode = package.Barcode,
            PackageStatus = package.PackageStatus,
            ShipmentId = shipment.Id,
            ShipmentTrackingNumber = shipment.TrackingNumber,
            ShipmentStatus = shipment.ShipmentStatus,
            OriginBranchId = shipment.OriginBranchId,
            DestinationBranchId = shipment.DestinationBranchId,
            WeightKg = package.WeightKg,
            PermittedNextOperation = nextOp,
            Message = message
        };
    }

    public async Task<ParcelHandlingEventDto> ReceiveParcelAsync(ParcelReceiveRequest request, CancellationToken cancellationToken = default)
    {
        var warehouse = await _context.Warehouses.FindAsync([request.WarehouseId], cancellationToken)
            ?? throw new NotFoundException("Warehouse", request.WarehouseId);
        EnsureWarehouseAccess(warehouse.BranchId);

        var package = await FindPackageAsync(request.TrackingNumberOrBarcode, cancellationToken)
            ?? throw new NotFoundException("ShipmentPackage", request.TrackingNumberOrBarcode);

        var shipment = package.Shipment;

        // Prevent duplicate receiving in same state
        if (package.PackageStatus == "Received")
            throw new ConflictException($"Package {package.Barcode} is already received at facility.");

        var ownsTx = _context.Database.IsRelational() && _context.Database.CurrentTransaction == null;
        await using var tx = ownsTx ? await _context.Database.BeginTransactionAsync(cancellationToken) : null;

        package.PackageStatus = "Received";
        package.UpdatedAt = DateTime.UtcNow;
        package.UpdatedBy = _currentUser.Username;

        // Coordinate shipment lifecycle status transition atomically within transaction
        if (shipment.ShipmentStatus == "PickedUp")
        {
            await _shipmentService.TransitionStatusAsync(shipment.Id.ToString(), new ShipmentStatusTransitionRequest
            {
                TargetStatus = "InSortingHub",
                LocationWarehouseId = warehouse.Id,
                Notes = $"Received at hub {warehouse.Code}"
            }, cancellationToken);
        }

        var handlingEvent = new ParcelHandlingEvent
        {
            ShipmentPackageId = package.Id,
            ShipmentId = shipment.Id,
            WarehouseId = request.WarehouseId,
            EventType = "Unloaded",
            EmployeeId = request.EmployeeId,
            ScannedAt = DateTime.UtcNow,
            DeviceId = request.DeviceId,
            Notes = request.Notes
        };

        _context.ParcelHandlingEvents.Add(handlingEvent);
        await _context.SaveChangesAsync(cancellationToken);

        if (tx != null)
            await tx.CommitAsync(cancellationToken);

        await _audit.RecordAsync("ReceiveParcel", "warehouse", "ParcelHandlingEvents", handlingEvent.Id.ToString(),
            null, new { package.Barcode, warehouse.Code, EventType = "Unloaded" }, notes: null, cancellationToken: cancellationToken);

        return MapToEventDto(handlingEvent, package, shipment, warehouse);
    }

    public async Task<ParcelHandlingEventDto> SortParcelAsync(ParcelSortRequest request, CancellationToken cancellationToken = default)
    {
        var warehouse = await _context.Warehouses.FindAsync([request.WarehouseId], cancellationToken)
            ?? throw new NotFoundException("Warehouse", request.WarehouseId);
        EnsureWarehouseAccess(warehouse.BranchId);

        var zone = await _context.WarehouseZones
            .FirstOrDefaultAsync(z => z.WarehouseId == request.WarehouseId && z.Id == request.ZoneId, cancellationToken)
            ?? throw new NotFoundException("WarehouseZone", request.ZoneId);

        if (!zone.IsActive)
            throw new ConflictException("Cannot sort into an inactive zone.");

        var package = await FindPackageAsync(request.TrackingNumberOrBarcode, cancellationToken)
            ?? throw new NotFoundException("ShipmentPackage", request.TrackingNumberOrBarcode);

        package.PackageStatus = "Sorted";
        package.UpdatedAt = DateTime.UtcNow;
        package.UpdatedBy = _currentUser.Username;

        var handlingEvent = new ParcelHandlingEvent
        {
            ShipmentPackageId = package.Id,
            ShipmentId = package.ShipmentId,
            WarehouseId = request.WarehouseId,
            EventType = "Sorted",
            EmployeeId = request.EmployeeId,
            ScannedAt = DateTime.UtcNow,
            BagNumber = request.TargetContainerCode,
            Notes = $"Sorted to zone {zone.Code}. {request.Notes}"
        };

        _context.ParcelHandlingEvents.Add(handlingEvent);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToEventDto(handlingEvent, package, package.Shipment, warehouse);
    }

    public async Task<ParcelHandlingEventDto> StageParcelAsync(ParcelStageRequest request, CancellationToken cancellationToken = default)
    {
        var warehouse = await _context.Warehouses.FindAsync([request.WarehouseId], cancellationToken)
            ?? throw new NotFoundException("Warehouse", request.WarehouseId);
        EnsureWarehouseAccess(warehouse.BranchId);

        var location = await _context.WarehouseLocations
            .Include(l => l.WarehouseZone)
            .FirstOrDefaultAsync(l => l.WarehouseZone.WarehouseId == request.WarehouseId && l.Id == request.LocationId, cancellationToken)
            ?? throw new NotFoundException("WarehouseLocation", request.LocationId);

        if (location.IsBlocked)
            throw new ConflictException("Cannot stage package in a blocked location.");

        var package = await FindPackageAsync(request.TrackingNumberOrBarcode, cancellationToken)
            ?? throw new NotFoundException("ShipmentPackage", request.TrackingNumberOrBarcode);

        package.PackageStatus = "Staged";
        package.UpdatedAt = DateTime.UtcNow;
        package.UpdatedBy = _currentUser.Username;

        var handlingEvent = new ParcelHandlingEvent
        {
            ShipmentPackageId = package.Id,
            ShipmentId = package.ShipmentId,
            WarehouseId = request.WarehouseId,
            EventType = "Staged",
            BagNumber = request.BagNumber,
            EmployeeId = request.EmployeeId,
            ScannedAt = DateTime.UtcNow,
            Notes = $"Staged at location {location.Code}. {request.Notes}"
        };

        _context.ParcelHandlingEvents.Add(handlingEvent);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToEventDto(handlingEvent, package, package.Shipment, warehouse);
    }

    public async Task<ParcelHandlingEventDto> RecordExceptionAsync(ParcelExceptionRequest request, CancellationToken cancellationToken = default)
    {
        var warehouse = await _context.Warehouses.FindAsync([request.WarehouseId], cancellationToken)
            ?? throw new NotFoundException("Warehouse", request.WarehouseId);
        EnsureWarehouseAccess(warehouse.BranchId);

        var package = await FindPackageAsync(request.TrackingNumberOrBarcode, cancellationToken)
            ?? throw new NotFoundException("ShipmentPackage", request.TrackingNumberOrBarcode);

        if (request.ExceptionType.Equals("Damaged", StringComparison.OrdinalIgnoreCase))
            package.PackageStatus = "Damaged";
        else if (request.ExceptionType.Equals("Missing", StringComparison.OrdinalIgnoreCase))
            package.PackageStatus = "Lost";

        package.UpdatedAt = DateTime.UtcNow;
        package.UpdatedBy = _currentUser.Username;

        var handlingEvent = new ParcelHandlingEvent
        {
            ShipmentPackageId = package.Id,
            ShipmentId = package.ShipmentId,
            WarehouseId = request.WarehouseId,
            EventType = "Exception",
            EmployeeId = request.EmployeeId,
            ScannedAt = DateTime.UtcNow,
            DeviceId = request.DeviceId,
            Notes = $"[{request.ExceptionType}] {request.Reason}"
        };

        _context.ParcelHandlingEvents.Add(handlingEvent);
        await _context.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync("ParcelException", "warehouse", "ParcelHandlingEvents", handlingEvent.Id.ToString(),
            null, new { package.Barcode, request.ExceptionType, request.Reason }, notes: null, cancellationToken: cancellationToken);

        return MapToEventDto(handlingEvent, package, package.Shipment, warehouse);
    }

    public async Task<PagedResult<ParcelHandlingEventDto>> GetEventsAsync(long? shipmentPackageId, long? shipmentId, long? warehouseId, string? eventType, PagedRequest request, CancellationToken cancellationToken = default)
    {
        var query = _context.ParcelHandlingEvents
            .Include(e => e.ShipmentPackage)
            .Include(e => e.Shipment)
            .Include(e => e.Warehouse)
            .Include(e => e.Employee)
            .AsNoTracking()
            .AsQueryable();

        if (!_currentUser.IsInRole(AppRoles.SuperAdmin))
        {
            var branches = _currentUser.AccessibleBranchIds;
            query = query.Where(e => branches.Contains(e.Warehouse.BranchId));
        }

        if (shipmentPackageId.HasValue) query = query.Where(e => e.ShipmentPackageId == shipmentPackageId.Value);
        if (shipmentId.HasValue) query = query.Where(e => e.ShipmentId == shipmentId.Value);
        if (warehouseId.HasValue) query = query.Where(e => e.WarehouseId == warehouseId.Value);
        if (!string.IsNullOrWhiteSpace(eventType)) query = query.Where(e => e.EventType == eventType);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(e => e.ScannedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(e => MapToEventDto(e, e.ShipmentPackage, e.Shipment, e.Warehouse))
            .ToListAsync(cancellationToken);

        return new PagedResult<ParcelHandlingEventDto>(items, totalCount, request.Page, request.PageSize);
    }

    public async Task<PagedResult<WarehouseDispatchDto>> GetDispatchesAsync(long? warehouseId, string? dispatchType, string? status, PagedRequest request, CancellationToken cancellationToken = default)
    {
        var query = _context.WarehouseDispatches
            .Include(d => d.Warehouse)
            .Include(d => d.DestinationWarehouse)
            .Include(d => d.AssignedCarrier)
            .Include(d => d.DispatchedByEmployee)
            .Include(d => d.DispatchPackages)
                .ThenInclude(dp => dp.ShipmentPackage)
            .Include(d => d.DispatchItems)
                .ThenInclude(di => di.Product)
            .Include(d => d.DispatchItems)
                .ThenInclude(di => di.SourceLocation)
            .AsNoTracking()
            .AsQueryable();

        if (!_currentUser.IsInRole(AppRoles.SuperAdmin))
        {
            var branches = _currentUser.AccessibleBranchIds;
            query = query.Where(d => branches.Contains(d.Warehouse.BranchId));
        }

        if (warehouseId.HasValue) query = query.Where(d => d.WarehouseId == warehouseId.Value);
        if (!string.IsNullOrWhiteSpace(dispatchType)) query = query.Where(d => d.DispatchType == dispatchType);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(d => d.Status == status);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(d => MapToDispatchDto(d))
            .ToListAsync(cancellationToken);

        return new PagedResult<WarehouseDispatchDto>(items, totalCount, request.Page, request.PageSize);
    }

    public async Task<WarehouseDispatchDto?> GetDispatchByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var dispatch = await _context.WarehouseDispatches
            .Include(d => d.Warehouse)
            .Include(d => d.DestinationWarehouse)
            .Include(d => d.AssignedCarrier)
            .Include(d => d.DispatchedByEmployee)
            .Include(d => d.DispatchPackages)
                .ThenInclude(dp => dp.ShipmentPackage)
            .Include(d => d.DispatchItems)
                .ThenInclude(di => di.Product)
            .Include(d => d.DispatchItems)
                .ThenInclude(di => di.SourceLocation)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (dispatch == null) return null;
        EnsureWarehouseAccess(dispatch.Warehouse.BranchId);
        return MapToDispatchDto(dispatch);
    }

    public async Task<WarehouseDispatchDto> CreateDispatchAsync(CreateWarehouseDispatchDto request, CancellationToken cancellationToken = default)
    {
        var warehouse = await _context.Warehouses.FindAsync([request.WarehouseId], cancellationToken)
            ?? throw new NotFoundException("Warehouse", request.WarehouseId);
        EnsureWarehouseAccess(warehouse.BranchId);

        var dispatchNumber = await _numberSequence.GetNextNumberAsync("DISPATCH", cancellationToken);

        var dispatch = new WarehouseDispatch
        {
            DispatchNumber = dispatchNumber,
            WarehouseId = request.WarehouseId,
            DispatchType = request.DispatchType,
            DestinationWarehouseId = request.DestinationWarehouseId,
            DestinationBranchId = request.DestinationBranchId,
            AssignedCarrierId = request.AssignedCarrierId,
            Status = "Draft",
            DispatchedByEmployeeId = request.DispatchedByEmployeeId,
            VehicleNumber = request.VehicleNumber,
            Notes = request.Notes,
            CreatedBy = _currentUser.Username
        };

        if (request.Packages != null)
        {
            foreach (var pkgReq in request.Packages)
            {
                var pkg = await _context.ShipmentPackages.FindAsync([pkgReq.ShipmentPackageId], cancellationToken)
                    ?? throw new NotFoundException("ShipmentPackage", pkgReq.ShipmentPackageId);

                dispatch.DispatchPackages.Add(new WarehouseDispatchPackage
                {
                    ShipmentPackageId = pkg.Id,
                    Barcode = pkg.Barcode,
                    ScannedAt = DateTime.UtcNow,
                    ScannedByEmployeeId = request.DispatchedByEmployeeId,
                    TargetContainerCode = pkgReq.TargetContainerCode
                });
            }
        }

        if (request.Items != null)
        {
            foreach (var itemReq in request.Items)
            {
                dispatch.DispatchItems.Add(new WarehouseDispatchItem
                {
                    ProductId = itemReq.ProductId,
                    RequestedQuantity = itemReq.RequestedQuantity,
                    PickedQuantity = itemReq.PickedQuantity,
                    UnitOfMeasureId = itemReq.UnitOfMeasureId,
                    SourceLocationId = itemReq.SourceLocationId,
                    LotNumber = itemReq.LotNumber,
                    Notes = itemReq.Notes
                });
            }
        }

        _context.WarehouseDispatches.Add(dispatch);
        await _context.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync("CreateDispatch", "warehouse", "WarehouseDispatches", dispatch.Id.ToString(),
            null, new { dispatch.DispatchNumber, dispatch.DispatchType, PackageCount = dispatch.DispatchPackages.Count },
            notes: null, cancellationToken: cancellationToken);

        return (await GetDispatchByIdAsync(dispatch.Id, cancellationToken))!;
    }

    public async Task<WarehouseDispatchDto> ConfirmDispatchAsync(long dispatchId, long employeeId, CancellationToken cancellationToken = default)
    {
        var dispatch = await _context.WarehouseDispatches
            .Include(d => d.Warehouse)
            .Include(d => d.DispatchPackages)
                .ThenInclude(dp => dp.ShipmentPackage)
                    .ThenInclude(p => p.Shipment)
            .Include(d => d.DispatchItems)
            .FirstOrDefaultAsync(d => d.Id == dispatchId, cancellationToken)
            ?? throw new NotFoundException("WarehouseDispatch", dispatchId);
        EnsureWarehouseAccess(dispatch.Warehouse.BranchId);

        if (dispatch.Status == "Dispatched")
            throw new ConflictException("Dispatch is already dispatched.");

        var ownsTx = _context.Database.IsRelational() && _context.Database.CurrentTransaction == null;
        await using var tx = ownsTx ? await _context.Database.BeginTransactionAsync(cancellationToken) : null;

        dispatch.Status = "Dispatched";
        dispatch.DispatchedAt = DateTime.UtcNow;
        dispatch.UpdatedAt = DateTime.UtcNow;
        dispatch.UpdatedBy = _currentUser.Username;

        // Deduct physical inventory if DispatchItems are present
        if (dispatch.DispatchItems.Count > 0)
        {
            var txnNumber = await _numberSequence.GetNextNumberAsync("INV-TXN", cancellationToken);
            var txn = new InventoryTransaction
            {
                TransactionNumber = txnNumber,
                WarehouseId = dispatch.WarehouseId,
                TransactionType = "Dispatch",
                ReferenceType = "WarehouseDispatch",
                ReferenceId = dispatch.Id,
                ReferenceNumber = dispatch.DispatchNumber,
                PerformedByEmployeeId = employeeId,
                TransactionDate = DateTime.UtcNow,
                Notes = $"Inventory dispatch confirmation {dispatch.DispatchNumber}",
                CreatedBy = _currentUser.Username
            };

            foreach (var item in dispatch.DispatchItems)
            {
                var qtyToDeduct = item.PickedQuantity > 0 ? item.PickedQuantity : item.RequestedQuantity;
                if (qtyToDeduct <= 0) continue;

                if (item.SourceLocationId <= 0)
                    throw new ConflictException($"Dispatch item for product #{item.ProductId} requires SourceLocationId to deduct inventory.");
                var locId = item.SourceLocationId;

                long? lotId = null;
                if (!string.IsNullOrWhiteSpace(item.LotNumber))
                {
                    lotId = await _context.InventoryLots.AsNoTracking()
                        .Where(l => l.ProductId == item.ProductId && l.LotNumber == item.LotNumber)
                        .Select(l => (long?)l.Id)
                        .FirstOrDefaultAsync(cancellationToken);
                }

                var balance = await _context.InventoryBalances
                    .FirstOrDefaultAsync(b => b.WarehouseId == dispatch.WarehouseId
                        && b.WarehouseLocationId == locId
                        && b.ProductId == item.ProductId
                        && b.LotId == lotId, cancellationToken)
                    ?? throw new ConflictException($"No inventory balance found at warehouse {dispatch.WarehouseId}, location {locId}, product {item.ProductId}.");

                var available = balance.QuantityOnHand - balance.QuantityAllocated;
                if (available < qtyToDeduct)
                {
                    throw new ConflictException(
                        $"Insufficient available inventory for product #{item.ProductId} at location #{locId}. Available={available}, Requested={qtyToDeduct}.");
                }

                var balanceBefore = balance.QuantityOnHand;
                balance.QuantityOnHand -= qtyToDeduct;

                txn.TransactionItems.Add(new InventoryTransactionItem
                {
                    ProductId = item.ProductId,
                    LocationId = locId,
                    LotId = lotId,
                    Quantity = qtyToDeduct,
                    BalanceBefore = balanceBefore,
                    BalanceAfter = balance.QuantityOnHand
                });
            }

            if (txn.TransactionItems.Count > 0)
                _context.InventoryTransactions.Add(txn);
        }

        // For parcel packages: update status & record handling events & transition shipment status
        foreach (var dp in dispatch.DispatchPackages)
        {
            var pkg = dp.ShipmentPackage;
            pkg.PackageStatus = "InTransit";
            pkg.UpdatedAt = DateTime.UtcNow;
            pkg.UpdatedBy = _currentUser.Username;

            _context.ParcelHandlingEvents.Add(new ParcelHandlingEvent
            {
                ShipmentPackageId = pkg.Id,
                ShipmentId = pkg.ShipmentId,
                WarehouseId = dispatch.WarehouseId,
                EventType = "Dispatched",
                NextDestinationWarehouseId = dispatch.DestinationWarehouseId,
                EmployeeId = employeeId,
                ScannedAt = DateTime.UtcNow,
                Notes = $"Dispatched via {dispatch.DispatchNumber}"
            });

            // If shipment is in sorting hub, transition to InTransit atomically
            if (pkg.Shipment.ShipmentStatus == "InSortingHub")
            {
                await _shipmentService.TransitionStatusAsync(pkg.Shipment.Id.ToString(), new ShipmentStatusTransitionRequest
                {
                    TargetStatus = "InTransit",
                    LocationWarehouseId = dispatch.WarehouseId,
                    Notes = $"Dispatched from {dispatch.Warehouse.Code}"
                }, cancellationToken);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        if (tx != null)
            await tx.CommitAsync(cancellationToken);

        await _audit.RecordAsync("ConfirmDispatch", "warehouse", "WarehouseDispatches", dispatch.Id.ToString(),
            new { Status = "Draft" }, new { Status = "Dispatched", dispatch.DispatchedAt },
            notes: null, cancellationToken: cancellationToken);

        return (await GetDispatchByIdAsync(dispatch.Id, cancellationToken))!;
    }

    private async Task<Domain.Entities.Shipping.ShipmentPackage?> FindPackageAsync(string identifier, CancellationToken ct)
    {
        return await _context.ShipmentPackages
            .Include(p => p.Shipment)
            .FirstOrDefaultAsync(p => p.Barcode == identifier || p.PackageCode == identifier || p.Shipment.TrackingNumber == identifier, ct);
    }

    private static ParcelHandlingEventDto MapToEventDto(ParcelHandlingEvent e, Domain.Entities.Shipping.ShipmentPackage p, Domain.Entities.Shipping.Shipment s, Domain.Entities.Organization.Warehouse w) => new()
    {
        Id = e.Id,
        ShipmentPackageId = e.ShipmentPackageId,
        PackageCode = p.PackageCode,
        Barcode = p.Barcode,
        ShipmentId = e.ShipmentId,
        ShipmentTrackingNumber = s.TrackingNumber,
        WarehouseId = e.WarehouseId,
        WarehouseCode = w.Code,
        EventType = e.EventType,
        BagNumber = e.BagNumber,
        NextDestinationWarehouseId = e.NextDestinationWarehouseId,
        EmployeeId = e.EmployeeId,
        EmployeeName = e.Employee?.FullName,
        ScannedAt = e.ScannedAt,
        DeviceId = e.DeviceId,
        Notes = e.Notes
    };

    private static WarehouseDispatchDto MapToDispatchDto(WarehouseDispatch d) => new()
    {
        Id = d.Id,
        PublicId = d.PublicId,
        DispatchNumber = d.DispatchNumber,
        WarehouseId = d.WarehouseId,
        WarehouseCode = d.Warehouse.Code,
        DispatchType = d.DispatchType,
        DestinationWarehouseId = d.DestinationWarehouseId,
        DestinationWarehouseCode = d.DestinationWarehouse?.Code,
        DestinationBranchId = d.DestinationBranchId,
        AssignedCarrierId = d.AssignedCarrierId,
        AssignedCarrierCode = d.AssignedCarrier?.Code,
        Status = d.Status,
        DispatchedByEmployeeId = d.DispatchedByEmployeeId,
        DispatchedByEmployeeName = d.DispatchedByEmployee?.FullName,
        DispatchedAt = d.DispatchedAt,
        VehicleNumber = d.VehicleNumber,
        Notes = d.Notes,
        Items = d.DispatchItems.Select(di => new WarehouseDispatchItemDto
        {
            Id = di.Id,
            ProductId = di.ProductId,
            ProductCode = di.Product.Sku,
            ProductName = di.Product.Name,
            RequestedQuantity = di.RequestedQuantity,
            PickedQuantity = di.PickedQuantity,
            UnitOfMeasureId = di.UnitOfMeasureId,
            SourceLocationId = di.SourceLocationId,
            SourceLocationCode = di.SourceLocation?.Code ?? string.Empty,
            LotNumber = di.LotNumber,
            Notes = di.Notes
        }).ToList(),
        Packages = d.DispatchPackages.Select(dp => new WarehouseDispatchPackageDto
        {
            Id = dp.Id,
            ShipmentPackageId = dp.ShipmentPackageId,
            PackageCode = dp.ShipmentPackage.PackageCode,
            Barcode = dp.Barcode,
            ScannedAt = dp.ScannedAt,
            ScannedByEmployeeId = dp.ScannedByEmployeeId,
            ScannedByEmployeeName = dp.ScannedByEmployee?.FullName,
            TargetContainerCode = dp.TargetContainerCode
        }).ToList()
    };
}
