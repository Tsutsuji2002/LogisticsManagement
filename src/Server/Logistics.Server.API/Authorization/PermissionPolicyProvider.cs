using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Logistics.Server.API.Authorization;

/// <summary>
/// Dynamic policy provider that synthesizes an authorization policy containing a <see cref="PermissionRequirement"/>
/// for any requested policy identifier matching dot-separated permission codes.
/// </summary>
public class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() =>
        _fallbackPolicyProvider.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() =>
        _fallbackPolicyProvider.GetFallbackPolicyAsync();

    public async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // Try getting an explicitly configured static policy first
        var policy = await _fallbackPolicyProvider.GetPolicyAsync(policyName);
        if (policy != null)
        {
            return policy;
        }

        // Dynamically build a permission-enforcing policy for permission codes
        var policyBuilder = new AuthorizationPolicyBuilder();
        policyBuilder.RequireAuthenticatedUser();
        policyBuilder.AddRequirements(new PermissionRequirement(policyName));
        return policyBuilder.Build();
    }
}
