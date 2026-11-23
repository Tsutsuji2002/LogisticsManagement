using Logistics.Server.Domain.Common;
using Logistics.Server.Domain.Entities.Configuration;
using Logistics.Server.Domain.Entities.Crm;

namespace Logistics.Server.Domain.Entities.Finance;

public class FinancialLedgerEntry : BaseEntity
{
    public string EntryNumber { get; set; } = string.Empty;
    public long CustomerId { get; set; }
    public string EntryType { get; set; } = string.Empty; // InvoiceCharge, PaymentReceipt, CodCollection, CodPayout, FeeAdjustment, CreditNote
    public string ReferenceType { get; set; } = string.Empty; // Invoice, Payment, CodRemittance, Adjustment
    public long ReferenceId { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public decimal RunningBalance { get; set; }
    public long CurrencyId { get; set; }
    public DateTime EntryDate { get; set; } = DateTime.UtcNow;
    public string Description { get; set; } = string.Empty;

    // Navigation
    public virtual Customer Customer { get; set; } = null!;
    public virtual Currency Currency { get; set; } = null!;
}
