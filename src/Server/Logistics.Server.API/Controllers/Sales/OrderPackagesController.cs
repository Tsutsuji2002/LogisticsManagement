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
/// Endpoints for managing parcels/packages within a sales order.
/// </summary>
[Route("api/v1/orders/{orderId}/packages")]
public class OrderPackagesController : BaseApiController
{
    private readonly IOrderPackageService _orderPackageService;
    private readonly IValidator<CreateOrderPackageRequest> _createValidator;
    private readonly IValidator<UpdateOrderPackageRequest> _updateValidator;

    public OrderPackagesController(
        IOrderPackageService orderPackageService,
        IValidator<CreateOrderPackageRequest> createValidator,
        IValidator<UpdateOrderPackageRequest> updateValidator)
    {
        _orderPackageService = orderPackageService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// Lists all packages registered under a sales order.
    /// </summary>
    [HttpGet]
    [HasPermission(AppPermissions.Sales.OrdersRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<OrderPackageDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPackages(string orderId, CancellationToken cancellationToken)
    {
        var packages = await _orderPackageService.GetPackagesAsync(orderId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<OrderPackageDto>>.Ok(packages));
    }

    /// <summary>
    /// Retrieves details of a specific package within a sales order.
    /// </summary>
    [HttpGet("{packageId}")]
    [HasPermission(AppPermissions.Sales.OrdersRead)]
    [ProducesResponseType(typeof(ApiResponse<OrderPackageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPackage(string orderId, string packageId, CancellationToken cancellationToken)
    {
        var package = await _orderPackageService.GetPackageByIdAsync(orderId, packageId, cancellationToken);
        if (package == null)
        {
            return NotFound(ApiResponse<OrderPackageDto>.Fail($"Order package '{packageId}' was not found."));
        }

        return Ok(ApiResponse<OrderPackageDto>.Ok(package));
    }

    /// <summary>
    /// Adds a package to an editable sales order (Draft or Confirmed) and recalculates totals.
    /// </summary>
    [HttpPost]
    [HasPermission(AppPermissions.Sales.OrdersUpdate)]
    [ProducesResponseType(typeof(ApiResponse<OrderPackageDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddPackage(string orderId, [FromBody] CreateOrderPackageRequest request, CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        var package = await _orderPackageService.AddPackageAsync(orderId, request, cancellationToken);
        return CreatedAtAction(nameof(GetPackage), new { orderId, packageId = package.Id }, ApiResponse<OrderPackageDto>.Ok(package, "Order package added successfully."));
    }

    /// <summary>
    /// Updates package weight, dimensions, barcode, or declared value in an editable order.
    /// </summary>
    [HttpPut("{packageId}")]
    [HasPermission(AppPermissions.Sales.OrdersUpdate)]
    [ProducesResponseType(typeof(ApiResponse<OrderPackageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePackage(string orderId, string packageId, [FromBody] UpdateOrderPackageRequest request, CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        var package = await _orderPackageService.UpdatePackageAsync(orderId, packageId, request, cancellationToken);
        return Ok(ApiResponse<OrderPackageDto>.Ok(package, "Order package updated successfully."));
    }

    /// <summary>
    /// Removes a package from an editable sales order and recalculates order aggregates.
    /// </summary>
    [HttpDelete("{packageId}")]
    [HasPermission(AppPermissions.Sales.OrdersUpdate)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemovePackage(string orderId, string packageId, CancellationToken cancellationToken)
    {
        await _orderPackageService.RemovePackageAsync(orderId, packageId, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }, "Order package deleted successfully."));
    }
}
