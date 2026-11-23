# Inventory Business Rules & Constraints

## 1. Stock Quantity Invariants
1. **Non-Negative On-Hand**:
   `QuantityOnHand` in `InventoryBalances` must never drop below 0. Any operation (Issue, Adjustment, Transfer) that would produce a negative quantity is rejected with a `409 ConflictException`.
2. **Allocation Boundaries**:
   $$\text{QuantityAllocated} \ge 0$$
   $$\text{QuantityAllocated} \le \text{QuantityOnHand}$$
   $$\text{QuantityAvailable} = \text{QuantityOnHand} - \text{QuantityAllocated} \ge 0$$
   Allocating more than `QuantityAvailable` is immediately rejected.
3. **Damaged Stock Isolation**:
   `QuantityDamaged` is recorded separately and is excluded from both `QuantityAllocated` and `QuantityAvailable`.

---

## 2. Inventory Inbound & Outbound Rules

### 2.1 Inbound Receipts (`WarehouseReceipts`)
- Receipts require an existing physical bin location (`WarehouseLocationId`).
- Target locations must not be blocked (`IsBlocked == false`).
- Upon confirmation:
  - `QuantityOnHand` increases in target balance record.
  - An `InventoryTransaction` of type `Receipt` is written with unit declared values.
  - Receipt status changes to `Completed` and cannot be modified or re-confirmed.

### 2.2 Stock Adjustments (`StockAdjustments`)
- Adjustments are created in `Pending` status.
- Can represent positive discrepancies (found stock) or negative discrepancies (shrinkage, damaged write-off).
- If negative, the system checks whether `QuantityOnHand + QuantityChange >= 0`. If not, it fails.
- Applying an adjustment requires managerial approval (`Adjustments.Manage`).
- Upon approval, the adjustment modifies `QuantityOnHand` and logs an `InventoryTransaction` of type `Adjustment`.

### 2.3 Stock Transfers (`StockTransfers`)
- Transfer is initiated between two distinct warehouses: `SourceWarehouseId != DestinationWarehouseId`.
- **Ship Step**:
  - Validates source has sufficient `QuantityAvailable`.
  - Deducts `QuantityOnHand` from source location.
  - Logs `InventoryTransaction` of type `TransferOut`.
  - Status becomes `Shipped`.
- **Receive Step**:
  - Adds `QuantityOnHand` to destination warehouse target location.
  - Logs `InventoryTransaction` of type `TransferIn`.
  - Status becomes `Received`.

---

## 3. Physical Storage & Bin Location Rules
- Each bin location has `MaxWeightKg` and `MaxVolumeCbm`.
- Blocked locations (`IsBlocked == true`) reject new receipts and putaway operations.
- Pick face, bulk storage, and quarantine zones are partitioned logically via `WarehouseZones`.

---

## 4. Multi-Tenant Branch Scoping & Authorization
- Every `Warehouse` belongs to a `Branch` (`BranchId`).
- Users can only view or operate on warehouses where `ICurrentUserService.AccessibleBranchIds` contains `Warehouse.BranchId`.
- Global administrators (empty `AccessibleBranchIds`) have unrestricted cross-branch access.
- Cross-branch transfers enforce access to the source warehouse during shipment and destination warehouse during receipt.
