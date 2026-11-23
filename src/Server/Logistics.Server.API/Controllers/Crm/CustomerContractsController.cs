using FluentValidation;
using Logistics.Server.API.Authorization;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Crm.DTOs;
using Logistics.Server.Application.Modules.Crm.Services;
using Logistics.Shared.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Logistics.Server.API.Controllers.Crm;

/// <summary>
/// Endpoints for managing commercial service contracts signed with customers.
/// </summary>
[Route("api/v1/customers/{customerId}/contracts")]
public class CustomerContractsController : BaseApiController
{
    private readonly ICustomerContractService _contractService;
    private readonly IValidator<CreateCustomerContractRequest> _createValidator;
    private readonly IValidator<UpdateCustomerContractRequest> _updateValidator;
    private readonly IValidator<UpdateCustomerContractStatusRequest> _statusValidator;

    public CustomerContractsController(
        ICustomerContractService contractService,
        IValidator<CreateCustomerContractRequest> createValidator,
        IValidator<UpdateCustomerContractRequest> updateValidator,
        IValidator<UpdateCustomerContractStatusRequest> statusValidator)
    {
        _contractService = contractService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _statusValidator = statusValidator;
    }

    /// <summary>
    /// Lists contracts established with a customer.
    /// </summary>
    [HttpGet]
    [HasPermission(AppPermissions.Crm.ContractsRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CustomerContractDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetContracts(
        string customerId,
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] bool? activeOnly,
        CancellationToken cancellationToken)
    {
        var contracts = await _contractService.GetContractsAsync(customerId, search, status, activeOnly, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<CustomerContractDto>>.Ok(contracts));
    }

    /// <summary>
    /// Retrieves a single customer contract details.
    /// </summary>
    [HttpGet("{contractId}")]
    [HasPermission(AppPermissions.Crm.ContractsRead)]
    [ProducesResponseType(typeof(ApiResponse<CustomerContractDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetContract(
        string customerId,
        string contractId,
        CancellationToken cancellationToken)
    {
        var contract = await _contractService.GetByIdOrPublicIdAsync(customerId, contractId, cancellationToken);
        if (contract == null)
        {
            return NotFound(ApiResponse<CustomerContractDto>.Fail($"Contract '{contractId}' not found."));
        }
        return Ok(ApiResponse<CustomerContractDto>.Ok(contract));
    }

    /// <summary>
    /// Authors a new commercial contract for a customer.
    /// </summary>
    [HttpPost]
    [HasPermission(AppPermissions.Crm.ContractsCreate)]
    [ProducesResponseType(typeof(ApiResponse<CustomerContractDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateContract(
        string customerId,
        [FromBody] CreateCustomerContractRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new Application.Common.Exceptions.ValidationException(validation.Errors);
        }

        var contract = await _contractService.CreateAsync(customerId, request, cancellationToken);
        return CreatedAtAction(nameof(GetContract), new { customerId, contractId = contract.PublicId }, ApiResponse<CustomerContractDto>.Ok(contract, "Contract created successfully."));
    }

    /// <summary>
    /// Updates terms, dates, and discounts of a customer contract.
    /// </summary>
    [HttpPut("{contractId}")]
    [HasPermission(AppPermissions.Crm.ContractsUpdate)]
    [ProducesResponseType(typeof(ApiResponse<CustomerContractDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateContract(
        string customerId,
        string contractId,
        [FromBody] UpdateCustomerContractRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new Application.Common.Exceptions.ValidationException(validation.Errors);
        }

        var contract = await _contractService.UpdateAsync(customerId, contractId, request, cancellationToken);
        return Ok(ApiResponse<CustomerContractDto>.Ok(contract, "Contract updated successfully."));
    }

    /// <summary>
    /// Transitions contract status (Draft, Active, Suspended, Expired, Terminated).
    /// </summary>
    [HttpPatch("{contractId}/status")]
    [HasPermission(AppPermissions.Crm.ContractsManage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateStatus(
        string customerId,
        string contractId,
        [FromBody] UpdateCustomerContractStatusRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _statusValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new Application.Common.Exceptions.ValidationException(validation.Errors);
        }

        await _contractService.UpdateStatusAsync(customerId, contractId, request, cancellationToken);
        return Ok(ApiResponse.Ok("Contract status updated successfully."));
    }
}
