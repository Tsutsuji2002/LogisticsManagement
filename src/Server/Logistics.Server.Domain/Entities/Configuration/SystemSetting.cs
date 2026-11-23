namespace Logistics.Server.Domain.Entities.Configuration;

public class SystemSetting
{
    public long Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ValueType { get; set; } = "String"; // String, Integer, Decimal, Boolean, Json
    public bool IsEncrypted { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }
}
