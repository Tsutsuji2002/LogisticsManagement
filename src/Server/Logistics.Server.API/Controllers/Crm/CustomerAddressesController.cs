using FluentValidation;
using Logistics.Server.API.Authorization;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Crm.DTOs;
using Logistics.Server.Application.Modules.Crm.Services;
using Logistics.Shared.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Logistics.Server.API.Controllers.Crm;

/// <summary>
/// Endpoints for managing multi-role shipping, billing, and warehouse pickup locations for a customer.
/// </summary>
[Route("api/v1/customers/{customerId}/addresses")]
public class CustomerAddressesController : BaseApiController
{
    private readonly ICustomerAddressService _addressService;
    private readonly IValidator<CreateCustomerAddressRequest> _createValidator;
    private readonly IValidator<UpdateCustomerAddressRequest> _updateValidator;

    public CustomerAddressesController(
        ICustomerAddressService addressService,
        IValidator<CreateCustomerAddressRequest> createValidator,
        IValidator<UpdateCustomerAddressRequest> updateValidator)
    {
        _addressService = addressService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// Lists all addresses linked to a customer.
    /// </summary>
    [HttpGet]
    [HasPermission(AppPermissions.Crm.AddressesRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CustomerAddressDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAddresses(
        string customerId,
        [FromQuery] string? addressType,
        [FromQuery] bool? isDefault,
        CancellationToken cancellationToken)
    {
        var addresses = await _addressService.GetAddressesAsync(customerId, addressType, isDefault, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<CustomerAddressDto>>.Ok(addresses));
    }

    /// <summary>
    /// Retrieves a single customer address detail.
    /// </summary>
    [HttpGet("{addressId}")]
    [HasPermission(AppPermissions.Crm.AddressesRead)]
    [ProducesResponseType(typeof(ApiResponse<CustomerAddressDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAddress(
        string customerId,
        string addressId,
        CancellationToken cancellationToken)
    {
        var address = await _addressService.GetByIdOrPublicIdAsync(customerId, addressId, cancellationToken);
        if (address == null)
        {
            return NotFound(ApiResponse<CustomerAddressDto>.Fail($"Address '{addressId}' not found."));
        }
        return Ok(ApiResponse<CustomerAddressDto>.Ok(address));
    }

    /// <summary>
    /// Links a new address to the customer.
    /// </summary>
    [HttpPost]
    [HasPermission(AppPermissions.Crm.AddressesCreate)]
    [ProducesResponseType(typeof(ApiResponse<CustomerAddressDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateAddress(
        string customerId,
        [FromBody] CreateCustomerAddressRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new Application.Common.Exceptions.ValidationException(validation.Errors);
        }

        var address = await _addressService.CreateAsync(customerId, request, cancellationToken);
        return CreatedAtAction(nameof(GetAddress), new { customerId, addressId = address.PublicId }, ApiResponse<CustomerAddressDto>.Ok(address, "Address linked successfully."));
    }

    /// <summary>
    /// Updates address details and metadata for a customer.
    /// </summary>
    [HttpPut("{addressId}")]
    [HasPermission(AppPermissions.Crm.AddressesUpdate)]
    [ProducesResponseType(typeof(ApiResponse<CustomerAddressDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAddress(
        string customerId,
        string addressId,
        [FromBody] UpdateCustomerAddressRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new Application.Common.Exceptions.ValidationException(validation.Errors);
        }

        var address = await _addressService.UpdateAsync(customerId, addressId, request, cancellationToken);
        return Ok(ApiResponse<CustomerAddressDto>.Ok(address, "Address updated successfully."));
    }

    /// <summary>
    /// Sets an address as the default selection for its address type.
    /// </summary>
    [HttpPatch("{addressId}/default")]
    [HasPermission(AppPermissions.Crm.AddressesManage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetDefault(
        string customerId,
        string addressId,
        [FromBody] SetDefaultAddressRequest request,
        CancellationToken cancellationToken)
    {
        await _addressService.SetDefaultAsync(customerId, addressId, request, cancellationToken);
        return Ok(ApiResponse.Ok("Default address flag updated successfully."));
    }

    /// <summary>
    /// Updates address status.
    /// </summary>
    [HttpPatch("{addressId}/status")]
    [HasPermission(AppPermissions.Crm.AddressesManage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        string customerId,
        string addressId,
        [FromBody] UpdateCustomerAddressStatusRequest request,
        CancellationToken cancellationToken)
    {
        await _addressService.UpdateStatusAsync(customerId, addressId, request, cancellationToken);
        return Ok(ApiResponse.Ok("Address status updated successfully."));
    }

    /// <summary>
    /// Removes an address link from the customer.
    /// </summary>
    [HttpDelete("{addressId}")]
    [HasPermission(AppPermissions.Crm.AddressesManage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAddress(
        string customerId,
        string addressId,
        CancellationToken cancellationToken)
    {
        await _addressService.DeleteAsync(customerId, addressId, cancellationToken);
        return Ok(ApiResponse.Ok("Address unlinked successfully."));
    }
}
