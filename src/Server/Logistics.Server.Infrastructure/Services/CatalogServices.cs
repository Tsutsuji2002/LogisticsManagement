using System.Text.Json;
using Logistics.Server.Application.Common.Exceptions;
using Logistics.Server.Application.Common.Interfaces;
using Logistics.Server.Application.Common.Models;
using Logistics.Server.Application.Modules.Catalog.DTOs;
using Logistics.Server.Application.Modules.Catalog.Services;
using Logistics.Server.Domain.Entities.Catalog;
using Logistics.Server.Domain.Entities.Configuration;
using Logistics.Server.Domain.Entities.Inventory;
using Logistics.Server.Domain.Entities.Sales;
using Logistics.Server.Infrastructure.Persistence;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Logistics.Server.Infrastructure.Services;

public class CategoryService : ICategoryService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(
        ApplicationDbContext context,
        IAuditService auditService,
        ICurrentUserService currentUserService,
        ILogger<CategoryService> logger)
    {
        _context = context;
        _auditService = auditService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<PagedResult<CategoryDto>> GetCategoriesAsync(
        string? search,
        bool? isActive,
        long? parentCategoryId,
        PagedRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ProductCategories
            .AsNoTracking()
            .Include(c => c.ParentCategory)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(c =>
                c.Code.ToLower().Contains(term) ||
                c.Name.ToLower().Contains(term) ||
                (c.Description != null && c.Description.ToLower().Contains(term)));
        }

        if (isActive.HasValue)
        {
            query = query.Where(c => c.IsActive == isActive.Value);
        }

        if (parentCategoryId.HasValue)
        {
            query = query.Where(c => c.ParentCategoryId == parentCategoryId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(c => c.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                PublicId = c.PublicId,
                Code = c.Code,
                Name = c.Name,
                Description = c.Description,
                ParentCategoryId = c.ParentCategoryId,
                ParentCategoryName = c.ParentCategory != null ? c.ParentCategory.Name : null,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt,
                CreatedBy = c.CreatedBy,
                UpdatedAt = c.UpdatedAt,
                UpdatedBy = c.UpdatedBy
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<CategoryDto>(items, totalCount, request.Page, request.PageSize);
    }

    public async Task<CategoryDto?> GetByIdOrPublicIdAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var category = await FindCategoryAsync(identifier, cancellationToken);
        if (category == null) return null;

        return MapToDto(category);
    }

    public async Task<CategoryDetailDto?> GetDetailByIdOrPublicIdAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var category = await FindCategoryAsync(identifier, cancellationToken);
        if (category == null) return null;

        var subCategoryCount = await _context.ProductCategories
            .CountAsync(c => c.ParentCategoryId == category.Id, cancellationToken);

        var productCount = await _context.Products
            .CountAsync(p => p.CategoryId == category.Id, cancellationToken);

        var activeProductCount = await _context.Products
            .CountAsync(p => p.CategoryId == category.Id && p.IsActive, cancellationToken);

        return new CategoryDetailDto
        {
            Id = category.Id,
            PublicId = category.PublicId,
            Code = category.Code,
            Name = category.Name,
            Description = category.Description,
            ParentCategoryId = category.ParentCategoryId,
            ParentCategoryName = category.ParentCategory?.Name,
            IsActive = category.IsActive,
            CreatedAt = category.CreatedAt,
            CreatedBy = category.CreatedBy,
            UpdatedAt = category.UpdatedAt,
            UpdatedBy = category.UpdatedBy,
            SubCategoryCount = subCategoryCount,
            ProductCount = productCount,
            ActiveProductCount = activeProductCount
        };
    }

    public async Task<IReadOnlyList<CategoryTreeDto>> GetCategoryTreeAsync(bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        var query = _context.ProductCategories.AsNoTracking();
        if (activeOnly)
        {
            query = query.Where(c => c.IsActive);
        }

        var allCategories = await query
            .Select(c => new
            {
                c.Id,
                c.PublicId,
                c.Code,
                c.Name,
                c.Description,
                c.ParentCategoryId,
                c.IsActive,
                ProductCount = c.Products.Count
            })
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

        var lookup = allCategories.ToLookup(c => c.ParentCategoryId);

        List<CategoryTreeDto> BuildSubtree(long? parentId)
        {
            var nodes = new List<CategoryTreeDto>();
            foreach (var item in lookup[parentId])
            {
                nodes.Add(new CategoryTreeDto
                {
                    Id = item.Id,
                    PublicId = item.PublicId,
                    Code = item.Code,
                    Name = item.Name,
                    Description = item.Description,
                    ParentCategoryId = item.ParentCategoryId,
                    IsActive = item.IsActive,
                    ProductCount = item.ProductCount,
                    Children = BuildSubtree(item.Id)
                });
            }
            return nodes;
        }

        return BuildSubtree(null);
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedCode = request.Code.Trim().ToUpperInvariant();

        var codeExists = await _context.ProductCategories
            .AnyAsync(c => c.Code == normalizedCode, cancellationToken);

        if (codeExists)
        {
            throw new ConflictException($"A category with code '{normalizedCode}' already exists.");
        }

        if (request.ParentCategoryId.HasValue)
        {
            var parent = await _context.ProductCategories
                .FirstOrDefaultAsync(c => c.Id == request.ParentCategoryId.Value, cancellationToken);

            if (parent == null)
            {
                throw new NotFoundException(nameof(ProductCategory), request.ParentCategoryId.Value);
            }

            if (!parent.IsActive)
            {
                throw new InvalidOperationException($"Cannot assign inactive parent category '{parent.Name}'.");
            }
        }

        var category = new ProductCategory
        {
            Code = normalizedCode,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            ParentCategoryId = request.ParentCategoryId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUserService.Username ?? "system"
        };

        await _context.ProductCategories.AddAsync(category, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        if (category.ParentCategoryId.HasValue)
        {
            await _context.Entry(category).Reference(c => c.ParentCategory).LoadAsync(cancellationToken);
        }

        var dto = MapToDto(category);

        await _auditService.RecordAsync(
            "CREATE",
            "catalog",
            "ProductCategories",
            category.Id.ToString(),
            null,
            dto,
            $"Created product category {category.Code}",
            cancellationToken);

        _logger.LogInformation("Product category '{Code}' ({Id}) created successfully.", category.Code, category.Id);
        return dto;
    }

    public async Task<CategoryDto> UpdateAsync(string identifier, UpdateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = await FindCategoryAsync(identifier, cancellationToken);
        if (category == null)
        {
            throw new NotFoundException(nameof(ProductCategory), identifier);
        }

        if (request.ParentCategoryId.HasValue)
        {
            if (request.ParentCategoryId.Value == category.Id)
            {
                throw new InvalidOperationException("A category cannot be its own parent.");
            }

            var parent = await _context.ProductCategories
                .FirstOrDefaultAsync(c => c.Id == request.ParentCategoryId.Value, cancellationToken);

            if (parent == null)
            {
                throw new NotFoundException(nameof(ProductCategory), request.ParentCategoryId.Value);
            }

            if (await IsDescendantAsync(category.Id, request.ParentCategoryId.Value, cancellationToken))
            {
                throw new InvalidOperationException("Circular category relationship detected: parent category cannot be a descendant of this category.");
            }
        }

        var oldSnapshot = MapToDto(category);

        category.Name = request.Name.Trim();
        category.Description = request.Description?.Trim();
        category.ParentCategoryId = request.ParentCategoryId;
        category.UpdatedAt = DateTime.UtcNow;
        category.UpdatedBy = _currentUserService.Username ?? "system";

        await _context.SaveChangesAsync(cancellationToken);

        if (category.ParentCategoryId.HasValue)
        {
            await _context.Entry(category).Reference(c => c.ParentCategory).LoadAsync(cancellationToken);
        }
        else
        {
            category.ParentCategory = null;
        }

        var newSnapshot = MapToDto(category);

        await _auditService.RecordAsync(
            "UPDATE",
            "catalog",
            "ProductCategories",
            category.Id.ToString(),
            oldSnapshot,
            newSnapshot,
            $"Updated product category {category.Code}",
            cancellationToken);

        _logger.LogInformation("Product category '{Code}' ({Id}) updated.", category.Code, category.Id);
        return newSnapshot;
    }

    public async Task UpdateStatusAsync(string identifier, UpdateCategoryStatusRequest request, CancellationToken cancellationToken = default)
    {
        var category = await FindCategoryAsync(identifier, cancellationToken);
        if (category == null)
        {
            throw new NotFoundException(nameof(ProductCategory), identifier);
        }

        if (!request.IsActive)
        {
            var allSubCategoryIds = await GetAllSubcategoryIdsAsync(category.Id, cancellationToken);
            allSubCategoryIds.Add(category.Id);

            var activeProductsCount = await _context.Products
                .CountAsync(p => allSubCategoryIds.Contains(p.CategoryId) && p.IsActive, cancellationToken);

            if (activeProductsCount > 0)
            {
                throw new InvalidOperationException($"Cannot deactivate category '{category.Name}' because it or its subcategories contain {activeProductsCount} active product(s). Deactivate products first.");
            }
        }

        var oldSnapshot = MapToDto(category);

        category.IsActive = request.IsActive;
        category.UpdatedAt = DateTime.UtcNow;
        category.UpdatedBy = _currentUserService.Username ?? "system";

        await _context.SaveChangesAsync(cancellationToken);

        var newSnapshot = MapToDto(category);

        await _auditService.RecordAsync(
            "UPDATE_STATUS",
            "catalog",
            "ProductCategories",
            category.Id.ToString(),
            oldSnapshot,
            newSnapshot,
            $"Changed status to {(request.IsActive ? "Active" : "Inactive")}. Reason: {request.Reason}",
            cancellationToken);

        _logger.LogInformation("Product category '{Code}' status updated to {IsActive}.", category.Code, category.IsActive);
    }

    public async Task DeleteAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var category = await FindCategoryAsync(identifier, cancellationToken);
        if (category == null)
        {
            throw new NotFoundException(nameof(ProductCategory), identifier);
        }

        var hasProducts = await _context.Products.AnyAsync(p => p.CategoryId == category.Id, cancellationToken);
        if (hasProducts)
        {
            throw new InvalidOperationException($"Cannot delete category '{category.Name}' because it is referenced by existing products. Deactivate it instead.");
        }

        var hasSubCategories = await _context.ProductCategories.AnyAsync(c => c.ParentCategoryId == category.Id, cancellationToken);
        if (hasSubCategories)
        {
            throw new InvalidOperationException($"Cannot delete category '{category.Name}' because subcategories exist under it. Reassign or remove subcategories first.");
        }

        var oldSnapshot = MapToDto(category);

        _context.ProductCategories.Remove(category);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            "DELETE",
            "catalog",
            "ProductCategories",
            category.Id.ToString(),
            oldSnapshot,
            null,
            $"Deleted category {category.Code}",
            cancellationToken);

        _logger.LogInformation("Product category '{Code}' ({Id}) deleted.", category.Code, category.Id);
    }

    private async Task<ProductCategory?> FindCategoryAsync(string identifier, CancellationToken cancellationToken)
    {
        if (long.TryParse(identifier, out var id))
        {
            return await _context.ProductCategories
                .Include(c => c.ParentCategory)
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }

        if (Guid.TryParse(identifier, out var publicId))
        {
            return await _context.ProductCategories
                .Include(c => c.ParentCategory)
                .FirstOrDefaultAsync(c => c.PublicId == publicId, cancellationToken);
        }

        var normalizedCode = identifier.Trim().ToUpperInvariant();
        return await _context.ProductCategories
            .Include(c => c.ParentCategory)
            .FirstOrDefaultAsync(c => c.Code == normalizedCode, cancellationToken);
    }

    private async Task<bool> IsDescendantAsync(long targetCategoryId, long candidateParentId, CancellationToken cancellationToken)
    {
        long? currentId = candidateParentId;
        var visited = new HashSet<long>();

        while (currentId.HasValue)
        {
            if (currentId.Value == targetCategoryId) return true;
            if (!visited.Add(currentId.Value)) break;

            currentId = await _context.ProductCategories
                .Where(c => c.Id == currentId.Value)
                .Select(c => c.ParentCategoryId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return false;
    }

    private async Task<List<long>> GetAllSubcategoryIdsAsync(long rootCategoryId, CancellationToken cancellationToken)
    {
        var result = new List<long>();
        var queue = new Queue<long>();
        queue.Enqueue(rootCategoryId);

        while (queue.Count > 0)
        {
            var parentId = queue.Dequeue();
            var childIds = await _context.ProductCategories
                .Where(c => c.ParentCategoryId == parentId)
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);

            foreach (var childId in childIds)
            {
                result.Add(childId);
                queue.Enqueue(childId);
            }
        }

        return result;
    }

    private static CategoryDto MapToDto(ProductCategory category) => new()
    {
        Id = category.Id,
        PublicId = category.PublicId,
        Code = category.Code,
        Name = category.Name,
        Description = category.Description,
        ParentCategoryId = category.ParentCategoryId,
        ParentCategoryName = category.ParentCategory?.Name,
        IsActive = category.IsActive,
        CreatedAt = category.CreatedAt,
        CreatedBy = category.CreatedBy,
        UpdatedAt = category.UpdatedAt,
        UpdatedBy = category.UpdatedBy
    };
}

public class ProductService : IProductService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ProductService> _logger;

    public ProductService(
        ApplicationDbContext context,
        IAuditService auditService,
        ICurrentUserService currentUserService,
        ILogger<ProductService> logger)
    {
        _context = context;
        _auditService = auditService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<PagedResult<ProductDto>> GetProductsAsync(
        string? search,
        long? categoryId,
        bool? isActive,
        long? unitOfMeasureId,
        bool? isDangerous,
        bool? isPerishable,
        decimal? minWeight,
        decimal? maxWeight,
        PagedRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.UnitOfMeasure)
            .Include(p => p.Currency)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p =>
                p.Sku.ToLower().Contains(term) ||
                (p.Barcode != null && p.Barcode.ToLower().Contains(term)) ||
                p.Name.ToLower().Contains(term) ||
                (p.Description != null && p.Description.ToLower().Contains(term)));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(p => p.IsActive == isActive.Value);
        }

        if (unitOfMeasureId.HasValue)
        {
            query = query.Where(p => p.UnitOfMeasureId == unitOfMeasureId.Value);
        }

        if (isDangerous.HasValue)
        {
            query = query.Where(p => p.IsDangerous == isDangerous.Value);
        }

        if (isPerishable.HasValue)
        {
            query = query.Where(p => p.IsPerishable == isPerishable.Value);
        }

        if (minWeight.HasValue)
        {
            query = query.Where(p => p.WeightKg >= minWeight.Value);
        }

        if (maxWeight.HasValue)
        {
            query = query.Where(p => p.WeightKg <= maxWeight.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(p => p.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                PublicId = p.PublicId,
                CategoryId = p.CategoryId,
                CategoryCode = p.Category.Code,
                CategoryName = p.Category.Name,
                Sku = p.Sku,
                Barcode = p.Barcode,
                Name = p.Name,
                Description = p.Description,
                UnitOfMeasureId = p.UnitOfMeasureId,
                UnitOfMeasureCode = p.UnitOfMeasure.Code,
                UnitOfMeasureName = p.UnitOfMeasure.Name,
                WeightKg = p.WeightKg,
                LengthCm = p.LengthCm,
                WidthCm = p.WidthCm,
                HeightCm = p.HeightCm,
                VolumeCbm = (p.LengthCm * p.WidthCm * p.HeightCm) / 1000000m,
                DeclaredValue = p.DeclaredValue,
                CurrencyId = p.CurrencyId,
                CurrencyCode = p.Currency.Code,
                IsPerishable = p.IsPerishable,
                IsDangerous = p.IsDangerous,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                CreatedBy = p.CreatedBy,
                UpdatedAt = p.UpdatedAt,
                UpdatedBy = p.UpdatedBy
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductDto>(items, totalCount, request.Page, request.PageSize);
    }

    public async Task<ProductDto?> GetByIdOrPublicIdAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var product = await FindProductAsync(identifier, cancellationToken);
        if (product == null) return null;

        return MapToDto(product);
    }

    public async Task<ProductDetailDto?> GetDetailByIdOrPublicIdAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var product = await FindProductAsync(identifier, cancellationToken);
        if (product == null) return null;

        string? parentCategoryName = null;
        if (product.Category.ParentCategoryId.HasValue)
        {
            parentCategoryName = await _context.ProductCategories
                .Where(c => c.Id == product.Category.ParentCategoryId.Value)
                .Select(c => c.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new ProductDetailDto
        {
            Id = product.Id,
            PublicId = product.PublicId,
            CategoryId = product.CategoryId,
            CategoryCode = product.Category.Code,
            CategoryName = product.Category.Name,
            Sku = product.Sku,
            Barcode = product.Barcode,
            Name = product.Name,
            Description = product.Description,
            UnitOfMeasureId = product.UnitOfMeasureId,
            UnitOfMeasureCode = product.UnitOfMeasure.Code,
            UnitOfMeasureName = product.UnitOfMeasure.Name,
            WeightKg = product.WeightKg,
            LengthCm = product.LengthCm,
            WidthCm = product.WidthCm,
            HeightCm = product.HeightCm,
            VolumeCbm = (product.LengthCm * product.WidthCm * product.HeightCm) / 1000000m,
            DeclaredValue = product.DeclaredValue,
            CurrencyId = product.CurrencyId,
            CurrencyCode = product.Currency.Code,
            IsPerishable = product.IsPerishable,
            IsDangerous = product.IsDangerous,
            IsActive = product.IsActive,
            CreatedAt = product.CreatedAt,
            CreatedBy = product.CreatedBy,
            UpdatedAt = product.UpdatedAt,
            UpdatedBy = product.UpdatedBy,
            ParentCategoryName = parentCategoryName,
            UnitType = product.UnitOfMeasure.Type,
            CurrencySymbol = product.Currency.Symbol
        };
    }

    public async Task<ProductDto?> GetByCodeOrBarcodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalized = code.Trim();
        var product = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.UnitOfMeasure)
            .Include(p => p.Currency)
            .FirstOrDefaultAsync(p => p.Sku == normalized || p.Barcode == normalized, cancellationToken);

        if (product == null) return null;
        return MapToDto(product);
    }

    public async Task<IReadOnlyList<ProductSearchResultDto>> SearchAsync(string query, int maxResults = 20, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<ProductSearchResultDto>();
        }

        var term = query.Trim().ToLower();

        return await _context.Products
            .AsNoTracking()
            .Where(p =>
                p.Sku.ToLower().Contains(term) ||
                (p.Barcode != null && p.Barcode.ToLower().Contains(term)) ||
                p.Name.ToLower().Contains(term))
            .OrderBy(p => p.Name)
            .Take(maxResults)
            .Select(p => new ProductSearchResultDto
            {
                Id = p.Id,
                PublicId = p.PublicId,
                Sku = p.Sku,
                Barcode = p.Barcode,
                Name = p.Name,
                CategoryName = p.Category.Name,
                UnitOfMeasureCode = p.UnitOfMeasure.Code,
                WeightKg = p.WeightKg,
                VolumeCbm = (p.LengthCm * p.WidthCm * p.HeightCm) / 1000000m,
                DeclaredValue = p.DeclaredValue,
                CurrencyCode = p.Currency.Code,
                IsPerishable = p.IsPerishable,
                IsDangerous = p.IsDangerous,
                IsActive = p.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedSku = request.Sku.Trim().ToUpperInvariant();

        var skuExists = await _context.Products
            .AnyAsync(p => p.Sku == normalizedSku, cancellationToken);

        if (skuExists)
        {
            throw new ConflictException($"A product with SKU '{normalizedSku}' already exists.");
        }

        var category = await _context.ProductCategories
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken);

        if (category == null)
        {
            throw new NotFoundException(nameof(ProductCategory), request.CategoryId);
        }

        if (!category.IsActive)
        {
            throw new InvalidOperationException($"Cannot create product under inactive category '{category.Name}'.");
        }

        var uom = await _context.UnitsOfMeasure
            .FirstOrDefaultAsync(u => u.Id == request.UnitOfMeasureId, cancellationToken);

        if (uom == null)
        {
            throw new NotFoundException(nameof(UnitOfMeasure), request.UnitOfMeasureId);
        }

        if (!uom.IsActive)
        {
            throw new InvalidOperationException($"Cannot assign inactive unit of measure '{uom.Name}'.");
        }

        var currencyId = await ResolveCurrencyIdAsync(request.CurrencyId, cancellationToken);

        var product = new Product
        {
            CategoryId = category.Id,
            Sku = normalizedSku,
            Barcode = request.Barcode?.Trim(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            UnitOfMeasureId = uom.Id,
            WeightKg = request.WeightKg,
            LengthCm = request.LengthCm,
            WidthCm = request.WidthCm,
            HeightCm = request.HeightCm,
            DeclaredValue = request.DeclaredValue,
            CurrencyId = currencyId,
            IsPerishable = request.IsPerishable,
            IsDangerous = request.IsDangerous,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUserService.Username ?? "system"
        };

        await _context.Products.AddAsync(product, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await _context.Entry(product).Reference(p => p.Category).LoadAsync(cancellationToken);
        await _context.Entry(product).Reference(p => p.UnitOfMeasure).LoadAsync(cancellationToken);
        await _context.Entry(product).Reference(p => p.Currency).LoadAsync(cancellationToken);

        var dto = MapToDto(product);

        await _auditService.RecordAsync(
            "CREATE",
            "catalog",
            "Products",
            product.Id.ToString(),
            null,
            dto,
            $"Created product {product.Sku}",
            cancellationToken);

        _logger.LogInformation("Product '{Sku}' ({Id}) created successfully.", product.Sku, product.Id);
        return dto;
    }

    public async Task<ProductDto> UpdateAsync(string identifier, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = await FindProductAsync(identifier, cancellationToken);
        if (product == null)
        {
            throw new NotFoundException(nameof(Product), identifier);
        }

        var category = await _context.ProductCategories
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken);

        if (category == null)
        {
            throw new NotFoundException(nameof(ProductCategory), request.CategoryId);
        }

        if (!category.IsActive && product.CategoryId != category.Id)
        {
            throw new InvalidOperationException($"Cannot move product to inactive category '{category.Name}'.");
        }

        var uom = await _context.UnitsOfMeasure
            .FirstOrDefaultAsync(u => u.Id == request.UnitOfMeasureId, cancellationToken);

        if (uom == null)
        {
            throw new NotFoundException(nameof(UnitOfMeasure), request.UnitOfMeasureId);
        }

        if (!uom.IsActive && product.UnitOfMeasureId != uom.Id)
        {
            throw new InvalidOperationException($"Cannot switch to inactive unit of measure '{uom.Name}'.");
        }

        var currencyId = await ResolveCurrencyIdAsync(request.CurrencyId, cancellationToken);

        var oldSnapshot = MapToDto(product);

        product.CategoryId = category.Id;
        product.Barcode = request.Barcode?.Trim();
        product.Name = request.Name.Trim();
        product.Description = request.Description?.Trim();
        product.UnitOfMeasureId = uom.Id;
        product.WeightKg = request.WeightKg;
        product.LengthCm = request.LengthCm;
        product.WidthCm = request.WidthCm;
        product.HeightCm = request.HeightCm;
        product.DeclaredValue = request.DeclaredValue;
        product.CurrencyId = currencyId;
        product.IsPerishable = request.IsPerishable;
        product.IsDangerous = request.IsDangerous;
        product.UpdatedAt = DateTime.UtcNow;
        product.UpdatedBy = _currentUserService.Username ?? "system";

        await _context.SaveChangesAsync(cancellationToken);

        await _context.Entry(product).Reference(p => p.Category).LoadAsync(cancellationToken);
        await _context.Entry(product).Reference(p => p.UnitOfMeasure).LoadAsync(cancellationToken);
        await _context.Entry(product).Reference(p => p.Currency).LoadAsync(cancellationToken);

        var newSnapshot = MapToDto(product);

        await _auditService.RecordAsync(
            "UPDATE",
            "catalog",
            "Products",
            product.Id.ToString(),
            oldSnapshot,
            newSnapshot,
            $"Updated product {product.Sku}",
            cancellationToken);

        _logger.LogInformation("Product '{Sku}' ({Id}) updated.", product.Sku, product.Id);
        return newSnapshot;
    }

    public async Task UpdateStatusAsync(string identifier, UpdateProductStatusRequest request, CancellationToken cancellationToken = default)
    {
        var product = await FindProductAsync(identifier, cancellationToken);
        if (product == null)
        {
            throw new NotFoundException(nameof(Product), identifier);
        }

        if (request.IsActive && !product.Category.IsActive)
        {
            throw new InvalidOperationException($"Cannot activate product '{product.Name}' while its category '{product.Category.Name}' is inactive.");
        }

        var oldSnapshot = MapToDto(product);

        product.IsActive = request.IsActive;
        product.UpdatedAt = DateTime.UtcNow;
        product.UpdatedBy = _currentUserService.Username ?? "system";

        await _context.SaveChangesAsync(cancellationToken);

        var newSnapshot = MapToDto(product);

        await _auditService.RecordAsync(
            "UPDATE_STATUS",
            "catalog",
            "Products",
            product.Id.ToString(),
            oldSnapshot,
            newSnapshot,
            $"Changed status to {(request.IsActive ? "Active" : "Inactive")}. Reason: {request.Reason}",
            cancellationToken);

        _logger.LogInformation("Product '{Sku}' status updated to {IsActive}.", product.Sku, product.IsActive);
    }

    public async Task DeleteAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var product = await FindProductAsync(identifier, cancellationToken);
        if (product == null)
        {
            throw new NotFoundException(nameof(Product), identifier);
        }

        var hasOrderItems = await _context.Set<OrderItem>()
            .AnyAsync(oi => oi.ProductId == product.Id, cancellationToken);

        if (hasOrderItems)
        {
            throw new InvalidOperationException($"Cannot delete product '{product.Sku}' because it is referenced in historical sales orders. Deactivate it instead.");
        }

        var hasInventoryBalances = await _context.Set<InventoryBalance>()
            .AnyAsync(s => s.ProductId == product.Id, cancellationToken);

        if (hasInventoryBalances)
        {
            throw new InvalidOperationException($"Cannot delete product '{product.Sku}' because it has active inventory stock balances. Deactivate it instead.");
        }

        var oldSnapshot = MapToDto(product);

        _context.Products.Remove(product);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            "DELETE",
            "catalog",
            "Products",
            product.Id.ToString(),
            oldSnapshot,
            null,
            $"Deleted product {product.Sku}",
            cancellationToken);

        _logger.LogInformation("Product '{Sku}' ({Id}) deleted.", product.Sku, product.Id);
    }

    private async Task<Product?> FindProductAsync(string identifier, CancellationToken cancellationToken)
    {
        if (long.TryParse(identifier, out var id))
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.UnitOfMeasure)
                .Include(p => p.Currency)
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        if (Guid.TryParse(identifier, out var publicId))
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.UnitOfMeasure)
                .Include(p => p.Currency)
                .FirstOrDefaultAsync(p => p.PublicId == publicId, cancellationToken);
        }

        var normalizedSku = identifier.Trim().ToUpperInvariant();
        return await _context.Products
            .Include(p => p.Category)
            .Include(p => p.UnitOfMeasure)
            .Include(p => p.Currency)
            .FirstOrDefaultAsync(p => p.Sku == normalizedSku, cancellationToken);
    }

    private async Task<long> ResolveCurrencyIdAsync(long? requestedCurrencyId, CancellationToken cancellationToken)
    {
        if (requestedCurrencyId.HasValue && requestedCurrencyId.Value > 0)
        {
            var exists = await _context.Currencies.AnyAsync(c => c.Id == requestedCurrencyId.Value && c.IsActive, cancellationToken);
            if (exists) return requestedCurrencyId.Value;
            throw new NotFoundException(nameof(Currency), requestedCurrencyId.Value);
        }

        var defaultCurrency = await _context.Currencies
            .Where(c => c.IsActive)
            .OrderByDescending(c => c.IsDefault)
            .FirstOrDefaultAsync(cancellationToken);

        if (defaultCurrency != null)
        {
            return defaultCurrency.Id;
        }

        var vnd = new Currency
        {
            Code = "VND",
            Name = "Vietnamese Dong",
            Symbol = "₫",
            DecimalPlaces = 0,
            ExchangeRateToDefault = 1.0m,
            IsDefault = true,
            IsActive = true
        };
        await _context.Currencies.AddAsync(vnd, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return vnd.Id;
    }

    private static ProductDto MapToDto(Product product) => new()
    {
        Id = product.Id,
        PublicId = product.PublicId,
        CategoryId = product.CategoryId,
        CategoryCode = product.Category.Code,
        CategoryName = product.Category.Name,
        Sku = product.Sku,
        Barcode = product.Barcode,
        Name = product.Name,
        Description = product.Description,
        UnitOfMeasureId = product.UnitOfMeasureId,
        UnitOfMeasureCode = product.UnitOfMeasure.Code,
        UnitOfMeasureName = product.UnitOfMeasure.Name,
        WeightKg = product.WeightKg,
        LengthCm = product.LengthCm,
        WidthCm = product.WidthCm,
        HeightCm = product.HeightCm,
        VolumeCbm = (product.LengthCm * product.WidthCm * product.HeightCm) / 1000000m,
        DeclaredValue = product.DeclaredValue,
        CurrencyId = product.CurrencyId,
        CurrencyCode = product.Currency.Code,
        IsPerishable = product.IsPerishable,
        IsDangerous = product.IsDangerous,
        IsActive = product.IsActive,
        CreatedAt = product.CreatedAt,
        CreatedBy = product.CreatedBy,
        UpdatedAt = product.UpdatedAt,
        UpdatedBy = product.UpdatedBy
    };
}

public class UnitOfMeasureService : IUnitOfMeasureService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ILogger<UnitOfMeasureService> _logger;

    public UnitOfMeasureService(
        ApplicationDbContext context,
        IAuditService auditService,
        ILogger<UnitOfMeasureService> logger)
    {
        _context = context;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<UnitOfMeasureDto>> GetUnitsAsync(string? search, string? type, bool? isActive, CancellationToken cancellationToken = default)
    {
        var query = _context.UnitsOfMeasure.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(u => u.Code.ToLower().Contains(term) || u.Name.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            var typeTerm = type.Trim();
            query = query.Where(u => u.Type.ToLower() == typeTerm.ToLower());
        }

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        return await query
            .OrderBy(u => u.Type)
            .ThenBy(u => u.Code)
            .Select(u => new UnitOfMeasureDto
            {
                Id = u.Id,
                Code = u.Code,
                Name = u.Name,
                Type = u.Type,
                ConversionFactor = u.ConversionFactor,
                IsBaseUnit = u.IsBaseUnit,
                IsActive = u.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<UnitOfMeasureDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var u = await _context.UnitsOfMeasure.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (u == null) return null;

        return MapToDto(u);
    }

    public async Task<UnitOfMeasureDto?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalized = code.Trim().ToUpperInvariant();
        var u = await _context.UnitsOfMeasure.AsNoTracking().FirstOrDefaultAsync(x => x.Code == normalized, cancellationToken);
        if (u == null) return null;

        return MapToDto(u);
    }

    public async Task<IReadOnlyList<UnitOfMeasureDto>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<UnitOfMeasureDto>();
        }

        var term = query.Trim().ToLower();
        return await _context.UnitsOfMeasure
            .AsNoTracking()
            .Where(u => u.Code.ToLower().Contains(term) || u.Name.ToLower().Contains(term))
            .OrderBy(u => u.Code)
            .Select(u => new UnitOfMeasureDto
            {
                Id = u.Id,
                Code = u.Code,
                Name = u.Name,
                Type = u.Type,
                ConversionFactor = u.ConversionFactor,
                IsBaseUnit = u.IsBaseUnit,
                IsActive = u.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<UnitOfMeasureDto> CreateAsync(CreateUnitOfMeasureRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedCode = request.Code.Trim().ToUpperInvariant();

        var exists = await _context.UnitsOfMeasure.AnyAsync(u => u.Code == normalizedCode, cancellationToken);
        if (exists)
        {
            throw new ConflictException($"A unit of measure with code '{normalizedCode}' already exists.");
        }

        var uom = new UnitOfMeasure
        {
            Code = normalizedCode,
            Name = request.Name.Trim(),
            Type = request.Type.Trim(),
            ConversionFactor = request.ConversionFactor,
            IsBaseUnit = request.IsBaseUnit,
            IsActive = true
        };

        await _context.UnitsOfMeasure.AddAsync(uom, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var dto = MapToDto(uom);

        await _auditService.RecordAsync(
            "CREATE",
            "configuration",
            "UnitsOfMeasure",
            uom.Id.ToString(),
            null,
            dto,
            $"Created unit of measure {uom.Code}",
            cancellationToken);

        _logger.LogInformation("Unit of measure '{Code}' ({Id}) created.", uom.Code, uom.Id);
        return dto;
    }

    public async Task<UnitOfMeasureDto> UpdateAsync(long id, UpdateUnitOfMeasureRequest request, CancellationToken cancellationToken = default)
    {
        var uom = await _context.UnitsOfMeasure.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (uom == null)
        {
            throw new NotFoundException(nameof(UnitOfMeasure), id);
        }

        var oldSnapshot = MapToDto(uom);

        uom.Name = request.Name.Trim();
        uom.Type = request.Type.Trim();
        uom.ConversionFactor = request.ConversionFactor;
        uom.IsBaseUnit = request.IsBaseUnit;

        await _context.SaveChangesAsync(cancellationToken);

        var newSnapshot = MapToDto(uom);

        await _auditService.RecordAsync(
            "UPDATE",
            "configuration",
            "UnitsOfMeasure",
            uom.Id.ToString(),
            oldSnapshot,
            newSnapshot,
            $"Updated unit of measure {uom.Code}",
            cancellationToken);

        _logger.LogInformation("Unit of measure '{Code}' ({Id}) updated.", uom.Code, uom.Id);
        return newSnapshot;
    }

    public async Task UpdateStatusAsync(long id, UpdateUnitStatusRequest request, CancellationToken cancellationToken = default)
    {
        var uom = await _context.UnitsOfMeasure.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (uom == null)
        {
            throw new NotFoundException(nameof(UnitOfMeasure), id);
        }

        if (!request.IsActive)
        {
            var activeProductCount = await _context.Products
                .CountAsync(p => p.UnitOfMeasureId == id && p.IsActive, cancellationToken);

            if (activeProductCount > 0)
            {
                throw new InvalidOperationException($"Cannot deactivate unit of measure '{uom.Code}' because it is in use by {activeProductCount} active product(s).");
            }
        }

        var oldSnapshot = MapToDto(uom);

        uom.IsActive = request.IsActive;
        await _context.SaveChangesAsync(cancellationToken);

        var newSnapshot = MapToDto(uom);

        await _auditService.RecordAsync(
            "UPDATE_STATUS",
            "configuration",
            "UnitsOfMeasure",
            uom.Id.ToString(),
            oldSnapshot,
            newSnapshot,
            $"Changed status to {(request.IsActive ? "Active" : "Inactive")}. Reason: {request.Reason}",
            cancellationToken);

        _logger.LogInformation("Unit of measure '{Code}' status updated to {IsActive}.", uom.Code, uom.IsActive);
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var uom = await _context.UnitsOfMeasure.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (uom == null)
        {
            throw new NotFoundException(nameof(UnitOfMeasure), id);
        }

        var isReferencedByProducts = await _context.Products.AnyAsync(p => p.UnitOfMeasureId == id, cancellationToken);
        if (isReferencedByProducts)
        {
            throw new InvalidOperationException($"Cannot delete unit of measure '{uom.Code}' because it is referenced by existing products. Deactivate it instead.");
        }

        var oldSnapshot = MapToDto(uom);

        _context.UnitsOfMeasure.Remove(uom);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            "DELETE",
            "configuration",
            "UnitsOfMeasure",
            uom.Id.ToString(),
            oldSnapshot,
            null,
            $"Deleted unit of measure {uom.Code}",
            cancellationToken);

        _logger.LogInformation("Unit of measure '{Code}' ({Id}) deleted.", uom.Code, uom.Id);
    }

    private static UnitOfMeasureDto MapToDto(UnitOfMeasure u) => new()
    {
        Id = u.Id,
        Code = u.Code,
        Name = u.Name,
        Type = u.Type,
        ConversionFactor = u.ConversionFactor,
        IsBaseUnit = u.IsBaseUnit,
        IsActive = u.IsActive
    };
}

public class LogisticsServiceCatalogService : ILogisticsServiceCatalogService
{
    private readonly ApplicationDbContext _context;

    private static readonly List<ServiceDto> StandardServices =
    [
        new()
        {
            ServiceCode = "SAMEDAY",
            ServiceName = "Same-Day Delivery",
            Description = "Guaranteed same-day express delivery within designated metropolitan areas.",
            ServiceCategory = "Express",
            EstimatedTransitHours = 6,
            MaxWeightKg = 15.0m,
            MaxLengthCm = 60.0m,
            IsCodSupported = true,
            IsInsuranceSupported = true,
            IsActive = true,
            ProviderType = "Internal"
        },
        new()
        {
            ServiceCode = "NEXTDAY",
            ServiceName = "Next-Day Express",
            Description = "Priority delivery guaranteed by the next business day across provincial capitals.",
            ServiceCategory = "Express",
            EstimatedTransitHours = 24,
            MaxWeightKg = 30.0m,
            MaxLengthCm = 100.0m,
            IsCodSupported = true,
            IsInsuranceSupported = true,
            IsActive = true,
            ProviderType = "Internal"
        },
        new()
        {
            ServiceCode = "STANDARD",
            ServiceName = "Standard Ground Freight",
            Description = "Cost-effective nationwide door-to-door ground shipping with scheduled dispatch.",
            ServiceCategory = "Standard",
            EstimatedTransitHours = 72,
            MaxWeightKg = 500.0m,
            MaxLengthCm = 300.0m,
            IsCodSupported = true,
            IsInsuranceSupported = true,
            IsActive = true,
            ProviderType = "Internal"
        },
        new()
        {
            ServiceCode = "ECONOMICAL",
            ServiceName = "Economical Bulk Cargo",
            Description = "Budget-friendly line-haul transportation for heavy or palletized non-urgent consignments.",
            ServiceCategory = "Economy",
            EstimatedTransitHours = 120,
            MaxWeightKg = 2000.0m,
            MaxLengthCm = 600.0m,
            IsCodSupported = false,
            IsInsuranceSupported = true,
            IsActive = true,
            ProviderType = "Internal"
        }
    ];

    public LogisticsServiceCatalogService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ServiceDto>> GetServicesAsync(bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        var services = new List<ServiceDto>();

        if (activeOnly)
        {
            services.AddRange(StandardServices.Where(s => s.IsActive));
        }
        else
        {
            services.AddRange(StandardServices);
        }

        var carrierServicesQuery = _context.CarrierServices
            .AsNoTracking()
            .Include(cs => cs.Carrier)
            .AsQueryable();

        if (activeOnly)
        {
            carrierServicesQuery = carrierServicesQuery.Where(cs => cs.IsActive && cs.Carrier.IsActive);
        }

        var carrierServices = await carrierServicesQuery
            .Select(cs => new ServiceDto
            {
                ServiceCode = cs.ServiceCode,
                ServiceName = $"{cs.Carrier.Name} - {cs.ServiceName}",
                Description = cs.Description,
                ServiceCategory = "ExternalCarrier",
                EstimatedTransitHours = cs.EstimatedTransitHours,
                MaxWeightKg = cs.MaxWeightKg,
                MaxLengthCm = cs.MaxLengthCm,
                IsCodSupported = cs.IsCodSupported,
                IsInsuranceSupported = cs.IsInsuranceSupported,
                IsActive = cs.IsActive,
                ProviderType = "Carrier"
            })
            .ToListAsync(cancellationToken);

        services.AddRange(carrierServices);
        return services;
    }

    public async Task<ServiceDto?> GetByCodeAsync(string serviceCode, CancellationToken cancellationToken = default)
    {
        var normalized = serviceCode.Trim().ToUpperInvariant();

        var internalService = StandardServices.FirstOrDefault(s => s.ServiceCode.Equals(normalized, StringComparison.OrdinalIgnoreCase));
        if (internalService != null) return internalService;

        var carrierService = await _context.CarrierServices
            .AsNoTracking()
            .Include(cs => cs.Carrier)
            .FirstOrDefaultAsync(cs => cs.ServiceCode == normalized, cancellationToken);

        if (carrierService == null) return null;

        return new ServiceDto
        {
            ServiceCode = carrierService.ServiceCode,
            ServiceName = $"{carrierService.Carrier.Name} - {carrierService.ServiceName}",
            Description = carrierService.Description,
            ServiceCategory = "ExternalCarrier",
            EstimatedTransitHours = carrierService.EstimatedTransitHours,
            MaxWeightKg = carrierService.MaxWeightKg,
            MaxLengthCm = carrierService.MaxLengthCm,
            IsCodSupported = carrierService.IsCodSupported,
            IsInsuranceSupported = carrierService.IsInsuranceSupported,
            IsActive = carrierService.IsActive,
            ProviderType = "Carrier"
        };
    }
}

public class CatalogReadService : ICatalogReadService
{
    private readonly ApplicationDbContext _context;
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;
    private readonly IUnitOfMeasureService _unitOfMeasureService;
    private readonly ILogisticsServiceCatalogService _serviceCatalogService;

    public CatalogReadService(
        ApplicationDbContext context,
        IProductService productService,
        ICategoryService categoryService,
        IUnitOfMeasureService unitOfMeasureService,
        ILogisticsServiceCatalogService serviceCatalogService)
    {
        _context = context;
        _productService = productService;
        _categoryService = categoryService;
        _unitOfMeasureService = unitOfMeasureService;
        _serviceCatalogService = serviceCatalogService;
    }

    public async Task<ProductDto?> GetProductByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _productService.GetByIdOrPublicIdAsync(id.ToString(), cancellationToken);
    }

    public async Task<ProductDto?> GetProductBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        return await _productService.GetByCodeOrBarcodeAsync(sku, cancellationToken);
    }

    public async Task<ProductDto?> GetProductByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        return await _productService.GetByCodeOrBarcodeAsync(barcode, cancellationToken);
    }

    public async Task<IReadOnlyList<ProductDto>> GetProductsByIdsAsync(IEnumerable<long> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0) return Array.Empty<ProductDto>();

        return await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.UnitOfMeasure)
            .Include(p => p.Currency)
            .Where(p => idList.Contains(p.Id))
            .Select(p => new ProductDto
            {
                Id = p.Id,
                PublicId = p.PublicId,
                CategoryId = p.CategoryId,
                CategoryCode = p.Category.Code,
                CategoryName = p.Category.Name,
                Sku = p.Sku,
                Barcode = p.Barcode,
                Name = p.Name,
                Description = p.Description,
                UnitOfMeasureId = p.UnitOfMeasureId,
                UnitOfMeasureCode = p.UnitOfMeasure.Code,
                UnitOfMeasureName = p.UnitOfMeasure.Name,
                WeightKg = p.WeightKg,
                LengthCm = p.LengthCm,
                WidthCm = p.WidthCm,
                HeightCm = p.HeightCm,
                VolumeCbm = (p.LengthCm * p.WidthCm * p.HeightCm) / 1000000m,
                DeclaredValue = p.DeclaredValue,
                CurrencyId = p.CurrencyId,
                CurrencyCode = p.Currency.Code,
                IsPerishable = p.IsPerishable,
                IsDangerous = p.IsDangerous,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                CreatedBy = p.CreatedBy,
                UpdatedAt = p.UpdatedAt,
                UpdatedBy = p.UpdatedBy
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductDto>> GetActiveProductsAsync(long? categoryId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.UnitOfMeasure)
            .Include(p => p.Currency)
            .Where(p => p.IsActive && p.Category.IsActive && p.UnitOfMeasure.IsActive);

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        return await query
            .OrderBy(p => p.Name)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                PublicId = p.PublicId,
                CategoryId = p.CategoryId,
                CategoryCode = p.Category.Code,
                CategoryName = p.Category.Name,
                Sku = p.Sku,
                Barcode = p.Barcode,
                Name = p.Name,
                Description = p.Description,
                UnitOfMeasureId = p.UnitOfMeasureId,
                UnitOfMeasureCode = p.UnitOfMeasure.Code,
                UnitOfMeasureName = p.UnitOfMeasure.Name,
                WeightKg = p.WeightKg,
                LengthCm = p.LengthCm,
                WidthCm = p.WidthCm,
                HeightCm = p.HeightCm,
                VolumeCbm = (p.LengthCm * p.WidthCm * p.HeightCm) / 1000000m,
                DeclaredValue = p.DeclaredValue,
                CurrencyId = p.CurrencyId,
                CurrencyCode = p.Currency.Code,
                IsPerishable = p.IsPerishable,
                IsDangerous = p.IsDangerous,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                CreatedBy = p.CreatedBy,
                UpdatedAt = p.UpdatedAt,
                UpdatedBy = p.UpdatedBy
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<CategoryDto?> GetCategoryByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _categoryService.GetByIdOrPublicIdAsync(id.ToString(), cancellationToken);
    }

    public async Task<UnitOfMeasureDto?> GetUnitOfMeasureByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _unitOfMeasureService.GetByIdAsync(id, cancellationToken);
    }

    public async Task<UnitOfMeasureDto?> GetUnitOfMeasureByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _unitOfMeasureService.GetByCodeAsync(code, cancellationToken);
    }

    public async Task<bool> CanProductBeUsedInTransactionAsync(long productId, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.UnitOfMeasure)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

        if (product == null) return false;
        return product.IsActive && product.Category.IsActive && product.UnitOfMeasure.IsActive;
    }

    public async Task<IReadOnlyList<ServiceDto>> GetServicesAsync(bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        return await _serviceCatalogService.GetServicesAsync(activeOnly, cancellationToken);
    }

    public async Task<ServiceDto?> GetServiceByCodeAsync(string serviceCode, CancellationToken cancellationToken = default)
    {
        return await _serviceCatalogService.GetByCodeAsync(serviceCode, cancellationToken);
    }

    public async Task<bool> CanServiceBeUsedInTransactionAsync(string serviceCode, CancellationToken cancellationToken = default)
    {
        var service = await _serviceCatalogService.GetByCodeAsync(serviceCode, cancellationToken);
        return service != null && service.IsActive;
    }
}
