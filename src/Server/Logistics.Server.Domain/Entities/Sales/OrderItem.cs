using Logistics.Server.Domain.Common;
using Logistics.Server.Domain.Entities.Catalog;
using Logistics.Server.Domain.Entities.Configuration;

namespace Logistics.Server.Domain.Entities.Sales;

public class OrderItem : BaseEntity
{
    public long OrderId { get; set; }
    public long? ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public int Quantity { get; set; }
    public long UnitOfMeasureId { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal DeclaredValue { get; set; }
    public decimal WeightKg { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public virtual Order Order { get; set; } = null!;
    public virtual Product? Product { get; set; }
    public virtual UnitOfMeasure UnitOfMeasure { get; set; } = null!;
}
