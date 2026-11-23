# COD & Finance Integration Specification

## 1. Integration Principles & Boundaries

The COD module operates in close coordination with the Finance and CRM domains without violating architectural boundaries:

1. **No Duplicate Financial Payments**:
   - COD funds are **custodial funds held in trust** for the customer, not operational company revenue.
   - Payout of COD proceeds is tracked in `CODRemittance` and `CustomerBalance.TotalCodHoldingAmount`. It does NOT create duplicate `Payment` or `PaymentAllocation` records in the corporate billing ledger unless explicit shipping fee netting occurs.

2. **Customer Balance Integration**:
   - `CustomerBalance.TotalCodHoldingAmount` stores the real-time sum of verified COD money held by the company.
   - **Cashier Verification**: Increments `TotalCodHoldingAmount`.
   - **Remittance Confirmation**: Decrements `TotalCodHoldingAmount`.
   - **Concurrency Control**: Protected by EF Core optimistic concurrency tokens (`RowVersion`).

3. **Shipment Lifecycle Validation**:
   - Collections link to `Shipment.Id` and require `Shipment.CodAmount > 0`.
   - Collections validate shipment status and payment terms.

4. **Multi-Branch Isolation**:
   - Driver operations are validated against employee branch assignment (`Employee.BranchId`).
   - Cashier operations and verification are restricted by destination branch (`Shipment.DestinationBranchId`).
   - Cross-branch access requires `SUPER_ADMIN` role or explicit branch authorization in `UserBranchAccess`.

5. **Audit Logging**:
   - All state transitions (`CreateCodCollection`, `DepositCodToBranch`, `VerifyCodCollection`, `RecordCodFailure`, `CreateCodRemittance`, `ConfirmCodRemittance`, `CancelCodRemittance`) invoke `IAuditService.RecordAsync` to persist immutable audit entries with old/new states.
