# Cash on Delivery (COD) Architecture Specification

## 1. Architectural Overview & Boundaries

The **Cash on Delivery (COD) Management Module** governs the full lifecycle of physical cash collections from recipients upon package delivery, driver handovers, branch deposit verifications, customer payout batching (remittance), and continuous financial reconciliation within the internal Logistics Management System.

Built within the **Modular Monolith + Clean Architecture** framework (.NET 9.0), the COD module bridges operational transport execution (Shipping Module) with financial accounting (Finance & CRM Modules) under strict multi-branch authorization boundaries.

```mermaid
graph TD
    subgraph Shipping Operations
        DEL["Delivery Attempt / POD"] --> COL_TRIG["Trigger COD Collection"]
    end

    subgraph COD Operations & Lifecycle
        COL_TRIG --> DRV_COL["CollectedByDriver\n(Driver holds cash)"]
        DRV_COL --> DEP_BR["DepositedToBranch\n(Turned in at Hub)"]
        DEP_BR --> VER_CSH["VerifiedByCashier\n(Counted into branch vault)"]
        VER_CSH --> REM_BATCH["Add to CODRemittance (Draft)\n(Grouped by Customer)"]
        REM_BATCH --> REM_CONF["Confirm Remittance (Completed)\n(Payout to Customer Bank)"]
    end

    subgraph Finance Integration
        VER_CSH -- "+ TotalCodHoldingAmount" --> CUST_BAL["Customer Balance\n(COD Holding)"]
        REM_CONF -- "- TotalCodHoldingAmount" --> CUST_BAL
        REM_CONF -- "Deduct Fees" --> CUST_REV["Shipping Revenue\n(Net Settlement)"]
    end

    subgraph Reconciliation & Audit
        DRV_COL -.-> RECON["ICodReconciliationService\n(Audit & Discrepancies)"]
        DEP_BR -.-> RECON
        VER_CSH -.-> RECON
        REM_CONF -.-> RECON
    end
```

---

## 2. Clean Architecture Layer Organization

### Domain Layer (`Logistics.Server.Domain.Entities.Finance`)
Zero external dependencies. Leverages the existing schema entities:
- `CODCollection`: The atomic cash collection event tied to a specific shipment, tracked from driver receipt to branch deposit, cashier verification, and customer payout.
- `CODRemittance`: The customer settlement header with sequential remittance number (`REM-YYYYMM-######`), bank details, total COD amount, deducted service fees, and net payout.
- `CODRemittanceItem`: Individual line items linking verified `CODCollection` records to their parent remittance batch with itemized fee deductions.

### Application Layer (`Logistics.Server.Application.Modules.Cod`)
- **DTOs (`DTOs/CodDtos.cs`)**: Strongly typed request/response models for Collections, Remittances, Employee operations, and Reconciliation summaries.
- **Service Interfaces (`Services/ICodServices.cs`)**:
  - `ICodCollectionService`: Collection creation, driver collection updates, branch deposit, cashier verification, and failure tracking.
  - `ICodEmployeeService`: Driver cash holdings, personal collection records, branch-level driver summaries.
  - `ICodRemittanceService`: Remittance drafting, item attachment/detachment, settlement confirmation, and cancellation.
  - `ICodReconciliationService`: High-level reconciliation overview, driver reconciliation, branch vault reconciliation, and remittance batch validation.
- **Validators (`Validators/CodValidators.cs`)**: FluentValidation rules enforcing business invariants.
- **Security (`Common/Security/AppPermissions.Cod`)**: 13 granular permissions governing COD operations.

### Infrastructure Layer (`Logistics.Server.Infrastructure.Services.CodServices.cs`)
- Direct integration with `ApplicationDbContext`, `ICurrentUserService`, `INumberSequenceService`, and `IAuditService`.
- Atomic database updates, branch security isolation, and optimistic concurrency.

### Presentation Layer (`Logistics.Server.API.Controllers.Cod`)
RESTful endpoints following API conventions with route prefixes:
- `/api/v1/cod/collections`
- `/api/v1/cod/remittances`
- `/api/v1/cod/operations`
- `/api/v1/cod/reconciliation`

---

## 3. Core Status Progression

### COD Collection Status
```mermaid
stateDiagram-v2
    [*] --> CollectedByDriver : Driver collects cash
    CollectedByDriver --> DepositedToBranch : Cash turned in to Branch
    DepositedToBranch --> VerifiedByCashier : Cashier verifies in safe
    CollectedByDriver --> VerifiedByCashier : Direct verification
    VerifiedByCashier --> RemittedToCustomer : Remittance Confirmed
    RemittedToCustomer --> VerifiedByCashier : Remittance Cancelled
```

### COD Remittance Status
```mermaid
stateDiagram-v2
    [*] --> Draft : Create remittance with collections
    Draft --> Approved : Management approval
    Draft --> Processing : Bank transfer initiated
    Draft --> Completed : Payout confirmed & funds transferred
    Processing --> Completed : Bank confirmation
    Draft --> Cancelled : Remittance cancelled (items released)
    Processing --> Cancelled : Cancelled before execution
    Completed --> [*] : Final State (Requires journal reversal)
```
