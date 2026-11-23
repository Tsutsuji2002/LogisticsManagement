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
/// Endpoints for managing snapshot addresses (Sender, Recipient, ReturnAddress) for a shipment.
/// </summary>
[Route("api/v1/shipments/{shipmentId}/addresses")]
public class ShipmentAddressesController : BaseApiController
{
    private readonly IShipmentAddressService _addressService;
    private readonly IValidator<UpdateShipmentAddressRequest> _updateValidator;

    public ShipmentAddressesController(
        IShipmentAddressService addressService,
        IValidator<UpdateShipmentAddressRequest> updateValidator)
    {
        _addressService = addressService;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// Lists all snapshot addresses for a shipment.
    /// </summary>
    [HttpGet]
    [HasPermission(AppPermissions.Shipping.ShipmentsRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ShipmentAddressDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAddresses(string shipmentId, CancellationToken cancellationToken)
    {
        var addresses = await _addressService.GetAddressesAsync(shipmentId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ShipmentAddressDto>>.Ok(addresses));
    }

    /// <summary>
    /// Retrieves a specific address by type (Sender, Recipient, ReturnAddress).
    /// </summary>
    [HttpGet("{addressType}")]
    [HasPermission(AppPermissions.Shipping.ShipmentsRead)]
    [ProducesResponseType(typeof(ApiResponse<ShipmentAddressDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAddressByType(string shipmentId, string addressType, CancellationToken cancellationToken)
    {
        var addr = await _addressService.GetAddressByTypeAsync(shipmentId, addressType, cancellationToken);
        if (addr == null)
        {
            return NotFound(ApiResponse<ShipmentAddressDto>.Fail($"Address type '{addressType}' was not found."));
        }

        return Ok(ApiResponse<ShipmentAddressDto>.Ok(addr));
    }

    /// <summary>
    /// Creates or updates a specific address by type (Sender, Recipient, ReturnAddress).
    /// </summary>
    [HttpPut("{addressType}")]
    [HasPermission(AppPermissions.Shipping.ShipmentsUpdate)]
    [ProducesResponseType(typeof(ApiResponse<ShipmentAddressDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SetAddress(string shipmentId, string addressType, [FromBody] UpdateShipmentAddressRequest request, CancellationToken cancellationToken)
    {
        var valResult = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!valResult.IsValid)
        {
            throw new ValidationException(valResult.ToDictionary());
        }

        var addr = await _addressService.SetAddressAsync(shipmentId, addressType, request, cancellationToken);
        return Ok(ApiResponse<ShipmentAddressDto>.Ok(addr, "Address updated successfully."));
    }
}
