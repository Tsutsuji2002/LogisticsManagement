using FluentValidation;
using Logistics.Server.Application.Modules.Carrier.DTOs;

namespace Logistics.Server.Application.Modules.Carrier.Validators;

public class CreateCarrierRequestValidator : AbstractValidator<CreateCarrierRequest>
{
    public CreateCarrierRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().MaximumLength(20)
            .Matches("^[A-Z0-9_]+$").WithMessage("Carrier code must be uppercase alphanumeric.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ContactPhone).MaximumLength(20);
        RuleFor(x => x.ContactEmail).MaximumLength(100).EmailAddress().When(x => !string.IsNullOrEmpty(x.ContactEmail));
        RuleFor(x => x.WebsiteUrl).MaximumLength(500);
        RuleFor(x => x.ApiBaseUrl).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TrackingUrlTemplate).MaximumLength(500);
    }
}

public class UpdateCarrierRequestValidator : AbstractValidator<UpdateCarrierRequest>
{
    public UpdateCarrierRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ContactPhone).MaximumLength(20);
        RuleFor(x => x.ContactEmail).MaximumLength(100).EmailAddress().When(x => !string.IsNullOrEmpty(x.ContactEmail));
        RuleFor(x => x.WebsiteUrl).MaximumLength(500);
        RuleFor(x => x.ApiBaseUrl).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TrackingUrlTemplate).MaximumLength(500);
    }
}

public class CreateCarrierServiceRequestValidator : AbstractValidator<CreateCarrierServiceRequest>
{
    public CreateCarrierServiceRequestValidator()
    {
        RuleFor(x => x.ServiceCode)
            .NotEmpty().MaximumLength(50)
            .Matches("^[A-Z0-9_]+$").WithMessage("Service code must be uppercase alphanumeric.");
        RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.EstimatedTransitHours).GreaterThan(0).When(x => x.EstimatedTransitHours.HasValue);
        RuleFor(x => x.MaxWeightKg).GreaterThan(0).When(x => x.MaxWeightKg.HasValue);
        RuleFor(x => x.MaxLengthCm).GreaterThan(0).When(x => x.MaxLengthCm.HasValue);
    }
}

public class UpdateCarrierServiceRequestValidator : AbstractValidator<UpdateCarrierServiceRequest>
{
    public UpdateCarrierServiceRequestValidator()
    {
        RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.EstimatedTransitHours).GreaterThan(0).When(x => x.EstimatedTransitHours.HasValue);
        RuleFor(x => x.MaxWeightKg).GreaterThan(0).When(x => x.MaxWeightKg.HasValue);
        RuleFor(x => x.MaxLengthCm).GreaterThan(0).When(x => x.MaxLengthCm.HasValue);
    }
}

public class UpsertCarrierConfigurationRequestValidator : AbstractValidator<UpsertCarrierConfigurationRequest>
{
    private static readonly HashSet<string> ValidEnvironments = new(StringComparer.OrdinalIgnoreCase) { "Sandbox", "Production" };

    public UpsertCarrierConfigurationRequestValidator()
    {
        RuleFor(x => x.Environment)
            .NotEmpty()
            .Must(e => ValidEnvironments.Contains(e))
            .WithMessage("Environment must be 'Sandbox' or 'Production'.");
        RuleFor(x => x.ApiKey).MaximumLength(500);
        RuleFor(x => x.ApiSecret).MaximumLength(500);
        RuleFor(x => x.ClientId).MaximumLength(200);
        RuleFor(x => x.Username).MaximumLength(200);
        RuleFor(x => x.Password).MaximumLength(500);
        RuleFor(x => x.WebhookSecret).MaximumLength(500);
    }
}

public class CreateTrackingMappingRequestValidator : AbstractValidator<CreateTrackingMappingRequest>
{
    private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Draft", "ReadyForPickup", "PickedUp", "InSortingHub", "InTransit",
        "OutForDelivery", "Delivered", "DeliveryFailed", "Returning", "Returned", "Cancelled"
    };

    public CreateTrackingMappingRequestValidator()
    {
        RuleFor(x => x.ExternalStatusCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ExternalStatusName).MaximumLength(100);
        RuleFor(x => x.InternalShipmentStatus)
            .NotEmpty()
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage("Invalid internal shipment status.");
        RuleFor(x => x.InternalSubStatus).MaximumLength(50);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public class UpdateTrackingMappingRequestValidator : AbstractValidator<UpdateTrackingMappingRequest>
{
    private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Draft", "ReadyForPickup", "PickedUp", "InSortingHub", "InTransit",
        "OutForDelivery", "Delivered", "DeliveryFailed", "Returning", "Returned", "Cancelled"
    };

    public UpdateTrackingMappingRequestValidator()
    {
        RuleFor(x => x.ExternalStatusName).MaximumLength(100);
        RuleFor(x => x.InternalShipmentStatus)
            .NotEmpty()
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage("Invalid internal shipment status.");
        RuleFor(x => x.InternalSubStatus).MaximumLength(50);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public class CarrierBookShipmentRequestValidator : AbstractValidator<CarrierBookShipmentRequest>
{
    public CarrierBookShipmentRequestValidator()
    {
        RuleFor(x => x.ShipmentIdentifier).NotEmpty();
        RuleFor(x => x.CarrierServiceCode).MaximumLength(50);
    }
}

public class CarrierRateQueryRequestValidator : AbstractValidator<CarrierRateQueryRequest>
{
    public CarrierRateQueryRequestValidator()
    {
        RuleFor(x => x.CarrierId).GreaterThan(0);
        RuleFor(x => x.WeightKg).GreaterThan(0);
        RuleFor(x => x.LengthCm).GreaterThanOrEqualTo(0);
        RuleFor(x => x.WidthCm).GreaterThanOrEqualTo(0);
        RuleFor(x => x.HeightCm).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DeclaredValue).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CodAmount).GreaterThanOrEqualTo(0);
    }
}
