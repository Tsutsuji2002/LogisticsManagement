using FluentValidation;
using Logistics.Server.API.Authorization;
using Logistics.Server.Application.Common.Exceptions;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Shipping.DTOs;
using Logistics.Server.Application.Modules.Shipping.Services;
using Logistics.Shared.Contracts;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.AspNetCore.Mvc;
using ValidationException = Logistics.Server.Application.Common.Exceptions.ValidationException;

namespace Logistics.Server.API.Controllers.Shipping;

/// <summary>
/// Endpoints for return request creation, approval, rejection, and tracking.
/// </summary>
[Route("api/v1/returns")]
public class ReturnRequestsController : BaseApiController
{
    private readonly IReturnRequestService _returnService;
    private readonly IValidator<CreateReturnRequestRequest> _createValidator;
    private readonly IValidator<ProcessReturnRequestRequest> _processValidator;

    public ReturnRequestsController(
        IReturnRequestService returnService,
        IValidator<CreateReturnRequestRequest> createValidator,
        IValidator<ProcessReturnRequestRequest> processValidator)
    {
        _returnService = returnService;
        _createValidator = createValidator;
        _processValidator = processValidator;
    }

    /// <summary>
    /// Lists and filters return requests with pagination.
    /// </summary>
    [HttpGet]
    [HasPermission(AppPermissions.Shipping.ReturnsRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<ReturnRequestDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReturnRequests(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] long? customerId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _returnService.GetReturnRequestsAsync(
            search, status, customerId, dateFrom, dateTo, request, cancellationToken);
        return Ok(ApiResponse<PagedResponse<ReturnRequestDto>>.Ok(result.ToPagedResponse()));
    }

    /// <summary>
    /// Retrieves a return request by ID or ReturnCode.
    /// </summary>
    [HttpGet("{id}")]
    [HasPermission(AppPermissions.Shipping.ReturnsRead)]
    [ProducesResponseType(typeof(ApiResponse<ReturnRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReturnRequest(string id, CancellationToken cancellationToken)
    {
        var rr = await _returnService.GetByIdOrCodeAsync(id, cancellationToken);
        if (rr == null)
        {
            return NotFound(ApiResponse<ReturnRequestDto>.Fail($"Return request '{id}' was not found."));
        }

        return Ok(ApiResponse<ReturnRequestDto>.Ok(rr));
    }

    /// <summary>
    /// Creates a new return request for a delivered or failed shipment.
    /// </summary>
    [HttpPost]
    [HasPermission(AppPermissions.Shipping.ReturnsManage)]
    [ProducesResponseType(typeof(ApiResponse<ReturnRequestDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateReturnRequest([FromBody] CreateReturnRequestRequest request, CancellationToken cancellationToken)
    {
        var valResult = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!valResult.IsValid)
        {
            throw new ValidationException(valResult.ToDictionary());
        }

        var rr = await _returnService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetReturnRequest), new { id = rr.ReturnCode }, ApiResponse<ReturnRequestDto>.Ok(rr, "Return request created successfully."));
    }

    /// <summary>
    /// Approves or rejects a return request.
    /// </summary>
    [HttpPost("{id}/process")]
    [HasPermission(AppPermissions.Shipping.ReturnsManage)]
    [ProducesResponseType(typeof(ApiResponse<ReturnRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ProcessReturnRequest(string id, [FromBody] ProcessReturnRequestRequest request, CancellationToken cancellationToken)
    {
        var valResult = await _processValidator.ValidateAsync(request, cancellationToken);
        if (!valResult.IsValid)
        {
            throw new ValidationException(valResult.ToDictionary());
        }

        var rr = await _returnService.ProcessAsync(id, request, cancellationToken);
        return Ok(ApiResponse<ReturnRequestDto>.Ok(rr, $"Return request {request.Decision.ToLower()} successfully."));
    }
}
