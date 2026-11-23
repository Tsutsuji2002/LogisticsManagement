namespace Logistics.Server.Domain.Entities.Identity;

public class RolePermission
{
    public long Id { get; set; }
    public long RoleId { get; set; }
    public long PermissionId { get; set; }
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
    public string? GrantedBy { get; set; }

    // Navigation
    public virtual Role Role { get; set; } = null!;
    public virtual Permission Permission { get; set; } = null!;
}
