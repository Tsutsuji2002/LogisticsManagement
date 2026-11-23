namespace Logistics.Server.Application.Modules.Identity.Services;

/// <summary>
/// Service contract for resolving user permissions with request-scoped caching.
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Returns the effective set of permission codes for the specified user ID across all their active roles.
    /// </summary>
    Task<IReadOnlySet<string>> GetUserPermissionsAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether the user has the specified permission code.
    /// </summary>
    Task<bool> HasPermissionAsync(long userId, string permissionCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the list of authorized branch IDs for the specified user ID.
    /// </summary>
    Task<IReadOnlyList<long>> GetUserBranchIdsAsync(long userId, CancellationToken cancellationToken = default);
}
