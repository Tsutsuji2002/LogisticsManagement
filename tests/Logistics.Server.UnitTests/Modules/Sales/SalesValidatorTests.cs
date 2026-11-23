using FluentAssertions;
using Logistics.Server.Application.Modules.Sales.DTOs;
using Logistics.Server.Application.Modules.Sales.Validators;
using Xunit;

namespace Logistics.Server.UnitTests.Modules.Sales;

public class SalesValidatorTests
{
    [Fact]
    public void CreateOrderRequestValidator_ValidRequest_Passes()
    {
        var validator = new CreateOrderRequestValidator();
        var request = new CreateOrderRequest
        {
            CustomerId = 1,
            BranchId = 1,
            CodAmount = 150000m,
            Notes = "Urgent customer delivery",
            RequestedPickupDate = DateTime.UtcNow.AddDays(1),
            RequiredDeliveryDate = DateTime.UtcNow.AddDays(3),
            PickupAddress = new OrderAddressRequest
            {
                ContactName = "Nguyen Van A",
                PhoneNumber = "0901234567",
                AddressLine1 = "123 Le Loi, District 1"
            },
            DeliveryAddress = new OrderAddressRequest
            {
                ContactName = "Tran Thi B",
                PhoneNumber = "0912345678",
                AddressLine1 = "456 Tran Hung Dao, District 5"
            }
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateOrderRequestValidator_InvalidCustomerId_Fails()
    {
        var validator = new CreateOrderRequestValidator();
        var request = new CreateOrderRequest
        {
            CustomerId = 0,
            CodAmount = 0
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CustomerId");
    }

    [Fact]
    public void CreateOrderRequestValidator_NegativeCodAmount_Fails()
    {
        var validator = new CreateOrderRequestValidator();
        var request = new CreateOrderRequest
        {
            CustomerId = 1,
            CodAmount = -100
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CodAmount");
    }

    [Fact]
    public void CreateOrderRequestValidator_DeliveryBeforePickup_Fails()
    {
        var validator = new CreateOrderRequestValidator();
        var request = new CreateOrderRequest
        {
            CustomerId = 1,
            RequestedPickupDate = DateTime.UtcNow.AddDays(3),
            RequiredDeliveryDate = DateTime.UtcNow.AddDays(1)
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CancelOrderRequestValidator_EmptyReason_Fails()
    {
        var validator = new CancelOrderRequestValidator();
        var request = new CancelOrderRequest { Reason = "" };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Reason");
    }

    [Fact]
    public void CancelOrderRequestValidator_ValidReason_Passes()
    {
        var validator = new CancelOrderRequestValidator();
        var request = new CancelOrderRequest { Reason = "Customer requested cancellation." };

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateOrderItemRequestValidator_MissingProduct_Fails()
    {
        var validator = new CreateOrderItemRequestValidator();
        var request = new CreateOrderItemRequest
        {
            ProductId = null,
            ProductName = null,
            Quantity = 2,
            UnitPrice = 50000
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateOrderItemRequestValidator_ValidItem_Passes()
    {
        var validator = new CreateOrderItemRequestValidator();
        var request = new CreateOrderItemRequest
        {
            ProductName = "Standard Parcel Box",
            Quantity = 2,
            UnitPrice = 50000,
            WeightKg = 1.5m
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateOrderPackageRequestValidator_ZeroWeight_Fails()
    {
        var validator = new CreateOrderPackageRequestValidator();
        var request = new CreateOrderPackageRequest
        {
            WeightKg = 0,
            LengthCm = 10,
            WidthCm = 10,
            HeightCm = 10
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "WeightKg");
    }

    [Fact]
    public void CreateOrderPackageRequestValidator_ValidPackage_Passes()
    {
        var validator = new CreateOrderPackageRequestValidator();
        var request = new CreateOrderPackageRequest
        {
            WeightKg = 2.5m,
            LengthCm = 30,
            WidthCm = 20,
            HeightCm = 15,
            DeclaredValue = 500000m
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void OrderAddressRequestValidator_MissingFields_Fails()
    {
        var validator = new OrderAddressRequestValidator();
        var request = new OrderAddressRequest
        {
            ContactName = "",
            PhoneNumber = "",
            AddressLine1 = ""
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ContactName");
        result.Errors.Should().Contain(e => e.PropertyName == "PhoneNumber");
        result.Errors.Should().Contain(e => e.PropertyName == "AddressLine1");
    }

    [Fact]
    public void OrderAddressRequestValidator_InvalidEmail_Fails()
    {
        var validator = new OrderAddressRequestValidator();
        var request = new OrderAddressRequest
        {
            ContactName = "John Doe",
            PhoneNumber = "0900000000",
            AddressLine1 = "123 Street",
            Email = "invalid-email-address"
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }
}
