using Logistics.Server.Application.Common.Models;
using Logistics.Server.Application.Modules.Sales.DTOs;
using Logistics.Shared.Contracts.Pagination;

namespace Logistics.Server.Application.Modules.Sales.Services;

public interface IOrderService
{
    Task<PagedResult<OrderDto>> GetOrdersAsync(
        string? search,
        string? orderStatus,
        string? paymentStatus,
        long? customerId,
        long? branchId,
        DateTime? dateFrom,
        DateTime? dateTo,
        PagedRequest request,
        CancellationToken cancellationToken = default);

    Task<OrderDetailDto?> GetByIdOrPublicIdAsync(string identifier, CancellationToken cancellationToken = default);
    Task<OrderDetailDto?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);
    Task<OrderDetailDto> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);
    Task<OrderDetailDto> UpdateAsync(string identifier, UpdateOrderRequest request, CancellationToken cancellationToken = default);
    Task<OrderDetailDto> ConfirmAsync(string identifier, CancellationToken cancellationToken = default);
    Task<OrderDetailDto> CancelAsync(string identifier, CancelOrderRequest request, CancellationToken cancellationToken = default);
}

public interface IOrderItemService
{
    Task<IReadOnlyList<OrderItemDto>> GetItemsAsync(string orderIdentifier, CancellationToken cancellationToken = default);
    Task<OrderItemDto?> GetItemByIdAsync(string orderIdentifier, string itemIdentifier, CancellationToken cancellationToken = default);
    Task<OrderItemDto> AddItemAsync(string orderIdentifier, CreateOrderItemRequest request, CancellationToken cancellationToken = default);
    Task<OrderItemDto> UpdateItemAsync(string orderIdentifier, string itemIdentifier, UpdateOrderItemRequest request, CancellationToken cancellationToken = default);
    Task RemoveItemAsync(string orderIdentifier, string itemIdentifier, CancellationToken cancellationToken = default);
}

public interface IOrderPackageService
{
    Task<IReadOnlyList<OrderPackageDto>> GetPackagesAsync(string orderIdentifier, CancellationToken cancellationToken = default);
    Task<OrderPackageDto?> GetPackageByIdAsync(string orderIdentifier, string packageIdentifier, CancellationToken cancellationToken = default);
    Task<OrderPackageDto> AddPackageAsync(string orderIdentifier, CreateOrderPackageRequest request, CancellationToken cancellationToken = default);
    Task<OrderPackageDto> UpdatePackageAsync(string orderIdentifier, string packageIdentifier, UpdateOrderPackageRequest request, CancellationToken cancellationToken = default);
    Task RemovePackageAsync(string orderIdentifier, string packageIdentifier, CancellationToken cancellationToken = default);
}

public interface IOrderAddressService
{
    Task<IReadOnlyList<OrderAddressDto>> GetAddressesAsync(string orderIdentifier, CancellationToken cancellationToken = default);
    Task<OrderAddressDto?> GetAddressByTypeAsync(string orderIdentifier, string addressType, CancellationToken cancellationToken = default);
    Task<OrderAddressDto> SetAddressAsync(string orderIdentifier, string addressType, OrderAddressRequest request, CancellationToken cancellationToken = default);
}
