using FluentValidation;
using Logistics.Server.API.Authorization;
using Logistics.Server.Application.Common.Exceptions;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Notification.DTOs;
using Logistics.Server.Application.Modules.Notification.Services;
using Logistics.Shared.Contracts;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.AspNetCore.Mvc;
using ValidationException = Logistics.Server.Application.Common.Exceptions.ValidationException;

namespace Logistics.Server.API.Controllers.Notifications;

/// <summary>
/// Notification template CRUD, activation control, and safe placeholder preview rendering.
/// </summary>
[Route("api/v1/notification-templates")]
public class NotificationTemplatesController : BaseApiController
{
    private readonly INotificationTemplateService _templateService;
    private readonly IValidator<CreateNotificationTemplateRequest> _createValidator;
    private readonly IValidator<UpdateNotificationTemplateRequest> _updateValidator;

    public NotificationTemplatesController(
        INotificationTemplateService templateService,
        IValidator<CreateNotificationTemplateRequest> createValidator,
        IValidator<UpdateNotificationTemplateRequest> updateValidator)
    {
        _templateService = templateService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet]
    [HasPermission(AppPermissions.Notification.TemplatesRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<NotificationTemplateDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTemplates(
        [FromQuery] string? search,
        [FromQuery] string? channel,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _templateService.GetTemplatesAsync(
            search, channel, isActive, new PagedRequest { Page = page, PageSize = pageSize }, cancellationToken);

        return Ok(ApiResponse<PagedResponse<NotificationTemplateDto>>.Ok(result.ToPagedResponse()));
    }

    [HttpGet("{identifier}")]
    [HasPermission(AppPermissions.Notification.TemplatesRead)]
    [ProducesResponseType(typeof(ApiResponse<NotificationTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdOrCode(string identifier, CancellationToken cancellationToken)
    {
        var template = await _templateService.GetByIdOrCodeAsync(identifier, cancellationToken)
            ?? throw new NotFoundException("NotificationTemplate", identifier);

        return Ok(ApiResponse<NotificationTemplateDto>.Ok(template));
    }

    [HttpPost]
    [HasPermission(AppPermissions.Notification.TemplatesManage)]
    [ProducesResponseType(typeof(ApiResponse<NotificationTemplateDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateNotificationTemplateRequest request, CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors.ToDictionary(e => e.PropertyName, e => new[] { e.ErrorMessage }));

        var created = await _templateService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetByIdOrCode), new { identifier = created.Id.ToString() },
            ApiResponse<NotificationTemplateDto>.Ok(created, "Notification template created."));
    }

    [HttpPut("{id:long}")]
    [HasPermission(AppPermissions.Notification.TemplatesManage)]
    [ProducesResponseType(typeof(ApiResponse<NotificationTemplateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateNotificationTemplateRequest request, CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors.ToDictionary(e => e.PropertyName, e => new[] { e.ErrorMessage }));

        var updated = await _templateService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<NotificationTemplateDto>.Ok(updated, "Notification template updated."));
    }

    [HttpPatch("{id:long}/status")]
    [HasPermission(AppPermissions.Notification.TemplatesManage)]
    [ProducesResponseType(typeof(ApiResponse<NotificationTemplateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateStatus(long id, [FromBody] UpdateTemplateStatusRequest request, CancellationToken cancellationToken)
    {
        var updated = await _templateService.SetActiveStatusAsync(id, request.IsActive, cancellationToken);
        return Ok(ApiResponse<NotificationTemplateDto>.Ok(updated, "Notification template status updated."));
    }

    [HttpPost("{id:long}/preview")]
    [HasPermission(AppPermissions.Notification.TemplatesRead)]
    [ProducesResponseType(typeof(ApiResponse<RenderedTemplateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Preview(long id, [FromBody] PreviewTemplateRequest request, CancellationToken cancellationToken)
    {
        var rendered = await _templateService.PreviewAsync(id, request, cancellationToken);
        return Ok(ApiResponse<RenderedTemplateDto>.Ok(rendered));
    }
}

