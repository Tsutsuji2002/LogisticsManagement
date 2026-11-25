using FluentValidation;
using Logistics.Server.API.Authorization;
using Logistics.Server.Application.Common.Exceptions;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.BackgroundProcessing.DTOs;
using Logistics.Server.Application.Modules.BackgroundProcessing.Services;
using Logistics.Shared.Contracts;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.AspNetCore.Mvc;
using ValidationException = Logistics.Server.Application.Common.Exceptions.ValidationException;

namespace Logistics.Server.API.Controllers.BackgroundProcessing;

/// <summary>
/// Scheduled report configurations, report definitions, manual execution triggers, and history.
/// </summary>
[Route("api/v1/scheduled-reports")]
public class ScheduledReportsController : BaseApiController
{
    private readonly IScheduledReportService _reportService;
    private readonly IValidator<CreateScheduledReportRequest> _createValidator;
    private readonly IValidator<UpdateScheduledReportRequest> _updateValidator;

    public ScheduledReportsController(
        IScheduledReportService reportService,
        IValidator<CreateScheduledReportRequest> createValidator,
        IValidator<UpdateScheduledReportRequest> updateValidator)
    {
        _reportService = reportService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet("definitions")]
    [HasPermission(AppPermissions.Notification.ReportsRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<ReportDefinitionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDefinitions(
        [FromQuery] string? category,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _reportService.GetDefinitionsAsync(
            category, isActive, new PagedRequest { Page = page, PageSize = pageSize }, cancellationToken);
        return Ok(ApiResponse<PagedResponse<ReportDefinitionDto>>.Ok(result.ToPagedResponse()));
    }

    [HttpGet("definitions/{id:long}")]
    [HasPermission(AppPermissions.Notification.ReportsRead)]
    [ProducesResponseType(typeof(ApiResponse<ReportDefinitionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDefinitionById(long id, CancellationToken cancellationToken)
    {
        var def = await _reportService.GetDefinitionByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("ReportDefinition", id);
        return Ok(ApiResponse<ReportDefinitionDto>.Ok(def));
    }

    [HttpGet]
    [HasPermission(AppPermissions.Notification.ReportsRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<ScheduledReportDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetScheduledReports(
        [FromQuery] long? branchId,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _reportService.GetScheduledReportsAsync(
            branchId, isActive, new PagedRequest { Page = page, PageSize = pageSize }, cancellationToken);
        return Ok(ApiResponse<PagedResponse<ScheduledReportDto>>.Ok(result.ToPagedResponse()));
    }

    [HttpGet("{id:long}")]
    [HasPermission(AppPermissions.Notification.ReportsRead)]
    [ProducesResponseType(typeof(ApiResponse<ScheduledReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetScheduledReportById(long id, CancellationToken cancellationToken)
    {
        var report = await _reportService.GetScheduledReportByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("ScheduledReport", id);
        return Ok(ApiResponse<ScheduledReportDto>.Ok(report));
    }

    [HttpPost]
    [HasPermission(AppPermissions.Notification.ReportsCreate)]
    [ProducesResponseType(typeof(ApiResponse<ScheduledReportDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateScheduledReportRequest request, CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors.ToDictionary(e => e.PropertyName, e => new[] { e.ErrorMessage }));

        var created = await _reportService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetScheduledReportById), new { id = created.Id },
            ApiResponse<ScheduledReportDto>.Ok(created, "Scheduled report created."));
    }

    [HttpPut("{id:long}")]
    [HasPermission(AppPermissions.Notification.ReportsUpdate)]
    [ProducesResponseType(typeof(ApiResponse<ScheduledReportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateScheduledReportRequest request, CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors.ToDictionary(e => e.PropertyName, e => new[] { e.ErrorMessage }));

        var updated = await _reportService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<ScheduledReportDto>.Ok(updated, "Scheduled report updated."));
    }

    [HttpPatch("{id:long}/status")]
    [HasPermission(AppPermissions.Notification.ReportsManage)]
    [ProducesResponseType(typeof(ApiResponse<ScheduledReportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateStatus(long id, [FromBody] UpdateScheduledReportStatusRequest request, CancellationToken cancellationToken)
    {
        var updated = await _reportService.SetActiveStatusAsync(id, request.IsActive, cancellationToken);
        return Ok(ApiResponse<ScheduledReportDto>.Ok(updated, "Scheduled report status updated."));
    }

    [HttpPost("{id:long}/run")]
    [HasPermission(AppPermissions.Notification.ReportsManage)]
    [ProducesResponseType(typeof(ApiResponse<ScheduledReportRunResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> TriggerRun(long id, CancellationToken cancellationToken)
    {
        var result = await _reportService.TriggerRunAsync(id, cancellationToken);
        return Ok(ApiResponse<ScheduledReportRunResultDto>.Ok(result, result.Message));
    }

    [HttpGet("{id:long}/history")]
    [HasPermission(AppPermissions.Notification.ReportsRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<BackgroundJobLogDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExecutionHistory(
        long id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var history = await _reportService.GetExecutionHistoryAsync(id, new PagedRequest { Page = page, PageSize = pageSize }, cancellationToken);
        return Ok(ApiResponse<PagedResponse<BackgroundJobLogDto>>.Ok(history.ToPagedResponse()));
    }
}

