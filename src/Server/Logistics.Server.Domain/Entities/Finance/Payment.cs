using Logistics.Server.Domain.Common;
using Logistics.Server.Domain.Entities.Configuration;
using Logistics.Server.Domain.Entities.Crm;
using Logistics.Server.Domain.Entities.Identity;

namespace Logistics.Server.Domain.Entities.Finance;

public class Payment : BaseEntity
{
    public string PaymentNumber { get; set; } = string.Empty;
    public long CustomerId { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public string PaymentMethod { get; set; } = "BankTransfer"; // Cash, BankTransfer, CreditCard, InternalCredit, CodDeduction
    public decimal Amount { get; set; }
    public long CurrencyId { get; set; }
    public string? TransactionReference { get; set; }
    public string? BankName { get; set; }
    public string? BankAccount { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = "Confirmed"; // Pending, Confirmed, Cancelled
    public long ConfirmedByUserId { get; set; }
    public DateTime ConfirmedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual Customer Customer { get; set; } = null!;
    public virtual Currency Currency { get; set; } = null!;
    public virtual UserAccount ConfirmedByUser { get; set; } = null!;
    public virtual ICollection<PaymentAllocation> Allocations { get; set; } = [];
}
