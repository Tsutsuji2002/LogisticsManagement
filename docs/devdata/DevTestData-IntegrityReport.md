# Dev Test Data Integrity Report

Generated from live SQL Server (`LAPTOP-S340\SQLEXPRESS`, database `LogisticsManagement`) verification via `POST /api/v1/dev/test-data/verify` after `ResetAndReseed (Standard)`.

## 1. Automated Integrity Verification Summary

- **Overall Status**: `ALL PASSED (8 / 8)`
- **Environment**: `Development`
- **Database**: `LAPTOP-S340\SQLEXPRESS / LogisticsManagement`

| # | Category | Check Name | Status | Violations | Observed Details |
|---|---|---|---|---:|---|
| 1 | Sales | Mọi đơn hàng đều có ít nhất 1 mặt hàng | PASS | 0 | All 600 orders have 1–3 `OrderItems` |
| 2 | Sales | Tổng giá trị khai báo đơn hàng = tổng chi tiết OrderItems | PASS | 0 | `Order.DeclaredValue == Sum(OrderItem.DeclaredValue)` across all 600 orders |
| 3 | Shipping | Vận đơn đã giao (Delivered) đều có ProofOfDelivery | PASS | 0 | All 435 `Delivered` shipments link to a `DeliveryAttempt` and `ProofOfDelivery` |
| 4 | Finance | Tổng cước Shipment.TotalAmount = tổng ShipmentCharges | PASS | 0 | All 511 shipments match `Sum(ShipmentCharge.Amount)` |
| 5 | Finance | Công thức Hóa đơn (SubTotal + Tax = Total & Paid <= Total) | PASS | 0 | All 261 invoices satisfy `SubTotal + TaxAmount == TotalAmount` and `PaidAmount <= TotalAmount` |
| 6 | Finance | Phân bổ thanh toán <= Số tiền thanh toán | PASS | 0 | All 183 payments have valid `PaymentAllocation` sums |
| 7 | COD | Đối soát COD (Đã hoàn trả <= Đã thu & công thức Net chính xác) | PASS | 0 | Collected: `1,111,127,000 ₫` \| Remitted (gross): `569,836,000 ₫` |
| 8 | Inventory | Tồn kho hợp lệ (OnHand >= Allocated >= 0) | PASS | 0 | All 120 `InventoryBalances` satisfy `0 <= Allocated <= OnHand` |

## 2. Cross-Dashboard Financial & Operational Reconciliation

Verified directly against `/api/v1/dashboard/{overview,finance,cod,inventory}`:

- **Invoicing & Receivables**:
  - Total Issued Invoices: `86,266,545 ₫` (261 invoices)
  - Confirmed Payments: `55,207,829 ₫` (183 payments)
  - Customer Outstanding Receivables: `31,058,716 ₫` (`86,266,545 - 55,207,829 = 31,058,716 ₫`, exact match)
  - Unbilled Shipment Charges: `39,075,119 ₫` (identical across Overview & Finance dashboards)
- **COD Pipeline**:
  - Expected COD on Active Shipments: `1,354,584,000 ₫`
  - Collected by Drivers: `1,111,127,000 ₫` (173 collections)
  - Verified by Cashiers (incl. Remitted): `830,144,000 ₫`
  - Remitted to Customers (Net): `561,286,000 ₫` (60 remittances; gross `569,836,000 ₫`)
  - Unremitted Verified Customer COD Holding: `260,308,000 ₫` (`830,144,000 - 569,836,000 = 260,308,000 ₫`, exact match)
  - Outstanding Driver Cash in Hand: `157,257,000 ₫`
- **Inventory**:
  - Total On Hand: `11,431` | Allocated: `744` | Available: `10,687` (`11,431 - 744 = 10,687`, exact match)
  - Low-stock SKU/warehouse combinations: `16` | Active reservations: `20`
- **Time-Window Distribution**:
  - Last 30 days (`2026-10-29..2026-11-28`): `129` Orders, `111` Shipments, `75` Delivered
  - Full 180-day history: `600` Orders, `511` Shipments, `435` Delivered
