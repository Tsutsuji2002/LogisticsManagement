namespace Logistics.Client.Application.Contracts.Notification;

public record NotificationDto
{
    public long Id { get; init; }
    public Guid PublicId { get; init; }
    public string? TemplateCode { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string Channel { get; init; } = "InApp";
    public string Priority { get; init; } = "Normal";
    public string Status { get; init; } = "Pending";
    public string? NotificationType { get; init; }
    public bool IsRead { get; init; }
    public DateTime? ReadAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record UnreadCountDto
{
    public long UnreadCount { get; init; }
}

public record CreateNotificationRequest
{
    public long? RecipientUserId { get; init; }
    public long? BranchId { get; init; }
    public bool IsSystemBroadcast { get; init; }
    public string? TemplateCode { get; init; }
    public string? Title { get; init; }
    public string? Content { get; init; }
    public string Channel { get; init; } = "InApp";
    public string Priority { get; init; } = "Normal";
}

public record NotificationTemplateDto
{
    public long Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Channel { get; init; } = "InApp";
    public string? SubjectTemplate { get; init; }
    public string BodyTemplate { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}

public record CreateNotificationTemplateRequest
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Channel { get; init; } = "InApp";
    public string? SubjectTemplate { get; init; }
    public string BodyTemplate { get; init; } = string.Empty;
}

public record UpdateNotificationTemplateRequest
{
    public string Name { get; init; } = string.Empty;
    public string Channel { get; init; } = "InApp";
    public string? SubjectTemplate { get; init; }
    public string BodyTemplate { get; init; } = string.Empty;
}

public record UpdateTemplateStatusRequest
{
    public bool IsActive { get; init; }
}
