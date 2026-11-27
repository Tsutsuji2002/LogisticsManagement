# Integration & Background Processing Audit

## 1. Carrier Integration & Webhooks

- **Adapters (`Logistics.Server.Integrations/Carriers/CarrierAdapters.cs`)**: `CarrierAdapterFactory` resolves `GHN`, `GHTK`, and `VTPOST` adapters extending `CarrierAdapterBase`. These provide structured mock/simulated carrier behaviors for offline development and integration testing; live production carrier API calls require environment-specific HTTP client extensions per carrier.
- **INT-01 Fixed (Duplicate Carrier Booking Guard)**: `CarrierIntegrationServiceImpl.BookShipmentAsync` now blocks re-booking shipments that already carry a `CarrierTrackingNumber` or are in terminal status (`Delivered`, `Returned`, `Cancelled`), and enforces branch access on both `BookShipmentAsync` and `CancelShipmentAsync`.
- **INT-02 Fixed (Webhook Signature Validation, Parsing & Replay Protection)**: `CarrierWebhookServiceImpl.ProcessWebhookAsync` previously wrote `"PENDING_PARSE"` without verifying signatures, parsing fields, updating shipments, or checking duplicate `ExternalEventId` values. Now:
  - Enforces HMAC-SHA256 verification (`X-Webhook-Signature`) via `CryptographicOperations.FixedTimeEquals` whenever an active `WebhookSecret` is configured, and rejects unsigned requests in non-Development hosts when no secret exists.
  - Parses tracking number, event status/code, description, and explicit `event_id` (falling back to deterministic SHA-256 hash of carrier code + payload for replay detection).
  - Deduplicates repeated `ExternalEventId` arrivals idempotently.
  - Translates active `CarrierTrackingMapping` rules through `ShipmentStateHelper.IsValidTransition` + `ShipmentStatusHistory` (`Source = "CarrierWebhook"`) + `DeliveryCompletedIntegrationEvent`.
- **INT-03 Fixed (Carrier Tracking Sync State-Machine Bypass)**: `CarrierTrackingSyncOrchestrator.SyncCarrierTrackingAsync` now enforces `ShipmentStateHelper.IsValidTransition`, appends `ShipmentStatusHistory` (`Source = "CarrierSync"`), updates `ActualDeliveryDate`, and enqueues `DeliveryCompletedIntegrationEvent` when transitioning to `Delivered`.

## 2. Transactional Outbox & Background Worker (`Logistics.Server.Worker`)

- **Worker Host Jobs**: `OutboxMessageProcessorJob`, `BackgroundJobQueueWorkerJob`, `NotificationDeliveryWorkerJob`, and `CarrierTrackingSyncJob` all create scoped DI containers per cycle and exit cleanly on `OperationCanceledException` during host shutdown.
- **SQL Server Concurrency**: `OutboxProcessorService.ClaimPendingMessagesAsync` and `BackgroundJobService.ClaimPendingJobsAsync` use `UPDATE TOP (@batchSize) ... WITH (UPDLOCK, READPAST, ROWLOCK) OUTPUT INSERTED.*` to safely coordinate multiple concurrent worker instances without double processing.
- **INT-04 Fixed (Shared ChangeTracker Contamination on Handler Failure)**:
  - When a handler threw an exception in `OutboxProcessorService.ProcessBatchAsync` or `BackgroundJobService.ProcessPendingJobsAsync`, EF Core retained newly added/modified domain entities staged by the failing handler inside `_context.ChangeTracker`. Subsequent `SaveChangesAsync()` to persist retry metadata re-failed or polluted the batch.
  - An initial fix detaching everything except the single failing message accidentally detached sibling batch messages claimed in `claimed`, stranding them in `InProgress` (caught by `SqlServerHardeningIntegrationTests`).
  - Final fix builds `var claimedSet = new HashSet<object>(claimed, ReferenceEqualityComparer.Instance);` and detaches **only** entries where `!claimedSet.Contains(entry.Entity)` before saving retry state. Verified on SQL Server LocalDB that failing message retries cleanly while subsequent messages in the same batch publish cleanly.
- **Notification Delivery**: `NotificationDeliveryService.ProcessPendingDeliveriesAsync` evaluates backoff outside transactions and never holds DB locks while calling `IEmailProvider.SendAsync`.
