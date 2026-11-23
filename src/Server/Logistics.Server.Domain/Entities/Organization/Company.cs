using Logistics.Server.Domain.Common;

namespace Logistics.Server.Domain.Entities.Organization;

public class Company : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string TradingName { get; set; } = string.Empty;
    public string TaxNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Website { get; set; }
    public string? LogoUrl { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual ICollection<Branch> Branches { get; set; } = [];
    public virtual ICollection<Department> Departments { get; set; } = [];
}
