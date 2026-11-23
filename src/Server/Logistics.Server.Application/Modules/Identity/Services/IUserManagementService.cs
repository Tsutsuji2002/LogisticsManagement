using Logistics.Server.Application.Common.Models;
using Logistics.Server.Application.Modules.Identity.DTOs;
using Logistics.Shared.Contracts.Pagination;

namespace Logistics.Server.Application.Modules.Identity.Services;

/// <summary>
/// Service contract for administrative management of user accounts, roles, and branch assignments.
/// </summary>
public interface IUserManagementService
{
    Task<PagedResult<UserAccountDto>> GetUsersAsync(
        string? search,
        string? roleCode,
        long? branchId,
        bool? isActive,
        PagedRequest request,
        CancellationToken cancellationToken = default);

    Task<UserAccountDto?> GetUserByPublicIdAsync(
        Guid publicId,
        CancellationToken cancellationToken = default);

    Task<UserAccountDto> CreateUserAsync(
        CreateUserRequest request,
        string createdByUsername,
        CancellationToken cancellationToken = default);

    Task<UserAccountDto> UpdateUserAsync(
        Guid publicId,
        UpdateUserRequest request,
        string updatedByUsername,
        CancellationToken cancellationToken = default);

    Task UpdateUserStatusAsync(
        Guid publicId,
        UpdateUserStatusRequest request,
        string updatedByUsername,
        CancellationToken cancellationToken = default);

    Task UpdateUserRolesAsync(
        Guid publicId,
        UpdateUserRolesRequest request,
        string updatedByUsername,
        CancellationToken cancellationToken = default);

    Task UpdateUserBranchesAsync(
        Guid publicId,
        UpdateUserBranchesRequest request,
        string updatedByUsername,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuthorizedBranchDto>> GetUserBranchesAsync(
        Guid publicId,
        CancellationToken cancellationToken = default);

    Task GrantUserBranchAccessAsync(
        Guid publicId,
        GrantBranchAccessRequest request,
        string grantedByUsername,
        CancellationToken cancellationToken = default);

    Task RevokeUserBranchAccessAsync(
        Guid publicId,
        long branchId,
        string revokedByUsername,
        CancellationToken cancellationToken = default);

    Task AdminResetPasswordAsync(
        Guid publicId,
        AdminResetPasswordRequest request,
        string updatedByUsername,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync(CancellationToken cancellationToken = default);
}
