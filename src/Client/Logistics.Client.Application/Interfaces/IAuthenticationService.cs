namespace Logistics.Client.Application.Interfaces;

/// <summary>
/// Client-side authentication service contract.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Indicates whether a user is currently authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Gets the display name or identifier of the currently signed-in user.
    /// </summary>
    string? CurrentUserName { get; }

    /// <summary>
    /// Attempts to authenticate the user with credentials.
    /// </summary>
    Task<bool> LoginAsync(string username, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs out the current user and clears session state.
    /// </summary>
    Task LogoutAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Event triggered when authentication state changes.
    /// </summary>
    event EventHandler? AuthenticationStateChanged;
}
