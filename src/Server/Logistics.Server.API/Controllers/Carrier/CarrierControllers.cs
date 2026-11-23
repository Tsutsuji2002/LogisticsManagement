using FluentValidation;
using Logistics.Server.API.Authorization;
using Logistics.Server.Application.Common.Exceptions;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Carrier.DTOs;
using Logistics.Server.Application.Modules.Carrier.Services;
using Logistics.Shared.Contracts;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ValidationException = Logistics.Server.Application.Common.Exceptions.ValidationException;

namespace Logistics.Server.API.Controllers.Carrier;

/// <summary>
/// Carrier CRUD operations.
/// </summary>
[Route("api/v1/carriers")]
public class CarriersController : BaseApiController
{
    private readonly ICarrierManagementService _carrierService;
    private readonly IValidator<CreateCarrierRequest> _createValidator;
    private readonly IValidator<UpdateCarrierRequest> _updateValidator;

    public CarriersController(
        ICarrierManagementService carrierService,
        IValidator<CreateCarrierRequest> createValidator,
        IValidator<UpdateCarrierRequest> updateValidator)
    {
        _carrierService = carrierService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// Lists all carriers with optional filtering.
    /// </summary>
    [HttpGet]
    [HasPermission(AppPermissions.Carrier.CarriersRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<CarrierDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCarriers(
        [FromQuery] string? search, [FromQuery] bool? isActive,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _carrierService.GetCarriersAsync(search, isActive,
            new PagedRequest { Page = page, PageSize = pageSize }, cancellationToken);
        return Ok(ApiResponse<PagedResponse<CarrierDto>>.Ok(result.ToPagedResponse()));
    }

    /// <summary>
    /// Gets a carrier by ID, PublicId, or Code.
    /// </summary>
    [HttpGet("{identifier}")]
    [HasPermission(AppPermissions.Carrier.CarriersRead)]
    [ProducesResponseType(typeof(ApiResponse<CarrierDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCarrier(string identifier, CancellationToken cancellationToken)
    {
        var carrier = await _carrierService.GetByIdOrCodeAsync(identifier, cancellationToken);
        if (carrier == null) return NotFound(ApiResponse.Fail($"Carrier '{identifier}' not found."));
        return Ok(ApiResponse<CarrierDetailDto>.Ok(carrier));
    }

    /// <summary>
    /// Creates a new carrier.
    /// </summary>
    [HttpPost]
    [HasPermission(AppPermissions.Carrier.CarriersManage)]
    [ProducesResponseType(typeof(ApiResponse<CarrierDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCarrier([FromBody] CreateCarrierRequest request, CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

        var carrier = await _carrierService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetCarrier), new { identifier = carrier.Id }, ApiResponse<CarrierDetailDto>.Ok(carrier));
    }

    /// <summary>
    /// Updates an existing carrier.
    /// </summary>
    [HttpPut("{identifier}")]
    [HasPermission(AppPermissions.Carrier.CarriersManage)]
    [ProducesResponseType(typeof(ApiResponse<CarrierDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCarrier(string identifier, [FromBody] UpdateCarrierRequest request, CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

        var carrier = await _carrierService.UpdateAsync(identifier, request, cancellationToken);
        return Ok(ApiResponse<CarrierDetailDto>.Ok(carrier));
    }

    /// <summary>
    /// Activates or deactivates a carrier.
    /// </summary>
    [HttpPatch("{identifier}/status")]
    [HasPermission(AppPermissions.Carrier.CarriersManage)]
    [ProducesResponseType(typeof(ApiResponse<CarrierDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ToggleCarrierStatus(string identifier, [FromQuery] bool isActive, CancellationToken cancellationToken)
    {
        var carrier = await _carrierService.ToggleActiveAsync(identifier, isActive, cancellationToken);
        return Ok(ApiResponse<CarrierDetailDto>.Ok(carrier));
    }
}

/// <summary>
/// Carrier service type management.
/// </summary>
[Route("api/v1/carriers/{carrierIdentifier}/services")]
public class CarrierServicesController : BaseApiController
{
    private readonly ICarrierServiceManagementService _serviceMgmt;
    private readonly IValidator<CreateCarrierServiceRequest> _createValidator;
    private readonly IValidator<UpdateCarrierServiceRequest> _updateValidator;

    public CarrierServicesController(
        ICarrierServiceManagementService serviceMgmt,
        IValidator<CreateCarrierServiceRequest> createValidator,
        IValidator<UpdateCarrierServiceRequest> updateValidator)
    {
        _serviceMgmt = serviceMgmt;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet]
    [HasPermission(AppPermissions.Carrier.CarriersRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CarrierServiceDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetServices(string carrierIdentifier, CancellationToken cancellationToken)
    {
        var services = await _serviceMgmt.GetServicesAsync(carrierIdentifier, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<CarrierServiceDto>>.Ok(services));
    }

    [HttpGet("{serviceId:long}")]
    [HasPermission(AppPermissions.Carrier.CarriersRead)]
    [ProducesResponseType(typeof(ApiResponse<CarrierServiceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetService(string carrierIdentifier, long serviceId, CancellationToken cancellationToken)
    {
        var service = await _serviceMgmt.GetServiceByIdAsync(carrierIdentifier, serviceId, cancellationToken);
        if (service == null) return NotFound(ApiResponse.Fail("Carrier service not found."));
        return Ok(ApiResponse<CarrierServiceDto>.Ok(service));
    }

    [HttpPost]
    [HasPermission(AppPermissions.Carrier.CarriersManage)]
    [ProducesResponseType(typeof(ApiResponse<CarrierServiceDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateService(string carrierIdentifier, [FromBody] CreateCarrierServiceRequest request, CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

        var service = await _serviceMgmt.CreateServiceAsync(carrierIdentifier, request, cancellationToken);
        return CreatedAtAction(nameof(GetService), new { carrierIdentifier, serviceId = service.Id }, ApiResponse<CarrierServiceDto>.Ok(service));
    }

    [HttpPut("{serviceId:long}")]
    [HasPermission(AppPermissions.Carrier.CarriersManage)]
    [ProducesResponseType(typeof(ApiResponse<CarrierServiceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateService(string carrierIdentifier, long serviceId, [FromBody] UpdateCarrierServiceRequest request, CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

        var service = await _serviceMgmt.UpdateServiceAsync(carrierIdentifier, serviceId, request, cancellationToken);
        return Ok(ApiResponse<CarrierServiceDto>.Ok(service));
    }

    [HttpPatch("{serviceId:long}/status")]
    [HasPermission(AppPermissions.Carrier.CarriersManage)]
    [ProducesResponseType(typeof(ApiResponse<CarrierServiceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ToggleServiceStatus(string carrierIdentifier, long serviceId, [FromQuery] bool isActive, CancellationToken cancellationToken)
    {
        var service = await _serviceMgmt.ToggleServiceActiveAsync(carrierIdentifier, serviceId, isActive, cancellationToken);
        return Ok(ApiResponse<CarrierServiceDto>.Ok(service));
    }
}

/// <summary>
/// Carrier configuration (credentials) management.
/// </summary>
[Route("api/v1/carriers/{carrierIdentifier}/configurations")]
public class CarrierConfigurationsController : BaseApiController
{
    private readonly ICarrierConfigurationService _configService;
    private readonly IValidator<UpsertCarrierConfigurationRequest> _validator;

    public CarrierConfigurationsController(ICarrierConfigurationService configService, IValidator<UpsertCarrierConfigurationRequest> validator)
    {
        _configService = configService;
        _validator = validator;
    }

    [HttpGet]
    [HasPermission(AppPermissions.Carrier.CarriersManage)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CarrierConfigurationDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConfigurations(string carrierIdentifier, CancellationToken cancellationToken)
    {
        var configs = await _configService.GetConfigurationsAsync(carrierIdentifier, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<CarrierConfigurationDto>>.Ok(configs));
    }

    [HttpPut]
    [HasPermission(AppPermissions.Carrier.CarriersManage)]
    [ProducesResponseType(typeof(ApiResponse<CarrierConfigurationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpsertConfiguration(string carrierIdentifier, [FromBody] UpsertCarrierConfigurationRequest request, CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

        var config = await _configService.UpsertConfigurationAsync(carrierIdentifier, request, cancellationToken);
        return Ok(ApiResponse<CarrierConfigurationDto>.Ok(config));
    }

    [HttpDelete("{configId:long}")]
    [HasPermission(AppPermissions.Carrier.CarriersManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteConfiguration(string carrierIdentifier, long configId, CancellationToken cancellationToken)
    {
        await _configService.DeleteConfigurationAsync(carrierIdentifier, configId, cancellationToken);
        return NoContent();
    }
}

/// <summary>
/// Carrier tracking status mapping management.
/// </summary>
[Route("api/v1/carriers/{carrierIdentifier}/tracking-mappings")]
public class CarrierTrackingMappingsController : BaseApiController
{
    private readonly ICarrierTrackingMappingService _mappingService;
    private readonly IValidator<CreateTrackingMappingRequest> _createValidator;
    private readonly IValidator<UpdateTrackingMappingRequest> _updateValidator;

    public CarrierTrackingMappingsController(
        ICarrierTrackingMappingService mappingService,
        IValidator<CreateTrackingMappingRequest> createValidator,
        IValidator<UpdateTrackingMappingRequest> updateValidator)
    {
        _mappingService = mappingService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet]
    [HasPermission(AppPermissions.Carrier.CarriersRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CarrierTrackingMappingDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMappings(string carrierIdentifier, CancellationToken cancellationToken)
    {
        var mappings = await _mappingService.GetMappingsAsync(carrierIdentifier, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<CarrierTrackingMappingDto>>.Ok(mappings));
    }

    [HttpPost]
    [HasPermission(AppPermissions.Carrier.CarriersManage)]
    [ProducesResponseType(typeof(ApiResponse<CarrierTrackingMappingDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateMapping(string carrierIdentifier, [FromBody] CreateTrackingMappingRequest request, CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

        var mapping = await _mappingService.CreateMappingAsync(carrierIdentifier, request, cancellationToken);
        return Created(string.Empty, ApiResponse<CarrierTrackingMappingDto>.Ok(mapping));
    }

    [HttpPut("{mappingId:long}")]
    [HasPermission(AppPermissions.Carrier.CarriersManage)]
    [ProducesResponseType(typeof(ApiResponse<CarrierTrackingMappingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateMapping(string carrierIdentifier, long mappingId, [FromBody] UpdateTrackingMappingRequest request, CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

        var mapping = await _mappingService.UpdateMappingAsync(carrierIdentifier, mappingId, request, cancellationToken);
        return Ok(ApiResponse<CarrierTrackingMappingDto>.Ok(mapping));
    }

    [HttpDelete("{mappingId:long}")]
    [HasPermission(AppPermissions.Carrier.CarriersManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteMapping(string carrierIdentifier, long mappingId, CancellationToken cancellationToken)
    {
        await _mappingService.DeleteMappingAsync(carrierIdentifier, mappingId, cancellationToken);
        return NoContent();
    }
}
