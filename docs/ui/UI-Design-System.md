# UI Design System — Logistics Enterprise Desktop (AntdUI)

## 1. Visual Direction & Tokens

The desktop client follows an Ant Design-inspired enterprise logistics aesthetic prioritizing legibility, dense structured tables, clear semantic status colors, and zero decorative clutter.

### Color Tokens (`Themes/AppTheme.cs`)

| Token | Light Theme | Dark Theme | Usage |
| :--- | :--- | :--- | :--- |
| `PrimaryBlue` | `#1677FF` | `#1677FF` | Primary actions, active links, in-progress states |
| `PrimaryHover` | `#4096FF` | `#4096FF` | Accent highlights, hover indicators |
| `SuccessEmerald` | `#16A34A` | `#16A34A` | Delivered, Paid, Approved, Active indicators |
| `WarningAmber` | `#D97706` | `#D97706` | Pending, Partial, Returning, Low-stock alerts |
| `DangerRed` | `#DC2626` | `#DC2626` | Failed delivery, Cancelled, Overdue, Destructive actions |
| `PurpleAccent` | `#722ED1` | `#722ED1` | Sorting Hub & secondary chart metrics |
| `SidebarBg` | `#001529` | `#001529` | Enterprise dark navigation sider |
| `SurfaceBg` | `#F5F7FA` | `#141414` | Main workspace background |
| `CardBg` | `#FFFFFF` | `#1F1F1F` | Card panels, tables, modal surfaces |
| `RowAltBg` | `#F8FAFC` | `#262626` | Alternating rows, footer strips, table headers |
| `BorderSubtle` | `#E2E8F0` | `#333333` | 1px card/table/divider borders |
| `TextPrimary` | `#0F172A` | `#F0F0F0` | Headings, table body values, labels |
| `TextSecondary` | `#475569` | `#A6A6A6` | Subtitles, secondary descriptions |
| `TextMuted` | `#64748B` | `#737373` | Captions, empty states, footer metadata |

## 2. Typography Hierarchy

All fonts use **`Segoe UI`** with `AntdUI.Config.TextRenderingHighQuality = true` to ensure accurate rendering of Vietnamese diacritics and English labels:

- `PageTitleFont`: `Segoe UI`, `14F`, `Bold`
- `SectionTitleFont`: `Segoe UI`, `10.5F`, `Bold`
- `BodyFont`: `Segoe UI`, `9.5F`, `Regular`
- `BodyBoldFont`: `Segoe UI`, `9.5F`, `Bold`
- `SmallFont`: `Segoe UI`, `8.5F`, `Regular`
- `KpiValueFont`: `Segoe UI`, `18F`, `Bold`

## 3. Theme Switching & Persistence

- Implemented via `AppThemeMode` (`Light` / `Dark`), `AppTheme.SetTheme(mode, rootForm)`, `AppTheme.ToggleTheme(rootForm)`, and `IThemeAware`.
- Switchable live from:
  1. `MainForm` top toolbar (`☀ Sáng` / `🌙 Tối`)
  2. `LoginForm` top bar & Quick API Settings modal
  3. `SettingsWorkspaceControl` configuration tab (`Light Theme` / `Dark Theme`)
- Theme changes apply recursively via `AppTheme.ApplyThemeToTree(root)` across cached modules, AntdUI window chrome (`TAMode`), tables, cards, KPIs, and bar charts, and persist to `%LOCALAPPDATA%\LogisticsManagement\theme.pref`.

## 4. Semantic Status Tag Mapping (`Themes/StatusTagHelper.cs`)

| `AntdUI.TTypeMini` | Mapped Workflow / Financial States |
| :--- | :--- |
| `Success` | `Delivered`, `Completed`, `Paid`, `Approved`, `Active`, `Confirmed`, `Settled`, `Reconciled`, `Matched`, `Succeeded`, `Sent`, `Passed`, `Healthy`, `Available`, `Yes`/`True` |
| `Primary` | `InTransit`, `OutForDelivery`, `InSortingHub`, `PickedUp`, `ReadyForPickup`, `ReadyToShip`, `Allocated`, `Picking`, `Packed`, `Processing`, `Booked`, `Assigned`, `Submitted`, `Collected`, `Published`, `Received`, `Dispatched` |
| `Info` | `Open`, `New`, `Read`, `Info`, `Scheduled`, `Standard` |
| `Warn` | `Pending`, `PartiallyPaid`, `Partial`, `Returning`, `OnHold`, `Warning`, `Warn`, `LowStock`, `Unread`, `Requested`, `Retry`, `PendingReview` |
| `Error` | `DeliveryFailed`, `Failed`, `Cancelled`, `Canceled`, `Rejected`, `Overdue`, `Disputed`, `Discrepancy`, `Shortage`, `Void`, `Voided`, `Locked`, `Suspended`, `Damaged`, `Expired`, `Error`, `False`/`No` |
| `Default` | `Draft`, `Inactive`, `Returned`, unmapped neutral values |
