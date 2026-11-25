using Logistics.Client.Application.Interfaces;
using Logistics.Client.Application.Permissions;

namespace Logistics.Client.Infrastructure.Services;

public sealed class ClientAuthorizationService : IAuthorizationService
{
    private readonly IAuthenticationService _authService;

    public ClientAuthorizationService(IAuthenticationService authService)
    {
        _authService = authService;
    }

    public IReadOnlySet<string> Permissions
        => new HashSet<string>(_authService.CurrentUser?.Permissions ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<string> Roles
        => _authService.CurrentUser?.Roles ?? Array.Empty<string>();

    public bool IsInRole(string roleCode)
        => Roles.Any(r => string.Equals(r, roleCode, StringComparison.OrdinalIgnoreCase));

    public bool HasPermission(string permissionCode)
    {
        if (!_authService.IsAuthenticated) return false;
        if (IsInRole(ClientPermissions.Roles.SuperAdmin)) return true;
        return Permissions.Contains(permissionCode);
    }

    public bool HasAnyPermission(params string[] permissionCodes)
    {
        if (!_authService.IsAuthenticated) return false;
        if (IsInRole(ClientPermissions.Roles.SuperAdmin)) return true;
        return permissionCodes.Any(p => Permissions.Contains(p));
    }
}
