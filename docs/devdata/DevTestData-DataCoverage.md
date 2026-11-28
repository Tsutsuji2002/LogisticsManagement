# Dev Test Data Coverage Matrix

Source of truth: `ApplicationDbContext` (`src/Server/Logistics.Server.Infrastructure/Persistence/ApplicationDbContext.cs`, 90 DbSets across 14 schemas).

## Data Coverage Matrix

| Module | Table / Entity | Previous Seed Count | Target (Standard) | Target (Large) | Generation Method | Dependencies | Status |
|---|---|---:|---:|---:|---|---|---|
| Identity | `Permissions`, `Roles`, `RolePermissions` | Full | Full | Full | `IdentityDataSeeder` | None | Covered |
| Identity | `UserAccounts`, `UserRoles`, `UserBranchAccesses` | 8 | 27 | 27 | `DevDataSeeder` (`SeedEmployeesAndUsersAsync`) | Roles, Branches, Employees | Enhanced |
| Identity | `LoginHistories` | 0 | 26 | 26 | `DevDataSeeder` | UserAccounts | Covered |
| Configuration | `Countries`, `Provinces`, `Districts`, `Wards` | 1 / 3 / 0 / 0 | 1 / 5 / 25 / 50 | 1 / 5 / 25 / 50 | `SeedGeographyAsync` | None | Enhanced |
| Configuration | `Currencies`, `UnitsOfMeasure`, `SystemSettings`, `NumberSequences` | 2 / 6 / 0 / 13 | 2 / 6 / 5+ / 13 | 2 / 6 / 5+ / 13 | `SeedCurrenciesAsync`, `SeedSystemSettingsAsync`, `UpdateNumberSequencesToMaxAsync` | None | Enhanced |
| Organization | `Companies`, `Branches`, `Departments`, `Warehouses` | 1 / 2 / 6 / 2 | 1 / 4 / 12 / 6 | 1 / 4 / 12 / 6 | `SeedCompanyBranchesAndWarehousesAsync`, `SeedDepartmentsAsync` | Geography | Enhanced |
| Organization | `Employees` | 7 | 36 | 36 | `SeedEmployeesAndUsersAsync` | Branches, Departments | Enhanced |
| CRM | `Customers` | 5 | 120 | 200 | `SeedCustomersAsync` | Branches, Employees | Enhanced |
| CRM | `CustomerContacts`, `CustomerAddresses`, `Addresses` | 0 / 0 / 0 | 198 / 240 / 250 | 330 / 400 / 410 | `SeedCustomersAsync` | Customers, Geography | Covered |
| CRM | `CustomerContracts`, `CustomerPricingRules` | 0 / 0 | 55 / 55 | 55 / 55 | `SeedCustomersAsync` | Customers | Covered |
| Catalog | `ProductCategories`, `Products` | 5 / 12 | 10 / 55 | 10 / 55 | `SeedProductCatalogAsync` | UnitsOfMeasure, Currencies | Enhanced |
| Sales | `Orders`, `OrderItems`, `OrderAddresses`, `OrderPackages` | 0 | 600 / ~1200 / 1200 / 600 | 950 / ~1900 / 1900 / 950 | `SeedOrdersAndShipmentsAsync` | Customers, Branches, Products, Geography | Covered |
| Shipping | `Shipments`, `ShipmentPackages`, `ShipmentAddresses` | 0 | ~530 / ~530 / ~1060 | ~840 / ~840 / ~1680 | `SeedOrdersAndShipmentsAsync` | Orders, Customers, Branches, Carriers | Covered |
| Shipping | `ShipmentStatusHistories`, `ShipmentAssignments`, `ShipmentRoutes` | 0 | ~2400 / ~530 / ~180 | ~3800 / ~840 / ~280 | `SeedOrdersAndShipmentsAsync` | Shipments, Employees, Warehouses | Covered |
| Shipping | `PickupRequests`, `DeliveryAttempts`, `ProofOfDeliveries`, `ReturnRequests` | 0 | 120 / ~410 / ~380 / ~25 | 180 / ~650 / ~600 / ~38 | `SeedOrdersAndShipmentsAsync`, `SeedPickupRequestsAsync` | Customers, Shipments, Employees | Covered |
| Carrier | `Carriers`, `CarrierServices`, `CarrierConfigurations`, `CarrierTrackingMappings` | 3 / 4 / 0 / 0 | 4 / 8 / 4 / 24 | 4 / 8 / 4 / 24 | `SeedCarriersAsync` | None | Enhanced |
| Carrier | `CarrierTrackingEvents`, `CarrierApiLogs`, `CarrierWebhookLogs`, `CarrierSyncJobs` | 0 | ~450 / 120 / 120 / 24 | ~500 / 120 / 120 / 24 | `SeedCarrierTelemetryAsync` | Carriers, Shipments | Covered |
| Warehouse | `WarehouseZones`, `WarehouseLocations` | 6 / 8 | 18 / 96 | 18 / 96 | `SeedWarehouseZonesAndLocationsAsync` | Warehouses | Enhanced |
| Warehouse | `WarehouseReceipts`, `WarehouseReceiptItems` | 0 | 210 / ~520 | 350 / ~875 | `SeedWarehouseOperationsAsync` | Warehouses, Products, Employees | Covered |
| Warehouse | `WarehouseDispatches`, `WarehouseDispatchItems`, `WarehouseDispatchPackages` | 0 | 155 / ~310 / 155 | 250 / ~500 / 250 | `SeedWarehouseOperationsAsync` | Warehouses, Products, ShipmentPackages | Covered |
| Warehouse | `ParcelHandlingEvents` | 0 | ~1320 | ~1320 | `SeedWarehouseOperationsAsync` | ShipmentPackages, Warehouses, Employees | Covered |
| Inventory | `InventoryItems`, `InventoryBalances`, `InventoryLots`, `InventorySerialNumbers` | 0 | 120 / 120 / 30 / 20 | 220 / 220 / 30 / 20 | `SeedInventoryDataAsync` | Products, Warehouses, WarehouseLocations | Covered |
| Inventory | `InventoryTransactions`, `InventoryTransactionItems`, `StockAdjustments`, `StockTransfers`, `InventoryReservations` | 0 | 120 / 120 / 24 / 20 / 25 | 220 / 220 / 24 / 20 / 25 | `SeedInventoryDataAsync` | Warehouses, Receipts, Orders | Covered |
| Finance | `ShippingRateCards`, `ShippingRateRules` | 1 / 4 | 2 / 7 | 2 / 7 | `SeedShippingRateCardAsync` | Currencies | Enhanced |
| Finance | `ShipmentCharges`, `Invoices`, `InvoiceItems`, `Payments`, `PaymentAllocations`, `CustomerBalances`, `FinancialLedgerEntries` | 0 | ~900 / ~85 / ~700 / ~65 / ~65 / ~110 / ~150 | ~1400 / ~130 / ~1100 / ~100 / ~100 / ~180 / ~230 | `SeedFinanceDataAsync` | Shipments, Customers, Currencies | Covered |
| COD | `CODCollections`, `CODRemittances`, `CODRemittanceItems` | 0 | ~150 / ~35 / ~110 | ~240 / ~50 / ~175 | `SeedCodDataAsync` | Delivered COD Shipments, Employees | Covered |
| Notification | `NotificationTemplates`, `Notifications`, `NotificationDeliveries`, `ReportDefinitions`, `ScheduledReports` | 3 / 0 / 0 / 0 / 0 | 6 / 110 / 110 / 5 / 3 | 6 / 180 / 180 / 5 / 3 | `SeedNotificationTemplatesAndReportsAsync`, `SeedNotificationsJobsAndAuditAsync` | UserAccounts, Branches | Enhanced |
| Integration & Audit | `BackgroundJobLogs`, `OutboxMessages`, `AuditLogs` | 0 | 40 / 50 / 80 | 40 / 50 / 80 | `SeedNotificationsJobsAndAuditAsync` | UserAccounts | Covered |
