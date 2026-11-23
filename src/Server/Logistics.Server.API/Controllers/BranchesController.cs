using FluentValidation;
using Logistics.Server.Application.Common.Exceptions;
using Logistics.Server.Application.Modules.Organization.DTOs;
using Logistics.Server.Application.Modules.Organization.Services;
using Logistics.Shared.Contracts;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace Logistics.Server.API.Controllers;

/// <summary>
/// Manages branch operations.
/// </summary>
public class BranchesController : BaseApiController
{
    private readonly IBranchService _branchService;
    private readonly IValidator<CreateBranchRequest> _createValidator;

    public BranchesController(IBranchService branchService, IValidator<CreateBranchRequest> createValidator)
    {
        _branchService = branchService;
        _createValidator = createValidator;
    }

    /// <summary>
    /// Gets a paginated list of branches, optionally filtered by company.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<BranchDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBranches([FromQuery] long? companyId, [FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        var result = await _branchService.GetBranchesAsync(companyId, request, cancellationToken);
        return Ok(ApiResponse<PagedResponse<BranchDto>>.Ok(result.ToPagedResponse()));
    }

    /// <summary>
    /// Gets a branch by its public identifier.
    /// </summary>
    [HttpGet("{publicId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<BranchDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBranch(Guid publicId, CancellationToken cancellationToken)
    {
        var branch = await _branchService.GetByPublicIdAsync(publicId, cancellationToken);
        if (branch == null)
        {
            throw new NotFoundException("Branch", publicId);
        }
        return Ok(ApiResponse<BranchDto>.Ok(branch));
    }

    /// <summary>
    /// Creates a new branch.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<BranchDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateBranch([FromBody] CreateBranchRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new Application.Common.Exceptions.ValidationException(validationResult.Errors);
        }

        var branch = await _branchService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetBranch), new { publicId = branch.PublicId }, ApiResponse<BranchDto>.Ok(branch));
    }
}
