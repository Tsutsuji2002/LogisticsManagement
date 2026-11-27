# System Architecture Review

## Solution Structure & Project Dependency Map

| Layer / Host | Project | Key Responsibilities & Dependencies |
|---|---|---|
| Shared Contracts | `Logistics.Shared.Contracts` | API envelope (`ApiResponse<T>`), `PagedRequest`, `PagedResponse<T>`, health DTOs. |
| Server Domain | `Logistics.Server.Domain` | Entities (`BaseEntity`), Identity, Organization, CRM, Catalog, Sales, Shipping, Carrier, Warehouse, Inventory, Finance, COD, Notification, Outbox. |
| Server Application | `Logistics.Server.Application` | Service interfaces, module DTOs, FluentValidation validators, `AppRoles`, `AppPermissions`, integration event records. |
| Server Integrations | `Logistics.Server.Integrations` | External carrier adapters (`GhnCarrierAdapter`, `GhtkCarrierAdapter`, `ViettelPostCarrierAdapter`, `CarrierAdapterFactory`) and email provider abstractions. |
| Server Infrastructure | `Logistics.Server.Infrastructure` | Single `ApplicationDbContext`, EF configurations, migrations, all domain service implementations, `OutboxProcessorService`, reporting query/export services, `DevDataSeeder`, `IdentityDataSeeder`. |
| Server API Host | `Logistics.Server.API` | ASP.NET Core controllers (`v1`), JWT Bearer pipeline, dynamic permission/branch authorization handlers, rate limiting, `GlobalExceptionMiddleware`, health checks. |
| Server Worker Host | `Logistics.Server.Worker` | Background `BackgroundService` hosts: `OutboxMessageProcessorJob`, `BackgroundJobQueueWorkerJob`, `NotificationDeliveryWorkerJob`, `CarrierTrackingSyncJob`. |
| Client Application | `Logistics.Client.Application` | Typed HTTP client interfaces, shared client contracts, `ClientPermissions`, localization strings (`AppStrings`). |
| Client Infrastructure | `Logistics.Client.Infrastructure` | `ApiClient`, `AuthenticationDelegatingHandler`, `ClientAuthenticationService`, `ClientAuthorizationService`, `BranchContextService`, `ClientSettingsService`, typed module API clients. |
| Client WinForms | `Logistics.Client.WinForms` | `MainForm`, `LoginForm`, `DpapiSecureTokenStore`, reusable `PagedDataGridControl`/`ModalFormDialog`, module workspace controls. |

## Boundary Assessment

1. **Database & Ownership Boundary**: A single `ApplicationDbContext` owns all schemas cleanly (`identity`, `organization`, `crm`, `catalog`, `sales`, `shipping`, `carrier`, `warehouse`, `inventory`, `finance`, `cod`, `notification`, `integration`, `configuration`). Reporting queries execute read-only against `ApplicationDbContext` via `AsNoTracking()` without owning mutable tables.
2. **Async Integration Boundary**: Domain state mutations (`OrderService`, `ShipmentService`, `DeliveryAttemptService`, `PaymentService`, `CodCollectionService`, `ReturnRequestService`) enqueue outbox events inside the business save cycle, which `OutboxProcessorService` (run by `Logistics.Server.Worker`) leases with SQL Server `UPDLOCK, READPAST, ROWLOCK` and dispatches idempotently to `NotificationOutboxEventHandlers`.
3. **Authentication & Branch Boundary**: Controllers enforce endpoint permissions via `[HasPermission(...)]` (`PermissionAuthorizationHandler`), while application services enforce row-level branch ownership via `ICurrentUserService` (`HasBranchAccess`, `AccessibleBranchIds`, `IsInRole(AppRoles.SuperAdmin)` / `"Admin"`).

## Architectural Findings & Known Limitations

- **Single-Host / Shared DB Coupling in Seeders**: `DevDataSeeder` runs conditionally when `db.Database.IsRelational()`, keeping InMemory integration tests clean while populating local/dev SQL Server databases safely.
- **No Dedicated Binary File Storage / Upload Controller**: `ProofOfDeliveryController` accepts `SignatureAttachmentId` / `PhotoAttachmentId` JSON metadata referencing `FileAttachments`, rather than exposing multipart `IFormFile` upload endpoints; the WinForms POD modal records local filename metadata honestly without faking remote blob storage.
- **Inventory Adjustments & Transfers Lack Detail Tables**: Domain tables `StockAdjustments` and `StockTransfers` contain header columns only (no child line-item tables in EF schema), preventing atomic balance postings upon draft approval/transfer receipt without a future schema migration.
- **Carrier Adapter Implementations**: `GhnCarrierAdapter`, `GhtkCarrierAdapter`, and `ViettelPostCarrierAdapter` extend `CarrierAdapterBase` without per-carrier live HTTP API credentials/endpoints; `CarrierConfiguration` credentials are stored in database columns as plaintext fields (masked in API DTOs).
