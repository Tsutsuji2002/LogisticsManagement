using Logistics.Server.Domain.Common;
using Logistics.Server.Domain.Entities.Identity;

namespace Logistics.Server.Domain.Entities.Audit;

public class AuditLog : BaseEntity
{
    public long? UserId { get; set; }
    public string? Username { get; set; }
    public string? UserRole { get; set; }
    public string Action { get; set; } = string.Empty; // Create, Update, Delete, StatusChange, Login, Logout
    public string TableName { get; set; } = string.Empty;
    public string SchemaName { get; set; } = string.Empty;
    public string PrimaryKey { get; set; } = string.Empty;
    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
    public string? AffectedColumns { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual UserAccount? User { get; set; }
}
