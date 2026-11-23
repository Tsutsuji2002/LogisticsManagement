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
/// Endpoints for managing physical packages assigned to a shipment.
/// </summary>
[Route("api/v1/shipments/{shipmentId}/packages")]
public class ShipmentPackagesController : BaseApiController
{
    private readonly IShipmentPackageService _packageService;
    private readonly IValidator<AddShipmentPackageRequest> _addValidator;
    private readonly IValidator<UpdateShipmentPackageRequest> _updateValidator;

    public ShipmentPackagesController(
        IShipmentPackageService packageService,
        IValidator<AddShipmentPackageRequest> addValidator,
        IValidator<UpdateShipmentPackageRequest> updateValidator)
    {
        _packageService = packageService;
        _addValidator = addValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// Lists all packages in a shipment.
    /// </summary>
    [HttpGet]
    [HasPermission(AppPermissions.Shipping.ShipmentsRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ShipmentPackageDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPackages(string shipmentId, CancellationToken cancellationToken)
    {
        var packages = await _packageService.GetPackagesAsync(shipmentId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ShipmentPackageDto>>.Ok(packages));
    }

    /// <summary>
    /// Retrieves a package by ID or package code.
    /// </summary>
    [HttpGet("{packageId}")]
    [HasPermission(AppPermissions.Shipping.ShipmentsRead)]
    [ProducesResponseType(typeof(ApiResponse<ShipmentPackageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPackage(string shipmentId, string packageId, CancellationToken cancellationToken)
    {
        var pkg = await _packageService.GetPackageByIdAsync(shipmentId, packageId, cancellationToken);
        if (pkg == null)
        {
            return NotFound(ApiResponse<ShipmentPackageDto>.Fail($"Package '{packageId}' not found."));
        }

        return Ok(ApiResponse<ShipmentPackageDto>.Ok(pkg));
    }

    /// <summary>
    /// Adds a new package to an editable shipment.
    /// </summary>
    [HttpPost]
    [HasPermission(AppPermissions.Shipping.ShipmentsUpdate)]
    [ProducesResponseType(typeof(ApiResponse<ShipmentPackageDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddPackage(string shipmentId, [FromBody] AddShipmentPackageRequest request, CancellationToken cancellationToken)
    {
        var valResult = await _addValidator.ValidateAsync(request, cancellationToken);
        if (!valResult.IsValid)
        {
            throw new ValidationException(valResult.ToDictionary());
        }

        var pkg = await _packageService.AddPackageAsync(shipmentId, request, cancellationToken);
        return CreatedAtAction(nameof(GetPackage), new { shipmentId, packageId = pkg.PackageCode }, ApiResponse<ShipmentPackageDto>.Ok(pkg, "Package added successfully."));
    }

    /// <summary>
    /// Updates package dimensions, weight, or status on an editable shipment.
    /// </summary>
    [HttpPut("{packageId}")]
    [HasPermission(AppPermissions.Shipping.ShipmentsUpdate)]
    [ProducesResponseType(typeof(ApiResponse<ShipmentPackageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdatePackage(string shipmentId, string packageId, [FromBody] UpdateShipmentPackageRequest request, CancellationToken cancellationToken)
    {
        var valResult = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!valResult.IsValid)
        {
            throw new ValidationException(valResult.ToDictionary());
        }

        var pkg = await _packageService.UpdatePackageAsync(shipmentId, packageId, request, cancellationToken);
        return Ok(ApiResponse<ShipmentPackageDto>.Ok(pkg, "Package updated successfully."));
    }

    /// <summary>
    /// Removes a package from an editable shipment.
    /// </summary>
    [HttpDelete("{packageId}")]
    [HasPermission(AppPermissions.Shipping.ShipmentsUpdate)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeletePackage(string shipmentId, string packageId, CancellationToken cancellationToken)
    {
        await _packageService.RemovePackageAsync(shipmentId, packageId, cancellationToken);
        return Ok(ApiResponse<bool>.Ok(true, "Package removed successfully."));
    }
}
