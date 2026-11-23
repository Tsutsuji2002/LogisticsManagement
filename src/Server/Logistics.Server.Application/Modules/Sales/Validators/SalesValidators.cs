using FluentValidation;
using Logistics.Server.Application.Modules.Sales.DTOs;

namespace Logistics.Server.Application.Modules.Sales.Validators;

public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.CustomerId)
            .GreaterThan(0)
            .WithMessage("CustomerId must be greater than 0.");

        RuleFor(x => x.BranchId)
            .GreaterThan(0)
            .When(x => x.BranchId.HasValue)
            .WithMessage("BranchId must be greater than 0 when specified.");

        RuleFor(x => x.CodAmount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("CodAmount cannot be negative.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .WithMessage("Notes cannot exceed 1000 characters.");

        RuleFor(x => x)
            .Must(x => !x.RequestedPickupDate.HasValue || !x.RequiredDeliveryDate.HasValue || x.RequiredDeliveryDate.Value >= x.RequestedPickupDate.Value)
            .WithMessage("Required delivery date cannot be earlier than requested pickup date.");

        When(x => x.PickupAddress != null, () =>
        {
            RuleFor(x => x.PickupAddress!)
                .SetValidator(new OrderAddressRequestValidator());
        });

        When(x => x.DeliveryAddress != null, () =>
        {
            RuleFor(x => x.DeliveryAddress!)
                .SetValidator(new OrderAddressRequestValidator());
        });

        RuleForEach(x => x.Items)
            .SetValidator(new CreateOrderItemRequestValidator())
            .When(x => x.Items != null && x.Items.Count > 0);

        RuleForEach(x => x.Packages)
            .SetValidator(new CreateOrderPackageRequestValidator())
            .When(x => x.Packages != null && x.Packages.Count > 0);
    }
}

public class UpdateOrderRequestValidator : AbstractValidator<UpdateOrderRequest>
{
    public UpdateOrderRequestValidator()
    {
        RuleFor(x => x.CodAmount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("CodAmount cannot be negative.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .WithMessage("Notes cannot exceed 1000 characters.");

        RuleFor(x => x)
            .Must(x => !x.RequestedPickupDate.HasValue || !x.RequiredDeliveryDate.HasValue || x.RequiredDeliveryDate.Value >= x.RequestedPickupDate.Value)
            .WithMessage("Required delivery date cannot be earlier than requested pickup date.");
    }
}

public class CancelOrderRequestValidator : AbstractValidator<CancelOrderRequest>
{
    public CancelOrderRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Cancellation reason is required.")
            .MaximumLength(500)
            .WithMessage("Cancellation reason cannot exceed 500 characters.");
    }
}

public class CreateOrderItemRequestValidator : AbstractValidator<CreateOrderItemRequest>
{
    public CreateOrderItemRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => (x.ProductId.HasValue && x.ProductId.Value > 0) || !string.IsNullOrWhiteSpace(x.ProductName))
            .WithMessage("Either ProductId or ProductName must be provided.");

        RuleFor(x => x.ProductName)
            .MaximumLength(200)
            .WithMessage("ProductName cannot exceed 200 characters.");

        RuleFor(x => x.Sku)
            .MaximumLength(50)
            .WithMessage("Sku cannot exceed 50 characters.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than 0.");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0)
            .WithMessage("UnitPrice cannot be negative.");

        RuleFor(x => x.DeclaredValue)
            .GreaterThanOrEqualTo(0)
            .WithMessage("DeclaredValue cannot be negative.");

        RuleFor(x => x.WeightKg)
            .GreaterThanOrEqualTo(0)
            .WithMessage("WeightKg cannot be negative.");

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .WithMessage("Notes cannot exceed 500 characters.");
    }
}

public class UpdateOrderItemRequestValidator : AbstractValidator<UpdateOrderItemRequest>
{
    public UpdateOrderItemRequestValidator()
    {
        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than 0.");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0)
            .WithMessage("UnitPrice cannot be negative.");

        RuleFor(x => x.DeclaredValue)
            .GreaterThanOrEqualTo(0)
            .WithMessage("DeclaredValue cannot be negative.");

        RuleFor(x => x.WeightKg)
            .GreaterThanOrEqualTo(0)
            .WithMessage("WeightKg cannot be negative.");

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .WithMessage("Notes cannot exceed 500 characters.");
    }
}

public class CreateOrderPackageRequestValidator : AbstractValidator<CreateOrderPackageRequest>
{
    public CreateOrderPackageRequestValidator()
    {
        RuleFor(x => x.WeightKg)
            .GreaterThan(0)
            .WithMessage("WeightKg must be greater than 0.");

        RuleFor(x => x.LengthCm)
            .GreaterThanOrEqualTo(0)
            .WithMessage("LengthCm cannot be negative.");

        RuleFor(x => x.WidthCm)
            .GreaterThanOrEqualTo(0)
            .WithMessage("WidthCm cannot be negative.");

        RuleFor(x => x.HeightCm)
            .GreaterThanOrEqualTo(0)
            .WithMessage("HeightCm cannot be negative.");

        RuleFor(x => x.DeclaredValue)
            .GreaterThanOrEqualTo(0)
            .WithMessage("DeclaredValue cannot be negative.");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Description cannot exceed 500 characters.");

        RuleFor(x => x.Barcode)
            .MaximumLength(100)
            .WithMessage("Barcode cannot exceed 100 characters.");

        RuleFor(x => x.SpecialInstructions)
            .MaximumLength(500)
            .WithMessage("SpecialInstructions cannot exceed 500 characters.");
    }
}

public class UpdateOrderPackageRequestValidator : AbstractValidator<UpdateOrderPackageRequest>
{
    public UpdateOrderPackageRequestValidator()
    {
        RuleFor(x => x.WeightKg)
            .GreaterThan(0)
            .WithMessage("WeightKg must be greater than 0.");

        RuleFor(x => x.LengthCm)
            .GreaterThanOrEqualTo(0)
            .WithMessage("LengthCm cannot be negative.");

        RuleFor(x => x.WidthCm)
            .GreaterThanOrEqualTo(0)
            .WithMessage("WidthCm cannot be negative.");

        RuleFor(x => x.HeightCm)
            .GreaterThanOrEqualTo(0)
            .WithMessage("HeightCm cannot be negative.");

        RuleFor(x => x.DeclaredValue)
            .GreaterThanOrEqualTo(0)
            .WithMessage("DeclaredValue cannot be negative.");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Description cannot exceed 500 characters.");

        RuleFor(x => x.Barcode)
            .MaximumLength(100)
            .WithMessage("Barcode cannot exceed 100 characters.");

        RuleFor(x => x.SpecialInstructions)
            .MaximumLength(500)
            .WithMessage("SpecialInstructions cannot exceed 500 characters.");
    }
}

public class OrderAddressRequestValidator : AbstractValidator<OrderAddressRequest>
{
    public OrderAddressRequestValidator()
    {
        RuleFor(x => x.ContactName)
            .NotEmpty()
            .WithMessage("ContactName is required.")
            .MaximumLength(150)
            .WithMessage("ContactName cannot exceed 150 characters.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .WithMessage("PhoneNumber is required.")
            .MaximumLength(50)
            .WithMessage("PhoneNumber cannot exceed 50 characters.");

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Email format is invalid.")
            .MaximumLength(150)
            .WithMessage("Email cannot exceed 150 characters.");

        RuleFor(x => x.AddressLine1)
            .NotEmpty()
            .WithMessage("AddressLine1 is required.")
            .MaximumLength(250)
            .WithMessage("AddressLine1 cannot exceed 250 characters.");

        RuleFor(x => x.AddressLine2)
            .MaximumLength(250)
            .WithMessage("AddressLine2 cannot exceed 250 characters.");

        RuleFor(x => x.PostalCode)
            .MaximumLength(20)
            .WithMessage("PostalCode cannot exceed 20 characters.");

        RuleFor(x => x.FormattedAddress)
            .MaximumLength(500)
            .WithMessage("FormattedAddress cannot exceed 500 characters.");
    }
}
