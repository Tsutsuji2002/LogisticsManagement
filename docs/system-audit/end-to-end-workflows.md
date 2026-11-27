# End-to-End Business Workflows Audit

## Workflow A: Customer -> Order -> Shipment -> Parcel Hub -> Delivery & POD -> Finance & COD -> Reporting

1. **CRM -> Sales**: Customer created/loaded under branch `SGN`; Sales order created in `Draft`, items & packages attached (`OrderTotals` recalculated), confirmed via `POST /api/v1/orders/{id}/confirm` (`OrderConfirmedIntegrationEvent` enqueued).
2. **Sales -> Shipping**: `POST /api/v1/shipments/from-order` creates a `ReadyForPickup` shipment using `NumberSequenceService` (`WAYBILL`), copies order packages/addresses, and verifies branch access.
3. **Warehouse Parcel Hub**: Transition to `PickedUp`, then `POST /api/v1/parcel-handling/receive` atomically marks the package `Received`, records `ParcelHandlingEvent (Unloaded)`, and transitions the shipment via `ShipmentStateHelper` to `InSortingHub`. Orders cannot be cancelled once any linked shipment reaches `PickedUp`, `InSortingHub`, `InTransit`, `OutForDelivery`, `Delivered`, `Returning`, or `Returned`.
4. **Last-Mile Delivery & POD**: `OutForDelivery` -> failed attempt creates `DeliveryFailed` + `DeliveryFailedIntegrationEvent` -> `ProofOfDeliveryService.CreateAsync` now blocks POD creation against non-`Delivered` attempts. After successful `Delivered` attempt (`DeliveryCompletedIntegrationEvent`), POD is recorded.
5. **COD Lifecycle**: `CreateCollectionAsync` enforces `0 <= CollectedAmount <= Shipment.CodAmount`; `DepositToBranchAsync` (`CollectedByDriver -> DepositedToBranch`); `VerifyCollectionAsync` (`VerifiedByCashier`) credits `CustomerBalance.TotalCodHoldingAmount` and enqueues `CodCollectedIntegrationEvent`. `CreateRemittanceAsync` and `AddItemAsync` now require `VerifiedByCashier`, and `ConfirmRemittanceAsync` completes settlement (`RemittedToCustomer`) and deducts `TotalCodHoldingAmount`.
6. **Finance Lifecycle**: `CreateDraftInvoiceAsync` / `AddInvoiceItemAsync` validates shipment charge ownership, prevents double invoicing, and marks `IsInvoiced = true` + `InvoiceId`; `IssueInvoiceAsync` posts debit ledger entry; `RegisterPaymentAsync` persists the `Payment` row first inside a transaction so `FinancialLedgerEntry.ReferenceId` accurately links `payment.Id`.
7. **Reporting**: Read-only queries under `/api/v1/dashboard/overview`, `/api/v1/dashboard/branches/{branchId}`, `/api/v1/reports/*`, and CSV exports reflect operational and financial aggregates using `AsNoTracking()` and UTC/UTC+7 windowing.

## Workflow B: Failed Delivery & Return Request Flow

- When a delivery attempt fails (`DeliveryFailed`), a return request can be created via `POST /api/v1/returns` (enforcing branch access and `Delivered`/`DeliveryFailed`/`Cancelled` eligibility) and approved via `POST /api/v1/returns/{id}/process`.
- **Hardening Fix Applied**: `ReturnRequestService.ProcessAsync` now checks `ShipmentStateHelper.IsValidTransition(shipment.ShipmentStatus, "Returning")` before setting shipment status to `Returning` (so returns initiated on `Delivered` shipments approve cleanly without corrupting terminal `Delivered` status).

## Workflow C: Physical Inventory Flow

- `WarehouseReceipt` confirmation increases `InventoryBalances.QuantityOnHand` and creates an `InventoryTransaction (Receipt)`.
- `InventoryReservation` allocates available quantity; `ConsumeReservationAsync` now runs in a relational transaction, rejects missing or insufficient balances instead of clamping to `0`, resolves employee identity cleanly instead of hardcoding `1`, and writes `Dispatch` transactions.
- **Hardening Fix Applied**: `WarehouseDispatch` confirmation (`ConfirmDispatchAsync`) previously ignored physical `DispatchItems`; it now validates source location availability, deducts `QuantityOnHand`, and creates `InventoryTransaction (Dispatch)` entries atomically with parcel dispatch transitions.

## Verification Status

Verified end-to-end across `HardeningAndE2EIntegrationTests` (`EndToEnd_Workflow_Order_Shipping_ParcelHub_Delivery_Pod_Finance_Cod_And_Dashboard_Succeeds`, `EndToEnd_Warehouse_DispatchItems_DeductsInventoryAtomically`) and `ShippingIntegrationTests`, `CodIntegrationTests`, `FinanceIntegrationTests`, and `WarehouseIntegrationTests`.
