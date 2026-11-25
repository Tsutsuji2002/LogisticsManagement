using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Logistics.Server.Application.Common.Exceptions;
using Logistics.Server.Application.Common.Interfaces;
using Logistics.Server.Application.Common.Models;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Identity.Services;
using Logistics.Server.Application.Modules.Notification.DTOs;
using Logistics.Server.Application.Modules.Notification.Services;
using Logistics.Server.Domain.Entities.Notification;
using Logistics.Server.Infrastructure.Persistence;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Logistics.Server.Infrastructure.Services;

// ═══════════════════════════════════════════════════════════════
//  1. SAFE NOTIFICATION TEMPLATE RENDERER
// ═══════════════════════════════════════════════════════════════

public sealed class NotificationTemplateRenderer : INotificationTemplateRenderer
{
    private static readonly Regex DoubleBraceRegex = new(@"\{\{\s*([A-Za-z0-9_]+)\s*\}\}", RegexOptions.Compiled);
    private static readonly Regex SingleBraceRegex = new(@"\{([A-Za-z0-9_]+)\}", RegexOptions.Compiled);
    private static readonly Regex DisallowedSyntaxRegex = new(@"(\{\{[^A-Za-z0-9_\s\}]+|<script\b|javascript:|@\(|\$\{)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public RenderedTemplateDto Render(
        string? subjectTemplate,
        string bodyTemplate,
        IReadOnlyCollection<string>? approvedPlaceholders,
        IReadOnlyDictionary<string, string>? parameters,
        bool htmlEscapeBody = false)
    {
        if (string.IsNullOrWhiteSpace(bodyTemplate))
            throw new ValidationException("BodyTemplate", "Template body cannot be empty.");

        ValidateSafeTemplateSyntax(subjectTemplate, nameof(subjectTemplate));
        ValidateSafeTemplateSyntax(bodyTemplate, nameof(bodyTemplate));

        var paramDict = parameters != null
            ? new Dictionary<string, string>(parameters, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        HashSet<string>? approvedSet = approvedPlaceholders != null && approvedPlaceholders.Count > 0
            ? new HashSet<string>(approvedPlaceholders, StringComparer.OrdinalIgnoreCase)
            : null;

        var usedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        CollectPlaceholders(subjectTemplate, usedKeys);
        CollectPlaceholders(bodyTemplate, usedKeys);

        if (approvedSet != null)
        {
            var unapproved = usedKeys.Where(k => !approvedSet.Contains(k)).ToList();
            if (unapproved.Count > 0)
            {
                throw new ValidationException(
                    "TemplatePlaceholders",
                    $"Template uses unapproved placeholders: {string.Join(", ", unapproved)}.");
            }
        }

        var missing = usedKeys.Where(k => !paramDict.ContainsKey(k)).ToList();
        if (missing.Count > 0)
        {
            throw new ValidationException(
                "TemplateParameters",
                $"Missing required template placeholders: {string.Join(", ", missing)}.");
        }

        var renderedSubject = Substitute(subjectTemplate ?? string.Empty, paramDict, htmlEscape: false);
        var renderedBody = Substitute(bodyTemplate, paramDict, htmlEscape: htmlEscapeBody);

        return new RenderedTemplateDto
        {
            Subject = renderedSubject,
            Body = renderedBody,
            ResolvedPlaceholders = usedKeys.OrderBy(k => k).ToList()
        };
    }

    private static void ValidateSafeTemplateSyntax(string? template, string fieldName)
    {
        if (string.IsNullOrEmpty(template)) return;
        if (DisallowedSyntaxRegex.IsMatch(template))
        {
            throw new ValidationException(fieldName, "Template contains disallowed script or expression syntax.");
        }
    }

    private static void CollectPlaceholders(string? template, HashSet<string> keys)
    {
        if (string.IsNullOrEmpty(template)) return;

        foreach (Match m in DoubleBraceRegex.Matches(template))
            keys.Add(m.Groups[1].Value);

        var withoutDouble = DoubleBraceRegex.Replace(template, string.Empty);
        foreach (Match m in SingleBraceRegex.Matches(withoutDouble))
            keys.Add(m.Groups[1].Value);
    }

    private static string Substitute(string template, Dictionary<string, string> parameters, bool htmlEscape)
    {
        if (string.IsNullOrEmpty(template)) return string.Empty;

        var result = DoubleBraceRegex.Replace(template, match =>
        {
            var key = match.Groups[1].Value;
            var val = parameters.TryGetValue(key, out var v) ? v ?? string.Empty : string.Empty;
            return htmlEscape ? WebUtility.HtmlEncode(val) : val;
        });

        result = SingleBraceRegex.Replace(result, match =>
        {
            var key = match.Groups[1].Value;
            var val = parameters.TryGetValue(key, out var v) ? v ?? string.Empty : string.Empty;
            return htmlEscape ? WebUtility.HtmlEncode(val) : val;
        });

        return result;
    }
}

// ═══════════════════════════════════════════════════════════════
//  2. NOTIFICATION TEMPLATE SERVICE
// ═══════════════════════════════════════════════════════════════

public sealed class NotificationTemplateService : INotificationTemplateService
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationTemplateRenderer _renderer;
    private readonly IAuditService _audit;
    private readonly ICurrentUserService _currentUser;

    public NotificationTemplateService(
        ApplicationDbContext context,
        INotificationTemplateRenderer renderer,
        IAuditService audit,
        ICurrentUserService currentUser)
    {
        _context = context;
        _renderer = renderer;
        _audit = audit;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<NotificationTemplateDto>> GetTemplatesAsync(
        string? search, string? channel, bool? isActive, PagedRequest pagedRequest, CancellationToken cancellationToken = default)
    {
        var query = _context.NotificationTemplates.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t => t.Code.Contains(search) || t.Name.Contains(search));
        if (!string.IsNullOrWhiteSpace(channel))
            query = query.Where(t => t.Channel == channel);
        if (isActive.HasValue)
            query = query.Where(t => t.IsActive == isActive.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(t => t.Code)
            .Skip((pagedRequest.Page - 1) * pagedRequest.PageSize)
            .Take(pagedRequest.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<NotificationTemplateDto>(
            items.Select(MapToDto).ToList(), totalCount, pagedRequest.Page, pagedRequest.PageSize);
    }

    public async Task<NotificationTemplateDto?> GetByIdOrCodeAsync(string identifier, CancellationToken cancellationToken = default)
    {
        NotificationTemplate? template;
        if (long.TryParse(identifier, out var id))
            template = await _context.NotificationTemplates.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        else
            template = await _context.NotificationTemplates.AsNoTracking().FirstOrDefaultAsync(t => t.Code == identifier.ToUpperInvariant(), cancellationToken);

        return template == null ? null : MapToDto(template);
    }

    public async Task<NotificationTemplateDto> CreateAsync(CreateNotificationTemplateRequest request, CancellationToken cancellationToken = default)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        if (await _context.NotificationTemplates.AnyAsync(t => t.Code == code, cancellationToken))
            throw new ConflictException("NotificationTemplate", "Code", code);

        // Validate template safety and allowed placeholder whitelist
        ValidateTemplatePlaceholders(request.SubjectTemplate, request.BodyTemplate, request.AllowedParameters);

        var template = new NotificationTemplate
        {
            Code = code,
            Name = request.Name.Trim(),
            Channel = request.Channel.Trim(),
            SubjectTemplate = request.SubjectTemplate,
            BodyTemplate = request.BodyTemplate,
            ParametersJson = request.AllowedParameters != null ? JsonSerializer.Serialize(request.AllowedParameters) : null,
            IsActive = true,
            CreatedBy = _currentUser.Username ?? "System"
        };

        await _context.NotificationTemplates.AddAsync(template, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync("CreateTemplate", "notification", "NotificationTemplates", template.Id.ToString(),
            null, new { template.Code, template.Name, template.Channel }, cancellationToken: cancellationToken);

        return MapToDto(template);
    }

    public async Task<NotificationTemplateDto> UpdateAsync(long id, UpdateNotificationTemplateRequest request, CancellationToken cancellationToken = default)
    {
        var template = await _context.NotificationTemplates.FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new NotFoundException("NotificationTemplate", id);

        ValidateTemplatePlaceholders(request.SubjectTemplate, request.BodyTemplate, request.AllowedParameters);

        var oldState = new { template.Name, template.Channel, template.SubjectTemplate };

        template.Name = request.Name.Trim();
        template.Channel = request.Channel.Trim();
        template.SubjectTemplate = request.SubjectTemplate;
        template.BodyTemplate = request.BodyTemplate;
        template.ParametersJson = request.AllowedParameters != null ? JsonSerializer.Serialize(request.AllowedParameters) : template.ParametersJson;
        template.UpdatedAt = DateTime.UtcNow;
        template.UpdatedBy = _currentUser.Username ?? "System";

        await _context.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync("UpdateTemplate", "notification", "NotificationTemplates", template.Id.ToString(),
            oldState, new { template.Name, template.Channel, template.SubjectTemplate }, cancellationToken: cancellationToken);

        return MapToDto(template);
    }

    public async Task<NotificationTemplateDto> SetActiveStatusAsync(long id, bool isActive, CancellationToken cancellationToken = default)
    {
        var template = await _context.NotificationTemplates.FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new NotFoundException("NotificationTemplate", id);

        template.IsActive = isActive;
        template.UpdatedAt = DateTime.UtcNow;
        template.UpdatedBy = _currentUser.Username ?? "System";

        await _context.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync(isActive ? "ActivateTemplate" : "DeactivateTemplate", "notification", "NotificationTemplates",
            template.Id.ToString(), null, new { template.IsActive }, cancellationToken: cancellationToken);

        return MapToDto(template);
    }

    public async Task<RenderedTemplateDto> PreviewAsync(long id, PreviewTemplateRequest request, CancellationToken cancellationToken = default)
    {
        var template = await _context.NotificationTemplates.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new NotFoundException("NotificationTemplate", id);

        var approved = ParseAllowedParameters(template.ParametersJson);
        return _renderer.Render(
            template.SubjectTemplate,
            template.BodyTemplate,
            approved,
            request.Parameters,
            htmlEscapeBody: request.HtmlEscapeBody || string.Equals(template.Channel, "Email", StringComparison.OrdinalIgnoreCase));
    }

    private void ValidateTemplatePlaceholders(string? subject, string body, List<string>? allowedParameters)
    {
        if (allowedParameters == null || allowedParameters.Count == 0)
        {
            // Render check against dummy dictionary? Only check disallowed syntax if no explicit whitelist passed
            var dummy = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match m in Regex.Matches($"{subject} {body}", @"\{\{\s*([A-Za-z0-9_]+)\s*\}\}|\{([A-Za-z0-9_]+)\}"))
            {
                var k = string.IsNullOrEmpty(m.Groups[1].Value) ? m.Groups[2].Value : m.Groups[1].Value;
                dummy[k] = "sample";
            }
            _renderer.Render(subject, body, null, dummy, false);
            return;
        }

        var testParams = allowedParameters.Distinct(StringComparer.OrdinalIgnoreCase)
            .ToDictionary(k => k, _ => "sample", StringComparer.OrdinalIgnoreCase);
        _renderer.Render(subject, body, allowedParameters, testParams, false);
    }

    public static List<string>? ParseAllowedParameters(string? parametersJson)
    {
        if (string.IsNullOrWhiteSpace(parametersJson)) return null;
        try
        {
            return JsonSerializer.Deserialize<List<string>>(parametersJson);
        }
        catch
        {
            return null;
        }
    }

    private static NotificationTemplateDto MapToDto(NotificationTemplate t) => new()
    {
        Id = t.Id,
        PublicId = t.PublicId,
        Code = t.Code,
        Name = t.Name,
        Channel = t.Channel,
        SubjectTemplate = t.SubjectTemplate,
        BodyTemplate = t.BodyTemplate,
        ParametersJson = t.ParametersJson,
        IsActive = t.IsActive,
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt
    };
}

// ═══════════════════════════════════════════════════════════════
//  3. NOTIFICATION SERVICE
// ═══════════════════════════════════════════════════════════════

public sealed class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationTemplateRenderer _renderer;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;
    private readonly ILogger<NotificationService> _logger;
    private readonly IPermissionService? _permissionService;

    public NotificationService(
        ApplicationDbContext context,
        INotificationTemplateRenderer renderer,
        ICurrentUserService currentUser,
        IAuditService audit,
        ILogger<NotificationService> logger,
        IPermissionService? permissionService = null)
    {
        _context = context;
        _renderer = renderer;
        _currentUser = currentUser;
        _audit = audit;
        _logger = logger;
        _permissionService = permissionService;
    }

    public async Task<NotificationDto> CreateForUserAsync(
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
        CancellationToken cancellationToken = default)
    {
        var list = await CreateAsync(new CreateNotificationRequest
        {
            RecipientUserId = userId,
            Title = title,
            Content = content,
            NotificationType = notificationType,
            Channel = channel,
            Priority = priority,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            BranchId = branchId,
            EventKey = eventKey
        }, cancellationToken);

        return list[0];
    }

    public async Task<IReadOnlyList<NotificationDto>> CreateForUsersAsync(
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
        CancellationToken cancellationToken = default)
    {
        return await CreateAsync(new CreateNotificationRequest
        {
            RecipientUserIds = userIds.Distinct().ToList(),
            Title = title,
            Content = content,
            NotificationType = notificationType,
            Channel = channel,
            Priority = priority,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            BranchId = branchId,
            EventKey = eventKeyPrefix
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationDto>> CreateForBranchAsync(
        long branchId,
        string title,
        string content,
        string? notificationType = null,
        string channel = "InApp",
        string priority = "Normal",
        string? referenceType = null,
        long? referenceId = null,
        string? eventKeyPrefix = null,
        CancellationToken cancellationToken = default)
    {
        return await CreateAsync(new CreateNotificationRequest
        {
            BranchId = branchId,
            Title = title,
            Content = content,
            NotificationType = notificationType,
            Channel = channel,
            Priority = priority,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            EventKey = eventKeyPrefix
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationDto>> CreateSystemNotificationAsync(
        string title,
        string content,
        string? notificationType = null,
        string priority = "High",
        string? referenceType = null,
        long? referenceId = null,
        string? eventKeyPrefix = null,
        CancellationToken cancellationToken = default)
    {
        return await CreateAsync(new CreateNotificationRequest
        {
            IsSystemBroadcast = true,
            Title = title,
            Content = content,
            NotificationType = notificationType ?? "SystemAlert",
            Channel = "InApp",
            Priority = priority,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            EventKey = eventKeyPrefix
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationDto>> CreateAsync(CreateNotificationRequest request, CancellationToken cancellationToken = default)
    {
        long? templateId = null;
        string title = request.Title ?? string.Empty;
        string content = request.Content ?? string.Empty;
        string effectiveChannel = request.Channel;

        if (!string.IsNullOrWhiteSpace(request.TemplateCode))
        {
            var template = await _context.NotificationTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Code == request.TemplateCode.Trim().ToUpperInvariant(), cancellationToken)
                ?? throw new NotFoundException("NotificationTemplate", request.TemplateCode);

            if (!template.IsActive)
                throw new ConflictException($"Notification template '{template.Code}' is inactive and cannot be used.");

            templateId = template.Id;
            effectiveChannel = string.IsNullOrWhiteSpace(request.Channel) ? template.Channel : request.Channel;
            var approved = NotificationTemplateService.ParseAllowedParameters(template.ParametersJson);

            var rendered = _renderer.Render(
                template.SubjectTemplate ?? template.Name,
                template.BodyTemplate,
                approved,
                request.TemplateParameters,
                htmlEscapeBody: string.Equals(effectiveChannel, "Email", StringComparison.OrdinalIgnoreCase));

            title = rendered.Subject;
            content = rendered.Body;
        }

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
            throw new ValidationException("Content", "Notification Title and Content are required.");

        var recipients = await ResolveRecipientsAsync(request, cancellationToken);
        if (recipients.Count == 0 && string.IsNullOrWhiteSpace(request.RecipientEmail))
        {
            _logger.LogWarning("No active recipients resolved for notification '{Title}'.", title);
            return Array.Empty<NotificationDto>();
        }

        var created = new List<Notification>();
        var actor = _currentUser.Username ?? "System";

        if (recipients.Count > 0)
        {
            foreach (var rec in recipients)
            {
                var itemEventKey = string.IsNullOrWhiteSpace(request.EventKey)
                    ? null
                    : $"{request.EventKey}:u{rec.Id}:{effectiveChannel}";

                if (!string.IsNullOrEmpty(itemEventKey))
                {
                    var existing = await _context.Notifications
                        .AsNoTracking()
                        .FirstOrDefaultAsync(n => n.EventKey == itemEventKey, cancellationToken);
                    if (existing != null)
                    {
                        created.Add(existing);
                        continue;
                    }
                }

                var isInApp = string.Equals(effectiveChannel, "InApp", StringComparison.OrdinalIgnoreCase);
                var notification = new Notification
                {
                    TemplateId = templateId,
                    RecipientUserId = rec.Id,
                    RecipientEmail = isInApp ? rec.Email : (request.RecipientEmail ?? rec.Email),
                    RecipientPhone = request.RecipientPhone,
                    Title = title.Length > 250 ? title[..250] : title,
                    Content = content,
                    Channel = effectiveChannel,
                    Priority = request.Priority,
                    Status = isInApp ? "Delivered" : "Pending",
                    NotificationType = request.NotificationType ?? request.ReferenceType,
                    ReferenceType = request.ReferenceType,
                    ReferenceId = request.ReferenceId,
                    BranchId = request.BranchId,
                    IsRead = false,
                    ExpiresAt = request.ExpiresAt,
                    EventKey = itemEventKey,
                    CreatedBy = actor
                };

                if (isInApp)
                {
                    notification.Deliveries.Add(new NotificationDelivery
                    {
                        Channel = "InApp",
                        AttemptNumber = 1,
                        AttemptedAt = DateTime.UtcNow,
                        Status = "Success",
                        GatewayResponse = "InAppPersisted"
                    });
                }

                await _context.Notifications.AddAsync(notification, cancellationToken);
                created.Add(notification);
            }
        }
        else if (!string.IsNullOrWhiteSpace(request.RecipientEmail))
        {
            var itemEventKey = string.IsNullOrWhiteSpace(request.EventKey)
                ? null
                : $"{request.EventKey}:e{request.RecipientEmail.Trim().ToLowerInvariant()}:{effectiveChannel}";

            if (!string.IsNullOrEmpty(itemEventKey))
            {
                var existing = await _context.Notifications
                    .AsNoTracking()
                    .FirstOrDefaultAsync(n => n.EventKey == itemEventKey, cancellationToken);
                if (existing != null)
                {
                    return [MapToDto(existing)];
                }
            }

            var notification = new Notification
            {
                TemplateId = templateId,
                RecipientEmail = request.RecipientEmail.Trim(),
                RecipientPhone = request.RecipientPhone,
                Title = title.Length > 250 ? title[..250] : title,
                Content = content,
                Channel = effectiveChannel,
                Priority = request.Priority,
                Status = string.Equals(effectiveChannel, "InApp", StringComparison.OrdinalIgnoreCase) ? "Delivered" : "Pending",
                NotificationType = request.NotificationType ?? request.ReferenceType,
                ReferenceType = request.ReferenceType,
                ReferenceId = request.ReferenceId,
                BranchId = request.BranchId,
                IsRead = false,
                ExpiresAt = request.ExpiresAt,
                EventKey = itemEventKey,
                CreatedBy = actor
            };

            await _context.Notifications.AddAsync(notification, cancellationToken);
            created.Add(notification);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return created.Select(n => MapToDto(n)).ToList();
    }

    public async Task<PagedResult<NotificationDto>> GetMyNotificationsAsync(
        NotificationFilterRequest filter, PagedRequest pagedRequest, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId
            ?? throw new ForbiddenAccessException("Authenticated user identity is required.");

        return await QueryNotificationsInternalAsync(userId, filter, pagedRequest, cancellationToken);
    }

    public async Task<PagedResult<NotificationDto>> GetNotificationsAsync(
        long? recipientUserId, NotificationFilterRequest filter, PagedRequest pagedRequest, CancellationToken cancellationToken = default)
    {
        var query = BuildFilterQuery(recipientUserId, filter);

        if (!_currentUser.IsInRole(AppRoles.SuperAdmin))
        {
            var accessibleBranches = _currentUser.AccessibleBranchIds;
            query = query.Where(n => !n.BranchId.HasValue || accessibleBranches.Contains(n.BranchId.Value));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var ordered = ApplyOrdering(query, filter.SortBy, filter.SortDescending);
        var items = await ordered
            .Skip((pagedRequest.Page - 1) * pagedRequest.PageSize)
            .Take(pagedRequest.PageSize)
            .Include(n => n.Template)
            .Include(n => n.Deliveries)
            .ToListAsync(cancellationToken);

        return new PagedResult<NotificationDto>(
            items.Select(MapToDto).ToList(), totalCount, pagedRequest.Page, pagedRequest.PageSize);
    }

    public async Task<NotificationDetailDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var notification = await _context.Notifications
            .Include(n => n.Template)
            .Include(n => n.Deliveries)
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

        if (notification == null) return null;

        await EnsureReadAccessAsync(notification, cancellationToken);

        return new NotificationDetailDto
        {
            Id = notification.Id,
            PublicId = notification.PublicId,
            TemplateId = notification.TemplateId,
            TemplateCode = notification.Template?.Code,
            RecipientUserId = notification.RecipientUserId,
            RecipientEmail = notification.RecipientEmail,
            RecipientPhone = notification.RecipientPhone,
            Title = notification.Title,
            Content = notification.Content,
            Channel = notification.Channel,
            Priority = notification.Priority,
            Status = notification.Status,
            NotificationType = notification.NotificationType,
            ReferenceType = notification.ReferenceType,
            ReferenceId = notification.ReferenceId,
            BranchId = notification.BranchId,
            IsRead = notification.IsRead,
            ReadAt = notification.ReadAt,
            ExpiresAt = notification.ExpiresAt,
            CreatedAt = notification.CreatedAt,
            DeliveriesCount = notification.Deliveries.Count,
            Deliveries = notification.Deliveries
                .OrderBy(d => d.AttemptNumber)
                .Select(d => new NotificationDeliveryDto
                {
                    Id = d.Id,
                    NotificationId = d.NotificationId,
                    Channel = d.Channel,
                    AttemptNumber = d.AttemptNumber,
                    AttemptedAt = d.AttemptedAt,
                    Status = d.Status,
                    GatewayResponse = d.GatewayResponse,
                    ErrorDetails = d.ErrorDetails
                }).ToList()
        };
    }

    public async Task<UnreadCountDto> GetUnreadCountAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId
            ?? throw new ForbiddenAccessException("Authenticated user identity is required.");

        var now = DateTime.UtcNow;
        var count = await _context.Notifications
            .AsNoTracking()
            .Where(n => n.RecipientUserId == userId
                        && !n.IsRead
                        && (n.ExpiresAt == null || n.ExpiresAt > now))
            .CountAsync(cancellationToken);

        return new UnreadCountDto { UnreadCount = count };
    }

    public async Task<NotificationDto> MarkAsReadAsync(long id, CancellationToken cancellationToken = default)
    {
        var notification = await _context.Notifications
            .Include(n => n.Template)
            .Include(n => n.Deliveries)
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken)
            ?? throw new NotFoundException("Notification", id);

        await EnsureWriteAccessAsync(notification, cancellationToken);

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            notification.UpdatedAt = DateTime.UtcNow;
            notification.UpdatedBy = _currentUser.Username ?? "System";
            await _context.SaveChangesAsync(cancellationToken);
        }

        return MapToDto(notification);
    }

    public async Task<int> MarkAllAsReadAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId
            ?? throw new ForbiddenAccessException("Authenticated user identity is required.");

        var now = DateTime.UtcNow;
        var unread = await _context.Notifications
            .Where(n => n.RecipientUserId == userId && !n.IsRead)
            .ToListAsync(cancellationToken);

        if (unread.Count == 0) return 0;

        foreach (var item in unread)
        {
            item.IsRead = true;
            item.ReadAt = now;
            item.UpdatedAt = now;
            item.UpdatedBy = _currentUser.Username ?? "System";
        }

        await _context.SaveChangesAsync(cancellationToken);
        return unread.Count;
    }

    private async Task<PagedResult<NotificationDto>> QueryNotificationsInternalAsync(
        long userId, NotificationFilterRequest filter, PagedRequest pagedRequest, CancellationToken cancellationToken)
    {
        var query = BuildFilterQuery(userId, filter);
        var totalCount = await query.CountAsync(cancellationToken);

        var ordered = ApplyOrdering(query, filter.SortBy, filter.SortDescending);
        var items = await ordered
            .Skip((pagedRequest.Page - 1) * pagedRequest.PageSize)
            .Take(pagedRequest.PageSize)
            .Include(n => n.Template)
            .Include(n => n.Deliveries)
            .ToListAsync(cancellationToken);

        return new PagedResult<NotificationDto>(
            items.Select(MapToDto).ToList(), totalCount, pagedRequest.Page, pagedRequest.PageSize);
    }

    private IQueryable<Notification> BuildFilterQuery(long? recipientUserId, NotificationFilterRequest filter)
    {
        var now = DateTime.UtcNow;
        var query = _context.Notifications.AsNoTracking().AsQueryable();

        if (recipientUserId.HasValue)
            query = query.Where(n => n.RecipientUserId == recipientUserId.Value);

        if (filter.IsRead.HasValue)
            query = query.Where(n => n.IsRead == filter.IsRead.Value);

        if (!string.IsNullOrWhiteSpace(filter.NotificationType))
            query = query.Where(n => n.NotificationType == filter.NotificationType);

        if (!string.IsNullOrWhiteSpace(filter.Channel))
            query = query.Where(n => n.Channel == filter.Channel);

        if (!string.IsNullOrWhiteSpace(filter.Status))
            query = query.Where(n => n.Status == filter.Status);

        if (filter.FromDate.HasValue)
            query = query.Where(n => n.CreatedAt >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(n => n.CreatedAt <= filter.ToDate.Value);

        if (filter.BranchId.HasValue)
            query = query.Where(n => n.BranchId == filter.BranchId.Value);

        if (!filter.IncludeExpired)
            query = query.Where(n => n.ExpiresAt == null || n.ExpiresAt > now);

        return query;
    }

    private static IOrderedQueryable<Notification> ApplyOrdering(IQueryable<Notification> query, string? sortBy, bool desc)
    {
        return (sortBy?.ToLowerInvariant()) switch
        {
            "priority" => desc ? query.OrderByDescending(n => n.Priority).ThenByDescending(n => n.Id) : query.OrderBy(n => n.Priority).ThenBy(n => n.Id),
            "status" => desc ? query.OrderByDescending(n => n.Status).ThenByDescending(n => n.Id) : query.OrderBy(n => n.Status).ThenBy(n => n.Id),
            "isread" => desc ? query.OrderByDescending(n => n.IsRead).ThenByDescending(n => n.Id) : query.OrderBy(n => n.IsRead).ThenBy(n => n.Id),
            _ => desc ? query.OrderByDescending(n => n.CreatedAt).ThenByDescending(n => n.Id) : query.OrderBy(n => n.CreatedAt).ThenBy(n => n.Id)
        };
    }

    private async Task EnsureReadAccessAsync(Notification notification, CancellationToken ct)
    {
        if (notification.RecipientUserId == _currentUser.UserId) return;

        var isAdmin = _currentUser.IsInRole(AppRoles.SuperAdmin)
            || (_currentUser.UserId.HasValue && _permissionService != null
                && await _permissionService.HasPermissionAsync(_currentUser.UserId.Value, AppPermissions.Notification.NotificationsManage, ct));

        if (isAdmin)
        {
            if (notification.BranchId.HasValue && !_currentUser.IsInRole(AppRoles.SuperAdmin) && !_currentUser.HasBranchAccess(notification.BranchId.Value))
                throw new ForbiddenAccessException();
            return;
        }

        throw new ForbiddenAccessException("You cannot access another user's notifications.");
    }

    private async Task EnsureWriteAccessAsync(Notification notification, CancellationToken ct)
    {
        if (notification.RecipientUserId == _currentUser.UserId) return;

        var isAdmin = _currentUser.IsInRole(AppRoles.SuperAdmin)
            || (_currentUser.UserId.HasValue && _permissionService != null
                && await _permissionService.HasPermissionAsync(_currentUser.UserId.Value, AppPermissions.Notification.NotificationsManage, ct));

        if (isAdmin) return;

        throw new ForbiddenAccessException("You cannot modify another user's notifications.");
    }

    private async Task<List<(long Id, string Email)>> ResolveRecipientsAsync(CreateNotificationRequest request, CancellationToken ct)
    {
        if (request.RecipientUserId.HasValue)
        {
            var user = await _context.UserAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == request.RecipientUserId.Value && u.IsActive, ct)
                ?? throw new NotFoundException("UserAccount", request.RecipientUserId.Value);
            return [(user.Id, user.Email)];
        }

        if (request.RecipientUserIds != null && request.RecipientUserIds.Count > 0)
        {
            var ids = request.RecipientUserIds.Distinct().ToList();
            var users = await _context.UserAccounts
                .AsNoTracking()
                .Where(u => ids.Contains(u.Id) && u.IsActive)
                .Select(u => new { u.Id, u.Email })
                .ToListAsync(ct);

            if (users.Count == 0)
                throw new NotFoundException("UserAccount", string.Join(",", ids));

            return users.Select(u => (u.Id, u.Email)).ToList();
        }

        if (request.BranchId.HasValue)
        {
            var users = await _context.UserBranchAccesses
                .AsNoTracking()
                .Where(uba => uba.BranchId == request.BranchId.Value && uba.User.IsActive)
                .Select(uba => new { uba.User.Id, uba.User.Email })
                .Distinct()
                .ToListAsync(ct);

            return users.Select(u => (u.Id, u.Email)).ToList();
        }

        if (request.IsSystemBroadcast)
        {
            var users = await _context.UserAccounts
                .AsNoTracking()
                .Where(u => u.IsActive)
                .Select(u => new { u.Id, u.Email })
                .ToListAsync(ct);

            return users.Select(u => (u.Id, u.Email)).ToList();
        }

        return [];
    }

    private static NotificationDto MapToDto(Notification n) => new()
    {
        Id = n.Id,
        PublicId = n.PublicId,
        TemplateId = n.TemplateId,
        TemplateCode = n.Template?.Code,
        RecipientUserId = n.RecipientUserId,
        RecipientEmail = n.RecipientEmail,
        RecipientPhone = n.RecipientPhone,
        Title = n.Title,
        Content = n.Content,
        Channel = n.Channel,
        Priority = n.Priority,
        Status = n.Status,
        NotificationType = n.NotificationType,
        ReferenceType = n.ReferenceType,
        ReferenceId = n.ReferenceId,
        BranchId = n.BranchId,
        IsRead = n.IsRead,
        ReadAt = n.ReadAt,
        ExpiresAt = n.ExpiresAt,
        CreatedAt = n.CreatedAt,
        DeliveriesCount = n.Deliveries?.Count ?? 0
    };
}

// ═══════════════════════════════════════════════════════════════
//  4. NOTIFICATION DELIVERY SERVICE (EMAIL + RETRIES + BACKOFF)
// ═══════════════════════════════════════════════════════════════

public sealed class NotificationDeliveryService : INotificationDeliveryService
{
    public const int MaxDeliveryAttempts = 5;

    private readonly ApplicationDbContext _context;
    private readonly IEmailProvider _emailProvider;
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationDeliveryService> _logger;

    public NotificationDeliveryService(
        ApplicationDbContext context,
        IEmailProvider emailProvider,
        INotificationService notificationService,
        ILogger<NotificationDeliveryService> logger)
    {
        _context = context;
        _emailProvider = emailProvider;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<int> ProcessPendingDeliveriesAsync(int batchSize = 50, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        // Find pending or eligible queued Email notifications without holding a transaction open during HTTP calls
        var candidates = await _context.Notifications
            .AsNoTracking()
            .Include(n => n.Deliveries)
            .Where(n => n.Channel == "Email"
                        && (n.Status == "Pending" || n.Status == "Queued")
                        && (n.ExpiresAt == null || n.ExpiresAt > now))
            .OrderBy(n => n.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        var processedCount = 0;
        foreach (var candidate in candidates)
        {
            if (cancellationToken.IsCancellationRequested) break;

            if (!IsBackoffElapsed(candidate.Deliveries, now))
                continue;

            try
            {
                await DeliverNotificationAsync(candidate.Id, cancellationToken);
                processedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception while delivering notification {NotificationId}.", candidate.Id);
            }
        }

        return processedCount;
    }

    public async Task<NotificationDetailDto> DeliverNotificationAsync(long notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _context.Notifications
            .Include(n => n.Deliveries)
            .FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken)
            ?? throw new NotFoundException("Notification", notificationId);

        // Idempotency guard: prevent duplicate delivery if already sent/delivered or succeeded previously
        if (notification.Status is "Sent" or "Delivered" ||
            notification.Deliveries.Any(d => d.Status == "Success"))
        {
            return (await _notificationService.GetByIdAsync(notification.Id, cancellationToken))!;
        }

        if (!string.Equals(notification.Channel, "Email", StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(notification.Channel, "InApp", StringComparison.OrdinalIgnoreCase))
            {
                notification.Status = "Delivered";
                notification.Deliveries.Add(new NotificationDelivery
                {
                    NotificationId = notification.Id,
                    Channel = "InApp",
                    AttemptNumber = notification.Deliveries.Count + 1,
                    AttemptedAt = DateTime.UtcNow,
                    Status = "Success",
                    GatewayResponse = "InAppPersisted"
                });
                await _context.SaveChangesAsync(cancellationToken);
            }
            else
            {
                // SMS / Webhook gateways are not implemented for direct notification channels
                notification.Status = "Failed";
                notification.Deliveries.Add(new NotificationDelivery
                {
                    NotificationId = notification.Id,
                    Channel = notification.Channel,
                    AttemptNumber = notification.Deliveries.Count + 1,
                    AttemptedAt = DateTime.UtcNow,
                    Status = "Failed",
                    ErrorDetails = $"Delivery channel '{notification.Channel}' does not have an active outbound provider."
                });
                await _context.SaveChangesAsync(cancellationToken);
            }

            return (await _notificationService.GetByIdAsync(notification.Id, cancellationToken))!;
        }

        if (string.IsNullOrWhiteSpace(notification.RecipientEmail))
        {
            notification.Status = "Failed";
            notification.Deliveries.Add(new NotificationDelivery
            {
                NotificationId = notification.Id,
                Channel = "Email",
                AttemptNumber = notification.Deliveries.Count + 1,
                AttemptedAt = DateTime.UtcNow,
                Status = "Failed",
                ErrorDetails = "Permanent: Missing recipient email address."
            });
            await _context.SaveChangesAsync(cancellationToken);
            return (await _notificationService.GetByIdAsync(notification.Id, cancellationToken))!;
        }

        var nextAttemptNumber = notification.Deliveries.Count + 1;
        var sendReq = new EmailSendRequest
        {
            ToEmail = notification.RecipientEmail,
            Subject = notification.Title,
            Body = notification.Content,
            IsHtml = true,
            IdempotencyKey = $"notif-{notification.PublicId}-att-{nextAttemptNumber}"
        };

        // Call external provider WITHOUT holding any open EF DB transaction
        var result = await _emailProvider.SendAsync(sendReq, cancellationToken);

        // Re-fetch or attach delivery safely
        var delivery = new NotificationDelivery
        {
            NotificationId = notification.Id,
            Channel = "Email",
            AttemptNumber = nextAttemptNumber,
            AttemptedAt = DateTime.UtcNow,
            Status = result.Succeeded ? "Success" : "Failed",
            GatewayResponse = result.GatewayResponse,
            ErrorDetails = result.ErrorDetails
        };

        if (result.RetryAfterSeconds.HasValue && !result.Succeeded)
        {
            delivery.GatewayResponse = string.IsNullOrEmpty(delivery.GatewayResponse)
                ? $"RetryAfter={result.RetryAfterSeconds}s"
                : $"{delivery.GatewayResponse}; RetryAfter={result.RetryAfterSeconds}s";
        }

        _context.NotificationDeliveries.Add(delivery);

        if (result.Succeeded)
        {
            notification.Status = "Sent";
        }
        else if (result.FailureClassification is EmailFailureClassification.Permanent or EmailFailureClassification.ConfigurationMissing
                 || nextAttemptNumber >= MaxDeliveryAttempts)
        {
            notification.Status = "Failed";
        }
        else
        {
            notification.Status = "Queued";
        }

        notification.UpdatedAt = DateTime.UtcNow;
        notification.UpdatedBy = "NotificationDeliveryService";
        await _context.SaveChangesAsync(cancellationToken);

        return (await _notificationService.GetByIdAsync(notification.Id, cancellationToken))!;
    }

    public static bool IsBackoffElapsed(ICollection<NotificationDelivery> deliveries, DateTime utcNow)
    {
        if (deliveries == null || deliveries.Count == 0) return true;

        var last = deliveries.OrderByDescending(d => d.AttemptNumber).First();
        if (last.Status == "Success") return false;

        // Check if GatewayResponse contained RetryAfter=Ns
        int? retryAfter = null;
        if (!string.IsNullOrEmpty(last.GatewayResponse))
        {
            var match = Regex.Match(last.GatewayResponse, @"RetryAfter=(\d+)s");
            if (match.Success && int.TryParse(match.Groups[1].Value, out var sec))
                retryAfter = sec;
        }

        var baseDelaySeconds = retryAfter ?? (int)Math.Pow(2, Math.Min(last.AttemptNumber, 6)) * 15;
        return last.AttemptedAt.AddSeconds(baseDelaySeconds) <= utcNow;
    }
}

