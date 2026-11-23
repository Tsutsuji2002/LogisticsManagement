using Microsoft.AspNetCore.Authorization;

namespace Logistics.Server.API.Authorization;

/// <summary>
/// Authorization requirement that validates whether the user holds a specific atomic permission code.
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        Permission = permission ?? throw new ArgumentNullException(nameof(permission));
    }
}
