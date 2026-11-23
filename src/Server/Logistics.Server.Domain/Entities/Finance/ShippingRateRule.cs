using Logistics.Server.Domain.Entities.Configuration;

namespace Logistics.Server.Domain.Entities.Finance;

public class ShippingRateRule
{
    public long Id { get; set; }
    public long ShippingRateCardId { get; set; }
    public string ServiceType { get; set; } = "StandardGround";
    public long? OriginProvinceId { get; set; }
    public long? DestinationProvinceId { get; set; }
    public decimal FromWeightKg { get; set; }
    public decimal ToWeightKg { get; set; }
    public decimal BaseRate { get; set; }
    public decimal AdditionalPerKgRate { get; set; }
    public decimal SurchargePercent { get; set; }
    public decimal MinimumFee { get; set; }
    public int EstimatedDays { get; set; } = 2;

    // Navigation
    public virtual ShippingRateCard ShippingRateCard { get; set; } = null!;
    public virtual Province? OriginProvince { get; set; }
    public virtual Province? DestinationProvince { get; set; }
}
