# Warehouse & Inventory Reports Specification

Strictly distinguishes **SKU Inventory Stock** (`InventoryBalances`, `InventoryItems`, `InventoryTransactions`, `InventoryReservations`, `InventoryLots`, `InventorySerialNumbers`) from **Transient Parcel Sorting / Transit Handling** (`ParcelHandlingEvents`, `WarehouseReceipts`, `WarehouseDispatches`).

## Dashboard Separation

- **`GET /api/v1/dashboard/warehouse`** (`reports.warehouse.read`):
  - Summarizes inbound `WarehouseReceipts`, outbound `WarehouseDispatches`, and parcel hub throughput (`ParcelsUnloadedAtHub`, `ParcelsSortedAtHub`, `ParcelsDispatchedFromHub`, `ParcelsAwaitingSorting`, `ParcelsAwaitingDispatch`) plus `ParcelEventsByType`.
- **`GET /api/v1/dashboard/inventory`** (`reports.inventory.read`):
  - Reports SKU snapshot (`DistinctProductsInStock`, `TotalQuantityOnHand`, `TotalQuantityAllocated`, `TotalQuantityAvailable = OnHand - Allocated`, `TotalQuantityDamaged`), active reservations (`ReservedUnitsTotal`), low-stock product/warehouse count (`QuantityAvailable <= ReorderPoint`), distinct lots, available serial numbers, and transaction breakdown.

## Detailed Endpoints

| Endpoint | Permission | Notes |
|---|---|---|
| `GET /api/v1/reports/warehouse-receipts` | `reports.warehouse.read` | Inbound receipts with line item counts, received quantities, and damaged quantities. |
| `GET /api/v1/reports/warehouse-dispatches` | `reports.warehouse.read` | Outbound dispatches with line item counts (`DispatchItems`) and parcel package counts (`DispatchPackages`). |
| `GET /api/v1/reports/parcel-handling` | `reports.warehouse.read` | Scan-level sorting hub events linking `TrackingNumber`, `PackageBarcode`, `EventType`, `EmployeeName`, and `ScannedAt`. |
| `GET /api/v1/reports/inventory-balances` | `reports.inventory.read` | SKU balance per warehouse/location/lot; supports `?onlyLowStock=true` comparing `QuantityOnHand - QuantityAllocated <= ReorderPoint`. |
| `GET /api/v1/reports/inventory-transactions` | `reports.inventory.read` | Ledger movements (`Receipt`, `Dispatch`, `TransferIn`, `TransferOut`, adjustments, cycle counts) with total moved quantities. |
