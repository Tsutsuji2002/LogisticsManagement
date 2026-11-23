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
/// Endpoints for dispatch, driver, courier, and fleet assignments for shipments.
/// </summary>
[Route("api/v1/shipments/{shipmentId}/assignments")]
public class ShipmentAssignmentsController : BaseApiController
{
    private readonly IShipmentAssignmentService _assignmentService;
    private readonly IValidator<CreateShipmentAssignmentRequest> _createValidator;
    private readonly IValidator<UpdateShipmentAssignmentRequest> _updateValidator;

    public ShipmentAssignmentsController(
        IShipmentAssignmentService assignmentService,
        IValidator<CreateShipmentAssignmentRequest> createValidator,
        IValidator<UpdateShipmentAssignmentRequest> updateValidator)
    {
        _assignmentService = assignmentService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// Lists all operational assignments for a shipment.
    /// </summary>
    [HttpGet]
    [HasPermission(AppPermissions.Shipping.ShipmentsRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ShipmentAssignmentDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAssignments(string shipmentId, CancellationToken cancellationToken)
    {
        var assignments = await _assignmentService.GetAssignmentsAsync(shipmentId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ShipmentAssignmentDto>>.Ok(assignments));
    }

    /// <summary>
    /// Retrieves an assignment by ID.
    /// </summary>
    [HttpGet("{assignmentId:long}")]
    [HasPermission(AppPermissions.Shipping.ShipmentsRead)]
    [ProducesResponseType(typeof(ApiResponse<ShipmentAssignmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAssignment(string shipmentId, long assignmentId, CancellationToken cancellationToken)
    {
        var assignment = await _assignmentService.GetAssignmentByIdAsync(shipmentId, assignmentId, cancellationToken);
        if (assignment == null)
        {
            return NotFound(ApiResponse<ShipmentAssignmentDto>.Fail($"Assignment '{assignmentId}' was not found."));
        }

        return Ok(ApiResponse<ShipmentAssignmentDto>.Ok(assignment));
    }

    /// <summary>
    /// Creates a new courier or fleet assignment for a shipment.
    /// </summary>
    [HttpPost]
    [HasPermission(AppPermissions.Shipping.ShipmentsAssign)]
    [ProducesResponseType(typeof(ApiResponse<ShipmentAssignmentDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateAssignment(string shipmentId, [FromBody] CreateShipmentAssignmentRequest request, CancellationToken cancellationToken)
    {
        var valResult = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!valResult.IsValid)
        {
            throw new ValidationException(valResult.ToDictionary());
        }

        var assignment = await _assignmentService.CreateAssignmentAsync(shipmentId, request, cancellationToken);
        return CreatedAtAction(nameof(GetAssignment), new { shipmentId, assignmentId = assignment.Id }, ApiResponse<ShipmentAssignmentDto>.Ok(assignment, "Assignment created successfully."));
    }

    /// <summary>
    /// Updates status or details of an existing assignment.
    /// </summary>
    [HttpPut("{assignmentId:long}")]
    [HasPermission(AppPermissions.Shipping.ShipmentsAssign)]
    [ProducesResponseType(typeof(ApiResponse<ShipmentAssignmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateAssignment(string shipmentId, long assignmentId, [FromBody] UpdateShipmentAssignmentRequest request, CancellationToken cancellationToken)
    {
        var valResult = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!valResult.IsValid)
        {
            throw new ValidationException(valResult.ToDictionary());
        }

        var assignment = await _assignmentService.UpdateAssignmentAsync(shipmentId, assignmentId, request, cancellationToken);
        return Ok(ApiResponse<ShipmentAssignmentDto>.Ok(assignment, "Assignment updated successfully."));
    }
}
