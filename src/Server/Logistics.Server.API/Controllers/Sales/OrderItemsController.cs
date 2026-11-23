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
/// Endpoints for managing items within a sales order.
/// </summary>
[Route("api/v1/orders/{orderId}/items")]
public class OrderItemsController : BaseApiController
{
    private readonly IOrderItemService _orderItemService;
    private readonly IValidator<CreateOrderItemRequest> _createValidator;
    private readonly IValidator<UpdateOrderItemRequest> _updateValidator;

    public OrderItemsController(
        IOrderItemService orderItemService,
        IValidator<CreateOrderItemRequest> createValidator,
        IValidator<UpdateOrderItemRequest> updateValidator)
    {
        _orderItemService = orderItemService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// Lists all items contained in a sales order.
    /// </summary>
    [HttpGet]
    [HasPermission(AppPermissions.Sales.OrdersRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<OrderItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetItems(string orderId, CancellationToken cancellationToken)
    {
        var items = await _orderItemService.GetItemsAsync(orderId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<OrderItemDto>>.Ok(items));
    }

    /// <summary>
    /// Retrieves a specific order item by ID or public GUID.
    /// </summary>
    [HttpGet("{itemId}")]
    [HasPermission(AppPermissions.Sales.OrdersRead)]
    [ProducesResponseType(typeof(ApiResponse<OrderItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetItem(string orderId, string itemId, CancellationToken cancellationToken)
    {
        var item = await _orderItemService.GetItemByIdAsync(orderId, itemId, cancellationToken);
        if (item == null)
        {
            return NotFound(ApiResponse<OrderItemDto>.Fail($"Order item '{itemId}' was not found."));
        }

        return Ok(ApiResponse<OrderItemDto>.Ok(item));
    }

    /// <summary>
    /// Adds a product line item to a Draft sales order and recalculates order aggregates.
    /// </summary>
    [HttpPost]
    [HasPermission(AppPermissions.Sales.OrdersUpdate)]
    [ProducesResponseType(typeof(ApiResponse<OrderItemDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddItem(string orderId, [FromBody] CreateOrderItemRequest request, CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        var item = await _orderItemService.AddItemAsync(orderId, request, cancellationToken);
        return CreatedAtAction(nameof(GetItem), new { orderId, itemId = item.Id }, ApiResponse<OrderItemDto>.Ok(item, "Order item added successfully."));
    }

    /// <summary>
    /// Updates quantity, price, or notes of an order item in a Draft sales order.
    /// </summary>
    [HttpPut("{itemId}")]
    [HasPermission(AppPermissions.Sales.OrdersUpdate)]
    [ProducesResponseType(typeof(ApiResponse<OrderItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateItem(string orderId, string itemId, [FromBody] UpdateOrderItemRequest request, CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        var item = await _orderItemService.UpdateItemAsync(orderId, itemId, request, cancellationToken);
        return Ok(ApiResponse<OrderItemDto>.Ok(item, "Order item updated successfully."));
    }

    /// <summary>
    /// Removes an item from a Draft sales order and recalculates aggregates.
    /// </summary>
    [HttpDelete("{itemId}")]
    [HasPermission(AppPermissions.Sales.OrdersUpdate)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveItem(string orderId, string itemId, CancellationToken cancellationToken)
    {
        await _orderItemService.RemoveItemAsync(orderId, itemId, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }, "Order item deleted successfully."));
    }
}
