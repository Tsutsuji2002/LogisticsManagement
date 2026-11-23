using Logistics.Server.API.Authorization;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Catalog.DTOs;
using Logistics.Server.Application.Modules.Catalog.Services;
using Logistics.Shared.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Logistics.Server.API.Controllers.Catalog;

/// <summary>
/// Endpoints for discovering available logistics service speed tiers and carrier offerings.
/// </summary>
[Route("api/v1/catalog/services")]
public class ServicesController : BaseApiController
{
    private readonly ILogisticsServiceCatalogService _serviceCatalogService;

    public ServicesController(ILogisticsServiceCatalogService serviceCatalogService)
    {
        _serviceCatalogService = serviceCatalogService;
    }

    /// <summary>
    /// Retrieves all available logistics delivery service tiers.
    /// </summary>
    [HttpGet]
    [HasPermission(AppPermissions.Catalog.ServicesRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ServiceDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetServices(
        [FromQuery] bool activeOnly,
        CancellationToken cancellationToken)
    {
        var services = await _serviceCatalogService.GetServicesAsync(activeOnly, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ServiceDto>>.Ok(services));
    }

    /// <summary>
    /// Retrieves service details by service code (e.g., SAMEDAY, NEXTDAY, STANDARD, ECONOMICAL).
    /// </summary>
    [HttpGet("{code}")]
    [HttpGet("code/{code}")]
    [HasPermission(AppPermissions.Catalog.ServicesRead)]
    [ProducesResponseType(typeof(ApiResponse<ServiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetServiceByCode(string code, CancellationToken cancellationToken)
    {
        var service = await _serviceCatalogService.GetByCodeAsync(code, cancellationToken);
        if (service == null)
        {
            return NotFound(ApiResponse<ServiceDto>.Fail($"Service with code '{code}' not found."));
        }

        return Ok(ApiResponse<ServiceDto>.Ok(service));
    }
}
