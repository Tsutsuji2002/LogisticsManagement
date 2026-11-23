# COD Reconciliation Specification

## 1. Overview & Business Purpose

COD reconciliation provides continuous cross-checks between transport events, driver cash in hand, branch vault balances, and customer payouts.

The reconciliation service (`ICodReconciliationService`) evaluates read-only aggregated positions across four core dimensions:
1. **Organizational Summary**: Company-wide or branch-scoped expected vs collected vs deposited vs verified vs remitted.
2. **Employee Reconciliation**: Individual driver collections, branch handovers, and outstanding cash in hand.
3. **Branch Vault Reconciliation**: Cashier intake, verified vault cash, and unremitted company holding.
4. **Remittance Reconciliation**: Itemized line-item integrity vs remittance header totals.

---

## 2. Core Formulas & Reconciliation Logic

### 2.1 Driver Cash in Hand (Outstanding Driver Custody)

$$\text{DriverCashInHand} = \text{CollectedAmount} - \text{DepositedToBranchAmount} - \text{VerifiedByCashierAmount}$$

- A positive $\text{DriverCashInHand}$ indicates funds collected in the field that have not yet been turned in to the branch counter.
- Driver summaries flag any collection older than the branch daily deposit cutoff (typically end of day).

### 2.2 Branch Vault Holding (Unverified Counter Intake)

$$\text{PendingCashierVerification} = \text{DepositedToBranchAmount} - \text{VerifiedByCashierAmount}$$

- Measures funds physically handed over by drivers at the branch desk awaiting cashier count and verification.

### 2.3 Company Customer Holding (Available for Remittance)

$$\text{AvailableForRemittance} = \text{VerifiedAmount} - \text{RemittedAmount}$$

- Represents verified funds in the company bank account or vault awaiting payout batching to shippers.
- Must reconcile to `CustomerBalance.TotalCodHoldingAmount`.

---

## 3. Discrepancy Detection & Categorization

| Discrepancy Type | Condition | Risk Level | Operational Resolution |
| :--- | :--- | :--- | :--- |
| `DriverShortage` | Driver deposited less than receipted amount. | High | Shift supervisor investigates driver receipt book and cash pouch. |
| `DelayedDeposit` | Driver holds cash $> 24$ hours without branch deposit. | High | Automated alert sent to Branch Manager. |
| `CounterDiscrepancy` | Cashier verified count differs from driver turn-in slip. | Medium | Joint recount between driver and cashier; physical recount. |
| `RemittanceVariance` | Sum of item net amounts $\neq$ header NetRemittanceAmount. | High | System prevents remittance confirmation until items match header. |

---

## 4. API Endpoints

- `GET /api/v1/cod/reconciliation/summary?branchId=&fromDate=&toDate=`
- `GET /api/v1/cod/reconciliation/employees/{employeeId}?fromDate=&toDate=`
- `GET /api/v1/cod/reconciliation/branches/{branchId}?fromDate=&toDate=`
- `GET /api/v1/cod/reconciliation/remittances/{remittanceId}`
