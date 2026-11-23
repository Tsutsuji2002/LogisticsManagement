using Logistics.Server.Domain.Common;

namespace Logistics.Server.Domain.Entities.Carrier;

public class CarrierWebhookLog : BaseHistoricalEntity
{
    public long CarrierId { get; set; }
    public string? ExternalEventId { get; set; }
    public string WebhookUrl { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = "POST";
    public string? HeadersJson { get; set; }
    public string PayloadJson { get; set; } = string.Empty;
    public int ResponseStatusCode { get; set; }
    public string? ResponseBody { get; set; }
    public bool ProcessedSuccessfully { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    public string? ErrorDetails { get; set; }
    public string ClientIpAddress { get; set; } = string.Empty;

    // Navigation
    public virtual Carrier Carrier { get; set; } = null!;
}
