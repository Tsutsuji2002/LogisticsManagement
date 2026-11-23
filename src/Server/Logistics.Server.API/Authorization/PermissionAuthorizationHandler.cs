using System.Security.Claims;
using Logistics.Server.Application.Modules.Identity.Services;
using Microsoft.AspNetCore.Authorization;

namespace Logistics.Server.API.Authorization;

/// <summary>
/// Evaluates whether the current authenticated user has been granted the required permission,
/// consulting the database-backed request-scoped permission service for immediate revocation freshness.
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermissionService _permissionService;

    public PermissionAuthorizationHandler(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? context.User.FindFirst("sub")?.Value;

        if (!long.TryParse(userIdClaim, out var userId))
        {
            return;
        }

        var hasPermission = await _permissionService.HasPermissionAsync(userId, requirement.Permission);
        if (hasPermission)
        {
            context.Succeed(requirement);
        }
    }
}
