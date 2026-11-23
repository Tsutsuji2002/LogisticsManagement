namespace Logistics.Server.Domain.Exceptions;

/// <summary>
/// Thrown when an expected entity does not exist in the domain.
/// </summary>
public class EntityNotFoundException : DomainException
{
    public string EntityName { get; }
    public object Key { get; }

    public EntityNotFoundException(string entityName, object key)
        : base($"Entity '{entityName}' with key '{key}' was not found.", "ENTITY_NOT_FOUND")
    {
        EntityName = entityName;
        Key = key;
    }
}
