using Logistics.Server.Application.Modules.Identity.DTOs;

namespace Logistics.Server.Application.Modules.Identity.Services;

/// <summary>
/// Service contract for user authentication, token refresh, logout, and password management.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticates a user with username/email and password, issuing access and refresh tokens.
    /// </summary>
    Task<LoginResponse> LoginAsync(
        LoginRequest request,
        string ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rotates a valid refresh token and issues a new access/refresh token pair. Detects and blocks token reuse.
    /// </summary>
    Task<LoginResponse> RefreshTokenAsync(
        RefreshTokenRequest request,
        string ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes the current session and refresh token.
    /// </summary>
    Task LogoutAsync(
        long userId,
        string? refreshToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all active sessions, refresh tokens, and invalidates the security stamp for the user.
    /// </summary>
    Task LogoutAllAsync(
        long userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the current authenticated user's profile with effective roles, permissions, and branches.
    /// </summary>
    Task<UserProfileDto> GetCurrentUserProfileAsync(
        long userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes the password for the authenticated user and terminates existing sessions.
    /// </summary>
    Task ChangePasswordAsync(
        long userId,
        ChangePasswordRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);
}
