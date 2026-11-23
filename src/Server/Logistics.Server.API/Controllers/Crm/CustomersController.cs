using FluentValidation;
using Logistics.Server.API.Authorization;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Crm.DTOs;
using Logistics.Server.Application.Modules.Crm.Services;
using Logistics.Shared.Contracts;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace Logistics.Server.API.Controllers.Crm;

/// <summary>
/// Endpoints for customer master management, directory listing, and metrics overview.
/// </summary>
[Route("api/v1/customers")]
public class CustomersController : BaseApiController
{
    private readonly ICustomerService _customerService;
    private readonly IValidator<CreateCustomerRequest> _createValidator;
    private readonly IValidator<UpdateCustomerRequest> _updateValidator;
    private readonly IValidator<UpdateCustomerStatusRequest> _statusValidator;

    public CustomersController(
        ICustomerService customerService,
        IValidator<CreateCustomerRequest> createValidator,
        IValidator<UpdateCustomerRequest> updateValidator,
        IValidator<UpdateCustomerStatusRequest> statusValidator)
    {
        _customerService = customerService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _statusValidator = statusValidator;
    }

    /// <summary>
    /// Searches and filters customers across accessible operating branches.
    /// </summary>
    [HttpGet]
    [HasPermission(AppPermissions.Crm.CustomersRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<CustomerDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCustomers(
        [FromQuery] string? search,
        [FromQuery] string? customerType,
        [FromQuery] bool? isActive,
        [FromQuery] long? branchId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _customerService.GetCustomersAsync(search, customerType, isActive, branchId, dateFrom, dateTo, request, cancellationToken);
        return Ok(ApiResponse<PagedResponse<CustomerDto>>.Ok(result.ToPagedResponse()));
    }

    /// <summary>
    /// Retrieves aggregate high-level CRM customer statistics.
    /// </summary>
    [HttpGet("overview")]
    [HasPermission(AppPermissions.Crm.CustomersOverviewRead)]
    [ProducesResponseType(typeof(ApiResponse<CustomerOverviewDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOverview(CancellationToken cancellationToken)
    {
        var overview = await _customerService.GetOverviewAsync(cancellationToken);
        return Ok(ApiResponse<CustomerOverviewDto>.Ok(overview));
    }

    /// <summary>
    /// Retrieves a single customer profile or consolidated customer details.
    /// </summary>
    [HttpGet("{id}")]
    [HasPermission(AppPermissions.Crm.CustomersRead)]
    [ProducesResponseType(typeof(ApiResponse<CustomerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CustomerDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCustomer(
        string id,
        [FromQuery] bool includeDetails = false,
        CancellationToken cancellationToken = default)
    {
        if (includeDetails)
        {
            var detail = await _customerService.GetDetailByIdOrPublicIdAsync(id, cancellationToken);
            if (detail == null)
            {
                return NotFound(ApiResponse<CustomerDetailDto>.Fail($"Customer '{id}' not found."));
            }
            return Ok(ApiResponse<CustomerDetailDto>.Ok(detail));
        }

        var customer = await _customerService.GetByIdOrPublicIdAsync(id, cancellationToken);
        if (customer == null)
        {
            return NotFound(ApiResponse<CustomerDto>.Fail($"Customer '{id}' not found."));
        }
        return Ok(ApiResponse<CustomerDto>.Ok(customer));
    }

    /// <summary>
    /// Creates a new customer record under an authorized branch.
    /// </summary>
    [HttpPost]
    [HasPermission(AppPermissions.Crm.CustomersCreate)]
    [ProducesResponseType(typeof(ApiResponse<CustomerDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCustomer(
        [FromBody] CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new Application.Common.Exceptions.ValidationException(validation.Errors);
        }

        var customer = await _customerService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetCustomer), new { id = customer.PublicId }, ApiResponse<CustomerDto>.Ok(customer, "Customer created successfully."));
    }

    /// <summary>
    /// Updates customer demographic and credit profile information.
    /// </summary>
    [HttpPut("{id}")]
    [HasPermission(AppPermissions.Crm.CustomersUpdate)]
    [ProducesResponseType(typeof(ApiResponse<CustomerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCustomer(
        string id,
        [FromBody] UpdateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new Application.Common.Exceptions.ValidationException(validation.Errors);
        }

        var customer = await _customerService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<CustomerDto>.Ok(customer, "Customer updated successfully."));
    }

    /// <summary>
    /// Activates or deactivates a customer profile.
    /// </summary>
    [HttpPatch("{id}/status")]
    [HasPermission(AppPermissions.Crm.CustomersStatusManage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        string id,
        [FromBody] UpdateCustomerStatusRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _statusValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new Application.Common.Exceptions.ValidationException(validation.Errors);
        }

        await _customerService.UpdateStatusAsync(id, request, cancellationToken);
        return Ok(ApiResponse.Ok("Customer status updated successfully."));
    }

    /// <summary>
    /// Deactivates a customer, verifying no active historical business dependencies exist.
    /// </summary>
    [HttpDelete("{id}")]
    [HasPermission(AppPermissions.Crm.CustomersDelete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCustomer(
        string id,
        CancellationToken cancellationToken)
    {
        await _customerService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok("Customer deactivated successfully."));
    }
}
