using FluentValidation;
using Logistics.Server.API.Authorization;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Crm.DTOs;
using Logistics.Server.Application.Modules.Crm.Services;
using Logistics.Shared.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Logistics.Server.API.Controllers.Crm;

/// <summary>
/// Endpoints for managing contact persons associated with a customer.
/// </summary>
[Route("api/v1/customers/{customerId}/contacts")]
public class CustomerContactsController : BaseApiController
{
    private readonly ICustomerContactService _contactService;
    private readonly IValidator<CreateCustomerContactRequest> _createValidator;
    private readonly IValidator<UpdateCustomerContactRequest> _updateValidator;

    public CustomerContactsController(
        ICustomerContactService contactService,
        IValidator<CreateCustomerContactRequest> createValidator,
        IValidator<UpdateCustomerContactRequest> updateValidator)
    {
        _contactService = contactService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// Lists all contacts for a specific customer.
    /// </summary>
    [HttpGet]
    [HasPermission(AppPermissions.Crm.ContactsRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CustomerContactDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetContacts(
        string customerId,
        [FromQuery] string? search,
        [FromQuery] bool? isPrimary,
        CancellationToken cancellationToken)
    {
        var contacts = await _contactService.GetContactsAsync(customerId, search, isPrimary, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<CustomerContactDto>>.Ok(contacts));
    }

    /// <summary>
    /// Retrieves a single customer contact by identifier.
    /// </summary>
    [HttpGet("{contactId}")]
    [HasPermission(AppPermissions.Crm.ContactsRead)]
    [ProducesResponseType(typeof(ApiResponse<CustomerContactDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetContact(
        string customerId,
        string contactId,
        CancellationToken cancellationToken)
    {
        var contact = await _contactService.GetByIdOrPublicIdAsync(customerId, contactId, cancellationToken);
        if (contact == null)
        {
            return NotFound(ApiResponse<CustomerContactDto>.Fail($"Contact '{contactId}' not found."));
        }
        return Ok(ApiResponse<CustomerContactDto>.Ok(contact));
    }

    /// <summary>
    /// Adds a new contact person to the customer.
    /// </summary>
    [HttpPost]
    [HasPermission(AppPermissions.Crm.ContactsCreate)]
    [ProducesResponseType(typeof(ApiResponse<CustomerContactDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateContact(
        string customerId,
        [FromBody] CreateCustomerContactRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new Application.Common.Exceptions.ValidationException(validation.Errors);
        }

        var contact = await _contactService.CreateAsync(customerId, request, cancellationToken);
        return CreatedAtAction(nameof(GetContact), new { customerId, contactId = contact.PublicId }, ApiResponse<CustomerContactDto>.Ok(contact, "Contact created successfully."));
    }

    /// <summary>
    /// Updates details of an existing customer contact.
    /// </summary>
    [HttpPut("{contactId}")]
    [HasPermission(AppPermissions.Crm.ContactsUpdate)]
    [ProducesResponseType(typeof(ApiResponse<CustomerContactDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateContact(
        string customerId,
        string contactId,
        [FromBody] UpdateCustomerContactRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new Application.Common.Exceptions.ValidationException(validation.Errors);
        }

        var contact = await _contactService.UpdateAsync(customerId, contactId, request, cancellationToken);
        return Ok(ApiResponse<CustomerContactDto>.Ok(contact, "Contact updated successfully."));
    }

    /// <summary>
    /// Sets a contact as the primary operational liaison for the customer.
    /// </summary>
    [HttpPatch("{contactId}/primary")]
    [HasPermission(AppPermissions.Crm.ContactsManage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetPrimary(
        string customerId,
        string contactId,
        CancellationToken cancellationToken)
    {
        await _contactService.SetPrimaryAsync(customerId, contactId, cancellationToken);
        return Ok(ApiResponse.Ok("Contact set as primary successfully."));
    }

    /// <summary>
    /// Updates contact status.
    /// </summary>
    [HttpPatch("{contactId}/status")]
    [HasPermission(AppPermissions.Crm.ContactsManage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        string customerId,
        string contactId,
        [FromBody] UpdateContactStatusRequest request,
        CancellationToken cancellationToken)
    {
        await _contactService.UpdateStatusAsync(customerId, contactId, request, cancellationToken);
        return Ok(ApiResponse.Ok("Contact status updated successfully."));
    }

    /// <summary>
    /// Deletes a contact from the customer.
    /// </summary>
    [HttpDelete("{contactId}")]
    [HasPermission(AppPermissions.Crm.ContactsManage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteContact(
        string customerId,
        string contactId,
        CancellationToken cancellationToken)
    {
        await _contactService.DeleteAsync(customerId, contactId, cancellationToken);
        return Ok(ApiResponse.Ok("Contact removed successfully."));
    }
}
