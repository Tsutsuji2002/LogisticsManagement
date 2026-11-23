using FluentValidation;
using Logistics.Server.API.Authorization;
using Logistics.Server.Application.Common.Exceptions;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Warehouse.DTOs;
using Logistics.Server.Application.Modules.Warehouse.Services;
using Logistics.Shared.Contracts;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.AspNetCore.Mvc;
using ValidationException = Logistics.Server.Application.Common.Exceptions.ValidationException;

namespace Logistics.Server.API.Controllers.Warehouse;

/// <summary>
/// Operational management of warehouses, storage/sorting zones, and bin locations.
/// </summary>
[Route("api/v1/warehouses")]
public class WarehousesController : BaseApiController
{
    private readonly IWarehouseManagementService _warehouseService;
    private readonly IValidator<CreateWarehouseRequest> _createWhValidator;
    private readonly IValidator<UpdateWarehouseRequest> _updateWhValidator;
    private readonly IValidator<CreateWarehouseZoneRequest> _createZoneValidator;
    private readonly IValidator<UpdateWarehouseZoneRequest> _updateZoneValidator;
    private readonly IValidator<CreateWarehouseLocationRequest> _createLocationValidator;

    public WarehousesController(
        IWarehouseManagementService warehouseService,
        IValidator<CreateWarehouseRequest> createWhValidator,
        IValidator<UpdateWarehouseRequest> updateWhValidator,
        IValidator<CreateWarehouseZoneRequest> createZoneValidator,
        IValidator<UpdateWarehouseZoneRequest> updateZoneValidator,
        IValidator<CreateWarehouseLocationRequest> createLocationValidator)
    {
        _warehouseService = warehouseService;
        _createWhValidator = createWhValidator;
        _updateWhValidator = updateWhValidator;
        _createZoneValidator = createZoneValidator;
        _updateZoneValidator = updateZoneValidator;
        _createLocationValidator = createLocationValidator;
    }

    // ───────────── Warehouse Endpoints ─────────────

    [HttpGet]
    [HasPermission(AppPermissions.Warehouse.OperationsRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<WarehouseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWarehouses(
        [FromQuery] string? search,
        [FromQuery] long? branchId,
        [FromQuery] string? type,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _warehouseService.GetWarehousesAsync(search, branchId, type, isActive,
            new PagedRequest { Page = page, PageSize = pageSize }, cancellationToken);
        return Ok(ApiResponse<PagedResponse<WarehouseDto>>.Ok(result.ToPagedResponse()));
    }

    [HttpGet("{id}")]
    [HasPermission(AppPermissions.Warehouse.OperationsRead)]
    [ProducesResponseType(typeof(ApiResponse<WarehouseDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWarehouse(string id, CancellationToken cancellationToken)
    {
        var warehouse = await _warehouseService.GetWarehouseByIdOrCodeAsync(id, cancellationToken);
        if (warehouse == null) throw new NotFoundException("Warehouse", id);
        return Ok(ApiResponse<WarehouseDetailDto>.Ok(warehouse));
    }

    [HttpPost]
    [HasPermission(AppPermissions.Warehouse.WarehousesManage)]
    [ProducesResponseType(typeof(ApiResponse<WarehouseDetailDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateWarehouse([FromBody] CreateWarehouseRequest request, CancellationToken cancellationToken)
    {
        var val = await _createWhValidator.ValidateAsync(request, cancellationToken);
        if (!val.IsValid)
            throw new ValidationException(val.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

        var warehouse = await _warehouseService.CreateWarehouseAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetWarehouse), new { id = warehouse.Id }, ApiResponse<WarehouseDetailDto>.Ok(warehouse));
    }

    [HttpPut("{id}")]
    [HasPermission(AppPermissions.Warehouse.WarehousesManage)]
    [ProducesResponseType(typeof(ApiResponse<WarehouseDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateWarehouse(string id, [FromBody] UpdateWarehouseRequest request, CancellationToken cancellationToken)
    {
        var val = await _updateWhValidator.ValidateAsync(request, cancellationToken);
        if (!val.IsValid)
            throw new ValidationException(val.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

        var warehouse = await _warehouseService.UpdateWarehouseAsync(id, request, cancellationToken);
        return Ok(ApiResponse<WarehouseDetailDto>.Ok(warehouse));
    }

    [HttpPatch("{id}/status")]
    [HasPermission(AppPermissions.Warehouse.WarehousesManage)]
    [ProducesResponseType(typeof(ApiResponse<WarehouseDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ToggleWarehouseStatus(string id, [FromQuery] bool isActive, CancellationToken cancellationToken)
    {
        var warehouse = await _warehouseService.ToggleWarehouseStatusAsync(id, isActive, cancellationToken);
        return Ok(ApiResponse<WarehouseDetailDto>.Ok(warehouse));
    }

    // ───────────── Zone Endpoints ─────────────

    [HttpGet("{id}/zones")]
    [HasPermission(AppPermissions.Warehouse.OperationsRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<WarehouseZoneDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetZones(string id, CancellationToken cancellationToken)
    {
        var zones = await _warehouseService.GetZonesAsync(id, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<WarehouseZoneDto>>.Ok(zones));
    }

    [HttpGet("{id}/zones/{zoneId:long}")]
    [HasPermission(AppPermissions.Warehouse.OperationsRead)]
    [ProducesResponseType(typeof(ApiResponse<WarehouseZoneDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetZone(string id, long zoneId, CancellationToken cancellationToken)
    {
        var zone = await _warehouseService.GetZoneByIdAsync(id, zoneId, cancellationToken);
        if (zone == null) throw new NotFoundException("WarehouseZone", zoneId);
        return Ok(ApiResponse<WarehouseZoneDto>.Ok(zone));
    }

    [HttpPost("{id}/zones")]
    [HasPermission(AppPermissions.Warehouse.ZonesManage)]
    [ProducesResponseType(typeof(ApiResponse<WarehouseZoneDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateZone(string id, [FromBody] CreateWarehouseZoneRequest request, CancellationToken cancellationToken)
    {
        var val = await _createZoneValidator.ValidateAsync(request, cancellationToken);
        if (!val.IsValid)
            throw new ValidationException(val.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

        var zone = await _warehouseService.CreateZoneAsync(id, request, cancellationToken);
        return CreatedAtAction(nameof(GetZone), new { id, zoneId = zone.Id }, ApiResponse<WarehouseZoneDto>.Ok(zone));
    }

    [HttpPut("{id}/zones/{zoneId:long}")]
    [HasPermission(AppPermissions.Warehouse.ZonesManage)]
    [ProducesResponseType(typeof(ApiResponse<WarehouseZoneDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateZone(string id, long zoneId, [FromBody] UpdateWarehouseZoneRequest request, CancellationToken cancellationToken)
    {
        var val = await _updateZoneValidator.ValidateAsync(request, cancellationToken);
        if (!val.IsValid)
            throw new ValidationException(val.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

        var zone = await _warehouseService.UpdateZoneAsync(id, zoneId, request, cancellationToken);
        return Ok(ApiResponse<WarehouseZoneDto>.Ok(zone));
    }

    [HttpPatch("{id}/zones/{zoneId:long}/status")]
    [HasPermission(AppPermissions.Warehouse.ZonesManage)]
    [ProducesResponseType(typeof(ApiResponse<WarehouseZoneDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ToggleZoneStatus(string id, long zoneId, [FromQuery] bool isActive, CancellationToken cancellationToken)
    {
        var zone = await _warehouseService.ToggleZoneStatusAsync(id, zoneId, isActive, cancellationToken);
        return Ok(ApiResponse<WarehouseZoneDto>.Ok(zone));
    }

    // ───────────── Location Endpoints ─────────────

    [HttpGet("{id}/locations")]
    [HasPermission(AppPermissions.Warehouse.OperationsRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<WarehouseLocationDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLocations(
        string id,
        [FromQuery] long? zoneId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _warehouseService.GetLocationsAsync(id, zoneId,
            new PagedRequest { Page = page, PageSize = pageSize }, cancellationToken);
        return Ok(ApiResponse<PagedResponse<WarehouseLocationDto>>.Ok(result.ToPagedResponse()));
    }

    [HttpGet("{id}/locations/{locationId:long}")]
    [HasPermission(AppPermissions.Warehouse.OperationsRead)]
    [ProducesResponseType(typeof(ApiResponse<WarehouseLocationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLocation(string id, long locationId, CancellationToken cancellationToken)
    {
        var location = await _warehouseService.GetLocationByIdAsync(id, locationId, cancellationToken);
        if (location == null) throw new NotFoundException("WarehouseLocation", locationId);
        return Ok(ApiResponse<WarehouseLocationDto>.Ok(location));
    }

    [HttpPost("{id}/zones/{zoneId:long}/locations")]
    [HasPermission(AppPermissions.Warehouse.LocationsManage)]
    [ProducesResponseType(typeof(ApiResponse<WarehouseLocationDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateLocation(string id, long zoneId, [FromBody] CreateWarehouseLocationRequest request, CancellationToken cancellationToken)
    {
        var val = await _createLocationValidator.ValidateAsync(request, cancellationToken);
        if (!val.IsValid)
            throw new ValidationException(val.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

        var location = await _warehouseService.CreateLocationAsync(id, zoneId, request, cancellationToken);
        return CreatedAtAction(nameof(GetLocation), new { id, locationId = location.Id }, ApiResponse<WarehouseLocationDto>.Ok(location));
    }

    [HttpPut("{id}/locations/{locationId:long}")]
    [HasPermission(AppPermissions.Warehouse.LocationsManage)]
    [ProducesResponseType(typeof(ApiResponse<WarehouseLocationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateLocation(string id, long locationId, [FromBody] UpdateWarehouseLocationRequest request, CancellationToken cancellationToken)
    {
        var location = await _warehouseService.UpdateLocationAsync(id, locationId, request, cancellationToken);
        return Ok(ApiResponse<WarehouseLocationDto>.Ok(location));
    }

    [HttpPatch("{id}/locations/{locationId:long}/block")]
    [HasPermission(AppPermissions.Warehouse.LocationsManage)]
    [ProducesResponseType(typeof(ApiResponse<WarehouseLocationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ToggleLocationBlock(string id, long locationId, [FromQuery] bool isBlocked, CancellationToken cancellationToken)
    {
        var location = await _warehouseService.ToggleLocationBlockAsync(id, locationId, isBlocked, cancellationToken);
        return Ok(ApiResponse<WarehouseLocationDto>.Ok(location));
    }
}
