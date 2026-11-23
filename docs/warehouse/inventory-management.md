# Inventory Management Architecture & Concepts

## 1. Domain Separation
In the internal logistics system, inventory and parcels serve fundamentally different purposes:

```
┌────────────────────────────────────────────────────────┐
│                   PHYSICAL LOGISTICS                   │
└───────────────┬────────────────────────┬───────────────┘
                │                        │
       [Physical Inventory]      [Customer Parcels]
                │                        │
  - Products & Raw Materials    - ShipmentPackages in Transit
  - Owned or Held by Org        - Owned by Customer
  - InventoryBalances table     - ParcelHandlingEvents table
  - Receipts, Txns, Adjustments - Scans, Sorting Lanes, Linehauls
  - Reorder points & Valuation  - Waybills & Tracking Numbers
```

> [!IMPORTANT]
> **Fundamental Principle**: Customer parcels (`ShipmentPackage`) traveling through hubs or cross-docks **NEVER** create or modify rows in `InventoryBalances`. Physical inventory balances strictly represent products cataloged in the `Products` table.

---

## 2. Inventory Core Components

### 2.1 Inventory Items (`InventoryItems`)
Configures stocking policy for a product within a specific warehouse:
- `ReorderPoint`: Minimum on-hand threshold triggering purchase/replenishment.
- `SafetyStock`: Buffer to prevent stockouts during lead times.
- `TargetStock`: Ideal stock level.
- `MinimumOrderQuantity` / `MaximumStock`: Order bounds.

### 2.2 Inventory Balances (`InventoryBalances`)
Maintains real-time stock levels broken down by `WarehouseId`, `WarehouseLocationId`, `ProductId`, and optional `LotId`:
- `QuantityOnHand`: Total physical quantity currently resting in the bin location.
- `QuantityAllocated`: Stock committed to confirmed sales orders that have not yet been picked/shipped.
- `QuantityAvailable`: Computed stock available for new commitments:
  $$\text{QuantityAvailable} = \text{QuantityOnHand} - \text{QuantityAllocated}$$
- `QuantityDamaged`: Physically isolated defective or broken units.

### 2.3 Double-Entry Inventory Ledger (`InventoryTransactions` & `InventoryTransactionItems`)
Every physical change in stock is recorded as an immutable transaction:
- Types: `Receipt`, `Issue`, `Adjustment`, `TransferIn`, `TransferOut`, `CycleCount`.
- Reference types: `WarehouseReceipt`, `StockAdjustment`, `StockTransfer`, `Order`.
- Details recorded per item: `Quantity`, `UnitCost`, `TotalCost`, `BalanceBefore`, `BalanceAfter`.
- Balance integrity: At any time, current balance equals initial balance plus cumulative signed transaction deltas.

### 2.4 Stock Reservations (`InventoryReservations`)
Prevents double-selling by allocating inventory to an approved order:
- `Status`: `Active`, `Fulfilled`, `Released`, `Expired`.
- Decreases `QuantityAvailable` by increasing `QuantityAllocated`.
- Auto-expires if the order is cancelled or times out.

### 2.5 Lot and Serial Tracking (`InventoryLots`, `InventorySerialNumbers`)
- **Lots**: Groups production runs with `ManufactureDate`, `ExpiryDate`, and vendor lot codes. Supports FEFO (First-Expired, First-Out).
- **Serial Numbers**: 1-to-1 tracking for unique serialized items. Records status (`InStock`, `Allocated`, `Dispatched`, `Returned`) and location.
