using Logistics.Server.Application.Modules.Catalog.DTOs;

namespace Logistics.Server.Application.Modules.Catalog.Services;

/// <summary>
/// Stable cross-module read interface exposing product, category, unit, and service
/// catalog information to Sales, Shipping, Warehouse, and Inventory domains without
/// coupling them to internal persistence layers.
/// </summary>
public interface ICatalogReadService
{
    Task<ProductDto?> GetProductByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<ProductDto?> GetProductBySkuAsync(string sku, CancellationToken cancellationToken = default);
    Task<ProductDto?> GetProductByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductDto>> GetProductsByIdsAsync(IEnumerable<long> ids, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductDto>> GetActiveProductsAsync(long? categoryId = null, CancellationToken cancellationToken = default);
    Task<CategoryDto?> GetCategoryByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<UnitOfMeasureDto?> GetUnitOfMeasureByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<UnitOfMeasureDto?> GetUnitOfMeasureByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<bool> CanProductBeUsedInTransactionAsync(long productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServiceDto>> GetServicesAsync(bool activeOnly = true, CancellationToken cancellationToken = default);
    Task<ServiceDto?> GetServiceByCodeAsync(string serviceCode, CancellationToken cancellationToken = default);
    Task<bool> CanServiceBeUsedInTransactionAsync(string serviceCode, CancellationToken cancellationToken = default);
}
