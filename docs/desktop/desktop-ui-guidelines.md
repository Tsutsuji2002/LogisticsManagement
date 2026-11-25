# Desktop UI & UX Guidelines

## 1. Visual Design System ([AppTheme.cs](file:///c:/Projects/LogisticsManagement/src/Client/Logistics.Client.WinForms/Themes/AppTheme.cs))

- **Typography**: `Segoe UI` hierarchy (`PageTitleFont` 14pt Bold, `SectionTitleFont` 10.5pt Bold, `BodyFont` 9.5pt, `SmallFont` 8.5pt, `KpiValueFont` 18pt Bold).
- **Color Tokens**:
  - Primary Dark Sidebar: `NavyDark (#0F172A)`, `SidebarBg (#1E293B)`
  - Primary Action Blue: `PrimaryBlue (#2563EB)`
  - Semantic Status Colors: `SuccessEmerald (#10B981)`, `WarningAmber (#F59E0B)`, `DangerRed (#DC2626)`
  - Surfaces & Borders: `SurfaceBg (#F5F7FA)`, `CardBg (#FFFFFF)`, `BorderSubtle (#E2E8F0)`
- **High-DPI & Resizability**:
  - Forms use `AutoScaleMode.Dpi` with explicit `MinimumSize` (`1024x640` for `MainForm`, `460x260+` for `ModalFormDialog`).
  - Workspaces compose `SplitContainer`, `DockStyle.Fill`, `FlowLayoutPanel`, and `TableLayoutPanel` rather than brittle fixed pixel coordinates.

## 2. Reusable Shared Controls

- **[PagedDataGridControl](file:///c:/Projects/LogisticsManagement/src/Client/Logistics.Client.WinForms/Controls/Common/PagedDataGridControl.cs)**: Standardizes top search input, custom filter slots, action button bar, double-buffered styled `DataGridView`, loading banner, inline error banner, empty-state messaging, and bottom server-side pagination controls (`First/Prev/Page/Next/Last` + `PageSize`).
- **[ModalFormDialog](file:///c:/Projects/LogisticsManagement/src/Client/Logistics.Client.WinForms/Controls/Common/ModalFormDialog.cs)**: Dynamically renders validated forms from `ModalFieldSpec`, maps backend `ApiException.ValidationErrors` onto WinForms `ErrorProvider`, disables the submit button while executing to prevent duplicate POSTs, and keeps the modal open on error so user input is preserved.
- **[UiAsyncRunner](file:///c:/Projects/LogisticsManagement/src/Client/Logistics.Client.WinForms/Helpers/UiAsyncRunner.cs)**: Wraps async UI handlers with wait cursor management, button disabling, `OperationCanceledException` suppression, and friendly `ApiException` formatting.

## 3. Localization

- Strings are centralized in [`AppStrings.cs`](file:///c:/Projects/LogisticsManagement/src/Client/Logistics.Client.Application/Localization/AppStrings.cs) defaulting to **Vietnamese (`vi-VN`)**, with live runtime switching to **English (`en-US`)** via header selector or Settings workspace.
- Display timestamps convert UTC timestamps to local Windows time (`dd/MM/yyyy HH:mm`) via `AppTheme.FormatLocalTime`.
- Currency/money values use `AppTheme.FormatMoney` (`N0 ₫` for `VND`, `N2` otherwise) with **zero client-side financial/tax/COD math** — all totals come directly from backend DTOs.
