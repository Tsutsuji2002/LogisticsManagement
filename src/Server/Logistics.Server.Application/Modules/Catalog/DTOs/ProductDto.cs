namespace Logistics.Server.Application.Modules.Catalog.DTOs;

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
    public DateTime CreatedDate => CreatedAt;
    public string? CreatedBy { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public DateTime? UpdatedDate => UpdatedAt;
    public string? UpdatedBy { get; init; }
}

public record ProductDetailDto : ProductDto
{
    public string? ParentCategoryName { get; init; }
    public string UnitType { get; init; } = string.Empty;
    public string CurrencySymbol { get; init; } = string.Empty;
}

public record ProductSearchResultDto
{
    public long Id { get; init; }
    public Guid PublicId { get; init; }
    public string Sku { get; init; } = string.Empty;
    public string? Barcode { get; init; }
    public string Name { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public string UnitOfMeasureCode { get; init; } = string.Empty;
    public decimal WeightKg { get; init; }
    public decimal VolumeCbm { get; init; }
    public decimal DeclaredValue { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public bool IsPerishable { get; init; }
    public bool IsDangerous { get; init; }
    public bool IsActive { get; init; }
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
