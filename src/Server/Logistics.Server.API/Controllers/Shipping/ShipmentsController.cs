using FluentValidation;
using Logistics.Server.API.Authorization;
using Logistics.Server.Application.Common.Exceptions;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Shipping.DTOs;
using Logistics.Server.Application.Modules.Shipping.Services;
using Logistics.Shared.Contracts;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.AspNetCore.Mvc;
using ValidationException = Logistics.Server.Application.Common.Exceptions.ValidationException;

namespace Logistics.Server.API.Controllers.Shipping;

/// <summary>
/// Endpoints for shipment creation, querying, updates, status transitions, and cancellation.
/// </summary>
[Route("api/v1/shipments")]
public class ShipmentsController : BaseApiController
{
    private readonly IShipmentService _shipmentService;
    private readonly IValidator<CreateShipmentFromOrderRequest> _createValidator;
    private readonly IValidator<UpdateShipmentRequest> _updateValidator;
    private readonly IValidator<CancelShipmentRequest> _cancelValidator;
    private readonly IValidator<ShipmentStatusTransitionRequest> _transitionValidator;

    public ShipmentsController(
        IShipmentService shipmentService,
        IValidator<CreateShipmentFromOrderRequest> createValidator,
        IValidator<UpdateShipmentRequest> updateValidator,
        IValidator<CancelShipmentRequest> cancelValidator,
        IValidator<ShipmentStatusTransitionRequest> transitionValidator)
    {
        _shipmentService = shipmentService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _cancelValidator = cancelValidator;
        _transitionValidator = transitionValidator;
    }

    /// <summary>
    /// Searches and filters shipments across accessible branches with pagination.
    /// </summary>
    [HttpGet]
    [HasPermission(AppPermissions.Shipping.ShipmentsRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<ShipmentDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetShipments(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] string? serviceType,
        [FromQuery] long? customerId,
        [FromQuery] long? originBranchId,
        [FromQuery] long? destinationBranchId,
        [FromQuery] long? carrierId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _shipmentService.GetShipmentsAsync(
            search, status, serviceType, customerId, originBranchId,
            destinationBranchId, carrierId, dateFrom, dateTo, request, cancellationToken);
        return Ok(ApiResponse<PagedResponse<ShipmentDto>>.Ok(result.ToPagedResponse()));
    }

    /// <summary>
    /// Retrieves shipment details by ID, PublicId GUID, or TrackingNumber.
    /// </summary>
    [HttpGet("{id}")]
    [HasPermission(AppPermissions.Shipping.ShipmentsRead)]
    [ProducesResponseType(typeof(ApiResponse<ShipmentDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetShipment(string id, CancellationToken cancellationToken)
    {
        var shipment = await _shipmentService.GetByIdOrTrackingAsync(id, cancellationToken);
        if (shipment == null)
        {
            return NotFound(ApiResponse<ShipmentDetailDto>.Fail($"Shipment '{id}' was not found."));
        }

        return Ok(ApiResponse<ShipmentDetailDto>.Ok(shipment));
    }

    /// <summary>
    /// Retrieves shipment details by tracking number.
    /// </summary>
    [HttpGet("number/{shipmentNumber}")]
    [HasPermission(AppPermissions.Shipping.ShipmentsRead)]
    [ProducesResponseType(typeof(ApiResponse<ShipmentDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByNumber(string shipmentNumber, CancellationToken cancellationToken)
    {
        var shipment = await _shipmentService.GetByIdOrTrackingAsync(shipmentNumber, cancellationToken);
        if (shipment == null)
        {
            return NotFound(ApiResponse<ShipmentDetailDto>.Fail($"Shipment '{shipmentNumber}' was not found."));
        }

        return Ok(ApiResponse<ShipmentDetailDto>.Ok(shipment));
    }

    /// <summary>
    /// Retrieves shipments created from a specific order.
    /// </summary>
    [HttpGet("by-order/{orderId}")]
    [HasPermission(AppPermissions.Shipping.ShipmentsRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ShipmentDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByOrder(long orderId, CancellationToken cancellationToken)
    {
        var shipments = await _shipmentService.GetByOrderIdAsync(orderId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ShipmentDto>>.Ok(shipments));
    }

    /// <summary>
    /// Creates a new shipment from an eligible confirmed sales order.
    /// </summary>
    [HttpPost("from-order")]
    [HasPermission(AppPermissions.Shipping.ShipmentsCreate)]
    [ProducesResponseType(typeof(ApiResponse<ShipmentDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateFromOrder([FromBody] CreateShipmentFromOrderRequest request, CancellationToken cancellationToken)
    {
        var valResult = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!valResult.IsValid)
        {
            throw new ValidationException(valResult.ToDictionary());
        }

        var shipment = await _shipmentService.CreateFromOrderAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetShipment), new { id = shipment.TrackingNumber }, ApiResponse<ShipmentDetailDto>.Ok(shipment, "Shipment created successfully."));
    }

    /// <summary>
    /// Updates editable properties of a Draft or ReadyForPickup shipment.
    /// </summary>
    [HttpPut("{id}")]
    [HasPermission(AppPermissions.Shipping.ShipmentsUpdate)]
    [ProducesResponseType(typeof(ApiResponse<ShipmentDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateShipment(string id, [FromBody] UpdateShipmentRequest request, CancellationToken cancellationToken)
    {
        var valResult = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!valResult.IsValid)
        {
            throw new ValidationException(valResult.ToDictionary());
        }

        var shipment = await _shipmentService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<ShipmentDetailDto>.Ok(shipment, "Shipment updated successfully."));
    }

    /// <summary>
    /// Cancels a shipment if allowed by its lifecycle state.
    /// </summary>
    [HttpPost("{id}/cancel")]
    [HasPermission(AppPermissions.Shipping.ShipmentsCancel)]
    [ProducesResponseType(typeof(ApiResponse<ShipmentDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelShipment(string id, [FromBody] CancelShipmentRequest request, CancellationToken cancellationToken)
    {
        var valResult = await _cancelValidator.ValidateAsync(request, cancellationToken);
        if (!valResult.IsValid)
        {
            throw new ValidationException(valResult.ToDictionary());
        }

        var shipment = await _shipmentService.CancelAsync(id, request, cancellationToken);
        return Ok(ApiResponse<ShipmentDetailDto>.Ok(shipment, "Shipment cancelled successfully."));
    }

    /// <summary>
    /// Executes a validated status transition for a shipment (e.g. ReadyForPickup -> PickedUp -> InTransit -> OutForDelivery).
    /// </summary>
    [HttpPost("{id}/transitions")]
    [HasPermission(AppPermissions.Shipping.ShipmentsManageStatus)]
    [ProducesResponseType(typeof(ApiResponse<ShipmentDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> TransitionStatus(string id, [FromBody] ShipmentStatusTransitionRequest request, CancellationToken cancellationToken)
    {
        var valResult = await _transitionValidator.ValidateAsync(request, cancellationToken);
        if (!valResult.IsValid)
        {
            throw new ValidationException(valResult.ToDictionary());
        }

        var shipment = await _shipmentService.TransitionStatusAsync(id, request, cancellationToken);
        return Ok(ApiResponse<ShipmentDetailDto>.Ok(shipment, $"Shipment transitioned to {request.TargetStatus} successfully."));
    }

    /// <summary>
    /// Retrieves immutable status history for a shipment.
    /// </summary>
    [HttpGet("{id}/history")]
    [HasPermission(AppPermissions.Shipping.TrackingRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ShipmentStatusHistoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatusHistory(string id, CancellationToken cancellationToken)
    {
        var histories = await _shipmentService.GetStatusHistoryAsync(id, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ShipmentStatusHistoryDto>>.Ok(histories));
    }
}
