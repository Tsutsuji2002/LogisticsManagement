# CRM — Customer Management Module Specification

## 1. Architectural Overview & Responsibilities

The **CRM (Customer Relationship Management)** module governs commercial client accounts, liaisons, physical and billing locations, commercial agreements, and custom pricing exception rules within the internal Logistics Management System.

### Module Architecture

```text
src/Server/
├── Logistics.Server.Domain/
│   └── Entities/Crm/
│       ├── Customer.cs             # Customer account profile
│       ├── CustomerContact.cs      # Contact persons and representatives
│       ├── CustomerAddress.cs      # Junction binding customer to addresses
│       ├── Address.cs              # Normalized geographic address
│       ├── CustomerContract.cs     # Legal service level agreements
│       └── CustomerPricingRule.cs  # Customer custom rate overrides
│
├── Logistics.Server.Application/
│   └── Modules/Crm/
│       ├── DTOs/
│       │   ├── CustomerDto.cs
│       │   ├── CustomerContactDto.cs
│       │   ├── CustomerAddressDto.cs
│       │   ├── CustomerContractDto.cs
│       │   └── CustomerPricingRuleDto.cs
│       ├── Services/
│       │   └── ICrmServices.cs     # 5 module service contracts
│       └── Validators/
│           └── CrmValidators.cs    # FluentValidation input rules
│
├── Logistics.Server.Infrastructure/
│   └── Services/
│       ├── CrmServices.cs          # EF Core implementations with branch isolation
│       └── AuditService.cs         # Audit trail logging
│
└── Logistics.Server.API/
    └── Controllers/Crm/
        ├── CustomersController.cs
        ├── CustomerContactsController.cs
        ├── CustomerAddressesController.cs
        ├── CustomerContractsController.cs
        └── CustomerPricingRulesController.cs
```

---

## 2. Entity Relationships & Domain Design

```mermaid
erDiagram
    Branches ||--o{ Customers : "registers"
    Employees ||--o{ Customers : "account_manager"
    Customers ||--o{ CustomerContacts : "has_contacts"
    Customers ||--o{ CustomerAddresses : "has_addresses"
    Addresses ||--o{ CustomerAddresses : "referenced_by"
    Customers ||--o{ CustomerContracts : "signs"
    Customers ||--o{ CustomerPricingRules : "custom_rules"
    CustomerContracts ||--o{ CustomerPricingRules : "governs"

    Customers {
        bigint Id PK
        uuid PublicId
        bigint BranchId FK
        varchar CustomerCode UK
        varchar CustomerType
        nvarchar Name
        nvarchar TradeName
        varchar TaxCode
        varchar Email
        varchar PhoneNumber
        decimal CreditLimit
        int CreditTermDays
        varchar PaymentType
        bigint AccountManagerEmployeeId FK
        bit IsActive
    }

    CustomerContacts {
        bigint Id PK
        uuid PublicId
        bigint CustomerId FK
        nvarchar ContactName
        nvarchar JobTitle
        varchar PhoneNumber
        varchar Email
        bit IsPrimary
        nvarchar Notes
    }

    CustomerAddresses {
        bigint Id PK
        uuid PublicId
        bigint CustomerId FK
        bigint AddressId FK
        varchar AddressType
        bit IsDefault
        nvarchar AddressLabel
    }

    Addresses {
        bigint Id PK
        uuid PublicId
        nvarchar ContactName
        varchar PhoneNumber
        varchar Email
        nvarchar AddressLine1
        nvarchar AddressLine2
        bigint CountryId FK
        bigint ProvinceId FK
        bigint DistrictId FK
        bigint WardId FK
        varchar PostalCode
        decimal Latitude
        decimal Longitude
        nvarchar FormattedAddress
    }

    CustomerContracts {
        bigint Id PK
        uuid PublicId
        bigint CustomerId FK
        varchar ContractNumber UK
        nvarchar Title
        date StartDate
        date EndDate
        varchar Status
        decimal DiscountRate
        int PaymentTermDays
        date SignedAt
        bigint FileAttachmentId FK
    }

    CustomerPricingRules {
        bigint Id PK
        uuid PublicId
        bigint CustomerId FK
        bigint CustomerContractId FK
        bigint ShippingRateCardId FK
        nvarchar Name
        decimal DiscountPercent
        decimal FlatDiscountAmount
        int Priority
        datetime2 EffectiveFrom
        datetime2 EffectiveTo
        bit IsActive
    }
```

---

## 3. Branch-Scoped Access Control

Every customer is registered under an operating company branch (`Customer.BranchId`). Access is strictly enforced on both reads and writes:

1. **SuperAdmin / Admin**: Unrestricted global visibility across all customers and branches.
2. **Branch-Scoped Users** (Branch Managers, Customer Service):
   - Only permitted to view and manage customers registered in branches matching their `AccessibleBranchIds` or `DefaultBranchId`.
   - Attempting to view or mutate a customer in another branch yields `403 Forbidden`.
   - Sub-resources (contacts, addresses, contracts, pricing rules) inherit their parent customer's branch authorization automatically.

---

## 4. Audit Trail Integration

All create, update, delete, and status change operations record an immutable audit trail entry in `audit.AuditLogs` via `IAuditService`:
- Captures `UserId`, `Username`, `UserRole`, `IpAddress`, `UserAgent`, and `CorrelationId`.
- Serializes clean before/after snapshots of changed data.
- Prevents object cycle errors through clean DTO mapping and `ReferenceHandler.IgnoreCycles`.
