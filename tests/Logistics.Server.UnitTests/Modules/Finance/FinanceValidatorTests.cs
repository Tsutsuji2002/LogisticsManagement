using FluentAssertions;
using Logistics.Server.Application.Modules.Finance.DTOs;
using Logistics.Server.Application.Modules.Finance.Validators;
using Xunit;

namespace Logistics.Server.UnitTests.Modules.Finance;

public class FinanceValidatorTests
{
    [Fact]
    public void CreateShippingRateCardRequestValidator_ValidRequest_Passes()
    {
        var validator = new CreateShippingRateCardRequestValidator();
        var request = new CreateShippingRateCardRequest
        {
            Code = "RATE-STD-2026",
            Name = "Standard Ground Domestic 2026",
            BaseCurrencyId = 1,
            EffectiveFrom = DateTime.UtcNow
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("rate code with spaces")]
    [InlineData("RATE@CARD")]
    public void CreateShippingRateCardRequestValidator_InvalidCode_Fails(string code)
    {
        var validator = new CreateShippingRateCardRequestValidator();
        var request = new CreateShippingRateCardRequest
        {
            Code = code,
            Name = "Rate Card",
            BaseCurrencyId = 1,
            EffectiveFrom = DateTime.UtcNow
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    [Fact]
    public void CreateShippingRateCardRequestValidator_InvalidEffectiveTo_Fails()
    {
        var validator = new CreateShippingRateCardRequestValidator();
        var request = new CreateShippingRateCardRequest
        {
            Code = "RATE-01",
            Name = "Rate Card",
            BaseCurrencyId = 1,
            EffectiveFrom = DateTime.UtcNow,
            EffectiveTo = DateTime.UtcNow.AddDays(-5) // Inverted date
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EffectiveTo");
    }

    [Fact]
    public void CreateShippingRateRuleRequestValidator_ValidRequest_Passes()
    {
        var validator = new CreateShippingRateRuleRequestValidator();
        var request = new CreateShippingRateRuleRequest
        {
            ServiceType = "StandardGround",
            FromWeightKg = 0m,
            ToWeightKg = 5m,
            BaseRate = 20000m,
            AdditionalPerKgRate = 2500m,
            MinimumFee = 20000m
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateShippingRateRuleRequestValidator_InvertedWeightRange_Fails()
    {
        var validator = new CreateShippingRateRuleRequestValidator();
        var request = new CreateShippingRateRuleRequest
        {
            ServiceType = "StandardGround",
            FromWeightKg = 10m,
            ToWeightKg = 5m, // from > to
            BaseRate = 20000m,
            AdditionalPerKgRate = 2500m
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ToWeightKg");
    }

    [Fact]
    public void CalculateShippingFeeRequestValidator_ValidRequest_Passes()
    {
        var validator = new CalculateShippingFeeRequestValidator();
        var request = new CalculateShippingFeeRequest
        {
            CustomerId = 1,
            ServiceType = "StandardGround",
            WeightKg = 2.5m,
            DeclaredValue = 500000m,
            CodAmount = 0m
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CalculateShippingFeeRequestValidator_ZeroWeight_Fails()
    {
        var validator = new CalculateShippingFeeRequestValidator();
        var request = new CalculateShippingFeeRequest
        {
            CustomerId = 1,
            ServiceType = "StandardGround",
            WeightKg = 0m
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "WeightKg");
    }

    [Fact]
    public void CreateChargeAdjustmentRequestValidator_ValidRequest_Passes()
    {
        var validator = new CreateChargeAdjustmentRequestValidator();
        var request = new CreateChargeAdjustmentRequest
        {
            ChargeType = "FuelSurcharge",
            Amount = 15000m,
            CurrencyId = 1,
            Description = "Unexpected remote distance surcharge"
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateChargeAdjustmentRequestValidator_ZeroAmount_Fails()
    {
        var validator = new CreateChargeAdjustmentRequestValidator();
        var request = new CreateChargeAdjustmentRequest
        {
            ChargeType = "FuelSurcharge",
            Amount = 0m,
            CurrencyId = 1,
            Description = "No cost"
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Amount");
    }

    [Fact]
    public void CreateInvoiceRequestValidator_ValidRequest_Passes()
    {
        var validator = new CreateInvoiceRequestValidator();
        var request = new CreateInvoiceRequest
        {
            CustomerId = 1,
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            TaxRate = 10m,
            PaymentTermsDays = 30,
            Items =
            [
                new CreateInvoiceItemRequest
                {
                    Description = "Freight delivery for shipment #1001",
                    Quantity = 1,
                    UnitPrice = 50000m
                }
            ]
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateInvoiceRequestValidator_InvalidCustomerId_Fails()
    {
        var validator = new CreateInvoiceRequestValidator();
        var request = new CreateInvoiceRequest
        {
            CustomerId = 0,
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30))
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CustomerId");
    }

    [Fact]
    public void RegisterPaymentRequestValidator_ValidRequest_Passes()
    {
        var validator = new RegisterPaymentRequestValidator();
        var request = new RegisterPaymentRequest
        {
            CustomerId = 1,
            CurrencyId = 1,
            Amount = 500000m,
            PaymentMethod = "BankTransfer",
            PaymentDate = DateTime.UtcNow,
            TransactionReference = "TXN-BANK-998822"
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RegisterPaymentRequestValidator_NegativeAmount_Fails()
    {
        var validator = new RegisterPaymentRequestValidator();
        var request = new RegisterPaymentRequest
        {
            CustomerId = 1,
            CurrencyId = 1,
            Amount = -100m,
            PaymentMethod = "Cash",
            PaymentDate = DateTime.UtcNow
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Amount");
    }

    [Fact]
    public void CreatePaymentAllocationRequestValidator_ValidRequest_Passes()
    {
        var validator = new CreatePaymentAllocationRequestValidator();
        var request = new CreatePaymentAllocationRequest
        {
            InvoiceId = 10,
            Amount = 250000m
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreatePaymentAllocationRequestValidator_ZeroAllocatedAmount_Fails()
    {
        var validator = new CreatePaymentAllocationRequestValidator();
        var request = new CreatePaymentAllocationRequest
        {
            InvoiceId = 10,
            Amount = 0m
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Amount");
    }
}
