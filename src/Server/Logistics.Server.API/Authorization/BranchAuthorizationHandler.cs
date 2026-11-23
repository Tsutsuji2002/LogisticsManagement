using Logistics.Server.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace Logistics.Server.API.Authorization;

/// <summary>
/// Authorization requirement verifying that the authenticated user possesses valid access to the active operating branch.
/// </summary>
public class BranchAccessRequirement : IAuthorizationRequirement
{
}

/// <summary>
/// Authorization handler evaluating whether the user is authorized for the active branch specified in the request context.
/// </summary>
public class BranchAuthorizationHandler : AuthorizationHandler<BranchAccessRequirement>
{
    private readonly ICurrentBranchContext _branchContext;

    public BranchAuthorizationHandler(ICurrentBranchContext branchContext)
    {
        _branchContext = branchContext;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        BranchAccessRequirement requirement)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        if (!_branchContext.HasActiveBranch)
        {
            // If no specific branch is requested or context has no active branch, deny branch-restricted operation
            return Task.CompletedTask;
        }

        if (_branchContext.IsAuthorizedForActiveBranch())
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
