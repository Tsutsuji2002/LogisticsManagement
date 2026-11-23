using FluentAssertions;
using Logistics.Server.Application.Modules.Catalog.DTOs;
using Logistics.Server.Application.Modules.Catalog.Validators;
using Xunit;

namespace Logistics.Server.UnitTests.Modules.Catalog;

public class CatalogValidatorTests
{
    private readonly CreateCategoryRequestValidator _createCategoryValidator = new();
    private readonly UpdateCategoryRequestValidator _updateCategoryValidator = new();
    private readonly UpdateCategoryStatusRequestValidator _updateCategoryStatusValidator = new();

    private readonly CreateProductRequestValidator _createProductValidator = new();
    private readonly UpdateProductRequestValidator _updateProductValidator = new();
    private readonly UpdateProductStatusRequestValidator _updateProductStatusValidator = new();

    private readonly CreateUnitOfMeasureRequestValidator _createUnitValidator = new();
    private readonly UpdateUnitOfMeasureRequestValidator _updateUnitValidator = new();
    private readonly UpdateUnitStatusRequestValidator _updateUnitStatusValidator = new();

    // ==========================================
    // Category Validator Tests
    // ==========================================

    [Fact]
    public void CreateCategory_WithValidData_ShouldPassValidation()
    {
        var request = new CreateCategoryRequest
        {
            Code = "ELEC-01",
            Name = "Consumer Electronics",
            Description = "Smartphones, laptops, and tablets",
            ParentCategoryId = null
        };

        var result = _createCategoryValidator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreateCategory_WithEmptyCode_ShouldFailValidation(string? code)
    {
        var request = new CreateCategoryRequest
        {
            Code = code!,
            Name = "Electronics"
        };

        var result = _createCategoryValidator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCategoryRequest.Code));
    }

    [Fact]
    public void CreateCategory_WithSpecialCharactersInCode_ShouldFailValidation()
    {
        var request = new CreateCategoryRequest
        {
            Code = "ELEC@#$%",
            Name = "Electronics"
        };

        var result = _createCategoryValidator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCategoryRequest.Code));
    }

    [Fact]
    public void CreateCategory_WithNegativeParentId_ShouldFailValidation()
    {
        var request = new CreateCategoryRequest
        {
            Code = "SUB-ELEC",
            Name = "Sub Category",
            ParentCategoryId = -5
        };

        var result = _createCategoryValidator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCategoryRequest.ParentCategoryId));
    }

    [Fact]
    public void UpdateCategory_WithEmptyName_ShouldFailValidation()
    {
        var request = new UpdateCategoryRequest
        {
            Name = "",
            Description = "Updated Description"
        };

        var result = _updateCategoryValidator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateCategoryRequest.Name));
    }

    [Fact]
    public void UpdateCategoryStatus_WithExcessiveReason_ShouldFailValidation()
    {
        var request = new UpdateCategoryStatusRequest
        {
            IsActive = false,
            Reason = new string('A', 300)
        };

        var result = _updateCategoryStatusValidator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateCategoryStatusRequest.Reason));
    }

    // ==========================================
    // Product Validator Tests
    // ==========================================

    [Fact]
    public void CreateProduct_WithValidData_ShouldPassValidation()
    {
        var request = new CreateProductRequest
        {
            CategoryId = 1,
            Sku = "SKU-IPHONE-15",
            Barcode = "8938500011223",
            Name = "Apple iPhone 15 Pro Max 256GB",
            Description = "Titanium blue smartphone",
            UnitOfMeasureId = 1,
            WeightKg = 0.221m,
            LengthCm = 15.99m,
            WidthCm = 7.67m,
            HeightCm = 0.82m,
            DeclaredValue = 29990000m,
            CurrencyId = 1,
            IsPerishable = false,
            IsDangerous = false
        };

        var result = _createProductValidator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateProduct_WithInvalidCategoryId_ShouldFailValidation(long categoryId)
    {
        var request = new CreateProductRequest
        {
            CategoryId = categoryId,
            Sku = "SKU-TEST",
            Name = "Test Product",
            UnitOfMeasureId = 1
        };

        var result = _createProductValidator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductRequest.CategoryId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreateProduct_WithEmptySku_ShouldFailValidation(string? sku)
    {
        var request = new CreateProductRequest
        {
            CategoryId = 1,
            Sku = sku!,
            Name = "Test Product",
            UnitOfMeasureId = 1
        };

        var result = _createProductValidator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductRequest.Sku));
    }

    [Theory]
    [InlineData(-1, 10, 10, 10)]
    [InlineData(10, -1, 10, 10)]
    [InlineData(10, 10, -1, 10)]
    [InlineData(10, 10, 10, -1)]
    public void CreateProduct_WithNegativeWeightOrDimensions_ShouldFailValidation(
        decimal weight, decimal length, decimal width, decimal height)
    {
        var request = new CreateProductRequest
        {
            CategoryId = 1,
            Sku = "SKU-DIM-TEST",
            Name = "Test Product",
            UnitOfMeasureId = 1,
            WeightKg = weight,
            LengthCm = length,
            WidthCm = width,
            HeightCm = height
        };

        var result = _createProductValidator.Validate(request);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateProduct_WithNegativeDeclaredValue_ShouldFailValidation()
    {
        var request = new CreateProductRequest
        {
            CategoryId = 1,
            Sku = "SKU-VAL-TEST",
            Name = "Test Product",
            UnitOfMeasureId = 1,
            DeclaredValue = -50000m
        };

        var result = _createProductValidator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductRequest.DeclaredValue));
    }

    [Fact]
    public void UpdateProduct_WithInvalidDimensions_ShouldFailValidation()
    {
        var request = new UpdateProductRequest
        {
            CategoryId = 1,
            Name = "Updated Product",
            UnitOfMeasureId = 1,
            WeightKg = -2.5m
        };

        var result = _updateProductValidator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateProductRequest.WeightKg));
    }

    // ==========================================
    // Unit of Measure Validator Tests
    // ==========================================

    [Fact]
    public void CreateUnitOfMeasure_WithValidData_ShouldPassValidation()
    {
        var request = new CreateUnitOfMeasureRequest
        {
            Code = "KG",
            Name = "Kilogram",
            Type = "Weight",
            ConversionFactor = 1.0m,
            IsBaseUnit = true
        };

        var result = _createUnitValidator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateUnitOfMeasure_WithInvalidType_ShouldFailValidation()
    {
        var request = new CreateUnitOfMeasureRequest
        {
            Code = "XYZ",
            Name = "Custom Measurement",
            Type = "Astronomical",
            ConversionFactor = 1.0m
        };

        var result = _createUnitValidator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUnitOfMeasureRequest.Type));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1.5)]
    public void CreateUnitOfMeasure_WithNonPositiveConversionFactor_ShouldFailValidation(decimal factor)
    {
        var request = new CreateUnitOfMeasureRequest
        {
            Code = "GRAM",
            Name = "Gram",
            Type = "Weight",
            ConversionFactor = factor
        };

        var result = _createUnitValidator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUnitOfMeasureRequest.ConversionFactor));
    }

    [Fact]
    public void UpdateUnitOfMeasure_WithValidData_ShouldPassValidation()
    {
        var request = new UpdateUnitOfMeasureRequest
        {
            Name = "Pounds Mass",
            Type = "Weight",
            ConversionFactor = 0.453592m,
            IsBaseUnit = false
        };

        var result = _updateUnitValidator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdateUnitStatus_WithExcessiveReason_ShouldFailValidation()
    {
        var request = new UpdateUnitStatusRequest
        {
            IsActive = false,
            Reason = new string('U', 255)
        };

        var result = _updateUnitStatusValidator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateUnitStatusRequest.Reason));
    }
}
