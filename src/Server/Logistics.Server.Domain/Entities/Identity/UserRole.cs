namespace Logistics.Server.Domain.Entities.Identity;

public class UserRole
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long RoleId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public string? AssignedBy { get; set; }

    // Navigation
    public virtual UserAccount User { get; set; } = null!;
    public virtual Role Role { get; set; } = null!;
}
