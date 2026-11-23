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
/// Cash on Delivery (COD) collection lifecycle, driver collection, branch deposit, and cashier verification.
/// </summary>
[Route("api/v1/cod/collections")]
public class CodCollectionsController : BaseApiController
{
    private readonly ICodCollectionService _collectionService;
    private readonly IValidator<CreateCodCollectionRequest> _createValidator;
    private readonly IValidator<RecordCodCollectionRequest> _recordValidator;
    private readonly IValidator<RecordCodFailureRequest> _failureValidator;
    private readonly IValidator<VerifyCodCollectionRequest> _verifyValidator;

    public CodCollectionsController(
        ICodCollectionService collectionService,
        IValidator<CreateCodCollectionRequest> createValidator,
        IValidator<RecordCodCollectionRequest> recordValidator,
        IValidator<RecordCodFailureRequest> failureValidator,
        IValidator<VerifyCodCollectionRequest> verifyValidator)
    {
        _collectionService = collectionService;
        _createValidator = createValidator;
        _recordValidator = recordValidator;
        _failureValidator = failureValidator;
        _verifyValidator = verifyValidator;
    }

    [HttpGet]
    [HasPermission(AppPermissions.Cod.CollectionsRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<CodCollectionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCollections(
        [FromQuery] long? branchId,
        [FromQuery] long? employeeId,
        [FromQuery] long? customerId,
        [FromQuery] string? status,
        [FromQuery] bool? isRemitted,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _collectionService.GetCollectionsAsync(
            branchId, employeeId, customerId, status, isRemitted, fromDate, toDate,
            new PagedRequest { Page = page, PageSize = pageSize }, cancellationToken);

        return Ok(ApiResponse<PagedResponse<CodCollectionDto>>.Ok(result.ToPagedResponse()));
    }

    [HttpGet("{id:long}")]
    [HasPermission(AppPermissions.Cod.CollectionsRead)]
    [ProducesResponseType(typeof(ApiResponse<CodCollectionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCollectionById(long id, CancellationToken cancellationToken)
    {
        var collection = await _collectionService.GetCollectionByIdAsync(id, cancellationToken);
        if (collection == null) throw new NotFoundException("CODCollection", id);
        return Ok(ApiResponse<CodCollectionDto>.Ok(collection));
    }

    [HttpGet("shipments/{shipmentId:long}")]
    [HasPermission(AppPermissions.Cod.CollectionsRead)]
    [ProducesResponseType(typeof(ApiResponse<CodCollectionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCollectionByShipmentId(long shipmentId, CancellationToken cancellationToken)
    {
        var collection = await _collectionService.GetCollectionByShipmentIdAsync(shipmentId, cancellationToken);
        if (collection == null) throw new NotFoundException("CODCollection for Shipment", shipmentId);
        return Ok(ApiResponse<CodCollectionDto>.Ok(collection));
    }

    [HttpGet("summary")]
    [HasPermission(AppPermissions.Cod.CollectionsRead)]
    [ProducesResponseType(typeof(ApiResponse<CodCollectionSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(
        [FromQuery] long? branchId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        CancellationToken cancellationToken = default)
    {
        var summary = await _collectionService.GetSummaryAsync(branchId, fromDate, toDate, cancellationToken);
        return Ok(ApiResponse<CodCollectionSummaryDto>.Ok(summary));
    }

    [HttpPost]
    [HasPermission(AppPermissions.Cod.CollectionsCreate)]
    [ProducesResponseType(typeof(ApiResponse<CodCollectionDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateCollection([FromBody] CreateCodCollectionRequest request, CancellationToken cancellationToken)
    {
        var val = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!val.IsValid) throw new ValidationException(val.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

        var collection = await _collectionService.CreateCollectionAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetCollectionById), new { id = collection.Id }, ApiResponse<CodCollectionDto>.Ok(collection));
    }

    [HttpPost("{id:long}/collect")]
    [HasPermission(AppPermissions.Cod.CollectionsCollect)]
    [ProducesResponseType(typeof(ApiResponse<CodCollectionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RecordCollection(long id, [FromBody] RecordCodCollectionRequest request, CancellationToken cancellationToken)
    {
        var val = await _recordValidator.ValidateAsync(request, cancellationToken);
        if (!val.IsValid) throw new ValidationException(val.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

        var collection = await _collectionService.RecordCollectionAsync(id, request, cancellationToken);
        return Ok(ApiResponse<CodCollectionDto>.Ok(collection, "COD collection recorded successfully."));
    }

    [HttpPost("{id:long}/deposit")]
    [HasPermission(AppPermissions.Cod.CollectionsCollect)]
    [ProducesResponseType(typeof(ApiResponse<CodCollectionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DepositToBranch(long id, [FromQuery] string? notes, CancellationToken cancellationToken)
    {
        var collection = await _collectionService.DepositToBranchAsync(id, notes, cancellationToken);
        return Ok(ApiResponse<CodCollectionDto>.Ok(collection, "COD deposited to branch successfully."));
    }

    [HttpPost("{id:long}/verify")]
    [HasPermission(AppPermissions.Cod.CollectionsVerify)]
    [ProducesResponseType(typeof(ApiResponse<CodCollectionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> VerifyCollection(long id, [FromBody] VerifyCodCollectionRequest request, CancellationToken cancellationToken)
    {
        var val = await _verifyValidator.ValidateAsync(request, cancellationToken);
        if (!val.IsValid) throw new ValidationException(val.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

        var collection = await _collectionService.VerifyCollectionAsync(id, request, cancellationToken);
        return Ok(ApiResponse<CodCollectionDto>.Ok(collection, "COD verified by cashier successfully."));
    }

    [HttpPost("{id:long}/record-failure")]
    [HasPermission(AppPermissions.Cod.CollectionsManage)]
    [ProducesResponseType(typeof(ApiResponse<CodCollectionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RecordFailure(long id, [FromBody] RecordCodFailureRequest request, CancellationToken cancellationToken)
    {
        var val = await _failureValidator.ValidateAsync(request, cancellationToken);
        if (!val.IsValid) throw new ValidationException(val.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

        var collection = await _collectionService.RecordFailureAsync(id, request, cancellationToken);
        return Ok(ApiResponse<CodCollectionDto>.Ok(collection, "COD collection failure recorded successfully."));
    }
}
