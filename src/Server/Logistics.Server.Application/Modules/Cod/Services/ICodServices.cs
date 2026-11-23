using Logistics.Server.Application.Common.Models;
using Logistics.Server.Application.Modules.Cod.DTOs;
using Logistics.Shared.Contracts.Pagination;

namespace Logistics.Server.Application.Modules.Cod.Services;

public interface ICodCollectionService
{
    Task<PagedResult<CodCollectionDto>> GetCollectionsAsync(
        long? branchId,
        long? employeeId,
        long? customerId,
        string? status,
        bool? isRemitted,
        DateOnly? fromDate,
        DateOnly? toDate,
        PagedRequest request,
        CancellationToken cancellationToken = default);

    Task<CodCollectionDto?> GetCollectionByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<CodCollectionDto?> GetCollectionByShipmentIdAsync(long shipmentId, CancellationToken cancellationToken = default);

    Task<CodCollectionDto> CreateCollectionAsync(CreateCodCollectionRequest request, CancellationToken cancellationToken = default);

    Task<CodCollectionDto> RecordCollectionAsync(long id, RecordCodCollectionRequest request, CancellationToken cancellationToken = default);

    Task<CodCollectionDto> DepositToBranchAsync(long id, string? notes, CancellationToken cancellationToken = default);

    Task<CodCollectionDto> VerifyCollectionAsync(long id, VerifyCodCollectionRequest request, CancellationToken cancellationToken = default);

    Task<CodCollectionDto> RecordFailureAsync(long id, RecordCodFailureRequest request, CancellationToken cancellationToken = default);

    Task<CodCollectionSummaryDto> GetSummaryAsync(long? branchId, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken = default);
}

public interface ICodEmployeeService
{
    Task<PagedResult<CodCollectionDto>> GetMyCollectionsAsync(string? status, PagedRequest request, CancellationToken cancellationToken = default);

    Task<EmployeeCodSummaryDto> GetMySummaryAsync(CancellationToken cancellationToken = default);

    Task<PagedResult<CodCollectionDto>> GetEmployeeCollectionsAsync(long employeeId, string? status, PagedRequest request, CancellationToken cancellationToken = default);

    Task<EmployeeCodSummaryDto> GetEmployeeSummaryAsync(long employeeId, CancellationToken cancellationToken = default);
}

public interface ICodRemittanceService
{
    Task<PagedResult<CodRemittanceDto>> GetRemittancesAsync(
        long? customerId,
        string? status,
        DateOnly? fromDate,
        DateOnly? toDate,
        PagedRequest request,
        CancellationToken cancellationToken = default);

    Task<CodRemittanceDto?> GetRemittanceByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<CodRemittanceDto> CreateRemittanceAsync(CreateCodRemittanceRequest request, CancellationToken cancellationToken = default);

    Task<CodRemittanceDto> AddItemAsync(long remittanceId, AddCodRemittanceItemRequest request, CancellationToken cancellationToken = default);

    Task<CodRemittanceDto> RemoveItemAsync(long remittanceId, long itemId, CancellationToken cancellationToken = default);

    Task<CodRemittanceDto> ConfirmRemittanceAsync(long id, ConfirmCodRemittanceRequest request, CancellationToken cancellationToken = default);

    Task<CodRemittanceDto> CancelRemittanceAsync(long id, string reason, CancellationToken cancellationToken = default);
}

public interface ICodReconciliationService
{
    Task<CodReconciliationSummaryDto> GetSummaryAsync(long? branchId, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken = default);

    Task<EmployeeCodReconciliationDto> ReconcileEmployeeAsync(long employeeId, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken = default);

    Task<BranchCodReconciliationDto> ReconcileBranchAsync(long branchId, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken = default);

    Task<RemittanceReconciliationDto> ReconcileRemittanceAsync(long remittanceId, CancellationToken cancellationToken = default);
}
