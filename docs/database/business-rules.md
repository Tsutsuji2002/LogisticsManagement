# Database Invariants and Business Rules

This document outlines the core business constraints, data integrity guarantees, and the demarcation between database-enforced and application-enforced rules.

---

## 1. Orders vs. Shipments (Strict Boundary)

- **Separation of Concerns**: An `Order` is a commercial agreement requesting transportation; a `Shipment` is the physical transit consignment.
- **Cardinality**: One `Order` may generate zero, one, or multiple `Shipments` (e.g., partial dispatch from separate warehouses, multi-part fulfillment).
- **Address Immutability**:
  - `sales.OrderAddresses` stores an immutable snapshot of sender and recipient details at the moment the order is confirmed.
  - `shipping.ShipmentAddresses` stores the exact operational address verified when dispatching the physical waybill.
  - Modifying `crm.Addresses` or customer default locations **never** mutates historical order or shipment records.
- **Cancellation Rule**:
  - An order can only be cancelled if none of its linked shipments have transitioned to `InTransit` or `Delivered`.
  - Once a shipment is physically received into a hub (`PickedUp` or `InSortingHub`), it must undergo a `ReturnRequest` (Reverse Logistics) rather than simple cancellation.

---

## 2. Parcel Hub Operations vs. Inventory Warehouse (Strict Boundary)

- **Operational Segregation**:
  - `warehouse.ParcelHandlingEvents` handles transit tracking scans (unload, sort, bag, load, hub-transfer) for `ShipmentPackages`.
  - `inventory.InventoryTransactions` and `inventory.InventoryBalances` govern physical stock of products (`catalog.Products`).
- **No Inadvertent Stock Adjustments**:
  - Scanning a parcel passing through a sorting hub **never** writes to `InventoryBalances` or creates an `InventoryTransaction`.
  - Inventory receipts (`WarehouseReceipts`) and dispatches (`WarehouseDispatches`) apply strictly to fulfillment inventory, not parcels in transit.

---

## 3. Inventory Balances and Transaction Reconciliation

- **Non-Negative Stock Invariant**:
  - `inventory.InventoryBalances.QuantityOnHand >= 0` enforced by check constraint.
  - `QuantityAllocated <= QuantityOnHand` enforced by application transaction boundary before issuing pick lists.
  - `QuantityAvailable` is a computed column: `QuantityOnHand - QuantityAllocated`.
- **Double-Entry Stock Accounting**:
  - Every modification to `InventoryBalances` must have a corresponding immutable record in `inventory.InventoryTransactions` and `inventory.InventoryTransactionItems`.
  - The sum of all item quantities in `InventoryTransactionItems` for a given `(WarehouseId, LocationId, ProductId, LotId)` must equal `QuantityOnHand`.
- **Inter-Warehouse Transfers**:
  - A `StockTransfer` moves stock out of `SourceWarehouseId` (creating a `TransferOut` transaction) into an in-transit virtual location, and into `DestinationWarehouseId` (creating a `TransferIn` transaction) only upon physical receipt.

---

## 4. Multi-Carrier Integration and Webhook Idempotency

- **Idempotent Webhook Ingestion**:
  - External tracking updates are guarded by `UNIQUE INDEX (CarrierId, ExternalEventId)`.
  - Any duplicate HTTP webhook delivered by external carrier networks is rejected or acknowledged without executing duplicate shipment status updates.
- **Sensitive Credential Protection**:
  - Carrier API tokens, passwords, and webhook secrets in `carrier.CarrierConfigurations` must be stored encrypted (AES-256).
  - Outbound logs in `carrier.CarrierApiLogs` and incoming headers in `carrier.CarrierWebhookLogs` must have authorization tokens masked prior to persistence.

---

## 5. Financial Integrity and COD Workflows

- **Monetary Precision**: All currency and fee values use `DECIMAL(19,4)` to prevent IEEE 754 floating-point inaccuracies.
- **Double COD Safeguard**:
  - `finance.CODCollections.ShipmentId` is unique; a shipment can only have one confirmed physical cash collection.
  - `finance.CODRemittanceItems.CODCollectionId` is unique; a collected COD amount can only be remitted to a customer once.
- **Payment Allocation Boundaries**:
  - An invoice's `PaidAmount` can never exceed `TotalAmount`.
  - The sum of `finance.PaymentAllocations.AllocatedAmount` for a given `PaymentId` can never exceed `finance.Payments.Amount`.
- **Double-Entry Customer Ledger**:
  - Every invoice issue adds a debit to `finance.FinancialLedgerEntries`.
  - Every received customer payment adds a credit.
  - Every COD collection and payout is tracked.
  - `finance.CustomerBalances` serves as an operational materialized balance verified against `FinancialLedgerEntries`.

---

## 6. Access Control and Branch Scoping

- **Branch Isolation**:
  - Users have an assigned set of branches in `identity.UserBranchAccess`.
  - The API layer applies an entity-level query filter verifying that `BranchId` on Orders, Shipments, and Warehouses matches the user's active branch authorization context.
- **Credential Segregation**:
  - Employee HR records in `organization.Employees` are separated from login credentials in `identity.UserAccounts`.
  - User accounts can be disabled (`IsActive = 0` or `IsLockedOut = 1`) immediately revoking active sessions without altering historical employee associations.

---

## 7. Outbox Pattern for Asynchronous Event Publishing

- **Transaction Invariant**:
  - When domain state changes (e.g., `Shipments` status updated to `Delivered`), the mutation and the corresponding `OutboxMessages` record are committed within the **exact same database transaction**.
  - A background worker reads `OutboxMessages WHERE Status = 'Pending'`, publishes to the message bus or notification pipeline, and marks `Status = 'Published'`, ensuring at-least-once delivery with zero data loss.
