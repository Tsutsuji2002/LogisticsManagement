using Logistics.Server.Domain.Common;
using Logistics.Server.Domain.Entities.Configuration;

namespace Logistics.Server.Domain.Entities.Finance;

public class ShippingRateCard : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long BaseCurrencyId { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual Currency BaseCurrency { get; set; } = null!;
    public virtual ICollection<ShippingRateRule> Rules { get; set; } = [];
}
