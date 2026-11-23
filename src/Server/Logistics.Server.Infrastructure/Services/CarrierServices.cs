using Logistics.Server.Application.Common.Exceptions;
using Logistics.Server.Application.Common.Interfaces;
using Logistics.Server.Application.Common.Models;
using Logistics.Server.Application.Modules.Carrier.DTOs;
using Logistics.Server.Application.Modules.Carrier.Services;
using Logistics.Server.Domain.Entities.Carrier;
using Logistics.Server.Infrastructure.Persistence;
using Logistics.Server.Integrations.Carriers;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Logistics.Server.Infrastructure.Services;

// ═══════════════════════════════════════════════════════════════
//  CARRIER MANAGEMENT SERVICE
// ═══════════════════════════════════════════════════════════════

public class CarrierManagementService : ICarrierManagementService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;
    private readonly ILogger<CarrierManagementService> _logger;

    public CarrierManagementService(
        ApplicationDbContext context,
        ICurrentUserService currentUser,
        IAuditService audit,
        ILogger<CarrierManagementService> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _audit = audit;
        _logger = logger;
    }

    public async Task<PagedResult<CarrierDto>> GetCarriersAsync(
        string? search, bool? isActive, PagedRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Carriers
            .Include(c => c.Services)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.Code.Contains(search) || c.Name.Contains(search));
        if (isActive.HasValue)
            query = query.Where(c => c.IsActive == isActive.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(c => c.Code)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => MapToDto(c))
            .ToListAsync(cancellationToken);

        return new PagedResult<CarrierDto>(items, totalCount, request.Page, request.PageSize);
    }

    public async Task<CarrierDetailDto?> GetByIdOrCodeAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var carrier = await FindCarrierAsync(identifier, cancellationToken, includeDetails: true);
        return carrier == null ? null : MapToDetailDto(carrier);
    }

    public async Task<CarrierDetailDto> CreateAsync(CreateCarrierRequest request, CancellationToken cancellationToken = default)
    {
        var exists = await _context.Carriers.AnyAsync(c => c.Code == request.Code, cancellationToken);
        if (exists)
            throw new ConflictException("Carrier", "Code", request.Code);

        var carrier = new Domain.Entities.Carrier.Carrier
        {
            Code = request.Code.ToUpperInvariant(),
            Name = request.Name,
            ContactPhone = request.ContactPhone,
            ContactEmail = request.ContactEmail,
            WebsiteUrl = request.WebsiteUrl,
            ApiBaseUrl = request.ApiBaseUrl,
            TrackingUrlTemplate = request.TrackingUrlTemplate,
            IsActive = true,
            CreatedBy = _currentUser.Username
        };

        _context.Carriers.Add(carrier);
        await _context.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync("Create", "carrier", "Carriers", carrier.Id.ToString(),
            null, new { carrier.Code, carrier.Name, carrier.ApiBaseUrl },
            notes: null, cancellationToken: cancellationToken);

        _logger.LogInformation("Carrier '{Code}' created with Id {Id}.", carrier.Code, carrier.Id);

        return MapToDetailDto(carrier);
    }

    public async Task<CarrierDetailDto> UpdateAsync(string identifier, UpdateCarrierRequest request, CancellationToken cancellationToken = default)
    {
        var carrier = await FindCarrierAsync(identifier, cancellationToken)
            ?? throw new NotFoundException("Carrier", identifier);

        var old = new { carrier.Name, carrier.ApiBaseUrl };

        carrier.Name = request.Name;
        carrier.ContactPhone = request.ContactPhone;
        carrier.ContactEmail = request.ContactEmail;
        carrier.WebsiteUrl = request.WebsiteUrl;
        carrier.ApiBaseUrl = request.ApiBaseUrl;
        carrier.TrackingUrlTemplate = request.TrackingUrlTemplate;
        carrier.UpdatedAt = DateTime.UtcNow;
        carrier.UpdatedBy = _currentUser.Username;

        await _context.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync("Update", "carrier", "Carriers", carrier.Id.ToString(),
            old, new { carrier.Name, carrier.ApiBaseUrl },
            notes: null, cancellationToken: cancellationToken);

        return MapToDetailDto(carrier);
    }

    public async Task<CarrierDetailDto> ToggleActiveAsync(string identifier, bool isActive, CancellationToken cancellationToken = default)
    {
        var carrier = await FindCarrierAsync(identifier, cancellationToken)
            ?? throw new NotFoundException("Carrier", identifier);

        carrier.IsActive = isActive;
        carrier.UpdatedAt = DateTime.UtcNow;
        carrier.UpdatedBy = _currentUser.Username;

        await _context.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync(isActive ? "Activate" : "Deactivate", "carrier", "Carriers", carrier.Id.ToString(),
            null, new { carrier.IsActive },
            notes: null, cancellationToken: cancellationToken);

        return MapToDetailDto(carrier);
    }

    private async Task<Domain.Entities.Carrier.Carrier?> FindCarrierAsync(string identifier, CancellationToken ct, bool includeDetails = false)
    {
        var query = _context.Carriers.AsQueryable();
        if (includeDetails)
        {
            query = query
                .Include(c => c.Services)
                .Include(c => c.TrackingMappings);
        }

        if (long.TryParse(identifier, out var id))
            return await query.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (Guid.TryParse(identifier, out var publicId))
            return await query.FirstOrDefaultAsync(c => c.PublicId == publicId, ct);
        return await query.FirstOrDefaultAsync(c => c.Code == identifier.ToUpperInvariant(), ct);
    }

    private static CarrierDto MapToDto(Domain.Entities.Carrier.Carrier c) => new()
    {
        Id = c.Id,
        PublicId = c.PublicId,
        Code = c.Code,
        Name = c.Name,
        ContactPhone = c.ContactPhone,
        ContactEmail = c.ContactEmail,
        WebsiteUrl = c.WebsiteUrl,
        ApiBaseUrl = c.ApiBaseUrl,
        TrackingUrlTemplate = c.TrackingUrlTemplate,
        IsActive = c.IsActive,
        CreatedAt = c.CreatedAt,
        CreatedBy = c.CreatedBy,
        UpdatedAt = c.UpdatedAt,
        UpdatedBy = c.UpdatedBy,
        ServiceCount = c.Services?.Count ?? 0
    };

    private static CarrierDetailDto MapToDetailDto(Domain.Entities.Carrier.Carrier c) => new()
    {
        Id = c.Id,
        PublicId = c.PublicId,
        Code = c.Code,
        Name = c.Name,
        ContactPhone = c.ContactPhone,
        ContactEmail = c.ContactEmail,
        WebsiteUrl = c.WebsiteUrl,
        ApiBaseUrl = c.ApiBaseUrl,
        TrackingUrlTemplate = c.TrackingUrlTemplate,
        IsActive = c.IsActive,
        CreatedAt = c.CreatedAt,
        CreatedBy = c.CreatedBy,
        UpdatedAt = c.UpdatedAt,
        UpdatedBy = c.UpdatedBy,
        ServiceCount = c.Services?.Count ?? 0,
        Services = c.Services?.Select(s => CarrierServiceManagementService.MapServiceToDto(s, c.Code)).ToList() ?? [],
        TrackingMappings = c.TrackingMappings?.Select(m => CarrierTrackingMappingServiceImpl.MapMappingToDto(m, c.Code)).ToList() ?? []
    };
}

// ═══════════════════════════════════════════════════════════════
//  CARRIER SERVICE MANAGEMENT
// ═══════════════════════════════════════════════════════════════

public class CarrierServiceManagementService : ICarrierServiceManagementService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;

    public CarrierServiceManagementService(ApplicationDbContext context, ICurrentUserService currentUser, IAuditService audit)
    {
        _context = context;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<IReadOnlyList<CarrierServiceDto>> GetServicesAsync(string carrierIdentifier, CancellationToken cancellationToken = default)
    {
        var carrier = await ResolveCarrierAsync(carrierIdentifier, cancellationToken);
        var services = await _context.CarrierServices
            .AsNoTracking()
            .Where(s => s.CarrierId == carrier.Id)
            .OrderBy(s => s.ServiceCode)
            .ToListAsync(cancellationToken);
        return services.Select(s => MapServiceToDto(s, carrier.Code)).ToList();
    }

    public async Task<CarrierServiceDto?> GetServiceByIdAsync(string carrierIdentifier, long serviceId, CancellationToken cancellationToken = default)
    {
        var carrier = await ResolveCarrierAsync(carrierIdentifier, cancellationToken);
        var service = await _context.CarrierServices.AsNoTracking()
            .FirstOrDefaultAsync(s => s.CarrierId == carrier.Id && s.Id == serviceId, cancellationToken);
        return service == null ? null : MapServiceToDto(service, carrier.Code);
    }

    public async Task<CarrierServiceDto> CreateServiceAsync(string carrierIdentifier, CreateCarrierServiceRequest request, CancellationToken cancellationToken = default)
    {
        var carrier = await ResolveCarrierAsync(carrierIdentifier, cancellationToken);

        var exists = await _context.CarrierServices.AnyAsync(
            s => s.CarrierId == carrier.Id && s.ServiceCode == request.ServiceCode, cancellationToken);
        if (exists)
            throw new ConflictException("CarrierService", "ServiceCode", request.ServiceCode);

        var service = new Domain.Entities.Carrier.CarrierService
        {
            CarrierId = carrier.Id,
            ServiceCode = request.ServiceCode.ToUpperInvariant(),
            ServiceName = request.ServiceName,
            Description = request.Description,
            EstimatedTransitHours = request.EstimatedTransitHours,
            MaxWeightKg = request.MaxWeightKg,
            MaxLengthCm = request.MaxLengthCm,
            IsCodSupported = request.IsCodSupported,
            IsInsuranceSupported = request.IsInsuranceSupported,
            IsActive = true,
            CreatedBy = _currentUser.Username
        };

        _context.CarrierServices.Add(service);
        await _context.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync("Create", "carrier", "CarrierServices", service.Id.ToString(),
            null, new { service.ServiceCode, service.ServiceName },
            notes: null, cancellationToken: cancellationToken);

        return MapServiceToDto(service, carrier.Code);
    }

    public async Task<CarrierServiceDto> UpdateServiceAsync(string carrierIdentifier, long serviceId, UpdateCarrierServiceRequest request, CancellationToken cancellationToken = default)
    {
        var carrier = await ResolveCarrierAsync(carrierIdentifier, cancellationToken);
        var service = await _context.CarrierServices.FirstOrDefaultAsync(
            s => s.CarrierId == carrier.Id && s.Id == serviceId, cancellationToken)
            ?? throw new NotFoundException("CarrierService", serviceId);

        service.ServiceName = request.ServiceName;
        service.Description = request.Description;
        service.EstimatedTransitHours = request.EstimatedTransitHours;
        service.MaxWeightKg = request.MaxWeightKg;
        service.MaxLengthCm = request.MaxLengthCm;
        service.IsCodSupported = request.IsCodSupported;
        service.IsInsuranceSupported = request.IsInsuranceSupported;
        service.UpdatedAt = DateTime.UtcNow;
        service.UpdatedBy = _currentUser.Username;

        await _context.SaveChangesAsync(cancellationToken);
        return MapServiceToDto(service, carrier.Code);
    }

    public async Task<CarrierServiceDto> ToggleServiceActiveAsync(string carrierIdentifier, long serviceId, bool isActive, CancellationToken cancellationToken = default)
    {
        var carrier = await ResolveCarrierAsync(carrierIdentifier, cancellationToken);
        var service = await _context.CarrierServices.FirstOrDefaultAsync(
            s => s.CarrierId == carrier.Id && s.Id == serviceId, cancellationToken)
            ?? throw new NotFoundException("CarrierService", serviceId);

        service.IsActive = isActive;
        service.UpdatedAt = DateTime.UtcNow;
        service.UpdatedBy = _currentUser.Username;

        await _context.SaveChangesAsync(cancellationToken);
        return MapServiceToDto(service, carrier.Code);
    }

    private async Task<Domain.Entities.Carrier.Carrier> ResolveCarrierAsync(string identifier, CancellationToken ct)
    {
        Domain.Entities.Carrier.Carrier? carrier;
        if (long.TryParse(identifier, out var id))
            carrier = await _context.Carriers.FirstOrDefaultAsync(c => c.Id == id, ct);
        else if (Guid.TryParse(identifier, out var pid))
            carrier = await _context.Carriers.FirstOrDefaultAsync(c => c.PublicId == pid, ct);
        else
            carrier = await _context.Carriers.FirstOrDefaultAsync(c => c.Code == identifier.ToUpperInvariant(), ct);

        return carrier ?? throw new NotFoundException("Carrier", identifier);
    }

    internal static CarrierServiceDto MapServiceToDto(Domain.Entities.Carrier.CarrierService s, string carrierCode) => new()
    {
        Id = s.Id,
        PublicId = s.PublicId,
        CarrierId = s.CarrierId,
        CarrierCode = carrierCode,
        ServiceCode = s.ServiceCode,
        ServiceName = s.ServiceName,
        Description = s.Description,
        EstimatedTransitHours = s.EstimatedTransitHours,
        MaxWeightKg = s.MaxWeightKg,
        MaxLengthCm = s.MaxLengthCm,
        IsCodSupported = s.IsCodSupported,
        IsInsuranceSupported = s.IsInsuranceSupported,
        IsActive = s.IsActive
    };
}

// ═══════════════════════════════════════════════════════════════
//  CARRIER CONFIGURATION SERVICE
// ═══════════════════════════════════════════════════════════════

public class CarrierConfigurationServiceImpl : ICarrierConfigurationService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;

    public CarrierConfigurationServiceImpl(ApplicationDbContext context, ICurrentUserService currentUser, IAuditService audit)
    {
        _context = context;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<IReadOnlyList<CarrierConfigurationDto>> GetConfigurationsAsync(string carrierIdentifier, CancellationToken cancellationToken = default)
    {
        var carrier = await ResolveCarrierAsync(carrierIdentifier, cancellationToken);
        var configs = await _context.CarrierConfigurations
            .AsNoTracking()
            .Where(c => c.CarrierId == carrier.Id)
            .OrderBy(c => c.Environment)
            .ToListAsync(cancellationToken);

        return configs.Select(c => MapToDto(c, carrier.Code)).ToList();
    }

    public async Task<CarrierConfigurationDto> UpsertConfigurationAsync(string carrierIdentifier, UpsertCarrierConfigurationRequest request, CancellationToken cancellationToken = default)
    {
        var carrier = await ResolveCarrierAsync(carrierIdentifier, cancellationToken);

        var existing = await _context.CarrierConfigurations
            .FirstOrDefaultAsync(c => c.CarrierId == carrier.Id
                && c.Environment == request.Environment, cancellationToken);

        if (existing != null)
        {
            // Update — only set non-null fields (allows partial credential updates)
            if (request.ApiKey != null) existing.ApiKey = request.ApiKey;
            if (request.ApiSecret != null) existing.ApiSecret = request.ApiSecret;
            if (request.ClientId != null) existing.ClientId = request.ClientId;
            if (request.Username != null) existing.Username = request.Username;
            if (request.Password != null) existing.EncryptedPassword = request.Password;
            if (request.WebhookSecret != null) existing.WebhookSecret = request.WebhookSecret;
            if (request.SettingsJson != null) existing.SettingsJson = request.SettingsJson;
            existing.IsActive = true;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = _currentUser.Username;

            await _context.SaveChangesAsync(cancellationToken);

            await _audit.RecordAsync("UpdateConfiguration", "carrier", "CarrierConfigurations",
                existing.Id.ToString(), null, new { existing.Environment, CredentialsUpdated = true },
                notes: null, cancellationToken: cancellationToken);

            return MapToDto(existing, carrier.Code);
        }

        var config = new CarrierConfiguration
        {
            CarrierId = carrier.Id,
            Environment = request.Environment,
            ApiKey = request.ApiKey ?? string.Empty,
            ApiSecret = request.ApiSecret,
            ClientId = request.ClientId,
            Username = request.Username,
            EncryptedPassword = request.Password,
            WebhookSecret = request.WebhookSecret,
            SettingsJson = request.SettingsJson,
            IsActive = true,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = _currentUser.Username
        };

        _context.CarrierConfigurations.Add(config);
        await _context.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync("CreateConfiguration", "carrier", "CarrierConfigurations",
            config.Id.ToString(), null, new { config.Environment },
            notes: null, cancellationToken: cancellationToken);

        return MapToDto(config, carrier.Code);
    }

    public async Task DeleteConfigurationAsync(string carrierIdentifier, long configId, CancellationToken cancellationToken = default)
    {
        var carrier = await ResolveCarrierAsync(carrierIdentifier, cancellationToken);
        var config = await _context.CarrierConfigurations
            .FirstOrDefaultAsync(c => c.CarrierId == carrier.Id && c.Id == configId, cancellationToken)
            ?? throw new NotFoundException("CarrierConfiguration", configId);

        _context.CarrierConfigurations.Remove(config);
        await _context.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync("DeleteConfiguration", "carrier", "CarrierConfigurations",
            configId.ToString(), new { config.Environment }, null,
            notes: null, cancellationToken: cancellationToken);
    }

    private async Task<Domain.Entities.Carrier.Carrier> ResolveCarrierAsync(string identifier, CancellationToken ct)
    {
        Domain.Entities.Carrier.Carrier? carrier;
        if (long.TryParse(identifier, out var id))
            carrier = await _context.Carriers.FirstOrDefaultAsync(c => c.Id == id, ct);
        else if (Guid.TryParse(identifier, out var pid))
            carrier = await _context.Carriers.FirstOrDefaultAsync(c => c.PublicId == pid, ct);
        else
            carrier = await _context.Carriers.FirstOrDefaultAsync(c => c.Code == identifier.ToUpperInvariant(), ct);
        return carrier ?? throw new NotFoundException("Carrier", identifier);
    }

    private static CarrierConfigurationDto MapToDto(CarrierConfiguration c, string carrierCode) => new()
    {
        Id = c.Id,
        CarrierId = c.CarrierId,
        CarrierCode = carrierCode,
        Environment = c.Environment,
        HasApiKey = !string.IsNullOrEmpty(c.ApiKey),
        HasApiSecret = !string.IsNullOrEmpty(c.ApiSecret),
        HasClientId = !string.IsNullOrEmpty(c.ClientId),
        HasUsername = !string.IsNullOrEmpty(c.Username),
        HasPassword = !string.IsNullOrEmpty(c.EncryptedPassword),
        HasWebhookSecret = !string.IsNullOrEmpty(c.WebhookSecret),
        SettingsJson = c.SettingsJson,
        IsActive = c.IsActive,
        UpdatedAt = c.UpdatedAt,
        UpdatedBy = c.UpdatedBy
    };
}

// ═══════════════════════════════════════════════════════════════
//  CARRIER TRACKING MAPPING SERVICE
// ═══════════════════════════════════════════════════════════════

public class CarrierTrackingMappingServiceImpl : ICarrierTrackingMappingService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;

    public CarrierTrackingMappingServiceImpl(ApplicationDbContext context, ICurrentUserService currentUser, IAuditService audit)
    {
        _context = context;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<IReadOnlyList<CarrierTrackingMappingDto>> GetMappingsAsync(string carrierIdentifier, CancellationToken cancellationToken = default)
    {
        var carrier = await ResolveCarrierAsync(carrierIdentifier, cancellationToken);
        var mappings = await _context.CarrierTrackingMappings
            .AsNoTracking()
            .Where(m => m.CarrierId == carrier.Id)
            .OrderBy(m => m.ExternalStatusCode)
            .ToListAsync(cancellationToken);
        return mappings.Select(m => MapMappingToDto(m, carrier.Code)).ToList();
    }

    public async Task<CarrierTrackingMappingDto> CreateMappingAsync(string carrierIdentifier, CreateTrackingMappingRequest request, CancellationToken cancellationToken = default)
    {
        var carrier = await ResolveCarrierAsync(carrierIdentifier, cancellationToken);

        var exists = await _context.CarrierTrackingMappings.AnyAsync(
            m => m.CarrierId == carrier.Id && m.ExternalStatusCode == request.ExternalStatusCode, cancellationToken);
        if (exists)
            throw new ConflictException("CarrierTrackingMapping", "ExternalStatusCode", request.ExternalStatusCode);

        var mapping = new CarrierTrackingMapping
        {
            CarrierId = carrier.Id,
            ExternalStatusCode = request.ExternalStatusCode,
            ExternalStatusName = request.ExternalStatusName,
            InternalShipmentStatus = request.InternalShipmentStatus,
            InternalSubStatus = request.InternalSubStatus,
            Description = request.Description,
            IsActive = true
        };

        _context.CarrierTrackingMappings.Add(mapping);
        await _context.SaveChangesAsync(cancellationToken);

        await _audit.RecordAsync("CreateMapping", "carrier", "CarrierTrackingMappings",
            mapping.Id.ToString(), null, new { mapping.ExternalStatusCode, mapping.InternalShipmentStatus },
            notes: null, cancellationToken: cancellationToken);

        return MapMappingToDto(mapping, carrier.Code);
    }

    public async Task<CarrierTrackingMappingDto> UpdateMappingAsync(string carrierIdentifier, long mappingId, UpdateTrackingMappingRequest request, CancellationToken cancellationToken = default)
    {
        var carrier = await ResolveCarrierAsync(carrierIdentifier, cancellationToken);
        var mapping = await _context.CarrierTrackingMappings
            .FirstOrDefaultAsync(m => m.CarrierId == carrier.Id && m.Id == mappingId, cancellationToken)
            ?? throw new NotFoundException("CarrierTrackingMapping", mappingId);

        mapping.ExternalStatusName = request.ExternalStatusName;
        mapping.InternalShipmentStatus = request.InternalShipmentStatus;
        mapping.InternalSubStatus = request.InternalSubStatus;
        mapping.Description = request.Description;

        await _context.SaveChangesAsync(cancellationToken);
        return MapMappingToDto(mapping, carrier.Code);
    }

    public async Task DeleteMappingAsync(string carrierIdentifier, long mappingId, CancellationToken cancellationToken = default)
    {
        var carrier = await ResolveCarrierAsync(carrierIdentifier, cancellationToken);
        var mapping = await _context.CarrierTrackingMappings
            .FirstOrDefaultAsync(m => m.CarrierId == carrier.Id && m.Id == mappingId, cancellationToken)
            ?? throw new NotFoundException("CarrierTrackingMapping", mappingId);

        _context.CarrierTrackingMappings.Remove(mapping);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<Domain.Entities.Carrier.Carrier> ResolveCarrierAsync(string identifier, CancellationToken ct)
    {
        Domain.Entities.Carrier.Carrier? carrier;
        if (long.TryParse(identifier, out var id))
            carrier = await _context.Carriers.FirstOrDefaultAsync(c => c.Id == id, ct);
        else if (Guid.TryParse(identifier, out var pid))
            carrier = await _context.Carriers.FirstOrDefaultAsync(c => c.PublicId == pid, ct);
        else
            carrier = await _context.Carriers.FirstOrDefaultAsync(c => c.Code == identifier.ToUpperInvariant(), ct);
        return carrier ?? throw new NotFoundException("Carrier", identifier);
    }

    internal static CarrierTrackingMappingDto MapMappingToDto(CarrierTrackingMapping m, string carrierCode) => new()
    {
        Id = m.Id,
        CarrierId = m.CarrierId,
        CarrierCode = carrierCode,
        ExternalStatusCode = m.ExternalStatusCode,
        ExternalStatusName = m.ExternalStatusName,
        InternalShipmentStatus = m.InternalShipmentStatus,
        InternalSubStatus = m.InternalSubStatus,
        Description = m.Description,
        IsActive = m.IsActive
    };
}

// ═══════════════════════════════════════════════════════════════
//  CARRIER INTEGRATION SERVICE
// ═══════════════════════════════════════════════════════════════

public class CarrierIntegrationServiceImpl : ICarrierIntegrationService
{
    private readonly ApplicationDbContext _context;
    private readonly ICarrierAdapterFactory _adapterFactory;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;
    private readonly ILogger<CarrierIntegrationServiceImpl> _logger;

    public CarrierIntegrationServiceImpl(
        ApplicationDbContext context,
        ICarrierAdapterFactory adapterFactory,
        ICurrentUserService currentUser,
        IAuditService audit,
        ILogger<CarrierIntegrationServiceImpl> logger)
    {
        _context = context;
        _adapterFactory = adapterFactory;
        _currentUser = currentUser;
        _audit = audit;
        _logger = logger;
    }

    public async Task<CarrierBookingResultDto> BookShipmentAsync(CarrierBookShipmentRequest request, CancellationToken cancellationToken = default)
    {
        var shipment = await ResolveShipmentAsync(request.ShipmentIdentifier, cancellationToken);

        if (shipment.AssignedCarrierId == null)
            throw new ConflictException("Shipment has no assigned carrier. Assign a carrier before booking.");

        var carrier = await _context.Carriers.FirstOrDefaultAsync(c => c.Id == shipment.AssignedCarrierId, cancellationToken)
            ?? throw new NotFoundException("Carrier", shipment.AssignedCarrierId);

        if (!carrier.IsActive)
            throw new ConflictException($"Carrier '{carrier.Code}' is not active.");

        var adapter = _adapterFactory.GetAdapter(carrier.Code);

        var senderAddress = shipment.Addresses?.FirstOrDefault(a => a.AddressType == "Sender");
        var recipientAddress = shipment.Addresses?.FirstOrDefault(a => a.AddressType == "Recipient");

        var createReq = new CarrierCreateShipmentRequest(
            ReferenceCode: shipment.TrackingNumber,
            ServiceCode: request.CarrierServiceCode ?? shipment.CarrierService?.ServiceCode ?? "STANDARD",
            SenderName: senderAddress?.ContactName ?? "N/A",
            SenderPhone: senderAddress?.PhoneNumber ?? "N/A",
            SenderAddress: senderAddress?.FormattedAddress ?? "N/A",
            RecipientName: recipientAddress?.ContactName ?? "N/A",
            RecipientPhone: recipientAddress?.PhoneNumber ?? "N/A",
            RecipientAddress: recipientAddress?.FormattedAddress ?? "N/A",
            WeightKg: shipment.ActualWeightKg,
            CodAmount: shipment.CodAmount,
            DeclaredValue: shipment.DeclaredValue,
            Note: shipment.Notes);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        CarrierCreateShipmentResponse response;
        try
        {
            response = await adapter.CreateShipmentAsync(createReq, cancellationToken);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            await LogApiCallAsync(carrier.Id, carrier.ApiBaseUrl + "/create", "POST",
                stopwatch.ElapsedMilliseconds, 0, false, ex.Message, cancellationToken);
            throw;
        }
        stopwatch.Stop();

        await LogApiCallAsync(carrier.Id, carrier.ApiBaseUrl + "/create", "POST",
            stopwatch.ElapsedMilliseconds, response.Success ? 200 : 400,
            response.Success, response.ErrorMessage, cancellationToken);

        if (response.Success)
        {
            shipment.CarrierTrackingNumber = response.CarrierTrackingNumber;
            if (shipment.ShipmentStatus == "Draft")
                shipment.ShipmentStatus = "ReadyForPickup";
            shipment.UpdatedAt = DateTime.UtcNow;
            shipment.UpdatedBy = _currentUser.Username;
            await _context.SaveChangesAsync(cancellationToken);

            await _audit.RecordAsync("BookWithCarrier", "shipping", "Shipments", shipment.Id.ToString(),
                null, new { carrier.Code, response.CarrierTrackingNumber },
                notes: null, cancellationToken: cancellationToken);

            _logger.LogInformation("Shipment {TrackingNumber} booked with carrier {Carrier}, tracking: {CarrierTracking}.",
                shipment.TrackingNumber, carrier.Code, response.CarrierTrackingNumber);
        }

        return new CarrierBookingResultDto
        {
            Success = response.Success,
            CarrierTrackingNumber = response.CarrierTrackingNumber,
            CarrierOrderCode = response.CarrierOrderCode,
            TotalFee = response.TotalFee,
            LabelUrl = response.LabelUrl,
            ErrorMessage = response.ErrorMessage
        };
    }

    public async Task<CarrierCancelResultDto> CancelShipmentAsync(CarrierCancelShipmentRequest request, CancellationToken cancellationToken = default)
    {
        var shipment = await ResolveShipmentAsync(request.ShipmentIdentifier, cancellationToken);

        if (string.IsNullOrEmpty(shipment.CarrierTrackingNumber))
            return new CarrierCancelResultDto { Success = true, ErrorMessage = "No carrier booking to cancel." };

        var carrier = await _context.Carriers.FirstOrDefaultAsync(c => c.Id == shipment.AssignedCarrierId, cancellationToken);
        if (carrier == null)
            return new CarrierCancelResultDto { Success = true, ErrorMessage = "Carrier not found; local cancel only." };

        // Currently adapters don't have a CancelAsync — log the request
        await _audit.RecordAsync("CancelCarrierBooking", "shipping", "Shipments", shipment.Id.ToString(),
            new { shipment.CarrierTrackingNumber }, null,
            notes: request.Reason, cancellationToken: cancellationToken);

        shipment.CarrierTrackingNumber = null;
        shipment.UpdatedAt = DateTime.UtcNow;
        shipment.UpdatedBy = _currentUser.Username;
        await _context.SaveChangesAsync(cancellationToken);

        return new CarrierCancelResultDto { Success = true };
    }

    public async Task<CarrierRateResultDto> CalculateRatesAsync(CarrierRateQueryRequest request, CancellationToken cancellationToken = default)
    {
        var carrier = await _context.Carriers.FirstOrDefaultAsync(c => c.Id == request.CarrierId, cancellationToken)
            ?? throw new NotFoundException("Carrier", request.CarrierId);

        var adapter = _adapterFactory.GetAdapter(carrier.Code);

        var rateReq = new CarrierRateRequest(
            OriginPostalCode: request.OriginPostalCode ?? string.Empty,
            DestinationPostalCode: request.DestinationPostalCode ?? string.Empty,
            OriginProvinceId: request.OriginProvinceId ?? 0,
            DestinationProvinceId: request.DestinationProvinceId ?? 0,
            WeightKg: request.WeightKg,
            LengthCm: request.LengthCm,
            WidthCm: request.WidthCm,
            HeightCm: request.HeightCm,
            DeclaredValue: request.DeclaredValue,
            CodAmount: request.CodAmount);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var response = await adapter.CalculateRatesAsync(rateReq, cancellationToken);
        stopwatch.Stop();

        await LogApiCallAsync(carrier.Id, carrier.ApiBaseUrl + "/rates", "POST",
            stopwatch.ElapsedMilliseconds, response.Success ? 200 : 400,
            response.Success, response.ErrorMessage, cancellationToken);

        return new CarrierRateResultDto
        {
            Success = response.Success,
            ErrorMessage = response.ErrorMessage,
            Options = response.Options.Select(o => new CarrierRateOptionDto
            {
                ServiceCode = o.ServiceCode,
                ServiceName = o.ServiceName,
                TotalFee = o.TotalFee,
                ShippingFee = o.ShippingFee,
                InsuranceFee = o.InsuranceFee,
                EstimatedDays = o.EstimatedDays
            }).ToList()
        };
    }

    public async Task<CarrierTrackingQueryDto> GetTrackingAsync(string carrierCode, string trackingNumber, CancellationToken cancellationToken = default)
    {
        var carrier = await _context.Carriers.FirstOrDefaultAsync(c => c.Code == carrierCode.ToUpperInvariant(), cancellationToken)
            ?? throw new NotFoundException("Carrier", carrierCode);

        var adapter = _adapterFactory.GetAdapter(carrier.Code);
        var trackingInfo = await adapter.GetTrackingAsync(trackingNumber, cancellationToken);

        return new CarrierTrackingQueryDto
        {
            TrackingNumber = trackingInfo.TrackingNumber,
            CarrierCode = trackingInfo.CarrierCode,
            CurrentStatus = trackingInfo.CurrentStatus,
            Events = trackingInfo.Events.Select(e => new CarrierTrackingEventDto
            {
                CarrierId = carrier.Id,
                CarrierCode = carrier.Code,
                CarrierTrackingNumber = trackingNumber,
                ExternalEventId = string.Empty,
                EventCode = e.StatusCode,
                EventDescription = e.StatusDescription,
                EventTimestamp = e.Timestamp,
                LocationName = e.Location,
                ProcessedStatus = "Live"
            }).ToList()
        };
    }

    private async Task<Domain.Entities.Shipping.Shipment> ResolveShipmentAsync(string identifier, CancellationToken ct)
    {
        var query = _context.Shipments
            .Include(s => s.CarrierService)
            .Include(s => s.Addresses)
            .AsQueryable();

        Domain.Entities.Shipping.Shipment? shipment;
        if (long.TryParse(identifier, out var id))
            shipment = await query.FirstOrDefaultAsync(s => s.Id == id, ct);
        else if (Guid.TryParse(identifier, out var pid))
            shipment = await query.FirstOrDefaultAsync(s => s.PublicId == pid, ct);
        else
            shipment = await query.FirstOrDefaultAsync(s => s.TrackingNumber == identifier, ct);

        return shipment ?? throw new NotFoundException("Shipment", identifier);
    }

    private async Task LogApiCallAsync(long carrierId, string endpoint, string httpMethod,
        long durationMs, int statusCode, bool isSuccess, string? errorMessage, CancellationToken ct)
    {
        var log = new CarrierApiLog
        {
            CarrierId = carrierId,
            Endpoint = endpoint,
            HttpMethod = httpMethod,
            DurationMs = (int)durationMs,
            ResponseStatusCode = statusCode,
            IsSuccess = isSuccess,
            ErrorMessage = errorMessage,
            Timestamp = DateTime.UtcNow
        };
        _context.CarrierApiLogs.Add(log);
        await _context.SaveChangesAsync(ct);
    }
}

// ═══════════════════════════════════════════════════════════════
//  CARRIER WEBHOOK SERVICE
// ═══════════════════════════════════════════════════════════════

public class CarrierWebhookServiceImpl : ICarrierWebhookService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CarrierWebhookServiceImpl> _logger;

    public CarrierWebhookServiceImpl(ApplicationDbContext context, ILogger<CarrierWebhookServiceImpl> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CarrierWebhookLogDto> ProcessWebhookAsync(string carrierCode, string payloadJson, string? signature, string clientIp, string webhookUrl, CancellationToken cancellationToken = default)
    {
        var carrier = await _context.Carriers.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Code == carrierCode.ToUpperInvariant(), cancellationToken)
            ?? throw new NotFoundException("Carrier", carrierCode);

        var log = new CarrierWebhookLog
        {
            CarrierId = carrier.Id,
            WebhookUrl = webhookUrl,
            HttpMethod = "POST",
            PayloadJson = payloadJson,
            ClientIpAddress = clientIp,
            ProcessedAt = DateTime.UtcNow,
            ResponseStatusCode = 200,
            ProcessedSuccessfully = true
        };

        try
        {
            // Parse tracking event from webhook payload
            var trackingEvent = new CarrierTrackingEvent
            {
                CarrierId = carrier.Id,
                CarrierTrackingNumber = "PENDING_PARSE",
                ExternalEventId = Guid.NewGuid().ToString("N"),
                EventCode = "WEBHOOK",
                EventDescription = "Webhook event received",
                EventTimestamp = DateTime.UtcNow,
                RawPayloadJson = payloadJson,
                ProcessedStatus = "Pending"
            };

            _context.CarrierTrackingEvents.Add(trackingEvent);
            log.ExternalEventId = trackingEvent.ExternalEventId;
        }
        catch (Exception ex)
        {
            log.ProcessedSuccessfully = false;
            log.ErrorDetails = ex.Message;
            log.ResponseStatusCode = 500;
            _logger.LogError(ex, "Failed to process webhook for carrier {CarrierCode}.", carrierCode);
        }

        _context.CarrierWebhookLogs.Add(log);
        await _context.SaveChangesAsync(cancellationToken);

        return MapWebhookLogToDto(log, carrier.Code);
    }

    public async Task<PagedResult<CarrierWebhookLogDto>> GetWebhookLogsAsync(string? carrierIdentifier, DateTime? from, DateTime? to, bool? processedSuccessfully, PagedRequest request, CancellationToken cancellationToken = default)
    {
        var query = _context.CarrierWebhookLogs
            .Include(w => w.Carrier)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(carrierIdentifier))
        {
            var carrier = await ResolveCarrierAsync(carrierIdentifier, cancellationToken);
            query = query.Where(w => w.CarrierId == carrier.Id);
        }
        if (from.HasValue) query = query.Where(w => w.ProcessedAt >= from.Value);
        if (to.HasValue) query = query.Where(w => w.ProcessedAt <= to.Value);
        if (processedSuccessfully.HasValue) query = query.Where(w => w.ProcessedSuccessfully == processedSuccessfully.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(w => w.ProcessedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<CarrierWebhookLogDto>(
            items.Select(w => MapWebhookLogToDto(w, w.Carrier?.Code ?? "UNKNOWN")).ToList(),
            totalCount, request.Page, request.PageSize);
    }

    private async Task<Domain.Entities.Carrier.Carrier> ResolveCarrierAsync(string identifier, CancellationToken ct)
    {
        Domain.Entities.Carrier.Carrier? carrier;
        if (long.TryParse(identifier, out var id))
            carrier = await _context.Carriers.FirstOrDefaultAsync(c => c.Id == id, ct);
        else if (Guid.TryParse(identifier, out var pid))
            carrier = await _context.Carriers.FirstOrDefaultAsync(c => c.PublicId == pid, ct);
        else
            carrier = await _context.Carriers.FirstOrDefaultAsync(c => c.Code == identifier.ToUpperInvariant(), ct);
        return carrier ?? throw new NotFoundException("Carrier", identifier);
    }

    private static CarrierWebhookLogDto MapWebhookLogToDto(CarrierWebhookLog w, string carrierCode) => new()
    {
        Id = w.Id,
        CarrierId = w.CarrierId,
        CarrierCode = carrierCode,
        ExternalEventId = w.ExternalEventId,
        WebhookUrl = w.WebhookUrl,
        HttpMethod = w.HttpMethod,
        ResponseStatusCode = w.ResponseStatusCode,
        ProcessedSuccessfully = w.ProcessedSuccessfully,
        ProcessedAt = w.ProcessedAt,
        ErrorDetails = w.ErrorDetails,
        ClientIpAddress = w.ClientIpAddress,
        CreatedAt = w.CreatedAt
    };
}

// ═══════════════════════════════════════════════════════════════
//  CARRIER API LOG SERVICE
// ═══════════════════════════════════════════════════════════════

public class CarrierApiLogServiceImpl : ICarrierApiLogService
{
    private readonly ApplicationDbContext _context;

    public CarrierApiLogServiceImpl(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<CarrierApiLogDto>> GetApiLogsAsync(string? carrierIdentifier, DateTime? from, DateTime? to, bool? isSuccess, PagedRequest request, CancellationToken cancellationToken = default)
    {
        var query = _context.CarrierApiLogs
            .Include(l => l.Carrier)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(carrierIdentifier))
        {
            var carrier = await ResolveCarrierAsync(carrierIdentifier, cancellationToken);
            query = query.Where(l => l.CarrierId == carrier.Id);
        }
        if (from.HasValue) query = query.Where(l => l.Timestamp >= from.Value);
        if (to.HasValue) query = query.Where(l => l.Timestamp <= to.Value);
        if (isSuccess.HasValue) query = query.Where(l => l.IsSuccess == isSuccess.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<CarrierApiLogDto>(
            items.Select(l => new CarrierApiLogDto
            {
                Id = l.Id,
                CarrierId = l.CarrierId,
                CarrierCode = l.Carrier?.Code ?? "UNKNOWN",
                Endpoint = l.Endpoint,
                HttpMethod = l.HttpMethod,
                ResponseStatusCode = l.ResponseStatusCode,
                DurationMs = l.DurationMs,
                Timestamp = l.Timestamp,
                IsSuccess = l.IsSuccess,
                ErrorMessage = l.ErrorMessage
            }).ToList(),
            totalCount, request.Page, request.PageSize);
    }

    private async Task<Domain.Entities.Carrier.Carrier> ResolveCarrierAsync(string identifier, CancellationToken ct)
    {
        Domain.Entities.Carrier.Carrier? carrier;
        if (long.TryParse(identifier, out var id))
            carrier = await _context.Carriers.FirstOrDefaultAsync(c => c.Id == id, ct);
        else if (Guid.TryParse(identifier, out var pid))
            carrier = await _context.Carriers.FirstOrDefaultAsync(c => c.PublicId == pid, ct);
        else
            carrier = await _context.Carriers.FirstOrDefaultAsync(c => c.Code == identifier.ToUpperInvariant(), ct);
        return carrier ?? throw new NotFoundException("Carrier", identifier);
    }
}

// ═══════════════════════════════════════════════════════════════
//  CARRIER SYNC JOB SERVICE
// ═══════════════════════════════════════════════════════════════

public class CarrierSyncJobServiceImpl : ICarrierSyncJobService
{
    private readonly ApplicationDbContext _context;

    public CarrierSyncJobServiceImpl(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<CarrierSyncJobDto>> GetSyncJobsAsync(string? carrierIdentifier, string? jobType, string? status, PagedRequest request, CancellationToken cancellationToken = default)
    {
        var query = _context.CarrierSyncJobs
            .Include(j => j.Carrier)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(carrierIdentifier))
        {
            var carrier = await ResolveCarrierAsync(carrierIdentifier, cancellationToken);
            query = query.Where(j => j.CarrierId == carrier.Id);
        }
        if (!string.IsNullOrWhiteSpace(jobType))
            query = query.Where(j => j.JobType == jobType);
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(j => j.Status == status);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(j => j.StartTime)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<CarrierSyncJobDto>(
            items.Select(j => MapSyncJobToDto(j)).ToList(),
            totalCount, request.Page, request.PageSize);
    }

    public async Task<CarrierSyncJobDto> StartSyncJobAsync(long carrierId, string jobType, CancellationToken cancellationToken = default)
    {
        var carrier = await _context.Carriers.FirstOrDefaultAsync(c => c.Id == carrierId, cancellationToken)
            ?? throw new NotFoundException("Carrier", carrierId);

        var job = new CarrierSyncJob
        {
            CarrierId = carrierId,
            JobType = jobType,
            StartTime = DateTime.UtcNow,
            Status = "Running"
        };

        _context.CarrierSyncJobs.Add(job);
        await _context.SaveChangesAsync(cancellationToken);

        return MapSyncJobToDto(job, carrier.Code);
    }

    public async Task<CarrierSyncJobDto> CompleteSyncJobAsync(long jobId, int successCount, int failureCount, string? errorLog, CancellationToken cancellationToken = default)
    {
        var job = await _context.CarrierSyncJobs
            .Include(j => j.Carrier)
            .FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken)
            ?? throw new NotFoundException("CarrierSyncJob", jobId);

        job.EndTime = DateTime.UtcNow;
        job.Status = failureCount > 0 && successCount == 0 ? "Failed" : "Completed";
        job.TotalRecordsProcessed = successCount + failureCount;
        job.TotalSuccessCount = successCount;
        job.TotalFailureCount = failureCount;
        job.ErrorLog = errorLog;

        await _context.SaveChangesAsync(cancellationToken);
        return MapSyncJobToDto(job);
    }

    private async Task<Domain.Entities.Carrier.Carrier> ResolveCarrierAsync(string identifier, CancellationToken ct)
    {
        Domain.Entities.Carrier.Carrier? carrier;
        if (long.TryParse(identifier, out var id))
            carrier = await _context.Carriers.FirstOrDefaultAsync(c => c.Id == id, ct);
        else if (Guid.TryParse(identifier, out var pid))
            carrier = await _context.Carriers.FirstOrDefaultAsync(c => c.PublicId == pid, ct);
        else
            carrier = await _context.Carriers.FirstOrDefaultAsync(c => c.Code == identifier.ToUpperInvariant(), ct);
        return carrier ?? throw new NotFoundException("Carrier", identifier);
    }

    private static CarrierSyncJobDto MapSyncJobToDto(CarrierSyncJob j, string? carrierCode = null) => new()
    {
        Id = j.Id,
        CarrierId = j.CarrierId,
        CarrierCode = carrierCode ?? j.Carrier?.Code ?? "UNKNOWN",
        JobType = j.JobType,
        StartTime = j.StartTime,
        EndTime = j.EndTime,
        Status = j.Status,
        TotalRecordsProcessed = j.TotalRecordsProcessed,
        TotalSuccessCount = j.TotalSuccessCount,
        TotalFailureCount = j.TotalFailureCount,
        ErrorLog = j.ErrorLog,
        CreatedAt = j.CreatedAt
    };
}

// ═══════════════════════════════════════════════════════════════
//  CARRIER TRACKING SYNC ORCHESTRATOR
// ═══════════════════════════════════════════════════════════════

public class CarrierTrackingSyncOrchestrator : ICarrierTrackingSyncOrchestrator
{
    private readonly ApplicationDbContext _context;
    private readonly ICarrierAdapterFactory _adapterFactory;
    private readonly ICarrierSyncJobService _syncJobService;
    private readonly ILogger<CarrierTrackingSyncOrchestrator> _logger;

    public CarrierTrackingSyncOrchestrator(
        ApplicationDbContext context,
        ICarrierAdapterFactory adapterFactory,
        ICarrierSyncJobService syncJobService,
        ILogger<CarrierTrackingSyncOrchestrator> logger)
    {
        _context = context;
        _adapterFactory = adapterFactory;
        _syncJobService = syncJobService;
        _logger = logger;
    }

    public async Task SyncAllActiveCarriersAsync(CancellationToken cancellationToken = default)
    {
        var activeCarriers = await _context.Carriers
            .AsNoTracking()
            .Where(c => c.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var carrier in activeCarriers)
        {
            try
            {
                await SyncCarrierTrackingAsync(carrier, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tracking sync failed for carrier {CarrierCode}.", carrier.Code);
            }
        }
    }

    private async Task SyncCarrierTrackingAsync(Domain.Entities.Carrier.Carrier carrier, CancellationToken ct)
    {
        var inTransitShipments = await _context.Shipments
            .Where(s => s.AssignedCarrierId == carrier.Id
                && s.CarrierTrackingNumber != null
                && s.ShipmentStatus != "Delivered"
                && s.ShipmentStatus != "Cancelled"
                && s.ShipmentStatus != "Returned")
            .ToListAsync(ct);

        if (inTransitShipments.Count == 0) return;

        var job = await _syncJobService.StartSyncJobAsync(carrier.Id, "TrackingPoll", ct);
        int successCount = 0, failureCount = 0;
        var errors = new List<string>();

        var adapter = _adapterFactory.GetAdapter(carrier.Code);

        foreach (var shipment in inTransitShipments)
        {
            try
            {
                var tracking = await adapter.GetTrackingAsync(shipment.CarrierTrackingNumber!, ct);

                foreach (var evt in tracking.Events)
                {
                    var eventExists = await _context.CarrierTrackingEvents.AnyAsync(
                        e => e.CarrierId == carrier.Id
                            && e.CarrierTrackingNumber == shipment.CarrierTrackingNumber
                            && e.EventCode == evt.StatusCode
                            && e.EventTimestamp == evt.Timestamp, ct);

                    if (!eventExists)
                    {
                        _context.CarrierTrackingEvents.Add(new CarrierTrackingEvent
                        {
                            CarrierId = carrier.Id,
                            CarrierTrackingNumber = shipment.CarrierTrackingNumber!,
                            ExternalEventId = Guid.NewGuid().ToString("N"),
                            EventCode = evt.StatusCode,
                            EventDescription = evt.StatusDescription,
                            EventTimestamp = evt.Timestamp,
                            LocationName = evt.Location,
                            RawPayloadJson = System.Text.Json.JsonSerializer.Serialize(evt),
                            ProcessedStatus = "Processed",
                            ProcessedShipmentId = shipment.Id,
                            ProcessedAt = DateTime.UtcNow
                        });
                    }
                }

                // Map carrier status to internal status via tracking mappings
                var mapping = await _context.CarrierTrackingMappings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.CarrierId == carrier.Id
                        && m.ExternalStatusCode == tracking.CurrentStatus
                        && m.IsActive, ct);

                if (mapping != null && mapping.InternalShipmentStatus != shipment.ShipmentStatus)
                {
                    shipment.ShipmentStatus = mapping.InternalShipmentStatus;
                    shipment.UpdatedAt = DateTime.UtcNow;
                    shipment.UpdatedBy = "System.TrackingSync";
                }

                successCount++;
            }
            catch (Exception ex)
            {
                failureCount++;
                errors.Add($"{shipment.TrackingNumber}: {ex.Message}");
                _logger.LogWarning(ex, "Tracking sync failed for shipment {TrackingNumber}.", shipment.TrackingNumber);
            }
        }

        await _context.SaveChangesAsync(ct);
        await _syncJobService.CompleteSyncJobAsync(job.Id, successCount, failureCount,
            errors.Count > 0 ? string.Join("\n", errors) : null, ct);

        _logger.LogInformation("Tracking sync for {CarrierCode}: {Success} success, {Failure} failures.",
            carrier.Code, successCount, failureCount);
    }
}
