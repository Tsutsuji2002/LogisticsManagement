# Operational Reports Specification

All operational reports are served through `IOperationalReportService` via `GET /api/v1/reports/*` with server-side branch scoping, pagination (`Page`, `PageSize`), sorting, and optional filters (`StartDate`, `EndDate`, `TimeZoneId`, `BranchId`, `CustomerId`, `EmployeeId`, `WarehouseId`, `CarrierId`, `CarrierServiceId`, `Status`, `Search`).

| Report Endpoint | Permission | Row DTO | Key Columns & Behavior |
|---|---|---|---|
| `GET /api/v1/reports/orders` | `reports.orders.read` | `OrderReportRowDto` | Order #, OrderDate (UTC), Status, PaymentStatus, BranchCode, CustomerName, ItemsCount, PackagesCount, Weight, DeclaredValue, CodAmount. |
| `GET /api/v1/reports/shipments` | `reports.shipments.read` | `ShipmentReportRowDto` | Tracking #, linked Order #, Status, ServiceType, Origin & Destination Branch codes, CarrierCode, ChargeableWeightKg, CodAmount, TotalAmount, Pickup/Estimated/Actual Delivery timestamps. |
| `GET /api/v1/reports/delivery-performance` | `reports.delivery.read` | `DeliveryPerformanceRowDto` | Attempt count per shipment, Pickup/SLA/Actual timestamps, computed `DeliveryDurationHours`, and `IsOnTime` boolean (`null` when no SLA). |
| `GET /api/v1/reports/carrier-performance` | `reports.carrier.read` | `CarrierPerformanceRowDto` | Carrier-level rollup combining shipment outcomes (`Total`, `Delivered`, `FailedOrReturned`) and integration telemetry (`CarrierApiLogs`, `CarrierWebhookLogs`, `CarrierSyncJobs`, `CarrierTrackingEvents`). |
