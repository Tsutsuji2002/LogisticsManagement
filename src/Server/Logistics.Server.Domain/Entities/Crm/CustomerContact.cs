using Logistics.Server.Domain.Common;

namespace Logistics.Server.Domain.Entities.Crm;

public class CustomerContact : BaseEntity
{
    public long CustomerId { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public string? JobTitle { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool IsPrimary { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public virtual Customer Customer { get; set; } = null!;
}
