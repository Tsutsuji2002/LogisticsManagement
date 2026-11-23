namespace Logistics.Server.Application.Modules.Identity.DTOs;

// ── Authentication DTOs ──

public record LoginRequest(
    string UsernameOrEmail,
    string Password,
    string? DeviceInfo = null
);

public record RefreshTokenRequest(
    string RefreshToken,
    string? DeviceInfo = null
);

public record LogoutRequest(
    string? RefreshToken = null
);

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword
);

public record AuthorizedBranchDto(
    long BranchId,
    Guid BranchPublicId,
    string Code,
    string Name,
    bool IsDefault
);

public record UserProfileDto(
    long Id,
    Guid PublicId,
    string Username,
    string Email,
    string DisplayName,
    long? EmployeeId,
    string? EmployeeCode,
    string? JobTitle,
    long? DefaultBranchId,
    string? DefaultBranchName,
    IReadOnlyList<AuthorizedBranchDto> AuthorizedBranches,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    bool IsActive
);

public record LoginResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    UserProfileDto User
);

public record UserSessionDto(
    long Id,
    string? DeviceInfo,
    string IpAddress,
    DateTime StartedAt,
    DateTime LastActivityAt,
    DateTime ExpiresAt,
    bool IsCurrentSession
);

// ── Administrative User Management DTOs ──

public record UserAccountDto(
    long Id,
    Guid PublicId,
    string Username,
    string Email,
    long? EmployeeId,
    string? EmployeeName,
    bool IsActive,
    bool IsLockedOut,
    DateTime? LockoutEndAt,
    int FailedLoginAttempts,
    DateTime? LastLoginAt,
    DateTime CreatedAt,
    IReadOnlyList<string> Roles,
    IReadOnlyList<AuthorizedBranchDto> Branches
);

public record CreateUserRequest(
    string Username,
    string Email,
    string Password,
    long? EmployeeId = null,
    IReadOnlyList<string>? Roles = null,
    IReadOnlyList<long>? BranchIds = null,
    long? DefaultBranchId = null
);

public record UpdateUserRequest(
    string Email,
    long? EmployeeId = null
);

public record UpdateUserStatusRequest(
    bool IsActive,
    bool UnlockAccount = false
);

public record UpdateUserRolesRequest(
    IReadOnlyList<string> Roles
);

public record UpdateUserBranchesRequest(
    IReadOnlyList<long> BranchIds,
    long? DefaultBranchId = null
);

public record GrantBranchAccessRequest(
    long BranchId,
    bool IsDefault = false
);

public record AdminResetPasswordRequest(
    string NewPassword
);

public record RoleDto(
    long Id,
    Guid PublicId,
    string Code,
    string Name,
    string? Description,
    bool IsSystem,
    bool IsActive,
    IReadOnlyList<string> Permissions
);

public record PermissionDto(
    long Id,
    Guid PublicId,
    string Module,
    string Code,
    string Name,
    string? Description
);
