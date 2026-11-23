using Logistics.Server.Domain.Entities.Crm;

namespace Logistics.Server.Domain.Entities.Finance;

public class CustomerBalance
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public decimal CurrentOutstandingBalance { get; set; }
    public decimal UnbilledChargesAmount { get; set; }
    public decimal TotalCodHoldingAmount { get; set; }
    public DateTime LastUpdatedDate { get; set; } = DateTime.UtcNow;
    public byte[]? RowVersion { get; set; }

    // Navigation
    public virtual Customer Customer { get; set; } = null!;
}
