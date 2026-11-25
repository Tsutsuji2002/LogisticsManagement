using FluentValidation;
using Logistics.Server.Application.Modules.Notification.DTOs;

namespace Logistics.Server.Application.Modules.Notification.Validators;

public class CreateNotificationRequestValidator : AbstractValidator<CreateNotificationRequest>
{
    private static readonly string[] AllowedChannels = ["InApp", "Email", "SMS", "Webhook"];
    private static readonly string[] AllowedPriorities = ["Low", "Normal", "High", "Urgent"];

    public CreateNotificationRequestValidator()
    {
        RuleFor(x => x.Channel)
            .NotEmpty().WithMessage("Channel is required.")
            .Must(c => AllowedChannels.Contains(c))
            .WithMessage($"Channel must be one of: {string.Join(", ", AllowedChannels)}.");

        RuleFor(x => x.Priority)
            .NotEmpty().WithMessage("Priority is required.")
            .Must(p => AllowedPriorities.Contains(p))
            .WithMessage($"Priority must be one of: {string.Join(", ", AllowedPriorities)}.");

        RuleFor(x => x)
            .Must(x => x.RecipientUserId.HasValue ||
                       (x.RecipientUserIds != null && x.RecipientUserIds.Count > 0) ||
                       !string.IsNullOrWhiteSpace(x.RecipientEmail) ||
                       x.BranchId.HasValue ||
                       x.IsSystemBroadcast)
            .WithMessage("At least one recipient target (RecipientUserId, RecipientUserIds, RecipientEmail, BranchId, or IsSystemBroadcast) must be provided.");

        RuleFor(x => x.RecipientEmail)
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.RecipientEmail))
            .MaximumLength(150);

        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.TemplateCode) || (!string.IsNullOrWhiteSpace(x.Title) && !string.IsNullOrWhiteSpace(x.Content)))
            .WithMessage("Either TemplateCode or both Title and Content must be supplied.");

        RuleFor(x => x.Title)
            .MaximumLength(250).When(x => !string.IsNullOrWhiteSpace(x.Title));

        RuleFor(x => x.NotificationType)
            .MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.NotificationType));

        RuleFor(x => x.EventKey)
            .MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.EventKey));
    }
}

public class CreateNotificationTemplateRequestValidator : AbstractValidator<CreateNotificationTemplateRequest>
{
    private static readonly string[] AllowedChannels = ["InApp", "Email", "SMS", "Webhook"];

    public CreateNotificationTemplateRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Template code is required.")
            .MaximumLength(100)
            .Matches("^[A-Z0-9_]+$").WithMessage("Template code must contain uppercase letters, digits, or underscores only.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Template name is required.")
            .MaximumLength(150);

        RuleFor(x => x.Channel)
            .NotEmpty()
            .Must(c => AllowedChannels.Contains(c))
            .WithMessage($"Channel must be one of: {string.Join(", ", AllowedChannels)}.");

        RuleFor(x => x.SubjectTemplate)
            .MaximumLength(500).When(x => x.SubjectTemplate != null);

        RuleFor(x => x.BodyTemplate)
            .NotEmpty().WithMessage("BodyTemplate is required.");
    }
}

public class UpdateNotificationTemplateRequestValidator : AbstractValidator<UpdateNotificationTemplateRequest>
{
    private static readonly string[] AllowedChannels = ["InApp", "Email", "SMS", "Webhook"];

    public UpdateNotificationTemplateRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Template name is required.")
            .MaximumLength(150);

        RuleFor(x => x.Channel)
            .NotEmpty()
            .Must(c => AllowedChannels.Contains(c))
            .WithMessage($"Channel must be one of: {string.Join(", ", AllowedChannels)}.");

        RuleFor(x => x.SubjectTemplate)
            .MaximumLength(500).When(x => x.SubjectTemplate != null);

        RuleFor(x => x.BodyTemplate)
            .NotEmpty().WithMessage("BodyTemplate is required.");
    }
}

