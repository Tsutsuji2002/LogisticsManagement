using Logistics.Server.Application.Common.Events;

namespace Logistics.Server.Application.Common.Interfaces;

/// <summary>
/// Persists integration events into the OutboxMessages table within the active unit-of-work transaction.
/// </summary>
public interface IOutboxService
{
    /// <summary>
    /// Stages an OutboxMessage in the DbContext change tracker so it commits atomically with business data.
    /// Does NOT call SaveChangesAsync.
    /// </summary>
    void Enqueue<TEvent>(TEvent integrationEvent, string aggregateType, string aggregateId, string? correlationId = null)
        where TEvent : IIntegrationEvent;
}

/// <summary>
/// Metadata context passed to an outbox event handler during dispatch.
/// </summary>
public sealed record OutboxEventContext(
    long MessageId,
    string EventType,
    string AggregateType,
    string AggregateId,
    string PayloadJson,
    string? CorrelationId,
    DateTime CreatedAt);

/// <summary>
/// Idempotent handler for a specific integration event type.
/// </summary>
public interface IOutboxEventHandler
{
    string EventType { get; }
    Task HandleAsync(OutboxEventContext context, CancellationToken cancellationToken = default);
}

