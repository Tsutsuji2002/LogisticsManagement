namespace Logistics.Server.Domain.Entities.Configuration;

public class Ward
{
    public long Id { get; set; }
    public long DistrictId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Phường / Xã / Thị trấn
    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual District District { get; set; } = null!;
}
