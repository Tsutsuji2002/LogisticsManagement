namespace Logistics.Server.Application.Common.Exceptions;

/// <summary>
/// Thrown when a requested resource or entity was not found.
/// </summary>
public class NotFoundException : Exception
{
    public string EntityName { get; }
    public object Key { get; }

    public NotFoundException(string entityName, object key)
        : base($"Resource '{entityName}' with identifier '{key}' was not found.")
    {
        EntityName = entityName;
        Key = key;
    }
}
