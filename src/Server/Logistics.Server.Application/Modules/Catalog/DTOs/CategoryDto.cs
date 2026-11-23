namespace Logistics.Server.Application.Modules.Catalog.DTOs;

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
    public DateTime CreatedDate => CreatedAt;
    public string? CreatedBy { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public DateTime? UpdatedDate => UpdatedAt;
    public string? UpdatedBy { get; init; }
}

public record CategoryDetailDto : CategoryDto
{
    public int SubCategoryCount { get; init; }
    public int ProductCount { get; init; }
    public int ActiveProductCount { get; init; }
}

public record CategoryTreeDto
{
    public long Id { get; init; }
    public Guid PublicId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public long? ParentCategoryId { get; init; }
    public bool IsActive { get; init; }
    public int ProductCount { get; init; }
    public List<CategoryTreeDto> Children { get; init; } = [];
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
