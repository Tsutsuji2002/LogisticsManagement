using Logistics.Server.Domain.Common;
using Logistics.Server.Domain.Entities.Organization;

namespace Logistics.Server.Domain.Entities.Identity;

public class UserAccount : BaseEntity
{
    public long? EmployeeId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString("N");
    public bool IsActive { get; set; } = true;
    public bool IsLockedOut { get; set; }
    public DateTime? LockoutEndAt { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LastLoginAt { get; set; }

    // Navigation
    public virtual Employee? Employee { get; set; }
    public virtual ICollection<UserRole> UserRoles { get; set; } = [];
    public virtual ICollection<UserBranchAccess> BranchAccesses { get; set; } = [];
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public virtual ICollection<UserSession> UserSessions { get; set; } = [];
    public virtual ICollection<LoginHistory> LoginHistories { get; set; } = [];
}
