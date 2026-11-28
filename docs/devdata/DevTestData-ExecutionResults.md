# Dev Test Data Execution Results

## 1. Build & Automated Test Suite Results

Executed after the final code edit on branch `develop`:

- **Solution Build** (`dotnet build LogisticsManagement.sln --no-restore`): **0 Warnings, 0 Errors** (all 13 projects succeeded).
- **Client Unit Tests** (`Logistics.Client.UnitTests`): **17 / 17 Passed** (0 failed, 0 skipped).
- **Server Unit Tests** (`Logistics.Server.UnitTests`): **253 / 253 Passed** (0 failed, 0 skipped).
- **Server Integration Tests** (`Logistics.Server.IntegrationTests`, InMemory suite including `DevTestDataIntegrationTests`): **116 / 116 Passed** (0 failed, 0 skipped).
  - Covers `Production` environment `403` guard, `DevSeed:Enabled=false` `403` guard, non-admin RBAC `403` guard, `ConfirmReset=false` `400` validation, `FullSeed`, `Incremental`, `VerifyIntegrity`, and `ResetAndReseed`.

## 2. Live SQL Server Execution Results (`Standard` Scale)

Executed against `LAPTOP-S340\SQLEXPRESS / LogisticsManagement` via `POST /api/v1/dev/test-data/execute` (`Mode = ResetAndReseed`, `Scale = Standard`, `RandomSeed = 42`):

- **Execution Duration**: `34,068 ms` (inside single atomic SQL transaction)
- **Integrity Report**: `AllPassed = True` (8/8 checks passed)

### Populated Table Counts Observed

| Module | Table | Count | Standard Target | Status |
|---|---|---:|---:|---|
| Organization | `Companies` | 1 | 1 | Populated |
| Organization | `Branches` | 4 | 4 | Populated |
| Organization | `Departments` | 12 | 12 | Populated |
| Organization | `Employees` | 36 | 36 | Populated |
| Organization | `Warehouses` | 6 | 6 | Populated |
| Identity | `UserAccounts` | 28 | 25 | Populated |
| CRM | `Customers` | 120 | 120 | Populated |
| CRM | `CustomerContacts` | 198 | 150 | Populated |
| CRM | `CustomerAddresses` | 240 | 240 | Populated |
| CRM | `CustomerContracts` | 55 | 40 | Populated |
| CRM | `CustomerPricingRules` | 55 | 40 | Populated |
| Catalog | `ProductCategories` | 10 | 10 | Populated |
| Catalog | `Products` | 55 | 55 | Populated |
| Sales | `Orders` | 600 | 600 | Populated |
| Sales | `OrderItems` | 1,194 | 1,000 | Populated |
| Sales | `OrderPackages` | 600 | 600 | Populated |
| Shipping | `Shipments` | 511 | 500 | Populated |
| Shipping | `ShipmentStatusHistories` | 3,026 | 2,000 | Populated |
| Shipping | `PickupRequests` | 120 | 120 | Populated |
| Shipping | `DeliveryAttempts` | 480 | 350 | Populated |
| Shipping | `ProofOfDeliveries` | 435 | 300 | Populated |
| Shipping | `ReturnRequests` | 25 | 20 | Populated |
| Carrier | `Carriers` | 4 | 4 | Populated |
| Carrier | `CarrierTrackingEvents` | 472 | 300 | Populated |
| Warehouse | `WarehouseLocations` | 96 | 96 | Populated |
| Warehouse | `WarehouseReceipts` | 210 | 210 | Populated |
| Warehouse | `WarehouseDispatches` | 155 | 155 | Populated |
| Warehouse | `ParcelHandlingEvents` | 1,320 | 1,000 | Populated |
| Inventory | `InventoryItems` | 120 | 100 | Populated |
| Inventory | `InventoryBalances` | 120 | 100 | Populated |
| Inventory | `InventoryTransactions` | 120 | 100 | Populated |
| Inventory | `StockAdjustments` | 24 | 24 | Populated |
| Finance | `Invoices` | 261 | 80 | Populated |
| Finance | `Payments` | 183 | 60 | Populated |
| Finance | `CustomerBalances` | 115 | 60 | Populated |
| COD | `CODCollections` | 173 | 120 | Populated |
| COD | `CODRemittances` | 60 | 30 | Populated |
| Notifications | `Notifications` | 110 | 110 | Populated |
| Audit | `AuditLogs` | 81 | 80 | Populated |
