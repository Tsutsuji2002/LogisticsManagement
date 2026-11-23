using Logistics.Server.API.Authorization;
using Logistics.Server.Application.Common.Exceptions;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Finance.DTOs;
using Logistics.Server.Application.Modules.Finance.Services;
using Logistics.Shared.Contracts;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace Logistics.Server.API.Controllers.Finance;

/// <summary>
/// Customer credit balances, outstanding receivables, unbilled charges, and reconciliation.
/// </summary>
[Route("api/v1/finance/customer-balances")]
public class CustomerBalancesController : BaseApiController
{
    private readonly ICustomerBalanceService _customerBalanceService;

    public CustomerBalancesController(ICustomerBalanceService customerBalanceService)
    {
        _customerBalanceService = customerBalanceService;
    }

    [HttpGet]
    [HasPermission(AppPermissions.Finance.BalancesRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<CustomerBalanceDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBalances(
        [FromQuery] long? branchId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _customerBalanceService.GetBalancesAsync(branchId, new PagedRequest { Page = page, PageSize = pageSize }, cancellationToken);
        return Ok(ApiResponse<PagedResponse<CustomerBalanceDto>>.Ok(result.ToPagedResponse()));
    }

    [HttpGet("{customerId:long}")]
    [HasPermission(AppPermissions.Finance.BalancesRead)]
    [ProducesResponseType(typeof(ApiResponse<CustomerBalanceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCustomerBalance(long customerId, CancellationToken cancellationToken)
    {
        var balance = await _customerBalanceService.GetCustomerBalanceAsync(customerId, cancellationToken);
        if (balance == null) throw new NotFoundException("CustomerBalance", customerId.ToString());
        return Ok(ApiResponse<CustomerBalanceDto>.Ok(balance));
    }

    [HttpPost("{customerId:long}/reconcile")]
    [HasPermission(AppPermissions.Finance.BalancesReconcile)]
    [ProducesResponseType(typeof(ApiResponse<CustomerBalanceReconciliationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReconcileBalance(long customerId, CancellationToken cancellationToken)
    {
        var report = await _customerBalanceService.ReconcileCustomerBalanceAsync(customerId, cancellationToken);
        return Ok(ApiResponse<CustomerBalanceReconciliationDto>.Ok(report, "Customer balance reconciled successfully."));
    }
}
