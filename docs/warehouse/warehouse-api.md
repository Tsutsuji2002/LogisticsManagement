# Warehouse & Inventory Management — API Reference

## 1. Overview
The Warehouse & Inventory Management Module exposes RESTful API endpoints for:
1. Physical facility structure (Warehouses, Zones, Bin Locations).
2. Physical inventory tracking (Items, Balances, Transactions, Receipts, Adjustments, Transfers, Reservations, Lots, Serials).
3. Parcel handling and sorting operations (Scanning, Receiving, Sorting, Staging, Exception Logging, Dispatches).

Base URL: `/api/v1`  
Authentication: Bearer JWT  
Authorization: Permission and Branch-access scoped (`ICurrentUserService`).

---

## 2. Warehouses & Locations Endpoints (`/api/v1/warehouses`)

| Method | Endpoint | Permission | Description |
|---|---|---|---|
| `GET` | `/api/v1/warehouses` | `Warehouses.Read` | List accessible warehouses (supports filtering by `branchId`, `type`, `isActive`). |
| `GET` | `/api/v1/warehouses/{id}` | `Warehouses.Read` | Get warehouse by ID. |
| `POST` | `/api/v1/warehouses` | `Warehouses.Manage` | Create a new warehouse facility (InventoryWarehouse or ParcelHub). |
| `PUT` | `/api/v1/warehouses/{id}` | `Warehouses.Manage` | Update warehouse properties. |
| `GET` | `/api/v1/warehouses/{warehouseId}/zones` | `Zones.Read` | List zones within a warehouse facility. |
| `POST` | `/api/v1/warehouses/{warehouseId}/zones` | `Zones.Manage` | Create a new zone (Storage, BulkStorage, ParcelSorting, StagingBay, Returns). |
| `GET` | `/api/v1/warehouses/{warehouseId}/locations` | `Locations.Read` | List bin/rack/shelf locations within a warehouse. |
| `POST` | `/api/v1/warehouses/{warehouseId}/zones/{zoneId}/locations` | `Locations.Manage` | Create a specific storage or staging location. |
| `PATCH` | `/api/v1/warehouses/{warehouseId}/locations/{locationId}/block?isBlocked={true/false}` | `Locations.Manage` | Lock or unlock a bin location (e.g., damaged rack or quarantine). |

---

## 3. Inventory Management Endpoints (`/api/v1/inventory`)

### Inventory Items & Balances
| Method | Endpoint | Permission | Description |
|---|---|---|---|
| `GET` | `/api/v1/inventory/items` | `Inventory.Read` | Query stocked product parameters (reorder point, safety stock, target stock). |
| `GET` | `/api/v1/inventory/items/{id}` | `Inventory.Read` | Get stocked inventory item definition by ID. |
| `POST` | `/api/v1/inventory/items` | `Inventory.Manage` | Define stocking rules for a product in a warehouse. |
| `GET` | `/api/v1/inventory/balances` | `Inventory.Read` | Query on-hand, allocated, and available balances by warehouse, location, product, or lot. |
| `GET` | `/api/v1/inventory/balances/{id}` | `Inventory.Read` | Get single inventory balance details. |
| `GET` | `/api/v1/inventory/transactions` | `Inventory.Read` | Audit ledger of all stock movements (Receipt, Issue, Adjustment, Transfer, Count). |

### Warehouse Receipts (Inbound Inventory)
| Method | Endpoint | Permission | Description |
|---|---|---|---|
| `GET` | `/api/v1/inventory/receipts` | `Receipts.Read` | List warehouse receipts (PurchaseOrder, Return, Transfer, Production). |
| `GET` | `/api/v1/inventory/receipts/{id}` | `Receipts.Read` | Get warehouse receipt details including items. |
| `POST` | `/api/v1/inventory/receipts` | `Receipts.Manage` | Create a draft inbound receipt header and items. |
| `POST` | `/api/v1/inventory/receipts/{id}/confirm` | `Receipts.Manage` | Confirm goods receipt: updates `InventoryBalances`, generates `InventoryTransaction` and sets status to `Completed`. |

### Stock Adjustments & Transfers
| Method | Endpoint | Permission | Description |
|---|---|---|---|
| `GET` | `/api/v1/inventory/adjustments` | `Adjustments.Read` | List cycle count and discrepancy stock adjustments. |
| `GET` | `/api/v1/inventory/adjustments/{id}` | `Adjustments.Read` | Get stock adjustment details. |
| `POST` | `/api/v1/inventory/adjustments` | `Adjustments.Manage` | Create a pending stock adjustment. |
| `POST` | `/api/v1/inventory/adjustments/{id}/approve?employeeId={id}` | `Adjustments.Manage` | Approve and apply stock adjustment, adjusting balances and logging transactions. |
| `GET` | `/api/v1/inventory/transfers` | `Transfers.Read` | List inter-warehouse stock transfers. |
| `GET` | `/api/v1/inventory/transfers/{id}` | `Transfers.Read` | Get stock transfer details. |
| `POST` | `/api/v1/inventory/transfers` | `Transfers.Manage` | Create a stock transfer between warehouses. |
| `POST` | `/api/v1/inventory/transfers/{id}/ship` | `Transfers.Manage` | Mark transfer shipped, deducting stock from source warehouse. |
| `POST` | `/api/v1/inventory/transfers/{id}/receive` | `Transfers.Manage` | Mark transfer received, adding stock to destination warehouse. |

### Stock Reservations, Lots & Serials
| Method | Endpoint | Permission | Description |
|---|---|---|---|
| `GET` | `/api/v1/inventory/reservations` | `Reservations.Read` | Query active or fulfilled order reservations. |
| `POST` | `/api/v1/inventory/reservations` | `Reservations.Manage` | Reserve available stock for a confirmed customer sales order. |
| `POST` | `/api/v1/inventory/reservations/{id}/release` | `Reservations.Manage` | Release allocated stock back to available pool. |
| `GET` | `/api/v1/inventory/lots` | `Lots.Read` | Query manufacturing batches, expiry dates, and lot numbers. |
| `POST` | `/api/v1/inventory/lots` | `Lots.Manage` | Register a new lot batch for a product. |
| `GET` | `/api/v1/inventory/serials` | `SerialNumbers.Read` | Query item serialized barcodes, status, and locations. |
| `POST` | `/api/v1/inventory/serials` | `SerialNumbers.Manage` | Register a unique serial number for high-value tracking. |

---

## 4. Parcel Handling & Sorting Endpoints (`/api/v1/parcel-handling`)

High-throughput scanning and operational hub processing endpoints:

| Method | Endpoint | Permission | Description |
|---|---|---|---|
| `POST` | `/api/v1/parcel-handling/scan` | `ParcelHandling.Read` | Fast barcode scan lookup (by package barcode, package code, or shipment tracking number). |
| `POST` | `/api/v1/parcel-handling/receive` | `ParcelHandling.Manage` | Record arrival and unloading at sorting hub; triggers shipment transition to `InSortingHub`. |
| `POST` | `/api/v1/parcel-handling/sort` | `ParcelHandling.Manage` | Assign scanned parcel to sorting lane or consolidation bag (`SortedToLane`, `Bagged`). |
| `POST` | `/api/v1/parcel-handling/stage` | `ParcelHandling.Manage` | Move package/bag to an outbound staging bay location (`StagedInBay`). |
| `POST` | `/api/v1/parcel-handling/exception` | `ParcelHandling.Manage` | Log handling exception (Damaged, Missing, Misrouted, LabelUnreadable). |
| `GET` | `/api/v1/parcel-handling/events` | `ParcelHandling.Read` | Audit log of physical handling events for a package, shipment, or warehouse. |

---

## 5. Warehouse Dispatches (`/api/v1/warehouse-dispatches`)

Manage outbound vehicle and container manifests:

| Method | Endpoint | Permission | Description |
|---|---|---|---|
| `GET` | `/api/v1/warehouse-dispatches` | `Dispatches.Read` | List warehouse dispatches (Transfer, CustomerDelivery, LineHaul). |
| `GET` | `/api/v1/warehouse-dispatches/{id}` | `Dispatches.Read` | Get dispatch manifest details with inventory items and parcel packages. |
| `POST` | `/api/v1/warehouse-dispatches` | `Dispatches.Manage` | Create a draft dispatch manifest. |
| `POST` | `/api/v1/warehouse-dispatches/{id}/add-package` | `Dispatches.Manage` | Scan and add a package to the outbound manifest. |
| `POST` | `/api/v1/warehouse-dispatches/{id}/confirm` | `Dispatches.Manage` | Finalize dispatch, record departure, and transition shipments to `InTransit`. |
