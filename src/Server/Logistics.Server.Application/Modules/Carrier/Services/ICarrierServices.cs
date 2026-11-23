using Logistics.Server.Application.Common.Models;
using Logistics.Server.Application.Modules.Carrier.DTOs;
using Logistics.Shared.Contracts.Pagination;

namespace Logistics.Server.Application.Modules.Carrier.Services;

/// <summary>
/// CRUD operations for carrier records.
/// </summary>
public interface ICarrierManagementService
{
    Task<PagedResult<CarrierDto>> GetCarriersAsync(
        string? search, bool? isActive, PagedRequest request,
        CancellationToken cancellationToken = default);

    Task<CarrierDetailDto?> GetByIdOrCodeAsync(string identifier, CancellationToken cancellationToken = default);
    Task<CarrierDetailDto> CreateAsync(CreateCarrierRequest request, CancellationToken cancellationToken = default);
    Task<CarrierDetailDto> UpdateAsync(string identifier, UpdateCarrierRequest request, CancellationToken cancellationToken = default);
    Task<CarrierDetailDto> ToggleActiveAsync(string identifier, bool isActive, CancellationToken cancellationToken = default);
}

/// <summary>
/// CRUD for carrier service types.
/// </summary>
public interface ICarrierServiceManagementService
{
    Task<IReadOnlyList<CarrierServiceDto>> GetServicesAsync(string carrierIdentifier, CancellationToken cancellationToken = default);
    Task<CarrierServiceDto?> GetServiceByIdAsync(string carrierIdentifier, long serviceId, CancellationToken cancellationToken = default);
    Task<CarrierServiceDto> CreateServiceAsync(string carrierIdentifier, CreateCarrierServiceRequest request, CancellationToken cancellationToken = default);
    Task<CarrierServiceDto> UpdateServiceAsync(string carrierIdentifier, long serviceId, UpdateCarrierServiceRequest request, CancellationToken cancellationToken = default);
    Task<CarrierServiceDto> ToggleServiceActiveAsync(string carrierIdentifier, long serviceId, bool isActive, CancellationToken cancellationToken = default);
}

/// <summary>
/// Manages encrypted carrier API credentials and configuration.
/// </summary>
public interface ICarrierConfigurationService
{
    Task<IReadOnlyList<CarrierConfigurationDto>> GetConfigurationsAsync(string carrierIdentifier, CancellationToken cancellationToken = default);
    Task<CarrierConfigurationDto> UpsertConfigurationAsync(string carrierIdentifier, UpsertCarrierConfigurationRequest request, CancellationToken cancellationToken = default);
    Task DeleteConfigurationAsync(string carrierIdentifier, long configId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Manages carrier external status → internal shipment status mappings.
/// </summary>
public interface ICarrierTrackingMappingService
{
    Task<IReadOnlyList<CarrierTrackingMappingDto>> GetMappingsAsync(string carrierIdentifier, CancellationToken cancellationToken = default);
    Task<CarrierTrackingMappingDto> CreateMappingAsync(string carrierIdentifier, CreateTrackingMappingRequest request, CancellationToken cancellationToken = default);
    Task<CarrierTrackingMappingDto> UpdateMappingAsync(string carrierIdentifier, long mappingId, UpdateTrackingMappingRequest request, CancellationToken cancellationToken = default);
    Task DeleteMappingAsync(string carrierIdentifier, long mappingId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Integration-layer operations: booking, cancellation, rate calculation via carrier adapters.
/// </summary>
public interface ICarrierIntegrationService
{
    Task<CarrierBookingResultDto> BookShipmentAsync(CarrierBookShipmentRequest request, CancellationToken cancellationToken = default);
    Task<CarrierCancelResultDto> CancelShipmentAsync(CarrierCancelShipmentRequest request, CancellationToken cancellationToken = default);
    Task<CarrierRateResultDto> CalculateRatesAsync(CarrierRateQueryRequest request, CancellationToken cancellationToken = default);
    Task<CarrierTrackingQueryDto> GetTrackingAsync(string carrierCode, string trackingNumber, CancellationToken cancellationToken = default);
}

/// <summary>
/// Processes incoming carrier webhooks and maps events to shipment tracking.
/// </summary>
public interface ICarrierWebhookService
{
    Task<CarrierWebhookLogDto> ProcessWebhookAsync(string carrierCode, string payloadJson, string? signature, string clientIp, string webhookUrl, CancellationToken cancellationToken = default);
    Task<PagedResult<CarrierWebhookLogDto>> GetWebhookLogsAsync(string? carrierIdentifier, DateTime? from, DateTime? to, bool? processedSuccessfully, PagedRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Queries carrier API call logs.
/// </summary>
public interface ICarrierApiLogService
{
    Task<PagedResult<CarrierApiLogDto>> GetApiLogsAsync(string? carrierIdentifier, DateTime? from, DateTime? to, bool? isSuccess, PagedRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Manages and queries carrier synchronization jobs.
/// </summary>
public interface ICarrierSyncJobService
{
    Task<PagedResult<CarrierSyncJobDto>> GetSyncJobsAsync(string? carrierIdentifier, string? jobType, string? status, PagedRequest request, CancellationToken cancellationToken = default);
    Task<CarrierSyncJobDto> StartSyncJobAsync(long carrierId, string jobType, CancellationToken cancellationToken = default);
    Task<CarrierSyncJobDto> CompleteSyncJobAsync(long jobId, int successCount, int failureCount, string? errorLog, CancellationToken cancellationToken = default);
}

/// <summary>
/// Coordinates background tracking synchronization across all active carriers.
/// </summary>
public interface ICarrierTrackingSyncOrchestrator
{
    Task SyncAllActiveCarriersAsync(CancellationToken cancellationToken = default);
}
