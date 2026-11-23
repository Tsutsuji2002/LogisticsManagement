using Logistics.Server.Domain.Common;

namespace Logistics.Server.Domain.Entities.Carrier;

public class CarrierService : BaseEntity
{
    public long CarrierId { get; set; }
    public string ServiceCode { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? EstimatedTransitHours { get; set; }
    public decimal? MaxWeightKg { get; set; }
    public decimal? MaxLengthCm { get; set; }
    public bool IsCodSupported { get; set; } = true;
    public bool IsInsuranceSupported { get; set; } = true;
    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual Carrier Carrier { get; set; } = null!;
}
