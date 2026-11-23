using FluentValidation;
using Logistics.Server.API.Authorization;
using Logistics.Server.Application.Common.Exceptions;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Organization.DTOs;
using Logistics.Server.Application.Modules.Organization.Services;
using Logistics.Shared.Contracts;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace Logistics.Server.API.Controllers.Organization;

/// <summary>
/// Regional operating branch management endpoints.
/// </summary>
[Route("api/v1/organization/branches")]
public class OrganizationBranchesController : BaseApiController
{
    private readonly IBranchService _branchService;
    private readonly IValidator<CreateBranchRequest> _createValidator;
    private readonly IValidator<UpdateBranchRequest> _updateValidator;
    private readonly IValidator<UpdateBranchStatusRequest> _statusValidator;

    public OrganizationBranchesController(
        IBranchService branchService,
        IValidator<CreateBranchRequest> createValidator,
        IValidator<UpdateBranchRequest> updateValidator,
        IValidator<UpdateBranchStatusRequest> statusValidator)
    {
        _branchService = branchService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _statusValidator = statusValidator;
    }

    /// <summary>
    /// Gets a paginated list of operating branches filtered by authorization and query parameters.
    /// </summary>
    [HttpGet]
    [HasPermission(AppPermissions.Organization.BranchesRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<BranchDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBranches(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] string? branchType,
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _branchService.GetBranchesAsync(search, isActive, branchType, request, cancellationToken);
        return Ok(ApiResponse<PagedResponse<BranchDto>>.Ok(result.ToPagedResponse()));
    }

    /// <summary>
    /// Gets branch details including department and warehouse counts.
    /// </summary>
    [HttpGet("{id}")]
    [HasPermission(AppPermissions.Organization.BranchesRead)]
    [ProducesResponseType(typeof(ApiResponse<BranchDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBranch(string id, CancellationToken cancellationToken)
    {
        var branch = await _branchService.GetByIdOrPublicIdAsync(id, cancellationToken);
        if (branch == null)
        {
            throw new NotFoundException("Branch", id);
        }
        return Ok(ApiResponse<BranchDetailDto>.Ok(branch));
    }

    /// <summary>
    /// Creates a new regional operating branch.
    /// </summary>
    [HttpPost]
    [HasPermission(AppPermissions.Organization.BranchesCreate)]
    [ProducesResponseType(typeof(ApiResponse<BranchDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateBranch([FromBody] CreateBranchRequest request, CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new Application.Common.Exceptions.ValidationException(validation.Errors);
        }

        var branch = await _branchService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetBranch), new { id = branch.PublicId }, ApiResponse<BranchDto>.Ok(branch, "Branch created successfully."));
    }

    /// <summary>
    /// Updates operating branch details.
    /// </summary>
    [HttpPut("{id}")]
    [HasPermission(AppPermissions.Organization.BranchesUpdate)]
    [ProducesResponseType(typeof(ApiResponse<BranchDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateBranch(string id, [FromBody] UpdateBranchRequest request, CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new Application.Common.Exceptions.ValidationException(validation.Errors);
        }

        var branch = await _branchService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<BranchDto>.Ok(branch, "Branch updated successfully."));
    }

    /// <summary>
    /// Activates or deactivates a branch.
    /// </summary>
    [HttpPatch("{id}/status")]
    [HasPermission(AppPermissions.Organization.BranchesManage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateBranchStatus(string id, [FromBody] UpdateBranchStatusRequest request, CancellationToken cancellationToken)
    {
        var validation = await _statusValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new Application.Common.Exceptions.ValidationException(validation.Errors);
        }

        await _branchService.UpdateStatusAsync(id, request, cancellationToken);
        return Ok(ApiResponse.Ok("Branch status updated successfully."));
    }

    /// <summary>
    /// Gets employees assigned to this branch.
    /// </summary>
    [HttpGet("{id}/employees")]
    [HasPermission(AppPermissions.Employees.Read)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<EmployeeDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBranchEmployees(
        string id,
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken)
    {
        var employees = await _branchService.GetBranchEmployeesAsync(id, search, status, request, cancellationToken);
        return Ok(ApiResponse<PagedResponse<EmployeeDto>>.Ok(employees.ToPagedResponse()));
    }

    /// <summary>
    /// Gets departments belonging to this branch.
    /// </summary>
    [HttpGet("{id}/departments")]
    [HasPermission(AppPermissions.Organization.DepartmentsRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DepartmentDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBranchDepartments(string id, CancellationToken cancellationToken)
    {
        var departments = await _branchService.GetBranchDepartmentsAsync(id, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<DepartmentDto>>.Ok(departments));
    }

    /// <summary>
    /// Gets warehouses operating under this branch.
    /// </summary>
    [HttpGet("{id}/warehouses")]
    [HasPermission(AppPermissions.Organization.WarehousesRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<WarehouseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBranchWarehouses(string id, CancellationToken cancellationToken)
    {
        var warehouses = await _branchService.GetBranchWarehousesAsync(id, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<WarehouseDto>>.Ok(warehouses));
    }

    /// <summary>
    /// Gets a summarized headcount and facility breakdown for this branch.
    /// </summary>
    [HttpGet("{id}/summary")]
    [HasPermission(AppPermissions.Organization.BranchesRead)]
    [ProducesResponseType(typeof(ApiResponse<BranchSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBranchSummary(string id, CancellationToken cancellationToken)
    {
        var summary = await _branchService.GetSummaryByIdOrPublicIdAsync(id, cancellationToken);
        if (summary == null)
        {
            throw new NotFoundException("Branch", id);
        }
        return Ok(ApiResponse<BranchSummaryDto>.Ok(summary));
    }
}
