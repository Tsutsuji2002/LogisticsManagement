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
/// Organizational department management endpoints.
/// </summary>
[Route("api/v1/organization/departments")]
public class OrganizationDepartmentsController : BaseApiController
{
    private readonly IDepartmentService _departmentService;
    private readonly IValidator<CreateDepartmentRequest> _createValidator;
    private readonly IValidator<UpdateDepartmentRequest> _updateValidator;
    private readonly IValidator<UpdateDepartmentStatusRequest> _statusValidator;

    public OrganizationDepartmentsController(
        IDepartmentService departmentService,
        IValidator<CreateDepartmentRequest> createValidator,
        IValidator<UpdateDepartmentRequest> updateValidator,
        IValidator<UpdateDepartmentStatusRequest> statusValidator)
    {
        _departmentService = departmentService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _statusValidator = statusValidator;
    }

    /// <summary>
    /// Gets a paginated list of departments.
    /// </summary>
    [HttpGet]
    [HasPermission(AppPermissions.Organization.DepartmentsRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<DepartmentDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDepartments(
        [FromQuery] string? search,
        [FromQuery] long? branchId,
        [FromQuery] bool? isActive,
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _departmentService.GetDepartmentsAsync(search, branchId, isActive, request, cancellationToken);
        return Ok(ApiResponse<PagedResponse<DepartmentDto>>.Ok(result.ToPagedResponse()));
    }

    /// <summary>
    /// Gets department details by its identifier.
    /// </summary>
    [HttpGet("{id}")]
    [HasPermission(AppPermissions.Organization.DepartmentsRead)]
    [ProducesResponseType(typeof(ApiResponse<DepartmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDepartment(string id, CancellationToken cancellationToken)
    {
        var department = await _departmentService.GetByIdOrPublicIdAsync(id, cancellationToken);
        if (department == null)
        {
            throw new NotFoundException("Department", id);
        }
        return Ok(ApiResponse<DepartmentDto>.Ok(department));
    }

    /// <summary>
    /// Creates a new department within an operating branch.
    /// </summary>
    [HttpPost]
    [HasPermission(AppPermissions.Organization.DepartmentsCreate)]
    [ProducesResponseType(typeof(ApiResponse<DepartmentDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateDepartment([FromBody] CreateDepartmentRequest request, CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new Application.Common.Exceptions.ValidationException(validation.Errors);
        }

        var department = await _departmentService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetDepartment), new { id = department.PublicId }, ApiResponse<DepartmentDto>.Ok(department, "Department created successfully."));
    }

    /// <summary>
    /// Updates department details.
    /// </summary>
    [HttpPut("{id}")]
    [HasPermission(AppPermissions.Organization.DepartmentsUpdate)]
    [ProducesResponseType(typeof(ApiResponse<DepartmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDepartment(string id, [FromBody] UpdateDepartmentRequest request, CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new Application.Common.Exceptions.ValidationException(validation.Errors);
        }

        var department = await _departmentService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<DepartmentDto>.Ok(department, "Department updated successfully."));
    }

    /// <summary>
    /// Activates or deactivates a department.
    /// </summary>
    [HttpPatch("{id}/status")]
    [HasPermission(AppPermissions.Organization.DepartmentsManage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateDepartmentStatus(string id, [FromBody] UpdateDepartmentStatusRequest request, CancellationToken cancellationToken)
    {
        var validation = await _statusValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new Application.Common.Exceptions.ValidationException(validation.Errors);
        }

        await _departmentService.UpdateStatusAsync(id, request, cancellationToken);
        return Ok(ApiResponse.Ok("Department status updated successfully."));
    }

    /// <summary>
    /// Gets employees assigned to this department.
    /// </summary>
    [HttpGet("{id}/employees")]
    [HasPermission(AppPermissions.Employees.Read)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<EmployeeDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDepartmentEmployees(string id, [FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        var employees = await _departmentService.GetDepartmentEmployeesAsync(id, request, cancellationToken);
        return Ok(ApiResponse<PagedResponse<EmployeeDto>>.Ok(employees.ToPagedResponse()));
    }
}
