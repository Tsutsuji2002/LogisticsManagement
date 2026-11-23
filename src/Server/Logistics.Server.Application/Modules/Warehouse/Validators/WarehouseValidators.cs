using FluentValidation;
using Logistics.Server.Application.Modules.Warehouse.DTOs;

namespace Logistics.Server.Application.Modules.Warehouse.Validators;

// ───────────── Warehouse Validators ─────────────

public class CreateWarehouseRequestValidator : AbstractValidator<CreateWarehouseRequest>
{
    private static readonly HashSet<string> ValidTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "InventoryWarehouse", "ParcelHub", "Hybrid"
    };

    public CreateWarehouseRequestValidator()
    {
        RuleFor(x => x.BranchId).GreaterThan(0).WithMessage("Branch is required.");
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50)
            .Matches("^[A-Z0-9_-]+$").WithMessage("Warehouse code must be uppercase alphanumeric with dashes/underscores.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Type).NotEmpty().Must(t => ValidTypes.Contains(t))
            .WithMessage("Warehouse type must be 'InventoryWarehouse', 'ParcelHub', or 'Hybrid'.");
        RuleFor(x => x.TotalAreaSqM).GreaterThanOrEqualTo(0);
    }
}

public class UpdateWarehouseRequestValidator : AbstractValidator<UpdateWarehouseRequest>
{
    private static readonly HashSet<string> ValidTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "InventoryWarehouse", "ParcelHub", "Hybrid"
    };

    public UpdateWarehouseRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Type).NotEmpty().Must(t => ValidTypes.Contains(t))
            .WithMessage("Warehouse type must be 'InventoryWarehouse', 'ParcelHub', or 'Hybrid'.");
        RuleFor(x => x.TotalAreaSqM).GreaterThanOrEqualTo(0);
    }
}

public class CreateWarehouseZoneRequestValidator : AbstractValidator<CreateWarehouseZoneRequest>
{
    private static readonly HashSet<string> ValidZoneTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Receiving", "BulkStorage", "Picking", "Staging", "ParcelSorting", "CrossDock", "Return", "Packing"
    };

    private static readonly HashSet<string> ValidTemperatureTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Ambient", "Chilled", "Frozen", "AirConditioned"
    };

    public CreateWarehouseZoneRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50)
            .Matches("^[A-Z0-9_-]+$").WithMessage("Zone code must be alphanumeric with dashes/underscores.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.ZoneType).NotEmpty().Must(t => ValidZoneTypes.Contains(t))
            .WithMessage("Invalid zone type.");
        RuleFor(x => x.TemperatureType).NotEmpty().Must(t => ValidTemperatureTypes.Contains(t))
            .WithMessage("Invalid temperature type.");
    }
}

public class UpdateWarehouseZoneRequestValidator : AbstractValidator<UpdateWarehouseZoneRequest>
{
    private static readonly HashSet<string> ValidZoneTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Receiving", "BulkStorage", "Picking", "Staging", "ParcelSorting", "CrossDock", "Return", "Packing"
    };

    private static readonly HashSet<string> ValidTemperatureTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Ambient", "Chilled", "Frozen", "AirConditioned"
    };

    public UpdateWarehouseZoneRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.ZoneType).NotEmpty().Must(t => ValidZoneTypes.Contains(t))
            .WithMessage("Invalid zone type.");
        RuleFor(x => x.TemperatureType).NotEmpty().Must(t => ValidTemperatureTypes.Contains(t))
            .WithMessage("Invalid temperature type.");
    }
}

public class CreateWarehouseLocationRequestValidator : AbstractValidator<CreateWarehouseLocationRequest>
{
    public CreateWarehouseLocationRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Barcode).NotEmpty().MaximumLength(100);
        RuleFor(x => x.MaxWeightKg).GreaterThan(0);
        RuleFor(x => x.MaxVolumeCbm).GreaterThan(0);
        RuleFor(x => x.Aisle).MaximumLength(20);
        RuleFor(x => x.Rack).MaximumLength(20);
        RuleFor(x => x.Shelf).MaximumLength(20);
        RuleFor(x => x.Bin).MaximumLength(20);
    }
}

// ───────────── Inventory Validators ─────────────

public class CreateInventoryItemRequestValidator : AbstractValidator<CreateInventoryItemRequest>
{
    public CreateInventoryItemRequestValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.WarehouseId).GreaterThan(0);
        RuleFor(x => x.ReorderPoint).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SafetyStock).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TargetStock).GreaterThan(0);
        RuleFor(x => x.MinimumOrderQuantity).GreaterThan(0);
        RuleFor(x => x.MaximumStock).GreaterThan(x => x.TargetStock);
    }
}

public class CreateWarehouseReceiptRequestValidator : AbstractValidator<CreateWarehouseReceiptRequest>
{
    private static readonly HashSet<string> ValidReceiptTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "InventoryInbound", "CustomerReturn", "TransferIn"
    };

    public CreateWarehouseReceiptRequestValidator()
    {
        RuleFor(x => x.WarehouseId).GreaterThan(0);
        RuleFor(x => x.ReceiptType).NotEmpty().Must(t => ValidReceiptTypes.Contains(t))
            .WithMessage("Invalid receipt type.");
        RuleFor(x => x.ReceivedByEmployeeId).GreaterThan(0);
        RuleFor(x => x.Items).NotEmpty().WithMessage("Receipt must contain at least one item.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).GreaterThan(0);
            item.RuleFor(i => i.ExpectedQuantity).GreaterThan(0);
            item.RuleFor(i => i.UnitOfMeasureId).GreaterThan(0);
        });
    }
}

public class CreateStockAdjustmentRequestValidator : AbstractValidator<CreateStockAdjustmentRequest>
{
    private static readonly HashSet<string> ValidReasons = new(StringComparer.OrdinalIgnoreCase)
    {
        "Damage", "Shrinkage", "CountingVariance", "Expired", "FoundGoods"
    };

    public CreateStockAdjustmentRequestValidator()
    {
        RuleFor(x => x.WarehouseId).GreaterThan(0);
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.LocationId).GreaterThan(0);
        RuleFor(x => x.ReasonCode).NotEmpty().Must(r => ValidReasons.Contains(r))
            .WithMessage("Reason code must be 'Damage', 'Shrinkage', 'CountingVariance', 'Expired', or 'FoundGoods'.");
        RuleFor(x => x.ReasonDescription).NotEmpty().MaximumLength(250);
        RuleFor(x => x.QuantityChange).NotEqual(0).WithMessage("Quantity change cannot be zero.");
    }
}

public class CreateStockTransferRequestValidator : AbstractValidator<CreateStockTransferRequest>
{
    public CreateStockTransferRequestValidator()
    {
        RuleFor(x => x.SourceWarehouseId).GreaterThan(0);
        RuleFor(x => x.DestinationWarehouseId).GreaterThan(0);
        RuleFor(x => x.RequestedByEmployeeId).GreaterThan(0);
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.SourceLocationId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Transfer quantity must be positive.");
        RuleFor(x => x).Must(x => x.SourceWarehouseId != x.DestinationWarehouseId)
            .WithMessage("Source and destination warehouses cannot be the same.");
    }
}

public class CreateInventoryReservationRequestValidator : AbstractValidator<CreateInventoryReservationRequest>
{
    public CreateInventoryReservationRequestValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.WarehouseId).GreaterThan(0);
        RuleFor(x => x.LocationId).GreaterThan(0);
        RuleFor(x => x.OrderId).GreaterThan(0);
        RuleFor(x => x.ReservedQuantity).GreaterThan(0);
        RuleFor(x => x.DurationMinutes).GreaterThan(0).LessThanOrEqualTo(43200); // Max 30 days
    }
}

public class CreateInventoryLotRequestValidator : AbstractValidator<CreateInventoryLotRequest>
{
    public CreateInventoryLotRequestValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.LotNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.VendorBatchCode).MaximumLength(50);
    }
}

public class CreateInventorySerialNumberRequestValidator : AbstractValidator<CreateInventorySerialNumberRequest>
{
    public CreateInventorySerialNumberRequestValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.SerialNumber).NotEmpty().MaximumLength(100);
        RuleFor(x => x.WarehouseId).GreaterThan(0);
    }
}

// ───────────── Parcel Handling Validators ─────────────

public class ScanPackageRequestValidator : AbstractValidator<ScanPackageRequest>
{
    public ScanPackageRequestValidator()
    {
        RuleFor(x => x.TrackingNumberOrBarcode).NotEmpty().MaximumLength(100);
        RuleFor(x => x.WarehouseId).GreaterThan(0);
        RuleFor(x => x.EmployeeId).GreaterThan(0);
    }
}

public class ParcelReceiveRequestValidator : AbstractValidator<ParcelReceiveRequest>
{
    public ParcelReceiveRequestValidator()
    {
        RuleFor(x => x.TrackingNumberOrBarcode).NotEmpty().MaximumLength(100);
        RuleFor(x => x.WarehouseId).GreaterThan(0);
        RuleFor(x => x.EmployeeId).GreaterThan(0);
    }
}

public class ParcelSortRequestValidator : AbstractValidator<ParcelSortRequest>
{
    public ParcelSortRequestValidator()
    {
        RuleFor(x => x.TrackingNumberOrBarcode).NotEmpty().MaximumLength(100);
        RuleFor(x => x.WarehouseId).GreaterThan(0);
        RuleFor(x => x.ZoneId).GreaterThan(0);
        RuleFor(x => x.EmployeeId).GreaterThan(0);
    }
}

public class ParcelStageRequestValidator : AbstractValidator<ParcelStageRequest>
{
    public ParcelStageRequestValidator()
    {
        RuleFor(x => x.TrackingNumberOrBarcode).NotEmpty().MaximumLength(100);
        RuleFor(x => x.WarehouseId).GreaterThan(0);
        RuleFor(x => x.LocationId).GreaterThan(0);
        RuleFor(x => x.EmployeeId).GreaterThan(0);
    }
}

public class ParcelExceptionRequestValidator : AbstractValidator<ParcelExceptionRequest>
{
    private static readonly HashSet<string> ValidExceptionTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Damaged", "Missing", "IncorrectDestination", "OperationalHold"
    };

    public ParcelExceptionRequestValidator()
    {
        RuleFor(x => x.TrackingNumberOrBarcode).NotEmpty().MaximumLength(100);
        RuleFor(x => x.WarehouseId).GreaterThan(0);
        RuleFor(x => x.EmployeeId).GreaterThan(0);
        RuleFor(x => x.ExceptionType).NotEmpty().Must(e => ValidExceptionTypes.Contains(e))
            .WithMessage("Exception type must be 'Damaged', 'Missing', 'IncorrectDestination', or 'OperationalHold'.");
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public class CreateWarehouseDispatchDtoValidator : AbstractValidator<CreateWarehouseDispatchDto>
{
    private static readonly HashSet<string> ValidDispatchTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "InventoryOutbound", "HubTransfer", "CourierDispatch", "ReturnOutbound"
    };

    public CreateWarehouseDispatchDtoValidator()
    {
        RuleFor(x => x.WarehouseId).GreaterThan(0);
        RuleFor(x => x.DispatchType).NotEmpty().Must(t => ValidDispatchTypes.Contains(t))
            .WithMessage("Invalid dispatch type.");
        RuleFor(x => x.DispatchedByEmployeeId).GreaterThan(0);
        RuleFor(x => x).Must(x => (x.Items != null && x.Items.Count > 0) || (x.Packages != null && x.Packages.Count > 0))
            .WithMessage("Dispatch must have at least one inventory item or parcel package.");
    }
}
