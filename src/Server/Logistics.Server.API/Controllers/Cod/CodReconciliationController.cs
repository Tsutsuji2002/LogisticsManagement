using Logistics.Server.API.Authorization;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Cod.DTOs;
using Logistics.Server.Application.Modules.Cod.Services;
using Logistics.Shared.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Logistics.Server.API.Controllers.Cod;

/// <summary>
/// Cash on Delivery (COD) reconciliation: expected vs collected vs deposited vs remitted, and discrepancy analysis.
/// </summary>
[Route("api/v1/cod/reconciliation")]
public class CodReconciliationController : BaseApiController
{
    private readonly ICodReconciliationService _reconciliationService;

    public CodReconciliationController(ICodReconciliationService reconciliationService)
    {
        _reconciliationService = reconciliationService;
    }

    /// <summary>
    /// High-level COD reconciliation overview across the organization or for a specific branch.
    /// </summary>
    [HttpGet("summary")]
    [HasPermission(AppPermissions.Cod.ReconciliationRead)]
    [ProducesResponseType(typeof(ApiResponse<CodReconciliationSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(
        [FromQuery] long? branchId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        CancellationToken cancellationToken = default)
    {
        var summary = await _reconciliationService.GetSummaryAsync(branchId, fromDate, toDate, cancellationToken);
        return Ok(ApiResponse<CodReconciliationSummaryDto>.Ok(summary));
    }

    /// <summary>
    /// Reconciles collections, deposits, and outstanding cash in hand for a specific employee/driver.
    /// </summary>
    [HttpGet("employees/{employeeId:long}")]
    [HasPermission(AppPermissions.Cod.ReconciliationRead)]
    [ProducesResponseType(typeof(ApiResponse<EmployeeCodReconciliationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReconcileEmployee(
        long employeeId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        CancellationToken cancellationToken = default)
    {
        var result = await _reconciliationService.ReconcileEmployeeAsync(employeeId, fromDate, toDate, cancellationToken);
        return Ok(ApiResponse<EmployeeCodReconciliationDto>.Ok(result));
    }

    /// <summary>
    /// Reconciles driver deposits, cashier verified amounts, and pending remittances for a specific branch.
    /// </summary>
    [HttpGet("branches/{branchId:long}")]
    [HasPermission(AppPermissions.Cod.ReconciliationRead)]
    [ProducesResponseType(typeof(ApiResponse<BranchCodReconciliationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReconcileBranch(
        long branchId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        CancellationToken cancellationToken = default)
    {
        var result = await _reconciliationService.ReconcileBranchAsync(branchId, fromDate, toDate, cancellationToken);
        return Ok(ApiResponse<BranchCodReconciliationDto>.Ok(result));
    }

    /// <summary>
    /// Validates itemized sum against header amounts for a specific remittance.
    /// </summary>
    [HttpGet("remittances/{remittanceId:long}")]
    [HasPermission(AppPermissions.Cod.ReconciliationRead)]
    [ProducesResponseType(typeof(ApiResponse<RemittanceReconciliationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReconcileRemittance(long remittanceId, CancellationToken cancellationToken)
    {
        var result = await _reconciliationService.ReconcileRemittanceAsync(remittanceId, cancellationToken);
        return Ok(ApiResponse<RemittanceReconciliationDto>.Ok(result));
    }
}
