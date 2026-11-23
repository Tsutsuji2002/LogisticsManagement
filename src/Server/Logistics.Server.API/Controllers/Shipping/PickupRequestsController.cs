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
/// Endpoints for customer pickup request creation, driver assignment, arrival, and completion.
/// </summary>
[Route("api/v1/pickup-requests")]
public class PickupRequestsController : BaseApiController
{
    private readonly IPickupRequestService _pickupService;
    private readonly IValidator<CreatePickupRequestRequest> _createValidator;
    private readonly IValidator<UpdatePickupRequestRequest> _updateValidator;

    public PickupRequestsController(
        IPickupRequestService pickupService,
        IValidator<CreatePickupRequestRequest> createValidator,
        IValidator<UpdatePickupRequestRequest> updateValidator)
    {
        _pickupService = pickupService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// Searches and lists pickup requests with pagination.
    /// </summary>
    [HttpGet]
    [HasPermission(AppPermissions.Shipping.PickupsRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<PickupRequestDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPickupRequests(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] long? customerId,
        [FromQuery] long? assignedEmployeeId,
        [FromQuery] DateOnly? dateFrom,
        [FromQuery] DateOnly? dateTo,
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _pickupService.GetPickupRequestsAsync(
            search, status, customerId, assignedEmployeeId, dateFrom, dateTo, request, cancellationToken);
        return Ok(ApiResponse<PagedResponse<PickupRequestDto>>.Ok(result.ToPagedResponse()));
    }

    /// <summary>
    /// Retrieves a pickup request by ID or PickupCode.
    /// </summary>
    [HttpGet("{id}")]
    [HasPermission(AppPermissions.Shipping.PickupsRead)]
    [ProducesResponseType(typeof(ApiResponse<PickupRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPickupRequest(string id, CancellationToken cancellationToken)
    {
        var pr = await _pickupService.GetByIdOrCodeAsync(id, cancellationToken);
        if (pr == null)
        {
            return NotFound(ApiResponse<PickupRequestDto>.Fail($"Pickup request '{id}' was not found."));
        }

        return Ok(ApiResponse<PickupRequestDto>.Ok(pr));
    }

    /// <summary>
    /// Creates a new customer pickup request in Pending status.
    /// </summary>
    [HttpPost]
    [HasPermission(AppPermissions.Shipping.PickupsCreate)]
    [ProducesResponseType(typeof(ApiResponse<PickupRequestDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePickupRequest([FromBody] CreatePickupRequestRequest request, CancellationToken cancellationToken)
    {
        var valResult = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!valResult.IsValid)
        {
            throw new ValidationException(valResult.ToDictionary());
        }

        var pr = await _pickupService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetPickupRequest), new { id = pr.PickupCode }, ApiResponse<PickupRequestDto>.Ok(pr, "Pickup request created successfully."));
    }

    /// <summary>
    /// Updates details of an active pickup request.
    /// </summary>
    [HttpPut("{id}")]
    [HasPermission(AppPermissions.Shipping.PickupsManage)]
    [ProducesResponseType(typeof(ApiResponse<PickupRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdatePickupRequest(string id, [FromBody] UpdatePickupRequestRequest request, CancellationToken cancellationToken)
    {
        var valResult = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!valResult.IsValid)
        {
            throw new ValidationException(valResult.ToDictionary());
        }

        var pr = await _pickupService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<PickupRequestDto>.Ok(pr, "Pickup request updated successfully."));
    }

    /// <summary>
    /// Assigns a driver/courier employee to a pickup request.
    /// </summary>
    [HttpPost("{id}/assign/{employeeId}")]
    [HasPermission(AppPermissions.Shipping.PickupsManage)]
    [ProducesResponseType(typeof(ApiResponse<PickupRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AssignDriver(string id, long employeeId, CancellationToken cancellationToken)
    {
        var pr = await _pickupService.AssignDriverAsync(id, employeeId, cancellationToken);
        return Ok(ApiResponse<PickupRequestDto>.Ok(pr, "Driver assigned successfully."));
    }

    /// <summary>
    /// Marks that the assigned driver has arrived at the pickup location.
    /// </summary>
    [HttpPost("{id}/confirm")]
    [HasPermission(AppPermissions.Shipping.PickupsManage)]
    [ProducesResponseType(typeof(ApiResponse<PickupRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> MarkDriverArrived(string id, CancellationToken cancellationToken)
    {
        var pr = await _pickupService.MarkDriverArrivedAsync(id, cancellationToken);
        return Ok(ApiResponse<PickupRequestDto>.Ok(pr, "Driver marked as arrived."));
    }

    /// <summary>
    /// Completes the pickup request and records actual parcels collected.
    /// </summary>
    [HttpPost("{id}/complete")]
    [HasPermission(AppPermissions.Shipping.PickupsManage)]
    [ProducesResponseType(typeof(ApiResponse<PickupRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CompletePickup(string id, [FromBody] CompletePickupRequest request, CancellationToken cancellationToken)
    {
        var pr = await _pickupService.CompleteAsync(id, request, cancellationToken);
        return Ok(ApiResponse<PickupRequestDto>.Ok(pr, "Pickup completed successfully."));
    }

    /// <summary>
    /// Cancels a pickup request.
    /// </summary>
    [HttpPost("{id}/cancel")]
    [HasPermission(AppPermissions.Shipping.PickupsManage)]
    [ProducesResponseType(typeof(ApiResponse<PickupRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelPickup(string id, [FromQuery] string reason, CancellationToken cancellationToken)
    {
        var pr = await _pickupService.CancelAsync(id, reason, cancellationToken);
        return Ok(ApiResponse<PickupRequestDto>.Ok(pr, "Pickup request cancelled."));
    }
}
