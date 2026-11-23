using Logistics.Server.Domain.Common;
using Logistics.Server.Domain.Entities.Crm;

namespace Logistics.Server.Domain.Entities.Finance;

public class Invoice : BaseEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public long CustomerId { get; set; }
    public DateOnly InvoiceDate { get; set; }
    public DateOnly DueDate { get; set; }
    public DateOnly? PeriodStartDate { get; set; }
    public DateOnly? PeriodEndDate { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxRate { get; set; } = 8.0m;
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal OutstandingAmount { get; private set; } // Computed in SQL Server
    public string Status { get; set; } = "Draft"; // Draft, Issued, PartiallyPaid, Paid, Overdue, Cancelled
    public int PaymentTermsDays { get; set; } = 30;
    public string? Notes { get; set; }

    // Navigation
    public virtual Customer Customer { get; set; } = null!;
    public virtual ICollection<InvoiceItem> InvoiceItems { get; set; } = [];
    public virtual ICollection<PaymentAllocation> PaymentAllocations { get; set; } = [];
}
