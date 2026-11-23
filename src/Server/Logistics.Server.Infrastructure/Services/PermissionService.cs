using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Identity.Services;
using Logistics.Server.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Logistics.Server.Infrastructure.Services;

/// <summary>
/// Scoped service for evaluating user permissions and branch access with request-level caching.
/// </summary>
public class PermissionService : IPermissionService
{
    private readonly ApplicationDbContext _context;
    private readonly Dictionary<long, IReadOnlySet<string>> _cachedPermissions = new();
    private readonly Dictionary<long, IReadOnlyList<long>> _cachedBranches = new();

    public PermissionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlySet<string>> GetUserPermissionsAsync(long userId, CancellationToken cancellationToken = default)
    {
        if (_cachedPermissions.TryGetValue(userId, out var cached))
        {
            return cached;
        }

        // Check if user has SUPER_ADMIN role
        var isSuperAdmin = await _context.UserRoles
            .AsNoTracking()
            .AnyAsync(ur => ur.UserId == userId && ur.Role.IsActive && ur.Role.Code == AppRoles.SuperAdmin, cancellationToken);

        HashSet<string> permissionCodes;

        if (isSuperAdmin)
        {
            // Super admins have all registered permissions
            var allPermissions = await _context.Permissions
                .AsNoTracking()
                .Select(p => p.Code)
                .ToListAsync(cancellationToken);

            permissionCodes = allPermissions.ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
        else
        {
            var userPermissions = await _context.UserRoles
                .AsNoTracking()
                .Where(ur => ur.UserId == userId && ur.Role.IsActive)
                .SelectMany(ur => ur.Role.RolePermissions)
                .Select(rp => rp.Permission.Code)
                .Distinct()
                .ToListAsync(cancellationToken);

            permissionCodes = userPermissions.ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        _cachedPermissions[userId] = permissionCodes;
        return permissionCodes;
    }

    public async Task<bool> HasPermissionAsync(long userId, string permissionCode, CancellationToken cancellationToken = default)
    {
        var permissions = await GetUserPermissionsAsync(userId, cancellationToken);
        return permissions.Contains(permissionCode);
    }

    public async Task<IReadOnlyList<long>> GetUserBranchIdsAsync(long userId, CancellationToken cancellationToken = default)
    {
        if (_cachedBranches.TryGetValue(userId, out var cached))
        {
            return cached;
        }

        // Check if user is SuperAdmin (access to all branches)
        var isSuperAdmin = await _context.UserRoles
            .AsNoTracking()
            .AnyAsync(ur => ur.UserId == userId && ur.Role.IsActive && ur.Role.Code == AppRoles.SuperAdmin, cancellationToken);

        List<long> branchIds;

        if (isSuperAdmin)
        {
            branchIds = await _context.Branches
                .AsNoTracking()
                .Where(b => b.IsActive)
                .Select(b => b.Id)
                .ToListAsync(cancellationToken);
        }
        else
        {
            branchIds = await _context.UserBranchAccesses
                .AsNoTracking()
                .Where(uba => uba.UserId == userId)
                .Select(uba => uba.BranchId)
                .Distinct()
                .ToListAsync(cancellationToken);
        }

        _cachedBranches[userId] = branchIds;
        return branchIds;
    }
}
