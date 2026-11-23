namespace Logistics.Server.Domain.Exceptions;

/// <summary>
/// Thrown when an optimistic concurrency conflict is detected.
/// </summary>
public class ConcurrencyConflictException : DomainException
{
    public ConcurrencyConflictException(string entityName, object key)
        : base($"Entity '{entityName}' with key '{key}' was modified or deleted by another user.", "CONCURRENCY_CONFLICT")
    {
    }
}
