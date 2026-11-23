# COD Business Rules & Invariants

## 1. Domain Invariants

1. **Shipment COD Mandatory Check**:
   - A `CODCollection` can only be initiated for shipments having `CodAmount > 0`.
   - Shipments with non-COD payment methods or zero COD amount cannot have collection records created.

2. **One-to-One Collection to Shipment Constraint**:
   - Each shipment has at most one associated `CODCollection` record.
   - Re-attempted collections operate on the same collection record or track failure history.

3. **Single Remittance Assignment**:
   - In accordance with `CODRemittanceItemConfiguration`, each `CODCollection` can belong to at most one `CODRemittanceItem` across the lifetime of the system.
   - Once a collection is added to a remittance, it cannot be added to another concurrent remittance.

4. **Remittance Status Prerequisites**:
   - Only collections with status `VerifiedByCashier` may be added to a remittance batch.
   - Collections in `CollectedByDriver` or `DepositedToBranch` cannot be added to remittances until physical cash has been verified in the branch safe.

5. **Customer Consistency in Remittance Batches**:
   - All collections bundled into a single `CODRemittance` must belong to the same `CustomerId`. Cross-customer batching is strictly forbidden.

6. **Currency Homogeneity**:
   - All collections and remittances must settle in the same currency (e.g., VND) as configured on the underlying shipment.

7. **Proportional Fee Deductions**:
   - When service fees are deducted at the remittance header level, the system must distribute fee deductions proportionally across items without creating negative net item amounts.
   - $\sum \text{DeductedShippingFee}_i \le \text{TotalCodAmount}$.

8. **Optimistic Concurrency on Customer Balance**:
   - Updates to `CustomerBalance.TotalCodHoldingAmount` must evaluate `RowVersion` to avoid lost updates under high concurrency.

---

## 2. Status Transition Matrix

### 2.1 COD Collection State Transitions

| From \ To | `CollectedByDriver` | `DepositedToBranch` | `VerifiedByCashier` | `RemittedToCustomer` |
| :--- | :---: | :---: | :---: | :---: |
| **New** | **Allowed** | Prohibited | Prohibited | Prohibited |
| `CollectedByDriver` | Allowed (update) | **Allowed** | **Allowed** (Direct) | Prohibited |
| `DepositedToBranch` | Prohibited | Allowed (update) | **Allowed** | Prohibited |
| `VerifiedByCashier` | Prohibited | Prohibited | Allowed (notes) | **Allowed** (Confirm) |
| `RemittedToCustomer` | Prohibited | Prohibited | **Allowed** (Cancel) | Prohibited |

### 2.2 COD Remittance State Transitions

| From \ To | `Draft` | `Approved` | `Processing` | `Completed` | `Cancelled` |
| :--- | :---: | :---: | :---: | :---: | :---: |
| **New** | **Allowed** | Prohibited | Prohibited | Prohibited | Prohibited |
| `Draft` | Allowed | **Allowed** | **Allowed** | **Allowed** | **Allowed** |
| `Approved` | Prohibited | Allowed | **Allowed** | **Allowed** | **Allowed** |
| `Processing` | Prohibited | Prohibited | Allowed | **Allowed** | **Allowed** |
| `Completed` | Prohibited | Prohibited | Prohibited | Locked | Prohibited |
| `Cancelled` | Prohibited | Prohibited | Prohibited | Prohibited | Locked |

---

## 3. Branch Authorization Rules

1. **Driver Operations**: Drivers can view their own collections (`cod.operations.read-own`). Branch managers can view collections of employees belonging to their assigned branches (`cod.operations.read-branch`).
2. **Cashier Verification**: Cashiers can only verify collections for shipments where `DestinationBranchId` matches a branch granted in their `UserBranchAccess`.
3. **Cross-Branch Restriction**: Non-admin users attempting to inspect or reconcile collections for unassigned branches receive `403 Forbidden`.
4. **Super Admin Exemption**: Users holding the `SUPER_ADMIN` role have organization-wide visibility across all branches.
