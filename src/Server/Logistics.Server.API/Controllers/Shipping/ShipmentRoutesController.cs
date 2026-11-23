using FluentValidation;
using Logistics.Server.API.Authorization;
using Logistics.Server.Application.Common.Exceptions;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Shipping.DTOs;
using Logistics.Server.Application.Modules.Shipping.Services;
using Logistics.Shared.Contracts;
using Microsoft.AspNetCore.Mvc;
using ValidationException = Logistics.Server.Application.Common.Exceptions.ValidationException;

namespace Logistics.Server.API.Controllers.Shipping;

/// <summary>
/// Endpoints for managing leg-by-leg routing segments for a shipment.
/// </summary>
[Route("api/v1/shipments/{shipmentId}/routes")]
public class ShipmentRoutesController : BaseApiController
{
    private readonly IShipmentRouteService _routeService;
    private readonly IValidator<AddShipmentRouteRequest> _addValidator;
    private readonly IValidator<UpdateShipmentRouteRequest> _updateValidator;

    public ShipmentRoutesController(
        IShipmentRouteService routeService,
        IValidator<AddShipmentRouteRequest> addValidator,
        IValidator<UpdateShipmentRouteRequest> updateValidator)
    {
        _routeService = routeService;
        _addValidator = addValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// Lists all planned and completed route segments for a shipment.
    /// </summary>
    [HttpGet]
    [HasPermission(AppPermissions.Shipping.RoutesRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ShipmentRouteDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoutes(string shipmentId, CancellationToken cancellationToken)
    {
        var routes = await _routeService.GetRoutesAsync(shipmentId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ShipmentRouteDto>>.Ok(routes));
    }

    /// <summary>
    /// Retrieves a route segment by ID.
    /// </summary>
    [HttpGet("{routeId:long}")]
    [HasPermission(AppPermissions.Shipping.RoutesRead)]
    [ProducesResponseType(typeof(ApiResponse<ShipmentRouteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoute(string shipmentId, long routeId, CancellationToken cancellationToken)
    {
        var route = await _routeService.GetRouteByIdAsync(shipmentId, routeId, cancellationToken);
        if (route == null)
        {
            return NotFound(ApiResponse<ShipmentRouteDto>.Fail($"Route segment '{routeId}' was not found."));
        }

        return Ok(ApiResponse<ShipmentRouteDto>.Ok(route));
    }

    /// <summary>
    /// Adds a new routing leg to a shipment.
    /// </summary>
    [HttpPost]
    [HasPermission(AppPermissions.Shipping.RoutesManage)]
    [ProducesResponseType(typeof(ApiResponse<ShipmentRouteDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddRoute(string shipmentId, [FromBody] AddShipmentRouteRequest request, CancellationToken cancellationToken)
    {
        var valResult = await _addValidator.ValidateAsync(request, cancellationToken);
        if (!valResult.IsValid)
        {
            throw new ValidationException(valResult.ToDictionary());
        }

        var route = await _routeService.AddRouteAsync(shipmentId, request, cancellationToken);
        return CreatedAtAction(nameof(GetRoute), new { shipmentId, routeId = route.Id }, ApiResponse<ShipmentRouteDto>.Ok(route, "Route segment added successfully."));
    }

    /// <summary>
    /// Updates status or scheduling of a route segment.
    /// </summary>
    [HttpPut("{routeId:long}")]
    [HasPermission(AppPermissions.Shipping.RoutesManage)]
    [ProducesResponseType(typeof(ApiResponse<ShipmentRouteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateRoute(string shipmentId, long routeId, [FromBody] UpdateShipmentRouteRequest request, CancellationToken cancellationToken)
    {
        var valResult = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!valResult.IsValid)
        {
            throw new ValidationException(valResult.ToDictionary());
        }

        var route = await _routeService.UpdateRouteAsync(shipmentId, routeId, request, cancellationToken);
        return Ok(ApiResponse<ShipmentRouteDto>.Ok(route, "Route segment updated successfully."));
    }

    /// <summary>
    /// Removes a route segment from a shipment.
    /// </summary>
    [HttpDelete("{routeId:long}")]
    [HasPermission(AppPermissions.Shipping.RoutesManage)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteRoute(string shipmentId, long routeId, CancellationToken cancellationToken)
    {
        await _routeService.RemoveRouteAsync(shipmentId, routeId, cancellationToken);
        return Ok(ApiResponse<bool>.Ok(true, "Route segment removed successfully."));
    }
}
