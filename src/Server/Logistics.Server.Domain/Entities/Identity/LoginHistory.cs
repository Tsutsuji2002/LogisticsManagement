namespace Logistics.Server.Domain.Entities.Identity;

public class LoginHistory
{
    public long Id { get; set; }
    public long? UserId { get; set; }
    public string UsernameAttempted { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public bool IsSuccessful { get; set; }
    public string? FailureReason { get; set; }
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual UserAccount? User { get; set; }
}
