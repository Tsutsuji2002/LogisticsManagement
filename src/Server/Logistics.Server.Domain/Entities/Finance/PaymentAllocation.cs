using Logistics.Server.Domain.Entities.Identity;

namespace Logistics.Server.Domain.Entities.Finance;

public class PaymentAllocation
{
    public long Id { get; set; }
    public long PaymentId { get; set; }
    public long InvoiceId { get; set; }
    public decimal AllocatedAmount { get; set; }
    public DateTime AllocatedAt { get; set; } = DateTime.UtcNow;
    public long? AllocatedByUserId { get; set; }

    // Navigation
    public virtual Payment Payment { get; set; } = null!;
    public virtual Invoice Invoice { get; set; } = null!;
    public virtual UserAccount? AllocatedByUser { get; set; }
}
