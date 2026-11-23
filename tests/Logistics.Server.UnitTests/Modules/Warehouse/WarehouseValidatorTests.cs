using FluentAssertions;
using Logistics.Server.Application.Modules.Warehouse.DTOs;
using Logistics.Server.Application.Modules.Warehouse.Validators;
using Xunit;

namespace Logistics.Server.UnitTests.Modules.Warehouse;

public class WarehouseValidatorTests
{
    [Fact]
    public void CreateWarehouseRequestValidator_ValidRequest_Passes()
    {
        var validator = new CreateWarehouseRequestValidator();
        var request = new CreateWarehouseRequest
        {
            BranchId = 1,
            Code = "WH_SGN_01",
            Name = "Saigon Main Storage Facility",
            Type = "InventoryWarehouse",
            TotalAreaSqM = 5000m
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("wh sgn 01")]
    [InlineData("WH@SGN")]
    public void CreateWarehouseRequestValidator_InvalidCode_Fails(string code)
    {
        var validator = new CreateWarehouseRequestValidator();
        var request = new CreateWarehouseRequest
        {
            BranchId = 1,
            Code = code,
            Name = "Saigon Warehouse",
            Type = "InventoryWarehouse"
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    [Theory]
    [InlineData("InvalidType")]
    [InlineData("StorageOnly")]
    public void CreateWarehouseRequestValidator_InvalidType_Fails(string type)
    {
        var validator = new CreateWarehouseRequestValidator();
        var request = new CreateWarehouseRequest
        {
            BranchId = 1,
            Code = "WH_SGN_01",
            Name = "Saigon Warehouse",
            Type = type
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Type");
    }

    [Fact]
    public void CreateWarehouseZoneRequestValidator_ValidRequest_Passes()
    {
        var validator = new CreateWarehouseZoneRequestValidator();
        var request = new CreateWarehouseZoneRequest
        {
            Code = "ZONE_BULK_A",
            Name = "Bulk Storage Zone A",
            ZoneType = "BulkStorage",
            TemperatureType = "Ambient"
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateWarehouseZoneRequestValidator_InvalidZoneType_Fails()
    {
        var validator = new CreateWarehouseZoneRequestValidator();
        var request = new CreateWarehouseZoneRequest
        {
            Code = "ZONE_A",
            Name = "Zone A",
            ZoneType = "NonExistentZoneType",
            TemperatureType = "Ambient"
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ZoneType");
    }

    [Fact]
    public void CreateWarehouseLocationRequestValidator_ValidRequest_Passes()
    {
        var validator = new CreateWarehouseLocationRequestValidator();
        var request = new CreateWarehouseLocationRequest
        {
            Code = "LOC-A-01-01",
            Barcode = "LOC-A-01-01-BC",
            Aisle = "A",
            Rack = "01",
            Shelf = "01",
            Bin = "01",
            MaxWeightKg = 1500m,
            MaxVolumeCbm = 6m
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateWarehouseLocationRequestValidator_NegativeWeightOrVolume_Fails()
    {
        var validator = new CreateWarehouseLocationRequestValidator();
        var request = new CreateWarehouseLocationRequest
        {
            Code = "LOC-01",
            Barcode = "BARCODE",
            MaxWeightKg = -10m,
            MaxVolumeCbm = 0m
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MaxWeightKg");
        result.Errors.Should().Contain(e => e.PropertyName == "MaxVolumeCbm");
    }

    [Fact]
    public void CreateInventoryItemRequestValidator_ValidRequest_Passes()
    {
        var validator = new CreateInventoryItemRequestValidator();
        var request = new CreateInventoryItemRequest
        {
            ProductId = 1,
            WarehouseId = 1,
            ReorderPoint = 20,
            SafetyStock = 10,
            TargetStock = 100,
            MinimumOrderQuantity = 5,
            MaximumStock = 500
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateInventoryItemRequestValidator_MaxStockLessThanTargetStock_Fails()
    {
        var validator = new CreateInventoryItemRequestValidator();
        var request = new CreateInventoryItemRequest
        {
            ProductId = 1,
            WarehouseId = 1,
            TargetStock = 200,
            MaximumStock = 150
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MaximumStock");
    }

    [Fact]
    public void CreateWarehouseReceiptRequestValidator_EmptyItems_Fails()
    {
        var validator = new CreateWarehouseReceiptRequestValidator();
        var request = new CreateWarehouseReceiptRequest
        {
            WarehouseId = 1,
            ReceiptType = "InventoryInbound",
            ReceivedByEmployeeId = 1,
            Items = []
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Items");
    }

    [Fact]
    public void CreateStockAdjustmentRequestValidator_ValidRequest_Passes()
    {
        var validator = new CreateStockAdjustmentRequestValidator();
        var request = new CreateStockAdjustmentRequest
        {
            WarehouseId = 1,
            ProductId = 1,
            LocationId = 1,
            ReasonCode = "CountingVariance",
            ReasonDescription = "Annual inventory cycle count variance",
            QuantityChange = -2
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateStockAdjustmentRequestValidator_ZeroQuantityChange_Fails()
    {
        var validator = new CreateStockAdjustmentRequestValidator();
        var request = new CreateStockAdjustmentRequest
        {
            WarehouseId = 1,
            ProductId = 1,
            LocationId = 1,
            ReasonCode = "Damage",
            ReasonDescription = "Damaged during handling",
            QuantityChange = 0
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "QuantityChange");
    }

    [Fact]
    public void CreateStockTransferRequestValidator_SameSourceAndDestination_Fails()
    {
        var validator = new CreateStockTransferRequestValidator();
        var request = new CreateStockTransferRequest
        {
            SourceWarehouseId = 1,
            DestinationWarehouseId = 1,
            RequestedByEmployeeId = 1,
            ProductId = 1,
            SourceLocationId = 1,
            Quantity = 10
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateInventoryReservationRequestValidator_ValidRequest_Passes()
    {
        var validator = new CreateInventoryReservationRequestValidator();
        var request = new CreateInventoryReservationRequest
        {
            ProductId = 1,
            WarehouseId = 1,
            LocationId = 1,
            OrderId = 1,
            ReservedQuantity = 5,
            DurationMinutes = 1440
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ScanPackageRequestValidator_EmptyBarcode_Fails()
    {
        var validator = new ScanPackageRequestValidator();
        var request = new ScanPackageRequest
        {
            TrackingNumberOrBarcode = "",
            WarehouseId = 1,
            EmployeeId = 1
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TrackingNumberOrBarcode");
    }

    [Theory]
    [InlineData("Damaged")]
    [InlineData("Missing")]
    [InlineData("IncorrectDestination")]
    [InlineData("OperationalHold")]
    public void ParcelExceptionRequestValidator_ValidExceptionType_Passes(string exceptionType)
    {
        var validator = new ParcelExceptionRequestValidator();
        var request = new ParcelExceptionRequest
        {
            TrackingNumberOrBarcode = "PKG-2026-001",
            WarehouseId = 1,
            EmployeeId = 1,
            ExceptionType = exceptionType,
            Reason = "Box was punctured during transport"
        };

        var result = validator.Validate(request);
        result.IsValid.Should().BeTrue();
    }
}
