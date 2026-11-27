# Desktop Client (WinForms) Audit

## Architecture & Security Review

- **Project Layering**: Clean separation across `Logistics.Client.Application` (contracts, interfaces, `ClientPermissions`, localization `AppStrings`), `Logistics.Client.Infrastructure` (`ApiClient`, `AuthenticationDelegatingHandler`, `ClientAuthenticationService`, `BranchContextService`, `ClientSettingsService`, typed API clients), and `Logistics.Client.WinForms` (`MainForm`, `LoginForm`, `DpapiSecureTokenStore`, workspace controls).
- **Token Security**:
  - `DpapiSecureTokenStore` protects only `StoredRefreshSession` using Windows DPAPI (`DataProtectionScope.CurrentUser` + entropy `"Logistics.Desktop.DPAPI.v1"`), never persisting passwords or access tokens on disk.
  - `AuthenticationDelegatingHandler` performs single-flight token refresh on expiry/401, attaches `Bearer` tokens and `X-Branch-Id` branch context headers, clones request content on single retry, and triggers `HandleSessionInvalidatedAsync` on refresh failure.
- **Environment & Production Guard**:
  - `ClientSettingsService.ValidateSettings` rejects invalid HTTP/HTTPS URLs, enforces timeouts in `[5, 300]` seconds, and blocks `localhost` / loopback URLs when `Environment == "Production"` (`appsettings.Production.json` ships with blank `BaseUrl` so Production cannot silently default to localhost).
- **Concurrency, Validation & UX Reliability**:
  - `UiAsyncRunner.RunSafeAsync` handles `OperationCanceledException`, surfaces structured `ApiException` and FluentValidation field errors, disables target trigger buttons while running, and restores `UseWaitCursor`.
  - `ModalFormDialog` disables `_saveBtn` during async submission, maps backend `ValidationErrors` onto an `ErrorProvider`, and closes only on success.

## Findings & Remediations

- **UI-01 Fixed (Unchecked Workspace Tabs on Warehouse, Inventory, and Carrier Controls)**:
  - `WarehouseWorkspaceControl` added `_tabFacilities`, `_tabParcelHub`, and `_tabDispatches` unconditionally without checking `ClientPermissions.Warehouse.*`.
  - `InventoryWorkspaceControl` added `_tabBalances`, `_tabReceipts`, `_tabMovements`, and `_tabTracking` unconditionally without checking `ClientPermissions.Inventory.*` / `Warehouse.ReceiptsManage`.
  - `CarrierWorkspaceControl` added `_tabCarriers`, `_tabOps`, and `_tabLogs` unconditionally without checking `ClientPermissions.Carrier.*`.
  - Additionally, `SelectSection` on all three controls assigned `_tabs.SelectedTab` directly without checking `_tabs.TabPages.Contains(tab)`, which throws `ArgumentException` when a tab is hidden.
  - **Fix Applied**: Added permission checks matching sibling modules (`FinanceWorkspaceControl`, `CodWorkspaceControl`, `ShippingWorkspaceControl`, `NotificationsWorkspaceControl`, `OrganizationWorkspaceControl`) and guarded `SelectSection` with `_tabs.TabPages.Contains(tab)`.
- **UI-02 Informational (Proof-of-Delivery File Picker)**:
  - `ShippingWorkspaceControl.RecordPodAsync` lets operators choose a local image file and honestly embeds the filename as metadata in `Notes`, rather than simulating nonexistent binary upload endpoints (`ProofOfDeliveryController` exposes JSON metadata only).
- **UI-03 Remaining Non-Blocking Item**: No automated WinForms UI test harness executes live form rendering (`Logistics.Client.WinForms` targets `net9.0-windows`); desktop API infrastructure and permission/branch/settings logic are covered by `Logistics.Client.UnitTests`, and all `net9.0-windows` targets compile with `0` errors.
