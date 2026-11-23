using Logistics.Server.Domain.Common;
using Logistics.Server.Domain.Entities.Identity;

namespace Logistics.Server.Domain.Entities.Audit;

public class FileAttachment : BaseEntity
{
    public string EntityType { get; set; } = string.Empty; // ProofOfDelivery, CustomerContract, CarrierLabel, Invoice, Receipt, DamageReport
    public long? EntityId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string FileHashSha256 { get; set; } = string.Empty;
    public long? UploadedByUserId { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }

    // Navigation
    public virtual UserAccount? UploadedByUser { get; set; }
}
