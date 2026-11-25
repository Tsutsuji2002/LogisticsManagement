# Cash on Delivery (COD) Reports Specification

Implemented across `DashboardQueryService.GetCodDashboardAsync` (`GET /api/v1/dashboard/cod`) and `FinancialReportService` (`reports.cod.read`), maintaining full consistency with `CodReconciliationService`.

## Reconciliation Pipeline & Formulas

1. **Expected COD (`ExpectedCod`)**:
   - Sum of `Shipment.CodAmount` across non-cancelled shipments with `CodAmount > 0`, scoped by `DestinationBranchId`.
2. **Collected by Drivers (`CollectedByDrivers`)**:
   - Sum of `CODCollections.CollectedAmount` across all collection statuses.
3. **Deposited to Branch (`DepositedToBranch`)**:
   - Sum of `CODCollections.CollectedAmount` where `Status` is `DepositedToBranch`, `VerifiedByCashier`, or `RemittedToCustomer`.
4. **Verified by Cashier (`VerifiedByCashier`)**:
   - Sum of `CODCollections.CollectedAmount` where `Status` is `VerifiedByCashier` or `RemittedToCustomer`.
5. **Remitted to Customers (`RemittedToCustomers`)**:
   - Sum of `CODRemittances.NetRemittanceAmount` where `Status == "Completed"`.
6. **Custody & Variance KPIs**:
   - `DriverCashInHand = Max(0, CollectedByDrivers - DepositedToBranch)`
   - `PendingCashierVerification = Max(0, DepositedToBranch - VerifiedByCashier)`
   - `DiscrepancyAmount = ExpectedCod - CollectedByDrivers`

## Detailed Endpoints

- `GET /api/v1/reports/cod-collections`: Driver collection rows including `ExpectedCodAmount`, `CollectedAmount`, and per-shipment `VarianceAmount = CollectedAmount - ExpectedCodAmount`.
- `GET /api/v1/reports/cod-remittances`: Remittance batches showing `TotalCodAmount`, `TotalServiceFeesDeducted`, `NetRemittanceAmount`, `ItemsCount`, and payout status.
- `GET /api/v1/reports/cod-reconciliation`: Branch-by-branch reconciliation summary scoped to accessible branches.
