using Logistics.Server.Domain.Common;
using Logistics.Server.Domain.Entities.Audit;

namespace Logistics.Server.Domain.Entities.Crm;

public class CustomerContract : BaseEntity
{
    public long CustomerId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Status { get; set; } = "Draft"; // Draft, Active, Expired, Terminated
    public decimal DiscountRate { get; set; }
    public int PaymentTermDays { get; set; } = 30;
    public DateOnly? SignedAt { get; set; }
    public long? FileAttachmentId { get; set; }

    // Navigation
    public virtual Customer Customer { get; set; } = null!;
    public virtual FileAttachment? FileAttachment { get; set; }
}
