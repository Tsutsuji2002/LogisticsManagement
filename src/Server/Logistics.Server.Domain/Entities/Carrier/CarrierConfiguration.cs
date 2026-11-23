namespace Logistics.Server.Domain.Entities.Carrier;

public class CarrierConfiguration
{
    public long Id { get; set; }
    public long CarrierId { get; set; }
    public string Environment { get; set; } = "Production"; // Sandbox, Production
    public string ApiKey { get; set; } = string.Empty;
    public string? ApiSecret { get; set; }
    public string? ClientId { get; set; }
    public string? Username { get; set; }
    public string? EncryptedPassword { get; set; }
    public string? WebhookSecret { get; set; }
    public string? SettingsJson { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }

    // Navigation
    public virtual Carrier Carrier { get; set; } = null!;
}
