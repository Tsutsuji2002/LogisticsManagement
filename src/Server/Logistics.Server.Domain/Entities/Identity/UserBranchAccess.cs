using Logistics.Server.Domain.Entities.Organization;

namespace Logistics.Server.Domain.Entities.Identity;

public class UserBranchAccess
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long BranchId { get; set; }
    public bool IsDefault { get; set; }
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
    public string? GrantedBy { get; set; }

    // Navigation
    public virtual UserAccount User { get; set; } = null!;
    public virtual Branch Branch { get; set; } = null!;
}
