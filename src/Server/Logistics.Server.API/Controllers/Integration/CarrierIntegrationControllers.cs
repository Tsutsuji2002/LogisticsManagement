using FluentValidation;
using Logistics.Server.API.Authorization;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Carrier.DTOs;
using Logistics.Server.Application.Modules.Carrier.Services;
using Logistics.Shared.Contracts;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ValidationException = Logistics.Server.Application.Common.Exceptions.ValidationException;

namespace Logistics.Server.API.Controllers.Integration;

/// <summary>
/// Carrier integration operations: booking, cancellation, rate queries, tracking.
/// </summary>
[Route("api/v1/carrier-integration")]
public class CarrierIntegrationController : BaseApiController
{
    private readonly ICarrierIntegrationService _integrationService;
    private readonly IValidator<CarrierBookShipmentRequest> _bookValidator;
    private readonly IValidator<CarrierRateQueryRequest> _rateValidator;

    public CarrierIntegrationController(
        ICarrierIntegrationService integrationService,
        IValidator<CarrierBookShipmentRequest> bookValidator,
        IValidator<CarrierRateQueryRequest> rateValidator)
    {
        _integrationService = integrationService;
        _bookValidator = bookValidator;
        _rateValidator = rateValidator;
    }

    /// <summary>
    /// Books a shipment with its assigned carrier.
    /// </summary>
    [HttpPost("book")]
    [HasPermission(AppPermissions.Carrier.CarriersManage)]
    [ProducesResponseType(typeof(ApiResponse<CarrierBookingResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> BookShipment([FromBody] CarrierBookShipmentRequest request, CancellationToken cancellationToken)
    {
        var validation = await _bookValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

        var result = await _integrationService.BookShipmentAsync(request, cancellationToken);
        return Ok(ApiResponse<CarrierBookingResultDto>.Ok(result));
    }

    /// <summary>
    /// Cancels a carrier booking for a shipment.
    /// </summary>
    [HttpPost("cancel")]
    [HasPermission(AppPermissions.Carrier.CarriersManage)]
    [ProducesResponseType(typeof(ApiResponse<CarrierCancelResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CancelShipment([FromBody] CarrierCancelShipmentRequest request, CancellationToken cancellationToken)
    {
        var result = await _integrationService.CancelShipmentAsync(request, cancellationToken);
        return Ok(ApiResponse<CarrierCancelResultDto>.Ok(result));
    }

    /// <summary>
    /// Calculates shipping rates from a carrier.
    /// </summary>
    [HttpPost("rates")]
    [HasPermission(AppPermissions.Carrier.CarriersRead)]
    [ProducesResponseType(typeof(ApiResponse<CarrierRateResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CalculateRates([FromBody] CarrierRateQueryRequest request, CancellationToken cancellationToken)
    {
        var validation = await _rateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

        var result = await _integrationService.CalculateRatesAsync(request, cancellationToken);
        return Ok(ApiResponse<CarrierRateResultDto>.Ok(result));
    }

    /// <summary>
    /// Gets live tracking from a carrier's API.
    /// </summary>
    [HttpGet("tracking/{carrierCode}/{trackingNumber}")]
    [HasPermission(AppPermissions.Carrier.CarriersRead)]
    [ProducesResponseType(typeof(ApiResponse<CarrierTrackingQueryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTracking(string carrierCode, string trackingNumber, CancellationToken cancellationToken)
    {
        var result = await _integrationService.GetTrackingAsync(carrierCode, trackingNumber, cancellationToken);
        return Ok(ApiResponse<CarrierTrackingQueryDto>.Ok(result));
    }
}

/// <summary>
/// Receives carrier webhook callbacks.
/// </summary>
[Route("api/v1/carrier-webhooks")]
[ApiController]
[AllowAnonymous]
public class CarrierWebhooksController : ControllerBase
{
    private readonly ICarrierWebhookService _webhookService;

    public CarrierWebhooksController(ICarrierWebhookService webhookService)
    {
        _webhookService = webhookService;
    }

    /// <summary>
    /// Receives a webhook callback from a carrier.
    /// </summary>
    [HttpPost("{carrierCode}")]
    [ProducesResponseType(typeof(ApiResponse<CarrierWebhookLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReceiveWebhook(string carrierCode, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);
        var signature = Request.Headers["X-Webhook-Signature"].FirstOrDefault();
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var webhookUrl = $"{Request.Scheme}://{Request.Host}{Request.Path}";

        var log = await _webhookService.ProcessWebhookAsync(carrierCode, payload, signature, clientIp, webhookUrl, cancellationToken);
        return Ok(ApiResponse<CarrierWebhookLogDto>.Ok(log));
    }
}

/// <summary>
/// Carrier monitoring: API logs, webhook logs, sync jobs.
/// </summary>
[Route("api/v1/carrier-monitoring")]
public class CarrierMonitoringController : BaseApiController
{
    private readonly ICarrierApiLogService _apiLogService;
    private readonly ICarrierWebhookService _webhookService;
    private readonly ICarrierSyncJobService _syncJobService;

    public CarrierMonitoringController(
        ICarrierApiLogService apiLogService,
        ICarrierWebhookService webhookService,
        ICarrierSyncJobService syncJobService)
    {
        _apiLogService = apiLogService;
        _webhookService = webhookService;
        _syncJobService = syncJobService;
    }

    /// <summary>
    /// Gets carrier API call logs.
    /// </summary>
    [HttpGet("api-logs")]
    [HasPermission(AppPermissions.Carrier.CarriersRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<CarrierApiLogDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetApiLogs(
        [FromQuery] string? carrierIdentifier,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] bool? isSuccess,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _apiLogService.GetApiLogsAsync(carrierIdentifier, from, to, isSuccess,
            new PagedRequest { Page = page, PageSize = pageSize }, cancellationToken);
        return Ok(ApiResponse<PagedResponse<CarrierApiLogDto>>.Ok(result.ToPagedResponse()));
    }

    /// <summary>
    /// Gets carrier webhook logs.
    /// </summary>
    [HttpGet("webhook-logs")]
    [HasPermission(AppPermissions.Carrier.CarriersRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<CarrierWebhookLogDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWebhookLogs(
        [FromQuery] string? carrierIdentifier,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] bool? processedSuccessfully,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _webhookService.GetWebhookLogsAsync(carrierIdentifier, from, to, processedSuccessfully,
            new PagedRequest { Page = page, PageSize = pageSize }, cancellationToken);
        return Ok(ApiResponse<PagedResponse<CarrierWebhookLogDto>>.Ok(result.ToPagedResponse()));
    }

    /// <summary>
    /// Gets carrier sync job history.
    /// </summary>
    [HttpGet("sync-jobs")]
    [HasPermission(AppPermissions.Carrier.CarriersRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<CarrierSyncJobDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSyncJobs(
        [FromQuery] string? carrierIdentifier,
        [FromQuery] string? jobType, [FromQuery] string? status,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _syncJobService.GetSyncJobsAsync(carrierIdentifier, jobType, status,
            new PagedRequest { Page = page, PageSize = pageSize }, cancellationToken);
        return Ok(ApiResponse<PagedResponse<CarrierSyncJobDto>>.Ok(result.ToPagedResponse()));
    }
}
