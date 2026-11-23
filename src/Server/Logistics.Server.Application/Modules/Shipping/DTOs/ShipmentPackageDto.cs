namespace Logistics.Server.Application.Modules.Shipping.DTOs;

public record ShipmentPackageDto
{
    public long Id { get; init; }
    public Guid PublicId { get; init; }
    public long ShipmentId { get; init; }
    public long? OrderPackageId { get; init; }
    public string PackageCode { get; init; } = string.Empty;
    public string Barcode { get; init; } = string.Empty;
    public decimal WeightKg { get; init; }
    public decimal LengthCm { get; init; }
    public decimal WidthCm { get; init; }
    public decimal HeightCm { get; init; }
    public decimal VolumetricWeightKg { get; init; }
    /// <summary>"Created", "Received", "Sorted", "InTransit", "Delivered", "Damaged", "Lost"</summary>
    public string PackageStatus { get; init; } = "Created";
    public string? SealNumber { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? UpdatedBy { get; init; }
}

public record AddShipmentPackageRequest
{
    public long? OrderPackageId { get; init; }
    public decimal WeightKg { get; init; }
    public decimal LengthCm { get; init; }
    public decimal WidthCm { get; init; }
    public decimal HeightCm { get; init; }
    public string? SealNumber { get; init; }
}

public record UpdateShipmentPackageRequest
{
    public decimal WeightKg { get; init; }
    public decimal LengthCm { get; init; }
    public decimal WidthCm { get; init; }
    public decimal HeightCm { get; init; }
    public string? SealNumber { get; init; }
    /// <summary>"Created", "Received", "Sorted", "InTransit", "Delivered", "Damaged", "Lost"</summary>
    public string PackageStatus { get; init; } = string.Empty;
}
