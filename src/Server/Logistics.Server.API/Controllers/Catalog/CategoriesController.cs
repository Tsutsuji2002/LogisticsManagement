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
/// Endpoints for hierarchical product category management.
/// </summary>
[Route("api/v1/catalog/categories")]
public class CategoriesController : BaseApiController
{
    private readonly ICategoryService _categoryService;
    private readonly IValidator<CreateCategoryRequest> _createValidator;
    private readonly IValidator<UpdateCategoryRequest> _updateValidator;
    private readonly IValidator<UpdateCategoryStatusRequest> _statusValidator;

    public CategoriesController(
        ICategoryService categoryService,
        IValidator<CreateCategoryRequest> createValidator,
        IValidator<UpdateCategoryRequest> updateValidator,
        IValidator<UpdateCategoryStatusRequest> statusValidator)
    {
        _categoryService = categoryService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _statusValidator = statusValidator;
    }

    /// <summary>
    /// Searches and filters product categories with pagination.
    /// </summary>
    [HttpGet]
    [HasPermission(AppPermissions.Catalog.CategoriesRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<CategoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] long? parentCategoryId,
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _categoryService.GetCategoriesAsync(search, isActive, parentCategoryId, request, cancellationToken);
        return Ok(ApiResponse<PagedResponse<CategoryDto>>.Ok(result.ToPagedResponse()));
    }

    /// <summary>
    /// Retrieves the nested category hierarchy tree.
    /// </summary>
    [HttpGet("tree")]
    [HasPermission(AppPermissions.Catalog.CategoriesRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CategoryTreeDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategoryTree(
        [FromQuery] bool activeOnly,
        CancellationToken cancellationToken)
    {
        var result = await _categoryService.GetCategoryTreeAsync(activeOnly, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<CategoryTreeDto>>.Ok(result));
    }

    /// <summary>
    /// Retrieves detailed category information by ID, PublicId, or Code.
    /// </summary>
    [HttpGet("{id}")]
    [HasPermission(AppPermissions.Catalog.CategoriesRead)]
    [ProducesResponseType(typeof(ApiResponse<CategoryDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategoryById(string id, CancellationToken cancellationToken)
    {
        var category = await _categoryService.GetDetailByIdOrPublicIdAsync(id, cancellationToken);
        if (category == null)
        {
            return NotFound(ApiResponse<CategoryDetailDto>.Fail($"Product category '{id}' not found."));
        }

        return Ok(ApiResponse<CategoryDetailDto>.Ok(category));
    }

    /// <summary>
    /// Creates a new product category.
    /// </summary>
    [HttpPost]
    [HasPermission(AppPermissions.Catalog.CategoriesCreate)]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCategory(
        [FromBody] CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        var created = await _categoryService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetCategoryById), new { id = created.Id }, ApiResponse<CategoryDto>.Ok(created, "Category created successfully."));
    }

    /// <summary>
    /// Updates an existing product category.
    /// </summary>
    [HttpPut("{id}")]
    [HasPermission(AppPermissions.Catalog.CategoriesUpdate)]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCategory(
        string id,
        [FromBody] UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        var updated = await _categoryService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<CategoryDto>.Ok(updated, "Category updated successfully."));
    }

    /// <summary>
    /// Activates or deactivates a category. Deactivation is rejected if active products exist in the category.
    /// </summary>
    [HttpPatch("{id}/status")]
    [HasPermission(AppPermissions.Catalog.CategoriesManageStatus)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        string id,
        [FromBody] UpdateCategoryStatusRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _statusValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        await _categoryService.UpdateStatusAsync(id, request, cancellationToken);
        return Ok(ApiResponse.Ok("Category status updated successfully."));
    }

    /// <summary>
    /// Deletes an unreferenced category without subcategories.
    /// </summary>
    [HttpDelete("{id}")]
    [HasPermission(AppPermissions.Catalog.CategoriesDelete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCategory(string id, CancellationToken cancellationToken)
    {
        await _categoryService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse.Ok("Product category deleted successfully."));
    }
}
