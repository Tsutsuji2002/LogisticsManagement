using Logistics.Server.Domain.Common;
using Logistics.Server.Domain.Entities.Configuration;
using Logistics.Server.Domain.Entities.Crm;
using Logistics.Server.Domain.Entities.Identity;

namespace Logistics.Server.Domain.Entities.Finance;

public class CODRemittance : BaseEntity
{
    public string RemittanceNumber { get; set; } = string.Empty;
    public long CustomerId { get; set; }
    public DateTime RemittanceDate { get; set; } = DateTime.UtcNow;
    public decimal TotalCodAmount { get; set; }
    public decimal TotalServiceFeesDeducted { get; set; }
    public decimal NetRemittanceAmount { get; set; }
    public long CurrencyId { get; set; }
    public string PaymentMethod { get; set; } = "BankTransfer";
    public string? PaymentReference { get; set; }
    public string Status { get; set; } = "Draft"; // Draft, Approved, Processing, Completed, Cancelled
    public string BankName { get; set; } = string.Empty;
    public string BankAccount { get; set; } = string.Empty;
    public string AccountHolderName { get; set; } = string.Empty;
    public long RemittedByUserId { get; set; }
    public DateTime? RemittedAt { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public virtual Customer Customer { get; set; } = null!;
    public virtual Currency Currency { get; set; } = null!;
    public virtual UserAccount RemittedByUser { get; set; } = null!;
    public virtual ICollection<CODRemittanceItem> RemittanceItems { get; set; } = [];
}
