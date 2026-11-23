using Logistics.Server.Domain.Common;

namespace Logistics.Server.Domain.Entities.Notification;

public class ReportDefinition : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // Operations, Fleet, Warehouse, Finance, Management
    public string? Description { get; set; }
    public string QueryDefinition { get; set; } = string.Empty;
    public string? ParametersSchemaJson { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual ICollection<ScheduledReport> ScheduledReports { get; set; } = [];
}
