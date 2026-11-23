using Logistics.Server.Domain.Common;

namespace Logistics.Server.Domain.Entities.Identity;

public class Role : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual ICollection<UserRole> UserRoles { get; set; } = [];
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = [];
}
