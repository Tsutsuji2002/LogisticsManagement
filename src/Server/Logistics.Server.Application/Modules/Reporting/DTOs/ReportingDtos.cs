using Logistics.Shared.Contracts.Pagination;

namespace Logistics.Server.Application.Modules.Reporting.DTOs;

// ═══════════════════════════════════════════════════════════════
//  1. SHARED QUERY & FILTER CONTRACTS
// ═══════════════════════════════════════════════════════════════

public record ReportFilterRequest
{
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public string TimeZoneId { get; init; } = "UTC";
    public long? BranchId { get; init; }
    public long? CustomerId { get; init; }
    public long? EmployeeId { get; init; }
    public long? WarehouseId { get; init; }
    public long? CarrierId { get; init; }
    public long? CarrierServiceId { get; init; }
    public string? Status { get; init; }
    public string? Search { get; init; }
}

public record PagedReportFilterRequest : ReportFilterRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;

    public PagedRequest ToPagedRequest() => new() { Page = Page, PageSize = PageSize };
}

public sealed record ReportTimeWindow(
    DateTime? UtcFromInclusive,
    DateTime? UtcToExclusive,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string TimeZoneId);

public record DateTrendPointDto(DateOnly Date, int Count, decimal Value = 0m);

public record StatusBreakdownDto(string Status, int Count, decimal Amount = 0m);

public record BranchBreakdownDto(long BranchId, string BranchCode, string BranchName, int Count, decimal Amount = 0m);

// ═══════════════════════════════════════════════════════════════
//  2. DASHBOARD DTOs
// ═══════════════════════════════════════════════════════════════

public record CompanyOverviewDashboardDto
{
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public string TimeZoneId { get; init; } = "UTC";

    // Lifetime Organization & Live Snapshot Metrics
    public int TotalActiveBranches { get; init; }
    public int TotalActiveEmployees { get; init; }
    public int TotalActiveCustomers { get; init; }
    public int ShipmentsCurrentlyInTransit { get; init; }
    public int ShipmentsAwaitingPickup { get; init; }
    public int ShipmentsAwaitingDelivery { get; init; }
    public int PendingWarehouseReceipts { get; init; }
    public int PendingWarehouseDispatches { get; init; }

    // Time-Filtered (when StartDate/EndDate supplied) or All-Time Activity Metrics
    public int TotalOrders { get; init; }
    public int TotalShipments { get; init; }
    public int DeliveredShipments { get; init; }
    public int FailedDeliveryAttempts { get; init; }
    public int ReturnedShipments { get; init; }

    // Inventory & Finance & COD Snapshot
    public InventorySnapshotDto InventorySummary { get; init; } = new();
    public FinanceSnapshotDto FinancialSummary { get; init; } = new();
    public CodSnapshotDto CodSummary { get; init; } = new();
}

public record InventorySnapshotDto
{
    public int DistinctProductsInStock { get; init; }
    public long TotalQuantityOnHand { get; init; }
    public long TotalQuantityAllocated { get; init; }
    public long TotalQuantityAvailable { get; init; }
    public long TotalQuantityDamaged { get; init; }
}

public record FinanceSnapshotDto
{
    public decimal TotalShipmentCharges { get; init; }
    public decimal TotalIssuedInvoiceAmount { get; init; }
    public decimal TotalPaymentsConfirmed { get; init; }
    public decimal TotalPaymentsAllocated { get; init; }
    public decimal TotalCustomerOutstandingReceivables { get; init; }
    public decimal TotalUnbilledCharges { get; init; }
}

public record CodSnapshotDto
{
    public decimal TotalExpectedCodOnShipments { get; init; }
    public decimal TotalCollectedByDrivers { get; init; }
    public decimal TotalDepositedToBranches { get; init; }
    public decimal TotalVerifiedByCashiers { get; init; }
    public decimal TotalRemittedToCustomers { get; init; }
    public decimal OutstandingDriverCashInHand { get; init; }
    public decimal TotalCustomerCodHoldingBalance { get; init; }
}

public record BranchDashboardDto
{
    public long BranchId { get; init; }
    public string BranchCode { get; init; } = string.Empty;
    public string BranchName { get; init; } = string.Empty;
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }

    public int OrdersCreated { get; init; }
    public int ShipmentsCreatedAtOrigin { get; init; }
    public int ShipmentsReceivedAtDestination { get; init; }
    public int ShipmentsDelivered { get; init; }
    public int ShipmentsDispatchedFromBranchWarehouses { get; init; }
    public decimal DeliverySuccessRatePercent { get; init; }

    public int PendingReceiptsCount { get; init; }
    public int PendingDispatchesCount { get; init; }

    public InventorySnapshotDto InventorySummary { get; init; } = new();
    public CodSnapshotDto CodOperations { get; init; } = new();
    public FinanceSnapshotDto? FinancialSummary { get; init; }
}

public record OperationsDashboardDto
{
    public int OrdersAwaitingProcessing { get; init; }
    public int ShipmentsAwaitingPickup { get; init; }
    public int ShipmentsInSortingHub { get; init; }
    public int ShipmentsInTransit { get; init; }
    public int ShipmentsAwaitingDelivery { get; init; }
    public int ShipmentsDelivered { get; init; }
    public int FailedDeliveryAttempts { get; init; }
    public int ActiveReturnRequests { get; init; }
    public int WarehouseReceiptsPending { get; init; }
    public int WarehouseDispatchesPending { get; init; }
    public int ParcelHandlingEventsCount { get; init; }
}

public record ShipmentDashboardDto
{
    public int TotalShipments { get; init; }
    public int DistinctOrdersCovered { get; init; }
    public List<StatusBreakdownDto> ByStatus { get; init; } = [];
    public List<StatusBreakdownDto> ByServiceType { get; init; } = [];
    public List<CarrierCountDto> ByCarrier { get; init; } = [];
    public List<BranchBreakdownDto> ByOriginBranch { get; init; } = [];
    public List<DateTrendPointDto> CreationTrend { get; init; } = [];
    public List<DateTrendPointDto> DeliveryTrend { get; init; } = [];
    public List<DateTrendPointDto> CancellationTrend { get; init; } = [];
    public List<DateTrendPointDto> ReturnTrend { get; init; } = [];
}

public record CarrierCountDto(long? CarrierId, string CarrierCode, string CarrierName, int ShipmentCount);

public record DeliveryPerformanceDashboardDto
{
    public int CompletedTerminalShipmentsCount { get; init; }
    public int DeliveredShipmentsCount { get; init; }
    public int FailedOutcomeShipmentsCount { get; init; }
    public int InTransitActiveExcludedCount { get; init; }
    public decimal? SuccessfulDeliveryRatePercent { get; init; }
    public decimal? FailedDeliveryRatePercent { get; init; }
    public double? AverageDeliveryDurationHours { get; init; }
    public double? AverageAttemptsPerAttemptedShipment { get; init; }

    public int DeliveredWithSlaCount { get; init; }
    public int DeliveredWithoutSlaCount { get; init; }
    public int OnTimeDeliveredCount { get; init; }
    public int LateDeliveredCount { get; init; }
    public decimal? OnTimeDeliveryRatePercent { get; init; }

    public List<DeliveryGroupPerformanceDto> ByBranch { get; init; } = [];
    public List<DeliveryGroupPerformanceDto> ByEmployee { get; init; } = [];
    public List<DeliveryGroupPerformanceDto> ByCarrier { get; init; } = [];
}

public record DeliveryGroupPerformanceDto
{
    public long KeyId { get; init; }
    public string KeyCode { get; init; } = string.Empty;
    public string KeyName { get; init; } = string.Empty;
    public int TotalAttempts { get; init; }
    public int DeliveredCount { get; init; }
    public int FailedCount { get; init; }
    public decimal? SuccessRatePercent { get; init; }
}

public record OrdersDashboardDto
{
    public int TotalOrders { get; init; }
    public decimal TotalDeclaredValue { get; init; }
    public decimal TotalCodAmount { get; init; }
    public int TotalOrderItems { get; init; }
    public int TotalOrderPackages { get; init; }
    public string? ServiceBreakdownNote { get; init; }
    public List<StatusBreakdownDto> ByStatus { get; init; } = [];
    public List<BranchBreakdownDto> ByBranch { get; init; } = [];
    public List<CustomerSummaryBreakdownDto> TopCustomers { get; init; } = [];
    public List<DateTrendPointDto> CreationTrend { get; init; } = [];
    public List<DateTrendPointDto> CancellationTrend { get; init; } = [];
}

public record CustomerSummaryBreakdownDto(long CustomerId, string CustomerCode, string CustomerName, int Count, decimal TotalAmount);

public record WarehouseDashboardDto
{
    public int TotalReceipts { get; init; }
    public int PendingReceipts { get; init; }
    public int CompletedReceipts { get; init; }
    public int TotalDispatches { get; init; }
    public int PendingDispatches { get; init; }
    public int CompletedDispatches { get; init; }
    public int ParcelEventsTotal { get; init; }
    public int ParcelsUnloadedAtHub { get; init; }
    public int ParcelsSortedAtHub { get; init; }
    public int ParcelsDispatchedFromHub { get; init; }
    public int ParcelsAwaitingSorting { get; init; }
    public int ParcelsAwaitingDispatch { get; init; }
    public List<StatusBreakdownDto> ParcelEventsByType { get; init; } = [];
}

public record InventoryDashboardDto
{
    public InventorySnapshotDto Summary { get; init; } = new();
    public int ActiveReservationsCount { get; init; }
    public long ReservedUnitsTotal { get; init; }
    public int LowStockProductWarehouseCount { get; init; }
    public int DistinctLotsCount { get; init; }
    public int SerialNumbersAvailableCount { get; init; }
    public int TransactionsCountInPeriod { get; init; }
    public List<StatusBreakdownDto> TransactionsByType { get; init; } = [];
    public List<WarehouseStockBreakdownDto> StockByWarehouse { get; init; } = [];
}

public record WarehouseStockBreakdownDto(long WarehouseId, string WarehouseCode, string WarehouseName, long QuantityOnHand, long QuantityAllocated, long QuantityAvailable, long QuantityDamaged);

public record FinanceDashboardDto
{
    public decimal TotalShipmentChargesAmount { get; init; }
    public decimal UnbilledShipmentChargesAmount { get; init; }
    public int DraftInvoicesCount { get; init; }
    public decimal DraftInvoicesTotal { get; init; }
    public int IssuedAndActiveInvoicesCount { get; init; }
    public decimal IssuedInvoicesTotal { get; init; }
    public int CancelledInvoicesCount { get; init; }
    public decimal CancelledInvoicesTotal { get; init; }
    public decimal TotalPaymentsConfirmed { get; init; }
    public decimal TotalPaymentsAllocated { get; init; }
    public decimal TotalUnallocatedPaymentCredit { get; init; }
    public decimal AuthoritativeCustomerReceivables { get; init; }
    public decimal AuthoritativeUnbilledBalance { get; init; }
    public List<DateTrendPointDto> InvoicingTrend { get; init; } = [];
    public List<DateTrendPointDto> PaymentCollectionTrend { get; init; } = [];
}

public record CodDashboardDto
{
    public decimal ExpectedCodOnShipments { get; init; }
    public decimal CollectedByDrivers { get; init; }
    public decimal DepositedToBranches { get; init; }
    public decimal VerifiedByCashiers { get; init; }
    public decimal RemittedToCustomersNet { get; init; }
    public decimal OutstandingDriverCashInHand { get; init; }
    public decimal UnremittedVerifiedHolding { get; init; }
    public decimal ExpectedVsCollectedDiscrepancy { get; init; }
    public int CollectionsCount { get; init; }
    public int CompletedRemittancesCount { get; init; }
    public List<StatusBreakdownDto> CollectionsByStatus { get; init; } = [];
    public List<BranchBreakdownDto> ByBranch { get; init; } = [];
    public List<DateTrendPointDto> CollectionTrend { get; init; } = [];
}

// ═══════════════════════════════════════════════════════════════
//  3. DETAILED REPORT ROW DTOs
// ═══════════════════════════════════════════════════════════════

public record OrderReportRowDto
{
    public long OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public DateTime OrderDate { get; init; }
    public string OrderStatus { get; init; } = string.Empty;
    public string PaymentStatus { get; init; } = string.Empty;
    public long BranchId { get; init; }
    public string BranchCode { get; init; } = string.Empty;
    public long CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public int ItemsCount { get; init; }
    public int PackagesCount { get; init; }
    public decimal TotalWeightKg { get; init; }
    public decimal DeclaredValue { get; init; }
    public decimal CodAmount { get; init; }
}

public record ShipmentReportRowDto
{
    public long ShipmentId { get; init; }
    public string TrackingNumber { get; init; } = string.Empty;
    public string? OrderNumber { get; init; }
    public string ShipmentStatus { get; init; } = string.Empty;
    public string ServiceType { get; init; } = string.Empty;
    public string OriginBranchCode { get; init; } = string.Empty;
    public string DestinationBranchCode { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string? CarrierCode { get; init; }
    public decimal ChargeableWeightKg { get; init; }
    public decimal CodAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? EstimatedDeliveryDate { get; init; }
    public DateTime? ActualDeliveryDate { get; init; }
}

public record DeliveryPerformanceRowDto
{
    public long ShipmentId { get; init; }
    public string TrackingNumber { get; init; } = string.Empty;
    public string DestinationBranchCode { get; init; } = string.Empty;
    public string? CarrierCode { get; init; }
    public string ShipmentStatus { get; init; } = string.Empty;
    public int AttemptCount { get; init; }
    public DateTime? PickupDate { get; init; }
    public DateTime? EstimatedDeliveryDate { get; init; }
    public DateTime? ActualDeliveryDate { get; init; }
    public double? DeliveryDurationHours { get; init; }
    public bool? IsOnTime { get; init; }
}

public record CarrierPerformanceRowDto
{
    public long CarrierId { get; init; }
    public string CarrierCode { get; init; } = string.Empty;
    public string CarrierName { get; init; } = string.Empty;
    public int TotalShipments { get; init; }
    public int DeliveredShipments { get; init; }
    public int FailedOrReturnedShipments { get; init; }
    public int ApiCallsTotal { get; init; }
    public int ApiCallsSucceeded { get; init; }
    public int ApiCallsFailed { get; init; }
    public int WebhooksTotal { get; init; }
    public int WebhooksSucceeded { get; init; }
    public int SyncJobsTotal { get; init; }
    public int SyncJobsFailed { get; init; }
    public int TrackingEventsTotal { get; init; }
    public int TrackingEventsFailed { get; init; }
}

public record WarehouseReceiptReportRowDto
{
    public long ReceiptId { get; init; }
    public string ReceiptNumber { get; init; } = string.Empty;
    public string WarehouseCode { get; init; } = string.Empty;
    public string ReceiptType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime ActualArrivalDate { get; init; }
    public int LineItemsCount { get; init; }
    public int TotalReceivedQuantity { get; init; }
    public int TotalDamagedQuantity { get; init; }
}

public record WarehouseDispatchReportRowDto
{
    public long DispatchId { get; init; }
    public string DispatchNumber { get; init; } = string.Empty;
    public string WarehouseCode { get; init; } = string.Empty;
    public string DispatchType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime? DispatchedAt { get; init; }
    public int LineItemsCount { get; init; }
    public int PackagesCount { get; init; }
}

public record ParcelHandlingReportRowDto
{
    public long EventId { get; init; }
    public string TrackingNumber { get; init; } = string.Empty;
    public string PackageBarcode { get; init; } = string.Empty;
    public string ShipmentStatus { get; init; } = string.Empty;
    public string WarehouseCode { get; init; } = string.Empty;
    public string EventType { get; init; } = string.Empty;
    public string EmployeeName { get; init; } = string.Empty;
    public DateTime ScannedAt { get; init; }
}

public record InventoryBalanceReportRowDto
{
    public long BalanceId { get; init; }
    public string WarehouseCode { get; init; } = string.Empty;
    public string LocationCode { get; init; } = string.Empty;
    public long ProductId { get; init; }
    public string ProductSku { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public string? LotNumber { get; init; }
    public DateOnly? LotExpiryDate { get; init; }
    public int QuantityOnHand { get; init; }
    public int QuantityAllocated { get; init; }
    public int QuantityAvailable { get; init; }
    public int QuantityDamaged { get; init; }
    public int? ReorderPoint { get; init; }
    public bool IsBelowReorderPoint { get; init; }
}

public record InventoryTransactionReportRowDto
{
    public long TransactionId { get; init; }
    public string TransactionNumber { get; init; } = string.Empty;
    public string WarehouseCode { get; init; } = string.Empty;
    public string TransactionType { get; init; } = string.Empty;
    public string ReferenceType { get; init; } = string.Empty;
    public string ReferenceNumber { get; init; } = string.Empty;
    public int TotalQuantity { get; init; }
    public DateTime TransactionDate { get; init; }
}

public record ShippingChargeReportRowDto
{
    public long ChargeId { get; init; }
    public long ShipmentId { get; init; }
    public string TrackingNumber { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string ChargeType { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public bool IsInvoiced { get; init; }
    public string? InvoiceNumber { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record InvoiceReportRowDto
{
    public long InvoiceId { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public long CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string BranchCode { get; init; } = string.Empty;
    public DateOnly InvoiceDate { get; init; }
    public DateOnly DueDate { get; init; }
    public string Status { get; init; } = string.Empty;
    public decimal SubTotal { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal PaidAmount { get; init; }
    public decimal OutstandingAmount { get; init; }
}

public record PaymentReportRowDto
{
    public long PaymentId { get; init; }
    public string PaymentNumber { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string BranchCode { get; init; } = string.Empty;
    public DateTime PaymentDate { get; init; }
    public string PaymentMethod { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public decimal AllocatedAmount { get; init; }
    public decimal UnallocatedAmount { get; init; }
}

public record ReceivableReportRowDto
{
    public long CustomerId { get; init; }
    public string CustomerCode { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string BranchCode { get; init; } = string.Empty;
    public decimal CurrentOutstandingBalance { get; init; }
    public decimal UnbilledChargesAmount { get; init; }
    public decimal TotalCodHoldingAmount { get; init; }
    public decimal? CreditLimit { get; init; }
    public DateTime LastUpdatedDate { get; init; }
}

public record CodCollectionReportRowDto
{
    public long CollectionId { get; init; }
    public string TrackingNumber { get; init; } = string.Empty;
    public string DestinationBranchCode { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string CollectedByEmployeeName { get; init; } = string.Empty;
    public decimal ExpectedCodAmount { get; init; }
    public decimal CollectedAmount { get; init; }
    public decimal VarianceAmount { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime CollectionDate { get; init; }
    public DateTime? VerifiedAt { get; init; }
}

public record CodRemittanceReportRowDto
{
    public long RemittanceId { get; init; }
    public string RemittanceNumber { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string BranchCode { get; init; } = string.Empty;
    public DateTime RemittanceDate { get; init; }
    public string Status { get; init; } = string.Empty;
    public decimal TotalCodAmount { get; init; }
    public decimal TotalServiceFeesDeducted { get; init; }
    public decimal NetRemittanceAmount { get; init; }
    public int ItemsCount { get; init; }
    public DateTime? RemittedAt { get; init; }
}

public record CodReconciliationReportRowDto
{
    public long BranchId { get; init; }
    public string BranchCode { get; init; } = string.Empty;
    public string BranchName { get; init; } = string.Empty;
    public decimal ExpectedCod { get; init; }
    public decimal CollectedByDrivers { get; init; }
    public decimal DepositedToBranch { get; init; }
    public decimal VerifiedByCashier { get; init; }
    public decimal RemittedToCustomers { get; init; }
    public decimal DriverCashInHand { get; init; }
    public decimal PendingCashierVerification { get; init; }
    public decimal DiscrepancyAmount { get; init; }
}

public record ReportExportResult(byte[] Content, string ContentType, string SafeFileName);
