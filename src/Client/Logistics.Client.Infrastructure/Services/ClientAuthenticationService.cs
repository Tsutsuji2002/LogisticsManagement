using Logistics.Client.Application.Interfaces;

namespace Logistics.Client.Infrastructure.Services;

/// <summary>
/// Client-side authentication service managing authentication tokens and session status.
/// </summary>
public class ClientAuthenticationService : IAuthenticationService
{
    private string? _currentUserName;
    private string? _token;

    public bool IsAuthenticated => !string.IsNullOrEmpty(_token);

    public string? CurrentUserName => _currentUserName;

    public event EventHandler? AuthenticationStateChanged;

    public async Task<bool> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        // Placeholder authentication workflow. Future integration will call POST /api/auth/login.
        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
        {
            _currentUserName = username;
            _token = "mock-jwt-token";
            AuthenticationStateChanged?.Invoke(this, EventArgs.Empty);
            return await Task.FromResult(true);
        }

        return await Task.FromResult(false);
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        _currentUserName = null;
        _token = null;
        AuthenticationStateChanged?.Invoke(this, EventArgs.Empty);
        await Task.CompletedTask;
    }
}
