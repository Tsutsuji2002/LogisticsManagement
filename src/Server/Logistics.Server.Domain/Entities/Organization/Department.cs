using Logistics.Server.Domain.Common;

namespace Logistics.Server.Domain.Entities.Organization;

public class Department : BaseEntity
{
    public long CompanyId { get; set; }
    public long BranchId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long? ManagerEmployeeId { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual Company Company { get; set; } = null!;
    public virtual Branch Branch { get; set; } = null!;
    public virtual Employee? ManagerEmployee { get; set; }
    public virtual ICollection<Employee> Employees { get; set; } = [];
}
