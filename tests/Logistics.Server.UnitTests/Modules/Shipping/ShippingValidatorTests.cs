using FluentAssertions;
using Logistics.Server.Application.Modules.Shipping.DTOs;
using Logistics.Server.Application.Modules.Shipping.Validators;
using Xunit;

namespace Logistics.Server.UnitTests.Modules.Shipping;

public class ShippingValidatorTests
{
    [Fact]
    public void CreateShipmentFromOrderRequestValidator_ValidRequest_Passes()
    {
        var validator = new CreateShipmentFromOrderRequestValidator();
        var request = new CreateShipmentFromOrderRequest
        {
            OrderId = 1,
            OriginBranchId = 1,
            DestinationBranchId = 2,
            ServiceType = "StandardGround",
            CodAmount = 100000m,
            DeclaredValue = 200000m
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateShipmentFromOrderRequestValidator_MissingDestinationBranch_Fails()
    {
        var validator = new CreateShipmentFromOrderRequestValidator();
        var request = new CreateShipmentFromOrderRequest
        {
            OrderId = 1,
            DestinationBranchId = 0,
            ServiceType = "StandardGround"
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DestinationBranchId");
    }

    [Fact]
    public void CreateShipmentFromOrderRequestValidator_InvalidServiceType_Fails()
    {
        var validator = new CreateShipmentFromOrderRequestValidator();
        var request = new CreateShipmentFromOrderRequest
        {
            OrderId = 1,
            DestinationBranchId = 2,
            ServiceType = "Teleportation"
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ServiceType");
    }

    [Fact]
    public void CancelShipmentRequestValidator_EmptyReason_Fails()
    {
        var validator = new CancelShipmentRequestValidator();
        var request = new CancelShipmentRequest { Reason = "" };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Reason");
    }

    [Fact]
    public void CancelShipmentRequestValidator_ValidReason_Passes()
    {
        var validator = new CancelShipmentRequestValidator();
        var request = new CancelShipmentRequest { Reason = "Customer cancelled order before dispatch." };

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ShipmentStatusTransitionRequestValidator_ValidStatus_Passes()
    {
        var validator = new ShipmentStatusTransitionRequestValidator();
        var request = new ShipmentStatusTransitionRequest { TargetStatus = "PickedUp" };

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ShipmentStatusTransitionRequestValidator_InvalidStatus_Fails()
    {
        var validator = new ShipmentStatusTransitionRequestValidator();
        var request = new ShipmentStatusTransitionRequest { TargetStatus = "Teleported" };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TargetStatus");
    }

    [Fact]
    public void AddShipmentPackageRequestValidator_ZeroWeight_Fails()
    {
        var validator = new AddShipmentPackageRequestValidator();
        var request = new AddShipmentPackageRequest
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
    public void AddShipmentPackageRequestValidator_ValidPackage_Passes()
    {
        var validator = new AddShipmentPackageRequestValidator();
        var request = new AddShipmentPackageRequest
        {
            WeightKg = 3.5m,
            LengthCm = 30,
            WidthCm = 20,
            HeightCm = 15
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdateShipmentAddressRequestValidator_MissingContactName_Fails()
    {
        var validator = new UpdateShipmentAddressRequestValidator();
        var request = new UpdateShipmentAddressRequest
        {
            ContactName = "",
            PhoneNumber = "0901234567",
            AddressLine1 = "123 Le Loi",
            CountryId = 1
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ContactName");
    }

    [Fact]
    public void CreatePickupRequestRequestValidator_ValidRequest_Passes()
    {
        var validator = new CreatePickupRequestRequestValidator();
        var request = new CreatePickupRequestRequest
        {
            CustomerId = 1,
            RequestedAddressId = 1,
            ContactName = "Nguyen Van A",
            ContactPhone = "0901112222",
            ScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            TimeSlot = "08:00 - 12:00",
            EstimatedParcels = 5
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RecordDeliveryAttemptRequestValidator_FailedWithoutReason_Fails()
    {
        var validator = new RecordDeliveryAttemptRequestValidator();
        var request = new RecordDeliveryAttemptRequest
        {
            AttemptedByEmployeeId = 1,
            AttemptedAt = DateTime.UtcNow,
            Status = "Failed",
            FailureReasonCode = null
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FailureReasonCode");
    }

    [Fact]
    public void RecordDeliveryAttemptRequestValidator_FailedWithValidReason_Passes()
    {
        var validator = new RecordDeliveryAttemptRequestValidator();
        var request = new RecordDeliveryAttemptRequest
        {
            AttemptedByEmployeeId = 1,
            AttemptedAt = DateTime.UtcNow,
            Status = "Failed",
            FailureReasonCode = "RecipientUnavailable"
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateProofOfDeliveryRequestValidator_InvalidRelation_Fails()
    {
        var validator = new CreateProofOfDeliveryRequestValidator();
        var request = new CreateProofOfDeliveryRequest
        {
            RecipientName = "Le Van C",
            RecipientRelation = "Neighbor",
            DeliveredAt = DateTime.UtcNow
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RecipientRelation");
    }

    [Fact]
    public void CreateProofOfDeliveryRequestValidator_ValidRelation_Passes()
    {
        var validator = new CreateProofOfDeliveryRequestValidator();
        var request = new CreateProofOfDeliveryRequest
        {
            RecipientName = "Le Van C",
            RecipientRelation = "Self",
            DeliveredAt = DateTime.UtcNow
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateReturnRequestRequestValidator_ShortDescription_Fails()
    {
        var validator = new CreateReturnRequestRequestValidator();
        var request = new CreateReturnRequestRequest
        {
            OriginalShipmentId = 1,
            ReasonCode = "DamagedItem",
            ReasonDescription = "Broken" // < 10 chars
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ReasonDescription");
    }

    [Fact]
    public void AddShipmentRouteRequestValidator_SameOriginAndDestination_Fails()
    {
        var validator = new AddShipmentRouteRequestValidator();
        var request = new AddShipmentRouteRequest
        {
            SequenceOrder = 1,
            FromWarehouseId = 5,
            ToWarehouseId = 5,
            TransportType = "Road"
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ToWarehouseId");
    }
}
