# Financial General Ledger

## 1. Overview & Double-Entry Principles

The **Financial General Ledger** (`IFinancialLedgerService`) serves as the permanent, immutable journal of all commercial transactions.

It records financial movements for receivables, payments, credit adjustments, and refunds with double-entry semantics, customer-level segregation, and continuous running balances.

---

## 2. Ledger Entity Specification (`finance.FinancialLedgerEntries`)

| Column | Type | Description |
| :--- | :--- | :--- |
| `Id` | `BIGINT IDENTITY` | Clustered primary key |
| `PublicId` | `UNIQUEIDENTIFIER` | External API reference |
| `EntryNumber` | `VARCHAR(50)` | Sequential number (`FLE-YYYYMM-######`) |
| `CustomerId` | `BIGINT` | Customer account identifier |
| `EntryDate` | `DATETIME2` | Transaction timestamp |
| `EntryType` | `VARCHAR(50)` | `ReceivableInvoice`, `CustomerPayment`, `InvoiceCancellation`, `CreditNote`, `DebitAdjustment` |
| `ReferenceType` | `VARCHAR(50)` | `Invoice`, `Payment`, `ShipmentCharge`, `CreditAdjustment` |
| `ReferenceId` | `BIGINT` | Primary key of source entity |
| `ReferenceNumber` | `VARCHAR(50)` | Human-readable document number (e.g. `INV-202611-000001`) |
| `DebitAmount` | `DECIMAL(19,4)` | Increases customer receivable |
| `CreditAmount` | `DECIMAL(19,4)` | Decreases customer receivable |
| `RunningBalance` | `DECIMAL(19,4)` | Customer account balance immediately following entry |
| `CurrencyId` | `BIGINT` | Foreign key to `configuration.Currencies` |
| `Description` | `NVARCHAR(500)` | Journal narrative / explanation |

---

## 3. Running Balance Calculation Formula

Ledger entries are strictly sequential per customer. The running balance is calculated atomically at insertion time:

$$\text{PreviousBalance} = \text{RunningBalance of most recent entry for Customer (or 0)}$$

$$\text{NewRunningBalance} = \text{PreviousBalance} + \text{DebitAmount} - \text{CreditAmount}$$

### Example Ledger Flow

| Entry # | Type | Ref # | Debit (VND) | Credit (VND) | Running Balance (VND) |
| :--- | :--- | :--- | :---: | :---: | :---: |
| `FLE-202611-000001` | `ReceivableInvoice` | `INV-202611-000001` | 108,000 | 0 | **108,000** |
| `FLE-202611-000002` | `CustomerPayment` | `PAY-202611-000001` | 0 | 100,000 | **8,000** |
| `FLE-202611-000003` | `InvoiceCancellation` | `INV-202611-000001` | 0 | 8,000 | **0** |

---

## 4. Query & Audit Capabilities

The ledger API exposes multi-criteria filtering:
- By Customer ID (`customerId`)
- By Entry Type (`entryType`)
- By Reference Type (`referenceType`)
- Paginated results with total debit, total credit, and running balance reporting.
