using Logistics.Server.Domain.Common;
using Logistics.Server.Domain.Entities.Crm;
using Logistics.Server.Domain.Entities.Identity;

namespace Logistics.Server.Domain.Entities.Organization;

public class Employee : BaseEntity
{
    public long BranchId { get; set; }
    public long? DepartmentId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public DateOnly HireDate { get; set; }
    public string Status { get; set; } = "Active";
    public long? AddressId { get; set; }

    // Navigation
    public virtual Branch Branch { get; set; } = null!;
    public virtual Department? Department { get; set; }
    public virtual Address? Address { get; set; }
    public virtual UserAccount? UserAccount { get; set; }
}
