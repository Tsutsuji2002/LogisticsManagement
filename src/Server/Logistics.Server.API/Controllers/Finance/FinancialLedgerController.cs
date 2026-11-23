using Logistics.Server.API.Authorization;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Finance.DTOs;
using Logistics.Server.Application.Modules.Finance.Services;
using Logistics.Shared.Contracts;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace Logistics.Server.API.Controllers.Finance;

/// <summary>
/// General financial ledger audit trail, double-entry tracking, and running customer ledger entries.
/// </summary>
[Route("api/v1/finance/ledger")]
public class FinancialLedgerController : BaseApiController
{
    private readonly IFinancialLedgerService _ledgerService;

    public FinancialLedgerController(IFinancialLedgerService ledgerService)
    {
        _ledgerService = ledgerService;
    }

    [HttpGet("entries")]
    [HasPermission(AppPermissions.Finance.LedgerRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<FinancialLedgerEntryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEntries(
        [FromQuery] long? customerId,
        [FromQuery] string? entryType,
        [FromQuery] string? referenceType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _ledgerService.GetLedgerEntriesAsync(customerId, entryType, referenceType, new PagedRequest { Page = page, PageSize = pageSize }, cancellationToken);
        return Ok(ApiResponse<PagedResponse<FinancialLedgerEntryDto>>.Ok(result.ToPagedResponse()));
    }
}
