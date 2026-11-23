namespace Logistics.Server.Domain.Common;

/// <summary>
/// Base class for all operational domain entities providing common identity, public GUID, and audit fields.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Internal surrogate primary key (BIGINT IDENTITY).
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Public globally unique identifier for external API and client references.
    /// </summary>
    public Guid PublicId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// UTC timestamp when the entity was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User identifier who created this record.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// UTC timestamp when the entity was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// User identifier who last modified this record.
    /// </summary>
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Concurrency token for optimistic locking in SQL Server (ROWVERSION).
    /// </summary>
    public byte[]? RowVersion { get; set; }

    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Domain events raised by this entity instance.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Records a domain event on this entity.
    /// </summary>
    public void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>
    /// Protected helper for subclasses to raise domain events.
    /// </summary>
    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>
    /// Clears pending domain events after successful publication.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
