namespace Logistics.Server.Application.Common.Interfaces;

/// <summary>
/// Abstraction over DateTime for testability.
/// </summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
