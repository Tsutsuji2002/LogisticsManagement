# WinForms Desktop Client Architecture

## 1. Architectural Boundaries

The desktop client strictly follows the repository's **Clean Architecture** separation and consumes the backend exclusively over REST/JSON APIs (`Logistics.Server.API`):

```text
Logistics.Client.WinForms (Presentation / Forms / UserControls)
    ├── depends on ──▶ Logistics.Client.Application (Interfaces, Contracts, Permissions, Navigation, Localization)
    ├── depends on ──▶ Logistics.Client.Infrastructure (HttpClient, Token Auth Handler, Typed API Clients, Config)
    └── depends on ──▶ Logistics.Shared.Contracts (ApiResponse, PagedRequest, PagedResponse, HealthCheckResponse)
```

- **Zero Direct Database Access**: `Logistics.Client.*` projects do NOT reference `EntityFrameworkCore`, `Microsoft.Data.SqlClient`, `Logistics.Server.*`, or any `DbContext`.
- **Single-Responsibility Layering**:
  - `Logistics.Client.Application`: Client DTOs mirroring backend JSON envelopes, `ClientPermissions`, `NavigationCatalog`, `AppStrings` (`vi-VN` / `en-US`), `IApiClient`, `IAuthenticationService`, `IAuthorizationService`, `IBranchContextService`, `IClientSettingsService`, `ISecureTokenStore`, and 15 typed domain API client interfaces.
  - `Logistics.Client.Infrastructure`: `ApiClient` HTTP wrapper, `AuthenticationDelegatingHandler`, `ClientAuthenticationService` (single-flight refresh via `SemaphoreSlim`), `ClientAuthorizationService`, `BranchContextService`, `ClientSettingsService`, and typed API client implementations.
  - `Logistics.Client.WinForms`: `Program` bootstrap + Serilog file logging, `DpapiSecureTokenStore`, `AppTheme`, reusable UI components (`PagedDataGridControl`, `ModalFormDialog`, `KpiCardControl`, `SimpleBarChartControl`, `UiAsyncRunner`), `LoginForm`, `MainForm` shell, and 14 module workspace controls.

## 2. Dependency Injection & Session Lifecycle

In [`Program.cs`](file:///c:/Projects/LogisticsManagement/src/Client/Logistics.Client.WinForms/Program.cs):

1. `DpapiSecureTokenStore` is registered before `AddClientInfrastructure(...)` so WinForms overrides the default `InMemorySecureTokenStore`.
2. `ClientSettingsService` resolves external configuration (`appsettings.json`, `appsettings.{ENV}.json`, and `%LocalAppData%\LogisticsManagement\desktop-settings.json`).
3. `ClientAuthenticationService.TryRestoreSessionAsync()` checks DPAPI-protected refresh session at startup.
4. If unauthenticated, `LoginForm` is shown as a modal dialog.
5. Once authenticated, `IServiceProvider.CreateScope()` creates an isolated `MainForm` scope with scoped workspace controls (`AddScoped`), ensuring module view caches reset cleanly whenever a user logs out or switches accounts.
6. When `MainForm.ReloginRequested` is set (manual logout, logout all devices, or `SessionExpired` event), `MainForm` disposes its scope cleanly and returns execution to the login loop.
