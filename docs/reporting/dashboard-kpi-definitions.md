# Dashboard KPI Definitions

## 1. Authoritative Scope & Time Window Distinctions

Dashboards clearly separate **Lifetime / Live Snapshot KPIs** from **Date-Filtered Period KPIs**:

| Category | Examples | Date Window Applied? |
|---|---|---|
| Live Organization & Inventory Snapshot | `TotalActiveBranches`, `TotalActiveEmployees`, `TotalActiveCustomers`, `TotalQuantityOnHand`, `TotalQuantityAllocated`, `TotalQuantityAvailable`, `LowStockProductWarehouseCount` | No (current state) |
| Live Pipeline Snapshots | `ShipmentsCurrentlyInTransit`, `ShipmentsAwaitingPickup` (Company Overview), `InTransitActiveExcludedCount` (Delivery Performance) | Respects branch scope |
| Period Metrics & Trends | Orders, Shipments Created/Delivered, Delivery Rates, Warehouse Receipts/Dispatches, Charges, Invoices, Payments, COD Collections/Remittances | Yes (when `StartDate`/`EndDate` supplied) |

## 2. Core Domain Distinctions

1. **Orders vs Shipments vs Packages**:
   - An `Order` can be split across multiple `Shipment` records (`PartiallyShipped`), and a `Shipment` can exist without an `Order` (`OrderId == null`).
   - `OrdersDashboardDto` tracks `TotalOrders`, `TotalOrderItems`, and `TotalOrderPackages` separately from `ShipmentDashboardDto` (`TotalShipments`, `DistinctOrdersCount`, `TotalShipmentPackages`).
   - Because `Order` has no `ServiceType` column in the schema, `OrdersDashboardDto.ServiceBreakdownNote` documents that service-tier breakdown is tracked at Shipment level (`ShipmentDashboardDto.ByServiceType`).
2. **Delivery Success & SLA Rates**:
   - **Terminal Outcomes Only**: `SuccessfulDeliveryRatePercent = DeliveredShipmentsCount / CompletedTerminalShipmentsCount * 100`, where `CompletedTerminalShipmentsCount` includes only `Delivered`, `DeliveryFailed`, `Returning`, and `Returned` shipments (`Draft`/`Cancelled` and active `InTransit` shipments reported in `InTransitActiveExcludedCount` are excluded from the denominator).
   - **SLA Compliance**: Computed only over delivered shipments that have `EstimatedDeliveryDate != null` (`DeliveredWithSlaCount`): `OnTimeDeliveryRatePercent = OnTimeDeliveredCount / DeliveredWithSlaCount * 100`. Delivered shipments without SLA target are surfaced explicitly in `DeliveredWithoutSlaCount`.
3. **Draft / Cancelled Invoice Exclusion**:
   - `FinanceDashboardDto` reports `DraftInvoicesCount`/`Total` and `CancelledInvoicesCount`/`Total` separately from `IssuedAndActiveInvoicesCount` (`Issued`, `PartiallyPaid`, `Paid`, `Overdue`).
   - `AuthoritativeCustomerReceivables` and `AuthoritativeUnbilledBalance` read from `CustomerBalances` rather than recomputing raw invoices.
4. **Confirmed vs Allocated Payments**:
   - `TotalPaymentsConfirmed` counts `Payments` with `Status == "Confirmed"`.
   - `TotalPaymentsAllocated` sums `PaymentAllocations.AllocatedAmount` on confirmed payments, exposing `TotalUnallocatedPaymentCredit = Max(0, TotalPaymentsConfirmed - TotalPaymentsAllocated)`.
