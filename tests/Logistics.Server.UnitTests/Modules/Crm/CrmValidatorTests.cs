using FluentAssertions;
using Logistics.Server.Application.Modules.Crm.DTOs;
using Logistics.Server.Application.Modules.Crm.Validators;
using Xunit;

namespace Logistics.Server.UnitTests.Modules.Crm;

public class CrmValidatorTests
{
    private readonly CreateCustomerRequestValidator _createCustomerValidator = new();
    private readonly UpdateCustomerRequestValidator _updateCustomerValidator = new();
    private readonly CreateCustomerContactRequestValidator _contactValidator = new();
    private readonly CreateCustomerAddressRequestValidator _addressValidator = new();
    private readonly CreateCustomerContractRequestValidator _contractValidator = new();
    private readonly CreateCustomerPricingRuleRequestValidator _pricingRuleValidator = new();

    #region Customer Validator Tests
    [Fact]
    public void CreateCustomerRequestValidator_WithValidData_ShouldPass()
    {
        var request = new CreateCustomerRequest(
            BranchId: 1,
            CustomerCode: "CUST-HN-001",
            CustomerType: "Corporate",
            Name: "Alpha Trading Company",
            TradeName: "Alpha Logistics",
            TaxCode: "0109887766",
            Email: "info@alphatrading.vn",
            PhoneNumber: "+84901234567",
            CreditLimit: 50000000,
            CreditTermDays: 30,
            PaymentType: "PostpaidMonthly"
        );

        var result = _createCustomerValidator.Validate(request);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("", "Customer code is required.")]
    [InlineData(null, "Customer code is required.")]
    public void CreateCustomerRequestValidator_WithEmptyCode_ShouldFail(string? code, string expectedMessage)
    {
        var request = new CreateCustomerRequest(
            BranchId: 1,
            CustomerCode: code!,
            CustomerType: "Individual",
            Name: "Nguyen Van A",
            TradeName: null,
            TaxCode: null,
            Email: "nguyenvana@gmail.com",
            PhoneNumber: "+84912345678"
        );

        var result = _createCustomerValidator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CustomerCode" && e.ErrorMessage == expectedMessage);
    }

    [Fact]
    public void CreateCustomerRequestValidator_WithInvalidCustomerType_ShouldFail()
    {
        var request = new CreateCustomerRequest(
            BranchId: 1,
            CustomerCode: "CUST-002",
            CustomerType: "Government",
            Name: "Department of Transportation",
            TradeName: null,
            TaxCode: null,
            Email: "transport@gov.vn",
            PhoneNumber: "+84241234567"
        );

        var result = _createCustomerValidator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CustomerType");
    }

    [Fact]
    public void CreateCustomerRequestValidator_WithNegativeCreditLimit_ShouldFail()
    {
        var request = new CreateCustomerRequest(
            BranchId: 1,
            CustomerCode: "CUST-003",
            CustomerType: "Corporate",
            Name: "Beta Corp",
            TradeName: null,
            TaxCode: null,
            Email: "finance@betacorp.vn",
            PhoneNumber: "+84909876543",
            CreditLimit: -1000
        );

        var result = _createCustomerValidator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CreditLimit");
    }

    [Fact]
    public void UpdateCustomerRequestValidator_WithValidData_ShouldPass()
    {
        var request = new UpdateCustomerRequest(
            CustomerType: "Individual",
            Name: "Tran Thi B",
            TradeName: null,
            TaxCode: null,
            Email: "b.tran@gmail.com",
            PhoneNumber: "+84988776655",
            CreditLimit: 0,
            CreditTermDays: 0,
            PaymentType: "Prepaid",
            AccountManagerEmployeeId: null
        );

        var result = _updateCustomerValidator.Validate(request);

        result.IsValid.Should().BeTrue();
    }
    #endregion

    #region Contact Validator Tests
    [Fact]
    public void CreateCustomerContactRequestValidator_WithValidData_ShouldPass()
    {
        var request = new CreateCustomerContactRequest(
            ContactName: "Le Van C",
            PhoneNumber: "+84911223344",
            JobTitle: "Procurement Manager",
            Email: "c.le@alphatrading.vn",
            IsPrimary: true
        );

        var result = _contactValidator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateCustomerContactRequestValidator_WithMissingName_ShouldFail()
    {
        var request = new CreateCustomerContactRequest(
            ContactName: "",
            PhoneNumber: "+84911223344"
        );

        var result = _contactValidator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ContactName");
    }

    [Fact]
    public void CreateCustomerContactRequestValidator_WithInvalidEmail_ShouldFail()
    {
        var request = new CreateCustomerContactRequest(
            ContactName: "Test Person",
            PhoneNumber: "+84911223344",
            Email: "not-an-email"
        );

        var result = _contactValidator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }
    #endregion

    #region Address Validator Tests
    [Fact]
    public void CreateCustomerAddressRequestValidator_WithValidData_ShouldPass()
    {
        var request = new CreateCustomerAddressRequest(
            AddressType: "PickupWarehouse",
            ContactName: "Hanoi Central Warehouse",
            PhoneNumber: "+842411223344",
            AddressLine1: "123 Pham Van Dong St",
            AddressLabel: "Main Hub",
            IsDefault: true,
            Latitude: 21.028511m,
            Longitude: 105.854444m
        );

        var result = _addressValidator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateCustomerAddressRequestValidator_WithInvalidType_ShouldFail()
    {
        var request = new CreateCustomerAddressRequest(
            AddressType: "OverseasDock",
            ContactName: "Port Authority",
            PhoneNumber: "+842411223344",
            AddressLine1: "1 Port Road"
        );

        var result = _addressValidator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AddressType");
    }
    #endregion

    #region Contract Validator Tests
    [Fact]
    public void CreateCustomerContractRequestValidator_WithValidData_ShouldPass()
    {
        var request = new CreateCustomerContractRequest(
            ContractNumber: "CTR-2026-001",
            Title: "Annual Ground Shipping Agreement",
            StartDate: new DateOnly(2026, 1, 1),
            EndDate: new DateOnly(2026, 12, 31),
            Status: "Draft",
            DiscountRate: 15.5m,
            PaymentTermDays: 30
        );

        var result = _contractValidator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateCustomerContractRequestValidator_WithEndDateBeforeStartDate_ShouldFail()
    {
        var request = new CreateCustomerContractRequest(
            ContractNumber: "CTR-2026-002",
            Title: "Invalid Date Range Agreement",
            StartDate: new DateOnly(2026, 12, 31),
            EndDate: new DateOnly(2026, 1, 1),
            Status: "Draft",
            DiscountRate: 10
        );

        var result = _contractValidator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EndDate");
    }

    [Fact]
    public void CreateCustomerContractRequestValidator_WithInvalidDiscountRate_ShouldFail()
    {
        var request = new CreateCustomerContractRequest(
            ContractNumber: "CTR-2026-003",
            Title: "Excessive Discount Agreement",
            StartDate: new DateOnly(2026, 1, 1),
            EndDate: new DateOnly(2026, 12, 31),
            Status: "Draft",
            DiscountRate: 120
        );

        var result = _contractValidator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DiscountRate");
    }
    #endregion

    #region Pricing Rule Validator Tests
    [Fact]
    public void CreateCustomerPricingRuleRequestValidator_WithValidData_ShouldPass()
    {
        var request = new CreateCustomerPricingRuleRequest(
            Name: "Volume Tier 1 Discount",
            EffectiveFrom: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EffectiveTo: new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc),
            DiscountPercent: 12.0m,
            FlatDiscountAmount: 5000,
            Priority: 1
        );

        var result = _pricingRuleValidator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateCustomerPricingRuleRequestValidator_WithEffectiveToBeforeFrom_ShouldFail()
    {
        var request = new CreateCustomerPricingRuleRequest(
            Name: "Invalid Date Pricing Rule",
            EffectiveFrom: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            EffectiveTo: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            DiscountPercent: 10
        );

        var result = _pricingRuleValidator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EffectiveTo");
    }

    [Fact]
    public void CreateCustomerPricingRuleRequestValidator_WithPriorityLessThanOne_ShouldFail()
    {
        var request = new CreateCustomerPricingRuleRequest(
            Name: "Zero Priority Rule",
            EffectiveFrom: DateTime.UtcNow,
            Priority: 0
        );

        var result = _pricingRuleValidator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Priority");
    }
    #endregion
}
