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
/// Endpoints for recording and querying last-mile delivery attempts.
/// </summary>
[Route("api/v1/shipments/{shipmentId}/delivery-attempts")]
public class DeliveryAttemptsController : BaseApiController
{
    private readonly IDeliveryAttemptService _attemptService;
    private readonly IValidator<RecordDeliveryAttemptRequest> _recordValidator;

    public DeliveryAttemptsController(
        IDeliveryAttemptService attemptService,
        IValidator<RecordDeliveryAttemptRequest> recordValidator)
    {
        _attemptService = attemptService;
        _recordValidator = recordValidator;
    }

    /// <summary>
    /// Lists all delivery attempts for a shipment.
    /// </summary>
    [HttpGet]
    [HasPermission(AppPermissions.Shipping.ShipmentsRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DeliveryAttemptDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAttempts(string shipmentId, CancellationToken cancellationToken)
    {
        var attempts = await _attemptService.GetAttemptsAsync(shipmentId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<DeliveryAttemptDto>>.Ok(attempts));
    }

    /// <summary>
    /// Retrieves a delivery attempt by ID.
    /// </summary>
    [HttpGet("{attemptId:long}")]
    [HasPermission(AppPermissions.Shipping.ShipmentsRead)]
    [ProducesResponseType(typeof(ApiResponse<DeliveryAttemptDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAttempt(string shipmentId, long attemptId, CancellationToken cancellationToken)
    {
        var attempt = await _attemptService.GetAttemptByIdAsync(shipmentId, attemptId, cancellationToken);
        if (attempt == null)
        {
            return NotFound(ApiResponse<DeliveryAttemptDto>.Fail($"Delivery attempt '{attemptId}' was not found."));
        }

        return Ok(ApiResponse<DeliveryAttemptDto>.Ok(attempt));
    }

    /// <summary>
    /// Records a new delivery attempt (successful, failed, or rescheduled).
    /// </summary>
    [HttpPost]
    [HasPermission(AppPermissions.Shipping.ShipmentsDeliver)]
    [ProducesResponseType(typeof(ApiResponse<DeliveryAttemptDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RecordAttempt(string shipmentId, [FromBody] RecordDeliveryAttemptRequest request, CancellationToken cancellationToken)
    {
        var valResult = await _recordValidator.ValidateAsync(request, cancellationToken);
        if (!valResult.IsValid)
        {
            throw new ValidationException(valResult.ToDictionary());
        }

        var attempt = await _attemptService.RecordAttemptAsync(shipmentId, request, cancellationToken);
        return CreatedAtAction(nameof(GetAttempt), new { shipmentId, attemptId = attempt.Id }, ApiResponse<DeliveryAttemptDto>.Ok(attempt, "Delivery attempt recorded successfully."));
    }
}
