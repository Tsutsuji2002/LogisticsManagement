namespace Logistics.Server.Application.Common.Exceptions;

/// <summary>
/// Thrown when a resource operation results in a conflict (e.g. duplicate key, concurrent modification).
/// </summary>
public class ConflictException : Exception
{
    public ConflictException(string message)
        : base(message)
    {
    }

    public ConflictException(string entityName, string key, string value)
        : base($"A '{entityName}' with {key} '{value}' already exists.")
    {
    }
}
