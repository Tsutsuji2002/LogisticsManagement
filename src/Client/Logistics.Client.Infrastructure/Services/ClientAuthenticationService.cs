using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Logistics.Client.Application.Contracts.Auth;
using Logistics.Client.Application.Interfaces;
using Logistics.Client.Infrastructure.Http;
using Logistics.Shared.Contracts;
using Microsoft.Extensions.Logging;

namespace Logistics.Client.Infrastructure.Services;

/// <summary>
/// Manages JWT Bearer in-memory access tokens, DPAPI-encrypted refresh tokens,
/// concurrent-safe single-flight refresh rotation (<see cref="SemaphoreSlim"/>), and background proactive renewal.
/// </summary>
public sealed class ClientAuthenticationService : IAuthenticationService, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ApiClientOptions _apiOptions;
    private readonly ISecureTokenStore _tokenStore;
    private readonly IBranchContextService _branchContext;
    private readonly ILogger<ClientAuthenticationService> _logger;

    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private System.Threading.Timer? _proactiveRefreshTimer;

    private string? _accessToken;
    private DateTime _accessTokenExpiresAtUtc = DateTime.MinValue;
    private string? _refreshToken;
    private DateTime _refreshTokenExpiresAtUtc = DateTime.MinValue;
    private UserProfileDto? _currentUser;

    public ClientAuthenticationService(
        IHttpClientFactory httpClientFactory,
        ApiClientOptions apiOptions,
        ISecureTokenStore tokenStore,
        IBranchContextService branchContext,
        ILogger<ClientAuthenticationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _apiOptions = apiOptions;
        _tokenStore = tokenStore;
        _branchContext = branchContext;
        _logger = logger;
    }

    public bool IsAuthenticated => !string.IsNullOrEmpty(_accessToken) && _currentUser != null;
    public string? CurrentUserName => _currentUser?.Username;
    public UserProfileDto? CurrentUser => _currentUser;
    public string? AccessToken => _accessToken;
    public DateTime AccessTokenExpiresAtUtc => _accessTokenExpiresAtUtc;

    public event EventHandler? AuthenticationStateChanged;
    public event EventHandler<string?>? SessionExpired;

    public async Task<bool> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var (success, _) = await LoginDetailedAsync(username, password, rememberUsername: true, cancellationToken);
        return success;
    }

    public async Task<(bool Success, string? ErrorMessage)> LoginDetailedAsync(
        string usernameOrEmail,
        string password,
        bool rememberUsername,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(usernameOrEmail) || string.IsNullOrWhiteSpace(password))
            return (false, "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.");

        try
        {
            using var client = CreateBareAuthHttpClient();
            var request = new LoginRequest(usernameOrEmail.Trim(), password, Environment.MachineName);
            using var response = await client.PostAsJsonAsync("api/v1/auth/login", request, JsonOptions, cancellationToken);

            var payloadText = await response.Content.ReadAsStringAsync(cancellationToken);
            var apiResponse = DeserializeSafe<ApiResponse<LoginResponse>>(payloadText);

            if (!response.IsSuccessStatusCode || apiResponse is not { Success: true, Data: not null })
            {
                var errMsg = apiResponse?.Message ?? "Tên đăng nhập hoặc mật khẩu không hợp lệ.";
                _logger.LogWarning("Authentication attempt failed with HTTP {StatusCode}.", (int)response.StatusCode);
                return (false, errMsg);
            }

            ApplyAuthenticatedSession(apiResponse.Data);
            _tokenStore.SaveRememberedUsername(rememberUsername ? apiResponse.Data.User.Username : null);

            _logger.LogInformation("User {Username} authenticated successfully.", apiResponse.Data.User.Username);
            AuthenticationStateChanged?.Invoke(this, EventArgs.Empty);
            return (true, null);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return (false, "Hết thời gian chờ phản hồi từ máy chủ API.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error connecting to API during login.");
            return (false, "Không thể kết nối tới máy chủ API.");
        }
    }

    public async Task<bool> TryRestoreSessionAsync(CancellationToken cancellationToken = default)
    {
        var saved = _tokenStore.LoadRefreshSession();
        if (saved == null || string.IsNullOrWhiteSpace(saved.RefreshToken) || saved.RefreshTokenExpiresAt <= DateTime.UtcNow)
        {
            _tokenStore.ClearRefreshSession();
            return false;
        }

        _refreshToken = saved.RefreshToken;
        _refreshTokenExpiresAtUtc = saved.RefreshTokenExpiresAt;

        var token = await GetValidAccessTokenAsync(forceRefresh: true, cancellationToken);
        return !string.IsNullOrWhiteSpace(token) && IsAuthenticated;
    }

    public async Task<string?> GetValidAccessTokenAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        if (!forceRefresh &&
            !string.IsNullOrEmpty(_accessToken) &&
            DateTime.UtcNow < _accessTokenExpiresAtUtc.AddMinutes(-2))
        {
            return _accessToken;
        }

        // Capture the current token before entering the lock so that concurrent callers that enter
        // the lock after the first caller has already refreshed can detect the token changed and skip.
        var tokenBeforeLock = _accessToken;

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            // For non-forced: skip if the token is now valid (another caller refreshed while we waited).
            // For forced: skip only if another caller already refreshed (token value changed since we checked).
            if (forceRefresh)
            {
                if (!string.IsNullOrEmpty(_accessToken) && _accessToken != tokenBeforeLock)
                {
                    // Another concurrent caller already performed the refresh; reuse the new token.
                    return _accessToken;
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(_accessToken) &&
                    DateTime.UtcNow < _accessTokenExpiresAtUtc.AddMinutes(-2))
                {
                    return _accessToken;
                }
            }

            if (string.IsNullOrWhiteSpace(_refreshToken))
                return null;

            using var client = CreateBareAuthHttpClient();
            var request = new RefreshTokenRequest(_refreshToken, Environment.MachineName);
            using var response = await client.PostAsJsonAsync("api/v1/auth/refresh", request, JsonOptions, cancellationToken);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Refresh token rejected with HTTP {StatusCode}. Clearing session.", (int)response.StatusCode);
                ClearLocalStateOnly();
                return null;
            }

            var apiResponse = DeserializeSafe<ApiResponse<LoginResponse>>(payload);
            if (apiResponse is not { Success: true, Data: not null })
            {
                ClearLocalStateOnly();
                return null;
            }

            ApplyAuthenticatedSession(apiResponse.Data);
            AuthenticationStateChanged?.Invoke(this, EventArgs.Empty);
            return _accessToken;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token refresh error.");
            ClearLocalStateOnly();
            return null;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public async Task<bool> RefreshUserProfileAsync(CancellationToken cancellationToken = default)
    {
        var token = await GetValidAccessTokenAsync(forceRefresh: false, cancellationToken);
        if (string.IsNullOrWhiteSpace(token)) return false;

        try
        {
            using var client = CreateBareAuthHttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await client.GetAsync("api/v1/auth/me", cancellationToken);
            if (!response.IsSuccessStatusCode) return false;

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var api = DeserializeSafe<ApiResponse<UserProfileDto>>(content);
            if (api is not { Success: true, Data: not null }) return false;

            _currentUser = api.Data;
            _branchContext.InitializeFromProfile(api.Data);
            AuthenticationStateChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh user profile.");
            return false;
        }
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        var accessToken = _accessToken;
        var refreshToken = _refreshToken;

        try
        {
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                using var client = CreateBareAuthHttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                await client.PostAsJsonAsync("api/v1/auth/logout", new LogoutRequest(refreshToken), JsonOptions, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Best-effort logout API revocation skipped due to transport error.");
        }
        finally
        {
            ClearLocalStateOnly();
            AuthenticationStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public async Task LogoutAllAsync(CancellationToken cancellationToken = default)
    {
        var accessToken = _accessToken;
        try
        {
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                using var client = CreateBareAuthHttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                await client.PostAsync("api/v1/auth/logout-all", null, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Best-effort logout-all API revocation skipped due to transport error.");
        }
        finally
        {
            ClearLocalStateOnly();
            AuthenticationStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public Task HandleSessionInvalidatedAsync(string? reason = null)
    {
        ClearLocalStateOnly();
        AuthenticationStateChanged?.Invoke(this, EventArgs.Empty);
        SessionExpired?.Invoke(this, reason);
        return Task.CompletedTask;
    }

    private void ApplyAuthenticatedSession(LoginResponse login)
    {
        _accessToken = login.AccessToken;
        _accessTokenExpiresAtUtc = login.AccessTokenExpiresAt.ToUniversalTime();
        _refreshToken = login.RefreshToken;
        _refreshTokenExpiresAtUtc = login.RefreshTokenExpiresAt.ToUniversalTime();
        _currentUser = login.User;

        _tokenStore.SaveRefreshSession(new StoredRefreshSession(
            _refreshToken,
            _refreshTokenExpiresAtUtc,
            login.User.Username));

        _branchContext.InitializeFromProfile(login.User);
        ScheduleProactiveRefresh();
    }

    private void ClearLocalStateOnly()
    {
        _proactiveRefreshTimer?.Dispose();
        _proactiveRefreshTimer = null;

        _accessToken = null;
        _accessTokenExpiresAtUtc = DateTime.MinValue;
        _refreshToken = null;
        _refreshTokenExpiresAtUtc = DateTime.MinValue;
        _currentUser = null;

        _tokenStore.ClearRefreshSession();
        _branchContext.Clear();
    }

    private void ScheduleProactiveRefresh()
    {
        _proactiveRefreshTimer?.Dispose();
        var due = _accessTokenExpiresAtUtc.AddMinutes(-2) - DateTime.UtcNow;
        if (due < TimeSpan.FromSeconds(15))
            due = TimeSpan.FromSeconds(15);

        _proactiveRefreshTimer = new System.Threading.Timer(async _ =>
        {
            try
            {
                var token = await GetValidAccessTokenAsync(forceRefresh: true);
                if (string.IsNullOrEmpty(token))
                {
                    await HandleSessionInvalidatedAsync("Không thể gia hạn phiên làm việc.");
                }
            }
            catch
            {
                // Ignored on background timer
            }
        }, null, due, Timeout.InfiniteTimeSpan);
    }

    private HttpClient CreateBareAuthHttpClient()
    {
        var client = _httpClientFactory.CreateClient("LogisticsAuth");
        if (!string.IsNullOrWhiteSpace(_apiOptions.BaseUrl) &&
            Uri.TryCreate(_apiOptions.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute, out var baseUri))
        {
            client.BaseAddress = baseUri;
        }
        client.Timeout = TimeSpan.FromSeconds(Math.Clamp(_apiOptions.TimeoutSeconds, 5, 300));
        return client;
    }

    private static T? DeserializeSafe<T>(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return default;
        try { return JsonSerializer.Deserialize<T>(raw, JsonOptions); }
        catch { return default; }
    }

    public void Dispose()
    {
        _proactiveRefreshTimer?.Dispose();
        _refreshLock.Dispose();
    }
}
