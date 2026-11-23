using FluentValidation;
using Logistics.Server.API.Authorization;
using Logistics.Server.Application.Common.Exceptions;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Finance.DTOs;
using Logistics.Server.Application.Modules.Finance.Services;
using Logistics.Shared.Contracts;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.AspNetCore.Mvc;
using ValidationException = Logistics.Server.Application.Common.Exceptions.ValidationException;

namespace Logistics.Server.API.Controllers.Finance;

/// <summary>
/// Customer billing, invoice generation, issuance, and item adjustments.
/// </summary>
[Route("api/v1/finance/invoices")]
public class InvoicesController : BaseApiController
{
    private readonly IInvoiceService _invoiceService;
    private readonly IValidator<CreateInvoiceRequest> _createInvoiceValidator;
    private readonly IValidator<CreateInvoiceItemRequest> _createItemValidator;
    private readonly IValidator<CancelInvoiceRequest> _cancelValidator;

    public InvoicesController(
        IInvoiceService invoiceService,
        IValidator<CreateInvoiceRequest> createInvoiceValidator,
        IValidator<CreateInvoiceItemRequest> createItemValidator,
        IValidator<CancelInvoiceRequest> cancelValidator)
    {
        _invoiceService = invoiceService;
        _createInvoiceValidator = createInvoiceValidator;
        _createItemValidator = createItemValidator;
        _cancelValidator = cancelValidator;
    }

    [HttpGet]
    [HasPermission(AppPermissions.Finance.InvoicesRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<InvoiceDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInvoices(
        [FromQuery] long? customerId,
        [FromQuery] string? status,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _invoiceService.GetInvoicesAsync(customerId, status, fromDate, toDate, new PagedRequest { Page = page, PageSize = pageSize }, cancellationToken);
        return Ok(ApiResponse<PagedResponse<InvoiceDto>>.Ok(result.ToPagedResponse()));
    }

    [HttpGet("{id:long}")]
    [HasPermission(AppPermissions.Finance.InvoicesRead)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInvoiceById(long id, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceService.GetInvoiceByIdAsync(id, cancellationToken);
        if (invoice == null) throw new NotFoundException("Invoice", id.ToString());
        return Ok(ApiResponse<InvoiceDto>.Ok(invoice));
    }

    [HttpPost]
    [HasPermission(AppPermissions.Finance.InvoicesCreate)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateDraftInvoice([FromBody] CreateInvoiceRequest request, CancellationToken cancellationToken)
    {
        var val = await _createInvoiceValidator.ValidateAsync(request, cancellationToken);
        if (!val.IsValid) throw new ValidationException(val.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

        var invoice = await _invoiceService.CreateDraftInvoiceAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetInvoiceById), new { id = invoice.Id }, ApiResponse<InvoiceDto>.Ok(invoice));
    }

    [HttpPost("{id:long}/items")]
    [HasPermission(AppPermissions.Finance.InvoicesCreate)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddInvoiceItem(long id, [FromBody] CreateInvoiceItemRequest request, CancellationToken cancellationToken)
    {
        var val = await _createItemValidator.ValidateAsync(request, cancellationToken);
        if (!val.IsValid) throw new ValidationException(val.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

        var invoice = await _invoiceService.AddInvoiceItemAsync(id, request, cancellationToken);
        return Ok(ApiResponse<InvoiceDto>.Ok(invoice));
    }

    [HttpDelete("{id:long}/items/{itemId:long}")]
    [HasPermission(AppPermissions.Finance.InvoicesCreate)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveInvoiceItem(long id, long itemId, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceService.RemoveInvoiceItemAsync(id, itemId, cancellationToken);
        return Ok(ApiResponse<InvoiceDto>.Ok(invoice));
    }

    [HttpPost("{id:long}/issue")]
    [HasPermission(AppPermissions.Finance.InvoicesIssue)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> IssueInvoice(long id, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceService.IssueInvoiceAsync(id, cancellationToken);
        return Ok(ApiResponse<InvoiceDto>.Ok(invoice));
    }

    [HttpPost("{id:long}/cancel")]
    [HasPermission(AppPermissions.Finance.InvoicesCancel)]
    [ProducesResponseType(typeof(ApiResponse<InvoiceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CancelInvoice(long id, [FromBody] CancelInvoiceRequest request, CancellationToken cancellationToken)
    {
        var val = await _cancelValidator.ValidateAsync(request, cancellationToken);
        if (!val.IsValid) throw new ValidationException(val.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

        var invoice = await _invoiceService.CancelInvoiceAsync(id, request, cancellationToken);
        return Ok(ApiResponse<InvoiceDto>.Ok(invoice));
    }
}
