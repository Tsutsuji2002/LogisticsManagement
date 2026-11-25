# Desktop Build, Run & Deployment Guide

## 1. Prerequisites

- **.NET 9 SDK** (desktop projects target `net9.0-windows` with Windows Forms enabled)
- Running instance of `Logistics.Server.API` (default development URL: `http://localhost:5289`)

## 2. Build & Test Commands

```powershell
# Build entire solution (Server + Client + Tests)
dotnet build LogisticsManagement.sln

# Build WinForms desktop client only
dotnet build src/Client/Logistics.Client.WinForms/Logistics.Client.WinForms.csproj

# Run client unit tests
dotnet test tests/Logistics.Client.UnitTests/Logistics.Client.UnitTests.csproj

# Run full solution test suite
dotnet test LogisticsManagement.sln
```

## 3. Running Locally (Development)

1. Start the Web API server:
   ```powershell
   dotnet run --project src/Server/Logistics.Server.API/Logistics.Server.API.csproj
   ```
2. Launch the WinForms desktop client:
   ```powershell
   dotnet run --project src/Client/Logistics.Client.WinForms/Logistics.Client.WinForms.csproj
   ```

## 4. Production Deployment Notes

1. Publish `Logistics.Client.WinForms`:
   ```powershell
   dotnet publish src/Client/Logistics.Client.WinForms/Logistics.Client.WinForms.csproj -c Release -r win-x64 --self-contained false
   ```
2. Set `DOTNET_ENVIRONMENT=Production` or update `appsettings.Production.json` `ApiClient:BaseUrl` to the HTTPS production API gateway URL before distribution.
3. Verify `appsettings.json` / `appsettings.Production.json` contain zero secrets, database connection strings, or localhost URLs.
4. End users can override URL / timeout / language via the Login Settings modal, stored per Windows user in `%LocalAppData%\LogisticsManagement\desktop-settings.json`.
