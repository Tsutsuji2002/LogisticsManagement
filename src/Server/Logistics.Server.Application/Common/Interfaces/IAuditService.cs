namespace Logistics.Server.Application.Common.Interfaces;

/// <summary>
/// Contract for recording immutable audit trail entries across business domains.
/// </summary>
public interface IAuditService
{
    Task RecordAsync(
        string action,
        string schemaName,
        string tableName,
        string primaryKey,
        object? oldValues,
        object? newValues,
        string? notes = null,
        CancellationToken cancellationToken = default);
}
