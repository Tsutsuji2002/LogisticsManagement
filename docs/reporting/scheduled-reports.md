# Scheduled Reports & Background Execution Engine

## 1. Execution Engine (`ReportingExecutionEngine`)

`ReportingExecutionEngine` implements `IReportExecutionEngine` inside `Logistics.Server.Infrastructure` and replaces `UnsupportedReportExecutionEngine` in DI. Because scheduled reports can execute inside `Logistics.Server.Worker` without an authenticated HTTP user context, `ReportingExecutionEngine` queries `ApplicationDbContext` directly using `branchId` (when scoped to a branch) or company-wide scope (when `branchId == null`).

- **Supported Output Format**: `Csv` generates UTF-8 BOM CSV bytes via `ReportingQueryHelpers.BuildCsv`.
- **Unsupported Output Formats (`Pdf`, `Excel`)**: Returns `ReportGenerationResult { IsSupported = false, Succeeded = false }`, causing `ScheduledReportService` to log a `Warning` execution entry rather than generating synthetic binary output.
- **Supported Seeded Report Codes (`Csv`)**:
  - `RPT_DAILY_DISPATCH`
  - `RPT_ORDERS`
  - `RPT_SHIPMENTS`
  - `RPT_INVENTORY_BALANCES`
  - `RPT_FINANCE_INVOICES`
  - `RPT_COD_RECONCILIATION`

## 2. Management Endpoints

Scheduled reports are manageable via both:
- `/api/v1/scheduled-reports/*` (`ScheduledReportsController` with `notification.reports.*` permissions)
- `/api/v1/reports/definitions` & `/api/v1/reports/scheduled/*` (`ReportsController` with `reports.schedule.manage`, where `DELETE /api/v1/reports/scheduled/{id}` softly deactivates the schedule via `SetActiveStatusAsync(id, false)`).
