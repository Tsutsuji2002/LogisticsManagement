using Logistics.Server.API.Authorization;
using Logistics.Server.Application.Common.Exceptions;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.BackgroundProcessing.DTOs;
using Logistics.Server.Application.Modules.BackgroundProcessing.Services;
using Logistics.Shared.Contracts;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.AspNetCore.Mvc;

namespace Logistics.Server.API.Controllers.BackgroundProcessing;

/// <summary>
/// Background job monitoring, manual retry/cancellation, outbox inspection, and operational health endpoints.
/// </summary>
[Route("api/v1/background-jobs")]
public class BackgroundJobsController : BaseApiController
{
    private readonly IBackgroundJobService _jobService;
    private readonly IOutboxProcessorService _outboxProcessor;

    public BackgroundJobsController(
        IBackgroundJobService jobService,
        IOutboxProcessorService outboxProcessor)
    {
        _jobService = jobService;
        _outboxProcessor = outboxProcessor;
    }

    [HttpGet]
    [HasPermission(AppPermissions.BackgroundProcessing.JobsRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<BackgroundJobLogDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetJobs(
        [FromQuery] string? jobName,
        [FromQuery] string? status,
        [FromQuery] string? triggerType,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _jobService.GetJobsAsync(
            jobName, status, triggerType, fromDate, toDate,
            new PagedRequest { Page = page, PageSize = pageSize }, cancellationToken);

        return Ok(ApiResponse<PagedResponse<BackgroundJobLogDto>>.Ok(result.ToPagedResponse()));
    }

    [HttpGet("failed")]
    [HasPermission(AppPermissions.BackgroundProcessing.JobsRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<BackgroundJobLogDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFailedJobs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _jobService.GetFailedJobsAsync(new PagedRequest { Page = page, PageSize = pageSize }, cancellationToken);
        return Ok(ApiResponse<PagedResponse<BackgroundJobLogDto>>.Ok(result.ToPagedResponse()));
    }

    [HttpGet("{id:long}")]
    [HasPermission(AppPermissions.BackgroundProcessing.JobsRead)]
    [ProducesResponseType(typeof(ApiResponse<BackgroundJobLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetJobById(long id, CancellationToken cancellationToken)
    {
        var job = await _jobService.GetJobByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("BackgroundJobLog", id);

        return Ok(ApiResponse<BackgroundJobLogDto>.Ok(job));
    }

    [HttpPost("{id:long}/retry")]
    [HasPermission(AppPermissions.BackgroundProcessing.JobsRetry)]
    [ProducesResponseType(typeof(ApiResponse<BackgroundJobLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RetryJob(long id, CancellationToken cancellationToken)
    {
        var job = await _jobService.RetryJobAsync(id, cancellationToken);
        return Ok(ApiResponse<BackgroundJobLogDto>.Ok(job, "Background job scheduled for retry."));
    }

    [HttpPost("{id:long}/cancel")]
    [HasPermission(AppPermissions.BackgroundProcessing.JobsManage)]
    [ProducesResponseType(typeof(ApiResponse<BackgroundJobLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CancelJob(long id, [FromQuery] string? reason, CancellationToken cancellationToken)
    {
        var job = await _jobService.CancelJobAsync(id, reason, cancellationToken);
        return Ok(ApiResponse<BackgroundJobLogDto>.Ok(job, "Pending background job cancelled."));
    }

    [HttpGet("health")]
    [HasPermission(AppPermissions.BackgroundProcessing.OperationsRead)]
    [ProducesResponseType(typeof(ApiResponse<BackgroundProcessingHealthDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOperationalHealth(CancellationToken cancellationToken)
    {
        var health = await _outboxProcessor.GetHealthSummaryAsync(cancellationToken);
        return Ok(ApiResponse<BackgroundProcessingHealthDto>.Ok(health));
    }

    [HttpGet("outbox")]
    [HasPermission(AppPermissions.BackgroundProcessing.OperationsRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<OutboxMessageDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOutboxMessages(
        [FromQuery] string? status,
        [FromQuery] string? eventType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _outboxProcessor.GetOutboxMessagesAsync(
            status, eventType, new PagedRequest { Page = page, PageSize = pageSize }, cancellationToken);
        return Ok(ApiResponse<PagedResponse<OutboxMessageDto>>.Ok(result.ToPagedResponse()));
    }

    [HttpPost("outbox/{id:long}/retry")]
    [HasPermission(AppPermissions.BackgroundProcessing.OperationsManage)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RetryOutboxMessage(long id, CancellationToken cancellationToken)
    {
        await _outboxProcessor.RetryOutboxMessageAsync(id, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { messageId = id }, "Outbox message queued for retry."));
    }
}

