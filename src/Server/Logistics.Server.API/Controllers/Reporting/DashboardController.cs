using FluentValidation;
using Logistics.Server.API.Authorization;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Reporting.DTOs;
using Logistics.Server.Application.Modules.Reporting.Services;
using Logistics.Shared.Contracts;
using Microsoft.AspNetCore.Mvc;
using ValidationException = Logistics.Server.Application.Common.Exceptions.ValidationException;

namespace Logistics.Server.API.Controllers.Reporting;

/// <summary>
/// Read-only KPI and operational/financial dashboard endpoints scoped by role permissions and branch access.
/// </summary>
[Route("api/v1/dashboard")]
public class DashboardController : BaseApiController
{
    private readonly IDashboardQueryService _dashboardService;
    private readonly IValidator<ReportFilterRequest> _filterValidator;

    public DashboardController(
        IDashboardQueryService dashboardService,
        IValidator<ReportFilterRequest> filterValidator)
    {
        _dashboardService = dashboardService;
        _filterValidator = filterValidator;
    }

    [HttpGet("overview")]
    [HasPermission(AppPermissions.Reporting.DashboardCompanyRead)]
    [ProducesResponseType(typeof(ApiResponse<CompanyOverviewDashboardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetCompanyOverview(
        [FromQuery] ReportFilterRequest filter,
        CancellationToken cancellationToken)
    {
        await ValidateFilterAsync(filter, cancellationToken);
        var data = await _dashboardService.GetCompanyOverviewAsync(filter, cancellationToken);
        return Ok(ApiResponse<CompanyOverviewDashboardDto>.Ok(data));
    }

    [HttpGet("branches/{branchId:long}")]
    [HasPermission(AppPermissions.Reporting.DashboardBranchRead)]
    [ProducesResponseType(typeof(ApiResponse<BranchDashboardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBranchDashboard(
        long branchId,
        [FromQuery] ReportFilterRequest filter,
        CancellationToken cancellationToken)
    {
        await ValidateFilterAsync(filter, cancellationToken);
        var data = await _dashboardService.GetBranchDashboardAsync(branchId, filter, cancellationToken);
        return Ok(ApiResponse<BranchDashboardDto>.Ok(data));
    }

    [HttpGet("operations")]
    [HasPermission(AppPermissions.Reporting.DashboardRead)]
    [ProducesResponseType(typeof(ApiResponse<OperationsDashboardDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOperationsDashboard(
        [FromQuery] ReportFilterRequest filter,
        CancellationToken cancellationToken)
    {
        await ValidateFilterAsync(filter, cancellationToken);
        var data = await _dashboardService.GetOperationsDashboardAsync(filter, cancellationToken);
        return Ok(ApiResponse<OperationsDashboardDto>.Ok(data));
    }

    [HttpGet("shipments")]
    [HasPermission(AppPermissions.Reporting.ShipmentsRead)]
    [ProducesResponseType(typeof(ApiResponse<ShipmentDashboardDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetShipmentsDashboard(
        [FromQuery] ReportFilterRequest filter,
        CancellationToken cancellationToken)
    {
        await ValidateFilterAsync(filter, cancellationToken);
        var data = await _dashboardService.GetShipmentDashboardAsync(filter, cancellationToken);
        return Ok(ApiResponse<ShipmentDashboardDto>.Ok(data));
    }

    [HttpGet("delivery-performance")]
    [HasPermission(AppPermissions.Reporting.DeliveryRead)]
    [ProducesResponseType(typeof(ApiResponse<DeliveryPerformanceDashboardDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDeliveryPerformanceDashboard(
        [FromQuery] ReportFilterRequest filter,
        CancellationToken cancellationToken)
    {
        await ValidateFilterAsync(filter, cancellationToken);
        var data = await _dashboardService.GetDeliveryPerformanceDashboardAsync(filter, cancellationToken);
        return Ok(ApiResponse<DeliveryPerformanceDashboardDto>.Ok(data));
    }

    [HttpGet("orders")]
    [HasPermission(AppPermissions.Reporting.OrdersRead)]
    [ProducesResponseType(typeof(ApiResponse<OrdersDashboardDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrdersDashboard(
        [FromQuery] ReportFilterRequest filter,
        CancellationToken cancellationToken)
    {
        await ValidateFilterAsync(filter, cancellationToken);
        var data = await _dashboardService.GetOrdersDashboardAsync(filter, cancellationToken);
        return Ok(ApiResponse<OrdersDashboardDto>.Ok(data));
    }

    [HttpGet("warehouse")]
    [HasPermission(AppPermissions.Reporting.WarehouseRead)]
    [ProducesResponseType(typeof(ApiResponse<WarehouseDashboardDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWarehouseDashboard(
        [FromQuery] ReportFilterRequest filter,
        CancellationToken cancellationToken)
    {
        await ValidateFilterAsync(filter, cancellationToken);
        var data = await _dashboardService.GetWarehouseDashboardAsync(filter, cancellationToken);
        return Ok(ApiResponse<WarehouseDashboardDto>.Ok(data));
    }

    [HttpGet("inventory")]
    [HasPermission(AppPermissions.Reporting.InventoryRead)]
    [ProducesResponseType(typeof(ApiResponse<InventoryDashboardDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInventoryDashboard(
        [FromQuery] ReportFilterRequest filter,
        CancellationToken cancellationToken)
    {
        await ValidateFilterAsync(filter, cancellationToken);
        var data = await _dashboardService.GetInventoryDashboardAsync(filter, cancellationToken);
        return Ok(ApiResponse<InventoryDashboardDto>.Ok(data));
    }

    [HttpGet("finance")]
    [HasPermission(AppPermissions.Reporting.FinanceRead)]
    [ProducesResponseType(typeof(ApiResponse<FinanceDashboardDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFinanceDashboard(
        [FromQuery] ReportFilterRequest filter,
        CancellationToken cancellationToken)
    {
        await ValidateFilterAsync(filter, cancellationToken);
        var data = await _dashboardService.GetFinanceDashboardAsync(filter, cancellationToken);
        return Ok(ApiResponse<FinanceDashboardDto>.Ok(data));
    }

    [HttpGet("cod")]
    [HasPermission(AppPermissions.Reporting.CodRead)]
    [ProducesResponseType(typeof(ApiResponse<CodDashboardDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCodDashboard(
        [FromQuery] ReportFilterRequest filter,
        CancellationToken cancellationToken)
    {
        await ValidateFilterAsync(filter, cancellationToken);
        var data = await _dashboardService.GetCodDashboardAsync(filter, cancellationToken);
        return Ok(ApiResponse<CodDashboardDto>.Ok(data));
    }

    private async Task ValidateFilterAsync(ReportFilterRequest filter, CancellationToken cancellationToken)
    {
        var result = await _filterValidator.ValidateAsync(filter, cancellationToken);
        if (!result.IsValid)
        {
            throw new ValidationException(result.Errors.ToDictionary(e => e.PropertyName, e => new[] { e.ErrorMessage }));
        }
    }
}
