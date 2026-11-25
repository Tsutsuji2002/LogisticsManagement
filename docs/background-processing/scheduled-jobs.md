# Scheduled Jobs Specification

## 1. Registered Hosted Background Services (`Logistics.Server.Worker`)

| Hosted Service | Config Interval Key | Default | Responsibilities |
| :--- | :--- | :--- | :--- |
| `OutboxMessageProcessorJob` | `Worker:OutboxPollingSeconds` | `10s` | Recovers stale `InProgress` leases (> `OutboxLeaseMinutes`, default 5m) and claims/dispatches pending outbox batches. |
| `NotificationDeliveryWorkerJob` | `Worker:NotificationPollingSeconds` | `15s` | Sends pending `Email` notifications with exponential backoff & `Retry-After` handling. |
| `BackgroundJobQueueWorkerJob` | `Worker:JobPollingSeconds` | `15s` | Recovers stale running jobs, executes pending queued jobs, triggers due `ScheduledReports` (~1m cadence), and runs maintenance cleanup. |
| `CarrierTrackingSyncJob` | `Worker:CarrierSyncMinutes` | `5m` | Polls active external carriers via `ICarrierTrackingSyncOrchestrator` and records job telemetry in `integration.BackgroundJobLogs`. |

---

## 2. Maintenance Cleanup (`IScheduledMaintenanceService`)

Executed periodically (`Worker:MaintenanceIntervalHours`, default `6h`):
- **Session & Token Retention Cleanup**: Deletes `UserSessions` expired or terminated > 7 days ago and `RefreshTokens` expired or revoked > 7 days ago (bounded batches of 500 rows).
- **Expired Notification Retention Cleanup**: Deletes `Notifications` whose `ExpiresAt` passed > 30 days ago (bounded batches of 500 rows).

---

## 3. Timezone & Cron Policy (`CronScheduleHelper`)

- Supports standard 5-field cron expressions (`minute hour day month dayOfWeek`) including `*`, `*/step`, ranges (`1-5`), and comma lists.
- Evaluates schedules in the configured Windows/IANA `TimeZoneId` (e.g., `SE Asia Standard Time`, `UTC`) and converts resulting timestamps to UTC.
- **Missed Execution Policy**: When a schedule runs after downtime, `NextRunAt` advances strictly forward from `DateTime.UtcNow` rather than firing multiple catch-up runs for missed historical windows.
- **Overlap Prevention**: Before running any scheduled job/report, `BackgroundJobLogs` is queried for active `Running` instances of the same `JobReference`. Overlapping runs are skipped or rejected with `409 Conflict`.
