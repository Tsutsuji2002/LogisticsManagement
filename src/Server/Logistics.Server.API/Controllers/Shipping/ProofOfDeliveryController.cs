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
/// Endpoints for managing and recording Proof of Delivery (POD).
/// </summary>
[Route("api/v1/shipments/{shipmentId}/proof-of-delivery")]
public class ProofOfDeliveryController : BaseApiController
{
    private readonly IProofOfDeliveryService _podService;
    private readonly IValidator<CreateProofOfDeliveryRequest> _createValidator;

    public ProofOfDeliveryController(
        IProofOfDeliveryService podService,
        IValidator<CreateProofOfDeliveryRequest> createValidator)
    {
        _podService = podService;
        _createValidator = createValidator;
    }

    /// <summary>
    /// Retrieves Proof of Delivery for a delivered shipment.
    /// </summary>
    [HttpGet]
    [HasPermission(AppPermissions.Shipping.ShipmentsRead)]
    [ProducesResponseType(typeof(ApiResponse<ProofOfDeliveryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProofOfDelivery(string shipmentId, CancellationToken cancellationToken)
    {
        var pod = await _podService.GetByShipmentAsync(shipmentId, cancellationToken);
        if (pod == null)
        {
            return NotFound(ApiResponse<ProofOfDeliveryDto>.Fail("Proof of delivery not found for this shipment."));
        }

        return Ok(ApiResponse<ProofOfDeliveryDto>.Ok(pod));
    }

    /// <summary>
    /// Records Proof of Delivery (recipient signature, photo, and details) for a successful delivery attempt.
    /// </summary>
    [HttpPost]
    [HasPermission(AppPermissions.Shipping.ShipmentsDeliver)]
    [ProducesResponseType(typeof(ApiResponse<ProofOfDeliveryDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateProofOfDelivery(
        string shipmentId,
        [FromQuery] long attemptId,
        [FromBody] CreateProofOfDeliveryRequest request,
        CancellationToken cancellationToken)
    {
        var valResult = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!valResult.IsValid)
        {
            throw new ValidationException(valResult.ToDictionary());
        }

        var pod = await _podService.CreateAsync(shipmentId, attemptId, request, cancellationToken);
        return CreatedAtAction(nameof(GetProofOfDelivery), new { shipmentId }, ApiResponse<ProofOfDeliveryDto>.Ok(pod, "Proof of delivery recorded successfully."));
    }
}
