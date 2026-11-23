using Logistics.Server.Domain.Common;
using Logistics.Server.Domain.Entities.Configuration;

namespace Logistics.Server.Domain.Entities.Catalog;

public class Product : BaseEntity
{
    public long CategoryId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long UnitOfMeasureId { get; set; }
    public decimal WeightKg { get; set; }
    public decimal LengthCm { get; set; }
    public decimal WidthCm { get; set; }
    public decimal HeightCm { get; set; }
    public decimal DeclaredValue { get; set; }
    public long CurrencyId { get; set; }
    public bool IsPerishable { get; set; }
    public bool IsDangerous { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual ProductCategory Category { get; set; } = null!;
    public virtual UnitOfMeasure UnitOfMeasure { get; set; } = null!;
    public virtual Currency Currency { get; set; } = null!;
}
