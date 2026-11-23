using FluentValidation;
using Logistics.Server.Application.Modules.Finance.DTOs;

namespace Logistics.Server.Application.Modules.Finance.Validators;

public class CreateShippingRateCardRequestValidator : AbstractValidator<CreateShippingRateCardRequest>
{
    public CreateShippingRateCardRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Rate card code is required.")
            .MaximumLength(50).WithMessage("Rate card code cannot exceed 50 characters.")
            .Matches(@"^[A-Z0-9_\-]+$").WithMessage("Code must contain only uppercase letters, numbers, hyphens, and underscores.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Rate card name is required.")
            .MaximumLength(150).WithMessage("Rate card name cannot exceed 150 characters.");

        RuleFor(x => x.BaseCurrencyId)
            .GreaterThan(0).WithMessage("Valid BaseCurrencyId is required.");

        RuleFor(x => x.EffectiveTo)
            .GreaterThanOrEqualTo(x => x.EffectiveFrom)
            .When(x => x.EffectiveTo.HasValue)
            .WithMessage("EffectiveTo date must be greater than or equal to EffectiveFrom.");
    }
}

public class UpdateShippingRateCardRequestValidator : AbstractValidator<UpdateShippingRateCardRequest>
{
    public UpdateShippingRateCardRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Rate card name is required.")
            .MaximumLength(150).WithMessage("Rate card name cannot exceed 150 characters.");

        RuleFor(x => x.BaseCurrencyId)
            .GreaterThan(0).WithMessage("Valid BaseCurrencyId is required.");

        RuleFor(x => x.EffectiveTo)
            .GreaterThanOrEqualTo(x => x.EffectiveFrom)
            .When(x => x.EffectiveTo.HasValue)
            .WithMessage("EffectiveTo date must be greater than or equal to EffectiveFrom.");
    }
}

public class CreateShippingRateRuleRequestValidator : AbstractValidator<CreateShippingRateRuleRequest>
{
    public CreateShippingRateRuleRequestValidator()
    {
        RuleFor(x => x.ServiceType)
            .NotEmpty().WithMessage("Service type is required.")
            .MaximumLength(50).WithMessage("Service type cannot exceed 50 characters.");

        RuleFor(x => x.FromWeightKg)
            .GreaterThanOrEqualTo(0).WithMessage("FromWeightKg must be non-negative.");

        RuleFor(x => x.ToWeightKg)
            .GreaterThanOrEqualTo(x => x.FromWeightKg)
            .WithMessage("ToWeightKg must be greater than or equal to FromWeightKg.");

        RuleFor(x => x.BaseRate)
            .GreaterThanOrEqualTo(0).WithMessage("BaseRate must be non-negative.");

        RuleFor(x => x.AdditionalPerKgRate)
            .GreaterThanOrEqualTo(0).WithMessage("AdditionalPerKgRate must be non-negative.");

        RuleFor(x => x.SurchargePercent)
            .GreaterThanOrEqualTo(0).WithMessage("SurchargePercent must be non-negative.")
            .LessThanOrEqualTo(100).WithMessage("SurchargePercent cannot exceed 100%.");

        RuleFor(x => x.MinimumFee)
            .GreaterThanOrEqualTo(0).WithMessage("MinimumFee must be non-negative.");

        RuleFor(x => x.EstimatedDays)
            .GreaterThanOrEqualTo(0).WithMessage("EstimatedDays must be non-negative.");
    }
}

public class UpdateShippingRateRuleRequestValidator : AbstractValidator<UpdateShippingRateRuleRequest>
{
    public UpdateShippingRateRuleRequestValidator()
    {
        RuleFor(x => x.ServiceType)
            .NotEmpty().WithMessage("Service type is required.")
            .MaximumLength(50).WithMessage("Service type cannot exceed 50 characters.");

        RuleFor(x => x.FromWeightKg)
            .GreaterThanOrEqualTo(0).WithMessage("FromWeightKg must be non-negative.");

        RuleFor(x => x.ToWeightKg)
            .GreaterThanOrEqualTo(x => x.FromWeightKg)
            .WithMessage("ToWeightKg must be greater than or equal to FromWeightKg.");

        RuleFor(x => x.BaseRate)
            .GreaterThanOrEqualTo(0).WithMessage("BaseRate must be non-negative.");

        RuleFor(x => x.AdditionalPerKgRate)
            .GreaterThanOrEqualTo(0).WithMessage("AdditionalPerKgRate must be non-negative.");

        RuleFor(x => x.SurchargePercent)
            .GreaterThanOrEqualTo(0).WithMessage("SurchargePercent must be non-negative.")
            .LessThanOrEqualTo(100).WithMessage("SurchargePercent cannot exceed 100%.");

        RuleFor(x => x.MinimumFee)
            .GreaterThanOrEqualTo(0).WithMessage("MinimumFee must be non-negative.");

        RuleFor(x => x.EstimatedDays)
            .GreaterThanOrEqualTo(0).WithMessage("EstimatedDays must be non-negative.");
    }
}

public class CalculateShippingFeeRequestValidator : AbstractValidator<CalculateShippingFeeRequest>
{
    public CalculateShippingFeeRequestValidator()
    {
        RuleFor(x => x.CustomerId)
            .GreaterThan(0).WithMessage("Valid CustomerId is required.");

        RuleFor(x => x.ServiceType)
            .NotEmpty().WithMessage("ServiceType is required.");

        RuleFor(x => x.WeightKg)
            .GreaterThan(0).WithMessage("WeightKg must be greater than zero.");

        RuleFor(x => x.DeclaredValue)
            .GreaterThanOrEqualTo(0).WithMessage("DeclaredValue must be non-negative.");

        RuleFor(x => x.CodAmount)
            .GreaterThanOrEqualTo(0).WithMessage("CodAmount must be non-negative.");
    }
}

public class CreateShipmentChargeRequestValidator : AbstractValidator<CreateShipmentChargeRequest>
{
    public CreateShipmentChargeRequestValidator()
    {
        RuleFor(x => x.ShipmentId)
            .GreaterThan(0).WithMessage("Valid ShipmentId is required.");

        RuleFor(x => x.ChargeType)
            .NotEmpty().WithMessage("ChargeType is required.")
            .MaximumLength(50);

        RuleFor(x => x.Amount)
            .NotEqual(0).WithMessage("Charge amount cannot be zero.");

        RuleFor(x => x.CurrencyId)
            .GreaterThan(0).WithMessage("Valid CurrencyId is required.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(500);
    }
}

public class CreateChargeAdjustmentRequestValidator : AbstractValidator<CreateChargeAdjustmentRequest>
{
    public CreateChargeAdjustmentRequestValidator()
    {
        RuleFor(x => x.ChargeType)
            .NotEmpty().WithMessage("ChargeType is required.")
            .MaximumLength(50);

        RuleFor(x => x.Amount)
            .NotEqual(0).WithMessage("Adjustment amount cannot be zero.");

        RuleFor(x => x.CurrencyId)
            .GreaterThan(0).WithMessage("Valid CurrencyId is required.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(500);
    }
}

public class CreateInvoiceRequestValidator : AbstractValidator<CreateInvoiceRequest>
{
    public CreateInvoiceRequestValidator()
    {
        RuleFor(x => x.CustomerId)
            .GreaterThan(0).WithMessage("Valid CustomerId is required.");

        RuleFor(x => x.TaxRate)
            .GreaterThanOrEqualTo(0).WithMessage("TaxRate must be non-negative.")
            .LessThanOrEqualTo(100).WithMessage("TaxRate cannot exceed 100%.");

        RuleFor(x => x.PaymentTermsDays)
            .GreaterThanOrEqualTo(0).WithMessage("PaymentTermsDays must be non-negative.");

        RuleFor(x => x.DueDate)
            .GreaterThanOrEqualTo(x => x.InvoiceDate)
            .When(x => x.DueDate.HasValue)
            .WithMessage("DueDate must be on or after InvoiceDate.");
    }
}

public class CreateInvoiceItemRequestValidator : AbstractValidator<CreateInvoiceItemRequest>
{
    public CreateInvoiceItemRequestValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(250);

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than zero.");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage("UnitPrice must be non-negative.");
    }
}

public class CancelInvoiceRequestValidator : AbstractValidator<CancelInvoiceRequest>
{
    public CancelInvoiceRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Cancellation reason is required.")
            .MaximumLength(500);
    }
}

public class RegisterPaymentRequestValidator : AbstractValidator<RegisterPaymentRequest>
{
    private static readonly string[] AllowedMethods = ["Cash", "BankTransfer", "CreditCard", "InternalCredit", "CodDeduction"];

    public RegisterPaymentRequestValidator()
    {
        RuleFor(x => x.CustomerId)
            .GreaterThan(0).WithMessage("Valid CustomerId is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Payment amount must be greater than zero.");

        RuleFor(x => x.CurrencyId)
            .GreaterThan(0).WithMessage("Valid CurrencyId is required.");

        RuleFor(x => x.PaymentMethod)
            .NotEmpty().WithMessage("Payment method is required.")
            .Must(m => AllowedMethods.Contains(m))
            .WithMessage($"Payment method must be one of: {string.Join(", ", AllowedMethods)}.");

        RuleFor(x => x.TransactionReference)
            .MaximumLength(100);

        RuleFor(x => x.BankName)
            .MaximumLength(100);

        RuleFor(x => x.BankAccount)
            .MaximumLength(50);
    }
}

public class CreatePaymentAllocationRequestValidator : AbstractValidator<CreatePaymentAllocationRequest>
{
    public CreatePaymentAllocationRequestValidator()
    {
        RuleFor(x => x.InvoiceId)
            .GreaterThan(0).WithMessage("Valid InvoiceId is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Allocated amount must be greater than zero.");
    }
}
