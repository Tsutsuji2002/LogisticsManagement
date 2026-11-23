namespace Logistics.Server.Application.Modules.Catalog.DTOs;

public record UnitOfMeasureDto
{
    public long Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public decimal ConversionFactor { get; init; }
    public bool IsBaseUnit { get; init; }
    public bool IsActive { get; init; }
}

public record CreateUnitOfMeasureRequest
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty; // Weight, Dimension, Volume, Count
    public decimal ConversionFactor { get; init; } = 1.0m;
    public bool IsBaseUnit { get; init; }
}

public record UpdateUnitOfMeasureRequest
{
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public decimal ConversionFactor { get; init; } = 1.0m;
    public bool IsBaseUnit { get; init; }
}

public record UpdateUnitStatusRequest
{
    public bool IsActive { get; init; }
    public string? Reason { get; init; }
}
