using Logistics.Client.Application.Contracts.Auth;
using Logistics.Client.Application.Contracts.Carrier;
using Logistics.Client.Application.Contracts.Catalog;
using Logistics.Client.Application.Contracts.Cod;
using Logistics.Client.Application.Contracts.Crm;
using Logistics.Client.Application.Contracts.Finance;
using Logistics.Client.Application.Contracts.Notification;
using Logistics.Client.Application.Contracts.Organization;
using Logistics.Client.Application.Contracts.Reporting;
using Logistics.Client.Application.Contracts.Sales;
using Logistics.Client.Application.Contracts.Shipping;
using Logistics.Client.Application.Contracts.Warehouse;
using Logistics.Shared.Contracts.DevData;
using Logistics.Shared.Contracts.Health;
using Logistics.Shared.Contracts.Pagination;

namespace Logistics.Client.Application.Interfaces;

public interface IHealthApiClient
{
    Task<HealthCheckResponse?> GetHealthAsync(CancellationToken ct = default);
}

public interface IDevTestDataApiClient
{
    Task<DevTestDataStatusDto> GetStatusAsync(CancellationToken ct = default);
    Task<ExecuteDevSeedResultDto> ExecuteAsync(ExecuteDevSeedRequest request, CancellationToken ct = default);
    Task<DevTestDataIntegrityReportDto> VerifyIntegrityAsync(CancellationToken ct = default);
}

public interface IAuthSessionApiClient
{
    Task<UserProfileDto> GetProfileAsync(CancellationToken ct = default);
    Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<UserSessionDto>> GetSessionsAsync(CancellationToken ct = default);
    Task TerminateSessionAsync(long sessionId, CancellationToken ct = default);
}

public interface IOrganizationApiClient
{
    Task<CompanyDto?> GetCompanyAsync(CancellationToken ct = default);
    Task<CompanyDto> UpdateCompanyAsync(UpdateCompanyRequest request, CancellationToken ct = default);
    Task<OrganizationOverviewDto> GetOverviewAsync(CancellationToken ct = default);

    Task<PagedResponse<BranchDto>> GetBranchesAsync(string? search, bool? isActive, string? branchType, PagedRequest paged, CancellationToken ct = default);
    Task<BranchDetailDto> GetBranchDetailAsync(string idOrCode, CancellationToken ct = default);
    Task<BranchDto> CreateBranchAsync(CreateBranchRequest request, CancellationToken ct = default);
    Task<BranchDto> UpdateBranchAsync(string id, UpdateBranchRequest request, CancellationToken ct = default);
    Task<BranchDto> ToggleBranchStatusAsync(string id, UpdateBranchStatusRequest request, CancellationToken ct = default);

    Task<PagedResponse<DepartmentDto>> GetDepartmentsAsync(string? search, long? branchId, bool? isActive, PagedRequest paged, CancellationToken ct = default);
    Task<DepartmentDto> CreateDepartmentAsync(CreateDepartmentRequest request, CancellationToken ct = default);
    Task<DepartmentDto> UpdateDepartmentAsync(string id, UpdateDepartmentRequest request, CancellationToken ct = default);
    Task<DepartmentDto> ToggleDepartmentStatusAsync(string id, UpdateDepartmentStatusRequest request, CancellationToken ct = default);
    Task<PagedResponse<EmployeeDto>> GetDepartmentEmployeesAsync(string id, PagedRequest paged, CancellationToken ct = default);

    Task<PagedResponse<EmployeeDto>> GetEmployeesAsync(string? search, long? branchId, long? departmentId, string? status, PagedRequest paged, CancellationToken ct = default);
    Task<EmployeeProfileDto> GetEmployeeProfileAsync(string id, CancellationToken ct = default);
    Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeRequest request, CancellationToken ct = default);
    Task<EmployeeDto> UpdateEmployeeAsync(string id, UpdateEmployeeRequest request, CancellationToken ct = default);
    Task<EmployeeDto> UpdateEmployeeStatusAsync(string id, UpdateEmployeeStatusRequest request, CancellationToken ct = default);
    Task<EmployeeAccountDto> CreateEmployeeAccountAsync(string id, CreateEmployeeAccountRequest request, CancellationToken ct = default);
    Task<EmployeeAccountDto> ToggleEmployeeAccountStatusAsync(string id, UpdateEmployeeAccountStatusRequest request, CancellationToken ct = default);
}

public interface IIdentityAdminApiClient
{
    Task<PagedResponse<UserAccountDto>> GetUsersAsync(string? search, string? role, long? branchId, bool? isActive, PagedRequest paged, CancellationToken ct = default);
    Task<UserAccountDto> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default);
    Task<UserAccountDto> UpdateUserAsync(Guid publicId, UpdateUserRequest request, CancellationToken ct = default);
    Task<UserAccountDto> ToggleUserStatusAsync(Guid publicId, UpdateUserStatusRequest request, CancellationToken ct = default);
    Task<UserAccountDto> UpdateUserRolesAsync(Guid publicId, UpdateUserRolesRequest request, CancellationToken ct = default);
    Task<UserAccountDto> UpdateUserBranchesAsync(Guid publicId, UpdateUserBranchesRequest request, CancellationToken ct = default);
    Task ResetUserPasswordAsync(Guid publicId, AdminResetPasswordRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync(CancellationToken ct = default);
}

public interface ICrmApiClient
{
    Task<PagedResponse<CustomerDto>> GetCustomersAsync(string? search, string? customerType, bool? isActive, long? branchId, PagedRequest paged, CancellationToken ct = default);
    Task<CustomerDetailDto> GetCustomerDetailAsync(string id, CancellationToken ct = default);
    Task<CustomerOverviewDto> GetCustomerOverviewAsync(CancellationToken ct = default);
    Task<CustomerDto> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken ct = default);
    Task<CustomerDto> UpdateCustomerAsync(string id, UpdateCustomerRequest request, CancellationToken ct = default);
    Task<CustomerDto> ToggleCustomerStatusAsync(string id, UpdateCustomerStatusRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<CustomerContactDto>> GetContactsAsync(string customerId, CancellationToken ct = default);
    Task<CustomerContactDto> CreateContactAsync(string customerId, CreateCustomerContactRequest request, CancellationToken ct = default);
    Task<CustomerContactDto> UpdateContactAsync(string customerId, string contactId, UpdateCustomerContactRequest request, CancellationToken ct = default);
    Task<CustomerContactDto> SetPrimaryContactAsync(string customerId, string contactId, CancellationToken ct = default);
    Task DeleteContactAsync(string customerId, string contactId, CancellationToken ct = default);

    Task<IReadOnlyList<CustomerAddressDto>> GetAddressesAsync(string customerId, string? addressType = null, CancellationToken ct = default);
    Task<CustomerAddressDto> CreateAddressAsync(string customerId, CreateCustomerAddressRequest request, CancellationToken ct = default);
    Task<CustomerAddressDto> UpdateAddressAsync(string customerId, string addressId, UpdateCustomerAddressRequest request, CancellationToken ct = default);
    Task<CustomerAddressDto> SetDefaultAddressAsync(string customerId, string addressId, SetDefaultAddressRequest request, CancellationToken ct = default);
    Task DeleteAddressAsync(string customerId, string addressId, CancellationToken ct = default);

    Task<IReadOnlyList<CustomerContractDto>> GetContractsAsync(string customerId, CancellationToken ct = default);
    Task<CustomerContractDto> CreateContractAsync(string customerId, CreateCustomerContractRequest request, CancellationToken ct = default);
    Task<CustomerContractDto> UpdateContractAsync(string customerId, string contractId, UpdateCustomerContractRequest request, CancellationToken ct = default);
    Task<CustomerContractDto> UpdateContractStatusAsync(string customerId, string contractId, UpdateCustomerContractStatusRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<CustomerPricingRuleDto>> GetPricingRulesAsync(string customerId, CancellationToken ct = default);
    Task<CustomerPricingRuleDto> CreatePricingRuleAsync(string customerId, CreateCustomerPricingRuleRequest request, CancellationToken ct = default);
    Task<CustomerPricingRuleDto> UpdatePricingRuleAsync(string customerId, string ruleId, UpdateCustomerPricingRuleRequest request, CancellationToken ct = default);
    Task<CustomerPricingRuleDto> TogglePricingRuleStatusAsync(string customerId, string ruleId, UpdateCustomerPricingRuleStatusRequest request, CancellationToken ct = default);
}

public interface ICatalogApiClient
{
    Task<PagedResponse<CategoryDto>> GetCategoriesAsync(string? search, bool? isActive, PagedRequest paged, CancellationToken ct = default);
    Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken ct = default);
    Task<CategoryDto> UpdateCategoryAsync(string id, UpdateCategoryRequest request, CancellationToken ct = default);
    Task<CategoryDto> ToggleCategoryStatusAsync(string id, UpdateCategoryStatusRequest request, CancellationToken ct = default);

    Task<PagedResponse<ProductDto>> GetProductsAsync(string? search, long? categoryId, bool? isActive, PagedRequest paged, CancellationToken ct = default);
    Task<ProductDto> CreateProductAsync(CreateProductRequest request, CancellationToken ct = default);
    Task<ProductDto> UpdateProductAsync(string id, UpdateProductRequest request, CancellationToken ct = default);
    Task<ProductDto> ToggleProductStatusAsync(string id, UpdateProductStatusRequest request, CancellationToken ct = default);

    Task<PagedResponse<UnitOfMeasureDto>> GetUnitsAsync(string? search, string? type, bool? isActive, PagedRequest paged, CancellationToken ct = default);
    Task<UnitOfMeasureDto> CreateUnitAsync(CreateUnitOfMeasureRequest request, CancellationToken ct = default);
    Task<UnitOfMeasureDto> UpdateUnitAsync(long id, UpdateUnitOfMeasureRequest request, CancellationToken ct = default);
    Task<UnitOfMeasureDto> ToggleUnitStatusAsync(long id, UpdateUnitStatusRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<ServiceDto>> GetServicesAsync(bool activeOnly = false, CancellationToken ct = default);
}

public interface ISalesApiClient
{
    Task<PagedResponse<OrderDto>> GetOrdersAsync(string? search, string? orderStatus, string? paymentStatus, long? customerId, long? branchId, DateTime? dateFrom, DateTime? dateTo, PagedRequest paged, CancellationToken ct = default);
    Task<OrderDetailDto> GetOrderDetailAsync(string id, CancellationToken ct = default);
    Task<OrderDetailDto> CreateOrderAsync(CreateOrderRequest request, CancellationToken ct = default);
    Task<OrderDetailDto> UpdateOrderAsync(string id, UpdateOrderRequest request, CancellationToken ct = default);
    Task<OrderDetailDto> ConfirmOrderAsync(string id, CancellationToken ct = default);
    Task<OrderDetailDto> CancelOrderAsync(string id, CancelOrderRequest request, CancellationToken ct = default);
    Task<OrderItemDto> AddOrderItemAsync(string orderId, CreateOrderItemRequest request, CancellationToken ct = default);
    Task DeleteOrderItemAsync(string orderId, string itemId, CancellationToken ct = default);
    Task<OrderPackageDto> AddOrderPackageAsync(string orderId, CreateOrderPackageRequest request, CancellationToken ct = default);
    Task DeleteOrderPackageAsync(string orderId, string packageId, CancellationToken ct = default);
}

public interface IShippingApiClient
{
    Task<PagedResponse<ShipmentDto>> GetShipmentsAsync(string? search, string? status, long? customerId, long? originBranchId, long? destinationBranchId, DateTime? dateFrom, DateTime? dateTo, PagedRequest paged, CancellationToken ct = default);
    Task<ShipmentDetailDto> GetShipmentDetailAsync(string idOrTracking, CancellationToken ct = default);
    Task<ShipmentDetailDto> CreateFromOrderAsync(CreateShipmentFromOrderRequest request, CancellationToken ct = default);
    Task<ShipmentDetailDto> TransitionStatusAsync(string id, ShipmentStatusTransitionRequest request, CancellationToken ct = default);
    Task<ShipmentDetailDto> CancelShipmentAsync(string id, CancelShipmentRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<ShipmentStatusHistoryDto>> GetStatusHistoryAsync(string id, CancellationToken ct = default);
    Task<ShipmentPackageDto> AddPackageAsync(string shipmentId, AddShipmentPackageRequest request, CancellationToken ct = default);
    Task<ShipmentAssignmentDto> AssignAsync(string shipmentId, CreateShipmentAssignmentRequest request, CancellationToken ct = default);
    Task<ShipmentRouteDto> AddRouteAsync(string shipmentId, AddShipmentRouteRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<DeliveryAttemptDto>> GetDeliveryAttemptsAsync(string shipmentId, CancellationToken ct = default);
    Task<DeliveryAttemptDto> RecordDeliveryAttemptAsync(string shipmentId, RecordDeliveryAttemptRequest request, CancellationToken ct = default);
    Task<ProofOfDeliveryDto?> GetProofOfDeliveryAsync(string shipmentId, CancellationToken ct = default);
    Task<ProofOfDeliveryDto> CreateProofOfDeliveryAsync(string shipmentId, long attemptId, CreateProofOfDeliveryRequest request, CancellationToken ct = default);

    Task<PagedResponse<PickupRequestDto>> GetPickupRequestsAsync(string? search, string? status, PagedRequest paged, CancellationToken ct = default);
    Task<PickupRequestDto> CreatePickupRequestAsync(CreatePickupRequestRequest request, CancellationToken ct = default);
    Task<PickupRequestDto> AssignPickupAsync(string id, long employeeId, CancellationToken ct = default);
    Task<PickupRequestDto> ConfirmPickupAsync(string id, CancellationToken ct = default);
    Task<PickupRequestDto> CompletePickupAsync(string id, CompletePickupRequest request, CancellationToken ct = default);
    Task<PickupRequestDto> CancelPickupAsync(string id, string reason, CancellationToken ct = default);

    Task<PagedResponse<ReturnRequestDto>> GetReturnRequestsAsync(string? search, string? status, PagedRequest paged, CancellationToken ct = default);
    Task<ReturnRequestDto> CreateReturnRequestAsync(CreateReturnRequestRequest request, CancellationToken ct = default);
    Task<ReturnRequestDto> ProcessReturnRequestAsync(string id, ProcessReturnRequestRequest request, CancellationToken ct = default);
}

public interface ICarrierApiClient
{
    Task<PagedResponse<CarrierDto>> GetCarriersAsync(string? search, bool? isActive, PagedRequest paged, CancellationToken ct = default);
    Task<CarrierDetailDto> GetCarrierDetailAsync(string identifier, CancellationToken ct = default);
    Task<CarrierDetailDto> CreateCarrierAsync(CreateCarrierRequest request, CancellationToken ct = default);
    Task<CarrierDetailDto> UpdateCarrierAsync(string identifier, UpdateCarrierRequest request, CancellationToken ct = default);
    Task<CarrierDetailDto> ToggleCarrierStatusAsync(string identifier, bool isActive, CancellationToken ct = default);
    Task<CarrierServiceDto> CreateCarrierServiceAsync(string carrierIdentifier, CreateCarrierServiceRequest request, CancellationToken ct = default);
    Task<CarrierServiceDto> ToggleCarrierServiceStatusAsync(string carrierIdentifier, long serviceId, bool isActive, CancellationToken ct = default);
    Task<IReadOnlyList<CarrierConfigurationDto>> GetConfigurationsAsync(string carrierIdentifier, CancellationToken ct = default);
    Task<CarrierConfigurationDto> UpsertConfigurationAsync(string carrierIdentifier, UpsertCarrierConfigurationRequest request, CancellationToken ct = default);
    Task<CarrierTrackingMappingDto> CreateTrackingMappingAsync(string carrierIdentifier, CreateTrackingMappingRequest request, CancellationToken ct = default);

    Task<CarrierBookingResultDto> BookShipmentAsync(CarrierBookShipmentRequest request, CancellationToken ct = default);
    Task<CarrierCancelResultDto> CancelShipmentAsync(CarrierCancelShipmentRequest request, CancellationToken ct = default);
    Task<CarrierRateResultDto> QueryRatesAsync(CarrierRateQueryRequest request, CancellationToken ct = default);
    Task<CarrierTrackingQueryDto> QueryTrackingAsync(string carrierCode, string trackingNumber, CancellationToken ct = default);

    Task<PagedResponse<CarrierApiLogDto>> GetApiLogsAsync(string? carrierIdentifier, bool? isSuccess, int page, int pageSize, CancellationToken ct = default);
    Task<PagedResponse<CarrierWebhookLogDto>> GetWebhookLogsAsync(string? carrierIdentifier, bool? processedSuccessfully, int page, int pageSize, CancellationToken ct = default);
    Task<PagedResponse<CarrierSyncJobDto>> GetSyncJobsAsync(string? carrierIdentifier, string? status, int page, int pageSize, CancellationToken ct = default);
}

public interface IWarehouseApiClient
{
    Task<PagedResponse<WarehouseDto>> GetWarehousesAsync(string? search, long? branchId, string? type, bool? isActive, int page, int pageSize, CancellationToken ct = default);
    Task<WarehouseDetailDto> GetWarehouseDetailAsync(string id, CancellationToken ct = default);
    Task<WarehouseDetailDto> CreateWarehouseAsync(CreateWarehouseRequest request, CancellationToken ct = default);
    Task<WarehouseDetailDto> ToggleWarehouseStatusAsync(string id, bool isActive, CancellationToken ct = default);
    Task<IReadOnlyList<WarehouseZoneDto>> GetZonesAsync(string warehouseId, CancellationToken ct = default);
    Task<WarehouseZoneDto> CreateZoneAsync(string warehouseId, CreateWarehouseZoneRequest request, CancellationToken ct = default);
    Task<PagedResponse<WarehouseLocationDto>> GetLocationsAsync(string warehouseId, long? zoneId, int page, int pageSize, CancellationToken ct = default);
    Task<WarehouseLocationDto> CreateLocationAsync(string warehouseId, long zoneId, CreateWarehouseLocationRequest request, CancellationToken ct = default);
    Task<WarehouseLocationDto> ToggleLocationBlockAsync(string warehouseId, long locationId, bool isBlocked, CancellationToken ct = default);

    Task<ScanPackageResultDto> ScanParcelAsync(ScanPackageRequest request, CancellationToken ct = default);
    Task<ParcelHandlingEventDto> ReceiveParcelAsync(ParcelReceiveRequest request, CancellationToken ct = default);
    Task<ParcelHandlingEventDto> SortParcelAsync(ParcelSortRequest request, CancellationToken ct = default);
    Task<ParcelHandlingEventDto> StageParcelAsync(ParcelStageRequest request, CancellationToken ct = default);
    Task<ParcelHandlingEventDto> RecordParcelExceptionAsync(ParcelExceptionRequest request, CancellationToken ct = default);
    Task<PagedResponse<ParcelHandlingEventDto>> GetParcelEventsAsync(long? warehouseId, long? shipmentId, string? eventType, int page, int pageSize, CancellationToken ct = default);

    Task<PagedResponse<WarehouseDispatchDto>> GetDispatchesAsync(long? warehouseId, string? status, int page, int pageSize, CancellationToken ct = default);
    Task<WarehouseDispatchDto> CreateDispatchAsync(CreateWarehouseDispatchDto request, CancellationToken ct = default);
    Task<WarehouseDispatchDto> ConfirmDispatchAsync(long id, long employeeId, CancellationToken ct = default);
}

public interface IInventoryApiClient
{
    Task<PagedResponse<InventoryItemDto>> GetItemsAsync(string? search, long? warehouseId, int page, int pageSize, CancellationToken ct = default);
    Task<InventoryItemDto> CreateItemAsync(CreateInventoryItemRequest request, CancellationToken ct = default);
    Task<PagedResponse<InventoryBalanceDto>> GetBalancesAsync(long? warehouseId, long? productId, long? locationId, int page, int pageSize, CancellationToken ct = default);
    Task<PagedResponse<InventoryTransactionDto>> GetTransactionsAsync(long? warehouseId, string? transactionType, int page, int pageSize, CancellationToken ct = default);
    Task<PagedResponse<WarehouseReceiptDto>> GetReceiptsAsync(long? warehouseId, string? status, int page, int pageSize, CancellationToken ct = default);
    Task<WarehouseReceiptDto> CreateReceiptAsync(CreateWarehouseReceiptRequest request, CancellationToken ct = default);
    Task<WarehouseReceiptDto> ConfirmReceiptAsync(long id, ConfirmReceiptRequest? request, CancellationToken ct = default);
    Task<PagedResponse<StockAdjustmentDto>> GetAdjustmentsAsync(long? warehouseId, string? status, int page, int pageSize, CancellationToken ct = default);
    Task<StockAdjustmentDto> CreateAdjustmentAsync(CreateStockAdjustmentRequest request, CancellationToken ct = default);
    Task<StockAdjustmentDto> ApproveAdjustmentAsync(long id, long employeeId, CancellationToken ct = default);
    Task<PagedResponse<StockTransferDto>> GetTransfersAsync(long? sourceWarehouseId, long? destinationWarehouseId, string? status, int page, int pageSize, CancellationToken ct = default);
    Task<StockTransferDto> CreateTransferAsync(CreateStockTransferRequest request, CancellationToken ct = default);
    Task<StockTransferDto> ConfirmTransferAsync(long id, long employeeId, CancellationToken ct = default);
    Task<StockTransferDto> ReceiveTransferAsync(long id, ReceiveStockTransferRequest request, long employeeId, CancellationToken ct = default);
    Task<PagedResponse<InventoryReservationDto>> GetReservationsAsync(long? warehouseId, string? status, int page, int pageSize, CancellationToken ct = default);
    Task<InventoryReservationDto> CreateReservationAsync(CreateInventoryReservationRequest request, CancellationToken ct = default);
    Task<InventoryReservationDto> ReleaseReservationAsync(long id, CancellationToken ct = default);
    Task<InventoryReservationDto> ConsumeReservationAsync(long id, CancellationToken ct = default);
    Task<PagedResponse<InventoryLotDto>> GetLotsAsync(long? productId, int page, int pageSize, CancellationToken ct = default);
    Task<InventoryLotDto> CreateLotAsync(CreateInventoryLotRequest request, CancellationToken ct = default);
    Task<PagedResponse<InventorySerialNumberDto>> GetSerialNumbersAsync(long? warehouseId, long? productId, int page, int pageSize, CancellationToken ct = default);
    Task<InventorySerialNumberDto> CreateSerialNumberAsync(CreateInventorySerialNumberRequest request, CancellationToken ct = default);
}

public interface IFinanceApiClient
{
    Task<PagedResponse<ShippingRateCardDto>> GetRateCardsAsync(string? search, bool? isActive, int page, int pageSize, CancellationToken ct = default);
    Task<ShippingRateCardDto> CreateRateCardAsync(CreateShippingRateCardRequest request, CancellationToken ct = default);
    Task<ShippingRateCardDto> UpdateRateCardAsync(long id, UpdateShippingRateCardRequest request, CancellationToken ct = default);
    Task<ShippingRateCardDto> ToggleRateCardStatusAsync(long id, bool isActive, CancellationToken ct = default);
    Task<IReadOnlyList<ShippingRateRuleDto>> GetRateRulesAsync(long rateCardId, CancellationToken ct = default);
    Task<ShippingRateRuleDto> CreateRateRuleAsync(long rateCardId, CreateShippingRateRuleRequest request, CancellationToken ct = default);

    Task<ShippingFeeCalculationResultDto> CalculateShippingFeeAsync(CalculateShippingFeeRequest request, CancellationToken ct = default);
    Task<PagedResponse<ShipmentChargeDto>> GetChargesAsync(long? shipmentId, long? customerId, bool? isInvoiced, int page, int pageSize, CancellationToken ct = default);
    Task<ShipmentChargeDto> CreateChargeAdjustmentAsync(long shipmentId, CreateChargeAdjustmentRequest request, CancellationToken ct = default);

    Task<PagedResponse<InvoiceDto>> GetInvoicesAsync(long? customerId, string? status, int page, int pageSize, CancellationToken ct = default);
    Task<InvoiceDto> GetInvoiceByIdAsync(long id, CancellationToken ct = default);
    Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceRequest request, CancellationToken ct = default);
    Task<InvoiceDto> IssueInvoiceAsync(long id, CancellationToken ct = default);
    Task<InvoiceDto> CancelInvoiceAsync(long id, CancelInvoiceRequest request, CancellationToken ct = default);

    Task<PagedResponse<PaymentDto>> GetPaymentsAsync(long? customerId, string? status, int page, int pageSize, CancellationToken ct = default);
    Task<PaymentDto> RegisterPaymentAsync(RegisterPaymentRequest request, CancellationToken ct = default);
    Task<PaymentDto> AllocatePaymentAsync(long paymentId, CreatePaymentAllocationRequest request, CancellationToken ct = default);
    Task<PaymentDto> ReverseAllocationAsync(long paymentId, long allocationId, CancellationToken ct = default);

    Task<PagedResponse<CustomerBalanceDto>> GetCustomerBalancesAsync(long? branchId, int page, int pageSize, CancellationToken ct = default);
    Task<CustomerBalanceReconciliationDto> ReconcileCustomerBalanceAsync(long customerId, CancellationToken ct = default);
    Task<PagedResponse<FinancialLedgerEntryDto>> GetLedgerEntriesAsync(long? customerId, string? entryType, int page, int pageSize, CancellationToken ct = default);
}

public interface ICodApiClient
{
    Task<PagedResponse<CodCollectionDto>> GetCollectionsAsync(long? branchId, long? employeeId, long? customerId, string? status, int page, int pageSize, CancellationToken ct = default);
    Task<CodCollectionSummaryDto> GetCollectionSummaryAsync(long? branchId, CancellationToken ct = default);
    Task<CodCollectionDto> CreateCollectionAsync(CreateCodCollectionRequest request, CancellationToken ct = default);
    Task<CodCollectionDto> CollectAsync(long id, RecordCodCollectionRequest request, CancellationToken ct = default);
    Task<CodCollectionDto> DepositAsync(long id, string? notes, CancellationToken ct = default);
    Task<CodCollectionDto> VerifyAsync(long id, VerifyCodCollectionRequest request, CancellationToken ct = default);
    Task<CodCollectionDto> RecordFailureAsync(long id, RecordCodFailureRequest request, CancellationToken ct = default);

    Task<PagedResponse<CodRemittanceDto>> GetRemittancesAsync(long? customerId, string? status, int page, int pageSize, CancellationToken ct = default);
    Task<CodRemittanceDto> GetRemittanceByIdAsync(long id, CancellationToken ct = default);
    Task<CodRemittanceDto> CreateRemittanceAsync(CreateCodRemittanceRequest request, CancellationToken ct = default);
    Task<CodRemittanceDto> AddRemittanceItemAsync(long remittanceId, AddCodRemittanceItemRequest request, CancellationToken ct = default);
    Task<CodRemittanceDto> ConfirmRemittanceAsync(long id, ConfirmCodRemittanceRequest request, CancellationToken ct = default);
    Task<CodRemittanceDto> CancelRemittanceAsync(long id, CancelCodRemittanceRequest request, CancellationToken ct = default);

    Task<CodReconciliationSummaryDto> GetReconciliationSummaryAsync(long? branchId, CancellationToken ct = default);
    Task<EmployeeCodReconciliationDto> ReconcileEmployeeAsync(long employeeId, CancellationToken ct = default);
    Task<BranchCodReconciliationDto> ReconcileBranchAsync(long branchId, CancellationToken ct = default);
}

public interface INotificationApiClient
{
    Task<PagedResponse<NotificationDto>> GetMyNotificationsAsync(bool? isRead, int page, int pageSize, CancellationToken ct = default);
    Task<PagedResponse<NotificationDto>> GetAllNotificationsAsync(long? branchId, bool? isRead, int page, int pageSize, CancellationToken ct = default);
    Task<UnreadCountDto> GetUnreadCountAsync(CancellationToken ct = default);
    Task<NotificationDto> MarkAsReadAsync(long id, CancellationToken ct = default);
    Task MarkAllReadAsync(CancellationToken ct = default);
    Task<NotificationDto> SendNotificationAsync(CreateNotificationRequest request, CancellationToken ct = default);

    Task<PagedResponse<NotificationTemplateDto>> GetTemplatesAsync(string? search, string? channel, int page, int pageSize, CancellationToken ct = default);
    Task<NotificationTemplateDto> CreateTemplateAsync(CreateNotificationTemplateRequest request, CancellationToken ct = default);
    Task<NotificationTemplateDto> UpdateTemplateAsync(long id, UpdateNotificationTemplateRequest request, CancellationToken ct = default);
    Task<NotificationTemplateDto> ToggleTemplateStatusAsync(long id, UpdateTemplateStatusRequest request, CancellationToken ct = default);
}

public interface IReportingApiClient
{
    Task<CompanyOverviewDashboardDto> GetCompanyOverviewAsync(ReportFilterRequest filter, CancellationToken ct = default);
    Task<BranchDashboardDto> GetBranchDashboardAsync(long branchId, ReportFilterRequest filter, CancellationToken ct = default);
    Task<OperationsDashboardDto> GetOperationsDashboardAsync(ReportFilterRequest filter, CancellationToken ct = default);
    Task<ShipmentDashboardDto> GetShipmentDashboardAsync(ReportFilterRequest filter, CancellationToken ct = default);
    Task<DeliveryPerformanceDashboardDto> GetDeliveryPerformanceDashboardAsync(ReportFilterRequest filter, CancellationToken ct = default);
    Task<OrdersDashboardDto> GetOrdersDashboardAsync(ReportFilterRequest filter, CancellationToken ct = default);
    Task<WarehouseDashboardDto> GetWarehouseDashboardAsync(ReportFilterRequest filter, CancellationToken ct = default);
    Task<InventoryDashboardDto> GetInventoryDashboardAsync(ReportFilterRequest filter, CancellationToken ct = default);
    Task<FinanceDashboardDto> GetFinanceDashboardAsync(ReportFilterRequest filter, CancellationToken ct = default);
    Task<CodDashboardDto> GetCodDashboardAsync(ReportFilterRequest filter, CancellationToken ct = default);

    Task<PagedResponse<OrderReportRowDto>> GetOrderReportAsync(ReportFilterRequest filter, int page, int pageSize, CancellationToken ct = default);
    Task<PagedResponse<ShipmentReportRowDto>> GetShipmentReportAsync(ReportFilterRequest filter, int page, int pageSize, CancellationToken ct = default);
    Task<PagedResponse<DeliveryPerformanceRowDto>> GetDeliveryPerformanceReportAsync(ReportFilterRequest filter, int page, int pageSize, CancellationToken ct = default);
    Task<PagedResponse<CarrierPerformanceRowDto>> GetCarrierPerformanceReportAsync(ReportFilterRequest filter, int page, int pageSize, CancellationToken ct = default);
    Task<PagedResponse<WarehouseReceiptReportRowDto>> GetWarehouseReceiptsReportAsync(ReportFilterRequest filter, int page, int pageSize, CancellationToken ct = default);
    Task<PagedResponse<WarehouseDispatchReportRowDto>> GetWarehouseDispatchesReportAsync(ReportFilterRequest filter, int page, int pageSize, CancellationToken ct = default);
    Task<PagedResponse<ParcelHandlingReportRowDto>> GetParcelHandlingReportAsync(ReportFilterRequest filter, int page, int pageSize, CancellationToken ct = default);
    Task<PagedResponse<InventoryBalanceReportRowDto>> GetInventoryBalancesReportAsync(ReportFilterRequest filter, bool onlyLowStock, int page, int pageSize, CancellationToken ct = default);
    Task<PagedResponse<InventoryTransactionReportRowDto>> GetInventoryTransactionsReportAsync(ReportFilterRequest filter, int page, int pageSize, CancellationToken ct = default);
    Task<PagedResponse<ShippingChargeReportRowDto>> GetShippingChargesReportAsync(ReportFilterRequest filter, bool? isInvoiced, int page, int pageSize, CancellationToken ct = default);
    Task<PagedResponse<InvoiceReportRowDto>> GetInvoicesReportAsync(ReportFilterRequest filter, int page, int pageSize, CancellationToken ct = default);
    Task<PagedResponse<PaymentReportRowDto>> GetPaymentsReportAsync(ReportFilterRequest filter, int page, int pageSize, CancellationToken ct = default);
    Task<PagedResponse<ReceivableReportRowDto>> GetReceivablesReportAsync(ReportFilterRequest filter, int page, int pageSize, CancellationToken ct = default);
    Task<PagedResponse<CodCollectionReportRowDto>> GetCodCollectionsReportAsync(ReportFilterRequest filter, int page, int pageSize, CancellationToken ct = default);
    Task<PagedResponse<CodRemittanceReportRowDto>> GetCodRemittancesReportAsync(ReportFilterRequest filter, int page, int pageSize, CancellationToken ct = default);
    Task<PagedResponse<CodReconciliationReportRowDto>> GetCodReconciliationReportAsync(ReportFilterRequest filter, int page, int pageSize, CancellationToken ct = default);

    Task<DownloadedFileDto> ExportCsvAsync(string reportType, ReportFilterRequest filter, CancellationToken ct = default);

    Task<PagedResponse<ReportDefinitionDto>> GetReportDefinitionsAsync(int page, int pageSize, CancellationToken ct = default);
    Task<PagedResponse<ScheduledReportDto>> GetScheduledReportsAsync(long? branchId, int page, int pageSize, CancellationToken ct = default);
    Task<ScheduledReportDto> CreateScheduledReportAsync(CreateScheduledReportRequest request, CancellationToken ct = default);
    Task<ScheduledReportRunResultDto> RunScheduledReportAsync(long id, CancellationToken ct = default);
    Task<BackgroundProcessingHealthDto> GetBackgroundHealthAsync(CancellationToken ct = default);
    Task<PagedResponse<BackgroundJobLogDto>> GetBackgroundJobsAsync(string? status, int page, int pageSize, CancellationToken ct = default);
    Task<BackgroundJobLogDto> RetryBackgroundJobAsync(long id, CancellationToken ct = default);
    Task<PagedResponse<OutboxMessageDto>> GetOutboxMessagesAsync(string? status, int page, int pageSize, CancellationToken ct = default);
    Task RetryOutboxMessageAsync(long id, CancellationToken ct = default);
}
