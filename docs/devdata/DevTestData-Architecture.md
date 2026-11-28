# Dev Test Data Architecture

## 1. Overview

The **Dev Test Data** subsystem enhances the existing `DevDataSeeder` (`Logistics.Server.Infrastructure.Services.DevDataSeeder`) rather than introducing a parallel seeding tool. It populates a comprehensive, interconnected Vietnamese logistics enterprise dataset across all 14 database schemas and 90 DbSets in `ApplicationDbContext`.

## 2. Layered Components

| Layer | Component | Responsibility |
|---|---|---|
| Shared Contracts | `src/Shared/Logistics.Shared.Contracts/DevData/DevTestDataContracts.cs` | Shared enums (`DevSeedMode`, `DevSeedScale`) and DTOs (`DevTestDataStatusDto`, `ExecuteDevSeedRequest`, `ExecuteDevSeedResultDto`, `DevTestDataIntegrityReportDto`) |
| Server Application | `src/Server/Logistics.Server.Application/Common/Interfaces/IDevTestDataService.cs` | Service abstraction for status inspection, seeding execution, and data integrity verification |
| Server Infrastructure | `DevDataSeeder.cs`, `DevDataSeeder.Operational.cs`, `DevDataSeeder.ResetAndVerify.cs` | Seed orchestration, deterministic realistic generation, historical timestamp backdating (`ExecuteUpdateAsync`), transactional reset, and integrity verification |
| Server API | `src/Server/Logistics.Server.API/Controllers/Admin/DevTestDataController.cs` | REST endpoints (`/api/v1/dev/test-data/status`, `/execute`, `/verify`) protected by RBAC + Development environment guards |
| Client Infrastructure | `ITypedApiClients.cs` / `DevTestDataApiClient` (`CoreApiClients.cs`) | Typed HTTP client calling the dev test data API |
| Desktop WinForms UI | `SettingsWorkspaceControl.cs` (`Dữ liệu Test (Dev)` tab) | Admin-only, Development-only tab displaying DB info, module counts, execution controls, and integrity check runner |

## 3. Execution Modes & Profiles

- **FullSeed**: Idempotent seed across reference and operational tables (`AnyAsync` guards per section). Executed automatically at API startup when `IWebHostEnvironment.IsDevelopment()` and `DevSeed:Enabled = true`.
- **Incremental**: Appends a fresh batch of orders, shipments, pickups, warehouse movements, invoices, payments, COD collections/remittances, notifications, and audit logs without colliding with existing unique codes.
- **ResetAndReseed**: Deletes operational/CRM/catalog/branch test data in strict reverse FK order inside a database transaction (`BeginTransactionAsync`), preserves system `Roles`, `Permissions`, `RolePermissions`, and the bootstrap `admin` account, and cleanly reseeds at the chosen scale.
- **ModuleSpecific**: Seeds selected module groups (`CRM`, `Catalog`, `Carrier`, `SalesShipping`, `Warehouse`, `Inventory`, `Finance`, `COD`, `Notifications`, `Audit`) while guaranteeing reference prerequisites.
- **Scales**: `Standard` (~600 orders, ~500+ shipments, 120 customers) and `Large` (~950 orders, ~800+ shipments, 200 customers).

## 4. Development-Only Safety Controls

1. **Controller Guard**: `DevTestDataController` verifies both `IWebHostEnvironment.IsDevelopment()` and `IConfiguration["DevSeed:Enabled"] == true` before serving any endpoint (returning `403 Forbidden` otherwise).
2. **Service Guard**: `DevDataSeeder.EnsureDevSafety` re-verifies environment, configuration flag, and rejects any database name containing `prod` (defense-in-depth).
3. **RBAC Authorization**: All endpoints require authenticated JWT + `[HasPermission(AppPermissions.Configuration.SettingsManage)]`.
4. **Destructive Confirmation**: `ResetAndReseed` requires `ConfirmReset = true` in the payload (plus interactive confirmation dialog in WinForms) and executes inside an atomic database transaction.
5. **UI Gating**: The `Dữ liệu Test (Dev)` tab in `SettingsWorkspaceControl` is hidden unless client environment is `Development` and the signed-in user has `configuration.settings.manage` or `SUPER_ADMIN`.
