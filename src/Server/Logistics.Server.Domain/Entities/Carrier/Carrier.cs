using Logistics.Server.Domain.Common;

namespace Logistics.Server.Domain.Entities.Carrier;

public class Carrier : BaseEntity
{
    public string Code { get; set; } = string.Empty; // GHN, GHTK, VTPOST, VNPOST
    public string Name { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public string? WebsiteUrl { get; set; }
    public string ApiBaseUrl { get; set; } = string.Empty;
    public string? TrackingUrlTemplate { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual ICollection<CarrierService> Services { get; set; } = [];
    public virtual ICollection<CarrierConfiguration> Configurations { get; set; } = [];
    public virtual ICollection<CarrierTrackingMapping> TrackingMappings { get; set; } = [];
}
