using Logistics.Server.Domain.Common;
using Logistics.Server.Domain.Entities.Finance;

namespace Logistics.Server.Domain.Entities.Crm;

public class CustomerPricingRule : BaseEntity
{
    public long CustomerId { get; set; }
    public long? CustomerContractId { get; set; }
    public long? ShippingRateCardId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal DiscountPercent { get; set; }
    public decimal FlatDiscountAmount { get; set; }
    public int Priority { get; set; } = 1;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual Customer Customer { get; set; } = null!;
    public virtual CustomerContract? CustomerContract { get; set; }
    public virtual ShippingRateCard? ShippingRateCard { get; set; }
}
