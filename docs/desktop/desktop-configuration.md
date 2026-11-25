# Desktop Configuration & Logging Guide

## 1. External Environment Configuration

The WinForms project ships external JSON configuration files copied to the output directory (`PreserveNewest`):

- [`appsettings.json`](file:///c:/Projects/LogisticsManagement/src/Client/Logistics.Client.WinForms/appsettings.json)
- [`appsettings.Development.json`](file:///c:/Projects/LogisticsManagement/src/Client/Logistics.Client.WinForms/appsettings.Development.json) -> `"BaseUrl": "http://localhost:5289"` (matching `Logistics.Server.API` `launchSettings.json`)
- [`appsettings.Production.json`](file:///c:/Projects/LogisticsManagement/src/Client/Logistics.Client.WinForms/appsettings.Production.json) -> `"BaseUrl": ""` (no hardcoded production URL or localhost fallback)

## 2. Resolution & Validation Rules ([ClientSettingsService.cs](file:///c:/Projects/LogisticsManagement/src/Client/Logistics.Client.Infrastructure/Configuration/ClientSettingsService.cs))

1. Reads `DOTNET_ENVIRONMENT` / `ASPNETCORE_ENVIRONMENT` (defaulting to `Development`).
2. Loads `appsettings.json` + `appsettings.{env}.json`.
3. Applies any saved user override at `%LocalAppData%\LogisticsManagement\desktop-settings.json`.
4. **Production Safety Guard**: `ValidateSettings(...)` rejects loopback / `localhost` URLs whenever `Environment == "Production"` and enforces timeout bounds (`5`–`300` seconds). Updating Base URL from `LoginForm` or `SettingsWorkspaceControl` immediately updates live `ApiClientOptions`.

## 3. Structured File Logging

Configured in [`Program.cs`](file:///c:/Projects/LogisticsManagement/src/Client/Logistics.Client.WinForms/Program.cs) using `Serilog` + `Serilog.Sinks.File`:

- **Log Folder**: `%LocalAppData%\LogisticsManagement\logs\desktop-YYYYMMDD.log`
- **Retention**: Daily rolling files with 14-day retention (`retainedFileCountLimit: 14`).
- **Security**: Passwords, JWT access tokens, refresh tokens, and carrier secrets are never logged.
- Users can open the log directory directly from `SettingsWorkspaceControl` -> *Cấu hình API & Nhật ký*.
