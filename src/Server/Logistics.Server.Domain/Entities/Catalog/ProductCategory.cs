using Logistics.Server.Domain.Common;

namespace Logistics.Server.Domain.Entities.Catalog;

public class ProductCategory : BaseEntity
{
    public long? ParentCategoryId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual ProductCategory? ParentCategory { get; set; }
    public virtual ICollection<ProductCategory> SubCategories { get; set; } = [];
    public virtual ICollection<Product> Products { get; set; } = [];
}
