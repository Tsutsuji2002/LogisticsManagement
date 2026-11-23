using Logistics.Server.Domain.Common;
using Logistics.Server.Domain.Entities.Crm;

namespace Logistics.Server.Domain.Entities.Organization;

public class Branch : BaseEntity
{
    public long CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string BranchType { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public long? AddressId { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual Company Company { get; set; } = null!;
    public virtual Address? Address { get; set; }
    public virtual ICollection<Department> Departments { get; set; } = [];
    public virtual ICollection<Employee> Employees { get; set; } = [];
    public virtual ICollection<Warehouse> Warehouses { get; set; } = [];
}
