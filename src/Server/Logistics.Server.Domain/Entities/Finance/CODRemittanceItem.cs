using Logistics.Server.Domain.Entities.Shipping;

namespace Logistics.Server.Domain.Entities.Finance;

public class CODRemittanceItem
{
    public long Id { get; set; }
    public long CODRemittanceId { get; set; }
    public long CODCollectionId { get; set; }
    public long ShipmentId { get; set; }
    public decimal CodAmount { get; set; }
    public decimal DeductedShippingFee { get; set; }
    public decimal NetAmount { get; set; }

    // Navigation
    public virtual CODRemittance CODRemittance { get; set; } = null!;
    public virtual CODCollection CODCollection { get; set; } = null!;
    public virtual Shipment Shipment { get; set; } = null!;
}
