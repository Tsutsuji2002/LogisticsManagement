using Logistics.Shared.Contracts.Pagination;

namespace Logistics.Server.Application.Modules.Cod.DTOs;

// ═══════════════════════════════════════════════════════════════
//  1. COD COLLECTIONS DTOs
// ═══════════════════════════════════════════════════════════════

public record CodCollectionDto
{
    public long Id { get; init; }
    public Guid PublicId { get; init; }
    public long ShipmentId { get; init; }
    public string ShipmentTrackingNumber { get; init; } = string.Empty;
    public long CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public long BranchId { get; init; }
    public string BranchName { get; init; } = string.Empty;
    public decimal ExpectedCodAmount { get; init; }
    public decimal CollectedAmount { get; init; }
    public long CurrencyId { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public DateTime CollectionDate { get; init; }
    public long CollectedByEmployeeId { get; init; }
    public string CollectedByEmployeeName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? DriverReceiptNumber { get; init; }
    public long? VerifiedByUserId { get; init; }
    public string? VerifiedByUsername { get; init; }
    public DateTime? VerifiedAt { get; init; }
    public string? Notes { get; init; }
    public bool IsRemitted { get; init; }
    public long? RemittanceId { get; init; }
    public string? RemittanceNumber { get; init; }
}

public record CreateCodCollectionRequest
{
    public long ShipmentId { get; init; }
    public decimal CollectedAmount { get; init; }
    public long CollectedByEmployeeId { get; init; }
    public string? DriverReceiptNumber { get; init; }
    public string? Notes { get; init; }
}

public record RecordCodCollectionRequest
{
    public decimal CollectedAmount { get; init; }
    public long? CollectedByEmployeeId { get; init; }
    public string? DriverReceiptNumber { get; init; }
    public string? Notes { get; init; }
}

public record RecordCodFailureRequest
{
    public string ReasonCode { get; init; } = string.Empty;
    public string? Notes { get; init; }
}

public record VerifyCodCollectionRequest
{
    public string? Notes { get; init; }
}

public record CodCollectionSummaryDto
{
    public decimal TotalExpectedAmount { get; init; }
    public decimal TotalCollectedAmount { get; init; }
    public decimal TotalVerifiedAmount { get; init; }
    public decimal TotalRemittedAmount { get; init; }
    public int PendingCollectionCount { get; init; }
    public int CollectedByDriverCount { get; init; }
    public int DepositedToBranchCount { get; init; }
    public int VerifiedByCashierCount { get; init; }
    public int RemittedToCustomerCount { get; init; }
}

// ═══════════════════════════════════════════════════════════════
//  2. COD REMITTANCES DTOs
// ═══════════════════════════════════════════════════════════════

public record CodRemittanceDto
{
    public long Id { get; init; }
    public Guid PublicId { get; init; }
    public string RemittanceNumber { get; init; } = string.Empty;
    public long CustomerId { get; init; }
    public string CustomerCode { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public DateTime RemittanceDate { get; init; }
    public decimal TotalCodAmount { get; init; }
    public decimal TotalServiceFeesDeducted { get; init; }
    public decimal NetRemittanceAmount { get; init; }
    public long CurrencyId { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public string PaymentMethod { get; init; } = string.Empty;
    public string? PaymentReference { get; init; }
    public string Status { get; init; } = string.Empty;
    public string BankName { get; init; } = string.Empty;
    public string BankAccount { get; init; } = string.Empty;
    public string AccountHolderName { get; init; } = string.Empty;
    public long RemittedByUserId { get; init; }
    public string? RemittedByUsername { get; init; }
    public DateTime? RemittedAt { get; init; }
    public string? Notes { get; init; }
    public int ItemsCount { get; init; }
    public List<CodRemittanceItemDto> Items { get; init; } = [];
}

public record CodRemittanceItemDto
{
    public long Id { get; init; }
    public long CODRemittanceId { get; init; }
    public long CODCollectionId { get; init; }
    public long ShipmentId { get; init; }
    public string ShipmentTrackingNumber { get; init; } = string.Empty;
    public decimal CodAmount { get; init; }
    public decimal DeductedShippingFee { get; init; }
    public decimal NetAmount { get; init; }
}

public record CreateCodRemittanceRequest
{
    public long CustomerId { get; init; }
    public long CurrencyId { get; init; }
    public string PaymentMethod { get; init; } = "BankTransfer";
    public string? PaymentReference { get; init; }
    public string BankName { get; init; } = string.Empty;
    public string BankAccount { get; init; } = string.Empty;
    public string AccountHolderName { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public List<long> CollectionIds { get; init; } = [];
    public decimal ServiceFeesDeducted { get; init; }
}

public record AddCodRemittanceItemRequest
{
    public long CODCollectionId { get; init; }
    public decimal DeductedShippingFee { get; init; }
}

public record ConfirmCodRemittanceRequest
{
    public string? PaymentReference { get; init; }
    public decimal? ActualRemittedAmount { get; init; }
    public string? Notes { get; init; }
}

public record CancelCodRemittanceRequest
{
    public string Reason { get; init; } = string.Empty;
}

// ═══════════════════════════════════════════════════════════════
//  3. EMPLOYEE COD OPERATIONS DTOs
// ═══════════════════════════════════════════════════════════════

public record EmployeeCodSummaryDto
{
    public long EmployeeId { get; init; }
    public string EmployeeCode { get; init; } = string.Empty;
    public string EmployeeName { get; init; } = string.Empty;
    public long BranchId { get; init; }
    public string BranchName { get; init; } = string.Empty;
    public decimal TotalExpectedAmount { get; init; }
    public decimal TotalCollectedAmount { get; init; }
    public decimal TotalDepositedAmount { get; init; }
    public decimal TotalVerifiedAmount { get; init; }
    public decimal OutstandingHoldingAmount => TotalCollectedAmount - TotalDepositedAmount;
    public int AssignedCount { get; init; }
    public int CollectedCount { get; init; }
    public int DepositedCount { get; init; }
    public int VerifiedCount { get; init; }
    public int DisputedCount { get; init; }
}

// ═══════════════════════════════════════════════════════════════
//  4. COD RECONCILIATION DTOs
// ═══════════════════════════════════════════════════════════════

public record CodReconciliationSummaryDto
{
    public decimal TotalShipmentCodExpected { get; init; }
    public decimal TotalCollectedByDrivers { get; init; }
    public decimal TotalDepositedToBranches { get; init; }
    public decimal TotalVerifiedByCashiers { get; init; }
    public decimal TotalRemittedToCustomers { get; init; }
    public decimal TotalOutstandingWithDrivers => TotalCollectedByDrivers - TotalDepositedToBranches;
    public decimal TotalHoldingInBranches => TotalVerifiedByCashiers - TotalRemittedToCustomers;
    public decimal DiscrepancyAmount { get; init; }
    public int TotalCollectionsCount { get; init; }
    public int TotalRemittancesCount { get; init; }
}

public record EmployeeCodReconciliationDto
{
    public long EmployeeId { get; init; }
    public string EmployeeName { get; init; } = string.Empty;
    public long BranchId { get; init; }
    public decimal TotalExpected { get; init; }
    public decimal TotalCollected { get; init; }
    public decimal TotalDeposited { get; init; }
    public decimal DriverCashHolding => TotalCollected - TotalDeposited;
    public bool HasOutstandingHolding => DriverCashHolding > 0;
    public int TotalAssignments { get; init; }
    public int TotalCompletedCollections { get; init; }
}

public record BranchCodReconciliationDto
{
    public long BranchId { get; init; }
    public string BranchCode { get; init; } = string.Empty;
    public string BranchName { get; init; } = string.Empty;
    public decimal TotalCollectedAtBranch { get; init; }
    public decimal TotalVerifiedAtBranch { get; init; }
    public decimal TotalRemittedFromBranch { get; init; }
    public decimal CurrentBranchCashHolding => TotalVerifiedAtBranch - TotalRemittedFromBranch;
    public int CollectionsCount { get; init; }
    public int RemittancesCount { get; init; }
}

public record RemittanceReconciliationDto
{
    public long RemittanceId { get; init; }
    public string RemittanceNumber { get; init; } = string.Empty;
    public long CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public decimal ExpectedCodTotal { get; init; }
    public decimal DeductedFeesTotal { get; init; }
    public decimal CalculatedNetAmount { get; init; }
    public decimal ActualRemittedAmount { get; init; }
    public decimal DiscrepancyAmount => ActualRemittedAmount - CalculatedNetAmount;
    public string Status { get; init; } = string.Empty;
    public bool IsReconciled => DiscrepancyAmount == 0m;
}
