using Logistics.Server.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Logistics.Server.Infrastructure.Services;

/// <summary>
/// Evaluates and authorizes the active operating branch scope for incoming API requests.
/// </summary>
public class CurrentBranchContext : ICurrentBranchContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICurrentUserService _currentUserService;

    public CurrentBranchContext(IHttpContextAccessor httpContextAccessor, ICurrentUserService currentUserService)
    {
        _httpContextAccessor = httpContextAccessor;
        _currentUserService = currentUserService;
    }

    public long? ActiveBranchId
    {
        get
        {
            var headerValue = _httpContextAccessor.HttpContext?.Request.Headers["X-Branch-Id"].FirstOrDefault();
            if (!string.IsNullOrEmpty(headerValue) && long.TryParse(headerValue, out var branchId))
            {
                return branchId;
            }

            return _currentUserService.DefaultBranchId;
        }
    }

    public bool HasActiveBranch => ActiveBranchId.HasValue;

    public bool IsAuthorizedForActiveBranch()
    {
        if (!ActiveBranchId.HasValue) return false;
        return _currentUserService.HasBranchAccess(ActiveBranchId.Value);
    }
}
