using FluentValidation;
using Logistics.Server.Application.Modules.Reporting.DTOs;

namespace Logistics.Server.Application.Modules.Reporting.Validators;

public class ReportFilterRequestValidator : AbstractValidator<ReportFilterRequest>
{
    public ReportFilterRequestValidator()
    {
        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate!.Value)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
            .WithMessage("EndDate must be greater than or equal to StartDate.");

        RuleFor(x => x)
            .Must(x => !x.StartDate.HasValue || !x.EndDate.HasValue || (x.EndDate.Value.DayNumber - x.StartDate.Value.DayNumber) <= 366)
            .WithMessage("Date range cannot exceed 366 days.");

        RuleFor(x => x.TimeZoneId)
            .NotEmpty().MaximumLength(100)
            .Must(BeValidTimeZone).WithMessage("Unrecognized TimeZoneId.");

        RuleFor(x => x.Search)
            .MaximumLength(100).When(x => !string.IsNullOrEmpty(x.Search));
    }

    private static bool BeValidTimeZone(string? tz)
    {
        if (string.IsNullOrWhiteSpace(tz)) return false;
        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(tz.Trim());
            return true;
        }
        catch
        {
            return false;
        }
    }
}
