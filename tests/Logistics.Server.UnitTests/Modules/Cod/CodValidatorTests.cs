using FluentAssertions;
using Logistics.Server.Application.Modules.Cod.DTOs;
using Logistics.Server.Application.Modules.Cod.Validators;
using Xunit;

namespace Logistics.Server.UnitTests.Modules.Cod;

public class CodValidatorTests
{
    // ═══════════════════════════════════════════════════════════════
    //  1. CreateCodCollectionRequestValidator Tests
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void CreateCodCollectionRequestValidator_ValidRequest_Passes()
    {
        var validator = new CreateCodCollectionRequestValidator();
        var request = new CreateCodCollectionRequest
        {
            ShipmentId = 10,
            CollectedAmount = 350000m,
            CollectedByEmployeeId = 5,
            DriverReceiptNumber = "RCP-2026-001",
            Notes = "Collected in cash from recipient"
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateCodCollectionRequestValidator_InvalidShipmentId_Fails(long shipmentId)
    {
        var validator = new CreateCodCollectionRequestValidator();
        var request = new CreateCodCollectionRequest
        {
            ShipmentId = shipmentId,
            CollectedAmount = 100000m,
            CollectedByEmployeeId = 1
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ShipmentId");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50000)]
    public void CreateCodCollectionRequestValidator_NonPositiveAmount_Fails(decimal amount)
    {
        var validator = new CreateCodCollectionRequestValidator();
        var request = new CreateCodCollectionRequest
        {
            ShipmentId = 1,
            CollectedAmount = amount,
            CollectedByEmployeeId = 1
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CollectedAmount");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateCodCollectionRequestValidator_InvalidEmployeeId_Fails(long empId)
    {
        var validator = new CreateCodCollectionRequestValidator();
        var request = new CreateCodCollectionRequest
        {
            ShipmentId = 1,
            CollectedAmount = 100000m,
            CollectedByEmployeeId = empId
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CollectedByEmployeeId");
    }

    [Fact]
    public void CreateCodCollectionRequestValidator_ExcessiveLength_Fails()
    {
        var validator = new CreateCodCollectionRequestValidator();
        var request = new CreateCodCollectionRequest
        {
            ShipmentId = 1,
            CollectedAmount = 100000m,
            CollectedByEmployeeId = 1,
            DriverReceiptNumber = new string('A', 51),
            Notes = new string('B', 501)
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DriverReceiptNumber");
        result.Errors.Should().Contain(e => e.PropertyName == "Notes");
    }

    // ═══════════════════════════════════════════════════════════════
    //  2. RecordCodCollectionRequestValidator Tests
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void RecordCodCollectionRequestValidator_ValidRequest_Passes()
    {
        var validator = new RecordCodCollectionRequestValidator();
        var request = new RecordCodCollectionRequest
        {
            CollectedAmount = 250000m,
            DriverReceiptNumber = "REC-999",
            Notes = "Paid cash"
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void RecordCodCollectionRequestValidator_NonPositiveAmount_Fails(decimal amount)
    {
        var validator = new RecordCodCollectionRequestValidator();
        var request = new RecordCodCollectionRequest
        {
            CollectedAmount = amount
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CollectedAmount");
    }

    // ═══════════════════════════════════════════════════════════════
    //  3. RecordCodFailureRequestValidator Tests
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void RecordCodFailureRequestValidator_ValidRequest_Passes()
    {
        var validator = new RecordCodFailureRequestValidator();
        var request = new RecordCodFailureRequest
        {
            ReasonCode = "CustomerRefusedPayment",
            Notes = "Customer refused because package was slightly damaged."
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RecordCodFailureRequestValidator_EmptyReasonCode_Fails(string reason)
    {
        var validator = new RecordCodFailureRequestValidator();
        var request = new RecordCodFailureRequest
        {
            ReasonCode = reason
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ReasonCode");
    }

    // ═══════════════════════════════════════════════════════════════
    //  4. VerifyCodCollectionRequestValidator Tests
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void VerifyCodCollectionRequestValidator_ValidRequest_Passes()
    {
        var validator = new VerifyCodCollectionRequestValidator();
        var request = new VerifyCodCollectionRequest
        {
            Notes = "Cash verified and deposited into branch safe."
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void VerifyCodCollectionRequestValidator_ExcessiveNotes_Fails()
    {
        var validator = new VerifyCodCollectionRequestValidator();
        var request = new VerifyCodCollectionRequest
        {
            Notes = new string('N', 501)
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Notes");
    }

    // ═══════════════════════════════════════════════════════════════
    //  5. CreateCodRemittanceRequestValidator Tests
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void CreateCodRemittanceRequestValidator_ValidRequest_Passes()
    {
        var validator = new CreateCodRemittanceRequestValidator();
        var request = new CreateCodRemittanceRequest
        {
            CustomerId = 1,
            CurrencyId = 1,
            PaymentMethod = "BankTransfer",
            BankName = "Vietcombank",
            BankAccount = "0123456789",
            AccountHolderName = "Nguyen Van A",
            CollectionIds = [1, 2, 3],
            ServiceFeesDeducted = 50000m
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateCodRemittanceRequestValidator_EmptyCollections_Fails()
    {
        var validator = new CreateCodRemittanceRequestValidator();
        var request = new CreateCodRemittanceRequest
        {
            CustomerId = 1,
            CurrencyId = 1,
            PaymentMethod = "BankTransfer",
            CollectionIds = []
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CollectionIds");
    }

    [Fact]
    public void CreateCodRemittanceRequestValidator_NegativeFee_Fails()
    {
        var validator = new CreateCodRemittanceRequestValidator();
        var request = new CreateCodRemittanceRequest
        {
            CustomerId = 1,
            CurrencyId = 1,
            PaymentMethod = "BankTransfer",
            CollectionIds = [1],
            ServiceFeesDeducted = -100m
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ServiceFeesDeducted");
    }

    // ═══════════════════════════════════════════════════════════════
    //  6. AddCodRemittanceItemRequestValidator Tests
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void AddCodRemittanceItemRequestValidator_ValidRequest_Passes()
    {
        var validator = new AddCodRemittanceItemRequestValidator();
        var request = new AddCodRemittanceItemRequest
        {
            CODCollectionId = 15,
            DeductedShippingFee = 25000m
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void AddCodRemittanceItemRequestValidator_InvalidId_Fails()
    {
        var validator = new AddCodRemittanceItemRequestValidator();
        var request = new AddCodRemittanceItemRequest
        {
            CODCollectionId = 0,
            DeductedShippingFee = 1000m
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CODCollectionId");
    }

    // ═══════════════════════════════════════════════════════════════
    //  7. ConfirmCodRemittanceRequestValidator Tests
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void ConfirmCodRemittanceRequestValidator_ValidRequest_Passes()
    {
        var validator = new ConfirmCodRemittanceRequestValidator();
        var request = new ConfirmCodRemittanceRequest
        {
            PaymentReference = "TXN-BANK-123456",
            ActualRemittedAmount = 1500000m,
            Notes = "Transfer confirmed by corporate accountant"
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ConfirmCodRemittanceRequestValidator_NonPositiveActualAmount_Fails()
    {
        var validator = new ConfirmCodRemittanceRequestValidator();
        var request = new ConfirmCodRemittanceRequest
        {
            ActualRemittedAmount = 0m
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ActualRemittedAmount");
    }

    // ═══════════════════════════════════════════════════════════════
    //  8. CancelCodRemittanceRequestValidator Tests
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void CancelCodRemittanceRequestValidator_ValidRequest_Passes()
    {
        var validator = new CancelCodRemittanceRequestValidator();
        var request = new CancelCodRemittanceRequest
        {
            Reason = "Customer bank account number was incorrect; reissuing new remittance."
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CancelCodRemittanceRequestValidator_EmptyReason_Fails(string reason)
    {
        var validator = new CancelCodRemittanceRequestValidator();
        var request = new CancelCodRemittanceRequest
        {
            Reason = reason
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Reason");
    }
}
