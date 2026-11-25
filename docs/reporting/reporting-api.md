# Reporting & Dashboard API Reference

All endpoints wrap responses in `ApiResponse<T>` or `ApiResponse<PagedResponse<T>>` (except file downloads from `/api/v1/reports/export/{reportType}` which return `text/csv; charset=utf-8`).

## 1. Dashboard Endpoints (`DashboardController` — `/api/v1/dashboard`)

| Method & Path | Permission | Response Type |
|---|---|---|
| `GET /api/v1/dashboard/overview` | `dashboard.company.read` | `CompanyOverviewDashboardDto` |
| `GET /api/v1/dashboard/branches/{branchId}` | `dashboard.branch.read` (+ branch check) | `BranchDashboardDto` |
| `GET /api/v1/dashboard/operations` | `dashboard.read` | `OperationsDashboardDto` |
| `GET /api/v1/dashboard/shipments` | `reports.shipments.read` | `ShipmentDashboardDto` |
| `GET /api/v1/dashboard/delivery-performance` | `reports.delivery.read` | `DeliveryPerformanceDashboardDto` |
| `GET /api/v1/dashboard/orders` | `reports.orders.read` | `OrdersDashboardDto` |
| `GET /api/v1/dashboard/warehouse` | `reports.warehouse.read` | `WarehouseDashboardDto` |
| `GET /api/v1/dashboard/inventory` | `reports.inventory.read` | `InventoryDashboardDto` |
| `GET /api/v1/dashboard/finance` | `reports.finance.read` | `FinanceDashboardDto` |
| `GET /api/v1/dashboard/cod` | `reports.cod.read` | `CodDashboardDto` |

## 2. Detailed Report & Management Endpoints (`ReportsController` — `/api/v1/reports`)

| Method & Path | Permission | Description |
|---|---|---|
| `GET /api/v1/reports/orders` | `reports.orders.read` | Paginated orders report |
| `GET /api/v1/reports/shipments` | `reports.shipments.read` | Paginated shipments report |
| `GET /api/v1/reports/delivery-performance` | `reports.delivery.read` | Delivery attempts, duration, SLA compliance |
| `GET /api/v1/reports/carrier-performance` | `reports.carrier.read` | Carrier volume & API/webhook telemetry |
| `GET /api/v1/reports/warehouse-receipts` | `reports.warehouse.read` | Inbound receipts report |
| `GET /api/v1/reports/warehouse-dispatches` | `reports.warehouse.read` | Outbound dispatches report |
| `GET /api/v1/reports/parcel-handling` | `reports.warehouse.read` | Hub parcel scan history |
| `GET /api/v1/reports/inventory-balances` | `reports.inventory.read` | SKU stock balances (`?onlyLowStock=true`) |
| `GET /api/v1/reports/inventory-transactions` | `reports.inventory.read` | Inventory ledger transactions |
| `GET /api/v1/reports/shipping-charges` | `reports.finance.read` | Shipment charges (`?isInvoiced=false`) |
| `GET /api/v1/reports/invoices` | `reports.finance.read` | Invoices & computed outstanding balance |
| `GET /api/v1/reports/payments` | `reports.finance.read` | Confirmed payments & allocation breakdown |
| `GET /api/v1/reports/receivables` | `reports.finance.read` | Customer balances & credit limits |
| `GET /api/v1/reports/cod-collections` | `reports.cod.read` | COD collections & variance report |
| `GET /api/v1/reports/cod-remittances` | `reports.cod.read` | COD remittance batches report |
| `GET /api/v1/reports/cod-reconciliation` | `reports.cod.read` | Per-branch COD reconciliation summary |
| `GET /api/v1/reports/export/{reportType}` | `reports.export` + domain read permission | CSV download (`?format=csv`) |
| `GET /api/v1/reports/definitions` | `reports.schedule.manage` | List active report definitions |
| `GET /api/v1/reports/scheduled` | `reports.schedule.manage` | List scheduled report configurations |
| `GET /api/v1/reports/scheduled/{id}` | `reports.schedule.manage` | Get single scheduled report |
| `POST /api/v1/reports/scheduled` | `reports.schedule.manage` | Create scheduled report |
| `PUT /api/v1/reports/scheduled/{id}` | `reports.schedule.manage` | Update scheduled report |
| `DELETE /api/v1/reports/scheduled/{id}` | `reports.schedule.manage` | Deactivate scheduled report (`IsActive = false`) |
