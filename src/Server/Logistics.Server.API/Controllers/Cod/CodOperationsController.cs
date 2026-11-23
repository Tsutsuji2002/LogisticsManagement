using Logistics.Server.API.Authorization;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Cod.DTOs;
using Logistics.Server.Application.Modules.Cod.Services;
using Logistics.Shared.Contracts;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace Logistics.Server.API.Controllers.Cod;

/// <summary>
/// Employee and Driver COD operations: assigned collections, cash holdings, settlement summaries.
/// </summary>
[Route("api/v1/cod/operations")]
public class CodOperationsController : BaseApiController
{
    private readonly ICodEmployeeService _employeeService;

    public CodOperationsController(ICodEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    /// <summary>
    /// Returns assigned collections for current authenticated employee/driver.
    /// </summary>
    [HttpGet("my-collections")]
    [HasPermission(AppPermissions.Cod.OperationsReadOwn)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<CodCollectionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyCollections(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _employeeService.GetMyCollectionsAsync(
            status,
            new PagedRequest { Page = page, PageSize = pageSize },
            cancellationToken);

        return Ok(ApiResponse<PagedResponse<CodCollectionDto>>.Ok(result.ToPagedResponse()));
    }

    /// <summary>
    /// Returns COD holding and pending summary for current authenticated employee/driver.
    /// </summary>
    [HttpGet("my-summary")]
    [HasPermission(AppPermissions.Cod.OperationsReadOwn)]
    [ProducesResponseType(typeof(ApiResponse<EmployeeCodSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMySummary(CancellationToken cancellationToken)
    {
        var summary = await _employeeService.GetMySummaryAsync(cancellationToken);
        return Ok(ApiResponse<EmployeeCodSummaryDto>.Ok(summary));
    }

    /// <summary>
    /// Returns assigned collections for a specific employee/driver (Branch or higher permission required).
    /// </summary>
    [HttpGet("employees/{employeeId:long}/collections")]
    [HasPermission(AppPermissions.Cod.OperationsReadBranch)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<CodCollectionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEmployeeCollections(
        long employeeId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _employeeService.GetEmployeeCollectionsAsync(
            employeeId,
            status,
            new PagedRequest { Page = page, PageSize = pageSize },
            cancellationToken);

        return Ok(ApiResponse<PagedResponse<CodCollectionDto>>.Ok(result.ToPagedResponse()));
    }

    /// <summary>
    /// Returns COD holding and pending summary for a specific employee/driver.
    /// </summary>
    [HttpGet("employees/{employeeId:long}/summary")]
    [HasPermission(AppPermissions.Cod.OperationsReadBranch)]
    [ProducesResponseType(typeof(ApiResponse<EmployeeCodSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEmployeeSummary(long employeeId, CancellationToken cancellationToken)
    {
        var summary = await _employeeService.GetEmployeeSummaryAsync(employeeId, cancellationToken);
        return Ok(ApiResponse<EmployeeCodSummaryDto>.Ok(summary));
    }
}
