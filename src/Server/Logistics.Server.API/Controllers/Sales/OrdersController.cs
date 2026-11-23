using FluentValidation;
using Logistics.Server.API.Authorization;
using Logistics.Server.Application.Common.Exceptions;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Sales.DTOs;
using Logistics.Server.Application.Modules.Sales.Services;
using Logistics.Shared.Contracts;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.AspNetCore.Mvc;
using ValidationException = Logistics.Server.Application.Common.Exceptions.ValidationException;

namespace Logistics.Server.API.Controllers.Sales;

/// <summary>
/// Endpoints for sales order creation, querying, confirmation, updates, and lifecycle management.
/// </summary>
[Route("api/v1/orders")]
public class OrdersController : BaseApiController
{
    private readonly IOrderService _orderService;
    private readonly IValidator<CreateOrderRequest> _createValidator;
    private readonly IValidator<UpdateOrderRequest> _updateValidator;
    private readonly IValidator<CancelOrderRequest> _cancelValidator;

    public OrdersController(
        IOrderService orderService,
        IValidator<CreateOrderRequest> createValidator,
        IValidator<UpdateOrderRequest> updateValidator,
        IValidator<CancelOrderRequest> cancelValidator)
    {
        _orderService = orderService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _cancelValidator = cancelValidator;
    }

    /// <summary>
    /// Searches and filters sales orders across accessible branches with pagination.
    /// </summary>
    [HttpGet]
    [HasPermission(AppPermissions.Sales.OrdersRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<OrderDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrders(
        [FromQuery] string? search,
        [FromQuery] string? orderStatus,
        [FromQuery] string? paymentStatus,
        [FromQuery] long? customerId,
        [FromQuery] long? branchId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _orderService.GetOrdersAsync(search, orderStatus, paymentStatus, customerId, branchId, dateFrom, dateTo, request, cancellationToken);
        return Ok(ApiResponse<PagedResponse<OrderDto>>.Ok(result.ToPagedResponse()));
    }

    /// <summary>
    /// Retrieves full order details including items, packages, and addresses by ID, public GUID, or OrderNumber.
    /// </summary>
    [HttpGet("{id}")]
    [HasPermission(AppPermissions.Sales.OrdersRead)]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrder(string id, CancellationToken cancellationToken)
    {
        var order = await _orderService.GetByIdOrPublicIdAsync(id, cancellationToken);
        if (order == null)
        {
            return NotFound(ApiResponse<OrderDetailDto>.Fail($"Order '{id}' was not found."));
        }

        return Ok(ApiResponse<OrderDetailDto>.Ok(order));
    }

    /// <summary>
    /// Creates a new sales order atomically in Draft status with items, packages, and addresses.
    /// </summary>
    [HttpPost]
    [HasPermission(AppPermissions.Sales.OrdersCreate)]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        var order = await _orderService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, ApiResponse<OrderDetailDto>.Ok(order, "Order created successfully."));
    }

    /// <summary>
    /// Updates general details, requested dates, and notes on an editable order.
    /// </summary>
    [HttpPut("{id}")]
    [HasPermission(AppPermissions.Sales.OrdersUpdate)]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOrder(string id, [FromBody] UpdateOrderRequest request, CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        var order = await _orderService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<OrderDetailDto>.Ok(order, "Order updated successfully."));
    }

    /// <summary>
    /// Confirms a Draft sales order, locking items and preparing it for fulfillment.
    /// </summary>
    [HttpPost("{id}/confirm")]
    [HasPermission(AppPermissions.Sales.OrdersUpdate)]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmOrder(string id, CancellationToken cancellationToken)
    {
        var order = await _orderService.ConfirmAsync(id, cancellationToken);
        return Ok(ApiResponse<OrderDetailDto>.Ok(order, "Order confirmed successfully."));
    }

    /// <summary>
    /// Cancels a sales order if no linked shipments have progressed to in-transit or delivered.
    /// </summary>
    [HttpPost("{id}/cancel")]
    [HasPermission(AppPermissions.Sales.OrdersCancel)]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelOrder(string id, [FromBody] CancelOrderRequest request, CancellationToken cancellationToken)
    {
        var validation = await _cancelValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        var order = await _orderService.CancelAsync(id, request, cancellationToken);
        return Ok(ApiResponse<OrderDetailDto>.Ok(order, "Order cancelled successfully."));
    }
}
