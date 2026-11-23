using Logistics.Server.Application.Common.Models;
using Logistics.Server.Application.Modules.Warehouse.DTOs;
using Logistics.Shared.Contracts.Pagination;

namespace Logistics.Server.Application.Modules.Warehouse.Services;

/// <summary>
/// Manages physical facilities: warehouses, storage/sorting zones, and bin locations.
/// </summary>
public interface IWarehouseManagementService
{
    Task<PagedResult<WarehouseDto>> GetWarehousesAsync(string? search, long? branchId, string? type, bool? isActive, PagedRequest request, CancellationToken cancellationToken = default);
    Task<WarehouseDetailDto?> GetWarehouseByIdOrCodeAsync(string identifier, CancellationToken cancellationToken = default);
    Task<WarehouseDetailDto> CreateWarehouseAsync(CreateWarehouseRequest request, CancellationToken cancellationToken = default);
    Task<WarehouseDetailDto> UpdateWarehouseAsync(string identifier, UpdateWarehouseRequest request, CancellationToken cancellationToken = default);
    Task<WarehouseDetailDto> ToggleWarehouseStatusAsync(string identifier, bool isActive, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WarehouseZoneDto>> GetZonesAsync(string warehouseIdentifier, CancellationToken cancellationToken = default);
    Task<WarehouseZoneDto?> GetZoneByIdAsync(string warehouseIdentifier, long zoneId, CancellationToken cancellationToken = default);
    Task<WarehouseZoneDto> CreateZoneAsync(string warehouseIdentifier, CreateWarehouseZoneRequest request, CancellationToken cancellationToken = default);
    Task<WarehouseZoneDto> UpdateZoneAsync(string warehouseIdentifier, long zoneId, UpdateWarehouseZoneRequest request, CancellationToken cancellationToken = default);
    Task<WarehouseZoneDto> ToggleZoneStatusAsync(string warehouseIdentifier, long zoneId, bool isActive, CancellationToken cancellationToken = default);

    Task<PagedResult<WarehouseLocationDto>> GetLocationsAsync(string warehouseIdentifier, long? zoneId, PagedRequest request, CancellationToken cancellationToken = default);
    Task<WarehouseLocationDto?> GetLocationByIdAsync(string warehouseIdentifier, long locationId, CancellationToken cancellationToken = default);
    Task<WarehouseLocationDto> CreateLocationAsync(string warehouseIdentifier, long zoneId, CreateWarehouseLocationRequest request, CancellationToken cancellationToken = default);
    Task<WarehouseLocationDto> UpdateLocationAsync(string warehouseIdentifier, long locationId, UpdateWarehouseLocationRequest request, CancellationToken cancellationToken = default);
    Task<WarehouseLocationDto> ToggleLocationBlockAsync(string warehouseIdentifier, long locationId, bool isBlocked, CancellationToken cancellationToken = default);
}

/// <summary>
/// Physical inventory operations: item rules, stock ledger, balance calculations, receipts, issues, adjustments, transfers, reservations, lots, and serial numbers.
/// </summary>
public interface IInventoryService
{
    Task<PagedResult<InventoryItemDto>> GetInventoryItemsAsync(string? search, long? warehouseId, bool? isActive, PagedRequest request, CancellationToken cancellationToken = default);
    Task<InventoryItemDto?> GetInventoryItemByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<InventoryItemDto> CreateInventoryItemAsync(CreateInventoryItemRequest request, CancellationToken cancellationToken = default);
    Task<InventoryItemDto> UpdateInventoryItemAsync(long id, UpdateInventoryItemRequest request, CancellationToken cancellationToken = default);

    Task<PagedResult<InventoryBalanceDto>> GetBalancesAsync(long? warehouseId, long? locationId, long? productId, long? lotId, PagedRequest request, CancellationToken cancellationToken = default);
    Task<InventoryBalanceDto?> GetBalanceByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<PagedResult<InventoryTransactionDto>> GetTransactionsAsync(long? warehouseId, string? transactionType, string? referenceType, DateTime? from, DateTime? to, PagedRequest request, CancellationToken cancellationToken = default);
    Task<InventoryTransactionDto?> GetTransactionByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<PagedResult<WarehouseReceiptDto>> GetReceiptsAsync(long? warehouseId, string? status, PagedRequest request, CancellationToken cancellationToken = default);
    Task<WarehouseReceiptDto?> GetReceiptByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<WarehouseReceiptDto> CreateReceiptAsync(CreateWarehouseReceiptRequest request, CancellationToken cancellationToken = default);
    Task<WarehouseReceiptDto> ConfirmReceiptAsync(long receiptId, ConfirmReceiptRequest request, CancellationToken cancellationToken = default);

    Task<PagedResult<StockAdjustmentDto>> GetAdjustmentsAsync(long? warehouseId, string? status, PagedRequest request, CancellationToken cancellationToken = default);
    Task<StockAdjustmentDto?> GetAdjustmentByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<StockAdjustmentDto> CreateAdjustmentAsync(CreateStockAdjustmentRequest request, CancellationToken cancellationToken = default);
    Task<StockAdjustmentDto> ApproveAdjustmentAsync(long adjustmentId, long employeeId, CancellationToken cancellationToken = default);

    Task<PagedResult<StockTransferDto>> GetTransfersAsync(long? sourceWarehouseId, long? destinationWarehouseId, string? status, PagedRequest request, CancellationToken cancellationToken = default);
    Task<StockTransferDto?> GetTransferByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<StockTransferDto> CreateTransferAsync(CreateStockTransferRequest request, CancellationToken cancellationToken = default);
    Task<StockTransferDto> ConfirmTransferAsync(long transferId, long employeeId, CancellationToken cancellationToken = default);
    Task<StockTransferDto> ReceiveTransferAsync(long transferId, ReceiveStockTransferRequest request, long employeeId, CancellationToken cancellationToken = default);

    Task<PagedResult<InventoryReservationDto>> GetReservationsAsync(long? warehouseId, long? productId, long? orderId, string? status, PagedRequest request, CancellationToken cancellationToken = default);
    Task<InventoryReservationDto> CreateReservationAsync(CreateInventoryReservationRequest request, CancellationToken cancellationToken = default);
    Task<InventoryReservationDto> ReleaseReservationAsync(long reservationId, CancellationToken cancellationToken = default);
    Task<InventoryReservationDto> ConsumeReservationAsync(long reservationId, CancellationToken cancellationToken = default);

    Task<PagedResult<InventoryLotDto>> GetLotsAsync(long? productId, PagedRequest request, CancellationToken cancellationToken = default);
    Task<InventoryLotDto> CreateLotAsync(CreateInventoryLotRequest request, CancellationToken cancellationToken = default);

    Task<PagedResult<InventorySerialNumberDto>> GetSerialNumbersAsync(long? productId, long? warehouseId, string? status, PagedRequest request, CancellationToken cancellationToken = default);
    Task<InventorySerialNumberDto> CreateSerialNumberAsync(CreateInventorySerialNumberRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Customer parcel sorting, scanning, staging, dispatching, and exceptions at distribution facilities.
/// </summary>
public interface IParcelHandlingService
{
    Task<ScanPackageResultDto> ScanPackageAsync(ScanPackageRequest request, CancellationToken cancellationToken = default);
    Task<ParcelHandlingEventDto> ReceiveParcelAsync(ParcelReceiveRequest request, CancellationToken cancellationToken = default);
    Task<ParcelHandlingEventDto> SortParcelAsync(ParcelSortRequest request, CancellationToken cancellationToken = default);
    Task<ParcelHandlingEventDto> StageParcelAsync(ParcelStageRequest request, CancellationToken cancellationToken = default);
    Task<ParcelHandlingEventDto> RecordExceptionAsync(ParcelExceptionRequest request, CancellationToken cancellationToken = default);

    Task<PagedResult<ParcelHandlingEventDto>> GetEventsAsync(long? shipmentPackageId, long? shipmentId, long? warehouseId, string? eventType, PagedRequest request, CancellationToken cancellationToken = default);

    Task<PagedResult<WarehouseDispatchDto>> GetDispatchesAsync(long? warehouseId, string? dispatchType, string? status, PagedRequest request, CancellationToken cancellationToken = default);
    Task<WarehouseDispatchDto?> GetDispatchByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<WarehouseDispatchDto> CreateDispatchAsync(CreateWarehouseDispatchDto request, CancellationToken cancellationToken = default);
    Task<WarehouseDispatchDto> ConfirmDispatchAsync(long dispatchId, long employeeId, CancellationToken cancellationToken = default);
}
