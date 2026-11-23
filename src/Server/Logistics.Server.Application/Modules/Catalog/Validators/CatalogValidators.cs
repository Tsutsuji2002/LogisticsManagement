using FluentValidation;
using Logistics.Server.Application.Modules.Catalog.DTOs;

namespace Logistics.Server.Application.Modules.Catalog.Validators;

public class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Category code is required.")
            .MaximumLength(50).WithMessage("Category code must not exceed 50 characters.")
            .Matches("^[a-zA-Z0-9_-]+$").WithMessage("Category code must contain only letters, numbers, hyphens, and underscores.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required.")
            .MaximumLength(150).WithMessage("Category name must not exceed 150 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.ParentCategoryId)
            .GreaterThan(0).WithMessage("Parent category ID must be a positive number.")
            .When(x => x.ParentCategoryId.HasValue);
    }
}

public class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required.")
            .MaximumLength(150).WithMessage("Category name must not exceed 150 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.ParentCategoryId)
            .GreaterThan(0).WithMessage("Parent category ID must be a positive number.")
            .When(x => x.ParentCategoryId.HasValue);
    }
}

public class UpdateCategoryStatusRequestValidator : AbstractValidator<UpdateCategoryStatusRequest>
{
    public UpdateCategoryStatusRequestValidator()
    {
        RuleFor(x => x.Reason)
            .MaximumLength(250).WithMessage("Status reason must not exceed 250 characters.")
            .When(x => !string.IsNullOrEmpty(x.Reason));
    }
}

public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("Valid Category ID is required.");

        RuleFor(x => x.Sku)
            .NotEmpty().WithMessage("Product SKU/Code is required.")
            .MaximumLength(50).WithMessage("Product SKU must not exceed 50 characters.")
            .Matches("^[a-zA-Z0-9_.-]+$").WithMessage("SKU must contain only alphanumeric characters, dashes, dots, and underscores.");

        RuleFor(x => x.Barcode)
            .MaximumLength(100).WithMessage("Barcode must not exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.Barcode));

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required.")
            .MaximumLength(200).WithMessage("Product name must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.UnitOfMeasureId)
            .GreaterThan(0).WithMessage("Valid Unit of Measure ID is required.");

        RuleFor(x => x.WeightKg)
            .GreaterThanOrEqualTo(0).WithMessage("Weight cannot be negative.")
            .LessThan(1000000m).WithMessage("Weight exceeds allowable limit.");

        RuleFor(x => x.LengthCm)
            .GreaterThanOrEqualTo(0).WithMessage("Length cannot be negative.")
            .LessThan(100000m).WithMessage("Length exceeds allowable limit.");

        RuleFor(x => x.WidthCm)
            .GreaterThanOrEqualTo(0).WithMessage("Width cannot be negative.")
            .LessThan(100000m).WithMessage("Width exceeds allowable limit.");

        RuleFor(x => x.HeightCm)
            .GreaterThanOrEqualTo(0).WithMessage("Height cannot be negative.")
            .LessThan(100000m).WithMessage("Height exceeds allowable limit.");

        RuleFor(x => x.DeclaredValue)
            .GreaterThanOrEqualTo(0).WithMessage("Declared value cannot be negative.");

        RuleFor(x => x.CurrencyId)
            .GreaterThan(0).WithMessage("Currency ID must be a positive number.")
            .When(x => x.CurrencyId.HasValue);
    }
}

public class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("Valid Category ID is required.");

        RuleFor(x => x.Barcode)
            .MaximumLength(100).WithMessage("Barcode must not exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.Barcode));

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required.")
            .MaximumLength(200).WithMessage("Product name must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.UnitOfMeasureId)
            .GreaterThan(0).WithMessage("Valid Unit of Measure ID is required.");

        RuleFor(x => x.WeightKg)
            .GreaterThanOrEqualTo(0).WithMessage("Weight cannot be negative.")
            .LessThan(1000000m).WithMessage("Weight exceeds allowable limit.");

        RuleFor(x => x.LengthCm)
            .GreaterThanOrEqualTo(0).WithMessage("Length cannot be negative.")
            .LessThan(100000m).WithMessage("Length exceeds allowable limit.");

        RuleFor(x => x.WidthCm)
            .GreaterThanOrEqualTo(0).WithMessage("Width cannot be negative.")
            .LessThan(100000m).WithMessage("Width exceeds allowable limit.");

        RuleFor(x => x.HeightCm)
            .GreaterThanOrEqualTo(0).WithMessage("Height cannot be negative.")
            .LessThan(100000m).WithMessage("Height exceeds allowable limit.");

        RuleFor(x => x.DeclaredValue)
            .GreaterThanOrEqualTo(0).WithMessage("Declared value cannot be negative.");

        RuleFor(x => x.CurrencyId)
            .GreaterThan(0).WithMessage("Currency ID must be a positive number.")
            .When(x => x.CurrencyId.HasValue);
    }
}

public class UpdateProductStatusRequestValidator : AbstractValidator<UpdateProductStatusRequest>
{
    public UpdateProductStatusRequestValidator()
    {
        RuleFor(x => x.Reason)
            .MaximumLength(250).WithMessage("Status reason must not exceed 250 characters.")
            .When(x => !string.IsNullOrEmpty(x.Reason));
    }
}

public class CreateUnitOfMeasureRequestValidator : AbstractValidator<CreateUnitOfMeasureRequest>
{
    private static readonly string[] AllowedTypes = ["Weight", "Dimension", "Volume", "Count", "Area", "Time"];

    public CreateUnitOfMeasureRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Unit code is required.")
            .MaximumLength(20).WithMessage("Unit code must not exceed 20 characters.")
            .Matches("^[a-zA-Z0-9_-]+$").WithMessage("Unit code must contain only alphanumeric characters, dashes, and underscores.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Unit name is required.")
            .MaximumLength(50).WithMessage("Unit name must not exceed 50 characters.");

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Unit type is required.")
            .MaximumLength(30).WithMessage("Unit type must not exceed 30 characters.")
            .Must(type => AllowedTypes.Contains(type, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Unit type must be one of: {string.Join(", ", AllowedTypes)}.");

        RuleFor(x => x.ConversionFactor)
            .GreaterThan(0).WithMessage("Conversion factor must be greater than zero.");
    }
}

public class UpdateUnitOfMeasureRequestValidator : AbstractValidator<UpdateUnitOfMeasureRequest>
{
    private static readonly string[] AllowedTypes = ["Weight", "Dimension", "Volume", "Count", "Area", "Time"];

    public UpdateUnitOfMeasureRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Unit name is required.")
            .MaximumLength(50).WithMessage("Unit name must not exceed 50 characters.");

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Unit type is required.")
            .MaximumLength(30).WithMessage("Unit type must not exceed 30 characters.")
            .Must(type => AllowedTypes.Contains(type, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Unit type must be one of: {string.Join(", ", AllowedTypes)}.");

        RuleFor(x => x.ConversionFactor)
            .GreaterThan(0).WithMessage("Conversion factor must be greater than zero.");
    }
}

public class UpdateUnitStatusRequestValidator : AbstractValidator<UpdateUnitStatusRequest>
{
    public UpdateUnitStatusRequestValidator()
    {
        RuleFor(x => x.Reason)
            .MaximumLength(250).WithMessage("Status reason must not exceed 250 characters.")
            .When(x => !string.IsNullOrEmpty(x.Reason));
    }
}
