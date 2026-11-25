using FluentValidation;
using Logistics.Server.Application.Modules.BackgroundProcessing.DTOs;

namespace Logistics.Server.Application.Modules.BackgroundProcessing.Validators;

public class EnqueueBackgroundJobRequestValidator : AbstractValidator<EnqueueBackgroundJobRequest>
{
    public EnqueueBackgroundJobRequestValidator()
    {
        RuleFor(x => x.JobName)
            .NotEmpty().WithMessage("JobName is required.")
            .MaximumLength(100);

        RuleFor(x => x.JobReference)
            .MaximumLength(150).When(x => x.JobReference != null);

        RuleFor(x => x.MaxAttempts)
            .InclusiveBetween(1, 10);

        RuleFor(x => x.PayloadJson)
            .MaximumLength(16384).When(x => x.PayloadJson != null)
            .WithMessage("PayloadJson cannot exceed 16 KB.");
    }
}

public class CreateScheduledReportRequestValidator : AbstractValidator<CreateScheduledReportRequest>
{
    private static readonly string[] AllowedFormats = ["Pdf", "Excel", "Csv"];

    public CreateScheduledReportRequestValidator()
    {
        RuleFor(x => x.ReportDefinitionId).GreaterThan(0);

        RuleFor(x => x.ScheduleCron)
            .NotEmpty().WithMessage("ScheduleCron is required.")
            .MaximumLength(100)
            .Must(BeValidFivePartCron).WithMessage("ScheduleCron must be a valid 5-field cron expression (minute hour day month weekday).");

        RuleFor(x => x.TimeZoneId)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.OutputFormat)
            .NotEmpty()
            .Must(f => AllowedFormats.Contains(f))
            .WithMessage($"OutputFormat must be one of: {string.Join(", ", AllowedFormats)}.");

        RuleFor(x => x.RecipientEmails)
            .NotEmpty().WithMessage("RecipientEmails is required.")
            .MaximumLength(500);
    }

    public static bool BeValidFivePartCron(string? cron)
    {
        if (string.IsNullOrWhiteSpace(cron)) return false;
        var parts = cron.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 5;
    }
}

public class UpdateScheduledReportRequestValidator : AbstractValidator<UpdateScheduledReportRequest>
{
    private static readonly string[] AllowedFormats = ["Pdf", "Excel", "Csv"];

    public UpdateScheduledReportRequestValidator()
    {
        RuleFor(x => x.ScheduleCron)
            .NotEmpty()
            .MaximumLength(100)
            .Must(CreateScheduledReportRequestValidator.BeValidFivePartCron)
            .WithMessage("ScheduleCron must be a valid 5-field cron expression.");

        RuleFor(x => x.TimeZoneId).NotEmpty().MaximumLength(100);

        RuleFor(x => x.OutputFormat)
            .NotEmpty()
            .Must(f => AllowedFormats.Contains(f))
            .WithMessage($"OutputFormat must be one of: {string.Join(", ", AllowedFormats)}.");

        RuleFor(x => x.RecipientEmails).NotEmpty().MaximumLength(500);
    }
}

