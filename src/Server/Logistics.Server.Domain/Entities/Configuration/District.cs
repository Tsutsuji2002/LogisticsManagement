namespace Logistics.Server.Domain.Entities.Configuration;

public class District
{
    public long Id { get; set; }
    public long ProvinceId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Quận / Huyện / Thị xã / Thành phố
    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual Province Province { get; set; } = null!;
    public virtual ICollection<Ward> Wards { get; set; } = [];
}
