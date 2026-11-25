# Background Processing Architecture Specification

## 1. Overview & Worker Topology

The **Background Processing Module** runs inside `Logistics.Server.Worker` (`.NET 9` Generic Host) alongside transactional queuing primitives in `Logistics.Server.Infrastructure`. It coordinates:
1. **Transactional Outbox Dispatching** (`OutboxMessageProcessorJob`)
2. **Outbound Notification Delivery** (`NotificationDeliveryWorkerJob`)
3. **Queued Job Execution, Scheduled Reports & Maintenance** (`BackgroundJobQueueWorkerJob`)
4. **External Carrier Tracking Polling** (`CarrierTrackingSyncJob`)

No external message brokers (Redis, Kafka, RabbitMQ) were introduced; concurrency safety across multiple worker instances relies on SQL Server row-level locking (`WITH (UPDLOCK, READPAST, ROWLOCK)`) and `UPDATE ... OUTPUT INSERTED.*` atomic claiming on `integration.OutboxMessages` and `integration.BackgroundJobLogs`.

---

## 2. Background Job Lifecycle (`integration.BackgroundJobLogs`)

| Status | Meaning | Permitted Transitions |
| :--- | :--- | :--- |
| `Pending` | Queued for execution or waiting for `NextRetryAt`. | -> `Running` (worker claim), -> `Cancelled` (manual cancel) |
| `Running` | Actively leased (`LockedAt`, `LockedBy`) by a worker instance. | -> `Success`, -> `Warning`, -> `Pending` (retry/stale recovery), -> `Failed` |
| `Success` | Completed cleanly. | Terminal |
| `Warning` | Executed but flagged a non-fatal condition (e.g., unsupported report renderer). | Terminal or -> `Pending` (manual retry) |
| `Failed` | Exhausted `MaxAttempts` or encountered permanent error. | -> `Pending` (manual retry) |
| `Cancelled` | Safely cancelled while `Pending`. | -> `Pending` (manual retry) |

---

## 3. Multi-Worker Concurrency, Stale Recovery & Shutdown

1. **Atomic Row Claiming**:
   - SQL Server workers claim jobs using a single atomic `UPDATE target SET Status = 'Running', AttemptCount = AttemptCount + 1, LockedAt = @now, LockedBy = @workerId OUTPUT INSERTED.* FROM [integration].[BackgroundJobLogs] target INNER JOIN (SELECT TOP (@batchSize) Id ... WITH (UPDLOCK, READPAST, ROWLOCK)) ...`.
   - `UPDLOCK` prevents two workers selecting the same row; `READPAST` prevents lock contention by skipping rows currently locked by another worker.
   - EF Core detaches any tracked snapshot prior to `Attach(row)` so state changes (`LockedBy = null`) persist accurately.
2. **Stale Processing Recovery**:
   - If a worker process crashes mid-execution, `RecoverStaleJobsAsync` identifies `Running` jobs whose `LockedAt` exceeds the configured lease timeout (`Worker:JobLeaseMinutes`, default 10m) and resets them to `Pending` (if `AttemptCount < MaxAttempts`) or marks them `Failed`.
3. **Graceful Shutdown**:
   - All hosted services observe `CancellationToken stoppingToken`. When shutdown is triggered during batch processing, uncompleted claimed items release their lease back to `Pending` cleanly using `CancellationToken.None` so no work is orphaned.
4. **Scoped DI Execution**:
   - Every polling iteration creates a fresh `IServiceScope` so `ApplicationDbContext` and scoped application services are properly disposed per cycle.
