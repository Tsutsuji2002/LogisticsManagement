using Logistics.Server.Domain.Common;
using Logistics.Server.Domain.Entities.Sales;

namespace Logistics.Server.Domain.Entities.Shipping;

public class ShipmentPackage : BaseEntity
{
    public long ShipmentId { get; set; }
    public long? OrderPackageId { get; set; }
    public string PackageCode { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public decimal WeightKg { get; set; }
    public decimal LengthCm { get; set; }
    public decimal WidthCm { get; set; }
    public decimal HeightCm { get; set; }
    public decimal VolumetricWeightKg { get; set; }
    public string PackageStatus { get; set; } = "Created"; // Created, Received, Sorted, InTransit, Delivered, Damaged, Lost
    public string? SealNumber { get; set; }

    // Navigation
    public virtual Shipment Shipment { get; set; } = null!;
    public virtual OrderPackage? OrderPackage { get; set; }
}
