# Desktop Module & Screen Inventory

All 14 workspace controls reside under `src\Client\Logistics.Client.WinForms\Controls\`:

1. **Executive Dashboard** ([ExecutiveDashboardControl.cs](file:///c:/Projects/LogisticsManagement/src/Client/Logistics.Client.WinForms/Controls/Dashboard/ExecutiveDashboardControl.cs))
   - Scope selector across 8 live backend dashboards (`CompanyOverview`, `Branch`, `Operations`, `DeliveryPerformance`, `Finance`, `COD`, `Warehouse`, `Inventory`), KPI cards, and custom GDI bar charts.
2. **Organization & Security** ([OrganizationWorkspaceControl.cs](file:///c:/Projects/LogisticsManagement/src/Client/Logistics.Client.WinForms/Controls/Organization/OrganizationWorkspaceControl.cs))
   - Company profile & overview metrics, Branches CRUD + toggle, Departments CRUD + toggle, Employees CRUD + status + account creation, Users & Roles admin management + admin password reset.
3. **CRM — Customers** ([CrmWorkspaceControl.cs](file:///c:/Projects/LogisticsManagement/src/Client/Logistics.Client.WinForms/Controls/CRM/CrmWorkspaceControl.cs))
   - Customer list + split detail workspace for Contacts, Addresses, Contracts, and Pricing Rules.
4. **Catalog** ([CatalogWorkspaceControl.cs](file:///c:/Projects/LogisticsManagement/src/Client/Logistics.Client.WinForms/Controls/Catalog/CatalogWorkspaceControl.cs))
   - Products (SKU), Categories, Units of Measure CRUD/toggle, and read-only Services reference view.
5. **Sales — Orders** ([SalesOrdersWorkspaceControl.cs](file:///c:/Projects/LogisticsManagement/src/Client/Logistics.Client.WinForms/Controls/Sales/SalesOrdersWorkspaceControl.cs))
   - Order lifecycle management (`Draft -> Confirmed -> Cancel`), detail inspection (OrderItems, OrderPackages, OrderAddresses), and quick item addition.
6. **Shipping & Delivery** ([ShippingWorkspaceControl.cs](file:///c:/Projects/LogisticsManagement/src/Client/Logistics.Client.WinForms/Controls/Shipping/ShippingWorkspaceControl.cs))
   - Shipments created from confirmed orders, status transitions, employee assignments, delivery attempt logging, Proof of Delivery metadata, Pickup Requests lifecycle, and Return Requests workflow.
7. **Carrier Integration (3PL)** ([CarrierWorkspaceControl.cs](file:///c:/Projects/LogisticsManagement/src/Client/Logistics.Client.WinForms/Controls/Carrier/CarrierWorkspaceControl.cs))
   - Carriers CRUD, Services & masked Configurations/Secrets management, Live Rate Query / Tracking / Booking / Cancellation, and API / Webhook / Sync Job logs.
8. **Warehouse & Parcel Hub** ([WarehouseWorkspaceControl.cs](file:///c:/Projects/LogisticsManagement/src/Client/Logistics.Client.WinForms/Controls/Warehouse/WarehouseWorkspaceControl.cs))
   - Warehouse Facilities / Zones / Locations CRUD + block toggles; separated Parcel Hub (`Scan`, `Receive`, `Sort`, `Stage`, `Exception`, `ParcelHandlingEvents`); Warehouse Dispatches create/confirm.
9. **Inventory Management** ([InventoryWorkspaceControl.cs](file:///c:/Projects/LogisticsManagement/src/Client/Logistics.Client.WinForms/Controls/Inventory/InventoryWorkspaceControl.cs))
   - Authoritative read-only `InventoryBalances` & `InventoryTransactions`, `InventoryItems` reorder thresholds, `WarehouseReceipts` create/confirm, `StockAdjustments` create/approve, `StockTransfers` create/confirm/receive, `InventoryReservations` create/release/consume, plus `InventoryLots` & `InventorySerialNumbers`.
10. **Finance & Billing** ([FinanceWorkspaceControl.cs](file:///c:/Projects/LogisticsManagement/src/Client/Logistics.Client.WinForms/Controls/Finance/FinanceWorkspaceControl.cs))
    - Rate Cards & Rate Rules CRUD, Server Shipping Fee Calculator, Shipment Charges & adjustments, Invoices CRUD/Issue/Cancel + Payments Register/Allocate, Authoritative Customer Balances + Reconciliation + Financial Ledger.
11. **COD Management** ([CodWorkspaceControl.cs](file:///c:/Projects/LogisticsManagement/src/Client/Logistics.Client.WinForms/Controls/COD/CodWorkspaceControl.cs))
    - COD Collections (`Pending -> CollectedByDriver -> DepositedToBranch -> VerifiedByCashier -> RemittedToCustomer` / Failure), COD Remittances + items + confirm/cancel, Employee & Branch COD Reconciliation.
12. **Notifications** ([NotificationsWorkspaceControl.cs](file:///c:/Projects/LogisticsManagement/src/Client/Logistics.Client.WinForms/Controls/Notifications/NotificationsWorkspaceControl.cs))
    - My Notifications / System-wide view, unread filter, Mark Read / Mark All Read, Send Notification, and Notification Templates CRUD.
13. **Reports & Background Processing** ([ReportsWorkspaceControl.cs](file:///c:/Projects/LogisticsManagement/src/Client/Logistics.Client.WinForms/Controls/Reports/ReportsWorkspaceControl.cs))
    - Explorer across all 16 reporting endpoints with date/branch/search filters + server-backed **CSV Export**, Scheduled Reports CRUD/manual run, Background Worker health banner, Background Jobs retry, and Outbox Messages retry.
14. **Settings & Security** ([SettingsWorkspaceControl.cs](file:///c:/Projects/LogisticsManagement/src/Client/Logistics.Client.WinForms/Controls/Settings/SettingsWorkspaceControl.cs))
    - Profile summary, Change Password, `/auth/me` sync, Active Sessions termination, API Environment/URL/Language configuration, and local Log Folder shortcut.
