using System.Text.Json;
using Logistics.Server.Application.Common.Exceptions;
using Logistics.Server.Application.Common.Interfaces;
using Logistics.Server.Application.Common.Models;
using Logistics.Server.Application.Modules.Identity.DTOs;
using Logistics.Server.Application.Modules.Identity.Services;
using Logistics.Server.Domain.Entities.Audit;
using Logistics.Server.Domain.Entities.Identity;
using Logistics.Server.Infrastructure.Persistence;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Logistics.Server.Infrastructure.Services;

/// <summary>
/// Service managing administrative user provisioning, role assignments, branch access control,
/// status toggling, and audit logging.
/// </summary>
public class UserManagementService : IUserManagementService
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly ICurrentUserService _currentUserService;

    public UserManagementService(
        ApplicationDbContext context,
        IPasswordHasherService passwordHasher,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResult<UserAccountDto>> GetUsersAsync(
        string? search,
        string? roleCode,
        long? branchId,
        bool? isActive,
        PagedRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _context.UserAccounts
            .AsNoTracking()
            .Include(u => u.Employee)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Include(u => u.BranchAccesses)
                .ThenInclude(ba => ba.Branch)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u => u.Username.Contains(term) || u.Email.Contains(term) ||
                (u.Employee != null && (u.Employee.FirstName.Contains(term) || u.Employee.LastName.Contains(term))));
        }

        if (!string.IsNullOrWhiteSpace(roleCode))
        {
            query = query.Where(u => u.UserRoles.Any(ur => ur.Role.Code == roleCode));
        }

        if (branchId.HasValue)
        {
            query = query.Where(u => u.BranchAccesses.Any(ba => ba.BranchId == branchId.Value));
        }

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => new UserAccountDto(
                u.Id,
                u.PublicId,
                u.Username,
                u.Email,
                u.EmployeeId,
                u.Employee != null ? u.Employee.FullName : null,
                u.IsActive,
                u.IsLockedOut,
                u.LockoutEndAt,
                u.FailedLoginAttempts,
                u.LastLoginAt,
                u.CreatedAt,
                u.UserRoles.Where(ur => ur.Role.IsActive).Select(ur => ur.Role.Code).ToList(),
                u.BranchAccesses.Where(ba => ba.Branch.IsActive).Select(ba => new AuthorizedBranchDto(
                    ba.BranchId,
                    ba.Branch.PublicId,
                    ba.Branch.Code,
                    ba.Branch.Name,
                    ba.IsDefault)).ToList()
            ))
            .ToListAsync(cancellationToken);

        return new PagedResult<UserAccountDto>(items, totalCount, request.Page, request.PageSize);
    }

    public async Task<UserAccountDto?> GetUserByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default)
    {
        var user = await _context.UserAccounts
            .AsNoTracking()
            .Include(u => u.Employee)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Include(u => u.BranchAccesses)
                .ThenInclude(ba => ba.Branch)
            .FirstOrDefaultAsync(u => u.PublicId == publicId, cancellationToken);

        if (user == null) return null;

        return MapToDto(user);
    }

    public async Task<UserAccountDto> CreateUserAsync(
        CreateUserRequest request,
        string createdByUsername,
        CancellationToken cancellationToken = default)
    {
        var normalizedUsername = request.Username.Trim();
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // Check unique username
        if (await _context.UserAccounts.AnyAsync(u => u.Username == normalizedUsername, cancellationToken))
        {
            throw new ConflictException("UserAccount", "Username", normalizedUsername);
        }

        // Check unique email
        if (await _context.UserAccounts.AnyAsync(u => u.Email == normalizedEmail, cancellationToken))
        {
            throw new ConflictException("UserAccount", "Email", normalizedEmail);
        }

        // Validate employee linkage
        if (request.EmployeeId.HasValue)
        {
            var employeeExists = await _context.Employees.AnyAsync(e => e.Id == request.EmployeeId.Value, cancellationToken);
            if (!employeeExists)
            {
                throw new NotFoundException("Employee", request.EmployeeId.Value);
            }

            var employeeAlreadyLinked = await _context.UserAccounts.AnyAsync(u => u.EmployeeId == request.EmployeeId.Value, cancellationToken);
            if (employeeAlreadyLinked)
            {
                throw new ConflictException("Employee is already associated with another user account.");
            }
        }

        var user = new UserAccount
        {
            Username = normalizedUsername,
            Email = normalizedEmail,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            SecurityStamp = Guid.NewGuid().ToString("N"),
            EmployeeId = request.EmployeeId,
            IsActive = true,
            CreatedBy = createdByUsername
        };

        // Assign roles
        if (request.Roles?.Count > 0)
        {
            var roles = await _context.Roles
                .Where(r => request.Roles.Contains(r.Code) && r.IsActive)
                .ToListAsync(cancellationToken);

            foreach (var role in roles)
            {
                user.UserRoles.Add(new UserRole
                {
                    RoleId = role.Id,
                    AssignedBy = createdByUsername,
                    AssignedAt = DateTime.UtcNow
                });
            }
        }

        // Assign branch accesses
        if (request.BranchIds?.Count > 0)
        {
            var branches = await _context.Branches
                .Where(b => request.BranchIds.Contains(b.Id) && b.IsActive)
                .ToListAsync(cancellationToken);

            foreach (var branch in branches)
            {
                user.BranchAccesses.Add(new UserBranchAccess
                {
                    BranchId = branch.Id,
                    IsDefault = request.DefaultBranchId.HasValue && request.DefaultBranchId.Value == branch.Id,
                    GrantedBy = createdByUsername,
                    GrantedAt = DateTime.UtcNow
                });
            }
        }

        await _context.UserAccounts.AddAsync(user, cancellationToken);
        await RecordAuditLogAsync("Create", "UserAccounts", user.Username, null, new { user.Username, user.Email, user.EmployeeId }, createdByUsername, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return await GetUserByPublicIdAsync(user.PublicId, cancellationToken)
            ?? throw new InvalidOperationException("Failed to load newly created user account.");
    }

    public async Task<UserAccountDto> UpdateUserAsync(
        Guid publicId,
        UpdateUserRequest request,
        string updatedByUsername,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.UserAccounts
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.PublicId == publicId, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException("UserAccount", publicId);
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (normalizedEmail != user.Email)
        {
            var emailExists = await _context.UserAccounts.AnyAsync(u => u.Email == normalizedEmail && u.Id != user.Id, cancellationToken);
            if (emailExists)
            {
                throw new ConflictException("UserAccount", "Email", normalizedEmail);
            }
        }

        if (request.EmployeeId.HasValue && request.EmployeeId.Value != user.EmployeeId)
        {
            var employeeExists = await _context.Employees.AnyAsync(e => e.Id == request.EmployeeId.Value, cancellationToken);
            if (!employeeExists)
            {
                throw new NotFoundException("Employee", request.EmployeeId.Value);
            }

            var employeeAlreadyLinked = await _context.UserAccounts.AnyAsync(u => u.EmployeeId == request.EmployeeId.Value && u.Id != user.Id, cancellationToken);
            if (employeeAlreadyLinked)
            {
                throw new ConflictException("Employee is already linked to another account.");
            }
        }

        var oldValues = new { user.Email, user.EmployeeId };
        user.Email = normalizedEmail;
        user.EmployeeId = request.EmployeeId;
        user.UpdatedBy = updatedByUsername;
        user.UpdatedAt = DateTime.UtcNow;

        await RecordAuditLogAsync("Update", "UserAccounts", user.Id.ToString(), oldValues, new { user.Email, user.EmployeeId }, updatedByUsername, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return await GetUserByPublicIdAsync(publicId, cancellationToken)
            ?? throw new NotFoundException("UserAccount", publicId);
    }

    public async Task UpdateUserStatusAsync(
        Guid publicId,
        UpdateUserStatusRequest request,
        string updatedByUsername,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.UserAccounts.FirstOrDefaultAsync(u => u.PublicId == publicId, cancellationToken);
        if (user == null)
        {
            throw new NotFoundException("UserAccount", publicId);
        }

        // Prevent self-deactivation
        if (_currentUserService.UserId.HasValue && _currentUserService.UserId.Value == user.Id && !request.IsActive)
        {
            throw new ForbiddenAccessException("Administrators cannot deactivate their own active user account.");
        }

        var oldStatus = user.IsActive;
        user.IsActive = request.IsActive;

        if (request.UnlockAccount)
        {
            user.IsLockedOut = false;
            user.LockoutEndAt = null;
            user.FailedLoginAttempts = 0;
        }

        if (!request.IsActive)
        {
            // Revoke all tokens & sessions upon deactivation
            user.SecurityStamp = Guid.NewGuid().ToString("N");

            var activeTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == user.Id && !rt.IsRevoked)
                .ToListAsync(cancellationToken);

            foreach (var token in activeTokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
            }

            var activeSessions = await _context.UserSessions
                .Where(s => s.UserId == user.Id && !s.IsTerminated)
                .ToListAsync(cancellationToken);

            foreach (var session in activeSessions)
            {
                session.IsTerminated = true;
            }
        }

        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = updatedByUsername;

        await RecordAuditLogAsync("StatusChange", "UserAccounts", user.Id.ToString(), new { IsActive = oldStatus }, new { request.IsActive, request.UnlockAccount }, updatedByUsername, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateUserRolesAsync(
        Guid publicId,
        UpdateUserRolesRequest request,
        string updatedByUsername,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.UserAccounts
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.PublicId == publicId, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException("UserAccount", publicId);
        }

        // Prevent self-revocation of administrative role
        if (_currentUserService.UserId.HasValue && _currentUserService.UserId.Value == user.Id)
        {
            var currentlyAdmin = user.UserRoles.Any(ur => ur.Role?.Code == "SUPER_ADMIN");
            var willBeAdmin = request.Roles.Contains("SUPER_ADMIN");
            if (currentlyAdmin && !willBeAdmin)
            {
                throw new ForbiddenAccessException("Administrators cannot revoke their own SuperAdmin role.");
            }
        }

        var availableRoles = await _context.Roles
            .Where(r => request.Roles.Contains(r.Code) && r.IsActive)
            .ToListAsync(cancellationToken);

        // Remove existing roles
        _context.UserRoles.RemoveRange(user.UserRoles);

        // Add new roles
        foreach (var role in availableRoles)
        {
            user.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id,
                AssignedBy = updatedByUsername,
                AssignedAt = DateTime.UtcNow
            });
        }

        // Invalidate security stamp to force token refresh
        user.SecurityStamp = Guid.NewGuid().ToString("N");
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = updatedByUsername;

        await RecordAuditLogAsync("RoleAssignment", "UserRoles", user.Id.ToString(), null, request.Roles, updatedByUsername, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateUserBranchesAsync(
        Guid publicId,
        UpdateUserBranchesRequest request,
        string updatedByUsername,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.UserAccounts
            .Include(u => u.BranchAccesses)
            .FirstOrDefaultAsync(u => u.PublicId == publicId, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException("UserAccount", publicId);
        }

        var availableBranches = await _context.Branches
            .Where(b => request.BranchIds.Contains(b.Id) && b.IsActive)
            .ToListAsync(cancellationToken);

        _context.UserBranchAccesses.RemoveRange(user.BranchAccesses);

        foreach (var branch in availableBranches)
        {
            user.BranchAccesses.Add(new UserBranchAccess
            {
                UserId = user.Id,
                BranchId = branch.Id,
                IsDefault = request.DefaultBranchId.HasValue && request.DefaultBranchId.Value == branch.Id,
                GrantedBy = updatedByUsername,
                GrantedAt = DateTime.UtcNow
            });
        }

        user.SecurityStamp = Guid.NewGuid().ToString("N");
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = updatedByUsername;

        await RecordAuditLogAsync("BranchAccessAssignment", "UserBranchAccess", user.Id.ToString(), null, request.BranchIds, updatedByUsername, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuthorizedBranchDto>> GetUserBranchesAsync(Guid publicId, CancellationToken cancellationToken = default)
    {
        var user = await _context.UserAccounts
            .AsNoTracking()
            .Include(u => u.BranchAccesses)
                .ThenInclude(ba => ba.Branch)
            .FirstOrDefaultAsync(u => u.PublicId == publicId, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException("UserAccount", publicId);
        }

        return user.BranchAccesses
            .Where(ba => ba.Branch.IsActive)
            .Select(ba => new AuthorizedBranchDto(
                ba.BranchId,
                ba.Branch.PublicId,
                ba.Branch.Code,
                ba.Branch.Name,
                ba.IsDefault))
            .ToList();
    }

    public async Task GrantUserBranchAccessAsync(
        Guid publicId,
        GrantBranchAccessRequest request,
        string grantedByUsername,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.UserAccounts
            .Include(u => u.BranchAccesses)
            .FirstOrDefaultAsync(u => u.PublicId == publicId, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException("UserAccount", publicId);
        }

        var branch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == request.BranchId, cancellationToken);
        if (branch == null || !branch.IsActive)
        {
            throw new NotFoundException("Branch", request.BranchId);
        }

        if (user.BranchAccesses.Any(ba => ba.BranchId == request.BranchId))
        {
            throw new ConflictException("UserBranchAccess", "BranchId", request.BranchId.ToString());
        }

        if (request.IsDefault)
        {
            foreach (var existing in user.BranchAccesses)
            {
                existing.IsDefault = false;
            }
        }

        user.BranchAccesses.Add(new UserBranchAccess
        {
            UserId = user.Id,
            BranchId = branch.Id,
            IsDefault = request.IsDefault || user.BranchAccesses.Count == 0,
            GrantedBy = grantedByUsername,
            GrantedAt = DateTime.UtcNow
        });

        user.SecurityStamp = Guid.NewGuid().ToString("N");
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = grantedByUsername;

        await RecordAuditLogAsync("BranchAccessGrant", "UserBranchAccess", user.Id.ToString(), null, new { request.BranchId, request.IsDefault }, grantedByUsername, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeUserBranchAccessAsync(
        Guid publicId,
        long branchId,
        string revokedByUsername,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.UserAccounts
            .Include(u => u.BranchAccesses)
            .FirstOrDefaultAsync(u => u.PublicId == publicId, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException("UserAccount", publicId);
        }

        var access = user.BranchAccesses.FirstOrDefault(ba => ba.BranchId == branchId);
        if (access == null)
        {
            throw new NotFoundException("UserBranchAccess", branchId);
        }

        var wasDefault = access.IsDefault;
        _context.UserBranchAccesses.Remove(access);

        if (wasDefault)
        {
            var nextDefault = user.BranchAccesses.FirstOrDefault(ba => ba.BranchId != branchId);
            if (nextDefault != null)
            {
                nextDefault.IsDefault = true;
            }
        }

        user.SecurityStamp = Guid.NewGuid().ToString("N");
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = revokedByUsername;

        await RecordAuditLogAsync("BranchAccessRevoke", "UserBranchAccess", user.Id.ToString(), new { BranchId = branchId }, null, revokedByUsername, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AdminResetPasswordAsync(
        Guid publicId,
        AdminResetPasswordRequest request,
        string updatedByUsername,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.UserAccounts.FirstOrDefaultAsync(u => u.PublicId == publicId, cancellationToken);
        if (user == null)
        {
            throw new NotFoundException("UserAccount", publicId);
        }

        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.SecurityStamp = Guid.NewGuid().ToString("N");
        user.FailedLoginAttempts = 0;
        user.IsLockedOut = false;
        user.LockoutEndAt = null;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = updatedByUsername;

        // Invalidate all existing tokens and sessions
        var activeTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == user.Id && !rt.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
        }

        var activeSessions = await _context.UserSessions
            .Where(s => s.UserId == user.Id && !s.IsTerminated)
            .ToListAsync(cancellationToken);

        foreach (var session in activeSessions)
        {
            session.IsTerminated = true;
        }

        await RecordAuditLogAsync("PasswordReset", "UserAccounts", user.Id.ToString(), null, "Password reset by administrator", updatedByUsername, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _context.Roles
            .AsNoTracking()
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .Where(r => r.IsActive)
            .ToListAsync(cancellationToken);

        return roles.Select(r => new RoleDto(
            r.Id,
            r.PublicId,
            r.Code,
            r.Name,
            r.Description,
            r.IsSystem,
            r.IsActive,
            r.RolePermissions.Select(rp => rp.Permission.Code).ToList()
        )).ToList();
    }

    public async Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync(CancellationToken cancellationToken = default)
    {
        var permissions = await _context.Permissions
            .AsNoTracking()
            .OrderBy(p => p.Module)
            .ThenBy(p => p.Code)
            .ToListAsync(cancellationToken);

        return permissions.Select(p => new PermissionDto(
            p.Id,
            p.PublicId,
            p.Module,
            p.Code,
            p.Name,
            p.Description
        )).ToList();
    }

    private static UserAccountDto MapToDto(UserAccount u) => new(
        u.Id,
        u.PublicId,
        u.Username,
        u.Email,
        u.EmployeeId,
        u.Employee?.FullName,
        u.IsActive,
        u.IsLockedOut,
        u.LockoutEndAt,
        u.FailedLoginAttempts,
        u.LastLoginAt,
        u.CreatedAt,
        u.UserRoles.Where(ur => ur.Role.IsActive).Select(ur => ur.Role.Code).ToList(),
        u.BranchAccesses.Where(ba => ba.Branch.IsActive).Select(ba => new AuthorizedBranchDto(
            ba.BranchId,
            ba.Branch.PublicId,
            ba.Branch.Code,
            ba.Branch.Name,
            ba.IsDefault)).ToList()
    );

    private async Task RecordAuditLogAsync(
        string action,
        string tableName,
        string primaryKey,
        object? oldValues,
        object? newValues,
        string actorUsername,
        CancellationToken cancellationToken)
    {
        var audit = new AuditLog
        {
            Action = action,
            TableName = tableName,
            SchemaName = "identity",
            PrimaryKey = primaryKey,
            OldValuesJson = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
            NewValuesJson = newValues != null ? JsonSerializer.Serialize(newValues) : null,
            Username = actorUsername,
            Timestamp = DateTime.UtcNow,
            CreatedBy = actorUsername,
            CreatedAt = DateTime.UtcNow
        };

        await _context.AuditLogs.AddAsync(audit, cancellationToken);
    }
}
