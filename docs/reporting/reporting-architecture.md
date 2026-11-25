# Reporting & Dashboard Architecture

## 1. Design Principles

The **Reporting & Dashboard Module** provides a read-only analytical layer across the existing Logistics Management modular monolith without creating a separate reporting database, secondary `DbContext`, or duplicating operational workflows:

1. **Single Authoritative DbContext**: All queries execute directly against `ApplicationDbContext` (`AsNoTracking()`) with relational command timeouts (`30s`) via `ReportingQueryHelpers.ConfigureReadOnlyTimeout`.
2. **Database-Side Aggregation**: Counting, grouping, summing, filtering, and pagination execute in SQL Server via EF Core LINQ projections; computed columns (`InventoryBalance.QuantityAvailable`, `Invoice.OutstandingAmount`) are evaluated explicitly (`QuantityOnHand - QuantityAllocated`, `TotalAmount - PaidAmount`) so queries behave identically on SQL Server and EF Core InMemory.
3. **Strict Branch & RBAC Enforcement**:
   - Every dashboard, report, and export resolves the caller's authorized scope through `ReportingQueryHelpers.ResolveBranchScope(ICurrentUserService, requestedBranchId)` (never trusting client-supplied `BranchId` values without server-side validation against `AccessibleBranchIds` or `AppRoles.SuperAdmin`).
   - Unauthorized cross-branch queries throw `ForbiddenAccessException` (HTTP `403`).
   - On `GET /api/v1/dashboard/branches/{branchId}`, `FinanceSummary` is included only if the caller also holds `reports.finance.read`.
4. **Timezone-Consistent Half-Open Date Windows**:
   - `ReportFilterRequest.StartDate` and `EndDate` are interpreted in `TimeZoneId` (default `UTC`, e.g., `SE Asia Standard Time`) and converted to half-open UTC bounds `[UtcFromInclusive, UtcToExclusive)`.
   - Validated by `ReportFilterRequestValidator` (`EndDate >= StartDate`, maximum `366`-day range, valid timezone ID, `Search <= 100` chars).
5. **Short-TTL Scoped Caching**:
   - `DashboardQueryService.GetCompanyOverviewAsync` caches scoped dashboard snapshots via `ICacheService` (`45s` TTL) keyed by branch scope (`global` vs sorted `AccessibleBranchIds` or requested `BranchId`) and query filters.

## 2. Clean Architecture Placement

- **Application**:
  - Contracts & Filters: `src/Server/Logistics.Server.Application/Modules/Reporting/DTOs/ReportingDtos.cs`
  - Service Interfaces: `src/Server/Logistics.Server.Application/Modules/Reporting/Services/IReportingServices.cs`
  - Validators: `src/Server/Logistics.Server.Application/Modules/Reporting/Validators/ReportingValidators.cs`
  - Permissions: `AppPermissions.Reporting` (`src/Server/Logistics.Server.Application/Common/Security/AppPermissions.cs`)
- **Infrastructure**:
  - `ReportingQueryHelpers`: `src/Server/Logistics.Server.Infrastructure/Services/Reporting/ReportingQueryHelpers.cs`
  - `DashboardQueryService`: `src/Server/Logistics.Server.Infrastructure/Services/Reporting/DashboardQueryService.cs`
  - `OperationalReportService`: `src/Server/Logistics.Server.Infrastructure/Services/Reporting/OperationalReportService.cs`
  - `FinancialReportService`: `src/Server/Logistics.Server.Infrastructure/Services/Reporting/FinancialReportService.cs`
  - `ReportExportService` & `ReportingExecutionEngine`: `src/Server/Logistics.Server.Infrastructure/Services/Reporting/ReportExportService.cs`
- **API**:
  - `DashboardController`: `src/Server/Logistics.Server.API/Controllers/Reporting/DashboardController.cs`
  - `ReportsController`: `src/Server/Logistics.Server.API/Controllers/Reporting/ReportsController.cs`
