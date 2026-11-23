using FluentValidation;
using Logistics.Server.API.Authorization;
using Logistics.Server.Application.Common.Exceptions;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Organization.DTOs;
using Logistics.Server.Application.Modules.Organization.Services;
using Logistics.Shared.Contracts;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace Logistics.Server.API.Controllers;

/// <summary>
/// Employee master data and user account integration endpoints.
/// </summary>
[Route("api/v1/[controller]")]
public class EmployeesController : BaseApiController
{
    private readonly IEmployeeService _employeeService;
    private readonly IValidator<CreateEmployeeRequest> _createValidator;
    private readonly IValidator<UpdateEmployeeRequest> _updateValidator;
    private readonly IValidator<UpdateEmployeeStatusRequest> _statusValidator;
    private readonly IValidator<CreateEmployeeAccountRequest> _accountCreateValidator;
    private readonly IValidator<UpdateEmployeeAccountStatusRequest> _accountStatusValidator;

    public EmployeesController(
        IEmployeeService employeeService,
        IValidator<CreateEmployeeRequest> createValidator,
        IValidator<UpdateEmployeeRequest> updateValidator,
        IValidator<UpdateEmployeeStatusRequest> statusValidator,
        IValidator<CreateEmployeeAccountRequest> accountCreateValidator,
        IValidator<UpdateEmployeeAccountStatusRequest> accountStatusValidator)
    {
        _employeeService = employeeService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _statusValidator = statusValidator;
        _accountCreateValidator = accountCreateValidator;
        _accountStatusValidator = accountStatusValidator;
    }

    /// <summary>
    /// Gets a paginated list of employees with search and multi-criteria filtering.
    /// </summary>
    [HttpGet]
    [HasPermission(AppPermissions.Employees.Read)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<EmployeeDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEmployees(
        [FromQuery] string? search,
        [FromQuery] long? branchId,
        [FromQuery] long? departmentId,
        [FromQuery] string? status,
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _employeeService.GetEmployeesAsync(search, branchId, departmentId, status, request, cancellationToken);
        return Ok(ApiResponse<PagedResponse<EmployeeDto>>.Ok(result.ToPagedResponse()));
    }

    /// <summary>
    /// Gets employee details by identifier.
    /// </summary>
    [HttpGet("{id}")]
    [HasPermission(AppPermissions.Employees.Read)]
    [ProducesResponseType(typeof(ApiResponse<EmployeeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEmployee(string id, CancellationToken cancellationToken)
    {
        var employee = await _employeeService.GetByIdOrPublicIdAsync(id, cancellationToken);
        if (employee == null)
        {
            throw new NotFoundException("Employee", id);
        }
        return Ok(ApiResponse<EmployeeDto>.Ok(employee));
    }

    /// <summary>
    /// Gets complete profile information for an employee.
    /// </summary>
    [HttpGet("{id}/profile")]
    [HasPermission(AppPermissions.Employees.Read)]
    [ProducesResponseType(typeof(ApiResponse<EmployeeProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEmployeeProfile(string id, CancellationToken cancellationToken)
    {
        var profile = await _employeeService.GetProfileByIdOrPublicIdAsync(id, cancellationToken);
        if (profile == null)
        {
            throw new NotFoundException("Employee", id);
        }
        return Ok(ApiResponse<EmployeeProfileDto>.Ok(profile));
    }

    /// <summary>
    /// Creates a new employee record. Does not provision a user account.
    /// </summary>
    [HttpPost]
    [HasPermission(AppPermissions.Employees.Create)]
    [ProducesResponseType(typeof(ApiResponse<EmployeeDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeRequest request, CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new Application.Common.Exceptions.ValidationException(validation.Errors);
        }

        var employee = await _employeeService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetEmployee), new { id = employee.PublicId }, ApiResponse<EmployeeDto>.Ok(employee, "Employee created successfully."));
    }

    /// <summary>
    /// Updates an existing employee's information.
    /// </summary>
    [HttpPut("{id}")]
    [HasPermission(AppPermissions.Employees.Update)]
    [ProducesResponseType(typeof(ApiResponse<EmployeeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateEmployee(string id, [FromBody] UpdateEmployeeRequest request, CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new Application.Common.Exceptions.ValidationException(validation.Errors);
        }

        var employee = await _employeeService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<EmployeeDto>.Ok(employee, "Employee updated successfully."));
    }

    /// <summary>
    /// Changes an employee's status. Deactivating or terminating automatically invalidates linked user sessions.
    /// </summary>
    [HttpPatch("{id}/status")]
    [HasPermission(AppPermissions.Employees.StatusManage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateEmployeeStatus(string id, [FromBody] UpdateEmployeeStatusRequest request, CancellationToken cancellationToken)
    {
        var validation = await _statusValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new Application.Common.Exceptions.ValidationException(validation.Errors);
        }

        await _employeeService.UpdateStatusAsync(id, request, cancellationToken);
        return Ok(ApiResponse.Ok("Employee status updated successfully."));
    }

    /// <summary>
    /// Gets linked user account information for an employee.
    /// </summary>
    [HttpGet("{id}/account")]
    [HasPermission(AppPermissions.Employees.AccountRead)]
    [ProducesResponseType(typeof(ApiResponse<EmployeeAccountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEmployeeAccount(string id, CancellationToken cancellationToken)
    {
        var account = await _employeeService.GetAccountAsync(id, cancellationToken);
        return Ok(ApiResponse<EmployeeAccountDto>.Ok(account));
    }

    /// <summary>
    /// Creates and links a new user account for an existing employee.
    /// </summary>
    [HttpPost("{id}/account")]
    [HasPermission(AppPermissions.Employees.AccountCreate)]
    [ProducesResponseType(typeof(ApiResponse<EmployeeAccountDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateEmployeeAccount(string id, [FromBody] CreateEmployeeAccountRequest request, CancellationToken cancellationToken)
    {
        var validation = await _accountCreateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new Application.Common.Exceptions.ValidationException(validation.Errors);
        }

        var account = await _employeeService.CreateAccountAsync(id, request, cancellationToken);
        return CreatedAtAction(nameof(GetEmployeeAccount), new { id }, ApiResponse<EmployeeAccountDto>.Ok(account, "User account created and linked to employee."));
    }

    /// <summary>
    /// Activates or deactivates an employee's linked user account.
    /// </summary>
    [HttpPatch("{id}/account/status")]
    [HasPermission(AppPermissions.Employees.AccountManage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEmployeeAccountStatus(string id, [FromBody] UpdateEmployeeAccountStatusRequest request, CancellationToken cancellationToken)
    {
        var validation = await _accountStatusValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new Application.Common.Exceptions.ValidationException(validation.Errors);
        }

        await _employeeService.UpdateAccountStatusAsync(id, request, cancellationToken);
        return Ok(ApiResponse.Ok("Employee user account status updated successfully."));
    }

    /// <summary>
    /// Gets assigned operating branch access for an employee.
    /// </summary>
    [HttpGet("{id}/branch-access")]
    [HasPermission(AppPermissions.Identity.UsersBranchAccessRead)]
    [ProducesResponseType(typeof(ApiResponse<EmployeeBranchAccessDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEmployeeBranchAccess(string id, CancellationToken cancellationToken)
    {
        var access = await _employeeService.GetBranchAccessAsync(id, cancellationToken);
        return Ok(ApiResponse<EmployeeBranchAccessDto>.Ok(access));
    }
}
