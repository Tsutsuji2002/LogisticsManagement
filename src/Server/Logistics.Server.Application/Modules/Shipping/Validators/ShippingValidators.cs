using FluentValidation;
using Logistics.Server.Application.Modules.Shipping.DTOs;

namespace Logistics.Server.Application.Modules.Shipping.Validators;

public class CreateShipmentFromOrderRequestValidator : AbstractValidator<CreateShipmentFromOrderRequest>
{
    private static readonly string[] ValidServiceTypes = ["SameDay", "NextDayExpress", "StandardGround", "Economical"];

    public CreateShipmentFromOrderRequestValidator()
    {
        RuleFor(x => x.OrderId)
            .GreaterThan(0).WithMessage("OrderId must be a valid positive identifier.");

        RuleFor(x => x.OriginBranchId)
            .GreaterThan(0).WithMessage("OriginBranchId must be a valid positive identifier.");

        RuleFor(x => x.DestinationBranchId)
            .GreaterThan(0).WithMessage("DestinationBranchId must be a valid positive identifier.")
            .NotEqual(x => x.OriginBranchId).WithMessage("Destination branch must differ from origin branch.");

        RuleFor(x => x.ServiceType)
            .NotEmpty().WithMessage("ServiceType is required.")
            .Must(v => ValidServiceTypes.Contains(v))
            .WithMessage($"ServiceType must be one of: {string.Join(", ", ValidServiceTypes)}.");

        RuleFor(x => x.DeclaredValue)
            .GreaterThanOrEqualTo(0).WithMessage("DeclaredValue cannot be negative.");

        RuleFor(x => x.CodAmount)
            .GreaterThanOrEqualTo(0).WithMessage("CodAmount cannot be negative.");

        When(x => x.PickupDate.HasValue && x.EstimatedDeliveryDate.HasValue, () =>
        {
            RuleFor(x => x.EstimatedDeliveryDate)
                .GreaterThanOrEqualTo(x => x.PickupDate)
                .WithMessage("EstimatedDeliveryDate must be on or after PickupDate.");
        });
    }
}

public class UpdateShipmentRequestValidator : AbstractValidator<UpdateShipmentRequest>
{
    private static readonly string[] ValidServiceTypes = ["SameDay", "NextDayExpress", "StandardGround", "Economical"];

    public UpdateShipmentRequestValidator()
    {
        RuleFor(x => x.ServiceType)
            .NotEmpty().WithMessage("ServiceType is required.")
            .Must(v => ValidServiceTypes.Contains(v))
            .WithMessage($"ServiceType must be one of: {string.Join(", ", ValidServiceTypes)}.");

        RuleFor(x => x.DeclaredValue)
            .GreaterThanOrEqualTo(0).WithMessage("DeclaredValue cannot be negative.");

        RuleFor(x => x.CodAmount)
            .GreaterThanOrEqualTo(0).WithMessage("CodAmount cannot be negative.");

        RuleFor(x => x.ShippingFee)
            .GreaterThanOrEqualTo(0).WithMessage("ShippingFee cannot be negative.");

        RuleFor(x => x.InsuranceFee)
            .GreaterThanOrEqualTo(0).WithMessage("InsuranceFee cannot be negative.");

        RuleFor(x => x.SurchargesTotal)
            .GreaterThanOrEqualTo(0).WithMessage("SurchargesTotal cannot be negative.");
    }
}

public class CancelShipmentRequestValidator : AbstractValidator<CancelShipmentRequest>
{
    public CancelShipmentRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Cancellation reason is required.")
            .MaximumLength(500).WithMessage("Cancellation reason must not exceed 500 characters.");
    }
}

public class ShipmentStatusTransitionRequestValidator : AbstractValidator<ShipmentStatusTransitionRequest>
{
    private static readonly string[] ValidStatuses =
    [
        "Draft", "ReadyForPickup", "PickedUp", "InSortingHub", "InTransit",
        "OutForDelivery", "Delivered", "DeliveryFailed", "Returning", "Returned", "Cancelled"
    ];

    private static readonly string[] ValidSources = ["Internal", "MobileApp", "CarrierWebhook", "BackgroundSync"];

    public ShipmentStatusTransitionRequestValidator()
    {
        RuleFor(x => x.TargetStatus)
            .NotEmpty().WithMessage("TargetStatus is required.")
            .Must(v => ValidStatuses.Contains(v))
            .WithMessage($"TargetStatus must be one of: {string.Join(", ", ValidStatuses)}.");

        RuleFor(x => x.Source)
            .NotEmpty().WithMessage("Source is required.")
            .Must(v => ValidSources.Contains(v))
            .WithMessage($"Source must be one of: {string.Join(", ", ValidSources)}.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes must not exceed 1000 characters.")
            .When(x => x.Notes is not null);
    }
}

public class AddShipmentPackageRequestValidator : AbstractValidator<AddShipmentPackageRequest>
{
    public AddShipmentPackageRequestValidator()
    {
        RuleFor(x => x.WeightKg)
            .GreaterThan(0).WithMessage("WeightKg must be greater than zero.");

        RuleFor(x => x.LengthCm)
            .GreaterThan(0).WithMessage("LengthCm must be greater than zero.");

        RuleFor(x => x.WidthCm)
            .GreaterThan(0).WithMessage("WidthCm must be greater than zero.");

        RuleFor(x => x.HeightCm)
            .GreaterThan(0).WithMessage("HeightCm must be greater than zero.");

        RuleFor(x => x.SealNumber)
            .MaximumLength(100).WithMessage("SealNumber must not exceed 100 characters.")
            .When(x => x.SealNumber is not null);
    }
}

public class UpdateShipmentPackageRequestValidator : AbstractValidator<UpdateShipmentPackageRequest>
{
    private static readonly string[] ValidStatuses =
        ["Created", "Received", "Sorted", "InTransit", "Delivered", "Damaged", "Lost"];

    public UpdateShipmentPackageRequestValidator()
    {
        RuleFor(x => x.WeightKg)
            .GreaterThan(0).WithMessage("WeightKg must be greater than zero.");

        RuleFor(x => x.LengthCm)
            .GreaterThan(0).WithMessage("LengthCm must be greater than zero.");

        RuleFor(x => x.WidthCm)
            .GreaterThan(0).WithMessage("WidthCm must be greater than zero.");

        RuleFor(x => x.HeightCm)
            .GreaterThan(0).WithMessage("HeightCm must be greater than zero.");

        RuleFor(x => x.PackageStatus)
            .NotEmpty().WithMessage("PackageStatus is required.")
            .Must(v => ValidStatuses.Contains(v))
            .WithMessage($"PackageStatus must be one of: {string.Join(", ", ValidStatuses)}.");

        RuleFor(x => x.SealNumber)
            .MaximumLength(100).WithMessage("SealNumber must not exceed 100 characters.")
            .When(x => x.SealNumber is not null);
    }
}

public class UpdateShipmentAddressRequestValidator : AbstractValidator<UpdateShipmentAddressRequest>
{
    public UpdateShipmentAddressRequestValidator()
    {
        RuleFor(x => x.ContactName)
            .NotEmpty().WithMessage("ContactName is required.")
            .MaximumLength(200).WithMessage("ContactName must not exceed 200 characters.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("PhoneNumber is required.")
            .MaximumLength(30).WithMessage("PhoneNumber must not exceed 30 characters.");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Email must be a valid email address.")
            .MaximumLength(254).WithMessage("Email must not exceed 254 characters.")
            .When(x => x.Email is not null);

        RuleFor(x => x.AddressLine1)
            .NotEmpty().WithMessage("AddressLine1 is required.")
            .MaximumLength(300).WithMessage("AddressLine1 must not exceed 300 characters.");

        RuleFor(x => x.AddressLine2)
            .MaximumLength(300).WithMessage("AddressLine2 must not exceed 300 characters.")
            .When(x => x.AddressLine2 is not null);

        RuleFor(x => x.CountryId)
            .GreaterThan(0).WithMessage("CountryId must be a valid positive identifier.");

        RuleFor(x => x.PostalCode)
            .MaximumLength(20).WithMessage("PostalCode must not exceed 20 characters.")
            .When(x => x.PostalCode is not null);
    }
}

public class CreatePickupRequestRequestValidator : AbstractValidator<CreatePickupRequestRequest>
{
    public CreatePickupRequestRequestValidator()
    {
        RuleFor(x => x.CustomerId)
            .GreaterThan(0).WithMessage("CustomerId must be a valid positive identifier.");

        RuleFor(x => x.RequestedAddressId)
            .GreaterThan(0).WithMessage("RequestedAddressId must be a valid positive identifier.");

        RuleFor(x => x.ScheduledDate)
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("ScheduledDate must be today or in the future.");

        RuleFor(x => x.TimeSlot)
            .NotEmpty().WithMessage("TimeSlot is required.")
            .MaximumLength(50).WithMessage("TimeSlot must not exceed 50 characters.");

        RuleFor(x => x.ContactName)
            .NotEmpty().WithMessage("ContactName is required.")
            .MaximumLength(200).WithMessage("ContactName must not exceed 200 characters.");

        RuleFor(x => x.ContactPhone)
            .NotEmpty().WithMessage("ContactPhone is required.")
            .MaximumLength(30).WithMessage("ContactPhone must not exceed 30 characters.");

        RuleFor(x => x.EstimatedParcels)
            .GreaterThan(0).WithMessage("EstimatedParcels must be at least 1.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes must not exceed 1000 characters.")
            .When(x => x.Notes is not null);
    }
}

public class UpdatePickupRequestRequestValidator : AbstractValidator<UpdatePickupRequestRequest>
{
    public UpdatePickupRequestRequestValidator()
    {
        RuleFor(x => x.ScheduledDate)
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("ScheduledDate must be today or in the future.");

        RuleFor(x => x.TimeSlot)
            .NotEmpty().WithMessage("TimeSlot is required.")
            .MaximumLength(50).WithMessage("TimeSlot must not exceed 50 characters.");

        RuleFor(x => x.ContactName)
            .NotEmpty().WithMessage("ContactName is required.")
            .MaximumLength(200).WithMessage("ContactName must not exceed 200 characters.");

        RuleFor(x => x.ContactPhone)
            .NotEmpty().WithMessage("ContactPhone is required.")
            .MaximumLength(30).WithMessage("ContactPhone must not exceed 30 characters.");

        RuleFor(x => x.EstimatedParcels)
            .GreaterThan(0).WithMessage("EstimatedParcels must be at least 1.");

        RuleFor(x => x.AssignedEmployeeId)
            .GreaterThan(0).WithMessage("AssignedEmployeeId must be a valid positive identifier.")
            .When(x => x.AssignedEmployeeId.HasValue);

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes must not exceed 1000 characters.")
            .When(x => x.Notes is not null);
    }
}

public class CompletePickupRequestValidator : AbstractValidator<CompletePickupRequest>
{
    public CompletePickupRequestValidator()
    {
        RuleFor(x => x.ActualParcelsPickedUp)
            .GreaterThanOrEqualTo(0).WithMessage("ActualParcelsPickedUp cannot be negative.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes must not exceed 1000 characters.")
            .When(x => x.Notes is not null);
    }
}

public class CreateShipmentAssignmentRequestValidator : AbstractValidator<CreateShipmentAssignmentRequest>
{
    private static readonly string[] ValidTypes = ["Pickup", "Linehaul", "LastMileDelivery"];

    public CreateShipmentAssignmentRequestValidator()
    {
        RuleFor(x => x.AssignmentType)
            .NotEmpty().WithMessage("AssignmentType is required.")
            .Must(v => ValidTypes.Contains(v))
            .WithMessage($"AssignmentType must be one of: {string.Join(", ", ValidTypes)}.");

        RuleFor(x => x.AssignedToEmployeeId)
            .GreaterThan(0).WithMessage("AssignedToEmployeeId must be a valid positive identifier.");

        RuleFor(x => x.AssignedBranchId)
            .GreaterThan(0).WithMessage("AssignedBranchId must be a valid positive identifier.");

        RuleFor(x => x.AssignedVehicleId)
            .GreaterThan(0).WithMessage("AssignedVehicleId must be a valid positive identifier.")
            .When(x => x.AssignedVehicleId.HasValue);

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes must not exceed 1000 characters.")
            .When(x => x.Notes is not null);
    }
}

public class UpdateShipmentAssignmentRequestValidator : AbstractValidator<UpdateShipmentAssignmentRequest>
{
    private static readonly string[] ValidStatuses = ["Assigned", "Accepted", "InProgress", "Completed", "Failed"];

    public UpdateShipmentAssignmentRequestValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .Must(v => ValidStatuses.Contains(v))
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}.");

        RuleFor(x => x.AssignedVehicleId)
            .GreaterThan(0).WithMessage("AssignedVehicleId must be a valid positive identifier.")
            .When(x => x.AssignedVehicleId.HasValue);

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes must not exceed 1000 characters.")
            .When(x => x.Notes is not null);
    }
}

public class RecordDeliveryAttemptRequestValidator : AbstractValidator<RecordDeliveryAttemptRequest>
{
    private static readonly string[] ValidStatuses = ["Delivered", "Failed", "Rescheduled"];

    public RecordDeliveryAttemptRequestValidator()
    {
        RuleFor(x => x.AttemptedByEmployeeId)
            .GreaterThan(0).WithMessage("AttemptedByEmployeeId must be a valid positive identifier.");

        RuleFor(x => x.AttemptedAt)
            .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5))
            .WithMessage("AttemptedAt cannot be in the future.");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .Must(v => ValidStatuses.Contains(v))
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}.");

        RuleFor(x => x.FailureReasonCode)
            .MaximumLength(100).WithMessage("FailureReasonCode must not exceed 100 characters.")
            .When(x => x.FailureReasonCode is not null);

        RuleFor(x => x.FailureNotes)
            .MaximumLength(1000).WithMessage("FailureNotes must not exceed 1000 characters.")
            .When(x => x.FailureNotes is not null);

        When(x => x.Status == "Failed" || x.Status == "Rescheduled", () =>
        {
            RuleFor(x => x.FailureReasonCode)
                .NotEmpty().WithMessage("FailureReasonCode is required when the attempt status is Failed or Rescheduled.");
        });

        RuleFor(x => x.NextScheduledAttemptDate)
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("NextScheduledAttemptDate must be today or in the future.")
            .When(x => x.NextScheduledAttemptDate.HasValue);
    }
}

public class CreateProofOfDeliveryRequestValidator : AbstractValidator<CreateProofOfDeliveryRequest>
{
    private static readonly string[] ValidRelations =
        ["Self", "FamilyMember", "Receptionist", "SecurityGuard", "Colleague"];

    public CreateProofOfDeliveryRequestValidator()
    {
        RuleFor(x => x.RecipientName)
            .NotEmpty().WithMessage("RecipientName is required.")
            .MaximumLength(200).WithMessage("RecipientName must not exceed 200 characters.");

        RuleFor(x => x.RecipientRelation)
            .NotEmpty().WithMessage("RecipientRelation is required.")
            .Must(v => ValidRelations.Contains(v))
            .WithMessage($"RecipientRelation must be one of: {string.Join(", ", ValidRelations)}.");

        RuleFor(x => x.RecipientNationalId)
            .MaximumLength(50).WithMessage("RecipientNationalId must not exceed 50 characters.")
            .When(x => x.RecipientNationalId is not null);

        RuleFor(x => x.DeliveredAt)
            .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5))
            .WithMessage("DeliveredAt cannot be in the future.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes must not exceed 1000 characters.")
            .When(x => x.Notes is not null);

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90m, 90m).WithMessage("Latitude must be between -90 and 90.")
            .When(x => x.Latitude.HasValue);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180m, 180m).WithMessage("Longitude must be between -180 and 180.")
            .When(x => x.Longitude.HasValue);
    }
}

public class CreateReturnRequestRequestValidator : AbstractValidator<CreateReturnRequestRequest>
{
    public CreateReturnRequestRequestValidator()
    {
        RuleFor(x => x.OriginalShipmentId)
            .GreaterThan(0).WithMessage("OriginalShipmentId must be a valid positive identifier.");

        RuleFor(x => x.ReasonCode)
            .NotEmpty().WithMessage("ReasonCode is required.")
            .MaximumLength(100).WithMessage("ReasonCode must not exceed 100 characters.");

        RuleFor(x => x.ReasonDescription)
            .NotEmpty().WithMessage("ReasonDescription is required.")
            .MinimumLength(10).WithMessage("ReasonDescription must be at least 10 characters.")
            .MaximumLength(1000).WithMessage("ReasonDescription must not exceed 1000 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes must not exceed 1000 characters.")
            .When(x => x.Notes is not null);
    }
}

public class ProcessReturnRequestRequestValidator : AbstractValidator<ProcessReturnRequestRequest>
{
    private static readonly string[] ValidDecisions = ["Approved", "Rejected"];

    public ProcessReturnRequestRequestValidator()
    {
        RuleFor(x => x.Decision)
            .NotEmpty().WithMessage("Decision is required.")
            .Must(v => ValidDecisions.Contains(v))
            .WithMessage($"Decision must be one of: {string.Join(", ", ValidDecisions)}.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes must not exceed 1000 characters.")
            .When(x => x.Notes is not null);
    }
}

public class AddShipmentRouteRequestValidator : AbstractValidator<AddShipmentRouteRequest>
{
    private static readonly string[] ValidTransportTypes = ["Road", "Air", "Rail", "Sea"];

    public AddShipmentRouteRequestValidator()
    {
        RuleFor(x => x.SequenceOrder)
            .GreaterThan(0).WithMessage("SequenceOrder must be a positive integer.");

        RuleFor(x => x.FromWarehouseId)
            .GreaterThan(0).WithMessage("FromWarehouseId must be a valid positive identifier.");

        RuleFor(x => x.ToWarehouseId)
            .GreaterThan(0).WithMessage("ToWarehouseId must be a valid positive identifier.")
            .NotEqual(x => x.FromWarehouseId).WithMessage("ToWarehouseId must differ from FromWarehouseId.");

        RuleFor(x => x.TransportType)
            .NotEmpty().WithMessage("TransportType is required.")
            .Must(v => ValidTransportTypes.Contains(v))
            .WithMessage($"TransportType must be one of: {string.Join(", ", ValidTransportTypes)}.");

        RuleFor(x => x.CarrierId)
            .GreaterThan(0).WithMessage("CarrierId must be a valid positive identifier.")
            .When(x => x.CarrierId.HasValue);

        RuleFor(x => x.VehicleNumber)
            .MaximumLength(50).WithMessage("VehicleNumber must not exceed 50 characters.")
            .When(x => x.VehicleNumber is not null);

        When(x => x.EstimatedDeparture.HasValue && x.EstimatedArrival.HasValue, () =>
        {
            RuleFor(x => x.EstimatedArrival)
                .GreaterThan(x => x.EstimatedDeparture)
                .WithMessage("EstimatedArrival must be after EstimatedDeparture.");
        });
    }
}

public class UpdateShipmentRouteRequestValidator : AbstractValidator<UpdateShipmentRouteRequest>
{
    private static readonly string[] ValidStatuses =
        ["Planned", "Dispatched", "Arrived", "Delayed", "Cancelled"];

    public UpdateShipmentRouteRequestValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .Must(v => ValidStatuses.Contains(v))
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}.");

        RuleFor(x => x.VehicleNumber)
            .MaximumLength(50).WithMessage("VehicleNumber must not exceed 50 characters.")
            .When(x => x.VehicleNumber is not null);

        When(x => x.EstimatedDeparture.HasValue && x.EstimatedArrival.HasValue, () =>
        {
            RuleFor(x => x.EstimatedArrival)
                .GreaterThan(x => x.EstimatedDeparture)
                .WithMessage("EstimatedArrival must be after EstimatedDeparture.");
        });

        When(x => x.ActualDeparture.HasValue && x.ActualArrival.HasValue, () =>
        {
            RuleFor(x => x.ActualArrival)
                .GreaterThan(x => x.ActualDeparture)
                .WithMessage("ActualArrival must be after ActualDeparture.");
        });
    }
}
