using Logistics.Server.Application.Common.Models;
using Logistics.Server.Application.Modules.Catalog.DTOs;
using Logistics.Shared.Contracts.Pagination;

namespace Logistics.Server.Application.Modules.Catalog.Services;

public interface ICategoryService
{
    Task<PagedResult<CategoryDto>> GetCategoriesAsync(
        string? search,
        bool? isActive,
        long? parentCategoryId,
        PagedRequest request,
        CancellationToken cancellationToken = default);

    Task<CategoryDto?> GetByIdOrPublicIdAsync(string identifier, CancellationToken cancellationToken = default);
    Task<CategoryDetailDto?> GetDetailByIdOrPublicIdAsync(string identifier, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CategoryTreeDto>> GetCategoryTreeAsync(bool activeOnly = false, CancellationToken cancellationToken = default);
    Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default);
    Task<CategoryDto> UpdateAsync(string identifier, UpdateCategoryRequest request, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(string identifier, UpdateCategoryStatusRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(string identifier, CancellationToken cancellationToken = default);
}

public interface IProductService
{
    Task<PagedResult<ProductDto>> GetProductsAsync(
        string? search,
        long? categoryId,
        bool? isActive,
        long? unitOfMeasureId,
        bool? isDangerous,
        bool? isPerishable,
        decimal? minWeight,
        decimal? maxWeight,
        PagedRequest request,
        CancellationToken cancellationToken = default);

    Task<ProductDto?> GetByIdOrPublicIdAsync(string identifier, CancellationToken cancellationToken = default);
    Task<ProductDetailDto?> GetDetailByIdOrPublicIdAsync(string identifier, CancellationToken cancellationToken = default);
    Task<ProductDto?> GetByCodeOrBarcodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductSearchResultDto>> SearchAsync(string query, int maxResults = 20, CancellationToken cancellationToken = default);
    Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<ProductDto> UpdateAsync(string identifier, UpdateProductRequest request, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(string identifier, UpdateProductStatusRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(string identifier, CancellationToken cancellationToken = default);
}

public interface IUnitOfMeasureService
{
    Task<IReadOnlyList<UnitOfMeasureDto>> GetUnitsAsync(
        string? search,
        string? type,
        bool? isActive,
        CancellationToken cancellationToken = default);

    Task<UnitOfMeasureDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<UnitOfMeasureDto?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UnitOfMeasureDto>> SearchAsync(string query, CancellationToken cancellationToken = default);
    Task<UnitOfMeasureDto> CreateAsync(CreateUnitOfMeasureRequest request, CancellationToken cancellationToken = default);
    Task<UnitOfMeasureDto> UpdateAsync(long id, UpdateUnitOfMeasureRequest request, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(long id, UpdateUnitStatusRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}

public interface ILogisticsServiceCatalogService
{
    Task<IReadOnlyList<ServiceDto>> GetServicesAsync(bool activeOnly = false, CancellationToken cancellationToken = default);
    Task<ServiceDto?> GetByCodeAsync(string serviceCode, CancellationToken cancellationToken = default);
}
