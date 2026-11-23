using FluentValidation;
using Logistics.Server.Application.Modules.Identity.DTOs;

namespace Logistics.Server.Application.Modules.Identity.Validators;

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required.");
    }
}

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    private static readonly HashSet<string> DisallowedPasswords = new(StringComparer.OrdinalIgnoreCase)
    {
        "Password123456", "Password12345!", "Admin12345678", "Welcome123456", "Logistics12345"
    };

    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(12).WithMessage("New password must be at least 12 characters long.")
            .MaximumLength(128).WithMessage("New password cannot exceed 128 characters.")
            .NotEqual(x => x.CurrentPassword).WithMessage("New password cannot be identical to the current password.")
            .Must(p => !DisallowedPasswords.Contains(p)).WithMessage("The selected password is too common or easily guessable.");
    }
}
