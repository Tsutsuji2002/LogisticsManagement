namespace Logistics.Server.Application.Common.Interfaces;

/// <summary>
/// Provides identity, roles, and branch scope information about the currently executing user.
/// </summary>
public interface ICurrentUserService
{
    long? UserId { get; }
    string? Username { get; }
    string? Email { get; }
    long? EmployeeId { get; }
    long? DefaultBranchId { get; }
    IReadOnlyList<long> AccessibleBranchIds { get; }
    IReadOnlyList<string> Roles { get; }
    bool IsAuthenticated { get; }

    bool IsInRole(string role);
    bool HasBranchAccess(long branchId);
}
