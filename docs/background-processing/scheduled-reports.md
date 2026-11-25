# Scheduled Reports Specification & Limitations

## 1. Implemented Capabilities (`IScheduledReportService`)

Managing `notification.ReportDefinitions` and `notification.ScheduledReports` supports:
- Listing and viewing `ReportDefinitions` (code, name, category, description, parameters schema).
- Creating, updating, enabling/disabling (`IsActive`), and listing `ScheduledReports`.
- **Cron & Timezone Scheduling**: 5-part cron evaluation in the report's `TimeZoneId` (`UTC`, `SE Asia Standard Time`, etc.).
- **Recipient Email Validation**: Parses semicolon/comma-separated email lists, validates `MailAddress` syntax, and rejects invalid addresses.
- **Branch-Level Authorization**:
  - Non-`SUPER_ADMIN` users can only read, create, update, or trigger scheduled reports assigned to branches in their `UserBranchAccess`.
  - Global reports (`BranchId == null`) require `SUPER_ADMIN` privileges.
- **Overlap Protection & Execution History**: Prevents concurrent execution of the same `ScheduledReport:{Id}` and records every manual or scheduled execution into `integration.BackgroundJobLogs`.

---

## 2. Explicit Limitation: Report Generation & Binary File Storage

Currently, the codebase contains schema definitions (`ReportDefinitions`, `ScheduledReports`, `audit.FileAttachments`) but **no concrete reporting/query execution engine or binary PDF/Excel rendering library** (such as QuestPDF, ClosedXML, or SSRS integration) has been implemented.

In strict adherence to architectural honesty:
1. A stable contract `IReportExecutionEngine` (`Task<ReportGenerationResult> GenerateAsync(...)`) is provided in `Logistics.Server.Application.Modules.BackgroundProcessing.Services`.
2. The default registered implementation `UnsupportedReportExecutionEngine` returns:
   - `IsSupported = false`
   - `Succeeded = false`
   - `Message = "Report generation engine is not yet implemented for Report '{Code}' ({Format}). No synthetic or placeholder report file was generated."`
3. Scheduled/manual triggers log this transparently as `Status = "Warning"` in `BackgroundJobLogs` and **never generate fake PDF/Excel files or fabricate financial/operational numbers**.
4. Once a concrete `IReportExecutionEngine` implementation is registered in DI, `ScheduledReportService` will automatically email generated reports to validated recipients via `INotificationService`.
