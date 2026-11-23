using FluentValidation;
using Logistics.Server.Application.Modules.Identity.DTOs;

namespace Logistics.Server.Application.Modules.Identity.Validators;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    private static readonly HashSet<string> DisallowedPasswords = new(StringComparer.OrdinalIgnoreCase)
    {
        "Password123456", "Password12345!", "Admin12345678", "Welcome123456", "Logistics12345"
    };

    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.")
            .MinimumLength(3).WithMessage("Username must be at least 3 characters long.")
            .MaximumLength(50).WithMessage("Username cannot exceed 50 characters.")
            .Matches(@"^[a-zA-Z0-9_\-\.]+$").WithMessage("Username can only contain alphanumeric characters, underscores, dashes, and periods.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email address is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(150).WithMessage("Email cannot exceed 150 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(12).WithMessage("Password must be at least 12 characters long.")
            .MaximumLength(128).WithMessage("Password cannot exceed 128 characters.")
            .Must(p => !DisallowedPasswords.Contains(p)).WithMessage("The selected password is too common or easily guessable.");
    }
}

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email address is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(150).WithMessage("Email cannot exceed 150 characters.");
    }
}

public class UpdateUserRolesRequestValidator : AbstractValidator<UpdateUserRolesRequest>
{
    public UpdateUserRolesRequestValidator()
    {
        RuleFor(x => x.Roles)
            .NotNull().WithMessage("Roles list cannot be null.");
    }
}

public class UpdateUserBranchesRequestValidator : AbstractValidator<UpdateUserBranchesRequest>
{
    public UpdateUserBranchesRequestValidator()
    {
        RuleFor(x => x.BranchIds)
            .NotNull().WithMessage("BranchIds list cannot be null.");
    }
}

public class GrantBranchAccessRequestValidator : AbstractValidator<GrantBranchAccessRequest>
{
    public GrantBranchAccessRequestValidator()
    {
        RuleFor(x => x.BranchId)
            .GreaterThan(0).WithMessage("BranchId must be greater than 0.");
    }
}

public class AdminResetPasswordRequestValidator : AbstractValidator<AdminResetPasswordRequest>
{
    private static readonly HashSet<string> DisallowedPasswords = new(StringComparer.OrdinalIgnoreCase)
    {
        "Password123456", "Password12345!", "Admin12345678", "Welcome123456", "Logistics12345"
    };

    public AdminResetPasswordRequestValidator()
    {
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(12).WithMessage("New password must be at least 12 characters long.")
            .MaximumLength(128).WithMessage("New password cannot exceed 128 characters.")
            .Must(p => !DisallowedPasswords.Contains(p)).WithMessage("The selected password is too common or easily guessable.");
    }
}
