namespace Logistics.Server.Domain.Common;

/// <summary>
/// Base implementation for domain events.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
