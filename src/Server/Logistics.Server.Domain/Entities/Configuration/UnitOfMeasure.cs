namespace Logistics.Server.Domain.Entities.Configuration;

public class UnitOfMeasure
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty; // KG, G, CM, M, CBM, PCS
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Weight, Dimension, Volume, Count
    public decimal ConversionFactor { get; set; } = 1.0m;
    public bool IsBaseUnit { get; set; }
    public bool IsActive { get; set; } = true;
}
