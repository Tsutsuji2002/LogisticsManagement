using FluentValidation;
using Logistics.Server.API.Authorization;
using Logistics.Server.Application.Common.Exceptions;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Cod.DTOs;
using Logistics.Server.Application.Modules.Cod.Services;
using Logistics.Shared.Contracts;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.AspNetCore.Mvc;
using ValidationException = Logistics.Server.Application.Common.Exceptions.ValidationException;

namespace Logistics.Server.API.Controllers.Cod;

/// <summary>
/// Cash on Delivery (COD) remittances management: grouping verified collections, customer settlements, payouts.
/// </summary>
[Route("api/v1/cod/remittances")]
public class CodRemittancesController : BaseApiController
{
    private readonly ICodRemittanceService _remittanceService;
    private readonly IValidator<CreateCodRemittanceRequest> _createValidator;
    private readonly IValidator<AddCodRemittanceItemRequest> _addItemValidator;
    private readonly IValidator<ConfirmCodRemittanceRequest> _confirmValidator;
    private readonly IValidator<CancelCodRemittanceRequest> _cancelValidator;

    public CodRemittancesController(
        ICodRemittanceService remittanceService,
        IValidator<CreateCodRemittanceRequest> createValidator,
        IValidator<AddCodRemittanceItemRequest> addItemValidator,
        IValidator<ConfirmCodRemittanceRequest> confirmValidator,
        IValidator<CancelCodRemittanceRequest> cancelValidator)
    {
        _remittanceService = remittanceService;
        _createValidator = createValidator;
        _addItemValidator = addItemValidator;
        _confirmValidator = confirmValidator;
        _cancelValidator = cancelValidator;
    }

    [HttpGet]
    [HasPermission(AppPermissions.Cod.RemittancesRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<CodRemittanceDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRemittances(
        [FromQuery] long? customerId,
        [FromQuery] string? status,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _remittanceService.GetRemittancesAsync(
            customerId, status, fromDate, toDate,
            new PagedRequest { Page = page, PageSize = pageSize }, cancellationToken);

        return Ok(ApiResponse<PagedResponse<CodRemittanceDto>>.Ok(result.ToPagedResponse()));
    }

    [HttpGet("{id:long}")]
    [HasPermission(AppPermissions.Cod.RemittancesRead)]
    [ProducesResponseType(typeof(ApiResponse<CodRemittanceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRemittanceById(long id, CancellationToken cancellationToken)
    {
        var remittance = await _remittanceService.GetRemittanceByIdAsync(id, cancellationToken);
        if (remittance == null) throw new NotFoundException("CODRemittance", id);
        return Ok(ApiResponse<CodRemittanceDto>.Ok(remittance));
    }

    [HttpPost]
    [HasPermission(AppPermissions.Cod.RemittancesCreate)]
    [ProducesResponseType(typeof(ApiResponse<CodRemittanceDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateRemittance([FromBody] CreateCodRemittanceRequest request, CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors.ToDictionary(e => e.PropertyName, e => new[] { e.ErrorMessage }));

        var created = await _remittanceService.CreateRemittanceAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetRemittanceById), new { id = created.Id }, ApiResponse<CodRemittanceDto>.Ok(created, "COD Remittance created successfully."));
    }

    [HttpPost("{id:long}/items")]
    [HasPermission(AppPermissions.Cod.RemittancesManage)]
    [ProducesResponseType(typeof(ApiResponse<CodRemittanceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddItem(long id, [FromBody] AddCodRemittanceItemRequest request, CancellationToken cancellationToken)
    {
        var validation = await _addItemValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors.ToDictionary(e => e.PropertyName, e => new[] { e.ErrorMessage }));

        var result = await _remittanceService.AddItemAsync(id, request, cancellationToken);
        return Ok(ApiResponse<CodRemittanceDto>.Ok(result, "Collection item added to remittance."));
    }

    [HttpDelete("{id:long}/items/{itemId:long}")]
    [HasPermission(AppPermissions.Cod.RemittancesManage)]
    [ProducesResponseType(typeof(ApiResponse<CodRemittanceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveItem(long id, long itemId, CancellationToken cancellationToken)
    {
        var result = await _remittanceService.RemoveItemAsync(id, itemId, cancellationToken);
        return Ok(ApiResponse<CodRemittanceDto>.Ok(result, "Item removed from remittance."));
    }

    [HttpPost("{id:long}/confirm")]
    [HasPermission(AppPermissions.Cod.RemittancesConfirm)]
    [ProducesResponseType(typeof(ApiResponse<CodRemittanceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ConfirmRemittance(long id, [FromBody] ConfirmCodRemittanceRequest request, CancellationToken cancellationToken)
    {
        var validation = await _confirmValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors.ToDictionary(e => e.PropertyName, e => new[] { e.ErrorMessage }));

        var result = await _remittanceService.ConfirmRemittanceAsync(id, request, cancellationToken);
        return Ok(ApiResponse<CodRemittanceDto>.Ok(result, "COD Remittance confirmed and completed."));
    }

    [HttpPost("{id:long}/cancel")]
    [HasPermission(AppPermissions.Cod.RemittancesManage)]
    [ProducesResponseType(typeof(ApiResponse<CodRemittanceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CancelRemittance(long id, [FromBody] CancelCodRemittanceRequest request, CancellationToken cancellationToken)
    {
        var validation = await _cancelValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors.ToDictionary(e => e.PropertyName, e => new[] { e.ErrorMessage }));

        var result = await _remittanceService.CancelRemittanceAsync(id, request.Reason, cancellationToken);
        return Ok(ApiResponse<CodRemittanceDto>.Ok(result, "COD Remittance cancelled successfully."));
    }
}
