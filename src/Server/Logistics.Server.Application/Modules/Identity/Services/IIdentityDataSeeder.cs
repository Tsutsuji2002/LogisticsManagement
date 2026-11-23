namespace Logistics.Server.Application.Modules.Identity.Services;

/// <summary>
/// Service contract for idempotent initial seeding of permissions, roles, role-permission matrix, and optional bootstrap admin.
/// </summary>
public interface IIdentityDataSeeder
{
    /// <summary>
    /// Seeds standard permissions, system roles, and default role-permission mappings.
    /// Optionally provisions a bootstrap admin if configured in environment variables or configuration.
    /// </summary>
    Task SeedAsync(CancellationToken cancellationToken = default);
}
