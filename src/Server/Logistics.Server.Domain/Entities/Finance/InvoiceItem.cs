using Logistics.Server.Domain.Entities.Shipping;

namespace Logistics.Server.Domain.Entities.Finance;

public class InvoiceItem
{
    public long Id { get; set; }
    public long InvoiceId { get; set; }
    public long? ShipmentId { get; set; }
    public long? ShipmentChargeId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }

    // Navigation
    public virtual Invoice Invoice { get; set; } = null!;
    public virtual Shipment? Shipment { get; set; }
    public virtual ShipmentCharge? ShipmentCharge { get; set; }
}
