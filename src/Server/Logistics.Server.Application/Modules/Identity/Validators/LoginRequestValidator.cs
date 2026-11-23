using FluentValidation;
using Logistics.Server.Application.Modules.Identity.DTOs;

namespace Logistics.Server.Application.Modules.Identity.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.UsernameOrEmail)
            .NotEmpty().WithMessage("Username or email is required.")
            .MaximumLength(150).WithMessage("Username or email cannot exceed 150 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MaximumLength(128).WithMessage("Password cannot exceed 128 characters.");
    }
}
