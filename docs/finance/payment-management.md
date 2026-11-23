# Payment & Allocation Management

## 1. Overview & Business Model

The Payment Management subsystem (`IPaymentService`) handles recording customer cash or bank remittances, managing unallocated customer credit, allocating payments against one or more outstanding invoices, and executing atomic allocation reversals.

---

## 2. Payment Lifecycle & Entities

### Payment Record (`finance.Payments`)
- **`PaymentNumber`**: Auto-generated via `INumberSequenceService` (`PAY-YYYYMM-######`).
- **`CustomerId`**: Foreign key to `crm.Customers`.
- **`Amount`**: Total payment amount received.
- **`PaymentMethod`**: `BankTransfer`, `Cash`, `CreditCard`, `Cheque`, `CODOffset`.
- **`PaymentDate`**: Timestamp of fund receipt.
- **`TransactionReference`**: External banking or transfer reference ID.
- **`AllocatedAmount`**: Sum of active `PaymentAllocations`.
- **`UnallocatedAmount`**: `Amount - AllocatedAmount` (available customer credit balance).
- **`Status`**: `Confirmed`, `Allocated`, `PartiallyAllocated`, `Reversed`.

### Payment Allocation (`finance.PaymentAllocations`)
- Links a payment to a specific `Invoice`.
- `AllocatedAmount`: Portion of payment credited to the invoice.
- Increases `Invoice.PaidAmount` and decreases `Invoice.OutstandingAmount`.
- When `Invoice.PaidAmount == Invoice.TotalAmount`, the invoice automatically transitions to `Paid`.

---

## 3. Allocation Mechanics & Concurrency Safety

1. **Partial Payments**: Payments smaller than the invoice total reduce the outstanding amount and mark the invoice `PartiallyPaid`.
2. **Multi-Invoice Application**: A single lump-sum customer payment can be split across multiple invoices.
3. **Overpayment & Unallocated Balance**: If payment exceeds the targeted invoices, the remainder remains in `UnallocatedAmount` as unapplied credit on the customer's account for future billing cycles.
4. **General Ledger Credit Posting**:
   - Registering a confirmed payment posts a **Credit** entry to the General Ledger:
     - EntryType: `CustomerPayment`
     - ReferenceType: `Payment`
     - DebitAmount: `0`
     - CreditAmount: `payment.Amount`
     - Running balance decreases by payment amount.

---

## 4. Reversal Workflow

In the event of disputed allocations, returned cheques, or misapplied payments:
- Operators call `POST /api/v1/finance/payments/{id}/allocations/{allocationId}/reverse`.
- Reversal atomically:
  - Subtracts `allocation.AllocatedAmount` from `Invoice.PaidAmount`.
  - Re-evaluates `Invoice.Status` (reverts from `Paid` to `PartiallyPaid` or `Issued`).
  - Restores `Payment.UnallocatedAmount`.
  - Deletes the allocation record from `finance.PaymentAllocations`.
  - Records an immutable audit log entry in `audit.AuditLogs`.
