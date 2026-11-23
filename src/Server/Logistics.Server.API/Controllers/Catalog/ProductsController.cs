using FluentValidation;
using Logistics.Server.API.Authorization;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Catalog.DTOs;
using Logistics.Server.Application.Modules.Catalog.Services;
using Logistics.Shared.Contracts;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.AspNetCore.Mvc;
using ValidationException = Logistics.Server.Application.Common.Exceptions.ValidationException;

namespace Logistics.Server.API.Controllers.Catalog;

/// <summary>
/// Endpoints for product catalog management, item specifications, and search.
/// </summary>
[Route("api/v1/catalog/products")]
public class ProductsController : BaseApiController
{
    private readonly IProductService _productService;
    private readonly IValidator<CreateProductRequest> _createValidator;
    private readonly IValidator<UpdateProductRequest> _updateValidator;
    private readonly IValidator<UpdateProductStatusRequest> _statusValidator;

    public ProductsController(
        IProductService productService,
        IValidator<CreateProductRequest> createValidator,
        IValidator<UpdateProductRequest> updateValidator,
        IValidator<UpdateProductStatusRequest> statusValidator)
    {
        _productService = productService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _statusValidator = statusValidator;
    }

    /// <summary>
    /// Searches and filters products with multi-parameter criteria and pagination.
    /// </summary>
    [HttpGet]
    [HasPermission(AppPermissions.Catalog.ProductsRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<ProductDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts(
        [FromQuery] string? search,
        [FromQuery] long? categoryId,
        [FromQuery] bool? isActive,
        [FromQuery] long? unitOfMeasureId,
        [FromQuery] bool? isDangerous,
        [FromQuery] bool? isPerishable,
        [FromQuery] decimal? minWeight,
        [FromQuery] decimal? maxWeight,
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _productService.GetProductsAsync(
            search, categoryId, isActive, unitOfMeasureId, isDangerous, isPerishable, minWeight, maxWeight, request, cancellationToken);
        return Ok(ApiResponse<PagedResponse<ProductDto>>.Ok(result.ToPagedResponse()));
    }

    /// <summary>
    /// Fast autocomplete search returning lightweight product projections.
    /// </summary>
    [HttpGet("search")]
    [HasPermission(AppPermissions.Catalog.ProductsRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ProductSearchResultDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] string? q,
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var results = await _productService.SearchAsync(q ?? string.Empty, limit, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ProductSearchResultDto>>.Ok(results));
    }

    /// <summary>
    /// Retrieves a product by exact SKU or Barcode.
    /// </summary>
    [HttpGet("code/{code}")]
    [HasPermission(AppPermissions.Catalog.ProductsRead)]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByCode(string code, CancellationToken cancellationToken)
    {
        var product = await _productService.GetByCodeOrBarcodeAsync(code, cancellationToken);
        if (product == null)
        {
            return NotFound(ApiResponse<ProductDto>.Fail($"Product with code/barcode '{code}' not found."));
        }

        return Ok(ApiResponse<ProductDto>.Ok(product));
    }

    /// <summary>
    /// Retrieves full product details by ID, PublicId, or SKU.
    /// </summary>
    [HttpGet("{id}")]
    [HasPermission(AppPermissions.Catalog.ProductsRead)]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductById(string id, CancellationToken cancellationToken)
    {
        var product = await _productService.GetDetailByIdOrPublicIdAsync(id, cancellationToken);
        if (product == null)
        {
            return NotFound(ApiResponse<ProductDetailDto>.Fail($"Product '{id}' not found."));
        }

        return Ok(ApiResponse<ProductDetailDto>.Ok(product));
    }

    /// <summary>
    /// Creates a new product in the catalog.
    /// </summary>
    [HttpPost]
    [HasPermission(AppPermissions.Catalog.ProductsCreate)]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateProduct(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        var created = await _productService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetProductById), new { id = created.Id }, ApiResponse<ProductDto>.Ok(created, "Product created successfully."));
    }

    /// <summary>
    /// Updates product specifications and attributes.
    /// </summary>
    [HttpPut("{id}")]
    [HasPermission(AppPermissions.Catalog.ProductsUpdate)]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProduct(
        string id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        var updated = await _productService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<ProductDto>.Ok(updated, "Product updated successfully."));
    }

    /// <summary>
    /// Activates or deactivates a product.
    /// </summary>
    [HttpPatch("{id}/status")]
    [HasPermission(AppPermissions.Catalog.ProductsManageStatus)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        string id,
        [FromBody] UpdateProductStatusRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _statusValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        await _productService.UpdateStatusAsync(id, request, cancellationToken);
        return Ok(ApiResponse.Ok("Product status updated successfully."));
    }

    /// <summary>
    /// Deletes an unreferenced product from the catalog.
    /// </summary>
    [HttpDelete("{id}")]
    [HasPermission(AppPermissions.Catalog.ProductsDelete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct(string id, CancellationToken cancellationToken)
    {
        await _productService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok("Product deleted successfully."));
    }
}
