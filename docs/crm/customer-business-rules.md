# CRM — Customer Business Rules & Data Integrity

## 1. Customer Master Integrity Rules

1. **Unique Customer Code**:
   - `Customer.CustomerCode` must be unique across the entire enterprise database.
   - Creating or updating a customer with an existing code throws `409 Conflict` (`RESOURCE_CONFLICT`).

2. **Customer Classification**:
   - `CustomerType` must strictly be either `Individual` or `Corporate`.

3. **Payment Terms & Policies**:
   - `PaymentType` must strictly be one of: `Prepaid`, `PostpaidMonthly`, `CodOffset`.
   - `CreditLimit` and `CreditTermDays` cannot be negative numbers.

4. **Deactivation vs. Physical Deletion**:
   - Customers who have existing business history (`sales.Orders`, `shipping.Shipments`, or `finance.Invoices`) **cannot be physically deleted**.
   - Attempting to delete a customer with historical records safely sets `IsActive = false` (or returns `409 Conflict` if hard deletion is attempted).

---

## 2. Customer Contact Rules

1. **Customer Ownership**:
   - A contact must belong to an existing, accessible customer (`contact.CustomerId == customer.Id`).
   - Cross-customer contact manipulation is prevented by verifying customer ownership on every operation.

2. **Single Primary Contact Rule**:
   - A customer can have at most one primary operational contact (`IsPrimary == true`).
   - When a contact is created or updated with `IsPrimary = true` or `PATCH /primary` is invoked, all other contacts for that customer are automatically updated to `IsPrimary = false`.

---

## 3. Customer Address & Location Rules

1. **Address Roles**:
   - `AddressType` must strictly be one of: `Sender`, `Recipient`, `Billing`, `PickupWarehouse`.

2. **Single Default Address Per Type**:
   - A customer can have at most one default address **per address role**.
   - When an address is marked default for an address type, existing default addresses of that same type for the customer are unset (`IsDefault = false`).

3. **Historical Address Preservation**:
   - Commercial orders (`sales.OrderAddresses`) and physical waybills (`shipping.ShipmentAddresses`) store immutable point-in-time address snapshots.
   - Modifying a customer's address in the CRM module **never** modifies historical orders or shipments.

---

## 4. Customer Contract Rules

1. **Unique Contract Number**:
   - `CustomerContract.ContractNumber` must be unique across the enterprise database.

2. **Validity Date Invariant**:
   - `StartDate <= EndDate`. Contracts cannot have an end date earlier than their start date.

3. **Status Transitions & Activation Guard**:
   - Valid statuses: `Draft`, `Active`, `Suspended`, `Expired`, `Terminated`.
   - A contract **cannot transition to `Active`** if the current date falls outside `[StartDate, EndDate]`.
   - Terminated contracts are final and cannot be reactivated.

---

## 5. Pricing Rule Precedence & Determinism

1. **Rule Evaluation Ordering**:
   When evaluating applicable custom pricing rules for a customer:
   - Filter active rules within the effective date range (`EffectiveFrom <= targetDate <= EffectiveTo`).
   - Order deterministically:
     1. `Priority ASC` (Priority 1 takes precedence over Priority 2).
     2. `DiscountPercent DESC` (Higher discount percentage takes precedence in ties).
     3. `EffectiveFrom DESC` (Most recent rule takes precedence).

2. **Financial Precision**:
   - Discount percentages are bounded within `[0.00, 100.00]`.
   - Flat discount amounts use `DECIMAL(19,4)` and cannot be negative.
