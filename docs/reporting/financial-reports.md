# Financial & Billing Reports Specification

Implemented in `FinancialReportService` (`IFinancialReportService`), strictly gated by `reports.finance.read` and scoped by authorized branches.

## Endpoints & Metrics

1. **`GET /api/v1/reports/shipping-charges`** (`ShippingChargeReportRowDto`):
   - Supports `?isInvoiced=true|false` to audit unbilled shipment charges waiting to be invoiced.
   - Displays charge type (`BaseFreight`, `CodFee`, etc.), amount, currency, invoiced flag, and linked `InvoiceNumber`.
2. **`GET /api/v1/reports/invoices`** (`InvoiceReportRowDto`):
   - Supports status filter (`Draft`, `Issued`, `PartiallyPaid`, `Paid`, `Overdue`, `Cancelled`) and customer branch scoping.
   - Evaluates `OutstandingAmount = TotalAmount - PaidAmount` explicitly in LINQ for uniform SQL Server and InMemory execution.
3. **`GET /api/v1/reports/payments`** (`PaymentReportRowDto`):
   - Breaks down every payment's `Amount`, `AllocatedAmount` (`Sum(PaymentAllocations.AllocatedAmount)`), and `UnallocatedAmount` customer credit.
4. **`GET /api/v1/reports/receivables`** (`ReceivableReportRowDto`):
   - Reads authoritative customer exposure directly from `CustomerBalances` (`CurrentOutstandingBalance`, `UnbilledChargesAmount`, `TotalCodHoldingAmount`) alongside `Customer.CreditLimit`.
