# Customer Balances & Credit Exposure

## 1. Overview & Risk Management

Customer Balance Management (`ICustomerBalanceService`) maintains an aggregated, real-time snapshot of financial liability and credit exposure for all active customers.

It is consulted by Order Management to prevent booking shipments beyond agreed credit limits and by Customer Service to review account standing.

---

## 2. Customer Balance Entity Model (`finance.CustomerBalances`)

```text
CustomerId                      -> FK to crm.Customers
CurrentOutstandingBalance       -> Sum of issued, unpaid invoice balances
UnbilledChargesAmount           -> Sum of delivered/un-invoiced shipment charges
TotalCodHoldingAmount           -> COD collected on behalf of customer awaiting remittance
TotalExposure                   -> CurrentOutstandingBalance + UnbilledChargesAmount
LastUpdatedDate                 -> Timestamp of latest financial modification
RowVersion                      -> Byte[] optimistic concurrency timestamp
```

### Exposure Metric Definition
$$\text{TotalExposure} = \text{CurrentOutstandingBalance} + \text{UnbilledChargesAmount}$$

- When a new order/shipment is booked, the system checks whether:
$$\text{TotalExposure} + \text{EstimatedOrderFee} \le \text{Customer.CreditLimit}$$

---

## 3. Balance Mutation Triggers

1. **Invoice Issuance**: Increases `CurrentOutstandingBalance` and decreases `UnbilledChargesAmount`.
2. **Invoice Cancellation**: Decreases `CurrentOutstandingBalance`.
3. **Payment Receipt / Allocation**: Decreases `CurrentOutstandingBalance` as payments settle invoices.
4. **Shipment Delivery (Unbilled Charge)**: Accrues `UnbilledChargesAmount` upon completed fulfillment before formal invoicing.

---

## 4. Reconciliation Engine (`ReconcileCustomerBalanceAsync`)

To guarantee mathematical auditability, the system provides an automated reconciliation endpoint:
`POST /api/v1/finance/customer-balances/{customerId}/reconcile`

The reconciliation algorithm:
1. Re-aggregates all active invoices for the customer:
   $$\text{ExpectedOutstanding} = \sum (\text{Invoice.TotalAmount} - \text{Invoice.PaidAmount})$$
2. Re-aggregates all un-invoiced shipment charges:
   $$\text{ExpectedUnbilled} = \sum \text{ShipmentCharges.Amount} \quad [\text{IsInvoiced} = \text{false}]$$
3. Compares recorded snapshot values against recomputed amounts:
   - If a discrepancy is detected (`!IsConsistent`), the balance record is automatically corrected to match source journals.
   - An audit trail record is generated documenting the adjustment details.
