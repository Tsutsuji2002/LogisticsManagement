namespace Logistics.Server.Application.Common.Interfaces;

/// <summary>
/// Unit of work interface for coordinating transactional persistence.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
