using FluentValidation;
using Logistics.Server.Application.Modules.Crm.DTOs;

namespace Logistics.Server.Application.Modules.Crm.Validators;

public class CreateCustomerRequestValidator : AbstractValidator<CreateCustomerRequest>
{
    private static readonly HashSet<string> AllowedCustomerTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Individual", "Corporate"
    };

    private static readonly HashSet<string> AllowedPaymentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Prepaid", "PostpaidMonthly", "CodOffset"
    };

    public CreateCustomerRequestValidator()
    {
        RuleFor(x => x.CustomerCode)
            .NotEmpty().WithMessage("Customer code is required.")
            .MaximumLength(50).WithMessage("Customer code cannot exceed 50 characters.");

        RuleFor(x => x.CustomerType)
            .NotEmpty().WithMessage("Customer type is required.")
            .Must(t => AllowedCustomerTypes.Contains(t))
            .WithMessage("Customer type must be either 'Individual' or 'Corporate'.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Customer name is required.")
            .MaximumLength(200).WithMessage("Customer name cannot exceed 200 characters.");

        RuleFor(x => x.TradeName)
            .MaximumLength(200).WithMessage("Trade name cannot exceed 200 characters.")
            .When(x => !string.IsNullOrEmpty(x.TradeName));

        RuleFor(x => x.TaxCode)
            .MaximumLength(50).WithMessage("Tax code cannot exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.TaxCode));

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(150).WithMessage("Email cannot exceed 150 characters.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .MaximumLength(50).WithMessage("Phone number cannot exceed 50 characters.");

        RuleFor(x => x.CreditLimit)
            .GreaterThanOrEqualTo(0).WithMessage("Credit limit cannot be negative.");

        RuleFor(x => x.CreditTermDays)
            .GreaterThanOrEqualTo(0).WithMessage("Credit term days cannot be negative.");

        RuleFor(x => x.PaymentType)
            .NotEmpty().WithMessage("Payment type is required.")
            .Must(p => AllowedPaymentTypes.Contains(p))
            .WithMessage("Payment type must be one of: Prepaid, PostpaidMonthly, CodOffset.");

        RuleFor(x => x.BranchId)
            .GreaterThan(0).WithMessage("BranchId must be greater than 0.")
            .When(x => x.BranchId.HasValue);

        RuleFor(x => x.AccountManagerEmployeeId)
            .GreaterThan(0).WithMessage("AccountManagerEmployeeId must be greater than 0.")
            .When(x => x.AccountManagerEmployeeId.HasValue);
    }
}

public class UpdateCustomerRequestValidator : AbstractValidator<UpdateCustomerRequest>
{
    private static readonly HashSet<string> AllowedCustomerTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Individual", "Corporate"
    };

    private static readonly HashSet<string> AllowedPaymentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Prepaid", "PostpaidMonthly", "CodOffset"
    };

    public UpdateCustomerRequestValidator()
    {
        RuleFor(x => x.CustomerType)
            .NotEmpty().WithMessage("Customer type is required.")
            .Must(t => AllowedCustomerTypes.Contains(t))
            .WithMessage("Customer type must be either 'Individual' or 'Corporate'.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Customer name is required.")
            .MaximumLength(200).WithMessage("Customer name cannot exceed 200 characters.");

        RuleFor(x => x.TradeName)
            .MaximumLength(200).WithMessage("Trade name cannot exceed 200 characters.")
            .When(x => !string.IsNullOrEmpty(x.TradeName));

        RuleFor(x => x.TaxCode)
            .MaximumLength(50).WithMessage("Tax code cannot exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.TaxCode));

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(150).WithMessage("Email cannot exceed 150 characters.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .MaximumLength(50).WithMessage("Phone number cannot exceed 50 characters.");

        RuleFor(x => x.CreditLimit)
            .GreaterThanOrEqualTo(0).WithMessage("Credit limit cannot be negative.");

        RuleFor(x => x.CreditTermDays)
            .GreaterThanOrEqualTo(0).WithMessage("Credit term days cannot be negative.");

        RuleFor(x => x.PaymentType)
            .NotEmpty().WithMessage("Payment type is required.")
            .Must(p => AllowedPaymentTypes.Contains(p))
            .WithMessage("Payment type must be one of: Prepaid, PostpaidMonthly, CodOffset.");

        RuleFor(x => x.AccountManagerEmployeeId)
            .GreaterThan(0).WithMessage("AccountManagerEmployeeId must be greater than 0.")
            .When(x => x.AccountManagerEmployeeId.HasValue);
    }
}

public class UpdateCustomerStatusRequestValidator : AbstractValidator<UpdateCustomerStatusRequest>
{
    public UpdateCustomerStatusRequestValidator()
    {
        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Reason));
    }
}

public class CreateCustomerContactRequestValidator : AbstractValidator<CreateCustomerContactRequest>
{
    public CreateCustomerContactRequestValidator()
    {
        RuleFor(x => x.ContactName)
            .NotEmpty().WithMessage("Contact name is required.")
            .MaximumLength(150).WithMessage("Contact name cannot exceed 150 characters.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .MaximumLength(50).WithMessage("Phone number cannot exceed 50 characters.");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(150).WithMessage("Email cannot exceed 150 characters.")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.JobTitle)
            .MaximumLength(100).WithMessage("Job title cannot exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.JobTitle));

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes cannot exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}

public class UpdateCustomerContactRequestValidator : AbstractValidator<UpdateCustomerContactRequest>
{
    public UpdateCustomerContactRequestValidator()
    {
        RuleFor(x => x.ContactName)
            .NotEmpty().WithMessage("Contact name is required.")
            .MaximumLength(150).WithMessage("Contact name cannot exceed 150 characters.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .MaximumLength(50).WithMessage("Phone number cannot exceed 50 characters.");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(150).WithMessage("Email cannot exceed 150 characters.")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.JobTitle)
            .MaximumLength(100).WithMessage("Job title cannot exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.JobTitle));

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes cannot exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}

public class CreateCustomerAddressRequestValidator : AbstractValidator<CreateCustomerAddressRequest>
{
    private static readonly HashSet<string> AllowedAddressTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Sender", "Recipient", "Billing", "PickupWarehouse"
    };

    public CreateCustomerAddressRequestValidator()
    {
        RuleFor(x => x.AddressType)
            .NotEmpty().WithMessage("Address type is required.")
            .Must(t => AllowedAddressTypes.Contains(t))
            .WithMessage("Address type must be one of: Sender, Recipient, Billing, PickupWarehouse.");

        RuleFor(x => x.ContactName)
            .NotEmpty().WithMessage("Contact name is required.")
            .MaximumLength(150).WithMessage("Contact name cannot exceed 150 characters.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .MaximumLength(50).WithMessage("Phone number cannot exceed 50 characters.");

        RuleFor(x => x.AddressLine1)
            .NotEmpty().WithMessage("Address line 1 is required.")
            .MaximumLength(250).WithMessage("Address line 1 cannot exceed 250 characters.");

        RuleFor(x => x.AddressLine2)
            .MaximumLength(250).WithMessage("Address line 2 cannot exceed 250 characters.")
            .When(x => !string.IsNullOrEmpty(x.AddressLine2));

        RuleFor(x => x.AddressLabel)
            .MaximumLength(100).WithMessage("Address label cannot exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.AddressLabel));

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(150).WithMessage("Email cannot exceed 150 characters.")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.PostalCode)
            .MaximumLength(20).WithMessage("Postal code cannot exceed 20 characters.")
            .When(x => !string.IsNullOrEmpty(x.PostalCode));

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90.")
            .When(x => x.Latitude.HasValue);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180.")
            .When(x => x.Longitude.HasValue);
    }
}

public class UpdateCustomerAddressRequestValidator : AbstractValidator<UpdateCustomerAddressRequest>
{
    private static readonly HashSet<string> AllowedAddressTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Sender", "Recipient", "Billing", "PickupWarehouse"
    };

    public UpdateCustomerAddressRequestValidator()
    {
        RuleFor(x => x.AddressType)
            .NotEmpty().WithMessage("Address type is required.")
            .Must(t => AllowedAddressTypes.Contains(t))
            .WithMessage("Address type must be one of: Sender, Recipient, Billing, PickupWarehouse.");

        RuleFor(x => x.ContactName)
            .NotEmpty().WithMessage("Contact name is required.")
            .MaximumLength(150).WithMessage("Contact name cannot exceed 150 characters.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .MaximumLength(50).WithMessage("Phone number cannot exceed 50 characters.");

        RuleFor(x => x.AddressLine1)
            .NotEmpty().WithMessage("Address line 1 is required.")
            .MaximumLength(250).WithMessage("Address line 1 cannot exceed 250 characters.");

        RuleFor(x => x.AddressLine2)
            .MaximumLength(250).WithMessage("Address line 2 cannot exceed 250 characters.")
            .When(x => !string.IsNullOrEmpty(x.AddressLine2));

        RuleFor(x => x.AddressLabel)
            .MaximumLength(100).WithMessage("Address label cannot exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.AddressLabel));

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(150).WithMessage("Email cannot exceed 150 characters.")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.PostalCode)
            .MaximumLength(20).WithMessage("Postal code cannot exceed 20 characters.")
            .When(x => !string.IsNullOrEmpty(x.PostalCode));

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90.")
            .When(x => x.Latitude.HasValue);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180.")
            .When(x => x.Longitude.HasValue);
    }
}

public class CreateCustomerContractRequestValidator : AbstractValidator<CreateCustomerContractRequest>
{
    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Draft", "Active", "Suspended", "Expired", "Terminated"
    };

    public CreateCustomerContractRequestValidator()
    {
        RuleFor(x => x.ContractNumber)
            .NotEmpty().WithMessage("Contract number is required.")
            .MaximumLength(100).WithMessage("Contract number cannot exceed 100 characters.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Contract title is required.")
            .MaximumLength(200).WithMessage("Contract title cannot exceed 200 characters.");

        RuleFor(x => x.EndDate)
            .Must((request, endDate) => endDate >= request.StartDate)
            .WithMessage("End date must be greater than or equal to start date.");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Contract status is required.")
            .Must(s => AllowedStatuses.Contains(s))
            .WithMessage("Contract status must be one of: Draft, Active, Suspended, Expired, Terminated.");

        RuleFor(x => x.DiscountRate)
            .InclusiveBetween(0, 100).WithMessage("Discount rate must be between 0 and 100 percent.");

        RuleFor(x => x.PaymentTermDays)
            .GreaterThanOrEqualTo(0).WithMessage("Payment term days cannot be negative.");
    }
}

public class UpdateCustomerContractRequestValidator : AbstractValidator<UpdateCustomerContractRequest>
{
    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Draft", "Active", "Suspended", "Expired", "Terminated"
    };

    public UpdateCustomerContractRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Contract title is required.")
            .MaximumLength(200).WithMessage("Contract title cannot exceed 200 characters.");

        RuleFor(x => x.EndDate)
            .Must((request, endDate) => endDate >= request.StartDate)
            .WithMessage("End date must be greater than or equal to start date.");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Contract status is required.")
            .Must(s => AllowedStatuses.Contains(s))
            .WithMessage("Contract status must be one of: Draft, Active, Suspended, Expired, Terminated.");

        RuleFor(x => x.DiscountRate)
            .InclusiveBetween(0, 100).WithMessage("Discount rate must be between 0 and 100 percent.");

        RuleFor(x => x.PaymentTermDays)
            .GreaterThanOrEqualTo(0).WithMessage("Payment term days cannot be negative.");
    }
}

public class UpdateCustomerContractStatusRequestValidator : AbstractValidator<UpdateCustomerContractStatusRequest>
{
    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Draft", "Active", "Suspended", "Expired", "Terminated"
    };

    public UpdateCustomerContractStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .Must(s => AllowedStatuses.Contains(s))
            .WithMessage("Status must be one of: Draft, Active, Suspended, Expired, Terminated.");

        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Reason));
    }
}

public class CreateCustomerPricingRuleRequestValidator : AbstractValidator<CreateCustomerPricingRuleRequest>
{
    public CreateCustomerPricingRuleRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Pricing rule name is required.")
            .MaximumLength(150).WithMessage("Pricing rule name cannot exceed 150 characters.");

        RuleFor(x => x.EffectiveFrom)
            .NotEmpty().WithMessage("Effective from date is required.");

        RuleFor(x => x.EffectiveTo)
            .Must((request, effectiveTo) => !effectiveTo.HasValue || effectiveTo.Value >= request.EffectiveFrom)
            .WithMessage("Effective to date must be greater than or equal to effective from date.")
            .When(x => x.EffectiveTo.HasValue);

        RuleFor(x => x.DiscountPercent)
            .InclusiveBetween(0, 100).WithMessage("Discount percent must be between 0 and 100.");

        RuleFor(x => x.FlatDiscountAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Flat discount amount cannot be negative.");

        RuleFor(x => x.Priority)
            .GreaterThanOrEqualTo(1).WithMessage("Priority must be at least 1.");
    }
}

public class UpdateCustomerPricingRuleRequestValidator : AbstractValidator<UpdateCustomerPricingRuleRequest>
{
    public UpdateCustomerPricingRuleRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Pricing rule name is required.")
            .MaximumLength(150).WithMessage("Pricing rule name cannot exceed 150 characters.");

        RuleFor(x => x.EffectiveFrom)
            .NotEmpty().WithMessage("Effective from date is required.");

        RuleFor(x => x.EffectiveTo)
            .Must((request, effectiveTo) => !effectiveTo.HasValue || effectiveTo.Value >= request.EffectiveFrom)
            .WithMessage("Effective to date must be greater than or equal to effective from date.")
            .When(x => x.EffectiveTo.HasValue);

        RuleFor(x => x.DiscountPercent)
            .InclusiveBetween(0, 100).WithMessage("Discount percent must be between 0 and 100.");

        RuleFor(x => x.FlatDiscountAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Flat discount amount cannot be negative.");

        RuleFor(x => x.Priority)
            .GreaterThanOrEqualTo(1).WithMessage("Priority must be at least 1.");
    }
}
