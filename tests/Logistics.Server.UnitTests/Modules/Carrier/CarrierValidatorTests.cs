using FluentAssertions;
using Logistics.Server.Application.Modules.Carrier.DTOs;
using Logistics.Server.Application.Modules.Carrier.Validators;
using Xunit;

namespace Logistics.Server.UnitTests.Modules.Carrier;

public class CarrierValidatorTests
{
    [Fact]
    public void CreateCarrierRequestValidator_ValidRequest_Passes()
    {
        var validator = new CreateCarrierRequestValidator();
        var request = new CreateCarrierRequest
        {
            Code = "GHN",
            Name = "Giao Hang Nhanh",
            ApiBaseUrl = "https://api.ghn.vn/v2",
            ContactEmail = "support@ghn.vn",
            ContactPhone = "19001206",
            WebsiteUrl = "https://ghn.vn"
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ghn-express")]
    [InlineData("GHN EXPRESS")]
    public void CreateCarrierRequestValidator_InvalidCode_Fails(string code)
    {
        var validator = new CreateCarrierRequestValidator();
        var request = new CreateCarrierRequest
        {
            Code = code,
            Name = "Giao Hang Nhanh",
            ApiBaseUrl = "https://api.ghn.vn"
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    [Fact]
    public void CreateCarrierRequestValidator_MissingNameOrApiBaseUrl_Fails()
    {
        var validator = new CreateCarrierRequestValidator();
        var request = new CreateCarrierRequest
        {
            Code = "GHN",
            Name = "",
            ApiBaseUrl = ""
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
        result.Errors.Should().Contain(e => e.PropertyName == "ApiBaseUrl");
    }

    [Fact]
    public void CreateCarrierRequestValidator_InvalidEmail_Fails()
    {
        var validator = new CreateCarrierRequestValidator();
        var request = new CreateCarrierRequest
        {
            Code = "GHN",
            Name = "Giao Hang Nhanh",
            ApiBaseUrl = "https://api.ghn.vn",
            ContactEmail = "invalid-email"
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ContactEmail");
    }

    [Fact]
    public void UpdateCarrierRequestValidator_ValidRequest_Passes()
    {
        var validator = new UpdateCarrierRequestValidator();
        var request = new UpdateCarrierRequest
        {
            Name = "Giao Hang Nhanh Express",
            ApiBaseUrl = "https://api.ghn.vn/v3"
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateCarrierServiceRequestValidator_ValidRequest_Passes()
    {
        var validator = new CreateCarrierServiceRequestValidator();
        var request = new CreateCarrierServiceRequest
        {
            ServiceCode = "STANDARD",
            ServiceName = "Standard Delivery",
            EstimatedTransitHours = 48,
            MaxWeightKg = 30m,
            MaxLengthCm = 150m,
            IsCodSupported = true,
            IsInsuranceSupported = true
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateCarrierServiceRequestValidator_InvalidServiceCode_Fails()
    {
        var validator = new CreateCarrierServiceRequestValidator();
        var request = new CreateCarrierServiceRequest
        {
            ServiceCode = "standard delivery",
            ServiceName = "Standard Delivery"
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ServiceCode");
    }

    [Fact]
    public void CreateCarrierServiceRequestValidator_NegativeDimensions_Fails()
    {
        var validator = new CreateCarrierServiceRequestValidator();
        var request = new CreateCarrierServiceRequest
        {
            ServiceCode = "STANDARD",
            ServiceName = "Standard",
            MaxWeightKg = -5m,
            MaxLengthCm = 0m,
            EstimatedTransitHours = -1
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MaxWeightKg");
        result.Errors.Should().Contain(e => e.PropertyName == "MaxLengthCm");
        result.Errors.Should().Contain(e => e.PropertyName == "EstimatedTransitHours");
    }

    [Theory]
    [InlineData("Sandbox")]
    [InlineData("Production")]
    [InlineData("production")]
    public void UpsertCarrierConfigurationRequestValidator_ValidEnvironment_Passes(string env)
    {
        var validator = new UpsertCarrierConfigurationRequestValidator();
        var request = new UpsertCarrierConfigurationRequest
        {
            Environment = env,
            ApiKey = "secret-api-key",
            ClientId = "client-123"
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Staging")]
    [InlineData("QA")]
    [InlineData("")]
    public void UpsertCarrierConfigurationRequestValidator_InvalidEnvironment_Fails(string env)
    {
        var validator = new UpsertCarrierConfigurationRequestValidator();
        var request = new UpsertCarrierConfigurationRequest
        {
            Environment = env
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Environment");
    }

    [Fact]
    public void CreateTrackingMappingRequestValidator_ValidRequest_Passes()
    {
        var validator = new CreateTrackingMappingRequestValidator();
        var request = new CreateTrackingMappingRequest
        {
            ExternalStatusCode = "DELIVERED",
            ExternalStatusName = "Delivered successfully",
            InternalShipmentStatus = "Delivered"
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateTrackingMappingRequestValidator_InvalidInternalStatus_Fails()
    {
        var validator = new CreateTrackingMappingRequestValidator();
        var request = new CreateTrackingMappingRequest
        {
            ExternalStatusCode = "CUSTOM_STATUS",
            InternalShipmentStatus = "NonExistentStatus"
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "InternalShipmentStatus");
    }

    [Fact]
    public void CarrierBookShipmentRequestValidator_ValidRequest_Passes()
    {
        var validator = new CarrierBookShipmentRequestValidator();
        var request = new CarrierBookShipmentRequest
        {
            ShipmentIdentifier = "SHIP-2026-0001",
            CarrierServiceCode = "STANDARD"
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CarrierBookShipmentRequestValidator_EmptyIdentifier_Fails()
    {
        var validator = new CarrierBookShipmentRequestValidator();
        var request = new CarrierBookShipmentRequest
        {
            ShipmentIdentifier = ""
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ShipmentIdentifier");
    }

    [Fact]
    public void CarrierRateQueryRequestValidator_ValidRequest_Passes()
    {
        var validator = new CarrierRateQueryRequestValidator();
        var request = new CarrierRateQueryRequest
        {
            CarrierId = 1,
            WeightKg = 2.5m,
            LengthCm = 20m,
            WidthCm = 15m,
            HeightCm = 10m,
            DeclaredValue = 500000m,
            CodAmount = 0m
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CarrierRateQueryRequestValidator_ZeroCarrierIdOrNegativeWeight_Fails()
    {
        var validator = new CarrierRateQueryRequestValidator();
        var request = new CarrierRateQueryRequest
        {
            CarrierId = 0,
            WeightKg = -1m
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CarrierId");
        result.Errors.Should().Contain(e => e.PropertyName == "WeightKg");
    }
}
