# Database Indexing Strategy

**Engine:** SQL Server 2022+  
**Design Principle:** Target high-throughput transactional queries, barcode scanning operations, status lookups, and financial reconciliations.

---

## 1. Indexing Philosophy
1. **Clustered Indexes on Surrogate Primary Keys (`Id`)**:
   - `BIGINT IDENTITY(1,1)` clustered primary keys guarantee strict sequential page insertion, zero page splits during normal inserts, compact B-tree non-leaf levels, and optimal join speed.
2. **Unique Nonclustered Indexes on Business Keys**:
   - Every human-facing code (e.g., `TrackingNumber`, `OrderNumber`, `CustomerCode`, `InvoiceNumber`, `Barcode`, `PublicId`) has a unique nonclustered index to enforce business uniqueness and provide O(log N) lookup.
3. **Idempotency Composite Indexes**:
   - Prevents duplicate webhook event processing and duplicate carrier tracking entries (`(CarrierId, ExternalEventId)`).
4. **Foreign Key Indexing**:
   - All high-frequency foreign keys used in parent-child navigation and join operations are indexed to eliminate table scans during joins and cascade validation checks.
5. **Filtered Nonclustered Indexes**:
   - Indexes on active records, unbilled charges, or pending outbox messages utilize `WHERE IsActive = 1` or `WHERE Status = 'Pending'` to minimize index size and memory footprint.

---

## 2. Table-by-Table Key Index Inventory

| Table | Index Name | Columns | Type | Purpose |
|:---|:---|:---|:---|:---|
| `organization.Companies` | `IX_Companies_PublicId` | `PublicId` | Unique Nonclustered | External API lookups. |
| `organization.Companies` | `IX_Companies_Code` | `Code` | Unique Nonclustered | Unique company code. |
| `organization.Companies` | `IX_Companies_TaxNumber` | `TaxNumber` | Unique Nonclustered | Tax code uniqueness. |
| `organization.Branches` | `IX_Branches_Code` | `Code` | Unique Nonclustered | Unique branch code lookup. |
| `organization.Branches` | `IX_Branches_CompanyId` | `CompanyId` | Nonclustered | Hierarchy traversal. |
| `organization.Departments` | `IX_Departments_BranchId_Code` | `BranchId, Code` | Unique Nonclustered | Scoped department code. |
| `organization.Employees` | `IX_Employees_Code` | `Code` | Unique Nonclustered | Employee staff code lookup. |
| `organization.Employees` | `IX_Employees_Email` | `Email` | Unique Nonclustered | Corporate email login / search. |
| `organization.Employees` | `IX_Employees_BranchId` | `BranchId` | Nonclustered | Filter employees by branch. |
| `organization.Warehouses` | `IX_Warehouses_Code` | `Code` | Unique Nonclustered | Facility code lookup. |
| `organization.Warehouses` | `IX_Warehouses_BranchId` | `BranchId` | Nonclustered | Branch facility association. |
| `identity.UserAccounts` | `IX_UserAccounts_Username` | `Username` | Unique Nonclustered | Login lookup (hot path). |
| `identity.UserAccounts` | `IX_UserAccounts_Email` | `Email` | Unique Nonclustered | Email recovery / login. |
| `identity.UserAccounts` | `IX_UserAccounts_EmployeeId` | `EmployeeId` | Unique Filtered (`WHERE EmployeeId IS NOT NULL`) | 1-to-1 employee link. |
| `identity.Roles` | `IX_Roles_Code` | `Code` | Unique Nonclustered | RBAC role evaluation. |
| `identity.Permissions` | `IX_Permissions_Code` | `Code` | Unique Nonclustered | Authorization middleware check. |
| `identity.UserRoles` | `IX_UserRoles_UserId_RoleId`| `UserId, RoleId` | Unique Nonclustered | User authorization evaluation. |
| `identity.RolePermissions` | `IX_RolePermissions_RoleId_PermissionId` | `RoleId, PermissionId` | Unique Nonclustered | Role authorization matrix. |
| `identity.UserBranchAccess`| `IX_UserBranchAccess_UserId_BranchId` | `UserId, BranchId` | Unique Nonclustered | Branch scoping enforcement. |
| `identity.RefreshTokens` | `IX_RefreshTokens_TokenHash` | `TokenHash` | Unique Nonclustered | JWT session renewal. |
| `identity.UserSessions` | `IX_UserSessions_SessionTokenHash` | `SessionTokenHash` | Unique Nonclustered | Desktop client session auth. |
| `identity.LoginHistories` | `IX_LoginHistories_UserId_AttemptedAt` | `UserId, AttemptedAt DESC` | Nonclustered | Security audit trail. |
| `crm.Customers` | `IX_Customers_CustomerCode` | `CustomerCode` | Unique Nonclustered | CRM customer search. |
| `crm.Customers` | `IX_Customers_BranchId` | `BranchId` | Nonclustered | Branch customer list. |
| `crm.CustomerContacts` | `IX_CustomerContacts_CustomerId` | `CustomerId` | Nonclustered | Client contact directory. |
| `crm.CustomerAddresses` | `IX_CustomerAddresses_CustomerId` | `CustomerId` | Nonclustered | Client address book. |
| `crm.CustomerContracts` | `IX_CustomerContracts_ContractNumber` | `ContractNumber` | Unique Nonclustered | Contract reference search. |
| `catalog.Products` | `IX_Products_Sku` | `Sku` | Unique Nonclustered | Product SKU search. |
| `catalog.Products` | `IX_Products_Barcode` | `Barcode` | Filtered Nonclustered (`WHERE Barcode IS NOT NULL`) | Barcode scanner lookup. |
| `sales.Orders` | `IX_Orders_OrderNumber` | `OrderNumber` | Unique Nonclustered | Order lookup (hot path). |
| `sales.Orders` | `IX_Orders_CustomerId_OrderDate` | `CustomerId, OrderDate DESC` | Nonclustered | Customer order history. |
| `sales.Orders` | `IX_Orders_OrderStatus` | `OrderStatus` | Nonclustered | Dispatch dashboard queue. |
| `sales.OrderItems` | `IX_OrderItems_OrderId` | `OrderId` | Nonclustered | Order detail retrieval. |
| `sales.OrderPackages` | `IX_OrderPackages_OrderId` | `OrderId` | Nonclustered | Package specification retrieval. |
| `shipping.Shipments` | `IX_Shipments_TrackingNumber`| `TrackingNumber` | Unique Nonclustered | Primary tracking search (extremely hot path). |
| `shipping.Shipments` | `IX_Shipments_CarrierTrackingNumber` | `CarrierTrackingNumber` | Filtered Nonclustered (`WHERE CarrierTrackingNumber IS NOT NULL`) | External carrier tracking sync. |
| `shipping.Shipments` | `IX_Shipments_OrderId` | `OrderId` | Nonclustered | Order fulfillment lookup. |
| `shipping.Shipments` | `IX_Shipments_CustomerId_CreatedAt` | `CustomerId, CreatedAt DESC` | Nonclustered | Customer shipment history. |
| `shipping.Shipments` | `IX_Shipments_ShipmentStatus` | `ShipmentStatus` | Nonclustered | Dispatcher control tower queues. |
| `shipping.ShipmentPackages`| `IX_ShipmentPackages_Barcode` | `Barcode` | Unique Nonclustered | Conveyor / handheld barcode scanning (hot path). |
| `shipping.ShipmentPackages`| `IX_ShipmentPackages_ShipmentId` | `ShipmentId` | Nonclustered | Consignment consolidation. |
| `shipping.ShipmentStatusHistories` | `IX_ShipmentStatusHistories_ShipmentId_ChangedAt` | `ShipmentId, ChangedAt DESC` | Nonclustered | Tracking timeline query. |
| `shipping.DeliveryAttempts`| `IX_DeliveryAttempts_ShipmentId_AttemptNumber` | `ShipmentId, AttemptNumber` | Unique Nonclustered | Redelivery tracking. |
| `shipping.ProofOfDeliveries`| `IX_ProofOfDeliveries_ShipmentId` | `ShipmentId` | Unique Nonclustered | POD verification. |
| `carrier.Carriers` | `IX_Carriers_Code` | `Code` | Unique Nonclustered | Carrier code lookup. |
| `carrier.CarrierTrackingEvents` | `IX_CarrierTrackingEvents_CarrierId_ExternalEventId` | `CarrierId, ExternalEventId` | Unique Nonclustered | Webhook idempotency guard. |
| `carrier.CarrierTrackingEvents` | `IX_CarrierTrackingEvents_ProcessedStatus` | `ProcessedStatus` | Filtered Nonclustered (`WHERE ProcessedStatus = 'Pending'`) | Background sync queue. |
| `warehouse.WarehouseLocations` | `IX_WarehouseLocations_Barcode` | `Barcode` | Unique Nonclustered | Warehouse bin scanning. |
| `warehouse.WarehouseReceipts` | `IX_WarehouseReceipts_ReceiptNumber` | `ReceiptNumber` | Unique Nonclustered | Inbound document lookup. |
| `warehouse.WarehouseDispatches` | `IX_WarehouseDispatches_DispatchNumber` | `DispatchNumber` | Unique Nonclustered | Outbound manifest lookup. |
| `warehouse.ParcelHandlingEvents` | `IX_ParcelHandlingEvents_ShipmentPackageId_ScannedAt` | `ShipmentPackageId, ScannedAt DESC` | Nonclustered | Parcel transit history. |
| `warehouse.ParcelHandlingEvents` | `IX_ParcelHandlingEvents_WarehouseId_ScannedAt` | `WarehouseId, ScannedAt DESC` | Nonclustered | Hub throughput analytics. |
| `inventory.InventoryBalances` | `IX_InventoryBalances_UniqueLocationProductLot` | `WarehouseId, WarehouseLocationId, ProductId, LotId` | Unique Nonclustered | Real-time stock reservation and deduction. |
| `inventory.InventoryTransactions` | `IX_InventoryTransactions_TransactionNumber` | `TransactionNumber` | Unique Nonclustered | Transaction audit trail. |
| `inventory.InventoryTransactions` | `IX_InventoryTransactions_WarehouseId_TransactionDate` | `WarehouseId, TransactionDate DESC` | Nonclustered | Facility stock audit. |
| `finance.Invoices` | `IX_Invoices_InvoiceNumber` | `InvoiceNumber` | Unique Nonclustered | Billing reference search. |
| `finance.Invoices` | `IX_Invoices_CustomerId_DueDate` | `CustomerId, DueDate` | Nonclustered | Aging accounts receivable. |
| `finance.Payments` | `IX_Payments_PaymentNumber` | `PaymentNumber` | Unique Nonclustered | Payment receipt search. |
| `finance.Payments` | `IX_Payments_CustomerId` | `CustomerId` | Nonclustered | Customer payment history. |
| `finance.CODCollections` | `IX_CODCollections_ShipmentId` | `ShipmentId` | Unique Nonclustered | COD collection verification. |
| `finance.CODRemittances` | `IX_CODRemittances_RemittanceNumber` | `RemittanceNumber` | Unique Nonclustered | Remittance disbursement search. |
| `finance.CODRemittanceItems` | `IX_CODRemittanceItems_CODCollectionId` | `CODCollectionId` | Unique Nonclustered | Prevents double-remittance. |
| `finance.FinancialLedgerEntries` | `IX_FinancialLedgerEntries_CustomerId_EntryDate` | `CustomerId, EntryDate DESC` | Nonclustered | Customer ledger statement. |
| `integration.OutboxMessages`| `IX_OutboxMessages_Status_CreatedAt` | `Status, CreatedAt` | Filtered Nonclustered (`WHERE Status = 'Pending'`) | High-speed polling of unsent domain events. |
| `audit.AuditLogs` | `IX_AuditLogs_TableName_PrimaryKey` | `TableName, PrimaryKey` | Nonclustered | Entity revision history audit. |
| `audit.FileAttachments` | `IX_FileAttachments_StoredFileName` | `StoredFileName` | Unique Nonclustered | Cloud blob pointer lookup. |
