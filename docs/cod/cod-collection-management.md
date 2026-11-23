# COD Collection Management Specification

## 1. Lifecycle Overview

The COD Collection lifecycle tracks funds from the physical moment a recipient hands cash to the delivery driver through to verified custody inside the branch safe.

```mermaid
sequenceDiagram
    autonumber
    actor Recipient
    actor Driver as Delivery Driver
    actor Cashier as Branch Cashier
    participant API as COD API
    participant DB as SQL Server
    participant CB as Customer Balance

    Recipient->>Driver: Pays Cash upon delivery
    Driver->>API: POST /api/v1/cod/collections (or /collect)
    API->>DB: Status = CollectedByDriver
    Note over Driver,API: Driver holds cash in field

    Driver->>Cashier: Hands over cash pouch at Hub counter
    Cashier->>API: POST /api/v1/cod/collections/{id}/deposit
    API->>DB: Status = DepositedToBranch

    Cashier->>Cashier: Physically counts & tallies banknotes
    Cashier->>API: POST /api/v1/cod/collections/{id}/verify
    API->>DB: Status = VerifiedByCashier, VerifiedByUserId set
    API->>CB: Increments TotalCodHoldingAmount
    Note over Cashier,CB: Funds verified in branch safe; ready for remittance
```

---

## 2. Status Definitions & Invariants

| Status | Responsibility | Description | Holding Balance Impact |
| :--- | :--- | :--- | :--- |
| `CollectedByDriver` | Delivery Driver | Driver has collected physical cash from the recipient upon delivery and issued a receipt. | None (Driver personal cash holding) |
| `DepositedToBranch` | Cashier / Hub Clerk | Driver turned in the collected funds at the branch counter; awaiting cashier physical audit. | None (Branch counter transit) |
| `VerifiedByCashier` | Cashier | Cashier physically counted banknotes, deposited funds into branch vault, and verified receipt. | `+ CustomerBalance.TotalCodHoldingAmount` |
| `RemittedToCustomer` | Finance / Treasury | Collection has been itemized in a confirmed `CODRemittance` and paid out to customer. | `- CustomerBalance.TotalCodHoldingAmount` |

---

## 3. Collection Operations

### 3.1 Creating a Collection Record
- **Endpoint**: `POST /api/v1/cod/collections`
- **Permission**: `cod.collections.create`
- **Validation**:
  - `ShipmentId` must point to an existing shipment with `CodAmount > 0`.
  - `CollectedAmount` must be greater than zero.
  - Driver/Employee must belong to the user's accessible branch.
  - Cannot duplicate: if a collection already exists for the shipment, existing record is returned or error thrown.

### 3.2 Recording Field Collection
- **Endpoint**: `POST /api/v1/cod/collections/{id}/collect`
- **Permission**: `cod.collections.collect`
- **Logic**:
  - Updates `CollectedAmount` and optional `DriverReceiptNumber`.
  - Sets collection date and ensures status is `CollectedByDriver`.

### 3.3 Branch Safe Deposit
- **Endpoint**: `POST /api/v1/cod/collections/{id}/deposit`
- **Permission**: `cod.collections.manage`
- **Logic**:
  - Transitions status from `CollectedByDriver` to `DepositedToBranch`.
  - Clears outstanding driver cash-in-hand balance.

### 3.4 Cashier Verification
- **Endpoint**: `POST /api/v1/cod/collections/{id}/verify`
- **Permission**: `cod.collections.verify`
- **Logic**:
  - Enforces branch access on `collection.Shipment.DestinationBranchId`.
  - Validates current status is `DepositedToBranch` or `CollectedByDriver`.
  - Transitions status to `VerifiedByCashier`.
  - Records `VerifiedByUserId` and `VerifiedAt` timestamp.
  - Atomically increments customer's `TotalCodHoldingAmount` in `CustomerBalance`.
  - Appends audit trail log.

### 3.5 Recording Collection Failure
- **Endpoint**: `POST /api/v1/cod/collections/{id}/record-failure`
- **Permission**: `cod.collections.manage`
- **Supported Reason Codes**:
  - `CustomerRefusedPayment`: Recipient refused to pay COD amount.
  - `CustomerNotAvailable`: Recipient was not at delivery address.
  - `IncorrectAmount`: Discrepancy between recipient expectation and shipment invoice.
  - `PackageDamaged`: Recipient rejected package due to visible packaging defects.
