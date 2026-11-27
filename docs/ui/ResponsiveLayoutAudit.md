# Responsive Layout & Resizing Audit (`Logistics.Client.WinForms`)

## 1. Executive Summary

A full-codebase layout audit was conducted across all forms, shell components, shared AntdUI controls, modal dialogs, and 14 business module workspaces in `src/Client/Logistics.Client.WinForms`. While core AntdUI controls and `Dock` structures are in place, multiple hardcoded sizes, non-wrapping toolbars, single-shot split containers, static table footers, and unbounded window dimensions degrade usability at smaller desktop widths (`1024px`), split-pane views (`~480px`), ultra-wide displays (`> 1920px`), and high-DPI scaling (`125%–150%`).

---

## 2. Discovered Layout & Resizing Issues

### A. Main Window, Sidebar, Header & Footer (`MainForm.cs`, `MainForm.Designer.cs`)

| Component | Current Behavior | Issue Identified | Target Fix |
| :--- | :--- | :--- | :--- |
| **`MainForm` Bounds** | `ClientSize = 1440x880`, `MinimumSize = 1120x680`, **no `MaximumSize`** | Cannot shrink to compact `1024px` laptops; stretches unconstrained on ultra-wide monitors | Enforce `MinimumSize = (1024, 660)`, `MaximumSize = (1920, 1200)` (clamped to screen working area if smaller) |
| **Sidebar (`_sidebarContainer`)** | Manual toggle only (`264px` / `68px`), no `AutoScroll` | At `1024px` width, `264px` sidebar squeezes workspaces to `760px`; tall menus lack scroll | Auto-collapse below `1180px` width (unless user toggled manually), auto-expand above `1240px`, enable vertical `AutoScroll` |
| **Header (`_subHeaderPanel`)** | Fixed `Height = 56`, single-row non-wrapping right flow (`~640px`) | At narrow width with expanded sidebar, breadcrumb space compresses severely | Support responsive width/wrapping or compact selector widths so all header controls remain accessible without clipping |
| **Footer (`_footerPanel`)** | Fixed `Width = 720` (`Dock.Left`) + `Width = 420` (`Dock.Right`) | `720 + 420 = 1140px`: overlaps left and right footer labels whenever window width `< 1170px` | Replace with responsive 2-column `TableLayoutPanel` (`62% / 38%`) with `AutoEllipsis = true` |

### B. Data Grids, Footers & Empty States (`PagedDataGridControl.cs`, `Themes/AppTheme.cs`)

| Component | Current Behavior | Issue Identified | Target Fix |
| :--- | :--- | :--- | :--- |
| **`_footerPanel`** | `_pageSummaryLabel` (`Width = 260`) + `_pagination` (`Width = 480`) (`740px` min) | Collides and clips inside vertical `SplitContainer` panes (`Inventory` Lots/Serials, `Organization` Roles/Permissions, `Finance` Invoices/Items, `~500px` wide) | Replace with responsive `TableLayoutPanel` switching dynamically between 1-row (`>= 680px`) and 2-row compact footer (`< 680px`) so total count, pages, prev/next, and page-size selector stay 100% visible |
| **Empty Table State** | `_table.EmptyHeader` unset (`false`); `_bannerAlert` renders outside table | Column headers vanish on 0 rows; external alert shifts layout height | Set `_table.EmptyHeader = true` so headers + centered empty message stay inside full-height table body with bottom footer anchored cleanly |
| **Resize Performance & Selection Loss** | `_table.SizeChanged` re-invokes `BindAntdTable`, clearing columns and re-binding `DataSource` | Live window resizing rebuilds columns on every pixel, flickers, and resets row selection/scroll | Decouple `UpdateAntdTableColumnWidths(Table)` to adjust `col.Width` proportionally without clearing columns or rebinding `DataSource` |
| **Filter Toolbar (`_filterPanel`)** | `_searchInput` fixed `Width = 280` | Can crowd narrow split panes (`~460px`) | Clamp search width dynamically (`160..280px`) and ensure clean `WrapContents = true` |

### C. Responsive Splitters (`Themes/AppTheme.cs`)

| Component | Current Behavior | Issue Identified | Target Fix |
| :--- | :--- | :--- | :--- |
| **`CreateSafeSplitContainer`** | `applied = true` runs clamping **only once** at first layout | Subsequent window shrinks fail to rebalance splitter proportionally and can compress `Panel2` below readable height | Keep initial ratio/safe bounds and re-clamp proportionally during resize without overriding deliberate user drags beyond safe limits |

### D. Executive Dashboard (`ExecutiveDashboardControl.cs`, `KpiAndChartControls.cs`)

| Component | Current Behavior | Issue Identified | Target Fix |
| :--- | :--- | :--- | :--- |
| **`_topBar` Filters** | Fixed `Height = 60` (`Dock = Top`) | Wrapping filters on narrow sizes (`1024px`) clip below `Y = 60` | Use `AutoSize = true` with `MinimumSize(0, 56)` |
| **`_kpiFlow` Cards** | Fixed `card.Width = 240` inside `FlowLayoutPanel` | Leaves uneven empty margins at intermediate/wide widths; does not adapt evenly into 1/2/3 columns | Dynamic responsive `TableLayoutPanel` switching breakpoints (`< 660px`: 1 col, `660..1039px`: 2 cols, `>= 1040px`: 3 cols) |
| **`_chartsGrid`** | Hardcoded `ColumnCount = 2` (`50% / 50%`) | Charts become too narrow horizontally below `760px` | Switch dynamically between 2-col (`>= 780px`) and stacked 1-col (`< 780px`), capped by centered max container width (`1600px`) |

### E. Forms, Modals & Workspace Fixed Heights

| File | Current Issue Identified | Target Fix |
| :--- | :--- | :--- |
| **`LoginForm.cs`** | Fixed `Size(940, 580)`, `MinimumSize(820, 520)`, fixed `_heroPanel` (`350px`) | Support smaller viewports / DPI scaling (`MinimumSize(700, 480)`, `MaximumSize(1160, 760)`), responsive hero collapse `< 740px`, `_formHost.AutoScroll = true` |
| **`ModalFormDialog.cs`** | Unbounded height (`up to 740`) regardless of screen height; fixed label width `165px` without screen-area clamp | Enforce `MinimumSize(440, 300)` and `MaximumSize` clamped inside `Screen.FromControl.WorkingArea`; wrap long labels cleanly |
| **`OrganizationWorkspaceControl.cs`** | Company `_companyInfoLabel` fixed `Height = 200`, `bar` non-wrapping `Height = 44` | Replace with `AutoSize = true`, `WrapContents = true`, `_tabCompany.AutoScroll = true` |
| **`SalesOrdersWorkspaceControl.cs`** / **`ShippingWorkspaceControl.cs`** | `_detailHeaderCard` / `_shipDetailCard` fixed `Height = 64` | Switch to `AutoSize = true` (`MinimumSize(0, 52)`) with wrapped `Label` width constrained cleanly |
| **`CarrierWorkspaceControl.cs`** | `_opsCard` fixed `Height = 290`; `bar` non-wrapping | `_opsCard` `AutoSize = true` (`MinimumSize(0, 210)`), `bar.WrapContents = true`, `_tabOps.AutoScroll = true` |
| **`WarehouseWorkspaceControl.cs`** | `_hubCard` inside split pane has fixed `controlsFlow`/`opsFlow` heights (`46`, `44`); `BuildSubPanel` `top` fixed `Height = 42` | Set `AutoSize = true, WrapContents = true` on toolbars, `_hubCard.AutoScroll = true` |
| **`CodWorkspaceControl.cs`** | `_reconSummaryCard` fixed `Height = 122`, `actionPanel` fixed `Height = 52`, `_colSummaryLabel` fixed `Height = 44` | Switch cards/bars to `AutoSize = true` + `WrapContents = true` |
| **`SettingsWorkspaceControl.cs`** | `_profileCard` fixed `Height = 270`; `_configCard` fixed `Height = 395`; tabs lack `AutoScroll` | Set `AutoSize = true`, `_profileSummaryLabel.AutoSize = true`, `AutoScroll = true` on tabs, balanced max form width (`760px`) |
| **`FinanceWorkspaceControl.cs`** | `rulesTop` fixed `Height = 42` (`no wrap`), `invSplit` `680px` initial distance | `rulesTop.AutoSize = true, WrapContents = true`; safe proportional split clamping |
