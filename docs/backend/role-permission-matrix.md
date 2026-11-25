# Default Role-Permission Matrix

## 1. Overview of System Roles

The Logistics Management System implements Role-Based Access Control (RBAC) backed by fine-grained, atomic permissions. The standard roles are defined in `AppRoles` and seeded idempotently via `IIdentityDataSeeder`:

| Role Code | Role Name | Scope / Responsibility |
| :--- | :--- | :--- |
| `SUPER_ADMIN` | System Administrator | Unrestricted system access across all modules and administrative functions. |
| `BRANCH_MANAGER` | Branch Manager | Complete management of an operating branch, its employees, inventory, and local shipments. |
| `OPERATIONS_MANAGER`| Operations Manager | High-level logistics dispatch, shipment assignment, carrier integration, and tracking operations. |
| `WAREHOUSE_STAFF` | Warehouse Staff | Inventory operations: receipts, dispatches, cycle counting, sorting, and stock movements. |
| `CUSTOMER_SERVICE` | Customer Service Staff | CRM operations, customer lookups, order placing, and shipment tracking inquiries. |
| `FINANCE_STAFF` | Finance Staff | Pricing rate cards, invoicing, customer billing, payment reconciliation, and COD collection. |
| `READ_ONLY` | Read-Only Staff | Read-only inspection access across operational modules for auditing and supervisory reviews. |

---

## 2. Default Permission Mapping Matrix

| Permission Code | Permission Description | SUPER_ADMIN | BRANCH_MGR | OPS_MGR | WAREHOUSE | CUST_SVC | FINANCE | READ_ONLY |
| :--- | :--- | :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| **Identity Module** | | | | | | | | |
| `identity.users.read` | View user accounts | ✓ | | | | | | |
| `identity.users.create` | Provision new users | ✓ | | | | | | |
| `identity.users.update` | Update user details | ✓ | | | | | | |
| `identity.users.delete` | Delete/deactivate users | ✓ | | | | | | |
| `identity.users.roles.manage` | Assign roles | ✓ | | | | | | |
| `identity.users.branches.manage` | Assign branch access | ✓ | | | | | | |
| `identity.users.reset-password` | Admin password reset | ✓ | | | | | | |
| `identity.roles.read` | View roles | ✓ | | | | | | |
| `identity.roles.manage` | Modify roles | ✓ | | | | | | |
| `identity.sessions.read` | View sessions | ✓ | | | | | | |
| `identity.sessions.manage` | Terminate sessions | ✓ | | | | | | |
| **Organization Module** | | | | | | | | |
| `organization.companies.read` | View companies | ✓ | | | | | | ✓ |
| `organization.companies.manage` | Manage companies | ✓ | | | | | | |
| `organization.branches.read` | View branches | ✓ | ✓ | ✓ | | | | ✓ |
| `organization.branches.manage` | Manage branches | ✓ | | | | | | |
| `organization.departments.read`| View departments | ✓ | ✓ | | | | | ✓ |
| `organization.departments.manage`| Manage departments | ✓ | | | | | | |
| `organization.employees.read` | View employees | ✓ | ✓ | | | | | ✓ |
| `organization.employees.manage` | Manage employees | ✓ | ✓ | | | | | |
| `organization.warehouses.read` | View warehouses | ✓ | ✓ | ✓ | | | | ✓ |
| `organization.warehouses.manage`| Manage warehouses | ✓ | | | | | | |
| **Configuration Module** | | | | | | | | |
| `configuration.settings.read` | View system settings | ✓ | | | | | | |
| `configuration.settings.manage`| Modify settings | ✓ | | | | | | |
| `configuration.reference.read` | View reference data | ✓ | | | | | | |
| `configuration.reference.manage`| Modify reference data | ✓ | | | | | | |
| **CRM Module** | | | | | | | | |
| `crm.customers.read` | View customers | ✓ | ✓ | | | ✓ | ✓ | ✓ |
| `crm.customers.create` | Create customers | ✓ | ✓ | | | ✓ | | |
| `crm.customers.update` | Update customers | ✓ | ✓ | | | ✓ | | |
| `crm.customers.delete` | Delete / deactivate customers | ✓ | | | | | | |
| `crm.customers.status.manage` | Activate/deactivate customers | ✓ | ✓ | | | | | |
| `crm.customers.overview.read` | View CRM overview metrics | ✓ | ✓ | | | ✓ | ✓ | ✓ |
| `crm.contacts.read` | View customer contacts | ✓ | ✓ | | | ✓ | | ✓ |
| `crm.contacts.create` | Create customer contacts | ✓ | ✓ | | | ✓ | | |
| `crm.contacts.update` | Update customer contacts | ✓ | ✓ | | | ✓ | | |
| `crm.contacts.manage` | Manage primary contact / status | ✓ | ✓ | | | ✓ | | |
| `crm.addresses.read` | View customer addresses | ✓ | ✓ | | | ✓ | | ✓ |
| `crm.addresses.create` | Add customer address | ✓ | ✓ | | | ✓ | | |
| `crm.addresses.update` | Update customer address | ✓ | ✓ | | | ✓ | | |
| `crm.addresses.manage` | Set default address / manage | ✓ | ✓ | | | ✓ | | |
| `crm.contracts.read` | View customer contracts | ✓ | ✓ | | | ✓ | ✓ | ✓ |
| `crm.contracts.create` | Create customer contracts | ✓ | ✓ | | | | ✓ | |
| `crm.contracts.update` | Update customer contracts | ✓ | ✓ | | | | ✓ | |
| `crm.contracts.manage` | Transition contract status | ✓ | ✓ | | | | ✓ | |
| `crm.pricing-rules.read` | View customer pricing rules | ✓ | ✓ | | | ✓ | ✓ | ✓ |
| `crm.pricing-rules.create` | Create customer pricing rules | ✓ | ✓ | | | | ✓ | |
| `crm.pricing-rules.update` | Update customer pricing rules | ✓ | ✓ | | | | ✓ | |
| `crm.pricing-rules.manage` | Manage pricing rule status | ✓ | ✓ | | | | ✓ | |
| **Catalog Module** | | | | | | | | |
| `catalog.categories.read` | View categories | ✓ | ✓ | | ✓ | ✓ | ✓ | ✓ |
| `catalog.categories.create` | Create categories | ✓ | ✓ | | | | | |
| `catalog.categories.update` | Update categories | ✓ | ✓ | | | | | |
| `catalog.categories.manage-status` | Manage category status | ✓ | ✓ | | | | | |
| `catalog.categories.delete` | Delete categories | ✓ | | | | | | |
| `catalog.products.read` | View products | ✓ | ✓ | | ✓ | ✓ | ✓ | ✓ |
| `catalog.products.create` | Create products | ✓ | ✓ | | | | | |
| `catalog.products.update` | Update products | ✓ | ✓ | | | | | |
| `catalog.products.manage-status` | Manage product status | ✓ | ✓ | | | | | |
| `catalog.products.delete` | Delete products | ✓ | | | | | | |
| `catalog.products.manage` | Manage products (legacy) | ✓ | | | | | | |
| `catalog.units.read` | View units of measure | ✓ | ✓ | | ✓ | ✓ | ✓ | ✓ |
| `catalog.units.manage` | Manage units of measure | ✓ | | | | | | |
| `catalog.services.read` | View logistics services | ✓ | ✓ | | | ✓ | ✓ | ✓ |
| **Sales Module** | | | | | | | | |
| `sales.orders.read` | View sales orders | ✓ | ✓ | | | ✓ | ✓ | ✓ |
| `sales.orders.create` | Create sales orders | ✓ | ✓ | | | ✓ | | |
| `sales.orders.update` | Edit sales orders | ✓ | ✓ | | | | | |
| `sales.orders.cancel` | Cancel sales orders | ✓ | | | | | | |
| **Shipping Module** | | | | | | | | |
| `shipping.shipments.read` | View shipments | ✓ | ✓ | ✓ | | ✓ | | ✓ |
| `shipping.shipments.create` | Create shipments | ✓ | ✓ | ✓ | | | | |
| `shipping.shipments.update` | Edit shipments | ✓ | ✓ | ✓ | | | | |
| `shipping.shipments.assign` | Assign couriers/routes | ✓ | ✓ | ✓ | | | | |
| `shipping.shipments.deliver` | Complete deliveries | ✓ | | ✓ | | | | |
| `shipping.shipments.cancel` | Cancel shipments | ✓ | ✓ | ✓ | | | | |
| `shipping.shipments.manage-status` | Execute status transitions | ✓ | ✓ | ✓ | | | | |
| `shipping.tracking.read` | View tracking history | ✓ | ✓ | ✓ | ✓ | ✓ | | ✓ |
| `shipping.pickups.read` | View pickup requests | ✓ | ✓ | ✓ | | ✓ | | ✓ |
| `shipping.pickups.create` | Create pickup requests | ✓ | ✓ | ✓ | | ✓ | | |
| `shipping.pickups.manage` | Manage pickup requests | ✓ | ✓ | ✓ | | | | |
| `shipping.returns.read` | View return requests | ✓ | ✓ | ✓ | | ✓ | | ✓ |
| `shipping.returns.manage` | Manage return requests | ✓ | ✓ | ✓ | | | | |
| `shipping.routes.read` | View shipment routes | ✓ | ✓ | ✓ | | | | |
| `shipping.routes.manage` | Manage shipment routes | ✓ | ✓ | ✓ | | | | |
| **Carrier Module** | | | | | | | | |
| `carrier.carriers.read` | View carriers | ✓ | ✓ | ✓ | | | | |
| `carrier.carriers.manage` | Configure carriers | ✓ | ✓ | ✓ | | | | |
| **Warehouse & Facilities Module** | | | | | | | | |
| `warehouse.warehouses.read` | View warehouses | ✓ | ✓ | ✓ | ✓ | | | ✓ |
| `warehouse.warehouses.manage` | Create/update warehouses | ✓ | ✓ | ✓ | | | | |
| `warehouse.zones.read` | View warehouse zones | ✓ | ✓ | ✓ | ✓ | | | ✓ |
| `warehouse.zones.manage` | Create/update zones | ✓ | ✓ | ✓ | | | | |
| `warehouse.locations.read` | View bin/rack locations | ✓ | ✓ | ✓ | ✓ | | | ✓ |
| `warehouse.locations.manage` | Manage & block locations | ✓ | ✓ | ✓ | ✓ | | | |
| `warehouse.operations.read` | View receipts/dispatch | ✓ | ✓ | ✓ | ✓ | | | ✓ |
| `warehouse.receipts.read` | View warehouse receipts | ✓ | ✓ | ✓ | ✓ | | | ✓ |
| `warehouse.receipts.manage` | Inbound receipt & putaway | ✓ | ✓ | | ✓ | | | |
| `warehouse.dispatches.read` | View dispatch manifests | ✓ | ✓ | ✓ | ✓ | | | ✓ |
| `warehouse.dispatches.manage` | Dispatch shipments & cargo | ✓ | ✓ | ✓ | ✓ | | | |
| `warehouse.sorting.manage` | Parcel hub sorting | ✓ | | ✓ | ✓ | | | |
| `warehouse.parcel-handling.read` | Fast scan & view events | ✓ | ✓ | ✓ | ✓ | | | ✓ |
| `warehouse.parcel-handling.manage` | Hub scan/sort/stage/exceptions | ✓ | | ✓ | ✓ | | | |
| **Inventory Module** | | | | | | | | |
| `inventory.stock.read` | View inventory levels & items | ✓ | ✓ | | ✓ | | | ✓ |
| `inventory.stock.manage` | Manage inventory stocking rules | ✓ | ✓ | | ✓ | | | |
| `inventory.adjustments.read` | View stock adjustments | ✓ | ✓ | | ✓ | | | ✓ |
| `inventory.adjustments.manage` | Create/approve adjustments | ✓ | ✓ | | ✓ | | | |
| `inventory.transfers.read` | View stock transfers | ✓ | ✓ | | ✓ | | | ✓ |
| `inventory.transfers.manage` | Create/ship/receive transfers | ✓ | ✓ | | ✓ | | | |
| `inventory.issues.manage` | Issue stock | ✓ | ✓ | | ✓ | | | |
| `inventory.reservations.read` | View stock reservations | ✓ | ✓ | | ✓ | | | ✓ |
| `inventory.reservations.manage` | Allocate & release stock | ✓ | ✓ | | ✓ | | | |
| `inventory.lots.read` | View lots & expiration | ✓ | ✓ | | ✓ | | | ✓ |
| `inventory.lots.manage` | Manage lots & batches | ✓ | ✓ | | ✓ | | | |
| `inventory.serials.read` | View serialized items | ✓ | ✓ | | ✓ | | | ✓ |
| `inventory.serials.manage` | Manage serial numbers | ✓ | ✓ | | ✓ | | | |
| **Finance Module** | | | | | | | | |
| `finance.rates.read` | View shipping rates & rules | ✓ | | | | | ✓ | ✓ |
| `finance.rates.manage` | Manage shipping rate cards & rules | ✓ | | | | | ✓ | |
| `finance.charges.read` | View shipment charges | ✓ | | | | | ✓ | ✓ |
| `finance.charges.calculate` | Calculate shipping fee breakdowns | ✓ | ✓ | ✓ | | ✓ | ✓ | ✓ |
| `finance.charges.adjust` | Add manual fee adjustments | ✓ | | | | | ✓ | |
| `finance.invoices.read` | View invoices & billing statements | ✓ | | | | | ✓ | ✓ |
| `finance.invoices.create` | Draft invoices & manage items | ✓ | | | | | ✓ | |
| `finance.invoices.issue` | Issue formal legal invoices | ✓ | | | | | ✓ | |
| `finance.invoices.cancel` | Cancel issued invoices | ✓ | | | | | ✓ | |
| `finance.payments.read` | View payments & allocations | ✓ | | | | | ✓ | ✓ |
| `finance.payments.manage` | Register payment receipts | ✓ | | | | | ✓ | |
| `finance.payments.allocate` | Allocate payments to invoices | ✓ | | | | | ✓ | |
| `finance.payments.reverse` | Reverse payment allocations | ✓ | | | | | ✓ | |
| `finance.balances.read` | View customer balances & exposure | ✓ | ✓ | | | ✓ | ✓ | ✓ |
| `finance.balances.reconcile` | Reconcile customer balances | ✓ | | | | | ✓ | |
| `finance.ledger.read` | View general ledger audit journal | ✓ | | | | | ✓ | ✓ |
| `finance.cod.manage` | Manage COD collections & remittance | ✓ | | | | | ✓ | |
| **COD Module** | | | | | | | | |
| `cod.collections.read` | View COD collection records & status | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `cod.collections.create` | Initiate COD collection for shipment | ✓ | | | | | ✓ | |
| `cod.collections.collect` | Record field cash collection by driver | ✓ | | | ✓ | | ✓ | |
| `cod.collections.verify` | Cashier verify cash and deposit in safe | ✓ | ✓ | | | | ✓ | |
| `cod.collections.manage` | Manage deposits & failed collections | ✓ | ✓ | ✓ | | | ✓ | |
| `cod.remittances.read` | View COD customer remittance batches | ✓ | ✓ | | | ✓ | ✓ | ✓ |
| `cod.remittances.create` | Create draft customer remittance batch | ✓ | | | | | ✓ | |
| `cod.remittances.confirm` | Confirm customer payout disbursement | ✓ | | | | | ✓ | |
| `cod.remittances.manage` | Add/remove items & cancel remittance | ✓ | | | | | ✓ | |
| `cod.reconciliation.read` | View COD reconciliation reports | ✓ | ✓ | | | | ✓ | ✓ |
| `cod.discrepancies.resolve` | Resolve cash discrepancies & shortages | ✓ | ✓ | | | | ✓ | |
| `cod.operations.read-own` | View own driver collections & holdings | ✓ | ✓ | ✓ | ✓ | | ✓ | |
| `cod.operations.read-branch` | View branch driver performance & summary | ✓ | ✓ | ✓ | | | ✓ | |
| **Notification Module** | | | | | | | | |
| `notification.notifications.read`| View notifications (legacy read) | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `notification.notifications.read-own` | Read own notifications & mark read | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `notification.notifications.manage` | Dispatch & view all/branch alerts | ✓ | | | | | | |
| `notification.templates.read` | View & preview notification templates | ✓ | ✓ | | | | | |
| `notification.templates.manage`| Create/update/toggle templates | ✓ | | | | | | |
| `notification.reports.read` | View report definitions & schedules | ✓ | ✓ | ✓ | | | ✓ | |
| `notification.reports.create` | Create branch scheduled reports | ✓ | ✓ | | | | | |
| `notification.reports.update` | Edit branch scheduled reports | ✓ | ✓ | | | | | |
| `notification.reports.manage` | Toggle & trigger scheduled reports| ✓ | | | | | | |
| **Background Processing** | | | | | | | | |
| `system.background-jobs.read` | View background jobs & failures | ✓ | | | | | | |
| `system.background-jobs.retry` | Retry failed background jobs | ✓ | | | | | | |
| `system.background-jobs.manage` | Cancel pending background jobs | ✓ | | | | | | |
| `system.background-processing.read` | View worker & outbox health | ✓ | | | | | | |
| `system.background-processing.manage` | Retry outbox messages & manage | ✓ | | | | | | |
| **Reporting & Dashboard Module** | | | | | | | | |
| `dashboard.read` | View operational dashboard KPIs | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `dashboard.company.read` | View company-wide executive overview | ✓ | | | | | | |
| `dashboard.branch.read` | View branch-specific dashboard | ✓ | ✓ | ✓ | | | ✓ | ✓ |
| `reports.orders.read` | View & query order reports | ✓ | ✓ | ✓ | | ✓ | | ✓ |
| `reports.shipments.read` | View & query shipment reports | ✓ | ✓ | ✓ | | ✓ | | ✓ |
| `reports.delivery.read` | View delivery & SLA performance | ✓ | ✓ | ✓ | | ✓ | | |
| `reports.warehouse.read` | View warehouse & hub throughput | ✓ | ✓ | ✓ | ✓ | | | |
| `reports.inventory.read` | View inventory & low-stock reports | ✓ | ✓ | ✓ | ✓ | | | |
| `reports.carrier.read` | View carrier telemetry & performance | ✓ | ✓ | ✓ | | | | |
| `reports.finance.read` | View financial & receivables reports | ✓ | | | | | ✓ | |
| `reports.cod.read` | View COD collection & reconciliation | ✓ | ✓ | ✓ | | | ✓ | |
| `reports.export` | Export permitted reports to CSV | ✓ | ✓ | ✓ | | | ✓ | |
| `reports.schedule.manage` | Manage report definitions & schedules | ✓ | | | | | | |
| **Audit Module** | | | | | | | | |
| `audit.logs.read` | View audit trail | ✓ | | | | | | |

---

## 3. Dynamic Permission Extensibility

When new business modules are added:
1. Define the permission code in `AppPermissions.{Module}`.
2. Add the metadata tuple to `AppPermissions.GetAllPermissions()`.
3. Add the mapping to `IdentityDataSeeder.SeedRolePermissionsAsync()`.
4. Apply `[HasPermission(AppPermissions.Module.Action)]` on the corresponding controller actions.
5. The seeder will automatically insert the missing permission on application startup without requiring database migration scripts.
