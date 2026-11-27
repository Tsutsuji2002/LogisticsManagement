# Performance Audit

## Query & Persistence Observations

1. **Read-Only Query Efficiency**: Reporting (`DashboardQueryService`, `FinancialReportService`, `OperationalReportService`) and catalog/CRM/order listing endpoints consistently apply `.AsNoTracking()` and server-side pagination (`PagedRequest` clamped to safe bounds, max 100 in API listings and capped at 10,000 rows in CSV exports via `ReportExportService`).
2. **In-Memory Aggregation Trade-Offs (Financial & COD Reports)**:
   - `CodCollectionService.GetSummaryAsync` and `CodReconciliationService.GetSummaryAsync` materialize matching filtered rows via `.ToListAsync()` and compute `Sum(...)` / `Count(...)` in memory. While date filters (`fromDate`, `toDate`) now cover both collections and remittances in `CodReconciliationService.GetSummaryAsync` and branch scoping prevents unbounded multi-tenant loads, large multi-year exports without date filters could load tens of thousands of rows. Server-side SQL `GroupBy(_ => 1)` projection is recommended if volume grows beyond ~50k rows per branch.
3. **Authentication Database Query per Request (Trade-Off)**:
   - Enabling `JwtBearerEvents.OnTokenValidated` in `Program.cs` adds one lightweight indexed primary-key projection query (`SELECT IsActive, IsLockedOut, LockoutEndAt, SecurityStamp FROM UserAccounts WHERE Id = @userId`) per authenticated HTTP request, eliminating the risk of stale/revoked JWT reuse across the 60-minute access-token window.
   - `PermissionService` caches permissions and branch lists per DI HTTP scope (`_cachedPermissions`, `_cachedBranches`). Wait/cross-request Redis/MemoryCache invalidation is not needed today because per-request queries stay small, though a short sliding cache (15-30s) keyed by `(UserId, SecurityStamp)` would cut DB round-trips at high QPS.
4. **Outbox & Background Job Throughput**:
   - `OutboxProcessorService` (`ClaimPendingMessagesAsync`) and `BackgroundJobService` (`ClaimPendingJobsAsync`) use SQL Server `UPDATE TOP (@batchSize) ... WITH (UPDLOCK, READPAST, ROWLOCK) OUTPUT INSERTED.*` — verified zero duplicate claims and non-blocking parallel worker throughput across 3 concurrent workers in `OutboxSqlServerConcurrencyTests`.
   - `NotificationDeliveryService.ProcessPendingDeliveriesAsync` evaluates `AsNoTracking()` candidates first and executes external `_emailProvider.SendAsync(...)` completely outside any DB transaction (`SendAsync` -> then persist `NotificationDelivery`).
5. **Tracking Sync Loops (`CarrierTrackingSyncOrchestrator`)**:
   - `SyncCarrierTrackingAsync` loops over active shipments, issuing `CarrierTrackingEvents.AnyAsync` and `CarrierTrackingMappings.FirstOrDefaultAsync` per shipment. Pre-loading carrier mappings per carrier once per sync cycle is an easy future micro-optimization (`Medium`/`Low`).
