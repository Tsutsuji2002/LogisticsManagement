using Logistics.Server.Application.Common.Exceptions;
using Logistics.Server.Application.Common.Interfaces;
using Logistics.Server.Application.Modules.Identity.DTOs;
using Logistics.Server.Application.Modules.Identity.Services;
using Logistics.Server.Domain.Entities.Identity;
using Logistics.Server.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Logistics.Server.Infrastructure.Services;

/// <summary>
/// Core implementation of the Authentication and Authorization system, enforcing credential validation,
/// account and employee status checks, failed attempt tracking, lockout, transactional token rotation,
/// reuse detection, session control, and password changes.
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IPermissionService _permissionService;

    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);

    public AuthenticationService(
        ApplicationDbContext context,
        IPasswordHasherService passwordHasher,
        ITokenService tokenService,
        IPermissionService permissionService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _permissionService = permissionService;
    }

    public async Task<LoginResponse> LoginAsync(
        LoginRequest request,
        string ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var normalizedInput = request.UsernameOrEmail.Trim();

        var user = await _context.UserAccounts
            .Include(u => u.Employee)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Include(u => u.BranchAccesses)
                .ThenInclude(ba => ba.Branch)
            .FirstOrDefaultAsync(u => u.Username == normalizedInput || u.Email == normalizedInput, cancellationToken);

        // 1. User not found -> record failure & return generic error
        if (user == null)
        {
            await RecordLoginHistoryAsync(null, normalizedInput, ipAddress, userAgent, false, "User not found", cancellationToken);
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        // 2. Check lockout status
        if (user.IsLockedOut && user.LockoutEndAt.HasValue)
        {
            if (user.LockoutEndAt.Value > DateTime.UtcNow)
            {
                await RecordLoginHistoryAsync(user.Id, user.Username, ipAddress, userAgent, false, "Account locked out", cancellationToken);
                var remainingMinutes = Math.Ceiling((user.LockoutEndAt.Value - DateTime.UtcNow).TotalMinutes);
                throw new ForbiddenAccessException($"Account is temporarily locked out due to multiple failed login attempts. Please try again in {remainingMinutes} minutes.");
            }

            // Lockout period expired -> automatically unlock
            user.IsLockedOut = false;
            user.LockoutEndAt = null;
            user.FailedLoginAttempts = 0;
        }

        // 3. Verify password
        var (isValid, needsRehash) = _passwordHasher.VerifyHashedPassword(user.PasswordHash, request.Password);
        if (!isValid)
        {
            user.FailedLoginAttempts++;

            if (user.FailedLoginAttempts >= MaxFailedAttempts)
            {
                user.IsLockedOut = true;
                user.LockoutEndAt = DateTime.UtcNow.Add(LockoutDuration);
            }

            await RecordLoginHistoryAsync(user.Id, user.Username, ipAddress, userAgent, false, "Invalid password", cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        // 4. Check active account status
        if (!user.IsActive)
        {
            await RecordLoginHistoryAsync(user.Id, user.Username, ipAddress, userAgent, false, "Account inactive", cancellationToken);
            throw new UnauthorizedAccessException("Account has been deactivated. Please contact your system administrator.");
        }

        // 5. Check associated employee status
        if (user.Employee != null && !string.Equals(user.Employee.Status, "Active", StringComparison.OrdinalIgnoreCase))
        {
            await RecordLoginHistoryAsync(user.Id, user.Username, ipAddress, userAgent, false, "Employee profile inactive", cancellationToken);
            throw new UnauthorizedAccessException("Associated employee profile is not active.");
        }

        // 6. Reset failed attempts & update login timestamps
        user.FailedLoginAttempts = 0;
        user.IsLockedOut = false;
        user.LockoutEndAt = null;
        user.LastLoginAt = DateTime.UtcNow;

        if (needsRehash)
        {
            user.PasswordHash = _passwordHasher.HashPassword(request.Password);
        }

        // 7. Resolve roles, permissions, branches
        var roles = user.UserRoles
            .Where(ur => ur.Role.IsActive)
            .Select(ur => ur.Role.Code)
            .ToList();

        var permissions = await _permissionService.GetUserPermissionsAsync(user.Id, cancellationToken);

        var branches = user.BranchAccesses
            .Where(ba => ba.Branch.IsActive)
            .Select(ba => new AuthorizedBranchDto(
                ba.BranchId,
                ba.Branch.PublicId,
                ba.Branch.Code,
                ba.Branch.Name,
                ba.IsDefault))
            .ToList();

        var defaultBranch = branches.FirstOrDefault(b => b.IsDefault) ?? branches.FirstOrDefault();

        // 8. Generate JWT Access Token
        var accessToken = _tokenService.GenerateAccessToken(
            userId: user.Id,
            publicId: user.PublicId,
            username: user.Username,
            email: user.Email,
            securityStamp: user.SecurityStamp,
            employeeId: user.EmployeeId,
            defaultBranchId: defaultBranch?.BranchId,
            accessibleBranchIds: branches.Select(b => b.BranchId).ToList(),
            roles: roles,
            out var accessTokenExpiresAt);

        // 9. Generate and store cryptographically secure Refresh Token
        var rawRefreshToken = _tokenService.GenerateRefreshToken();
        var refreshTokenHash = _tokenService.HashToken(rawRefreshToken);
        var refreshTokenExpiresAt = DateTime.UtcNow.Add(RefreshTokenLifetime);

        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            ExpiresAt = refreshTokenExpiresAt,
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress
        };
        await _context.RefreshTokens.AddAsync(refreshTokenEntity, cancellationToken);

        // 10. Create UserSession
        var sessionEntity = new UserSession
        {
            UserId = user.Id,
            SessionTokenHash = _tokenService.HashToken(Guid.NewGuid().ToString("N")),
            DeviceInfo = request.DeviceInfo ?? userAgent,
            IpAddress = ipAddress,
            StartedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
            ExpiresAt = refreshTokenExpiresAt,
            IsTerminated = false
        };
        await _context.UserSessions.AddAsync(sessionEntity, cancellationToken);

        // 11. Record successful login history
        await RecordLoginHistoryAsync(user.Id, user.Username, ipAddress, userAgent, true, null, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        // 12. Build user profile response
        var userProfile = new UserProfileDto(
            Id: user.Id,
            PublicId: user.PublicId,
            Username: user.Username,
            Email: user.Email,
            DisplayName: user.Employee?.FullName ?? user.Username,
            EmployeeId: user.EmployeeId,
            EmployeeCode: user.Employee?.Code,
            JobTitle: user.Employee?.JobTitle,
            DefaultBranchId: defaultBranch?.BranchId,
            DefaultBranchName: defaultBranch?.Name,
            AuthorizedBranches: branches,
            Roles: roles,
            Permissions: permissions.ToList(),
            IsActive: user.IsActive
        );

        return new LoginResponse(
            AccessToken: accessToken,
            AccessTokenExpiresAt: accessTokenExpiresAt,
            RefreshToken: rawRefreshToken,
            RefreshTokenExpiresAt: refreshTokenExpiresAt,
            User: userProfile
        );
    }

    public async Task<LoginResponse> RefreshTokenAsync(
        RefreshTokenRequest request,
        string ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = _tokenService.HashToken(request.RefreshToken);

        var existingToken = await _context.RefreshTokens
            .Include(rt => rt.User)
                .ThenInclude(u => u.Employee)
            .Include(rt => rt.User)
                .ThenInclude(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
            .Include(rt => rt.User)
                .ThenInclude(u => u.BranchAccesses)
                    .ThenInclude(ba => ba.Branch)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

        if (existingToken == null)
        {
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        // ── REUSE DETECTION ──
        if (existingToken.IsRevoked)
        {
            // Revoked token reused -> potential token theft! Invalidate all tokens & sessions for user immediately.
            var allActiveTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == existingToken.UserId && !rt.IsRevoked)
                .ToListAsync(cancellationToken);

            foreach (var token in allActiveTokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
            }

            var allActiveSessions = await _context.UserSessions
                .Where(s => s.UserId == existingToken.UserId && !s.IsTerminated)
                .ToListAsync(cancellationToken);

            foreach (var session in allActiveSessions)
            {
                session.IsTerminated = true;
            }

            // Invalidate security stamp
            existingToken.User.SecurityStamp = Guid.NewGuid().ToString("N");

            await _context.SaveChangesAsync(cancellationToken);

            throw new UnauthorizedAccessException("Refresh token reuse detected. All active sessions have been terminated for security.");
        }

        // Check expiration
        if (existingToken.ExpiresAt <= DateTime.UtcNow)
        {
            existingToken.IsRevoked = true;
            existingToken.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            throw new UnauthorizedAccessException("Refresh token has expired. Please log in again.");
        }

        var user = existingToken.User;

        // Check user active status
        if (!user.IsActive || user.IsLockedOut)
        {
            throw new UnauthorizedAccessException("User account is inactive or locked.");
        }

        if (user.Employee != null && !string.Equals(user.Employee.Status, "Active", StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Associated employee profile is inactive.");
        }

        // ── TOKEN ROTATION ──
        var newRawRefreshToken = _tokenService.GenerateRefreshToken();
        var newRefreshTokenHash = _tokenService.HashToken(newRawRefreshToken);
        var newRefreshTokenExpiresAt = DateTime.UtcNow.Add(RefreshTokenLifetime);

        // Mark previous token rotated and link to replacement
        existingToken.IsRevoked = true;
        existingToken.RevokedAt = DateTime.UtcNow;
        existingToken.ReplacedByToken = newRefreshTokenHash;

        var newRefreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = newRefreshTokenHash,
            ExpiresAt = newRefreshTokenExpiresAt,
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress
        };
        await _context.RefreshTokens.AddAsync(newRefreshTokenEntity, cancellationToken);

        // Roles, permissions, branches
        var roles = user.UserRoles
            .Where(ur => ur.Role.IsActive)
            .Select(ur => ur.Role.Code)
            .ToList();

        var permissions = await _permissionService.GetUserPermissionsAsync(user.Id, cancellationToken);

        var branches = user.BranchAccesses
            .Where(ba => ba.Branch.IsActive)
            .Select(ba => new AuthorizedBranchDto(
                ba.BranchId,
                ba.Branch.PublicId,
                ba.Branch.Code,
                ba.Branch.Name,
                ba.IsDefault))
            .ToList();

        var defaultBranch = branches.FirstOrDefault(b => b.IsDefault) ?? branches.FirstOrDefault();

        // New JWT Access Token
        var newAccessToken = _tokenService.GenerateAccessToken(
            userId: user.Id,
            publicId: user.PublicId,
            username: user.Username,
            email: user.Email,
            securityStamp: user.SecurityStamp,
            employeeId: user.EmployeeId,
            defaultBranchId: defaultBranch?.BranchId,
            accessibleBranchIds: branches.Select(b => b.BranchId).ToList(),
            roles: roles,
            out var newAccessTokenExpiresAt);

        // Update session activity
        var activeSession = await _context.UserSessions
            .Where(s => s.UserId == user.Id && !s.IsTerminated)
            .OrderByDescending(s => s.LastActivityAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeSession != null)
        {
            activeSession.LastActivityAt = DateTime.UtcNow;
            activeSession.ExpiresAt = newRefreshTokenExpiresAt;
        }

        await _context.SaveChangesAsync(cancellationToken);

        var userProfile = new UserProfileDto(
            Id: user.Id,
            PublicId: user.PublicId,
            Username: user.Username,
            Email: user.Email,
            DisplayName: user.Employee?.FullName ?? user.Username,
            EmployeeId: user.EmployeeId,
            EmployeeCode: user.Employee?.Code,
            JobTitle: user.Employee?.JobTitle,
            DefaultBranchId: defaultBranch?.BranchId,
            DefaultBranchName: defaultBranch?.Name,
            AuthorizedBranches: branches,
            Roles: roles,
            Permissions: permissions.ToList(),
            IsActive: user.IsActive
        );

        return new LoginResponse(
            AccessToken: newAccessToken,
            AccessTokenExpiresAt: newAccessTokenExpiresAt,
            RefreshToken: newRawRefreshToken,
            RefreshTokenExpiresAt: newRefreshTokenExpiresAt,
            User: userProfile
        );
    }

    public async Task LogoutAsync(long userId, string? refreshToken, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            var tokenHash = _tokenService.HashToken(refreshToken);
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash && rt.UserId == userId, cancellationToken);

            if (token != null && !token.IsRevoked)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
            }
        }

        // Terminate the most recent active session for this user
        var latestSession = await _context.UserSessions
            .Where(s => s.UserId == userId && !s.IsTerminated)
            .OrderByDescending(s => s.LastActivityAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestSession != null)
        {
            latestSession.IsTerminated = true;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task LogoutAllAsync(long userId, CancellationToken cancellationToken = default)
    {
        var activeTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
        }

        var activeSessions = await _context.UserSessions
            .Where(s => s.UserId == userId && !s.IsTerminated)
            .ToListAsync(cancellationToken);

        foreach (var session in activeSessions)
        {
            session.IsTerminated = true;
        }

        var user = await _context.UserAccounts.FindAsync([userId], cancellationToken);
        if (user != null)
        {
            user.SecurityStamp = Guid.NewGuid().ToString("N");
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserProfileDto> GetCurrentUserProfileAsync(long userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.UserAccounts
            .AsNoTracking()
            .Include(u => u.Employee)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Include(u => u.BranchAccesses)
                .ThenInclude(ba => ba.Branch)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException("UserAccount", userId);
        }

        var roles = user.UserRoles
            .Where(ur => ur.Role.IsActive)
            .Select(ur => ur.Role.Code)
            .ToList();

        var permissions = await _permissionService.GetUserPermissionsAsync(user.Id, cancellationToken);

        var branches = user.BranchAccesses
            .Where(ba => ba.Branch.IsActive)
            .Select(ba => new AuthorizedBranchDto(
                ba.BranchId,
                ba.Branch.PublicId,
                ba.Branch.Code,
                ba.Branch.Name,
                ba.IsDefault))
            .ToList();

        var defaultBranch = branches.FirstOrDefault(b => b.IsDefault) ?? branches.FirstOrDefault();

        return new UserProfileDto(
            Id: user.Id,
            PublicId: user.PublicId,
            Username: user.Username,
            Email: user.Email,
            DisplayName: user.Employee?.FullName ?? user.Username,
            EmployeeId: user.EmployeeId,
            EmployeeCode: user.Employee?.Code,
            JobTitle: user.Employee?.JobTitle,
            DefaultBranchId: defaultBranch?.BranchId,
            DefaultBranchName: defaultBranch?.Name,
            AuthorizedBranches: branches,
            Roles: roles,
            Permissions: permissions.ToList(),
            IsActive: user.IsActive
        );
    }

    public async Task ChangePasswordAsync(
        long userId,
        ChangePasswordRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var user = await _context.UserAccounts.FindAsync([userId], cancellationToken);
        if (user == null)
        {
            throw new NotFoundException("UserAccount", userId);
        }

        var (isValid, _) = _passwordHasher.VerifyHashedPassword(user.PasswordHash, request.CurrentPassword);
        if (!isValid)
        {
            throw new ValidationException("CurrentPassword", "The current password provided is incorrect.");
        }

        // Update password and invalidate security stamp
        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.SecurityStamp = Guid.NewGuid().ToString("N");
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = user.Username;

        // Revoke all existing refresh tokens and sessions
        var activeTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
        }

        var activeSessions = await _context.UserSessions
            .Where(s => s.UserId == userId && !s.IsTerminated)
            .ToListAsync(cancellationToken);

        foreach (var session in activeSessions)
        {
            session.IsTerminated = true;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task RecordLoginHistoryAsync(
        long? userId,
        string usernameAttempted,
        string ipAddress,
        string? userAgent,
        bool isSuccessful,
        string? failureReason,
        CancellationToken cancellationToken)
    {
        var history = new LoginHistory
        {
            UserId = userId,
            UsernameAttempted = usernameAttempted,
            IpAddress = ipAddress,
            UserAgent = userAgent?.Length > 500 ? userAgent[..500] : userAgent,
            IsSuccessful = isSuccessful,
            FailureReason = failureReason,
            AttemptedAt = DateTime.UtcNow
        };

        await _context.LoginHistories.AddAsync(history, cancellationToken);
    }
}
