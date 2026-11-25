using Logistics.Server.Application.Common.Models;
using Logistics.Server.Application.Modules.Notification.DTOs;
using Logistics.Shared.Contracts.Pagination;

namespace Logistics.Server.Application.Modules.Notification.Services;

public interface INotificationService
{
    Task<NotificationDto> CreateForUserAsync(
        long userId,
        string title,
        string content,
        string? notificationType = null,
        string channel = "InApp",
        string priority = "Normal",
        string? referenceType = null,
        long? referenceId = null,
        long? branchId = null,
        string? eventKey = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationDto>> CreateForUsersAsync(
        IEnumerable<long> userIds,
        string title,
        string content,
        string? notificationType = null,
        string channel = "InApp",
        string priority = "Normal",
        string? referenceType = null,
        long? referenceId = null,
        long? branchId = null,
        string? eventKeyPrefix = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationDto>> CreateForBranchAsync(
        long branchId,
        string title,
        string content,
        string? notificationType = null,
        string channel = "InApp",
        string priority = "Normal",
        string? referenceType = null,
        long? referenceId = null,
        string? eventKeyPrefix = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationDto>> CreateSystemNotificationAsync(
        string title,
        string content,
        string? notificationType = null,
        string priority = "High",
        string? referenceType = null,
        long? referenceId = null,
        string? eventKeyPrefix = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationDto>> CreateAsync(CreateNotificationRequest request, CancellationToken cancellationToken = default);

    Task<PagedResult<NotificationDto>> GetMyNotificationsAsync(NotificationFilterRequest filter, PagedRequest pagedRequest, CancellationToken cancellationToken = default);

    Task<PagedResult<NotificationDto>> GetNotificationsAsync(long? recipientUserId, NotificationFilterRequest filter, PagedRequest pagedRequest, CancellationToken cancellationToken = default);

    Task<NotificationDetailDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<UnreadCountDto> GetUnreadCountAsync(CancellationToken cancellationToken = default);

    Task<NotificationDto> MarkAsReadAsync(long id, CancellationToken cancellationToken = default);

    Task<int> MarkAllAsReadAsync(CancellationToken cancellationToken = default);
}

public interface INotificationTemplateService
{
    Task<PagedResult<NotificationTemplateDto>> GetTemplatesAsync(string? search, string? channel, bool? isActive, PagedRequest pagedRequest, CancellationToken cancellationToken = default);

    Task<NotificationTemplateDto?> GetByIdOrCodeAsync(string identifier, CancellationToken cancellationToken = default);

    Task<NotificationTemplateDto> CreateAsync(CreateNotificationTemplateRequest request, CancellationToken cancellationToken = default);

    Task<NotificationTemplateDto> UpdateAsync(long id, UpdateNotificationTemplateRequest request, CancellationToken cancellationToken = default);

    Task<NotificationTemplateDto> SetActiveStatusAsync(long id, bool isActive, CancellationToken cancellationToken = default);

    Task<RenderedTemplateDto> PreviewAsync(long id, PreviewTemplateRequest request, CancellationToken cancellationToken = default);
}

public interface INotificationTemplateRenderer
{
    RenderedTemplateDto Render(
        string? subjectTemplate,
        string bodyTemplate,
        IReadOnlyCollection<string>? approvedPlaceholders,
        IReadOnlyDictionary<string, string>? parameters,
        bool htmlEscapeBody = false);
}

public interface INotificationDeliveryService
{
    Task<int> ProcessPendingDeliveriesAsync(int batchSize = 50, CancellationToken cancellationToken = default);

    Task<NotificationDetailDto> DeliverNotificationAsync(long notificationId, CancellationToken cancellationToken = default);
}

public interface IEmailProvider
{
    string ProviderName { get; }
    Task<EmailSendResult> SendAsync(EmailSendRequest request, CancellationToken cancellationToken = default);
}

