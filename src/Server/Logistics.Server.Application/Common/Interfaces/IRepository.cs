using Logistics.Server.Domain.Common;

namespace Logistics.Server.Application.Common.Interfaces;

/// <summary>
/// Generic repository interface for entity persistence supporting both internal PK and external PublicId.
/// </summary>
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<T?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
    IQueryable<T> Query();
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(T entity, CancellationToken cancellationToken = default);
}
