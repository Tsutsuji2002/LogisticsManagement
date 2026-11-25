# Transactional Outbox Pattern Specification

## 1. Delivery Guarantees & Transaction Boundary

The Outbox Pattern guarantees **at-least-once delivery** (never claiming impossible exactly-once execution over distributed network boundaries) combined with **idempotent event handlers**.

- **Atomic Commit**: Business services (`OrderService`, `ShipmentService`, `DeliveryAttemptService`, `ReturnRequestService`, `CarrierIntegrationServiceImpl`, `InventoryService`, `InvoiceService`, `PaymentService`, `CodCollectionService`, `CodRemittanceService`) stage `OutboxMessage` records via `IOutboxService.Enqueue(...)` inside the exact same EF Core `SaveChangesAsync()` transaction as the domain mutation.
- **No Orphaned Events on Rollback**: If a business validation fails or the database transaction rolls back, zero `OutboxMessages` are left behind.
- **No Long DB Locks**: Outbox messages are claimed quickly in small batches (`OutboxBatchSize = 50`), dispatched outside long transactions, and marked `Published` or scheduled for retry.

---

## 2. Versioned Event Contracts (`IntegrationEventTypes`)

All events serialize plain JSON contracts with explicit versioned string identifiers (`EventType`), avoiding fragile CLR assembly-qualified names:

| Domain | Event Type Identifier | Trigger Point |
| :--- | :--- | :--- |
| **Sales** | `sales.order.created.v1` | Order created |
| **Sales** | `sales.order.confirmed.v1` | Order confirmed |
| **Sales** | `sales.order.cancelled.v1` | Order cancelled |
| **Shipping** | `shipping.shipment.created.v1` | Shipment created from order |
| **Shipping** | `shipping.shipment.status-changed.v1` | Shipment status transition |
| **Shipping** | `shipping.delivery.completed.v1` | Delivery attempt succeeded |
| **Shipping** | `shipping.delivery.failed.v1` | Delivery attempt failed |
| **Shipping** | `shipping.return.initiated.v1` | Return request created |
| **Carrier** | `carrier.booking.outcome.v1` | External carrier booking result |
| **Carrier** | `carrier.sync.failed.v1` | Sync job encountered failures |
| **Warehouse** | `warehouse.receipt.completed.v1` | Inventory receipt finalized |
| **Finance** | `finance.invoice.issued.v1` | Draft invoice issued |
| **Finance** | `finance.payment.confirmed.v1` | Payment registered/confirmed |
| **COD** | `cod.collection.verified.v1` | Cashier verified COD cash |
| **COD** | `cod.remittance.confirmed.v1` | COD remittance payout confirmed |
| **COD** | `cod.discrepancy.detected.v1` | COD discrepancy alert |

---

## 3. Retry Policy, Dead-Lettering & Idempotency

1. **Exponential Backoff + Jitter**:
   - Failed dispatches increment `RetryCount` and set `NextRetryAt = UtcNow + 5s * 2^RetryCount + randomJitter(250..5000ms)`.
   - Maximum retries: `5` (`OutboxProcessorService.MaxRetryCount`). On the 5th failure, the message transitions to terminal `Status = "Failed"` for operator inspection/manual retry (`/api/v1/background-jobs/outbox/{id}/retry`).
2. **Unknown Event Types**:
   - Unknown `EventType` values are logged as warnings and marked `Published` with explanatory `ErrorMessage` (`No handler registered`) so unrecognized historical versions cannot stall queue throughput.
3. **Handler Idempotency (`EventKey`)**:
   - `OutboxNotificationHandlerBase<T>` assigns deterministic `EventKey` prefixes (`outbox:{MessageId}:{event-slug}`) mapped to a filtered unique index on `notification.Notifications(EventKey)`. Duplicate outbox deliveries safely resolve existing notification rows instead of duplicating alerts.
