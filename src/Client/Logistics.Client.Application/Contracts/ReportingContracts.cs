namespace Logistics.Client.Application.Contracts.Reporting;

public record ReportFilterRequest
{
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public string TimeZoneId { get; init; } = "SE Asia Standard Time";
    public long? BranchId { get; init; }
    public long? CustomerId { get; init; }
    public long? EmployeeId { get; init; }
    public long? WarehouseId { get; init; }
    public long? CarrierId { get; init; }
    public string? Status { get; init; }
    public string? Search { get; init; }
}

public record DateTrendPointDto(DateOnly Date, int Count, decimal Value = 0m);
public record StatusBreakdownDto(string Status, int Count, decimal Amount = 0m);
public record BranchBreakdownDto(long BranchId, string BranchCode, string BranchName, int Count, decimal Amount = 0m);

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

public record CompanyOverviewDashboardDto
{
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public int TotalActiveBranches { get; init; }
    public int TotalActiveEmployees { get; init; }
    public int TotalActiveCustomers { get; init; }
    public int ShipmentsCurrentlyInTransit { get; init; }
    public int ShipmentsAwaitingPickup { get; init; }
    public int ShipmentsAwaitingDelivery { get; init; }
    public int PendingWarehouseReceipts { get; init; }
    public int PendingWarehouseDispatches { get; init; }
    public int TotalOrders { get; init; }
    public int TotalShipments { get; init; }
    public int DeliveredShipments { get; init; }
    public int FailedDeliveryAttempts { get; init; }
    public int ReturnedShipments { get; init; }
    public InventorySnapshotDto InventorySummary { get; init; } = new();
    public FinanceSnapshotDto FinancialSummary { get; init; } = new();
    public CodSnapshotDto CodSummary { get; init; } = new();
}

public record BranchDashboardDto
{
    public long BranchId { get; init; }
    public string BranchCode { get; init; } = string.Empty;
    public string BranchName { get; init; } = string.Empty;
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
    public List<BranchBreakdownDto> ByOriginBranch { get; init; } = [];
    public List<DateTrendPointDto> CreationTrend { get; init; } = [];
    public List<DateTrendPointDto> DeliveryTrend { get; init; } = [];
}

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
}

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
}

public record FinanceDashboardDto
{
    public decimal TotalShipmentChargesAmount { get; init; }
    public decimal UnbilledShipmentChargesAmount { get; init; }
    public int DraftInvoicesCount { get; init; }
    public decimal DraftInvoicesTotal { get; init; }
    public int IssuedAndActiveInvoicesCount { get; init; }
    public decimal IssuedInvoicesTotal { get; init; }
    public decimal TotalPaymentsConfirmed { get; init; }
    public decimal TotalPaymentsAllocated { get; init; }
    public decimal TotalUnallocatedPaymentCredit { get; init; }
    public decimal AuthoritativeCustomerReceivables { get; init; }
    public decimal AuthoritativeUnbilledBalance { get; init; }
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
}

// Report Rows
public record OrderReportRowDto(long OrderId, string OrderNumber, DateTime OrderDate, string OrderStatus, string PaymentStatus, long BranchId, string BranchCode, long CustomerId, string CustomerName, int ItemsCount, int PackagesCount, decimal TotalWeightKg, decimal DeclaredValue, decimal CodAmount);
public record ShipmentReportRowDto(long ShipmentId, string TrackingNumber, string? OrderNumber, string ShipmentStatus, string ServiceType, string OriginBranchCode, string DestinationBranchCode, string CustomerName, string? CarrierCode, decimal ChargeableWeightKg, decimal CodAmount, decimal TotalAmount, DateTime CreatedAt, DateTime? EstimatedDeliveryDate, DateTime? ActualDeliveryDate);
public record DeliveryPerformanceRowDto(long ShipmentId, string TrackingNumber, string DestinationBranchCode, string? CarrierCode, string ShipmentStatus, int AttemptCount, DateTime? PickupDate, DateTime? EstimatedDeliveryDate, DateTime? ActualDeliveryDate, double? DeliveryDurationHours, bool? IsOnTime);
public record CarrierPerformanceRowDto(long CarrierId, string CarrierCode, string CarrierName, int TotalShipments, int DeliveredShipments, int FailedOrReturnedShipments, int ApiCallsTotal, int ApiCallsSucceeded, int ApiCallsFailed, int WebhooksTotal, int WebhooksSucceeded, int SyncJobsTotal, int SyncJobsFailed, int TrackingEventsTotal, int TrackingEventsFailed);
public record WarehouseReceiptReportRowDto(long ReceiptId, string ReceiptNumber, string WarehouseCode, string ReceiptType, string Status, DateTime ActualArrivalDate, int LineItemsCount, int TotalReceivedQuantity, int TotalDamagedQuantity);
public record WarehouseDispatchReportRowDto(long DispatchId, string DispatchNumber, string WarehouseCode, string DispatchType, string Status, DateTime? DispatchedAt, int LineItemsCount, int PackagesCount);
public record ParcelHandlingReportRowDto(long EventId, string TrackingNumber, string PackageBarcode, string ShipmentStatus, string WarehouseCode, string EventType, string EmployeeName, DateTime ScannedAt);
public record InventoryBalanceReportRowDto(long BalanceId, string WarehouseCode, string LocationCode, long ProductId, string ProductSku, string ProductName, string? LotNumber, DateOnly? LotExpiryDate, int QuantityOnHand, int QuantityAllocated, int QuantityAvailable, int QuantityDamaged, int? ReorderPoint, bool IsBelowReorderPoint);
public record InventoryTransactionReportRowDto(long TransactionId, string TransactionNumber, string WarehouseCode, string TransactionType, string ReferenceType, string ReferenceNumber, int TotalQuantity, DateTime TransactionDate);
public record ShippingChargeReportRowDto(long ChargeId, long ShipmentId, string TrackingNumber, string CustomerName, string ChargeType, decimal Amount, string CurrencyCode, bool IsInvoiced, string? InvoiceNumber, DateTime CreatedAt);
public record InvoiceReportRowDto(long InvoiceId, string InvoiceNumber, long CustomerId, string CustomerName, string BranchCode, DateOnly InvoiceDate, DateOnly DueDate, string Status, decimal SubTotal, decimal TaxAmount, decimal TotalAmount, decimal PaidAmount, decimal OutstandingAmount);
public record PaymentReportRowDto(long PaymentId, string PaymentNumber, string CustomerName, string BranchCode, DateTime PaymentDate, string PaymentMethod, string Status, decimal Amount, decimal AllocatedAmount, decimal UnallocatedAmount);
public record ReceivableReportRowDto(long CustomerId, string CustomerCode, string CustomerName, string BranchCode, decimal CurrentOutstandingBalance, decimal UnbilledChargesAmount, decimal TotalCodHoldingAmount, decimal? CreditLimit, DateTime LastUpdatedDate);
public record CodCollectionReportRowDto(long CollectionId, string TrackingNumber, string DestinationBranchCode, string CustomerName, string CollectedByEmployeeName, decimal ExpectedCodAmount, decimal CollectedAmount, decimal VarianceAmount, string Status, DateTime CollectionDate, DateTime? VerifiedAt);
public record CodRemittanceReportRowDto(long RemittanceId, string RemittanceNumber, string CustomerName, string BranchCode, DateTime RemittanceDate, string Status, decimal TotalCodAmount, decimal TotalServiceFeesDeducted, decimal NetRemittanceAmount, int ItemsCount, DateTime? RemittedAt);
public record CodReconciliationReportRowDto(long BranchId, string BranchCode, string BranchName, decimal ExpectedCod, decimal CollectedByDrivers, decimal DepositedToBranch, decimal VerifiedByCashier, decimal RemittedToCustomers, decimal DriverCashInHand, decimal PendingCashierVerification, decimal DiscrepancyAmount);

// Scheduled Reports & Background Jobs
public record ReportDefinitionDto(long Id, string Code, string Name, string Category, string? Description, bool IsActive);
public record ScheduledReportDto(long Id, Guid PublicId, long ReportDefinitionId, string ReportCode, string ReportName, long? BranchId, string? BranchName, string ScheduleCron, string TimeZoneId, string OutputFormat, string RecipientEmails, DateTime? LastRunAt, DateTime NextRunAt, bool IsActive, DateTime CreatedAt);
public record CreateScheduledReportRequest(long ReportDefinitionId, long? BranchId, string ScheduleCron, string TimeZoneId, string OutputFormat, string RecipientEmails, bool IsActive = true);
public record ScheduledReportRunResultDto(long ScheduledReportId, string ExecutionId, string Status, bool IsReportGenerationSupported, string Message, DateTime ExecutedAt);
public record BackgroundJobLogDto(long Id, string JobName, string ExecutionId, string? JobReference, string TriggerType, DateTime StartedAt, DateTime? CompletedAt, string Status, int AttemptCount, int MaxAttempts, string? ErrorSummary, DateTime CreatedAt);
public record OutboxMessageDto(long Id, string EventType, string AggregateType, string AggregateId, string Status, int RetryCount, string? ErrorMessage, DateTime CreatedAt);
public record BackgroundProcessingHealthDto(DateTime TimestampUtc, string Status, int PendingOutboxCount, int FailedOutboxCount, int PendingJobsCount, int FailedJobsCount, int DueScheduledReportsCount);
public record DownloadedFileDto(byte[] Content, string ContentType, string FileName);
