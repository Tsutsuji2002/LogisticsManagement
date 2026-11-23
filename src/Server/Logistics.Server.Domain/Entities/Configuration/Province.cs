namespace Logistics.Server.Domain.Entities.Configuration;

public class Province
{
    public long Id { get; set; }
    public long CountryId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Tỉnh / Thành phố
    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual Country Country { get; set; } = null!;
    public virtual ICollection<District> Districts { get; set; } = [];
}
