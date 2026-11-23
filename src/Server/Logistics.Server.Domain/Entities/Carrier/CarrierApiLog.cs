using Logistics.Server.Domain.Common;

namespace Logistics.Server.Domain.Entities.Carrier;

public class CarrierApiLog : BaseHistoricalEntity
{
    public long CarrierId { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public string? RequestPayloadMasked { get; set; }
    public string? ResponsePayloadMasked { get; set; }
    public int ResponseStatusCode { get; set; }
    public int DurationMs { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }

    // Navigation
    public virtual Carrier Carrier { get; set; } = null!;
}
