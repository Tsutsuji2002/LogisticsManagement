using Logistics.Client.Application.Contracts.Auth;

namespace Logistics.Client.Application.Interfaces;

/// <summary>
/// Client-side authentication service managing JWT access tokens, DPAPI refresh persistence, and session lifecycle.
/// </summary>
public interface IAuthenticationService
{
    bool IsAuthenticated { get; }
    string? CurrentUserName { get; }
    UserProfileDto? CurrentUser { get; }
    string? AccessToken { get; }
    DateTime AccessTokenExpiresAtUtc { get; }

    Task<bool> LoginAsync(string username, string password, CancellationToken cancellationToken = default);

    Task<(bool Success, string? ErrorMessage)> LoginDetailedAsync(
        string usernameOrEmail,
        string password,
        bool rememberUsername,
        CancellationToken cancellationToken = default);

    Task<bool> TryRestoreSessionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a valid in-memory access token, performing single-flight refresh if near expiry.
    /// </summary>
    Task<string?> GetValidAccessTokenAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);

    Task<bool> RefreshUserProfileAsync(CancellationToken cancellationToken = default);

    Task LogoutAsync(CancellationToken cancellationToken = default);

    Task LogoutAllAsync(CancellationToken cancellationToken = default);

    Task HandleSessionInvalidatedAsync(string? reason = null);

    event EventHandler? AuthenticationStateChanged;
    event EventHandler<string?>? SessionExpired;
}
