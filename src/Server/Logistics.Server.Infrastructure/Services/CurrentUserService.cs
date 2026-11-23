using System.Security.Claims;
using Logistics.Server.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Logistics.Server.Infrastructure.Services;

/// <summary>
/// Resolves current user details and branch access permissions from the active HTTP context.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public long? UserId
    {
        get
        {
            var value = User?.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User?.FindFirstValue("sub")
                     ?? User?.FindFirstValue("userId");
            return long.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Username => User?.Identity?.Name
                            ?? User?.FindFirstValue("unique_name")
                            ?? User?.FindFirstValue("preferred_username");

    public string? Email => User?.FindFirstValue(ClaimTypes.Email);

    public long? EmployeeId
    {
        get
        {
            var value = User?.FindFirstValue("employee_id");
            return long.TryParse(value, out var id) ? id : null;
        }
    }

    public long? DefaultBranchId
    {
        get
        {
            var value = User?.FindFirstValue("branch_id");
            return long.TryParse(value, out var id) ? id : null;
        }
    }

    public IReadOnlyList<long> AccessibleBranchIds
    {
        get
        {
            if (User == null) return [];
            return User.FindAll("branch_access")
                .Select(c => long.TryParse(c.Value, out var id) ? id : (long?)null)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToList();
        }
    }

    public IReadOnlyList<string> Roles
    {
        get
        {
            if (User == null) return [];
            return User.FindAll(ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();
        }
    }

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public bool IsInRole(string role) => User?.IsInRole(role) == true;

    public bool HasBranchAccess(long branchId)
    {
        if (IsInRole("Admin") || IsInRole("SuperAdmin") || IsInRole("SUPER_ADMIN"))
            return true;

        if (DefaultBranchId == branchId)
            return true;

        return AccessibleBranchIds.Contains(branchId);
    }
}
