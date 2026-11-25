using Logistics.Client.Application.Contracts.Carrier;
using Logistics.Client.Application.Contracts.Catalog;
using Logistics.Client.Application.Contracts.Crm;
using Logistics.Client.Application.Contracts.Sales;
using Logistics.Client.Application.Contracts.Shipping;
using Logistics.Client.Application.Interfaces;
using Logistics.Shared.Contracts;
using Logistics.Shared.Contracts.Pagination;

namespace Logistics.Client.Infrastructure.Api;

public sealed class CrmApiClient(IApiClient api) : ICrmApiClient
{
    public async Task<PagedResponse<CustomerDto>> GetCustomersAsync(string? search, string? customerType, bool? isActive, long? branchId, PagedRequest paged, CancellationToken ct = default)
    {
        var url = ApiClientHelpers.BuildUrl("api/v1/customers",
            ("search", search), ("customerType", customerType), ("isActive", isActive), ("branchId", branchId), ("page", paged.Page), ("pageSize", paged.PageSize));
        return ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<CustomerDto>>>(url, ct), "GET customers");
    }

    public async Task<CustomerDetailDto> GetCustomerDetailAsync(string id, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<CustomerDetailDto>>($"api/v1/customers/{Uri.EscapeDataString(id)}?includeDetails=true", ct), "GET customer detail");

    public async Task<CustomerOverviewDto> GetCustomerOverviewAsync(CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<CustomerOverviewDto>>("api/v1/customers/overview", ct), "GET customer overview");

    public async Task<CustomerDto> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CreateCustomerRequest, ApiResponse<CustomerDto>>("api/v1/customers", request, ct), "POST customer");

    public async Task<CustomerDto> UpdateCustomerAsync(string id, UpdateCustomerRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PutAsync<UpdateCustomerRequest, ApiResponse<CustomerDto>>($"api/v1/customers/{Uri.EscapeDataString(id)}", request, ct), "PUT customer");

    public async Task<CustomerDto> ToggleCustomerStatusAsync(string id, UpdateCustomerStatusRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PatchAsync<UpdateCustomerStatusRequest, ApiResponse<CustomerDto>>($"api/v1/customers/{Uri.EscapeDataString(id)}/status", request, ct), "PATCH customer status");

    public async Task<IReadOnlyList<CustomerContactDto>> GetContactsAsync(string customerId, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<IReadOnlyList<CustomerContactDto>>>($"api/v1/customers/{Uri.EscapeDataString(customerId)}/contacts", ct), "GET contacts");

    public async Task<CustomerContactDto> CreateContactAsync(string customerId, CreateCustomerContactRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CreateCustomerContactRequest, ApiResponse<CustomerContactDto>>($"api/v1/customers/{Uri.EscapeDataString(customerId)}/contacts", request, ct), "POST contact");

    public async Task<CustomerContactDto> UpdateContactAsync(string customerId, string contactId, UpdateCustomerContactRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PutAsync<UpdateCustomerContactRequest, ApiResponse<CustomerContactDto>>($"api/v1/customers/{Uri.EscapeDataString(customerId)}/contacts/{Uri.EscapeDataString(contactId)}", request, ct), "PUT contact");

    public async Task<CustomerContactDto> SetPrimaryContactAsync(string customerId, string contactId, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PatchAsync<object?, ApiResponse<CustomerContactDto>>($"api/v1/customers/{Uri.EscapeDataString(customerId)}/contacts/{Uri.EscapeDataString(contactId)}/primary", null, ct), "PATCH primary contact");

    public Task DeleteContactAsync(string customerId, string contactId, CancellationToken ct = default)
        => api.DeleteAsync($"api/v1/customers/{Uri.EscapeDataString(customerId)}/contacts/{Uri.EscapeDataString(contactId)}", ct);

    public async Task<IReadOnlyList<CustomerAddressDto>> GetAddressesAsync(string customerId, string? addressType = null, CancellationToken ct = default)
    {
        var url = ApiClientHelpers.BuildUrl($"api/v1/customers/{Uri.EscapeDataString(customerId)}/addresses", ("addressType", addressType));
        return ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<IReadOnlyList<CustomerAddressDto>>>(url, ct), "GET addresses");
    }

    public async Task<CustomerAddressDto> CreateAddressAsync(string customerId, CreateCustomerAddressRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CreateCustomerAddressRequest, ApiResponse<CustomerAddressDto>>($"api/v1/customers/{Uri.EscapeDataString(customerId)}/addresses", request, ct), "POST address");

    public async Task<CustomerAddressDto> UpdateAddressAsync(string customerId, string addressId, UpdateCustomerAddressRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PutAsync<UpdateCustomerAddressRequest, ApiResponse<CustomerAddressDto>>($"api/v1/customers/{Uri.EscapeDataString(customerId)}/addresses/{Uri.EscapeDataString(addressId)}", request, ct), "PUT address");

    public async Task<CustomerAddressDto> SetDefaultAddressAsync(string customerId, string addressId, SetDefaultAddressRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PatchAsync<SetDefaultAddressRequest, ApiResponse<CustomerAddressDto>>($"api/v1/customers/{Uri.EscapeDataString(customerId)}/addresses/{Uri.EscapeDataString(addressId)}/default", request, ct), "PATCH default address");

    public Task DeleteAddressAsync(string customerId, string addressId, CancellationToken ct = default)
        => api.DeleteAsync($"api/v1/customers/{Uri.EscapeDataString(customerId)}/addresses/{Uri.EscapeDataString(addressId)}", ct);

    public async Task<IReadOnlyList<CustomerContractDto>> GetContractsAsync(string customerId, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<IReadOnlyList<CustomerContractDto>>>($"api/v1/customers/{Uri.EscapeDataString(customerId)}/contracts", ct), "GET contracts");

    public async Task<CustomerContractDto> CreateContractAsync(string customerId, CreateCustomerContractRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CreateCustomerContractRequest, ApiResponse<CustomerContractDto>>($"api/v1/customers/{Uri.EscapeDataString(customerId)}/contracts", request, ct), "POST contract");

    public async Task<CustomerContractDto> UpdateContractAsync(string customerId, string contractId, UpdateCustomerContractRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PutAsync<UpdateCustomerContractRequest, ApiResponse<CustomerContractDto>>($"api/v1/customers/{Uri.EscapeDataString(customerId)}/contracts/{Uri.EscapeDataString(contractId)}", request, ct), "PUT contract");

    public async Task<CustomerContractDto> UpdateContractStatusAsync(string customerId, string contractId, UpdateCustomerContractStatusRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PatchAsync<UpdateCustomerContractStatusRequest, ApiResponse<CustomerContractDto>>($"api/v1/customers/{Uri.EscapeDataString(customerId)}/contracts/{Uri.EscapeDataString(contractId)}/status", request, ct), "PATCH contract status");

    public async Task<IReadOnlyList<CustomerPricingRuleDto>> GetPricingRulesAsync(string customerId, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<IReadOnlyList<CustomerPricingRuleDto>>>($"api/v1/customers/{Uri.EscapeDataString(customerId)}/pricing-rules", ct), "GET pricing rules");

    public async Task<CustomerPricingRuleDto> CreatePricingRuleAsync(string customerId, CreateCustomerPricingRuleRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CreateCustomerPricingRuleRequest, ApiResponse<CustomerPricingRuleDto>>($"api/v1/customers/{Uri.EscapeDataString(customerId)}/pricing-rules", request, ct), "POST pricing rule");

    public async Task<CustomerPricingRuleDto> UpdatePricingRuleAsync(string customerId, string ruleId, UpdateCustomerPricingRuleRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PutAsync<UpdateCustomerPricingRuleRequest, ApiResponse<CustomerPricingRuleDto>>($"api/v1/customers/{Uri.EscapeDataString(customerId)}/pricing-rules/{Uri.EscapeDataString(ruleId)}", request, ct), "PUT pricing rule");

    public async Task<CustomerPricingRuleDto> TogglePricingRuleStatusAsync(string customerId, string ruleId, UpdateCustomerPricingRuleStatusRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PatchAsync<UpdateCustomerPricingRuleStatusRequest, ApiResponse<CustomerPricingRuleDto>>($"api/v1/customers/{Uri.EscapeDataString(customerId)}/pricing-rules/{Uri.EscapeDataString(ruleId)}/status", request, ct), "PATCH pricing rule status");
}

public sealed class CatalogApiClient(IApiClient api) : ICatalogApiClient
{
    public async Task<PagedResponse<CategoryDto>> GetCategoriesAsync(string? search, bool? isActive, PagedRequest paged, CancellationToken ct = default)
    {
        var url = ApiClientHelpers.BuildUrl("api/v1/catalog/categories",
            ("search", search), ("isActive", isActive), ("page", paged.Page), ("pageSize", paged.PageSize));
        return ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<CategoryDto>>>(url, ct), "GET categories");
    }

    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CreateCategoryRequest, ApiResponse<CategoryDto>>("api/v1/catalog/categories", request, ct), "POST category");

    public async Task<CategoryDto> UpdateCategoryAsync(string id, UpdateCategoryRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PutAsync<UpdateCategoryRequest, ApiResponse<CategoryDto>>($"api/v1/catalog/categories/{Uri.EscapeDataString(id)}", request, ct), "PUT category");

    public async Task<CategoryDto> ToggleCategoryStatusAsync(string id, UpdateCategoryStatusRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PatchAsync<UpdateCategoryStatusRequest, ApiResponse<CategoryDto>>($"api/v1/catalog/categories/{Uri.EscapeDataString(id)}/status", request, ct), "PATCH category status");

    public async Task<PagedResponse<ProductDto>> GetProductsAsync(string? search, long? categoryId, bool? isActive, PagedRequest paged, CancellationToken ct = default)
    {
        var url = ApiClientHelpers.BuildUrl("api/v1/catalog/products",
            ("search", search), ("categoryId", categoryId), ("isActive", isActive), ("page", paged.Page), ("pageSize", paged.PageSize));
        return ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<ProductDto>>>(url, ct), "GET products");
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CreateProductRequest, ApiResponse<ProductDto>>("api/v1/catalog/products", request, ct), "POST product");

    public async Task<ProductDto> UpdateProductAsync(string id, UpdateProductRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PutAsync<UpdateProductRequest, ApiResponse<ProductDto>>($"api/v1/catalog/products/{Uri.EscapeDataString(id)}", request, ct), "PUT product");

    public async Task<ProductDto> ToggleProductStatusAsync(string id, UpdateProductStatusRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PatchAsync<UpdateProductStatusRequest, ApiResponse<ProductDto>>($"api/v1/catalog/products/{Uri.EscapeDataString(id)}/status", request, ct), "PATCH product status");

    public async Task<PagedResponse<UnitOfMeasureDto>> GetUnitsAsync(string? search, string? type, bool? isActive, PagedRequest paged, CancellationToken ct = default)
    {
        var url = ApiClientHelpers.BuildUrl("api/v1/catalog/units",
            ("search", search), ("type", type), ("isActive", isActive), ("page", paged.Page), ("pageSize", paged.PageSize));
        return ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<UnitOfMeasureDto>>>(url, ct), "GET units");
    }

    public async Task<UnitOfMeasureDto> CreateUnitAsync(CreateUnitOfMeasureRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CreateUnitOfMeasureRequest, ApiResponse<UnitOfMeasureDto>>("api/v1/catalog/units", request, ct), "POST unit");

    public async Task<UnitOfMeasureDto> UpdateUnitAsync(long id, UpdateUnitOfMeasureRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PutAsync<UpdateUnitOfMeasureRequest, ApiResponse<UnitOfMeasureDto>>($"api/v1/catalog/units/{id}", request, ct), "PUT unit");

    public async Task<UnitOfMeasureDto> ToggleUnitStatusAsync(long id, UpdateUnitStatusRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PatchAsync<UpdateUnitStatusRequest, ApiResponse<UnitOfMeasureDto>>($"api/v1/catalog/units/{id}/status", request, ct), "PATCH unit status");

    public async Task<IReadOnlyList<ServiceDto>> GetServicesAsync(bool activeOnly = false, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<IReadOnlyList<ServiceDto>>>(ApiClientHelpers.BuildUrl("api/v1/catalog/services", ("activeOnly", activeOnly)), ct), "GET services");
}

public sealed class SalesApiClient(IApiClient api) : ISalesApiClient
{
    public async Task<PagedResponse<OrderDto>> GetOrdersAsync(string? search, string? orderStatus, string? paymentStatus, long? customerId, long? branchId, DateTime? dateFrom, DateTime? dateTo, PagedRequest paged, CancellationToken ct = default)
    {
        var url = ApiClientHelpers.BuildUrl("api/v1/orders",
            ("search", search), ("orderStatus", orderStatus), ("paymentStatus", paymentStatus), ("customerId", customerId),
            ("branchId", branchId), ("dateFrom", dateFrom), ("dateTo", dateTo), ("page", paged.Page), ("pageSize", paged.PageSize));
        return ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<OrderDto>>>(url, ct), "GET orders");
    }

    public async Task<OrderDetailDto> GetOrderDetailAsync(string id, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<OrderDetailDto>>($"api/v1/orders/{Uri.EscapeDataString(id)}", ct), "GET order detail");

    public async Task<OrderDetailDto> CreateOrderAsync(CreateOrderRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CreateOrderRequest, ApiResponse<OrderDetailDto>>("api/v1/orders", request, ct), "POST order");

    public async Task<OrderDetailDto> UpdateOrderAsync(string id, UpdateOrderRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PutAsync<UpdateOrderRequest, ApiResponse<OrderDetailDto>>($"api/v1/orders/{Uri.EscapeDataString(id)}", request, ct), "PUT order");

    public async Task<OrderDetailDto> ConfirmOrderAsync(string id, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostEmptyAsync<ApiResponse<OrderDetailDto>>($"api/v1/orders/{Uri.EscapeDataString(id)}/confirm", ct), "POST order confirm");

    public async Task<OrderDetailDto> CancelOrderAsync(string id, CancelOrderRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CancelOrderRequest, ApiResponse<OrderDetailDto>>($"api/v1/orders/{Uri.EscapeDataString(id)}/cancel", request, ct), "POST order cancel");

    public async Task<OrderItemDto> AddOrderItemAsync(string orderId, CreateOrderItemRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CreateOrderItemRequest, ApiResponse<OrderItemDto>>($"api/v1/orders/{Uri.EscapeDataString(orderId)}/items", request, ct), "POST order item");

    public Task DeleteOrderItemAsync(string orderId, string itemId, CancellationToken ct = default)
        => api.DeleteAsync($"api/v1/orders/{Uri.EscapeDataString(orderId)}/items/{Uri.EscapeDataString(itemId)}", ct);

    public async Task<OrderPackageDto> AddOrderPackageAsync(string orderId, CreateOrderPackageRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CreateOrderPackageRequest, ApiResponse<OrderPackageDto>>($"api/v1/orders/{Uri.EscapeDataString(orderId)}/packages", request, ct), "POST order package");

    public Task DeleteOrderPackageAsync(string orderId, string packageId, CancellationToken ct = default)
        => api.DeleteAsync($"api/v1/orders/{Uri.EscapeDataString(orderId)}/packages/{Uri.EscapeDataString(packageId)}", ct);
}

public sealed class ShippingApiClient(IApiClient api) : IShippingApiClient
{
    public async Task<PagedResponse<ShipmentDto>> GetShipmentsAsync(string? search, string? status, long? customerId, long? originBranchId, long? destinationBranchId, DateTime? dateFrom, DateTime? dateTo, PagedRequest paged, CancellationToken ct = default)
    {
        var url = ApiClientHelpers.BuildUrl("api/v1/shipments",
            ("search", search), ("status", status), ("customerId", customerId),
            ("originBranchId", originBranchId), ("destinationBranchId", destinationBranchId),
            ("dateFrom", dateFrom), ("dateTo", dateTo), ("page", paged.Page), ("pageSize", paged.PageSize));
        return ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<ShipmentDto>>>(url, ct), "GET shipments");
    }

    public async Task<ShipmentDetailDto> GetShipmentDetailAsync(string idOrTracking, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<ShipmentDetailDto>>($"api/v1/shipments/{Uri.EscapeDataString(idOrTracking)}", ct), "GET shipment detail");

    public async Task<ShipmentDetailDto> CreateFromOrderAsync(CreateShipmentFromOrderRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CreateShipmentFromOrderRequest, ApiResponse<ShipmentDetailDto>>("api/v1/shipments/from-order", request, ct), "POST shipment from-order");

    public async Task<ShipmentDetailDto> TransitionStatusAsync(string id, ShipmentStatusTransitionRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<ShipmentStatusTransitionRequest, ApiResponse<ShipmentDetailDto>>($"api/v1/shipments/{Uri.EscapeDataString(id)}/transitions", request, ct), "POST shipment transition");

    public async Task<ShipmentDetailDto> CancelShipmentAsync(string id, CancelShipmentRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CancelShipmentRequest, ApiResponse<ShipmentDetailDto>>($"api/v1/shipments/{Uri.EscapeDataString(id)}/cancel", request, ct), "POST shipment cancel");

    public async Task<IReadOnlyList<ShipmentStatusHistoryDto>> GetStatusHistoryAsync(string id, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<IReadOnlyList<ShipmentStatusHistoryDto>>>($"api/v1/shipments/{Uri.EscapeDataString(id)}/history", ct), "GET shipment history");

    public async Task<ShipmentPackageDto> AddPackageAsync(string shipmentId, AddShipmentPackageRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<AddShipmentPackageRequest, ApiResponse<ShipmentPackageDto>>($"api/v1/shipments/{Uri.EscapeDataString(shipmentId)}/packages", request, ct), "POST shipment package");

    public async Task<ShipmentAssignmentDto> AssignAsync(string shipmentId, CreateShipmentAssignmentRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CreateShipmentAssignmentRequest, ApiResponse<ShipmentAssignmentDto>>($"api/v1/shipments/{Uri.EscapeDataString(shipmentId)}/assignments", request, ct), "POST shipment assignment");

    public async Task<ShipmentRouteDto> AddRouteAsync(string shipmentId, AddShipmentRouteRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<AddShipmentRouteRequest, ApiResponse<ShipmentRouteDto>>($"api/v1/shipments/{Uri.EscapeDataString(shipmentId)}/routes", request, ct), "POST shipment route");

    public async Task<IReadOnlyList<DeliveryAttemptDto>> GetDeliveryAttemptsAsync(string shipmentId, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<IReadOnlyList<DeliveryAttemptDto>>>($"api/v1/shipments/{Uri.EscapeDataString(shipmentId)}/delivery-attempts", ct), "GET delivery attempts");

    public async Task<DeliveryAttemptDto> RecordDeliveryAttemptAsync(string shipmentId, RecordDeliveryAttemptRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<RecordDeliveryAttemptRequest, ApiResponse<DeliveryAttemptDto>>($"api/v1/shipments/{Uri.EscapeDataString(shipmentId)}/delivery-attempts", request, ct), "POST delivery attempt");

    public async Task<ProofOfDeliveryDto?> GetProofOfDeliveryAsync(string shipmentId, CancellationToken ct = default)
    {
        try
        {
            return (await api.GetAsync<ApiResponse<ProofOfDeliveryDto>>($"api/v1/shipments/{Uri.EscapeDataString(shipmentId)}/proof-of-delivery", ct))?.Data;
        }
        catch (Exceptions.ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<ProofOfDeliveryDto> CreateProofOfDeliveryAsync(string shipmentId, long attemptId, CreateProofOfDeliveryRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CreateProofOfDeliveryRequest, ApiResponse<ProofOfDeliveryDto>>($"api/v1/shipments/{Uri.EscapeDataString(shipmentId)}/proof-of-delivery?attemptId={attemptId}", request, ct), "POST proof-of-delivery");

    public async Task<PagedResponse<PickupRequestDto>> GetPickupRequestsAsync(string? search, string? status, PagedRequest paged, CancellationToken ct = default)
    {
        var url = ApiClientHelpers.BuildUrl("api/v1/pickup-requests", ("search", search), ("status", status), ("page", paged.Page), ("pageSize", paged.PageSize));
        return ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<PickupRequestDto>>>(url, ct), "GET pickup-requests");
    }

    public async Task<PickupRequestDto> CreatePickupRequestAsync(CreatePickupRequestRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CreatePickupRequestRequest, ApiResponse<PickupRequestDto>>("api/v1/pickup-requests", request, ct), "POST pickup-request");

    public async Task<PickupRequestDto> AssignPickupAsync(string id, long employeeId, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostEmptyAsync<ApiResponse<PickupRequestDto>>($"api/v1/pickup-requests/{Uri.EscapeDataString(id)}/assign/{employeeId}", ct), "POST assign pickup");

    public async Task<PickupRequestDto> ConfirmPickupAsync(string id, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostEmptyAsync<ApiResponse<PickupRequestDto>>($"api/v1/pickup-requests/{Uri.EscapeDataString(id)}/confirm", ct), "POST confirm pickup");

    public async Task<PickupRequestDto> CompletePickupAsync(string id, CompletePickupRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CompletePickupRequest, ApiResponse<PickupRequestDto>>($"api/v1/pickup-requests/{Uri.EscapeDataString(id)}/complete", request, ct), "POST complete pickup");

    public async Task<PickupRequestDto> CancelPickupAsync(string id, string reason, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostEmptyAsync<ApiResponse<PickupRequestDto>>(ApiClientHelpers.BuildUrl($"api/v1/pickup-requests/{Uri.EscapeDataString(id)}/cancel", ("reason", reason)), ct), "POST cancel pickup");

    public async Task<PagedResponse<ReturnRequestDto>> GetReturnRequestsAsync(string? search, string? status, PagedRequest paged, CancellationToken ct = default)
    {
        var url = ApiClientHelpers.BuildUrl("api/v1/returns", ("search", search), ("status", status), ("page", paged.Page), ("pageSize", paged.PageSize));
        return ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<ReturnRequestDto>>>(url, ct), "GET returns");
    }

    public async Task<ReturnRequestDto> CreateReturnRequestAsync(CreateReturnRequestRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CreateReturnRequestRequest, ApiResponse<ReturnRequestDto>>("api/v1/returns", request, ct), "POST return");

    public async Task<ReturnRequestDto> ProcessReturnRequestAsync(string id, ProcessReturnRequestRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<ProcessReturnRequestRequest, ApiResponse<ReturnRequestDto>>($"api/v1/returns/{Uri.EscapeDataString(id)}/process", request, ct), "POST process return");
}

public sealed class CarrierApiClient(IApiClient api) : ICarrierApiClient
{
    public async Task<PagedResponse<CarrierDto>> GetCarriersAsync(string? search, bool? isActive, PagedRequest paged, CancellationToken ct = default)
    {
        var url = ApiClientHelpers.BuildUrl("api/v1/carriers", ("search", search), ("isActive", isActive), ("page", paged.Page), ("pageSize", paged.PageSize));
        return ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<CarrierDto>>>(url, ct), "GET carriers");
    }

    public async Task<CarrierDetailDto> GetCarrierDetailAsync(string identifier, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<CarrierDetailDto>>($"api/v1/carriers/{Uri.EscapeDataString(identifier)}", ct), "GET carrier detail");

    public async Task<CarrierDetailDto> CreateCarrierAsync(CreateCarrierRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CreateCarrierRequest, ApiResponse<CarrierDetailDto>>("api/v1/carriers", request, ct), "POST carrier");

    public async Task<CarrierDetailDto> UpdateCarrierAsync(string identifier, UpdateCarrierRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PutAsync<UpdateCarrierRequest, ApiResponse<CarrierDetailDto>>($"api/v1/carriers/{Uri.EscapeDataString(identifier)}", request, ct), "PUT carrier");

    public async Task<CarrierDetailDto> ToggleCarrierStatusAsync(string identifier, bool isActive, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PatchAsync<object?, ApiResponse<CarrierDetailDto>>($"api/v1/carriers/{Uri.EscapeDataString(identifier)}/status?isActive={isActive.ToString().ToLowerInvariant()}", null, ct), "PATCH carrier status");

    public async Task<CarrierServiceDto> CreateCarrierServiceAsync(string carrierIdentifier, CreateCarrierServiceRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CreateCarrierServiceRequest, ApiResponse<CarrierServiceDto>>($"api/v1/carriers/{Uri.EscapeDataString(carrierIdentifier)}/services", request, ct), "POST carrier service");

    public async Task<CarrierServiceDto> ToggleCarrierServiceStatusAsync(string carrierIdentifier, long serviceId, bool isActive, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PatchAsync<object?, ApiResponse<CarrierServiceDto>>($"api/v1/carriers/{Uri.EscapeDataString(carrierIdentifier)}/services/{serviceId}/status?isActive={isActive.ToString().ToLowerInvariant()}", null, ct), "PATCH service status");

    public async Task<IReadOnlyList<CarrierConfigurationDto>> GetConfigurationsAsync(string carrierIdentifier, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<IReadOnlyList<CarrierConfigurationDto>>>($"api/v1/carriers/{Uri.EscapeDataString(carrierIdentifier)}/configurations", ct), "GET carrier configs");

    public async Task<CarrierConfigurationDto> UpsertConfigurationAsync(string carrierIdentifier, UpsertCarrierConfigurationRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PutAsync<UpsertCarrierConfigurationRequest, ApiResponse<CarrierConfigurationDto>>($"api/v1/carriers/{Uri.EscapeDataString(carrierIdentifier)}/configurations", request, ct), "PUT carrier config");

    public async Task<CarrierTrackingMappingDto> CreateTrackingMappingAsync(string carrierIdentifier, CreateTrackingMappingRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CreateTrackingMappingRequest, ApiResponse<CarrierTrackingMappingDto>>($"api/v1/carriers/{Uri.EscapeDataString(carrierIdentifier)}/tracking-mappings", request, ct), "POST tracking mapping");

    public async Task<CarrierBookingResultDto> BookShipmentAsync(CarrierBookShipmentRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CarrierBookShipmentRequest, ApiResponse<CarrierBookingResultDto>>("api/v1/carrier-integration/book", request, ct), "POST carrier book");

    public async Task<CarrierCancelResultDto> CancelShipmentAsync(CarrierCancelShipmentRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CarrierCancelShipmentRequest, ApiResponse<CarrierCancelResultDto>>("api/v1/carrier-integration/cancel", request, ct), "POST carrier cancel");

    public async Task<CarrierRateResultDto> QueryRatesAsync(CarrierRateQueryRequest request, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.PostAsync<CarrierRateQueryRequest, ApiResponse<CarrierRateResultDto>>("api/v1/carrier-integration/rates", request, ct), "POST carrier rates");

    public async Task<CarrierTrackingQueryDto> QueryTrackingAsync(string carrierCode, string trackingNumber, CancellationToken ct = default)
        => ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<CarrierTrackingQueryDto>>($"api/v1/carrier-integration/tracking/{Uri.EscapeDataString(carrierCode)}/{Uri.EscapeDataString(trackingNumber)}", ct), "GET carrier tracking");

    public async Task<PagedResponse<CarrierApiLogDto>> GetApiLogsAsync(string? carrierIdentifier, bool? isSuccess, int page, int pageSize, CancellationToken ct = default)
    {
        var url = ApiClientHelpers.BuildUrl("api/v1/carrier-monitoring/api-logs", ("carrierIdentifier", carrierIdentifier), ("isSuccess", isSuccess), ("page", page), ("pageSize", pageSize));
        return ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<CarrierApiLogDto>>>(url, ct), "GET api-logs");
    }

    public async Task<PagedResponse<CarrierWebhookLogDto>> GetWebhookLogsAsync(string? carrierIdentifier, bool? processedSuccessfully, int page, int pageSize, CancellationToken ct = default)
    {
        var url = ApiClientHelpers.BuildUrl("api/v1/carrier-monitoring/webhook-logs", ("carrierIdentifier", carrierIdentifier), ("processedSuccessfully", processedSuccessfully), ("page", page), ("pageSize", pageSize));
        return ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<CarrierWebhookLogDto>>>(url, ct), "GET webhook-logs");
    }

    public async Task<PagedResponse<CarrierSyncJobDto>> GetSyncJobsAsync(string? carrierIdentifier, string? status, int page, int pageSize, CancellationToken ct = default)
    {
        var url = ApiClientHelpers.BuildUrl("api/v1/carrier-monitoring/sync-jobs", ("carrierIdentifier", carrierIdentifier), ("status", status), ("page", page), ("pageSize", pageSize));
        return ApiClientHelpers.UnwrapRequired(await api.GetAsync<ApiResponse<PagedResponse<CarrierSyncJobDto>>>(url, ct), "GET sync-jobs");
    }
}
