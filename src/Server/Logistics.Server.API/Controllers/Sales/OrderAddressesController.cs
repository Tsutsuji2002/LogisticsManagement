using FluentValidation;
using Logistics.Server.API.Authorization;
using Logistics.Server.Application.Common.Exceptions;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Sales.DTOs;
using Logistics.Server.Application.Modules.Sales.Services;
using Logistics.Shared.Contracts;
using Microsoft.AspNetCore.Mvc;
using ValidationException = Logistics.Server.Application.Common.Exceptions.ValidationException;

namespace Logistics.Server.API.Controllers.Sales;

/// <summary>
/// Endpoints for managing origin (sender/pickup) and destination (recipient/delivery) address snapshots of a sales order.
/// </summary>
[Route("api/v1/orders/{orderId}/addresses")]
public class OrderAddressesController : BaseApiController
{
    private readonly IOrderAddressService _orderAddressService;
    private readonly IValidator<OrderAddressRequest> _addressValidator;

    public OrderAddressesController(
        IOrderAddressService orderAddressService,
        IValidator<OrderAddressRequest> addressValidator)
    {
        _orderAddressService = orderAddressService;
        _addressValidator = addressValidator;
    }

    /// <summary>
    /// Lists snapshot addresses (sender/pickup and recipient/delivery) associated with the order.
    /// </summary>
    [HttpGet]
    [HasPermission(AppPermissions.Sales.OrdersRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<OrderAddressDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAddresses(string orderId, CancellationToken cancellationToken)
    {
        var addresses = await _orderAddressService.GetAddressesAsync(orderId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<OrderAddressDto>>.Ok(addresses));
    }

    /// <summary>
    /// Retrieves a specific address by type ('Sender' / 'Pickup' or 'Recipient' / 'Delivery').
    /// </summary>
    [HttpGet("{addressType}")]
    [HasPermission(AppPermissions.Sales.OrdersRead)]
    [ProducesResponseType(typeof(ApiResponse<OrderAddressDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAddressByType(string orderId, string addressType, CancellationToken cancellationToken)
    {
        var address = await _orderAddressService.GetAddressByTypeAsync(orderId, addressType, cancellationToken);
        if (address == null)
        {
            return NotFound(ApiResponse<OrderAddressDto>.Fail($"Address of type '{addressType}' was not found for order '{orderId}'."));
        }

        return Ok(ApiResponse<OrderAddressDto>.Ok(address));
    }

    /// <summary>
    /// Sets or updates the specified address snapshot ('Sender' / 'Pickup' or 'Recipient' / 'Delivery') on an editable order.
    /// </summary>
    [HttpPut("{addressType}")]
    [HasPermission(AppPermissions.Sales.OrdersUpdate)]
    [ProducesResponseType(typeof(ApiResponse<OrderAddressDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetAddress(string orderId, string addressType, [FromBody] OrderAddressRequest request, CancellationToken cancellationToken)
    {
        var validation = await _addressValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        var address = await _orderAddressService.SetAddressAsync(orderId, addressType, request, cancellationToken);
        return Ok(ApiResponse<OrderAddressDto>.Ok(address, "Order address updated successfully."));
    }
}
