using FluentValidation;
using Logistics.Server.Application.Modules.Cod.DTOs;

namespace Logistics.Server.Application.Modules.Cod.Validators;

public class CreateCodCollectionRequestValidator : AbstractValidator<CreateCodCollectionRequest>
{
    public CreateCodCollectionRequestValidator()
    {
        RuleFor(x => x.ShipmentId)
            .GreaterThan(0).WithMessage("Valid ShipmentId is required.");

        RuleFor(x => x.CollectedAmount)
            .GreaterThan(0).WithMessage("CollectedAmount must be greater than zero.");

        RuleFor(x => x.CollectedByEmployeeId)
            .GreaterThan(0).WithMessage("Valid CollectedByEmployeeId is required.");

        RuleFor(x => x.DriverReceiptNumber)
            .MaximumLength(50).WithMessage("DriverReceiptNumber cannot exceed 50 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes cannot exceed 500 characters.");
    }
}

public class RecordCodCollectionRequestValidator : AbstractValidator<RecordCodCollectionRequest>
{
    public RecordCodCollectionRequestValidator()
    {
        RuleFor(x => x.CollectedAmount)
            .GreaterThan(0).WithMessage("CollectedAmount must be greater than zero.");

        RuleFor(x => x.DriverReceiptNumber)
            .MaximumLength(50).WithMessage("DriverReceiptNumber cannot exceed 50 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes cannot exceed 500 characters.");
    }
}

public class RecordCodFailureRequestValidator : AbstractValidator<RecordCodFailureRequest>
{
    public RecordCodFailureRequestValidator()
    {
        RuleFor(x => x.ReasonCode)
            .NotEmpty().WithMessage("ReasonCode is required.")
            .MaximumLength(100).WithMessage("ReasonCode cannot exceed 100 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes cannot exceed 500 characters.");
    }
}

public class VerifyCodCollectionRequestValidator : AbstractValidator<VerifyCodCollectionRequest>
{
    public VerifyCodCollectionRequestValidator()
    {
        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes cannot exceed 500 characters.");
    }
}

public class CreateCodRemittanceRequestValidator : AbstractValidator<CreateCodRemittanceRequest>
{
    public CreateCodRemittanceRequestValidator()
    {
        RuleFor(x => x.CustomerId)
            .GreaterThan(0).WithMessage("Valid CustomerId is required.");

        RuleFor(x => x.CurrencyId)
            .GreaterThan(0).WithMessage("Valid CurrencyId is required.");

        RuleFor(x => x.PaymentMethod)
            .NotEmpty().WithMessage("PaymentMethod is required.")
            .MaximumLength(50).WithMessage("PaymentMethod cannot exceed 50 characters.");

        RuleFor(x => x.BankName)
            .MaximumLength(100).WithMessage("BankName cannot exceed 100 characters.");

        RuleFor(x => x.BankAccount)
            .MaximumLength(50).WithMessage("BankAccount cannot exceed 50 characters.");

        RuleFor(x => x.AccountHolderName)
            .MaximumLength(150).WithMessage("AccountHolderName cannot exceed 150 characters.");

        RuleFor(x => x.CollectionIds)
            .NotNull().WithMessage("CollectionIds list cannot be null.")
            .NotEmpty().WithMessage("At least one COD collection must be selected for remittance.");

        RuleFor(x => x.ServiceFeesDeducted)
            .GreaterThanOrEqualTo(0).WithMessage("ServiceFeesDeducted must be non-negative.");
    }
}

public class AddCodRemittanceItemRequestValidator : AbstractValidator<AddCodRemittanceItemRequest>
{
    public AddCodRemittanceItemRequestValidator()
    {
        RuleFor(x => x.CODCollectionId)
            .GreaterThan(0).WithMessage("Valid CODCollectionId is required.");

        RuleFor(x => x.DeductedShippingFee)
            .GreaterThanOrEqualTo(0).WithMessage("DeductedShippingFee must be non-negative.");
    }
}

public class ConfirmCodRemittanceRequestValidator : AbstractValidator<ConfirmCodRemittanceRequest>
{
    public ConfirmCodRemittanceRequestValidator()
    {
        RuleFor(x => x.ActualRemittedAmount)
            .GreaterThan(0).When(x => x.ActualRemittedAmount.HasValue)
            .WithMessage("ActualRemittedAmount must be greater than zero when specified.");

        RuleFor(x => x.PaymentReference)
            .MaximumLength(100).WithMessage("PaymentReference cannot exceed 100 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes cannot exceed 500 characters.");
    }
}

public class CancelCodRemittanceRequestValidator : AbstractValidator<CancelCodRemittanceRequest>
{
    public CancelCodRemittanceRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Cancellation reason is required.")
            .MaximumLength(500).WithMessage("Cancellation reason cannot exceed 500 characters.");
    }
}
