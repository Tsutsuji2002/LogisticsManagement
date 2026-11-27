# AntdUI Integration Report

## 1. Library Adoption & Compatibility

| Item | Details |
| :--- | :--- |
| **Package** | `AntdUI` |
| **Adopted Version** | `2.4.21` (latest stable release verified via NuGet) |
| **License** | `Apache-2.0` (verified open-source permissive license, zero commercial restrictions) |
| **Package Management** | Central Package Management (`Directory.Packages.props` + `src/Client/Logistics.Client.WinForms/Logistics.Client.WinForms.csproj`) |
| **Target Framework Compatibility** | `net9.0-windows` preserved without upgrading or downgrading the solution target framework (`AntdUI 2.4.21` ships native `lib/net9.0-windows7.0/AntdUI.dll`) |
| **Business Logic Isolation** | Zero backend or `Logistics.Client.Application` / `Logistics.Client.Infrastructure` changes required |

## 2. Package Configuration

`Directory.Packages.props`:
```xml
<!-- Desktop UI (Ant Design for WinForms - Apache-2.0) -->
<PackageVersion Include="AntdUI" Version="2.4.21" />
```

`src/Client/Logistics.Client.WinForms/Logistics.Client.WinForms.csproj`:
```xml
<PackageReference Include="AntdUI" />
```

## 3. Startup & Global Configuration

`Program.cs` initializes AntdUI global style configuration before launching `LoginForm` or `MainForm`:

- `AppTheme.InitializeAntdTheme()` configures:
  - `AntdUI.Config.Animation = true`
  - `AntdUI.Config.ShadowEnabled = true`
  - `AntdUI.Config.TextRenderingHighQuality = true`
  - Semantic palette tokens via `AntdUI.Style.SetPrimary(...)`, `SetSuccess(...)`, `SetWarning(...)`, `SetError(...)`, `SetInfo(...)`
  - Saved Light/Dark preference loaded from `%LOCALAPPDATA%\LogisticsManagement\theme.pref` and applied to `AntdUI.Config.IsDark`.

## 4. WinForms + AntdUI Hybrid Boundaries

Only standard WinForms primitives that AntdUI does not replace (and which preserve clean WinForms layout geometry) were retained alongside AntdUI controls:

- Layout containers: `TableLayoutPanel`, `FlowLayoutPanel`, `System.Windows.Forms.Panel`, `System.Windows.Forms.Label` (aliased clearly where needed to resolve ambiguity with `AntdUI.Label`)
- Native dialogs / OS interop: `SaveFileDialog`, `Process.Start("explorer.exe", ...)` for CSV exports and desktop logs
- Custom GDI+ chart surface (`SimpleBarChartControl`) hosted inside an `AntdUI.Panel` card for theme-aware Light/Dark KPI charts.
- Legacy unreferenced helpers `DashboardControl.cs` and `PlaceholderModuleControl.cs` remain intact for unit/reference stability and are not instantiated by `Program.cs` or `MainForm.cs`.
