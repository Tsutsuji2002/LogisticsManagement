using Logistics.Server.Domain.Common;
using Logistics.Server.Domain.Entities.Configuration;
using Logistics.Server.Domain.Entities.Identity;
using Logistics.Server.Domain.Entities.Organization;
using Logistics.Server.Domain.Entities.Shipping;

namespace Logistics.Server.Domain.Entities.Finance;

public class CODCollection : BaseEntity
{
    public long ShipmentId { get; set; }
    public decimal CollectedAmount { get; set; }
    public long CurrencyId { get; set; }
    public DateTime CollectionDate { get; set; } = DateTime.UtcNow;
    public long CollectedByEmployeeId { get; set; }
    public string Status { get; set; } = "CollectedByDriver"; // CollectedByDriver, DepositedToBranch, VerifiedByCashier, RemittedToCustomer
    public string? DriverReceiptNumber { get; set; }
    public long? VerifiedByUserId { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public virtual Shipment Shipment { get; set; } = null!;
    public virtual Currency Currency { get; set; } = null!;
    public virtual Employee CollectedByEmployee { get; set; } = null!;
    public virtual UserAccount? VerifiedByUser { get; set; }
    public virtual CODRemittanceItem? RemittanceItem { get; set; }
}
