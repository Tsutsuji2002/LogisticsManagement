using FluentValidation;
using Logistics.Server.Application.Modules.Organization.DTOs;

namespace Logistics.Server.Application.Modules.Organization.Validators;

public class CreateCompanyRequestValidator : AbstractValidator<CreateCompanyRequest>
{
    public CreateCompanyRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Company code is required.")
            .MaximumLength(50).WithMessage("Company code cannot exceed 50 characters.");

        RuleFor(x => x.LegalName)
            .NotEmpty().WithMessage("Legal name is required.")
            .MaximumLength(200).WithMessage("Legal name cannot exceed 200 characters.");

        RuleFor(x => x.TradingName)
            .NotEmpty().WithMessage("Trading name is required.")
            .MaximumLength(200).WithMessage("Trading name cannot exceed 200 characters.");

        RuleFor(x => x.TaxNumber)
            .NotEmpty().WithMessage("Tax number is required.")
            .MaximumLength(50).WithMessage("Tax number cannot exceed 50 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(150).WithMessage("Email cannot exceed 150 characters.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone is required.")
            .MaximumLength(50).WithMessage("Phone cannot exceed 50 characters.");
    }
}

public class UpdateCompanyRequestValidator : AbstractValidator<UpdateCompanyRequest>
{
    public UpdateCompanyRequestValidator()
    {
        RuleFor(x => x.LegalName)
            .NotEmpty().WithMessage("Legal name is required.")
            .MaximumLength(200).WithMessage("Legal name cannot exceed 200 characters.");

        RuleFor(x => x.TradingName)
            .NotEmpty().WithMessage("Trading name is required.")
            .MaximumLength(200).WithMessage("Trading name cannot exceed 200 characters.");

        RuleFor(x => x.TaxNumber)
            .NotEmpty().WithMessage("Tax number is required.")
            .MaximumLength(50).WithMessage("Tax number cannot exceed 50 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(150).WithMessage("Email cannot exceed 150 characters.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone is required.")
            .MaximumLength(50).WithMessage("Phone cannot exceed 50 characters.");
    }
}

public class CreateBranchRequestValidator : AbstractValidator<CreateBranchRequest>
{
    private static readonly HashSet<string> AllowedBranchTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Headquarters", "RegionalOffice", "TransitHub", "DeliveryPost", "SortingHub", "Office", "Hybrid"
    };

    public CreateBranchRequestValidator()
    {
        RuleFor(x => x.CompanyId)
            .GreaterThan(0).When(x => x.CompanyId.HasValue)
            .WithMessage("CompanyId must be greater than 0.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Branch code is required.")
            .MaximumLength(50).WithMessage("Branch code cannot exceed 50 characters.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Branch name is required.")
            .MaximumLength(150).WithMessage("Branch name cannot exceed 150 characters.");

        RuleFor(x => x.BranchType)
            .NotEmpty().WithMessage("Branch type is required.")
            .Must(t => AllowedBranchTypes.Contains(t))
            .WithMessage("Branch type must be one of: Headquarters, RegionalOffice, TransitHub, DeliveryPost, SortingHub, Office, Hybrid.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone number is required.")
            .MaximumLength(50).WithMessage("Phone number cannot exceed 50 characters.");

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("A valid email address is required.")
            .MaximumLength(150).WithMessage("Email cannot exceed 150 characters.");
    }
}

public class UpdateBranchRequestValidator : AbstractValidator<UpdateBranchRequest>
{
    private static readonly HashSet<string> AllowedBranchTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Headquarters", "RegionalOffice", "TransitHub", "DeliveryPost", "SortingHub", "Office", "Hybrid"
    };

    public UpdateBranchRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Branch name is required.")
            .MaximumLength(150).WithMessage("Branch name cannot exceed 150 characters.");

        RuleFor(x => x.BranchType)
            .NotEmpty().WithMessage("Branch type is required.")
            .Must(t => AllowedBranchTypes.Contains(t))
            .WithMessage("Branch type must be one of: Headquarters, RegionalOffice, TransitHub, DeliveryPost, SortingHub, Office, Hybrid.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone number is required.")
            .MaximumLength(50).WithMessage("Phone number cannot exceed 50 characters.");

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("A valid email address is required.")
            .MaximumLength(150).WithMessage("Email cannot exceed 150 characters.");
    }
}

public class UpdateBranchStatusRequestValidator : AbstractValidator<UpdateBranchStatusRequest>
{
    public UpdateBranchStatusRequestValidator()
    {
        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters.");
    }
}

public class CreateDepartmentRequestValidator : AbstractValidator<CreateDepartmentRequest>
{
    public CreateDepartmentRequestValidator()
    {
        RuleFor(x => x.BranchId)
            .GreaterThan(0).WithMessage("BranchId must be greater than 0.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Department code is required.")
            .MaximumLength(50).WithMessage("Department code cannot exceed 50 characters.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Department name is required.")
            .MaximumLength(150).WithMessage("Department name cannot exceed 150 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");
    }
}

public class UpdateDepartmentRequestValidator : AbstractValidator<UpdateDepartmentRequest>
{
    public UpdateDepartmentRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Department name is required.")
            .MaximumLength(150).WithMessage("Department name cannot exceed 150 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");
    }
}

public class UpdateDepartmentStatusRequestValidator : AbstractValidator<UpdateDepartmentStatusRequest>
{
    public UpdateDepartmentStatusRequestValidator()
    {
        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters.");
    }
}

public class CreateEmployeeRequestValidator : AbstractValidator<CreateEmployeeRequest>
{
    private static readonly HashSet<string> AllowedGenders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Male", "Female", "Other"
    };

    public CreateEmployeeRequestValidator()
    {
        RuleFor(x => x.BranchId)
            .GreaterThan(0).WithMessage("BranchId must be greater than 0.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Employee code is required.")
            .MaximumLength(50).WithMessage("Employee code cannot exceed 50 characters.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(150).WithMessage("Email cannot exceed 150 characters.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .MaximumLength(50).WithMessage("Phone number cannot exceed 50 characters.");

        RuleFor(x => x.JobTitle)
            .NotEmpty().WithMessage("Job title is required.")
            .MaximumLength(100).WithMessage("Job title cannot exceed 100 characters.");

        RuleFor(x => x.Gender)
            .Must(g => string.IsNullOrEmpty(g) || AllowedGenders.Contains(g))
            .WithMessage("Gender must be one of: Male, Female, Other.");

        RuleFor(x => x.HireDate)
            .NotEmpty().WithMessage("Hire date is required.");
    }
}

public class UpdateEmployeeRequestValidator : AbstractValidator<UpdateEmployeeRequest>
{
    private static readonly HashSet<string> AllowedGenders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Male", "Female", "Other"
    };

    public UpdateEmployeeRequestValidator()
    {
        RuleFor(x => x.BranchId)
            .GreaterThan(0).WithMessage("BranchId must be greater than 0.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(150).WithMessage("Email cannot exceed 150 characters.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .MaximumLength(50).WithMessage("Phone number cannot exceed 50 characters.");

        RuleFor(x => x.JobTitle)
            .NotEmpty().WithMessage("Job title is required.")
            .MaximumLength(100).WithMessage("Job title cannot exceed 100 characters.");

        RuleFor(x => x.Gender)
            .Must(g => string.IsNullOrEmpty(g) || AllowedGenders.Contains(g))
            .WithMessage("Gender must be one of: Male, Female, Other.");

        RuleFor(x => x.HireDate)
            .NotEmpty().WithMessage("Hire date is required.");
    }
}

public class UpdateEmployeeStatusRequestValidator : AbstractValidator<UpdateEmployeeStatusRequest>
{
    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Active", "Inactive", "OnLeave", "Resigned", "Terminated"
    };

    public UpdateEmployeeStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .Must(s => AllowedStatuses.Contains(s))
            .WithMessage("Status must be one of: Active, Inactive, OnLeave, Resigned, Terminated.");

        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters.");
    }
}

public class CreateEmployeeAccountRequestValidator : AbstractValidator<CreateEmployeeAccountRequest>
{
    private static readonly HashSet<string> DisallowedPasswords = new(StringComparer.OrdinalIgnoreCase)
    {
        "Password123456", "Password12345!", "Admin12345678", "Welcome123456", "Logistics12345"
    };

    public CreateEmployeeAccountRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.")
            .MinimumLength(3).WithMessage("Username must be at least 3 characters long.")
            .MaximumLength(50).WithMessage("Username cannot exceed 50 characters.")
            .Matches(@"^[a-zA-Z0-9_\-\.]+$").WithMessage("Username can only contain alphanumeric characters, underscores, dashes, and periods.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(12).WithMessage("Password must be at least 12 characters long.")
            .MaximumLength(128).WithMessage("Password cannot exceed 128 characters.")
            .Must(p => !DisallowedPasswords.Contains(p)).WithMessage("The selected password is too common or easily guessable.");

        RuleFor(x => x.RoleCodes)
            .NotNull().WithMessage("Role codes cannot be null.")
            .Must(r => r.Count > 0).WithMessage("At least one role must be assigned.");
    }
}

public class UpdateEmployeeAccountStatusRequestValidator : AbstractValidator<UpdateEmployeeAccountStatusRequest>
{
    public UpdateEmployeeAccountStatusRequestValidator()
    {
        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters.");
    }
}
