namespace Logistics.Server.Application.Modules.Catalog.DTOs;

public record ServiceDto
{
    public string ServiceCode { get; init; } = string.Empty;
    public string ServiceName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string ServiceCategory { get; init; } = string.Empty;
    public int? EstimatedTransitHours { get; init; }
    public decimal? MaxWeightKg { get; init; }
    public decimal? MaxLengthCm { get; init; }
    public bool IsCodSupported { get; init; } = true;
    public bool IsInsuranceSupported { get; init; } = true;
    public bool IsActive { get; init; } = true;
    public string? ProviderType { get; init; } // "Internal" or "Carrier"
}
