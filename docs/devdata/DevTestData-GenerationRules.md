# Dev Test Data Generation Rules

## 1. Synthetic Vietnamese Business Domain Rules

All generated records use fictional identities and synthetic Vietnamese logistics data:
- **Company**: `Công ty TNHH Vận Tải & Logistics Phương Nam (Mẫu)` (`LMS Express Vietnam`)
- **Branches**: 4 operational branches (`HCM`, `HAN`, `DAN`, `CTO`) spanning Southern, Northern, Central, and Mekong Delta hubs.
- **Warehouses**: 6 facilities (`WH-HCM-01`, `WH-HCM-02` ParcelHub, `WH-HAN-01`, `WH-HAN-02` ParcelHub, `WH-DAN-01`, `WH-CTO-01`) with Receiving, BulkStorage, and ParcelSorting zones.
- **Employees & Accounts**: 36 active employees across branches/departments and 27 user accounts spanning `BRANCH_MANAGER`, `OPERATIONS_MANAGER`, `FINANCE_STAFF`, `CUSTOMER_SERVICE`, `WAREHOUSE_STAFF`, and `READ_ONLY` (default dev password `Test@123456`).
- **Customers**: Corporate (65%) and Individual (35%) customers across branches with realistic tax codes, Vietnamese street/district/province addresses, contracts, and pricing rules.

## 2. Temporal Distribution (90–180 Days)

- Orders and shipments are distributed across the past 180 days (`DaysAgo(0..179)`), allowing dashboard filters (7 days, 30 days, 90 days, 6 months) and trend charts to show continuous daily activity.
- `ApplicationDbContext.SaveChangesAsync` stamps `CreatedAt = DateTime.UtcNow` on entity insertion; `DevDataSeeder.BackdateCreatedAtAsync<TEntity>` explicitly updates `CreatedAt` in SQL Server via `ExecuteUpdateAsync` after batch insert so time-windowed reporting queries return accurate historical trends.
- Shipment statuses correlate realistically with record age:
  - 0–1 days: `ReadyForPickup`, `PickedUp`
  - 2–3 days: `InSortingHub`
  - 4–6 days: `InTransit`, `OutForDelivery`
  - 7+ days: ~89% `Delivered`, ~5.5% `DeliveryFailed`, ~4.5% `Returned` (plus ~4% cancelled orders).

## 3. Financial, COD & Inventory Consistency Rules

1. **Orders**: `Order.DeclaredValue == Sum(OrderItem.DeclaredValue)` and `Order.TotalWeightKg == Sum(OrderItem.WeightKg)`.
2. **Shipments & Charges**: `Shipment.TotalAmount == ShippingFee + InsuranceFee + SurchargesTotal == Sum(ShipmentCharge.Amount)`.
3. **Invoices & Payments**:
   - ~80% of delivered shipment charges are billed into monthly customer invoices (leaving ~20% unbilled so unbilled receivables KPIs are non-zero).
   - `Invoice.SubTotal == Sum(InvoiceItem.Amount)`, `Invoice.TaxAmount == Sum(InvoiceItem.TaxAmount)` (8% VAT), `Invoice.TotalAmount == SubTotal + TaxAmount`.
   - `PaymentAllocation` links confirmed payments to `Paid` and `PartiallyPaid` invoices so `Invoice.PaidAmount == Sum(PaymentAllocation.AllocatedAmount)`.
4. **COD Pipeline**:
   - ~40% of orders carry COD (`CodAmount > 0`).
   - Delivered COD shipments generate `CODCollection` rows split across `CollectedByDriver` (~20%), `DepositedToBranch` (~10%), `VerifiedByCashier` (~18%), and `RemittedToCustomer` (~52%).
   - Remitted collections are grouped into `Completed` `CODRemittance` batches deducting 1.5% COD service fee (`NetRemittanceAmount = TotalCodAmount - TotalServiceFeesDeducted`).
5. **Customer Balances**: `ReconcileCustomerBalancesAsync` computes `CurrentOutstandingBalance`, `UnbilledChargesAmount`, and `TotalCodHoldingAmount` directly from authoritative `Invoices`, unbilled `ShipmentCharges`, and unremitted verified `CODCollections`.
6. **Inventory**: ~15% of warehouse SKUs are intentionally stocked below `ReorderPoint` (15) to exercise low-stock dashboard alerts, while keeping `0 <= QuantityAllocated <= QuantityOnHand`.

## 4. Number Sequence Synchronization

After seeding, `UpdateNumberSequencesToMaxAsync` advances `NumberSequences.NextNumber` across all 13 scopes (`ORDER`, `WAYBILL`, `INV`, `PAY`, `FLE`, `RECEIPT`, `DISPATCH`, `ADJUST`, `TRANSFER`, `INV-TXN`, `PICKUP`, `RETURN`, `REMITTANCE`) past the seeded count so subsequent records created via UI or API never collide with unique codes.
