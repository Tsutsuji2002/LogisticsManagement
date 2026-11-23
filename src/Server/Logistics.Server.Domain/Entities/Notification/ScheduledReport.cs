using Logistics.Server.Domain.Common;

namespace Logistics.Server.Domain.Entities.Notification;

public class ScheduledReport : BaseEntity
{
    public long ReportDefinitionId { get; set; }
    public string ScheduleCron { get; set; } = string.Empty;
    public string OutputFormat { get; set; } = "Pdf"; // Pdf, Excel, Csv
    public string RecipientEmails { get; set; } = string.Empty;
    public DateTime? LastRunAt { get; set; }
    public DateTime NextRunAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual ReportDefinition ReportDefinition { get; set; } = null!;
}
