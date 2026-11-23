using FluentValidation;
using Logistics.Server.API.Authorization;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Catalog.DTOs;
using Logistics.Server.Application.Modules.Catalog.Services;
using Logistics.Shared.Contracts;
using Microsoft.AspNetCore.Mvc;
using ValidationException = Logistics.Server.Application.Common.Exceptions.ValidationException;

namespace Logistics.Server.API.Controllers.Catalog;

/// <summary>
/// Endpoints for managing and querying units of measure (weight, dimension, volume, count).
/// </summary>
[Route("api/v1/catalog/units")]
public class UnitsOfMeasureController : BaseApiController
{
    private readonly IUnitOfMeasureService _uomService;
    private readonly IValidator<CreateUnitOfMeasureRequest> _createValidator;
    private readonly IValidator<UpdateUnitOfMeasureRequest> _updateValidator;
    private readonly IValidator<UpdateUnitStatusRequest> _statusValidator;

    public UnitsOfMeasureController(
        IUnitOfMeasureService uomService,
        IValidator<CreateUnitOfMeasureRequest> createValidator,
        IValidator<UpdateUnitOfMeasureRequest> updateValidator,
        IValidator<UpdateUnitStatusRequest> statusValidator)
    {
        _uomService = uomService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _statusValidator = statusValidator;
    }

    /// <summary>
    /// Lists all units of measure with optional type and active status filters.
    /// </summary>
    [HttpGet]
    [HasPermission(AppPermissions.Catalog.UnitsRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<UnitOfMeasureDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnits(
        [FromQuery] string? search,
        [FromQuery] string? type,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        var units = await _uomService.GetUnitsAsync(search, type, isActive, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<UnitOfMeasureDto>>.Ok(units));
    }

    /// <summary>
    /// Searches units of measure by code or name.
    /// </summary>
    [HttpGet("search")]
    [HasPermission(AppPermissions.Catalog.UnitsRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<UnitOfMeasureDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] string? q,
        CancellationToken cancellationToken)
    {
        var units = await _uomService.SearchAsync(q ?? string.Empty, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<UnitOfMeasureDto>>.Ok(units));
    }

    /// <summary>
    /// Retrieves a unit of measure by ID.
    /// </summary>
    [HttpGet("{id:long}")]
    [HasPermission(AppPermissions.Catalog.UnitsRead)]
    [ProducesResponseType(typeof(ApiResponse<UnitOfMeasureDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUnitById(long id, CancellationToken cancellationToken)
    {
        var unit = await _uomService.GetByIdAsync(id, cancellationToken);
        if (unit == null)
        {
            return NotFound(ApiResponse<UnitOfMeasureDto>.Fail($"Unit of measure with ID {id} not found."));
        }

        return Ok(ApiResponse<UnitOfMeasureDto>.Ok(unit));
    }

    /// <summary>
    /// Creates a new administrative unit of measure.
    /// </summary>
    [HttpPost]
    [HasPermission(AppPermissions.Catalog.UnitsManage)]
    [ProducesResponseType(typeof(ApiResponse<UnitOfMeasureDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateUnit(
        [FromBody] CreateUnitOfMeasureRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        var created = await _uomService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetUnitById), new { id = created.Id }, ApiResponse<UnitOfMeasureDto>.Ok(created, "Unit of measure created successfully."));
    }

    /// <summary>
    /// Updates unit of measure properties.
    /// </summary>
    [HttpPut("{id:long}")]
    [HasPermission(AppPermissions.Catalog.UnitsManage)]
    [ProducesResponseType(typeof(ApiResponse<UnitOfMeasureDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUnit(
        long id,
        [FromBody] UpdateUnitOfMeasureRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        var updated = await _uomService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<UnitOfMeasureDto>.Ok(updated, "Unit of measure updated successfully."));
    }

    /// <summary>
    /// Activates or deactivates a unit of measure.
    /// </summary>
    [HttpPatch("{id:long}/status")]
    [HasPermission(AppPermissions.Catalog.UnitsManage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        long id,
        [FromBody] UpdateUnitStatusRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _statusValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        await _uomService.UpdateStatusAsync(id, request, cancellationToken);
        return Ok(ApiResponse.Ok("Unit of measure status updated successfully."));
    }

    /// <summary>
    /// Deletes an unreferenced unit of measure.
    /// </summary>
    [HttpDelete("{id:long}")]
    [HasPermission(AppPermissions.Catalog.UnitsManage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUnit(long id, CancellationToken cancellationToken)
    {
        await _uomService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok("Unit of measure deleted successfully."));
    }
}
