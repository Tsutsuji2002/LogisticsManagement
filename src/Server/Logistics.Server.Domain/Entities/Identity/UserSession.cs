namespace Logistics.Server.Domain.Entities.Identity;

public class UserSession
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string SessionTokenHash { get; set; } = string.Empty;
    public string? DeviceInfo { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public bool IsTerminated { get; set; }

    // Navigation
    public virtual UserAccount User { get; set; } = null!;
}
