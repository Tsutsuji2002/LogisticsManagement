# AntdUI Component Architecture & Developer Guide

## 1. Shell & Application Chrome

- **`MainForm` (`AntdUI.Window`)**:
  - Top OS chrome via `AntdUI.PageHeader` (borderless window minimize/maximize/close + drag-move).
  - Collapsible dark left sidebar using `AntdUI.Panel` + `AntdUI.Menu` (`TMenuMode.Inline`, `TAMode.Dark`, inline vector SVG icons, dynamically generated from `NavigationCatalog.GetVisibleGroups(IAuthorizationService)`).
  - Top workspace toolbar with sidebar toggle (`AntdUI.Button`), `AntdUI.Breadcrumb`, Branch scope `AntdUI.Select`, Theme toggle (`AntdUI.Button`), Language selector (`AntdUI.Select`), Notification Bell (`AntdUI.Button` + `AntdUI.Badge`), and User `AntdUI.Dropdown`.
- **`LoginForm` (`AntdUI.Window`)**:
  - Enterprise split layout pairing a `#001529` product overview hero panel with an `AntdUI.Panel` card hosting `AntdUI.Input` credentials, password visibility toggle, `AntdUI.Checkbox` username recall, `AntdUI.Alert` health/error status, and a Quick API & Theme Settings modal.

## 2. Reusable Shared Controls (`Controls/Common/`)

| Component | AntdUI Primitives Used | Responsibilities |
| :--- | :--- | :--- |
| **`AntPageHeaderControl`** | `AntdUI.Panel`, `AntdUI.Button` | Standardized page/section header with title, subtitle, and right-aligned action buttons |
| **`PagedDataGridControl`** | `AntdUI.Panel`, `AntdUI.Input`, `AntdUI.Button`, `AntdUI.Alert`, `AntdUI.Table`, `AntdUI.Pagination` | Debounced search (`320ms`), custom filter slot, loading/empty/error alert states, auto-sized `AntdUI.Table`, and size-configurable server pagination (`10/20/50/100`) |
| **`ModalFormDialog`** | `AntdUI.Window`, `AntdUI.PageHeader`, `AntdUI.Alert`, `AntdUI.Input`, `AntdUI.InputNumber`, `AntdUI.DatePicker`, `AntdUI.Select`, `AntdUI.Checkbox`, `AntdUI.Button` | Schema-driven modal (`ModalFieldSpec`) with required checks, `ApiException.ValidationErrors` inline field error display, loading spinner submit lock, and Dark/Light sync |
| **`KpiCardControl`** | `AntdUI.Panel` | Dashboard metric card with accent color stripe and theme switching |
| **`SimpleBarChartControl`** | `AntdUI.Panel` + GDI+ | Horizontal comparative bar chart adapting track/text colors dynamically to Light/Dark themes |

## 3. Data Table Column Auto-Sizing & Status Badges (`AppTheme.BindAntdTable<T>`)

`AppTheme.BindAntdTable<T>` guarantees readable columns across dense logistics DTOs:

1. Filters out complex nested collection properties while formatting `IReadOnlyList<string>` (e.g., `Roles`) into comma-separated strings.
2. Formats PascalCase property headers cleanly (`PublicId` -> `Public ID`, `CreatedAtUtc` -> `Created At`, `CodAmount` -> `COD Amount`, word-spaced identifiers).
3. Attaches semantic `AntdUI.CellTag` renderers automatically for boolean and status columns (`Status`, `*Status`, `IsActive`, `Decision`, etc.) via `StatusTagHelper`.
4. Converts `DateTime` / `DateTimeOffset` to local `dd/MM/yyyy HH:mm` strings and formats `decimal` financial/rate columns right-aligned.
5. Measures header width (`TextRenderer.MeasureText(title) + 36`) and samples up to 80 rows to set explicit `Column.MinWidth` and `Column.Width`, expanding evenly when extra viewport width is available and scrolling horizontally otherwise—preventing header truncation (`"Publ..."`, `"Retu..."`).

## 4. Async Error Handling & Modals (`Helpers/UiAsyncRunner.cs`)

- `UiAsyncRunner.RunSafeAsync` automatically sets `AntdUI.Button.Loading = true` during async API requests and displays `AntdUI.Modal` / `AntdUI.Alert` errors (`401` handled via session expiry, `403` warning icon, validation error bullet list).
- `UiAsyncRunner.Confirm` / `ShowInfo` use styled `AntdUI.Modal.open(new Modal.Config(...))` dialogs synchronized with current theme mode.
- `UiAsyncRunner.ShowToastSuccess` / `ShowToastInfo` expose non-blocking `AntdUI.Message` toast banners.
