namespace Logistics.Server.Domain.Entities.Configuration;

public class Country
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty; // ISO 2-letter
    public string Alpha3Code { get; set; } = string.Empty; // ISO 3-letter
    public string Name { get; set; } = string.Empty;
    public string? NativeName { get; set; }
    public int? NumericCode { get; set; }
    public string? PhonePrefix { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual ICollection<Province> Provinces { get; set; } = [];
}
