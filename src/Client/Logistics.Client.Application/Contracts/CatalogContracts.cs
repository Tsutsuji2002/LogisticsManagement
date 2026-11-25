namespace Logistics.Client.Application.Contracts.Catalog;

public record CategoryDto
{
    public long Id { get; init; }
    public Guid PublicId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public long? ParentCategoryId { get; init; }
    public string? ParentCategoryName { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record CreateCategoryRequest
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public long? ParentCategoryId { get; init; }
}

public record UpdateCategoryRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public long? ParentCategoryId { get; init; }
}

public record UpdateCategoryStatusRequest
{
    public bool IsActive { get; init; }
    public string? Reason { get; init; }
}

public record ProductDto
{
    public long Id { get; init; }
    public Guid PublicId { get; init; }
    public long CategoryId { get; init; }
    public string CategoryCode { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public string? Barcode { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public long UnitOfMeasureId { get; init; }
    public string UnitOfMeasureCode { get; init; } = string.Empty;
    public string UnitOfMeasureName { get; init; } = string.Empty;
    public decimal WeightKg { get; init; }
    public decimal LengthCm { get; init; }
    public decimal WidthCm { get; init; }
    public decimal HeightCm { get; init; }
    public decimal VolumeCbm { get; init; }
    public decimal DeclaredValue { get; init; }
    public long CurrencyId { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public bool IsPerishable { get; init; }
    public bool IsDangerous { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public record CreateProductRequest
{
    public long CategoryId { get; init; }
    public string Sku { get; init; } = string.Empty;
    public string? Barcode { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public long UnitOfMeasureId { get; init; }
    public decimal WeightKg { get; init; }
    public decimal LengthCm { get; init; }
    public decimal WidthCm { get; init; }
    public decimal HeightCm { get; init; }
    public decimal DeclaredValue { get; init; }
    public long? CurrencyId { get; init; }
    public bool IsPerishable { get; init; }
    public bool IsDangerous { get; init; }
}

public record UpdateProductRequest
{
    public long CategoryId { get; init; }
    public string? Barcode { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public long UnitOfMeasureId { get; init; }
    public decimal WeightKg { get; init; }
    public decimal LengthCm { get; init; }
    public decimal WidthCm { get; init; }
    public decimal HeightCm { get; init; }
    public decimal DeclaredValue { get; init; }
    public long? CurrencyId { get; init; }
    public bool IsPerishable { get; init; }
    public bool IsDangerous { get; init; }
}

public record UpdateProductStatusRequest
{
    public bool IsActive { get; init; }
    public string? Reason { get; init; }
}

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
    public string Type { get; init; } = "Count";
    public decimal ConversionFactor { get; init; } = 1.0m;
    public bool IsBaseUnit { get; init; }
}

public record UpdateUnitOfMeasureRequest
{
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = "Count";
    public decimal ConversionFactor { get; init; } = 1.0m;
    public bool IsBaseUnit { get; init; }
}

public record UpdateUnitStatusRequest
{
    public bool IsActive { get; init; }
    public string? Reason { get; init; }
}

public record ServiceDto
{
    public string ServiceCode { get; init; } = string.Empty;
    public string ServiceName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string ServiceCategory { get; init; } = string.Empty;
    public int? EstimatedTransitHours { get; init; }
    public decimal? MaxWeightKg { get; init; }
    public decimal? MaxLengthCm { get; init; }
    public bool IsCodSupported { get; init; }
    public bool IsInsuranceSupported { get; init; }
    public bool IsActive { get; init; }
    public string? ProviderType { get; init; }
}
