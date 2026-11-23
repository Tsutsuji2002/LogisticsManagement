using Logistics.Server.Domain.Entities.Configuration;
using Logistics.Server.Domain.Entities.Shipping;

namespace Logistics.Server.Domain.Entities.Finance;

public class ShipmentCharge
{
    public long Id { get; set; }
    public long ShipmentId { get; set; }
    public string ChargeType { get; set; } = "BaseFreight"; // BaseFreight, CodFee, Insurance, FuelSurcharge, RemoteAreaFee, ReturnFee, RedeliveryFee, Other
    public decimal Amount { get; set; }
    public long CurrencyId { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsInvoiced { get; set; }
    public long? InvoiceId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual Shipment Shipment { get; set; } = null!;
    public virtual Currency Currency { get; set; } = null!;
    public virtual Invoice? Invoice { get; set; }
}
