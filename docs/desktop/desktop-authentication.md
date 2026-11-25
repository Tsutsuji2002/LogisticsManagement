# Desktop Authentication & Session Lifecycle

## 1. Login & Credential Handling ([LoginForm.cs](file:///c:/Projects/LogisticsManagement/src/Client/Logistics.Client.WinForms/Forms/Authentication/LoginForm.cs))

- Supports `UsernameOrEmail` and masked `Password` with show-password toggle and **Remember username only** checkbox.
- Passwords are NEVER saved to disk, `appsettings`, DPAPI storage, or Serilog logs.
- Includes inline **Test API Connection** button (`/api/health`) and **API Server Settings** dialog so administrators can switch between Development and Production endpoints before signing in.

## 2. Token Storage ([DpapiSecureTokenStore.cs](file:///c:/Projects/LogisticsManagement/src/Client/Logistics.Client.WinForms/Security/DpapiSecureTokenStore.cs))

- **Access Token (JWT)**: Held strictly in memory inside `ClientAuthenticationService` and cleared on logout or expiry.
- **Refresh Token**: Encrypted using Windows DPAPI (`ProtectedData.Protect` with `DataProtectionScope.CurrentUser`) and persisted at `%LocalAppData%\LogisticsManagement\session.dat`.
- **Remembered Username**: Stored separately at `%LocalAppData%\LogisticsManagement\remembered-user.txt` when opted in; cleared when unchecked.

## 3. Concurrency-Safe Token Refresh ([ClientAuthenticationService.cs](file:///c:/Projects/LogisticsManagement/src/Client/Logistics.Client.Infrastructure/Services/ClientAuthenticationService.cs))

- **Single-Flight Lock**: Uses `SemaphoreSlim(1, 1)` around `/api/v1/auth/refresh` so multiple simultaneous UI/grid requests hitting expiry or `401` coalesce into a **single** refresh call using the rotating refresh token.
- **Token Rotation Check**: Inside the semaphore lock, if another thread already refreshed the access token beyond the 2-minute safety buffer, queued callers reuse the fresh token without issuing duplicate refresh requests.
- **Proactive Renewal Timer**: Renews automatically 2 minutes before `AccessTokenExpiresAtUtc`.
- **Session Invalidation**: If refresh fails or is revoked (`401`), clears memory + DPAPI store + branch context and raises `SessionExpired`, cleanly closing `MainForm` and returning to `LoginForm`.

## 4. Session Management & Password Change

In [`SettingsWorkspaceControl.cs`](file:///c:/Projects/LogisticsManagement/src/Client/Logistics.Client.WinForms/Controls/Settings/SettingsWorkspaceControl.cs):
- Change current password (`POST /api/v1/auth/change-password`).
- View active login sessions (`GET /api/v1/auth/sessions`), terminate individual sessions (`DELETE /api/v1/auth/sessions/{id}`), or trigger `Logout All Devices` (`POST /api/v1/auth/logout-all`).
- Forgot-password is not exposed as an unauthenticated flow because the backend requires either authenticated `/auth/change-password` or admin `/api/v1/admin/users/{publicId}/reset-password`.
