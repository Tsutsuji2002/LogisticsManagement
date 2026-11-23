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
/// Payment receipt recording, invoice allocation, and allocation reversal management.
/// </summary>
[Route("api/v1/finance/payments")]
public class PaymentsController : BaseApiController
{
    private readonly IPaymentService _paymentService;
    private readonly IValidator<RegisterPaymentRequest> _registerValidator;
    private readonly IValidator<CreatePaymentAllocationRequest> _allocationValidator;

    public PaymentsController(
        IPaymentService paymentService,
        IValidator<RegisterPaymentRequest> registerValidator,
        IValidator<CreatePaymentAllocationRequest> allocationValidator)
    {
        _paymentService = paymentService;
        _registerValidator = registerValidator;
        _allocationValidator = allocationValidator;
    }

    [HttpGet]
    [HasPermission(AppPermissions.Finance.PaymentsRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<PaymentDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPayments(
        [FromQuery] long? customerId,
        [FromQuery] string? status,
        [FromQuery] string? paymentMethod,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _paymentService.GetPaymentsAsync(customerId, status, paymentMethod, new PagedRequest { Page = page, PageSize = pageSize }, cancellationToken);
        return Ok(ApiResponse<PagedResponse<PaymentDto>>.Ok(result.ToPagedResponse()));
    }

    [HttpGet("{id:long}")]
    [HasPermission(AppPermissions.Finance.PaymentsRead)]
    [ProducesResponseType(typeof(ApiResponse<PaymentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPaymentById(long id, CancellationToken cancellationToken)
    {
        var payment = await _paymentService.GetPaymentByIdAsync(id, cancellationToken);
        if (payment == null) throw new NotFoundException("Payment", id.ToString());
        return Ok(ApiResponse<PaymentDto>.Ok(payment));
    }

    [HttpPost]
    [HasPermission(AppPermissions.Finance.PaymentsManage)]
    [ProducesResponseType(typeof(ApiResponse<PaymentDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterPayment([FromBody] RegisterPaymentRequest request, CancellationToken cancellationToken)
    {
        var val = await _registerValidator.ValidateAsync(request, cancellationToken);
        if (!val.IsValid) throw new ValidationException(val.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

        var created = await _paymentService.RegisterPaymentAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetPaymentById), new { id = created.Id }, ApiResponse<PaymentDto>.Ok(created, "Payment registered successfully."));
    }

    [HttpPost("{id:long}/allocations")]
    [HasPermission(AppPermissions.Finance.PaymentsAllocate)]
    [ProducesResponseType(typeof(ApiResponse<PaymentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AllocatePayment(long id, [FromBody] CreatePaymentAllocationRequest request, CancellationToken cancellationToken)
    {
        var val = await _allocationValidator.ValidateAsync(request, cancellationToken);
        if (!val.IsValid) throw new ValidationException(val.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

        var updated = await _paymentService.AllocatePaymentAsync(id, request, cancellationToken);
        return Ok(ApiResponse<PaymentDto>.Ok(updated, "Payment allocated to invoice successfully."));
    }

    [HttpPost("{id:long}/allocations/{allocationId:long}/reverse")]
    [HasPermission(AppPermissions.Finance.PaymentsReverse)]
    [ProducesResponseType(typeof(ApiResponse<PaymentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReverseAllocation(long id, long allocationId, CancellationToken cancellationToken)
    {
        var updated = await _paymentService.ReverseAllocationAsync(id, allocationId, cancellationToken);
        return Ok(ApiResponse<PaymentDto>.Ok(updated, "Payment allocation reversed successfully."));
    }
}
