# WinForms Desktop Client Authentication & Token Integration Guide

## 1. Overview & Architecture

The WinForms Desktop Client communicates with the ASP.NET Core Web API via HTTP. The client does **NOT** access SQL Server directly. All operational and identity transactions are mediated by the Web API using JWT Bearer authentication, refresh token rotation, and branch-scoped headers.

```text
┌────────────────────────────────────────────────────────┐
│             WinForms Desktop Client Application         │
│                                                        │
│  ┌──────────────────┐         ┌─────────────────────┐  │
│  │   UI Forms       │ ◄─────► │  AuthenticationMgr  │  │
│  └──────────────────┘         └──────────┬──────────┘  │
│                                          │             │
│                       ┌──────────────────▼──────────┐  │
│                       │ Secure Token Store (DPAPI)  │  │
│                       └─────────────────────────────┘  │
└──────────────────────────────────────┬─────────────────┘
                                       │ HTTPS
                                       │ Authorization: Bearer <JWT>
                                       │ X-Branch-Id: <BranchId>
                                       v
┌────────────────────────────────────────────────────────┐
│                 ASP.NET Core Web API                   │
└────────────────────────────────────────────────────────┘
```

---

## 2. Secure Local Token Storage

### Rules for the Desktop Client:
1. **Never store plaintext passwords**: Passwords entered on the login form must be used exclusively to construct the `LoginRequest` payload and immediately cleared from memory (`SecureString` or overwritten char arrays).
2. **Never store refresh tokens in plain text**: Do not save tokens in `.config`, registry, or standard JSON files.
3. **Use Windows Data Protection API (DPAPI)** or **Windows Credential Manager**:
   - Access tokens should reside strictly in memory (`private string _accessToken`).
   - Refresh tokens should be encrypted with DPAPI (`ProtectedData.Protect` using `DataProtectionScope.CurrentUser`) before persisting to local user app data.

---

## 3. Login Workflow Implementation

1. The client invokes `POST /api/v1/auth/login`:
   ```csharp
   var request = new LoginRequest(username, password, Environment.MachineName);
   var response = await _httpClient.PostAsJsonAsync("api/v1/auth/login", request);
   ```
2. On `200 OK`:
   - Store `accessToken` and `accessTokenExpiresAt` in memory.
   - Store encrypted `refreshToken` and `refreshTokenExpiresAt` in local DPAPI store.
   - Cache `UserProfileDto` (user ID, roles, permissions, authorized branches).
   - Set the active branch ID (defaulting to `defaultBranchId`).
   - Open main application workspace dashboard.
3. On `401 Unauthorized` / `403 Forbidden`:
   - Read `ApiResponse.Message` and display error feedback to user (e.g. invalid credentials or account lockout).

---

## 4. Automatic Token Refresh & Concurrency Guard

Access tokens expire after 15 minutes. To ensure a seamless user experience:
1. **Proactive Refresh Timer**:
   - Set an internal background timer to trigger 2 minutes before `accessTokenExpiresAt`.
   - Call `POST /api/v1/auth/refresh` with the current refresh token.
   - Save the new access token and new rotated refresh token.
2. **Concurrent Refresh Race Protection (`SemaphoreSlim`)**:
   - Multiple background API calls may discover an expired token simultaneously.
   - Use a `SemaphoreSlim(1, 1)` lock to guarantee only **ONE** refresh request is sent to the API.
   - Any parallel thread waiting on the semaphore re-reads the updated token once the lock releases.

```csharp
private readonly SemaphoreSlim _refreshLock = new(1, 1);

public async Task<string> GetValidAccessTokenAsync()
{
    if (DateTime.UtcNow < _accessTokenExpiresAt.AddMinutes(-2))
    {
        return _accessToken;
    }

    await _refreshLock.WaitAsync();
    try
    {
        // Double-check after acquiring lock
        if (DateTime.UtcNow < _accessTokenExpiresAt.AddMinutes(-2))
        {
            return _accessToken;
        }

        var refreshRequest = new RefreshTokenRequest(_refreshToken, Environment.MachineName);
        var response = await _httpClient.PostAsJsonAsync("api/v1/auth/refresh", refreshRequest);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
            _accessToken = result.Data.AccessToken;
            _accessTokenExpiresAt = result.Data.AccessTokenExpiresAt;
            _refreshToken = result.Data.RefreshToken;
            _refreshTokenExpiresAt = result.Data.RefreshTokenExpiresAt;
            SaveEncryptedRefreshToken(_refreshToken);
            return _accessToken;
        }
        else
        {
            // Token expired, revoked, or reuse detected -> Force re-login
            await HandleSessionInvalidatedAsync();
            throw new UnauthorizedAccessException("Session has expired. Please log in again.");
        }
    }
    finally
    {
        _refreshLock.Release();
    }
}
```

---

## 5. HTTP Client DelegatingHandler Integration

Register a custom `DelegatingHandler` in the WinForms HTTP client pipeline:

```csharp
public class AuthenticationDelegatingHandler : DelegatingHandler
{
    private readonly ITokenManager _tokenManager;
    private readonly IBranchSelectionManager _branchManager;

    public AuthenticationDelegatingHandler(ITokenManager tokenManager, IBranchSelectionManager branchManager)
    {
        _tokenManager = tokenManager;
        _branchManager = branchManager;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _tokenManager.GetValidAccessTokenAsync();
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        if (_branchManager.ActiveBranchId.HasValue)
        {
            request.Headers.Add("X-Branch-Id", _branchManager.ActiveBranchId.Value.ToString());
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            // Token rejected (e.g. administrator reset security stamp or session terminated)
            await _tokenManager.HandleSessionInvalidatedAsync();
        }

        return response;
    }
}
```

---

## 6. Handling HTTP 401 & 403 in the UI

1. **HTTP 401 Unauthorized**:
   - The user's credentials, access token, or refresh token are no longer valid.
   - Clear in-memory tokens.
   - Delete stored encrypted refresh token.
   - Present a non-intrusive dialog informing the user that their session has expired, and redirect to the Login form.
2. **HTTP 403 Forbidden**:
   - The user is authenticated but lacks the required permission or does not have access to the selected operating branch.
   - Display an access-denied notification banner: `"You do not possess the required permission to perform this action. Contact your system administrator."`
   - Do **not** redirect to login or clear authentication tokens.

---

## 7. Graceful Logout

When the user clicks "Log Out":
1. Fire a background request to `POST /api/v1/auth/logout` sending the current refresh token.
2. Delete the local DPAPI encrypted token file.
3. Clear in-memory token and profile variables.
4. Close all open workspace windows and display the Login dialog.
