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
/// User in-app notification retrieval, read-state tracking, and administrative notification dispatch.
/// </summary>
[Route("api/v1/notifications")]
public class NotificationsController : BaseApiController
{
    private readonly INotificationService _notificationService;
    private readonly IValidator<CreateNotificationRequest> _createValidator;

    public NotificationsController(
        INotificationService notificationService,
        IValidator<CreateNotificationRequest> createValidator)
    {
        _notificationService = notificationService;
        _createValidator = createValidator;
    }

    /// <summary>
    /// Lists notifications belonging to the currently authenticated user.
    /// </summary>
    [HttpGet]
    [HasPermission(AppPermissions.Notification.NotificationsReadOwn)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<NotificationDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyNotifications(
        [FromQuery] bool? isRead,
        [FromQuery] string? notificationType,
        [FromQuery] string? channel,
        [FromQuery] string? status,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] bool includeExpired = false,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var filter = new NotificationFilterRequest
        {
            IsRead = isRead,
            NotificationType = notificationType,
            Channel = channel,
            Status = status,
            FromDate = fromDate,
            ToDate = toDate,
            IncludeExpired = includeExpired,
            SortBy = sortBy,
            SortDescending = sortDescending
        };

        var result = await _notificationService.GetMyNotificationsAsync(
            filter, new PagedRequest { Page = page, PageSize = pageSize }, cancellationToken);

        return Ok(ApiResponse<PagedResponse<NotificationDto>>.Ok(result.ToPagedResponse()));
    }

    /// <summary>
    /// Administrative view of notifications across recipients and accessible branches.
    /// </summary>
    [HttpGet("all")]
    [HasPermission(AppPermissions.Notification.NotificationsManage)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<NotificationDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllNotifications(
        [FromQuery] long? recipientUserId,
        [FromQuery] long? branchId,
        [FromQuery] bool? isRead,
        [FromQuery] string? notificationType,
        [FromQuery] string? channel,
        [FromQuery] string? status,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var filter = new NotificationFilterRequest
        {
            BranchId = branchId,
            IsRead = isRead,
            NotificationType = notificationType,
            Channel = channel,
            Status = status,
            FromDate = fromDate,
            ToDate = toDate
        };

        var result = await _notificationService.GetNotificationsAsync(
            recipientUserId, filter, new PagedRequest { Page = page, PageSize = pageSize }, cancellationToken);

        return Ok(ApiResponse<PagedResponse<NotificationDto>>.Ok(result.ToPagedResponse()));
    }

    /// <summary>
    /// Returns the unread notification count for the currently authenticated user.
    /// </summary>
    [HttpGet("unread-count")]
    [HasPermission(AppPermissions.Notification.NotificationsReadOwn)]
    [ProducesResponseType(typeof(ApiResponse<UnreadCountDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken)
    {
        var result = await _notificationService.GetUnreadCountAsync(cancellationToken);
        return Ok(ApiResponse<UnreadCountDto>.Ok(result));
    }

    /// <summary>
    /// Retrieves notification details (strictly isolated to owner unless administrative permission is held).
    /// </summary>
    [HttpGet("{id:long}")]
    [HasPermission(AppPermissions.Notification.NotificationsReadOwn)]
    [ProducesResponseType(typeof(ApiResponse<NotificationDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var item = await _notificationService.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Notification", id);

        return Ok(ApiResponse<NotificationDetailDto>.Ok(item));
    }

    /// <summary>
    /// Creates and dispatches notifications to specified users, a branch, or system-wide.
    /// </summary>
    [HttpPost]
    [HasPermission(AppPermissions.Notification.NotificationsManage)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<NotificationDto>>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateNotificationRequest request, CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors.ToDictionary(e => e.PropertyName, e => new[] { e.ErrorMessage }));

        var created = await _notificationService.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<IReadOnlyList<NotificationDto>>.Ok(created, $"Created {created.Count} notification(s)."));
    }

    /// <summary>
    /// Marks a specific notification as read for the owning user.
    /// </summary>
    [HttpPatch("{id:long}/read")]
    [HasPermission(AppPermissions.Notification.NotificationsReadOwn)]
    [ProducesResponseType(typeof(ApiResponse<NotificationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(long id, CancellationToken cancellationToken)
    {
        var result = await _notificationService.MarkAsReadAsync(id, cancellationToken);
        return Ok(ApiResponse<NotificationDto>.Ok(result, "Notification marked as read."));
    }

    /// <summary>
    /// Marks all unread notifications belonging to the current user as read.
    /// </summary>
    [HttpPost("mark-all-read")]
    [HasPermission(AppPermissions.Notification.NotificationsReadOwn)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        var count = await _notificationService.MarkAllAsReadAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { markedCount = count }, $"Marked {count} notification(s) as read."));
    }
}

