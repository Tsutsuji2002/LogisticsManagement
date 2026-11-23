using Microsoft.AspNetCore.Authorization;

namespace Logistics.Server.API.Authorization;

/// <summary>
/// Convenience attribute for enforcing atomic permission authorization policies.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permission) : base(policy: permission)
    {
    }
}
