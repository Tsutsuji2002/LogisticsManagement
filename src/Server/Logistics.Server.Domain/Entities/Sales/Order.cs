using Logistics.Server.Domain.Common;
using Logistics.Server.Domain.Entities.Configuration;
using Logistics.Server.Domain.Entities.Crm;
using Logistics.Server.Domain.Entities.Organization;

namespace Logistics.Server.Domain.Entities.Sales;

public class Order : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;
    public long CustomerId { get; set; }
    public long BranchId { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public DateTime? RequestedPickupDate { get; set; }
    public DateTime? RequiredDeliveryDate { get; set; }
    public string OrderStatus { get; set; } = "Draft"; // Draft, Confirmed, Processing, PartiallyShipped, Shipped, Delivered, Cancelled
    public string PaymentStatus { get; set; } = "Pending"; // Pending, Paid, Partial, Invoiced
    public decimal TotalWeightKg { get; set; }
    public decimal TotalVolumeCbm { get; set; }
    public decimal DeclaredValue { get; set; }
    public long CurrencyId { get; set; }
    public decimal CodAmount { get; set; }
    public string? Notes { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelledBy { get; set; }

    // Navigation
    public virtual Customer Customer { get; set; } = null!;
    public virtual Branch Branch { get; set; } = null!;
    public virtual Currency Currency { get; set; } = null!;
    public virtual ICollection<OrderItem> OrderItems { get; set; } = [];
    public virtual ICollection<OrderAddress> OrderAddresses { get; set; } = [];
    public virtual ICollection<OrderPackage> OrderPackages { get; set; } = [];
}
