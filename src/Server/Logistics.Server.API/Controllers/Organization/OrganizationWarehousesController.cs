using Logistics.Server.API.Authorization;
using Logistics.Server.Application.Common.Exceptions;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Organization.DTOs;
using Logistics.Server.Application.Modules.Organization.Services;
using Logistics.Shared.Contracts;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace Logistics.Server.API.Controllers.Organization;

/// <summary>
/// Organization-level warehouse and sorting hub information endpoints.
/// </summary>
[Route("api/v1/organization/warehouses")]
public class OrganizationWarehousesController : BaseApiController
{
    private readonly IWarehouseService _warehouseService;

    public OrganizationWarehousesController(IWarehouseService warehouseService)
    {
        _warehouseService = warehouseService;
    }

    /// <summary>
    /// Gets a paginated list of warehouses scoped by branch authorization.
    /// </summary>
    [HttpGet]
    [HasPermission(AppPermissions.Organization.WarehousesRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<WarehouseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWarehouses(
        [FromQuery] string? search,
        [FromQuery] long? branchId,
        [FromQuery] string? type,
        [FromQuery] bool? isActive,
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _warehouseService.GetWarehousesAsync(search, branchId, type, isActive, request, cancellationToken);
        return Ok(ApiResponse<PagedResponse<WarehouseDto>>.Ok(result.ToPagedResponse()));
    }

    /// <summary>
    /// Gets warehouse details including operational storage zones.
    /// </summary>
    [HttpGet("{id}")]
    [HasPermission(AppPermissions.Organization.WarehousesRead)]
    [ProducesResponseType(typeof(ApiResponse<WarehouseDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWarehouse(string id, CancellationToken cancellationToken)
    {
        var warehouse = await _warehouseService.GetByIdOrPublicIdAsync(id, cancellationToken);
        if (warehouse == null)
        {
            throw new NotFoundException("Warehouse", id);
        }
        return Ok(ApiResponse<WarehouseDetailDto>.Ok(warehouse));
    }
}
