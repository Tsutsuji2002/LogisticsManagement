using Logistics.Server.Domain.Common;
using Logistics.Server.Domain.Entities.Organization;

namespace Logistics.Server.Domain.Entities.Crm;

public class Customer : BaseEntity
{
    public long BranchId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerType { get; set; } = "Individual"; // Individual, Corporate
    public string Name { get; set; } = string.Empty;
    public string? TradeName { get; set; }
    public string? TaxCode { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public decimal CreditLimit { get; set; }
    public int CreditTermDays { get; set; }
    public string PaymentType { get; set; } = "Prepaid"; // Prepaid, PostpaidMonthly, CodOffset
    public long? AccountManagerEmployeeId { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual Branch Branch { get; set; } = null!;
    public virtual Employee? AccountManagerEmployee { get; set; }
    public virtual ICollection<CustomerContact> Contacts { get; set; } = [];
    public virtual ICollection<CustomerAddress> CustomerAddresses { get; set; } = [];
    public virtual ICollection<CustomerContract> Contracts { get; set; } = [];
    public virtual ICollection<CustomerPricingRule> PricingRules { get; set; } = [];
}
