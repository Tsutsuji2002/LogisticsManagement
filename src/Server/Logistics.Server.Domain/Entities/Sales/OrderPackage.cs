using Logistics.Server.Domain.Common;

namespace Logistics.Server.Domain.Entities.Sales;

public class OrderPackage : BaseEntity
{
    public long OrderId { get; set; }
    public string PackageCode { get; set; } = string.Empty;
    public decimal WeightKg { get; set; }
    public decimal LengthCm { get; set; }
    public decimal WidthCm { get; set; }
    public decimal HeightCm { get; set; }
    public decimal VolumetricWeightKg { get; set; }
    public decimal DeclaredValue { get; set; }
    public string? Description { get; set; }
    public string? Barcode { get; set; }
    public string? SpecialInstructions { get; set; }

    // Navigation
    public virtual Order Order { get; set; } = null!;
}
