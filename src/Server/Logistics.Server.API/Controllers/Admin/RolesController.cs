using Logistics.Server.API.Authorization;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Identity.DTOs;
using Logistics.Server.Application.Modules.Identity.Services;
using Logistics.Shared.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Logistics.Server.API.Controllers.Admin;

/// <summary>
/// Administrative endpoints for reading system roles and atomic permission definitions.
/// </summary>
[Route("api/v1/admin")]
public class RolesController : BaseApiController
{
    private readonly IUserManagementService _userManagementService;

    public RolesController(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    /// <summary>
    /// Gets all active system roles and their assigned permissions.
    /// </summary>
    [HttpGet("roles")]
    [HasPermission(AppPermissions.Identity.RolesRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RoleDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
    {
        var roles = await _userManagementService.GetRolesAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<RoleDto>>.Ok(roles));
    }

    /// <summary>
    /// Gets all registered atomic system permissions grouped by module.
    /// </summary>
    [HttpGet("permissions")]
    [HasPermission(AppPermissions.Identity.RolesRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PermissionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPermissions(CancellationToken cancellationToken)
    {
        var permissions = await _userManagementService.GetPermissionsAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<PermissionDto>>.Ok(permissions));
    }
}
