namespace Logistics.Server.Application.Modules.Sales.DTOs;

public record OrderPackageDto
{
    public long Id { get; init; }
    public Guid PublicId { get; init; }
    public long OrderId { get; init; }
    public string PackageCode { get; init; } = string.Empty;
    public decimal WeightKg { get; init; }
    public decimal LengthCm { get; init; }
    public decimal WidthCm { get; init; }
    public decimal HeightCm { get; init; }
    public decimal VolumetricWeightKg { get; init; }
    public decimal DeclaredValue { get; init; }
    public string? Description { get; init; }
    public string? Barcode { get; init; }
    public string? SpecialInstructions { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? UpdatedBy { get; init; }
}

public record CreateOrderPackageRequest
{
    public string? PackageCode { get; init; }
    public decimal WeightKg { get; init; }
    public decimal LengthCm { get; init; }
    public decimal WidthCm { get; init; }
    public decimal HeightCm { get; init; }
    public decimal DeclaredValue { get; init; }
    public string? Description { get; init; }
    public string? Barcode { get; init; }
    public string? SpecialInstructions { get; init; }
}

public record UpdateOrderPackageRequest
{
    public decimal WeightKg { get; init; }
    public decimal LengthCm { get; init; }
    public decimal WidthCm { get; init; }
    public decimal HeightCm { get; init; }
    public decimal DeclaredValue { get; init; }
    public string? Description { get; init; }
    public string? Barcode { get; init; }
    public string? SpecialInstructions { get; init; }
}
