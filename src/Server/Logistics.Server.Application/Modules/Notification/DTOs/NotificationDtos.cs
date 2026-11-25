namespace Logistics.Server.Application.Modules.Notification.DTOs;

// ═══════════════════════════════════════════════════════════════
//  1. NOTIFICATION DTOs
// ═══════════════════════════════════════════════════════════════

public record NotificationDto
{
    public long Id { get; init; }
    public Guid PublicId { get; init; }
    public long? TemplateId { get; init; }
    public string? TemplateCode { get; init; }
    public long? RecipientUserId { get; init; }
    public string? RecipientEmail { get; init; }
    public string? RecipientPhone { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string Channel { get; init; } = "InApp";
    public string Priority { get; init; } = "Normal";
    public string Status { get; init; } = "Pending";
    public string? NotificationType { get; init; }
    public string? ReferenceType { get; init; }
    public long? ReferenceId { get; init; }
    public long? BranchId { get; init; }
    public bool IsRead { get; init; }
    public DateTime? ReadAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public int DeliveriesCount { get; init; }
}

public record NotificationDetailDto : NotificationDto
{
    public List<NotificationDeliveryDto> Deliveries { get; init; } = [];
}

public record NotificationDeliveryDto
{
    public long Id { get; init; }
    public long NotificationId { get; init; }
    public string Channel { get; init; } = string.Empty;
    public int AttemptNumber { get; init; }
    public DateTime AttemptedAt { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? GatewayResponse { get; init; }
    public string? ErrorDetails { get; init; }
}

public record UnreadCountDto
{
    public long UnreadCount { get; init; }
}

public record NotificationFilterRequest
{
    public bool? IsRead { get; init; }
    public string? NotificationType { get; init; }
    public string? Channel { get; init; }
    public string? Status { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public long? BranchId { get; init; }
    public bool IncludeExpired { get; init; } = false;
    public string? SortBy { get; init; }
    public bool SortDescending { get; init; } = true;
}

public record CreateNotificationRequest
{
    public long? RecipientUserId { get; init; }
    public List<long>? RecipientUserIds { get; init; }
    public string? RecipientEmail { get; init; }
    public string? RecipientPhone { get; init; }
    public long? BranchId { get; init; }
    public bool IsSystemBroadcast { get; init; }
    public string? TemplateCode { get; init; }
    public Dictionary<string, string>? TemplateParameters { get; init; }
    public string? Title { get; init; }
    public string? Content { get; init; }
    public string Channel { get; init; } = "InApp";
    public string Priority { get; init; } = "Normal";
    public string? NotificationType { get; init; }
    public string? ReferenceType { get; init; }
    public long? ReferenceId { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public string? EventKey { get; init; }
}

// ═══════════════════════════════════════════════════════════════
//  2. NOTIFICATION TEMPLATE DTOs
// ═══════════════════════════════════════════════════════════════

public record NotificationTemplateDto
{
    public long Id { get; init; }
    public Guid PublicId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Channel { get; init; } = "InApp";
    public string? SubjectTemplate { get; init; }
    public string BodyTemplate { get; init; } = string.Empty;
    public string? ParametersJson { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record CreateNotificationTemplateRequest
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Channel { get; init; } = "InApp";
    public string? SubjectTemplate { get; init; }
    public string BodyTemplate { get; init; } = string.Empty;
    public List<string>? AllowedParameters { get; init; }
}

public record UpdateNotificationTemplateRequest
{
    public string Name { get; init; } = string.Empty;
    public string Channel { get; init; } = "InApp";
    public string? SubjectTemplate { get; init; }
    public string BodyTemplate { get; init; } = string.Empty;
    public List<string>? AllowedParameters { get; init; }
}

public record UpdateTemplateStatusRequest
{
    public bool IsActive { get; init; }
}

public record PreviewTemplateRequest
{
    public Dictionary<string, string> Parameters { get; init; } = [];
    public bool HtmlEscapeBody { get; init; }
}

public record RenderedTemplateDto
{
    public string Subject { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public List<string> ResolvedPlaceholders { get; init; } = [];
}

// ═══════════════════════════════════════════════════════════════
//  3. EMAIL PROVIDER CONTRACTS
// ═══════════════════════════════════════════════════════════════

public sealed record EmailSendRequest
{
    public string ToEmail { get; init; } = string.Empty;
    public string? ToName { get; init; }
    public string Subject { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public bool IsHtml { get; init; } = true;
    public string? IdempotencyKey { get; init; }
}

public enum EmailFailureClassification
{
    None = 0,
    Transient = 1,
    Permanent = 2,
    ConfigurationMissing = 3
}

public sealed record EmailSendResult
{
    public bool Succeeded { get; init; }
    public string? ProviderMessageId { get; init; }
    public string? GatewayResponse { get; init; }
    public string? ErrorDetails { get; init; }
    public EmailFailureClassification FailureClassification { get; init; } = EmailFailureClassification.None;
    public int? RetryAfterSeconds { get; init; }
}

