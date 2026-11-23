using FluentValidation;
using Logistics.Server.API.Authorization;
using Logistics.Server.Application.Common.Exceptions;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Warehouse.DTOs;
using Logistics.Server.Application.Modules.Warehouse.Services;
using Logistics.Shared.Contracts;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.AspNetCore.Mvc;
using ValidationException = Logistics.Server.Application.Common.Exceptions.ValidationException;

namespace Logistics.Server.API.Controllers.Warehouse;

/// <summary>
/// Parcel scanning, receiving, sorting, staging, and exception tracking at distribution hubs.
/// </summary>
[Route("api/v1/parcel-handling")]
public class ParcelHandlingController : BaseApiController
{
    private readonly IParcelHandlingService _parcelService;
    private readonly IValidator<ScanPackageRequest> _scanValidator;
    private readonly IValidator<ParcelReceiveRequest> _receiveValidator;
    private readonly IValidator<ParcelSortRequest> _sortValidator;
    private readonly IValidator<ParcelStageRequest> _stageValidator;
    private readonly IValidator<ParcelExceptionRequest> _exceptionValidator;

    public ParcelHandlingController(
        IParcelHandlingService parcelService,
        IValidator<ScanPackageRequest> scanValidator,
        IValidator<ParcelReceiveRequest> receiveValidator,
        IValidator<ParcelSortRequest> sortValidator,
        IValidator<ParcelStageRequest> stageValidator,
        IValidator<ParcelExceptionRequest> exceptionValidator)
    {
        _parcelService = parcelService;
        _scanValidator = scanValidator;
        _receiveValidator = receiveValidator;
        _sortValidator = sortValidator;
        _stageValidator = stageValidator;
        _exceptionValidator = exceptionValidator;
    }

    [HttpPost("scan")]
    [HasPermission(AppPermissions.Warehouse.SortingManage)]
    [ProducesResponseType(typeof(ApiResponse<ScanPackageResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ScanPackage([FromBody] ScanPackageRequest request, CancellationToken cancellationToken)
    {
        var val = await _scanValidator.ValidateAsync(request, cancellationToken);
        if (!val.IsValid) throw new ValidationException(val.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

        var result = await _parcelService.ScanPackageAsync(request, cancellationToken);
        return Ok(ApiResponse<ScanPackageResultDto>.Ok(result));
    }

    [HttpPost("receive")]
    [HasPermission(AppPermissions.Warehouse.ReceiptsManage)]
    [ProducesResponseType(typeof(ApiResponse<ParcelHandlingEventDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReceiveParcel([FromBody] ParcelReceiveRequest request, CancellationToken cancellationToken)
    {
        var val = await _receiveValidator.ValidateAsync(request, cancellationToken);
        if (!val.IsValid) throw new ValidationException(val.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

        var result = await _parcelService.ReceiveParcelAsync(request, cancellationToken);
        return Ok(ApiResponse<ParcelHandlingEventDto>.Ok(result));
    }

    [HttpPost("sort")]
    [HasPermission(AppPermissions.Warehouse.SortingManage)]
    [ProducesResponseType(typeof(ApiResponse<ParcelHandlingEventDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SortParcel([FromBody] ParcelSortRequest request, CancellationToken cancellationToken)
    {
        var val = await _sortValidator.ValidateAsync(request, cancellationToken);
        if (!val.IsValid) throw new ValidationException(val.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

        var result = await _parcelService.SortParcelAsync(request, cancellationToken);
        return Ok(ApiResponse<ParcelHandlingEventDto>.Ok(result));
    }

    [HttpPost("stage")]
    [HasPermission(AppPermissions.Warehouse.SortingManage)]
    [ProducesResponseType(typeof(ApiResponse<ParcelHandlingEventDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> StageParcel([FromBody] ParcelStageRequest request, CancellationToken cancellationToken)
    {
        var val = await _stageValidator.ValidateAsync(request, cancellationToken);
        if (!val.IsValid) throw new ValidationException(val.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

        var result = await _parcelService.StageParcelAsync(request, cancellationToken);
        return Ok(ApiResponse<ParcelHandlingEventDto>.Ok(result));
    }

    [HttpPost("exception")]
    [HasPermission(AppPermissions.Warehouse.OperationsRead)]
    [ProducesResponseType(typeof(ApiResponse<ParcelHandlingEventDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RecordException([FromBody] ParcelExceptionRequest request, CancellationToken cancellationToken)
    {
        var val = await _exceptionValidator.ValidateAsync(request, cancellationToken);
        if (!val.IsValid) throw new ValidationException(val.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

        var result = await _parcelService.RecordExceptionAsync(request, cancellationToken);
        return Ok(ApiResponse<ParcelHandlingEventDto>.Ok(result));
    }

    [HttpGet("events")]
    [HasPermission(AppPermissions.Warehouse.OperationsRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<ParcelHandlingEventDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEvents(
        [FromQuery] long? shipmentPackageId,
        [FromQuery] long? shipmentId,
        [FromQuery] long? warehouseId,
        [FromQuery] string? eventType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _parcelService.GetEventsAsync(shipmentPackageId, shipmentId, warehouseId, eventType,
            new PagedRequest { Page = page, PageSize = pageSize }, cancellationToken);
        return Ok(ApiResponse<PagedResponse<ParcelHandlingEventDto>>.Ok(result.ToPagedResponse()));
    }
}

/// <summary>
/// Warehouse parcel and inventory outbound dispatch workflows.
/// </summary>
[Route("api/v1/warehouse-dispatches")]
public class WarehouseDispatchesController : BaseApiController
{
    private readonly IParcelHandlingService _parcelService;
    private readonly IValidator<CreateWarehouseDispatchDto> _createDispatchValidator;

    public WarehouseDispatchesController(
        IParcelHandlingService parcelService,
        IValidator<CreateWarehouseDispatchDto> createDispatchValidator)
    {
        _parcelService = parcelService;
        _createDispatchValidator = createDispatchValidator;
    }

    [HttpGet]
    [HasPermission(AppPermissions.Warehouse.OperationsRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<WarehouseDispatchDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDispatches(
        [FromQuery] long? warehouseId,
        [FromQuery] string? dispatchType,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _parcelService.GetDispatchesAsync(warehouseId, dispatchType, status,
            new PagedRequest { Page = page, PageSize = pageSize }, cancellationToken);
        return Ok(ApiResponse<PagedResponse<WarehouseDispatchDto>>.Ok(result.ToPagedResponse()));
    }

    [HttpGet("{id:long}")]
    [HasPermission(AppPermissions.Warehouse.OperationsRead)]
    [ProducesResponseType(typeof(ApiResponse<WarehouseDispatchDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDispatch(long id, CancellationToken cancellationToken)
    {
        var dispatch = await _parcelService.GetDispatchByIdAsync(id, cancellationToken);
        if (dispatch == null) throw new NotFoundException("WarehouseDispatch", id);
        return Ok(ApiResponse<WarehouseDispatchDto>.Ok(dispatch));
    }

    [HttpPost]
    [HasPermission(AppPermissions.Warehouse.DispatchesManage)]
    [ProducesResponseType(typeof(ApiResponse<WarehouseDispatchDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateDispatch([FromBody] CreateWarehouseDispatchDto request, CancellationToken cancellationToken)
    {
        var val = await _createDispatchValidator.ValidateAsync(request, cancellationToken);
        if (!val.IsValid) throw new ValidationException(val.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

        var dispatch = await _parcelService.CreateDispatchAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetDispatch), new { id = dispatch.Id }, ApiResponse<WarehouseDispatchDto>.Ok(dispatch));
    }

    [HttpPost("{id:long}/confirm")]
    [HasPermission(AppPermissions.Warehouse.DispatchesManage)]
    [ProducesResponseType(typeof(ApiResponse<WarehouseDispatchDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ConfirmDispatch(long id, [FromQuery] long employeeId, CancellationToken cancellationToken)
    {
        var dispatch = await _parcelService.ConfirmDispatchAsync(id, employeeId, cancellationToken);
        return Ok(ApiResponse<WarehouseDispatchDto>.Ok(dispatch));
    }
}
