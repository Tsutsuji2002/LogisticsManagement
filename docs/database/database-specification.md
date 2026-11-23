# Logistics Management System — Database Specification

**Document Version:** 1.0.0  
**Status:** Authoritative Specification & Baseline  
**Target Engine:** Microsoft SQL Server 2022+ / Azure SQL Database  
**Object-Relational Mapping (ORM):** Entity Framework Core 9.0  
**Architectural Style:** Modular Monolith within Clean Architecture (Internal Logistics Operations)

---

## 1. Project Context and Design Principles

### 1.1 Business Context
The Logistics Management System is designed for a nationwide **internal logistics enterprise**. The system operates the company's internal logistics lifecycle end-to-end:
- Managing multiple regional branches, departments, and employees.
- Managing customers, contacts, multi-role addresses, contracts, and commercial terms.
- Managing order booking, parcel packaging, declared values, and weight measurements.
- Managing physical shipments, dispatching, routing, multicarrier handoffs, tracking, delivery attempts, and proof-of-delivery (POD).
- Operating dual-purpose physical facilities: **Inventory Warehouses** (stocking, picking, packing, inventory lot control, cycle counting) and **Parcel Sorting Hubs** (high-throughput barcode scanning, sorting, staging, cross-docking).
- Integrating with external commercial shipping carriers (such as GHN, GHTK, Viettel Post) via an adapter pattern.
- Handling financial operations: fee calculation, invoicing, payments, partial allocations, Cash-on-Delivery (COD) driver collections, and customer remittances.
- Reliability patterns: Outbox message pattern for asynchronous events, immutable audit trails, and background job telemetry.

### 1.2 Non-Negotiable Architectural Decisions
1. **Internal Logistics Enterprise Model**: The company is a single unified enterprise with multiple operating branches. **No SaaS multi-tenancy (`TenantId`) is permitted.**
2. **Order vs. Shipment Separation**:
   - An `Order` represents the customer's commercial request to transport goods.
   - A `Shipment` is the physical unit of movement (waybill/consignment) with route assignments, package scans, and status milestones. One order can map to multiple shipments.
3. **Inventory vs. Parcel Hub Separation**:
   - Inventory movements (`inventory` schema) adjust physical stock balances and require lots/locations.
   - Parcel scans (`warehouse.ParcelHandlingEvents`) track transit packages moving through hubs and **never** manipulate product inventory quantities.
4. **Historical Immutability**:
   - Historical tables (`ShipmentStatusHistories`, `ParcelHandlingEvents`, `CarrierApiLogs`, `InventoryTransactions`, `AuditLogs`, `FinancialLedgerEntries`) are write-only / append-only.
   - Soft-delete flags and mutable update timestamps are avoided on immutable log tables.
5. **No Direct Database Access from Desktop**:
   - The Windows Forms desktop client accesses SQL Server strictly through the ASP.NET Core Web API.
6. **SQL Server Schemas**:
   - Database objects are segregated into 14 domain-aligned schemas rather than `dbo`.

---

## 2. SQL Server Schemas

| Schema | Domain Focus |
|:---|:---|
| `organization` | Company legal profile, regional branches, departments, employee master records, and facility master records. |
| `identity` | User accounts, credentials, RBAC roles, fine-grained permissions, branch-level access control, and session tokens. |
| `configuration` | Vietnamese administrative divisions (Provinces, Districts, Wards), currencies, UOMs, system settings, and sequence generators. |
| `crm` | Customer profiles, contact persons, multi-purpose normalized addresses, customer contracts, and custom pricing rules. |
| `catalog` | Product categories, SKU definitions, parcel classifications, dimensional and weight standards. |
| `sales` | Commercial orders, order line items, order-level sender/recipient address snapshots, and order package specs. |
| `shipping` | Physical shipments (waybills), package tracking, route segments, assignments, pickup requests, delivery attempts, and PODs. |
| `carrier` | External carrier profiles (GHN, GHTK, etc.), carrier services, encrypted API configs, status mappings, and raw webhook/API logs. |
| `warehouse` | Warehouse zones, bin locations, inventory receipts, outbound dispatches, and high-frequency parcel handling scan events. |
| `inventory` | Stock items, lots, serial numbers, operational balances, transactions, adjustments, transfers, and reservations. |
| `finance` | Rate cards, shipping charges, invoices, invoice line items, payments, payment allocations, COD collections, remittances, and ledger. |
| `notification` | Multichannel notification templates, dispatch records, delivery logs, report definitions, and scheduled reports. |
| `integration` | Background job execution telemetry and transactional Outbox messages for reliable event dispatch. |
| `audit` | Comprehensive entity change tracking (audit log) and centralized binary file metadata attachments. |

---

## 3. Data Type and Common Column Conventions

### 3.1 Data Types
- **Internal Primary Keys**: `BIGINT IDENTITY(1,1)` (clustered primary key for maximum indexing efficiency, join performance, and page fill density).
- **Public Identifiers**: `UNIQUEIDENTIFIER` (`DEFAULT NEWSEQUENTIALID()`, nonclustered unique index for secure REST API endpoints and client references).
- **Money & Financial Quantities**: `DECIMAL(19,4)` for monetary amounts; `DECIMAL(12,3)` for physical weights (kg) and dimensions (cm).
- **Date & Time**: `DATETIME2(7)` stored strictly in UTC (`DEFAULT SYSUTCDATETIME()`).
- **Codes & Keys**: `VARCHAR(50)` / `VARCHAR(100)` for ASCII codes (ISO codes, tracking numbers, barcodes).
- **Unicode Text**: `NVARCHAR(n)` for Vietnamese and international text.
- **Optimistic Concurrency**: `ROWVERSION` (`byte[]`) on mutable operational and financial master records.

### 3.2 Common Columns by Pattern
- **Mutable Master/Operational Entities**:
  - `Id` (BIGINT, PK, IDENTITY)
  - `PublicId` (UNIQUEIDENTIFIER, UNIQUE, NOT NULL)
  - `CreatedAt` (DATETIME2, NOT NULL)
  - `CreatedBy` (NVARCHAR(100), NULL)
  - `UpdatedAt` (DATETIME2, NULL)
  - `UpdatedBy` (NVARCHAR(100), NULL)
  - `RowVersion` (ROWVERSION, NOT NULL)
- **Immutable Historical / Ledger Entities**:
  - `Id` (BIGINT, PK, IDENTITY)
  - `Timestamp` / `CreatedAt` (DATETIME2, NOT NULL)
  - Relevant foreign keys, payload/details, and operational actor.

---

## 4. Module Table Specifications

### 4.1 Organization Module (`organization` schema)

#### 1. `organization.Companies`
Represents the parent logistics company legal entity.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Internal identifier.
- `PublicId` (UNIQUEIDENTIFIER, UNIQUE, NOT NULL, DEFAULT NEWSEQUENTIALID()): External reference.
- `Code` (VARCHAR(50), NOT NULL, UNIQUE): Unique company code (e.g., `LOG-VN`).
- `LegalName` (NVARCHAR(200), NOT NULL): Official registered corporate name.
- `TradingName` (NVARCHAR(200), NOT NULL): Commercial brand name.
- `TaxNumber` (VARCHAR(50), NOT NULL, UNIQUE): Enterprise tax registration code.
- `Email` (VARCHAR(150), NOT NULL): Official company contact email.
- `Phone` (VARCHAR(50), NOT NULL): Official corporate phone number.
- `Website` (VARCHAR(200), NULL): Corporate web portal.
- `LogoUrl` (NVARCHAR(500), NULL): URL or file path to corporate logo.
- `IsActive` (BIT, NOT NULL, DEFAULT 1): Operational status flag.
- `CreatedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): UTC creation timestamp.
- `CreatedBy` (NVARCHAR(100), NULL): User identity who created the record.
- `UpdatedAt` (DATETIME2, NULL): UTC timestamp of last update.
- `UpdatedBy` (NVARCHAR(100), NULL): User identity of last updater.
- `RowVersion` (ROWVERSION, NOT NULL): Concurrency token.

#### 2. `organization.Branches`
Regional operating hubs and administrative branch offices.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Internal identifier.
- `PublicId` (UNIQUEIDENTIFIER, UNIQUE, NOT NULL, DEFAULT NEWSEQUENTIALID()): External reference.
- `CompanyId` (BIGINT, NOT NULL, FK -> `organization.Companies.Id`): Parent company reference.
- `Code` (VARCHAR(50), NOT NULL, UNIQUE): Branch identifier code (e.g., `BR-HAN-01`, `BR-SGN-01`).
- `Name` (NVARCHAR(150), NOT NULL): Full branch name.
- `BranchType` (VARCHAR(50), NOT NULL): Categorization (`Headquarters`, `RegionalOffice`, `TransitHub`, `DeliveryPost`).
- `Phone` (VARCHAR(50), NOT NULL): Branch phone number.
- `Email` (VARCHAR(150), NULL): Branch operational email.
- `AddressId` (BIGINT, NULL, FK -> `crm.Addresses.Id`): Physical address reference.
- `IsActive` (BIT, NOT NULL, DEFAULT 1): Branch active status.
- `CreatedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): UTC creation timestamp.
- `CreatedBy` (NVARCHAR(100), NULL): Creator user identifier.
- `UpdatedAt` (DATETIME2, NULL): Timestamp of last update.
- `UpdatedBy` (NVARCHAR(100), NULL): Last update user identifier.
- `RowVersion` (ROWVERSION, NOT NULL): Concurrency token.

#### 3. `organization.Departments`
Functional divisions within branches (e.g., Fleet Dispatch, Warehouse, Customer Care, Accounting).
- `Id` (BIGINT, PK, IDENTITY(1,1)): Internal identifier.
- `PublicId` (UNIQUEIDENTIFIER, UNIQUE, NOT NULL, DEFAULT NEWSEQUENTIALID()): External reference.
- `CompanyId` (BIGINT, NOT NULL, FK -> `organization.Companies.Id`): Parent company reference.
- `BranchId` (BIGINT, NOT NULL, FK -> `organization.Branches.Id`): Associated branch.
- `Code` (VARCHAR(50), NOT NULL): Department code unique within the branch.
- `Name` (NVARCHAR(150), NOT NULL): Department title.
- `Description` (NVARCHAR(500), NULL): Scope and responsibility details.
- `ManagerEmployeeId` (BIGINT, NULL, FK -> `organization.Employees.Id`): Appointed department manager.
- `IsActive` (BIT, NOT NULL, DEFAULT 1): Active flag.
- `CreatedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Creation timestamp.
- `CreatedBy` (NVARCHAR(100), NULL): Creator username.
- `UpdatedAt` (DATETIME2, NULL): Update timestamp.
- `UpdatedBy` (NVARCHAR(100), NULL): Updater username.
- `RowVersion` (ROWVERSION, NOT NULL): Concurrency token.
- *Unique Constraint*: `(BranchId, Code)`.

#### 4. `organization.Employees`
Personnel working for the enterprise (drivers, warehouse operators, sorters, dispatchers, accountants).
- `Id` (BIGINT, PK, IDENTITY(1,1)): Internal identifier.
- `PublicId` (UNIQUEIDENTIFIER, UNIQUE, NOT NULL, DEFAULT NEWSEQUENTIALID()): External reference.
- `BranchId` (BIGINT, NOT NULL, FK -> `organization.Branches.Id`): Assigned primary operating branch.
- `DepartmentId` (BIGINT, NULL, FK -> `organization.Departments.Id`): Assigned functional department.
- `Code` (VARCHAR(50), NOT NULL, UNIQUE): Unique employee badge / staff code (e.g., `EMP-10042`).
- `FirstName` (NVARCHAR(100), NOT NULL): First name.
- `LastName` (NVARCHAR(100), NOT NULL): Last name / Middle name.
- `FullName` (NVARCHAR(200), NOT NULL): Complete name.
- `Email` (VARCHAR(150), NOT NULL, UNIQUE): Corporate or official email.
- `PhoneNumber` (VARCHAR(50), NOT NULL): Contact phone number.
- `DateOfBirth` (DATE, NULL): Birthdate.
- `Gender` (VARCHAR(20), NULL): Gender identifier (`Male`, `Female`, `Other`).
- `JobTitle` (NVARCHAR(100), NOT NULL): Position designation (`Courier`, `Dispatcher`, `ForkliftOperator`, etc.).
- `HireDate` (DATE, NOT NULL): Employment commencement date.
- `Status` (VARCHAR(30), NOT NULL, DEFAULT 'Active'): Status (`Active`, `OnLeave`, `Suspended`, `Terminated`).
- `AddressId` (BIGINT, NULL, FK -> `crm.Addresses.Id`): Residential address reference.
- `CreatedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Creation timestamp.
- `CreatedBy` (NVARCHAR(100), NULL): Creator user.
- `UpdatedAt` (DATETIME2, NULL): Update timestamp.
- `UpdatedBy` (NVARCHAR(100), NULL): Updater user.
- `RowVersion` (ROWVERSION, NOT NULL): Concurrency token.

#### 5. `organization.Warehouses`
Physical logistics facilities: includes inventory fulfillment warehouses and parcel sorting/distribution hubs.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Internal identifier.
- `PublicId` (UNIQUEIDENTIFIER, UNIQUE, NOT NULL, DEFAULT NEWSEQUENTIALID()): External reference.
- `BranchId` (BIGINT, NOT NULL, FK -> `organization.Branches.Id`): Controlling branch.
- `Code` (VARCHAR(50), NOT NULL, UNIQUE): Facility code (e.g., `WH-HN-CENTRAL`, `HUB-SGN-NORTH`).
- `Name` (NVARCHAR(150), NOT NULL): Display name of the facility.
- `Type` (VARCHAR(50), NOT NULL): Functional nature (`InventoryWarehouse`, `ParcelHub`, `Hybrid`).
- `TotalAreaSqM` (DECIMAL(12,2), NOT NULL, DEFAULT 0): Usable floor area in square meters.
- `AddressId` (BIGINT, NULL, FK -> `crm.Addresses.Id`): Geographic address reference.
- `ManagerEmployeeId` (BIGINT, NULL, FK -> `organization.Employees.Id`): Facility director.
- `IsActive` (BIT, NOT NULL, DEFAULT 1): Operational status flag.
- `CreatedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Creation timestamp.
- `CreatedBy` (NVARCHAR(100), NULL): Creator user.
- `UpdatedAt` (DATETIME2, NULL): Update timestamp.
- `UpdatedBy` (NVARCHAR(100), NULL): Updater user.
- `RowVersion` (ROWVERSION, NOT NULL): Concurrency token.

---

### 4.2 Identity and Access Control Module (`identity` schema)

#### 6. `identity.UserAccounts`
Login credentials and authentication state. Strictly separated from `Employees`.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Internal identifier.
- `PublicId` (UNIQUEIDENTIFIER, UNIQUE, NOT NULL, DEFAULT NEWSEQUENTIALID()): External reference.
- `EmployeeId` (BIGINT, NULL, FK -> `organization.Employees.Id`, UNIQUE): Linked employee profile (optional for system service accounts).
- `Username` (VARCHAR(100), NOT NULL, UNIQUE): Unique login name.
- `Email` (VARCHAR(150), NOT NULL, UNIQUE): Primary account email.
- `PasswordHash` (VARCHAR(500), NOT NULL): Cryptographically hashed password (PBKDF2/Argon2/bcrypt).
- `SecurityStamp` (VARCHAR(100), NOT NULL): Random stamp invalidated upon password/role modifications.
- `IsActive` (BIT, NOT NULL, DEFAULT 1): Whether account is enabled.
- `IsLockedOut` (BIT, NOT NULL, DEFAULT 0): Brute-force lockout flag.
- `LockoutEndAt` (DATETIME2, NULL): Expiration of account lockout.
- `FailedLoginAttempts` (INT, NOT NULL, DEFAULT 0): Consecutive failed password counter.
- `LastLoginAt` (DATETIME2, NULL): Last successful authentication timestamp.
- `CreatedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Account creation timestamp.
- `CreatedBy` (NVARCHAR(100), NULL): Creator user.
- `UpdatedAt` (DATETIME2, NULL): Update timestamp.
- `UpdatedBy` (NVARCHAR(100), NULL): Updater user.
- `RowVersion` (ROWVERSION, NOT NULL): Concurrency token.

#### 7. `identity.Roles`
System roles for Role-Based Access Control (RBAC).
- `Id` (BIGINT, PK, IDENTITY(1,1)): Internal identifier.
- `PublicId` (UNIQUEIDENTIFIER, UNIQUE, NOT NULL, DEFAULT NEWSEQUENTIALID()): External reference.
- `Code` (VARCHAR(50), NOT NULL, UNIQUE): Unique role identifier (e.g., `SUPER_ADMIN`, `DISPATCHER`, `COURIER`, `WAREHOUSE_OP`).
- `Name` (NVARCHAR(100), NOT NULL): Human-readable name.
- `Description` (NVARCHAR(500), NULL): Scope description.
- `IsSystem` (BIT, NOT NULL, DEFAULT 0): Prevents deletion of core system roles.
- `IsActive` (BIT, NOT NULL, DEFAULT 1): Active flag.
- `CreatedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Creation timestamp.
- `CreatedBy` (NVARCHAR(100), NULL): Creator user.
- `UpdatedAt` (DATETIME2, NULL): Update timestamp.
- `UpdatedBy` (NVARCHAR(100), NULL): Updater user.
- `RowVersion` (ROWVERSION, NOT NULL): Concurrency token.

#### 8. `identity.Permissions`
Atomic functional entitlements within the application.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Internal identifier.
- `PublicId` (UNIQUEIDENTIFIER, UNIQUE, NOT NULL, DEFAULT NEWSEQUENTIALID()): External reference.
- `Module` (VARCHAR(50), NOT NULL): Grouping module (`Orders`, `Shipments`, `Inventory`, `Finance`, `Users`).
- `Code` (VARCHAR(100), NOT NULL, UNIQUE): Canonical permission string (e.g., `shipments.create`, `inventory.adjust`, `finance.remit_cod`).
- `Name` (NVARCHAR(150), NOT NULL): Friendly permission name.
- `Description` (NVARCHAR(500), NULL): Details on granted actions.
- `CreatedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Creation timestamp.

#### 9. `identity.UserRoles`
Many-to-many junction binding users to roles.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `UserId` (BIGINT, NOT NULL, FK -> `identity.UserAccounts.Id`): User account reference.
- `RoleId` (BIGINT, NOT NULL, FK -> `identity.Roles.Id`): Assigned role.
- `AssignedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Assignment timestamp.
- `AssignedBy` (NVARCHAR(100), NULL): Granting administrator username.
- *Unique Constraint*: `(UserId, RoleId)`.

#### 10. `identity.RolePermissions`
Many-to-many junction granting atomic permissions to roles.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `RoleId` (BIGINT, NOT NULL, FK -> `identity.Roles.Id`): Target role.
- `PermissionId` (BIGINT, NOT NULL, FK -> `identity.Permissions.Id`): Entitled permission.
- `GrantedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Grant timestamp.
- `GrantedBy` (NVARCHAR(100), NULL): Granting administrator.
- *Unique Constraint*: `(RoleId, PermissionId)`.

#### 11. `identity.UserBranchAccess`
Restricts user operations to authorized company branches. Users may be granted access to multiple branches.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `UserId` (BIGINT, NOT NULL, FK -> `identity.UserAccounts.Id`): Target user.
- `BranchId` (BIGINT, NOT NULL, FK -> `organization.Branches.Id`): Permitted branch.
- `IsDefault` (BIT, NOT NULL, DEFAULT 0): Whether this is the user's primary operating branch upon login.
- `GrantedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Grant timestamp.
- `GrantedBy` (NVARCHAR(100), NULL): Granting administrator.
- *Unique Constraint*: `(UserId, BranchId)`.

#### 12. `identity.RefreshTokens`
Cryptographically random tokens supporting JWT session renewal without storing cleartext tokens.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `UserId` (BIGINT, NOT NULL, FK -> `identity.UserAccounts.Id`): Owning user account.
- `TokenHash` (VARCHAR(256), NOT NULL, UNIQUE): SHA-256 hash of the issued refresh token.
- `ExpiresAt` (DATETIME2, NOT NULL): Expiration date/time in UTC.
- `IsRevoked` (BIT, NOT NULL, DEFAULT 0): Invalidation flag.
- `RevokedAt` (DATETIME2, NULL): Revocation timestamp.
- `ReplacedByToken` (VARCHAR(256), NULL): Invalidation rotation trail.
- `CreatedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Issuance timestamp.
- `CreatedByIp` (VARCHAR(50), NULL): Client IP address at token creation.

#### 13. `identity.UserSessions`
Tracks active client sessions across desktop client instances.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `UserId` (BIGINT, NOT NULL, FK -> `identity.UserAccounts.Id`): Session owner.
- `SessionTokenHash` (VARCHAR(256), NOT NULL, UNIQUE): Hash of the active session token.
- `DeviceInfo` (NVARCHAR(250), NULL): Client workstation hostname, OS version, app version.
- `IpAddress` (VARCHAR(50), NOT NULL): Client network IP address.
- `StartedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Session initiation time.
- `LastActivityAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Heartbeat / last request time.
- `ExpiresAt` (DATETIME2, NOT NULL): Session timeout threshold.
- `IsTerminated` (BIT, NOT NULL, DEFAULT 0): Termination flag.

#### 14. `identity.LoginHistories`
Append-only log of all authentication attempts for compliance and anomaly detection.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `UserId` (BIGINT, NULL, FK -> `identity.UserAccounts.Id`): Resolved user account (if recognized).
- `UsernameAttempted` (VARCHAR(100), NOT NULL): The exact username passed in the login request.
- `IpAddress` (VARCHAR(50), NOT NULL): Client network address.
- `UserAgent` (NVARCHAR(300), NULL): Client software signature.
- `IsSuccessful` (BIT, NOT NULL): Success/failure outcome.
- `FailureReason` (NVARCHAR(200), NULL): Reason for denial (`InvalidCredentials`, `AccountLocked`, `IpBlocked`).
- `AttemptedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): UTC timestamp.

---

### 4.3 Configuration and Reference Data Module (`configuration` schema)

#### 15. `configuration.Countries`
Master reference table of sovereign nations.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `Code` (VARCHAR(2), NOT NULL, UNIQUE): ISO 3166-1 alpha-2 code (e.g., `VN`, `US`, `SG`).
- `Alpha3Code` (VARCHAR(3), NOT NULL, UNIQUE): ISO 3166-1 alpha-3 code (e.g., `VNM`, `USA`).
- `Name` (NVARCHAR(150), NOT NULL): Common country name.
- `NativeName` (NVARCHAR(150), NULL): Name in native language.
- `NumericCode` (INT, NULL): ISO 3166-1 numeric code.
- `PhonePrefix` (VARCHAR(10), NULL): International calling prefix (e.g., `+84`).
- `IsActive` (BIT, NOT NULL, DEFAULT 1): Active status.

#### 16. `configuration.Provinces`
Vietnamese provinces / centrally-governed cities (Tỉnh / Thành phố trực thuộc Trung ương).
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `CountryId` (BIGINT, NOT NULL, FK -> `configuration.Countries.Id`): Parent nation.
- `Code` (VARCHAR(20), NOT NULL, UNIQUE): Official administrative code (e.g., `01` for Hanoi, `79` for HCMC).
- `Name` (NVARCHAR(150), NOT NULL): Official province name.
- `Type` (NVARCHAR(50), NOT NULL): Category (`Thành phố Trung ương`, `Tỉnh`).
- `IsActive` (BIT, NOT NULL, DEFAULT 1): Active flag.

#### 17. `configuration.Districts`
Urban districts / rural districts / provincial cities (Quận / Huyện / Thị xã / Thành phố thuộc tỉnh).
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `ProvinceId` (BIGINT, NOT NULL, FK -> `configuration.Provinces.Id`): Parent province.
- `Code` (VARCHAR(20), NOT NULL, UNIQUE): Official district administrative code.
- `Name` (NVARCHAR(150), NOT NULL): Official district name.
- `Type` (NVARCHAR(50), NOT NULL): Category (`Quận`, `Huyện`, `Thị xã`, `Thành phố`).
- `IsActive` (BIT, NOT NULL, DEFAULT 1): Active flag.

#### 18. `configuration.Wards`
Wards / Communes / Townships (Phường / Xã / Thị trấn).
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `DistrictId` (BIGINT, NOT NULL, FK -> `configuration.Districts.Id`): Parent district.
- `Code` (VARCHAR(20), NOT NULL, UNIQUE): Official ward administrative code.
- `Name` (NVARCHAR(150), NOT NULL): Official ward name.
- `Type` (NVARCHAR(50), NOT NULL): Category (`Phường`, `Xã`, `Thị trấn`).
- `IsActive` (BIT, NOT NULL, DEFAULT 1): Active flag.

#### 19. `configuration.Currencies`
ISO currency definitions for multi-currency transactions.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `Code` (VARCHAR(3), NOT NULL, UNIQUE): ISO 4217 code (e.g., `VND`, `USD`, `EUR`).
- `Name` (NVARCHAR(100), NOT NULL): Currency name.
- `Symbol` (NVARCHAR(10), NOT NULL): Display symbol (e.g., `₫`, `$`).
- `DecimalPlaces` (INT, NOT NULL, DEFAULT 0): Decimal precision (0 for VND, 2 for USD).
- `ExchangeRateToDefault` (DECIMAL(18,6), NOT NULL, DEFAULT 1.0): Conversion factor to company base currency.
- `IsDefault` (BIT, NOT NULL, DEFAULT 0): Base operating currency indicator.
- `IsActive` (BIT, NOT NULL, DEFAULT 1): Active flag.

#### 20. `configuration.UnitsOfMeasure`
Physical units of measurement for weights, volumes, packages, and items.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `Code` (VARCHAR(20), NOT NULL, UNIQUE): Short code (e.g., `KG`, `G`, `CM`, `M`, `CBM`, `PCS`, `BOX`).
- `Name` (NVARCHAR(100), NOT NULL): Full unit name.
- `Type` (VARCHAR(30), NOT NULL): Physical dimension (`Weight`, `Dimension`, `Volume`, `Count`).
- `ConversionFactor` (DECIMAL(18,6), NOT NULL, DEFAULT 1.0): Multiplier relative to base unit of the dimension.
- `IsBaseUnit` (BIT, NOT NULL, DEFAULT 0): Baseline unit flag.
- `IsActive` (BIT, NOT NULL, DEFAULT 1): Active flag.

#### 21. `configuration.SystemSettings`
Dynamically configurable application and business parameters.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `Category` (VARCHAR(50), NOT NULL): Setting category (`General`, `Shipping`, `Finance`, `Security`).
- `Key` (VARCHAR(100), NOT NULL, UNIQUE): Unique configuration key.
- `Value` (NVARCHAR(MAX), NOT NULL): Serialized configuration value.
- `Description` (NVARCHAR(500), NULL): Documentation of setting behavior.
- `ValueType` (VARCHAR(30), NOT NULL): Data format (`String`, `Integer`, `Decimal`, `Boolean`, `Json`).
- `IsEncrypted` (BIT, NOT NULL, DEFAULT 0): Indicates sensitive encrypted values.
- `UpdatedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Timestamp of update.
- `UpdatedBy` (NVARCHAR(100), NULL): User who modified the setting.

#### 22. `configuration.NumberSequences`
High-concurrency document sequence number generator for orders, waybills, invoices, receipts, and batches.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `Scope` (VARCHAR(50), NOT NULL, UNIQUE): Document type (e.g., `ORDER`, `WAYBILL`, `INVOICE`, `RECEIPT`, `DISPATCH`).
- `Prefix` (VARCHAR(20), NOT NULL): Fixed string prefix (e.g., `ORD`, `WB`, `INV`).
- `Suffix` (VARCHAR(20), NULL): Optional trailing token.
- `NextNumber` (BIGINT, NOT NULL, DEFAULT 1): Next integer value to allocate.
- `Increment` (INT, NOT NULL, DEFAULT 1): Increment interval.
- `NumberLength` (INT, NOT NULL, DEFAULT 8): Zero-padding length.
- `CurrentFormat` (VARCHAR(50), NOT NULL): Format pattern (e.g., `{PREFIX}-{YYYYMM}-{NUMBER}`).
- `ResetFrequency` (VARCHAR(20), NOT NULL, DEFAULT 'None'): Reset cycle (`None`, `Yearly`, `Monthly`, `Daily`).
- `LastResetDate` (DATE, NULL): Date of most recent sequence reset.

---

### 4.4 CRM Module (`crm` schema)

#### 23. `crm.Addresses`
Normalized geographic addresses supporting geo-coordinates, administrative IDs, and formatted prose.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `PublicId` (UNIQUEIDENTIFIER, UNIQUE, NOT NULL, DEFAULT NEWSEQUENTIALID()): External reference.
- `ContactName` (NVARCHAR(150), NOT NULL): Resident / Facility contact name.
- `PhoneNumber` (VARCHAR(50), NOT NULL): Phone contact for delivery/pickup.
- `Email` (VARCHAR(150), NULL): Contact email.
- `AddressLine1` (NVARCHAR(250), NOT NULL): House number, alley, street name.
- `AddressLine2` (NVARCHAR(250), NULL): Building, floor, room number.
- `CountryId` (BIGINT, NOT NULL, FK -> `configuration.Countries.Id`): Sovereign nation.
- `ProvinceId` (BIGINT, NULL, FK -> `configuration.Provinces.Id`): Province / City.
- `DistrictId` (BIGINT, NULL, FK -> `configuration.Districts.Id`): District.
- `WardId` (BIGINT, NULL, FK -> `configuration.Wards.Id`): Ward / Commune.
- `PostalCode` (VARCHAR(20), NULL): Postal / ZIP code.
- `Latitude` (DECIMAL(10,7), NULL): GPS latitude coordinates.
- `Longitude` (DECIMAL(10,7), NULL): GPS longitude coordinates.
- `FormattedAddress` (NVARCHAR(500), NOT NULL): Full denormalized search/print string.
- `CreatedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Creation timestamp.
- `CreatedBy` (NVARCHAR(100), NULL): Creator user.
- `UpdatedAt` (DATETIME2, NULL): Update timestamp.
- `UpdatedBy` (NVARCHAR(100), NULL): Updater user.

#### 24. `crm.Customers`
Client master records: accounts for both corporate shippers (B2B) and individual consumers (B2C).
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `PublicId` (UNIQUEIDENTIFIER, UNIQUE, NOT NULL, DEFAULT NEWSEQUENTIALID()): External reference.
- `BranchId` (BIGINT, NOT NULL, FK -> `organization.Branches.Id`): Managing company branch.
- `CustomerCode` (VARCHAR(50), NOT NULL, UNIQUE): Unique customer code (e.g., `CUST-100293`).
- `CustomerType` (VARCHAR(30), NOT NULL): Client classification (`Individual`, `Corporate`).
- `Name` (NVARCHAR(200), NOT NULL): Full legal customer name.
- `TradeName` (NVARCHAR(200), NULL): Doing-Business-As commercial brand name.
- `TaxCode` (VARCHAR(50), NULL): Corporate tax number.
- `Email` (VARCHAR(150), NOT NULL): Primary contact email.
- `PhoneNumber` (VARCHAR(50), NOT NULL): Primary contact phone.
- `CreditLimit` (DECIMAL(19,4), NOT NULL, DEFAULT 0): Maximum approved outstanding credit.
- `CreditTermDays` (INT, NOT NULL, DEFAULT 0): Payment term days (0 for prepaid).
- `PaymentType` (VARCHAR(30), NOT NULL, DEFAULT 'Prepaid'): Term policy (`Prepaid`, `PostpaidMonthly`, `CodOffset`).
- `AccountManagerEmployeeId` (BIGINT, NULL, FK -> `organization.Employees.Id`): Assigned sales representative.
- `IsActive` (BIT, NOT NULL, DEFAULT 1): Active customer flag.
- `CreatedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Creation timestamp.
- `CreatedBy` (NVARCHAR(100), NULL): Creator username.
- `UpdatedAt` (DATETIME2, NULL): Update timestamp.
- `UpdatedBy` (NVARCHAR(100), NULL): Updater username.
- `RowVersion` (ROWVERSION, NOT NULL): Concurrency token.

#### 25. `crm.CustomerContacts`
Authorized representatives, logistics coordinators, and billing contacts for a customer.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `PublicId` (UNIQUEIDENTIFIER, UNIQUE, NOT NULL, DEFAULT NEWSEQUENTIALID()): External reference.
- `CustomerId` (BIGINT, NOT NULL, FK -> `crm.Customers.Id`): Parent customer.
- `ContactName` (NVARCHAR(150), NOT NULL): Contact person's full name.
- `JobTitle` (NVARCHAR(100), NULL): Role / Title within client company.
- `PhoneNumber` (VARCHAR(50), NOT NULL): Direct mobile or desk phone.
- `Email` (VARCHAR(150), NULL): Direct email address.
- `IsPrimary` (BIT, NOT NULL, DEFAULT 0): Primary operational liaison flag.
- `Notes` (NVARCHAR(500), NULL): Miscellaneous contact notes.
- `CreatedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Creation timestamp.
- `CreatedBy` (NVARCHAR(100), NULL): Creator user.
- `UpdatedAt` (DATETIME2, NULL): Update timestamp.
- `UpdatedBy` (NVARCHAR(100), NULL): Updater user.

#### 26. `crm.CustomerAddresses`
Junction connecting customers to their standard shipping, recipient, billing, and warehouse pickup locations.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `PublicId` (UNIQUEIDENTIFIER, UNIQUE, NOT NULL, DEFAULT NEWSEQUENTIALID()): External reference.
- `CustomerId` (BIGINT, NOT NULL, FK -> `crm.Customers.Id`): Parent customer.
- `AddressId` (BIGINT, NOT NULL, FK -> `crm.Addresses.Id`): Linked geographic address.
- `AddressType` (VARCHAR(30), NOT NULL): Role (`Sender`, `Recipient`, `Billing`, `PickupWarehouse`).
- `IsDefault` (BIT, NOT NULL, DEFAULT 0): Default selection flag for the specified type.
- `AddressLabel` (NVARCHAR(100), NULL): User-friendly tag (e.g., `Main Hanoi Warehouse`, `HCMC Factory Gate 2`).
- `CreatedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Creation timestamp.
- `CreatedBy` (NVARCHAR(100), NULL): Creator user.
- `UpdatedAt` (DATETIME2, NULL): Update timestamp.
- `UpdatedBy` (NVARCHAR(100), NULL): Updater user.

#### 27. `crm.CustomerContracts`
Legal service contracts establishing pricing terms, SLAs, and credit terms for corporate clients.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `PublicId` (UNIQUEIDENTIFIER, UNIQUE, NOT NULL, DEFAULT NEWSEQUENTIALID()): External reference.
- `CustomerId` (BIGINT, NOT NULL, FK -> `crm.Customers.Id`): Contracting customer.
- `ContractNumber` (VARCHAR(100), NOT NULL, UNIQUE): Unique contractual identifier.
- `Title` (NVARCHAR(200), NOT NULL): Contract title.
- `StartDate` (DATE, NOT NULL): Effective commencement date.
- `EndDate` (DATE, NOT NULL): Expiration date.
- `Status` (VARCHAR(30), NOT NULL, DEFAULT 'Draft'): Status (`Draft`, `Active`, `Expired`, `Terminated`).
- `DiscountRate` (DECIMAL(5,2), NOT NULL, DEFAULT 0): Overall negotiated discount percentage.
- `PaymentTermDays` (INT, NOT NULL, DEFAULT 30): Credit days granted under this contract.
- `SignedAt` (DATE, NULL): Formal signature date.
- `FileAttachmentId` (BIGINT, NULL, FK -> `audit.FileAttachments.Id`): Scanned contract document.
- `CreatedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Creation timestamp.
- `CreatedBy` (NVARCHAR(100), NULL): Creator user.
- `UpdatedAt` (DATETIME2, NULL): Update timestamp.
- `UpdatedBy` (NVARCHAR(100), NULL): Updater user.
- `RowVersion` (ROWVERSION, NOT NULL): Concurrency token.

#### 28. `crm.CustomerPricingRules`
Customer-specific rate overrides, fuel surcharge exceptions, and volume discounts.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `PublicId` (UNIQUEIDENTIFIER, UNIQUE, NOT NULL, DEFAULT NEWSEQUENTIALID()): External reference.
- `CustomerId` (BIGINT, NOT NULL, FK -> `crm.Customers.Id`): Beneficiary customer.
- `CustomerContractId` (BIGINT, NULL, FK -> `crm.CustomerContracts.Id`): Governing legal contract.
- `ShippingRateCardId` (BIGINT, NULL, FK -> `finance.ShippingRateCards.Id`): Custom rate card assigned.
- `Name` (NVARCHAR(150), NOT NULL): Rule description (e.g., `15% Discount on Express North-South`).
- `DiscountPercent` (DECIMAL(5,2), NOT NULL, DEFAULT 0): Percentage discount.
- `FlatDiscountAmount` (DECIMAL(19,4), NOT NULL, DEFAULT 0): Flat deduction per shipment.
- `Priority` (INT, NOT NULL, DEFAULT 1): Evaluation order precedence.
- `EffectiveFrom` (DATETIME2, NOT NULL): Start date/time.
- `EffectiveTo` (DATETIME2, NULL): End date/time.
- `IsActive` (BIT, NOT NULL, DEFAULT 1): Active flag.
- `CreatedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Creation timestamp.
- `CreatedBy` (NVARCHAR(100), NULL): Creator user.
- `UpdatedAt` (DATETIME2, NULL): Update timestamp.
- `UpdatedBy` (NVARCHAR(100), NULL): Updater user.

---

### 4.5 Catalog Module (`catalog` schema)

#### 29. `catalog.ProductCategories`
Hierarchical product and parcel classification hierarchy.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `PublicId` (UNIQUEIDENTIFIER, UNIQUE, NOT NULL, DEFAULT NEWSEQUENTIALID()): External reference.
- `ParentCategoryId` (BIGINT, NULL, FK -> `catalog.ProductCategories.Id`): Parent category in hierarchy.
- `Code` (VARCHAR(50), NOT NULL, UNIQUE): Unique category code.
- `Name` (NVARCHAR(150), NOT NULL): Category title.
- `Description` (NVARCHAR(500), NULL): Detailed explanation.
- `IsActive` (BIT, NOT NULL, DEFAULT 1): Active flag.
- `CreatedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Creation timestamp.
- `CreatedBy` (NVARCHAR(100), NULL): Creator user.
- `UpdatedAt` (DATETIME2, NULL): Update timestamp.
- `UpdatedBy` (NVARCHAR(100), NULL): Updater user.

#### 30. `catalog.Products`
Product definitions and standardized parcel types. Distinct from physical inventory balances.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `PublicId` (UNIQUEIDENTIFIER, UNIQUE, NOT NULL, DEFAULT NEWSEQUENTIALID()): External reference.
- `CategoryId` (BIGINT, NOT NULL, FK -> `catalog.ProductCategories.Id`): Product category.
- `Sku` (VARCHAR(100), NOT NULL, UNIQUE): Stock Keeping Unit code.
- `Barcode` (VARCHAR(100), NULL): EAN/UPC/Code128 barcode string.
- `Name` (NVARCHAR(200), NOT NULL): Standard product or parcel description.
- `Description` (NVARCHAR(1000), NULL): Detailed item specifications.
- `UnitOfMeasureId` (BIGINT, NOT NULL, FK -> `configuration.UnitsOfMeasure.Id`): Packaging / trading unit.
- `WeightKg` (DECIMAL(12,3), NOT NULL, DEFAULT 0): Standard physical unit weight.
- `LengthCm` (DECIMAL(12,3), NOT NULL, DEFAULT 0): Standard length.
- `WidthCm` (DECIMAL(12,3), NOT NULL, DEFAULT 0): Standard width.
- `HeightCm` (DECIMAL(12,3), NOT NULL, DEFAULT 0): Standard height.
- `DeclaredValue` (DECIMAL(19,4), NOT NULL, DEFAULT 0): Default declared item value.
- `CurrencyId` (BIGINT, NOT NULL, FK -> `configuration.Currencies.Id`): Currency for declared value.
- `IsPerishable` (BIT, NOT NULL, DEFAULT 0): Flag for temperature/time-sensitive goods.
- `IsDangerous` (BIT, NOT NULL, DEFAULT 0): Flag for hazardous materials (HAZMAT).
- `IsActive` (BIT, NOT NULL, DEFAULT 1): Active item flag.
- `CreatedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Creation timestamp.
- `CreatedBy` (NVARCHAR(100), NULL): Creator user.
- `UpdatedAt` (DATETIME2, NULL): Update timestamp.
- `UpdatedBy` (NVARCHAR(100), NULL): Updater user.
- `RowVersion` (ROWVERSION, NOT NULL): Concurrency token.

---

### 4.6 Sales and Orders Module (`sales` schema)

#### 31. `sales.Orders`
Customer transport and fulfillment request. Represents the customer order before physical movement.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `PublicId` (UNIQUEIDENTIFIER, UNIQUE, NOT NULL, DEFAULT NEWSEQUENTIALID()): External reference.
- `OrderNumber` (VARCHAR(50), NOT NULL, UNIQUE): Human-readable order code (e.g., `ORD-202611-0001`).
- `CustomerId` (BIGINT, NOT NULL, FK -> `crm.Customers.Id`): Booking customer.
- `BranchId` (BIGINT, NOT NULL, FK -> `organization.Branches.Id`): Booking branch office.
- `OrderDate` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Commercial order timestamp.
- `RequestedPickupDate` (DATETIME2, NULL): Customer's requested collection window.
- `RequiredDeliveryDate` (DATETIME2, NULL): Target delivery deadline.
- `OrderStatus` (VARCHAR(50), NOT NULL, DEFAULT 'Draft'): Operational status (`Draft`, `Confirmed`, `Processing`, `PartiallyShipped`, `Shipped`, `Delivered`, `Cancelled`).
- `PaymentStatus` (VARCHAR(50), NOT NULL, DEFAULT 'Pending'): Financial payment status (`Pending`, `Paid`, `Partial`, `Invoiced`).
- `TotalWeightKg` (DECIMAL(12,3), NOT NULL, DEFAULT 0): Aggregated consignment weight.
- `TotalVolumeCbm` (DECIMAL(12,3), NOT NULL, DEFAULT 0): Aggregated volume in cubic meters.
- `DeclaredValue` (DECIMAL(19,4), NOT NULL, DEFAULT 0): Total declared insurance value.
- `CurrencyId` (BIGINT, NOT NULL, FK -> `configuration.Currencies.Id`): Currency for declared value.
- `CodAmount` (DECIMAL(19,4), NOT NULL, DEFAULT 0): Total Cash on Delivery requested by sender.
- `Notes` (NVARCHAR(1000), NULL): Customer instructions.
- `CancellationReason` (NVARCHAR(500), NULL): Reason if order was cancelled.
- `CancelledAt` (DATETIME2, NULL): UTC cancellation time.
- `CancelledBy` (NVARCHAR(100), NULL): User who authorized cancellation.
- `CreatedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Creation timestamp.
- `CreatedBy` (NVARCHAR(100), NULL): Creator user.
- `UpdatedAt` (DATETIME2, NULL): Update timestamp.
- `UpdatedBy` (NVARCHAR(100), NULL): Updater user.
- `RowVersion` (ROWVERSION, NOT NULL): Concurrency token.

#### 32. `sales.OrderItems`
Line items detailed in an order.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `PublicId` (UNIQUEIDENTIFIER, UNIQUE, NOT NULL, DEFAULT NEWSEQUENTIALID()): External reference.
- `OrderId` (BIGINT, NOT NULL, FK -> `sales.Orders.Id`): Parent order.
- `ProductId` (BIGINT, NULL, FK -> `catalog.Products.Id`): Linked catalog product (optional).
- `ProductName` (NVARCHAR(200), NOT NULL): Exact description of goods.
- `Sku` (VARCHAR(100), NULL): SKU reference.
- `Quantity` (INT, NOT NULL): Item quantity count (> 0).
- `UnitOfMeasureId` (BIGINT, NOT NULL, FK -> `configuration.UnitsOfMeasure.Id`): Unit of measurement.
- `UnitPrice` (DECIMAL(19,4), NOT NULL, DEFAULT 0): Unit declared sales value.
- `TotalPrice` (DECIMAL(19,4), NOT NULL, DEFAULT 0): Total line declared value (`Quantity * UnitPrice`).
- `DeclaredValue` (DECIMAL(19,4), NOT NULL, DEFAULT 0): Insurance declared value for this line.
- `WeightKg` (DECIMAL(12,3), NOT NULL, DEFAULT 0): Total weight of line items.
- `Notes` (NVARCHAR(500), NULL): Handling remarks.

#### 33. `sales.OrderAddresses`
Historical snapshot of address information preserved at order creation to protect against customer profile changes.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `OrderId` (BIGINT, NOT NULL, FK -> `sales.Orders.Id`): Parent order.
- `AddressType` (VARCHAR(30), NOT NULL): Role (`Sender`, `Recipient`, `Pickup`).
- `ContactName` (NVARCHAR(150), NOT NULL): Contact person name at time of order.
- `PhoneNumber` (VARCHAR(50), NOT NULL): Phone number at time of order.
- `Email` (VARCHAR(150), NULL): Email address.
- `AddressLine1` (NVARCHAR(250), NOT NULL): Street address.
- `AddressLine2` (NVARCHAR(250), NULL): Extended details.
- `CountryId` (BIGINT, NOT NULL, FK -> `configuration.Countries.Id`): Country.
- `ProvinceId` (BIGINT, NULL, FK -> `configuration.Provinces.Id`): Province.
- `DistrictId` (BIGINT, NULL, FK -> `configuration.Districts.Id`): District.
- `WardId` (BIGINT, NULL, FK -> `configuration.Wards.Id`): Ward.
- `PostalCode` (VARCHAR(20), NULL): Postal code.
- `FormattedAddress` (NVARCHAR(500), NOT NULL): Complete denormalized address string.
- *Unique Constraint*: `(OrderId, AddressType)`.

#### 34. `sales.OrderPackages`
Physical package specifications requested under an order.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `PublicId` (UNIQUEIDENTIFIER, UNIQUE, NOT NULL, DEFAULT NEWSEQUENTIALID()): External reference.
- `OrderId` (BIGINT, NOT NULL, FK -> `sales.Orders.Id`): Parent order.
- `PackageCode` (VARCHAR(50), NOT NULL): Sequential package code within order (e.g., `PKG-01`).
- `WeightKg` (DECIMAL(12,3), NOT NULL): Actual gross weight.
- `LengthCm` (DECIMAL(12,3), NOT NULL): Package length.
- `WidthCm` (DECIMAL(12,3), NOT NULL): Package width.
- `HeightCm` (DECIMAL(12,3), NOT NULL): Package height.
- `VolumetricWeightKg` (DECIMAL(12,3), NOT NULL): Standard volumetric weight `(L * W * H / 5000)`.
- `DeclaredValue` (DECIMAL(19,4), NOT NULL, DEFAULT 0): Declared insurance value for this parcel.
- `Description` (NVARCHAR(500), NULL): Package contents description.
- `Barcode` (VARCHAR(100), NULL): Manufacturer or customer barcode on parcel.
- `SpecialInstructions` (NVARCHAR(500), NULL): Fragile, handle with care, keep upright, etc.

---

### 4.7 Shipping Module (`shipping` schema)

#### 35. `shipping.Shipments`
The operational unit used to move freight. Associated with an order (or created directly for walk-in counter shipments).
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `PublicId` (UNIQUEIDENTIFIER, UNIQUE, NOT NULL, DEFAULT NEWSEQUENTIALID()): External reference.
- `TrackingNumber` (VARCHAR(50), NOT NULL, UNIQUE): Unique public master tracking number (e.g., `LMS-VN-89472918`).
- `OrderId` (BIGINT, NULL, FK -> `sales.Orders.Id`): Associated commercial order.
- `CustomerId` (BIGINT, NOT NULL, FK -> `crm.Customers.Id`): Billing/Shipper customer.
- `OriginBranchId` (BIGINT, NOT NULL, FK -> `organization.Branches.Id`): Originating dispatch branch.
- `DestinationBranchId` (BIGINT, NOT NULL, FK -> `organization.Branches.Id`): Final destination delivery branch.
- `AssignedCarrierId` (BIGINT, NULL, FK -> `carrier.Carriers.Id`): External carrier handling the transit (NULL if internal fleet).
- `CarrierServiceId` (BIGINT, NULL, FK -> `carrier.CarrierServices.Id`): External carrier service tier.
- `CarrierTrackingNumber` (VARCHAR(100), NULL): Waybill number assigned by external carrier.
- `ShipmentStatus` (VARCHAR(50), NOT NULL, DEFAULT 'Draft'): State machine milestone:
  (`Draft`, `ReadyForPickup`, `PickedUp`, `InSortingHub`, `InTransit`, `OutForDelivery`, `Delivered`, `DeliveryFailed`, `Returning`, `Returned`, `Cancelled`).
- `ServiceType` (VARCHAR(50), NOT NULL): Internal service speed (`SameDay`, `NextDayExpress`, `StandardGround`, `Economical`).
- `TotalPackages` (INT, NOT NULL, DEFAULT 1): Number of physical packages comprising this consignment.
- `ActualWeightKg` (DECIMAL(12,3), NOT NULL): Weighed total scale weight.
- `ChargeableWeightKg` (DECIMAL(12,3), NOT NULL): `MAX(ActualWeightKg, VolumetricWeightKg)`.
- `DeclaredValue` (DECIMAL(19,4), NOT NULL, DEFAULT 0): Insured value.
- `CodAmount` (DECIMAL(19,4), NOT NULL, DEFAULT 0): Cash on Delivery amount to collect from consignee.
- `ShippingFee` (DECIMAL(19,4), NOT NULL, DEFAULT 0): Base freight fee.
- `InsuranceFee` (DECIMAL(19,4), NOT NULL, DEFAULT 0): Cargo insurance premium.
- `SurchargesTotal` (DECIMAL(19,4), NOT NULL, DEFAULT 0): Aggregated fuel/remote area surcharges.
- `TotalAmount` (DECIMAL(19,4), NOT NULL, DEFAULT 0): Total payable shipping charge.
- `CurrencyId` (BIGINT, NOT NULL, FK -> `configuration.Currencies.Id`): Billing currency.
- `PickupDate` (DATETIME2, NULL): Actual time parcel was received into logistics custody.
- `EstimatedDeliveryDate` (DATETIME2, NULL): SLA delivery expectation.
- `ActualDeliveryDate` (DATETIME2, NULL): Completion timestamp.
- `Notes` (NVARCHAR(1000), NULL): Dispatching notes.
- `CreatedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Creation timestamp.
- `CreatedBy` (NVARCHAR(100), NULL): Creator user.
- `UpdatedAt` (DATETIME2, NULL): Update timestamp.
- `UpdatedBy` (NVARCHAR(100), NULL): Updater user.
- `RowVersion` (ROWVERSION, NOT NULL): Concurrency token.

#### 36. `shipping.ShipmentPackages`
Physical parcels contained within a shipment.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `PublicId` (UNIQUEIDENTIFIER, UNIQUE, NOT NULL, DEFAULT NEWSEQUENTIALID()): External reference.
- `ShipmentId` (BIGINT, NOT NULL, FK -> `shipping.Shipments.Id`): Master shipment.
- `OrderPackageId` (BIGINT, NULL, FK -> `sales.OrderPackages.Id`): Original order package specification.
- `PackageCode` (VARCHAR(50), NOT NULL): Unique package identifier within the shipment (e.g., `PKG-01/02`).
- `Barcode` (VARCHAR(100), NOT NULL, UNIQUE): Unique scan barcode printed on the package label.
- `WeightKg` (DECIMAL(12,3), NOT NULL): Scaled weight.
- `LengthCm` (DECIMAL(12,3), NOT NULL): Measured length.
- `WidthCm` (DECIMAL(12,3), NOT NULL): Measured width.
- `HeightCm` (DECIMAL(12,3), NOT NULL): Measured height.
- `VolumetricWeightKg` (DECIMAL(12,3), NOT NULL): Calculated volumetric weight.
- `PackageStatus` (VARCHAR(50), NOT NULL, DEFAULT 'Created'): State (`Created`, `Received`, `Sorted`, `InTransit`, `Delivered`, `Damaged`, `Lost`).
- `SealNumber` (VARCHAR(50), NULL): Security tamper-evident seal number.
- `CreatedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Creation timestamp.

#### 37. `shipping.ShipmentAddresses`
Immutable address records representing sender, recipient, and return addresses for this specific shipment.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `ShipmentId` (BIGINT, NOT NULL, FK -> `shipping.Shipments.Id`): Parent shipment.
- `AddressType` (VARCHAR(30), NOT NULL): Role (`Sender`, `Recipient`, `ReturnAddress`).
- `ContactName` (NVARCHAR(150), NOT NULL): Contact name.
- `PhoneNumber` (VARCHAR(50), NOT NULL): Contact telephone.
- `Email` (VARCHAR(150), NULL): Contact email.
- `AddressLine1` (NVARCHAR(250), NOT NULL): Street address.
- `AddressLine2` (NVARCHAR(250), NULL): Suite/unit details.
- `CountryId` (BIGINT, NOT NULL, FK -> `configuration.Countries.Id`): Country.
- `ProvinceId` (BIGINT, NULL, FK -> `configuration.Provinces.Id`): Province.
- `DistrictId` (BIGINT, NULL, FK -> `configuration.Districts.Id`): District.
- `WardId` (BIGINT, NULL, FK -> `configuration.Wards.Id`): Ward.
- `PostalCode` (VARCHAR(20), NULL): Postal code.
- `FormattedAddress` (NVARCHAR(500), NOT NULL): Full address print text.
- *Unique Constraint*: `(ShipmentId, AddressType)`.

#### 38. `shipping.ShipmentStatusHistories`
Append-only immutable audit trail of every status transition and location checkpoint a shipment undergoes.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `ShipmentId` (BIGINT, NOT NULL, FK -> `shipping.Shipments.Id`): Master shipment.
- `Status` (VARCHAR(50), NOT NULL): New status milestone.
- `SubStatus` (VARCHAR(50), NULL): Granular qualification (`DriverAssigned`, `DelayedByWeather`, `CustomerPostponed`).
- `LocationWarehouseId` (BIGINT, NULL, FK -> `organization.Warehouses.Id`): Facility where milestone occurred.
- `LocationBranchId` (BIGINT, NULL, FK -> `organization.Branches.Id`): Branch where milestone occurred.
- `Notes` (NVARCHAR(1000), NULL): Operational remarks.
- `ChangedByUserId` (BIGINT, NULL, FK -> `identity.UserAccounts.Id`): Authenticated user responsible.
- `ChangedByEmployeeId` (BIGINT, NULL, FK -> `organization.Employees.Id`): Driver or worker responsible.
- `ChangedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): UTC event timestamp.
- `Source` (VARCHAR(50), NOT NULL, DEFAULT 'Internal'): Event origin (`Internal`, `MobileApp`, `CarrierWebhook`, `BackgroundSync`).

#### 39. `shipping.ShipmentAssignments`
Assigns couriers, drivers, and fleet vehicles to perform pickup, linehaul transfer, or last-mile delivery tasks.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `ShipmentId` (BIGINT, NOT NULL, FK -> `shipping.Shipments.Id`): Assigned shipment.
- `AssignmentType` (VARCHAR(30), NOT NULL): Assignment phase (`Pickup`, `Linehaul`, `LastMileDelivery`).
- `AssignedToEmployeeId` (BIGINT, NOT NULL, FK -> `organization.Employees.Id`): Courier / driver staff member.
- `AssignedBranchId` (BIGINT, NOT NULL, FK -> `organization.Branches.Id`): Supervising branch.
- `AssignedVehicleId` (VARCHAR(50), NULL): License plate or vehicle code.
- `AssignedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Assignment dispatch timestamp.
- `AssignedByUserId` (BIGINT, NOT NULL, FK -> `identity.UserAccounts.Id`): Dispatcher user who assigned.
- `Status` (VARCHAR(30), NOT NULL, DEFAULT 'Assigned'): Progress (`Assigned`, `Accepted`, `InProgress`, `Completed`, `Failed`).
- `CompletedAt` (DATETIME2, NULL): Completion timestamp.
- `Notes` (NVARCHAR(500), NULL): Dispatch notes.

#### 40. `shipping.PickupRequests`
Dispatches drivers to collect parcels from customer premises.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `PublicId` (UNIQUEIDENTIFIER, UNIQUE, NOT NULL, DEFAULT NEWSEQUENTIALID()): External reference.
- `PickupCode` (VARCHAR(50), NOT NULL, UNIQUE): Unique request identifier (e.g., `PKP-202611-0021`).
- `CustomerId` (BIGINT, NOT NULL, FK -> `crm.Customers.Id`): Requesting customer.
- `RequestedAddressId` (BIGINT, NOT NULL, FK -> `crm.Addresses.Id`): Physical collection address.
- `ScheduledDate` (DATE, NOT NULL): Target collection date.
- `TimeSlot` (VARCHAR(50), NOT NULL): Window (e.g., `09:00 - 12:00`, `14:00 - 17:00`).
- `Status` (VARCHAR(30), NOT NULL, DEFAULT 'Pending'): Workflow (`Pending`, `Assigned`, `DriverArrived`, `Completed`, `Cancelled`).
- `AssignedEmployeeId` (BIGINT, NULL, FK -> `organization.Employees.Id`): Assigned pickup driver.
- `ContactName` (NVARCHAR(150), NOT NULL): On-site contact.
- `ContactPhone` (VARCHAR(50), NOT NULL): On-site contact phone.
- `EstimatedParcels` (INT, NOT NULL, DEFAULT 1): Anticipated package count.
- `ActualParcelsPickedUp` (INT, NOT NULL, DEFAULT 0): Actual count scanned.
- `Notes` (NVARCHAR(500), NULL): Special instructions.
- `CreatedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Creation timestamp.
- `CreatedBy` (NVARCHAR(100), NULL): Creator user.
- `UpdatedAt` (DATETIME2, NULL): Update timestamp.
- `UpdatedBy` (NVARCHAR(100), NULL): Updater user.
- `RowVersion` (ROWVERSION, NOT NULL): Concurrency token.

#### 41. `shipping.DeliveryAttempts`
Tracks every last-mile delivery attempt made by a courier, documenting success, failure reasons, and GPS coordinates.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `ShipmentId` (BIGINT, NOT NULL, FK -> `shipping.Shipments.Id`): Target shipment.
- `AttemptNumber` (INT, NOT NULL): Attempt iteration (1, 2, 3).
- `AttemptedByEmployeeId` (BIGINT, NOT NULL, FK -> `organization.Employees.Id`): Driver making attempt.
- `AttemptedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): UTC attempt time.
- `Status` (VARCHAR(30), NOT NULL): Outcome (`Delivered`, `Failed`, `Rescheduled`).
- `FailureReasonCode` (VARCHAR(50), NULL): Standard failure code (`RecipientUnavailable`, `WrongAddress`, `RefusedDelivery`, `CustomerUnreachable`, `CodDispute`).
- `FailureNotes` (NVARCHAR(500), NULL): Qualitative notes.
- `NextScheduledAttemptDate` (DATE, NULL): Follow-up redelivery date.
- `RecipientContacted` (BIT, NOT NULL, DEFAULT 0): Whether driver reached consignee by phone.
- `Latitude` (DECIMAL(10,7), NULL): Geolocation where attempt was made.
- `Longitude` (DECIMAL(10,7), NULL): Geolocation where attempt was made.
- *Unique Constraint*: `(ShipmentId, AttemptNumber)`.

#### 42. `shipping.ProofOfDeliveries`
Legal and operational confirmation of delivery with consignee identity, signature, photo, and geolocation.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `ShipmentId` (BIGINT, NOT NULL, FK -> `shipping.Shipments.Id`, UNIQUE): Master shipment.
- `DeliveryAttemptId` (BIGINT, NOT NULL, FK -> `shipping.DeliveryAttempts.Id`): Linked successful attempt.
- `RecipientName` (NVARCHAR(150), NOT NULL): Person who received the goods.
- `RecipientRelation` (NVARCHAR(50), NOT NULL, DEFAULT 'Self'): Relationship (`Self`, `FamilyMember`, `Receptionist`, `SecurityGuard`, `Colleague`).
- `RecipientNationalId` (VARCHAR(50), NULL): Identification card number (CCCD/CMND).
- `DeliveredAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Final handoff time.
- `SignatureAttachmentId` (BIGINT, NULL, FK -> `audit.FileAttachments.Id`): Digital signature image.
- `PhotoAttachmentId` (BIGINT, NULL, FK -> `audit.FileAttachments.Id`): Photo of parcel delivered at door/reception.
- `Notes` (NVARCHAR(500), NULL): Remarks.
- `Latitude` (DECIMAL(10,7), NULL): Delivery GPS latitude.
- `Longitude` (DECIMAL(10,7), NULL): Delivery GPS longitude.
- `VerifiedByUserId` (BIGINT, NULL, FK -> `identity.UserAccounts.Id`): Auditor or dispatcher who verified the POD.
- `VerifiedAt` (DATETIME2, NULL): Audit verification time.

#### 43. `shipping.ReturnRequests`
Manages return-to-sender (RTS) lifecycles when delivery fails or is rejected.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `PublicId` (UNIQUEIDENTIFIER, UNIQUE, NOT NULL, DEFAULT NEWSEQUENTIALID()): External reference.
- `ReturnCode` (VARCHAR(50), NOT NULL, UNIQUE): Unique return code (e.g., `RET-202611-0012`).
- `OriginalShipmentId` (BIGINT, NOT NULL, FK -> `shipping.Shipments.Id`): Outbound shipment that failed.
- `NewReturnShipmentId` (BIGINT, NULL, FK -> `shipping.Shipments.Id`): New reverse shipment created for transit back to sender.
- `ReasonCode` (VARCHAR(50), NOT NULL): Reason code (`CustomerRefused`, `ExpiredMaxAttempts`, `IncorrectGoods`, `DamagedInTransit`).
- `ReasonDescription` (NVARCHAR(500), NOT NULL): Detailed explanation.
- `Status` (VARCHAR(30), NOT NULL, DEFAULT 'Requested'): Return progress (`Requested`, `Approved`, `InTransit`, `ReturnedToSender`, `Rejected`).
- `RequestedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Request timestamp.
- `ApprovedByUserId` (BIGINT, NULL, FK -> `identity.UserAccounts.Id`): Approving manager.
- `ApprovedAt` (DATETIME2, NULL): Approval timestamp.
- `Notes` (NVARCHAR(500), NULL): Remarks.

#### 44. `shipping.ShipmentRoutes`
Planned and executed multi-leg transit segments between warehouses and hubs (Linehaul legs).
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `ShipmentId` (BIGINT, NOT NULL, FK -> `shipping.Shipments.Id`): Associated shipment.
- `SequenceOrder` (INT, NOT NULL): Leg sequence (1 = Branch to Hub, 2 = Hub to Hub, 3 = Hub to Branch).
- `FromWarehouseId` (BIGINT, NOT NULL, FK -> `organization.Warehouses.Id`): Departure facility.
- `ToWarehouseId` (BIGINT, NOT NULL, FK -> `organization.Warehouses.Id`): Destination facility.
- `TransportType` (VARCHAR(30), NOT NULL, DEFAULT 'Road'): Mode (`Road`, `Air`, `Rail`, `Sea`).
- `CarrierId` (BIGINT, NULL, FK -> `carrier.Carriers.Id`): Third-party linehaul provider (if outsourced).
- `VehicleNumber` (VARCHAR(50), NULL): Fleet truck license plate.
- `EstimatedDeparture` (DATETIME2, NULL): Scheduled departure.
- `ActualDeparture` (DATETIME2, NULL): Actual departure.
- `EstimatedArrival` (DATETIME2, NULL): Scheduled arrival.
- `ActualArrival` (DATETIME2, NULL): Actual arrival.
- `Status` (VARCHAR(30), NOT NULL, DEFAULT 'Planned'): Leg state (`Planned`, `Dispatched`, `Arrived`, `Delayed`, `Cancelled`).
- *Unique Constraint*: `(ShipmentId, SequenceOrder)`.

---

### 4.8 Carrier Integration Module (`carrier` schema)

#### 45. `carrier.Carriers`
Commercial carrier master records (e.g., Giao Hang Nhanh, Giao Hang Tiet Kiem, Viettel Post, Vietnam Post).
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `PublicId` (UNIQUEIDENTIFIER, UNIQUE, NOT NULL, DEFAULT NEWSEQUENTIALID()): External reference.
- `Code` (VARCHAR(50), NOT NULL, UNIQUE): Unique carrier code (`GHN`, `GHTK`, `VTPOST`, `VNPOST`).
- `Name` (NVARCHAR(150), NOT NULL): Commercial brand name.
- `ContactPhone` (VARCHAR(50), NULL): Carrier support phone.
- `ContactEmail` (VARCHAR(150), NULL): Carrier support email.
- `WebsiteUrl` (VARCHAR(200), NULL): Carrier web portal.
- `ApiBaseUrl` (VARCHAR(250), NOT NULL): REST API base endpoint.
- `TrackingUrlTemplate` (VARCHAR(300), NULL): URL template with `{TRACKING_NUMBER}` placeholder.
- `IsActive` (BIT, NOT NULL, DEFAULT 1): Carrier availability.
- `CreatedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Creation timestamp.
- `CreatedBy` (NVARCHAR(100), NULL): Creator user.
- `UpdatedAt` (DATETIME2, NULL): Update timestamp.
- `UpdatedBy` (NVARCHAR(100), NULL): Updater user.
- `RowVersion` (ROWVERSION, NOT NULL): Concurrency token.

#### 46. `carrier.CarrierServices`
Service levels offered by each external carrier (Express, Standard, Bulk, Air).
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `PublicId` (UNIQUEIDENTIFIER, UNIQUE, NOT NULL, DEFAULT NEWSEQUENTIALID()): External reference.
- `CarrierId` (BIGINT, NOT NULL, FK -> `carrier.Carriers.Id`): Parent carrier.
- `ServiceCode` (VARCHAR(50), NOT NULL): Carrier's internal service code (e.g., `GHN_EXPRESS`, `GHTK_FAST`).
- `ServiceName` (NVARCHAR(150), NOT NULL): Display service title.
- `Description` (NVARCHAR(500), NULL): Service description and SLA.
- `EstimatedTransitHours` (INT, NULL): Promised transit turnaround in hours.
- `MaxWeightKg` (DECIMAL(12,3), NULL): Maximum allowable parcel weight.
- `MaxLengthCm` (DECIMAL(12,3), NULL): Maximum allowable parcel length.
- `IsCodSupported` (BIT, NOT NULL, DEFAULT 1): Whether carrier collects Cash on Delivery.
- `IsInsuranceSupported` (BIT, NOT NULL, DEFAULT 1): Whether carrier provides value insurance.
- `IsActive` (BIT, NOT NULL, DEFAULT 1): Active flag.
- `CreatedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Creation timestamp.
- `CreatedBy` (NVARCHAR(100), NULL): Creator user.
- `UpdatedAt` (DATETIME2, NULL): Update timestamp.
- `UpdatedBy` (NVARCHAR(100), NULL): Updater user.
- *Unique Constraint*: `(CarrierId, ServiceCode)`.

#### 47. `carrier.CarrierConfigurations`
Encrypted carrier API credentials, tokens, webhook secrets, and tenant credentials.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `CarrierId` (BIGINT, NOT NULL, FK -> `carrier.Carriers.Id`): Target carrier.
- `Environment` (VARCHAR(20), NOT NULL, DEFAULT 'Production'): Environment (`Sandbox`, `Production`).
- `ApiKey` (NVARCHAR(500), NOT NULL): Encrypted API authentication token / token string.
- `ApiSecret` (NVARCHAR(500), NULL): Encrypted API secret.
- `ClientId` (NVARCHAR(200), NULL): Merchant / Client ID.
- `Username` (VARCHAR(100), NULL): Login username.
- `EncryptedPassword` (NVARCHAR(500), NULL): Encrypted password.
- `WebhookSecret` (NVARCHAR(500), NULL): Shared secret for webhook HMAC signature validation.
- `SettingsJson` (NVARCHAR(MAX), NULL): Additional provider-specific settings in JSON.
- `IsActive` (BIT, NOT NULL, DEFAULT 1): Active configuration flag.
- `UpdatedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Timestamp of last update.
- `UpdatedBy` (NVARCHAR(100), NULL): User who updated config.
- *Unique Constraint*: `(CarrierId, Environment)`.

#### 48. `carrier.CarrierTrackingMappings`
Translates external carrier status codes to internal standardized `ShipmentStatus` milestones.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `CarrierId` (BIGINT, NOT NULL, FK -> `carrier.Carriers.Id`): Originating carrier.
- `ExternalStatusCode` (VARCHAR(100), NOT NULL): Carrier's native raw status code (e.g., `ready_to_pick`, `delivering`, `return`).
- `ExternalStatusName` (NVARCHAR(150), NULL): Carrier's descriptive status name.
- `InternalShipmentStatus` (VARCHAR(50), NOT NULL): Target internal system status.
- `InternalSubStatus` (VARCHAR(50), NULL): Detailed internal sub-status.
- `Description` (NVARCHAR(500), NULL): Notes on mapping rationale.
- `IsActive` (BIT, NOT NULL, DEFAULT 1): Active flag.
- *Unique Constraint*: `(CarrierId, ExternalStatusCode)`.

#### 49. `carrier.CarrierTrackingEvents`
Stores external tracking milestones received from carrier webhooks or sync jobs. Ensures strict idempotency.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `CarrierId` (BIGINT, NOT NULL, FK -> `carrier.Carriers.Id`): Carrier providing update.
- `CarrierTrackingNumber` (VARCHAR(100), NOT NULL): Carrier waybill code.
- `ExternalEventId` (VARCHAR(150), NOT NULL): Unique event ID from carrier (used for deduplication).
- `EventCode` (VARCHAR(100), NOT NULL): Raw event code.
- `EventDescription` (NVARCHAR(500), NOT NULL): Status narrative text.
- `EventTimestamp` (DATETIME2, NOT NULL): UTC time the event occurred in carrier network.
- `LocationName` (NVARCHAR(200), NULL): Carrier transit hub or city name.
- `RawPayloadJson` (NVARCHAR(MAX), NOT NULL): Complete raw JSON received.
- `ProcessedStatus` (VARCHAR(30), NOT NULL, DEFAULT 'Pending'): Processing state (`Pending`, `Processed`, `Ignored`, `Failed`).
- `ProcessedShipmentId` (BIGINT, NULL, FK -> `shipping.Shipments.Id`): Resolved internal shipment.
- `ProcessedAt` (DATETIME2, NULL): Timestamp when processed into shipment history.
- `ErrorMessage` (NVARCHAR(MAX), NULL): Processing error details if failed.
- *Unique Constraint*: `(CarrierId, ExternalEventId)`.

#### 50. `carrier.CarrierWebhookLogs`
Append-only log of raw incoming webhook HTTP POST requests.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `CarrierId` (BIGINT, NOT NULL, FK -> `carrier.Carriers.Id`): Carrier identified by endpoint or signature.
- `ExternalEventId` (VARCHAR(150), NULL): Event ID extracted from payload.
- `WebhookUrl` (VARCHAR(300), NOT NULL): Invoked webhook endpoint.
- `HttpMethod` (VARCHAR(10), NOT NULL, DEFAULT 'POST'): HTTP method.
- `HeadersJson` (NVARCHAR(MAX), NULL): Request headers (authorization tokens masked).
- `PayloadJson` (NVARCHAR(MAX), NOT NULL): Complete raw body.
- `ResponseStatusCode` (INT, NOT NULL): HTTP status code returned to carrier.
- `ResponseBody` (NVARCHAR(MAX), NULL): Response returned to carrier.
- `ProcessedSuccessfully` (BIT, NOT NULL): Whether payload was parsed and dispatched to queue.
- `ProcessedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Received timestamp.
- `ErrorDetails` (NVARCHAR(MAX), NULL): Error trace if processing failed.
- `ClientIpAddress` (VARCHAR(50), NOT NULL): Remote IP address of sender.

#### 51. `carrier.CarrierApiLogs`
Outbound API request/response audit trail with sensitive token masking.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `CarrierId` (BIGINT, NOT NULL, FK -> `carrier.Carriers.Id`): Carrier contacted.
- `Endpoint` (VARCHAR(300), NOT NULL): Target carrier URL.
- `HttpMethod` (VARCHAR(10), NOT NULL): Method (`GET`, `POST`, `PUT`, `DELETE`).
- `RequestPayloadMasked` (NVARCHAR(MAX), NULL): Sent payload with credentials redacted.
- `ResponsePayloadMasked` (NVARCHAR(MAX), NULL): Received response with credentials redacted.
- `ResponseStatusCode` (INT, NOT NULL): Returned HTTP code.
- `DurationMs` (INT, NOT NULL): Network round-trip duration in milliseconds.
- `Timestamp` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Request timestamp.
- `IsSuccess` (BIT, NOT NULL): Success indicator (2xx code).
- `ErrorMessage` (NVARCHAR(MAX), NULL): Network / API error details.

#### 52. `carrier.CarrierSyncJobs`
Operational logs of background worker polling jobs synchronizing tracking statuses with carriers.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `CarrierId` (BIGINT, NOT NULL, FK -> `carrier.Carriers.Id`): Synchronized carrier.
- `JobType` (VARCHAR(50), NOT NULL): Synchronization type (`TrackingPoll`, `FeeReconciliation`, `ManifestSync`).
- `StartTime` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Job start time.
- `EndTime` (DATETIME2, NULL): Job completion time.
- `Status` (VARCHAR(30), NOT NULL, DEFAULT 'Running'): Execution state (`Running`, `Completed`, `Failed`).
- `TotalRecordsProcessed` (INT, NOT NULL, DEFAULT 0): Count of shipments inspected.
- `TotalSuccessCount` (INT, NOT NULL, DEFAULT 0): Successfully synced records.
- `TotalFailureCount` (INT, NOT NULL, DEFAULT 0): Errored records.
- `ErrorLog` (NVARCHAR(MAX), NULL): Aggregated errors.

---

### 4.9 Warehouse and Parcel Hub Module (`warehouse` schema)

#### 53. `warehouse.WarehouseZones`
Physical sections within a facility (Receiving, Bulk Storage, Picking, Staging, Parcel Sorting, Cross-dock).
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `PublicId` (UNIQUEIDENTIFIER, UNIQUE, NOT NULL, DEFAULT NEWSEQUENTIALID()): External reference.
- `WarehouseId` (BIGINT, NOT NULL, FK -> `organization.Warehouses.Id`): Host facility.
- `Code` (VARCHAR(50), NOT NULL): Zone code (e.g., `ZONE-SORT`, `ZONE-RACK-A`).
- `Name` (NVARCHAR(150), NOT NULL): Display zone name.
- `ZoneType` (VARCHAR(50), NOT NULL): Purpose (`Receiving`, `BulkStorage`, `Picking`, `Staging`, `ParcelSorting`, `CrossDock`, `Return`, `Packing`).
- `TemperatureType` (VARCHAR(30), NOT NULL, DEFAULT 'Ambient'): Climate (`Ambient`, `Chilled`, `Frozen`, `AirConditioned`).
- `IsActive` (BIT, NOT NULL, DEFAULT 1): Active flag.
- `CreatedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Creation timestamp.
- `CreatedBy` (NVARCHAR(100), NULL): Creator user.
- `UpdatedAt` (DATETIME2, NULL): Update timestamp.
- `UpdatedBy` (NVARCHAR(100), NULL): Updater user.
- *Unique Constraint*: `(WarehouseId, Code)`.

#### 54. `warehouse.WarehouseLocations`
Precise storage bin / slot / rack coordinate inside an inventory warehouse zone.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `PublicId` (UNIQUEIDENTIFIER, UNIQUE, NOT NULL, DEFAULT NEWSEQUENTIALID()): External reference.
- `WarehouseZoneId` (BIGINT, NOT NULL, FK -> `warehouse.WarehouseZones.Id`): Parent zone.
- `Code` (VARCHAR(50), NOT NULL): Canonical location string (e.g., `A-01-02-B` for Aisle A, Rack 01, Shelf 02, Bin B).
- `Aisle` (VARCHAR(20), NULL): Aisle.
- `Rack` (VARCHAR(20), NULL): Rack identifier.
- `Shelf` (VARCHAR(20), NULL): Shelf level.
- `Bin` (VARCHAR(20), NULL): Bin coordinate.
- `Barcode` (VARCHAR(100), NOT NULL, UNIQUE): Printable scan barcode for forklift/handheld operators.
- `MaxWeightKg` (DECIMAL(12,3), NOT NULL, DEFAULT 1000): Structural weight capacity.
- `MaxVolumeCbm` (DECIMAL(12,3), NOT NULL, DEFAULT 5): Volume capacity in cubic meters.
- `IsOccupied` (BIT, NOT NULL, DEFAULT 0): Occupancy indicator.
- `IsBlocked` (BIT, NOT NULL, DEFAULT 0): Maintenance / quarantine block flag.
- `CreatedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Creation timestamp.
- `CreatedBy` (NVARCHAR(100), NULL): Creator user.
- `UpdatedAt` (DATETIME2, NULL): Update timestamp.
- `UpdatedBy` (NVARCHAR(100), NULL): Updater user.

#### 55. `warehouse.WarehouseReceipts`
Inbound documentation for receiving physical goods (vendor deliveries, stock transfers, customer returns).
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `PublicId` (UNIQUEIDENTIFIER, UNIQUE, NOT NULL, DEFAULT NEWSEQUENTIALID()): External reference.
- `ReceiptNumber` (VARCHAR(50), NOT NULL, UNIQUE): Unique receipt code (e.g., `RCP-202611-00041`).
- `WarehouseId` (BIGINT, NOT NULL, FK -> `organization.Warehouses.Id`): Receiving warehouse.
- `ReceiptType` (VARCHAR(50), NOT NULL): Inbound nature (`InventoryInbound`, `ParcelInbound`, `CustomerReturn`, `TransferIn`).
- `SourceBranchId` (BIGINT, NULL, FK -> `organization.Branches.Id`): Sending branch (for transfers).
- `SourceVendor` (NVARCHAR(200), NULL): Vendor or supplier name.
- `ExpectedArrivalDate` (DATETIME2, NULL): Scheduled arrival.
- `ActualArrivalDate` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Gate arrival timestamp.
- `Status` (VARCHAR(30), NOT NULL, DEFAULT 'Draft'): Workflow (`Draft`, `Pending`, `Receiving`, `Completed`, `Cancelled`).
- `ReceivedByEmployeeId` (BIGINT, NOT NULL, FK -> `organization.Employees.Id`): Warehouse operator receiving goods.
- `Notes` (NVARCHAR(500), NULL): Receiving notes.
- `CreatedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Creation timestamp.
- `CreatedBy` (NVARCHAR(100), NULL): Creator user.
- `UpdatedAt` (DATETIME2, NULL): Update timestamp.
- `UpdatedBy` (NVARCHAR(100), NULL): Updater user.
- `RowVersion` (ROWVERSION, NOT NULL): Concurrency token.

#### 56. `warehouse.WarehouseReceiptItems`
Individual product lines received in an inbound receipt.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `WarehouseReceiptId` (BIGINT, NOT NULL, FK -> `warehouse.WarehouseReceipts.Id`): Parent receipt.
- `ProductId` (BIGINT, NOT NULL, FK -> `catalog.Products.Id`): Catalog product.
- `ExpectedQuantity` (INT, NOT NULL): Manifested expected quantity.
- `ReceivedQuantity` (INT, NOT NULL): Verified good condition quantity.
- `DamagedQuantity` (INT, NOT NULL, DEFAULT 0): Damaged / defective count.
- `UnitOfMeasureId` (BIGINT, NOT NULL, FK -> `configuration.UnitsOfMeasure.Id`): UOM.
- `TargetLocationId` (BIGINT, NULL, FK -> `warehouse.WarehouseLocations.Id`): Put-away destination location.
- `LotNumber` (VARCHAR(100), NULL): Batch/Lot identifier.
- `ExpiryDate` (DATE, NULL): Product expiration date.
- `Notes` (NVARCHAR(500), NULL): Variance explanations.

#### 57. `warehouse.WarehouseDispatches`
Outbound manifest documentation for shipping goods or dispatching linehaul transfer trucks.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `PublicId` (UNIQUEIDENTIFIER, UNIQUE, NOT NULL, DEFAULT NEWSEQUENTIALID()): External reference.
- `DispatchNumber` (VARCHAR(50), NOT NULL, UNIQUE): Unique manifest code (e.g., `DSP-202611-00089`).
- `WarehouseId` (BIGINT, NOT NULL, FK -> `organization.Warehouses.Id`): Departure warehouse.
- `DispatchType` (VARCHAR(50), NOT NULL): Dispatch nature (`InventoryOutbound`, `HubTransfer`, `CourierDispatch`, `ReturnOutbound`).
- `DestinationWarehouseId` (BIGINT, NULL, FK -> `organization.Warehouses.Id`): Receiving target warehouse.
- `DestinationBranchId` (BIGINT, NULL, FK -> `organization.Branches.Id`): Receiving target branch.
- `AssignedCarrierId` (BIGINT, NULL, FK -> `carrier.Carriers.Id`): Contracted linehaul carrier (if applicable).
- `Status` (VARCHAR(30), NOT NULL, DEFAULT 'Draft'): Workflow (`Draft`, `Picking`, `Packed`, `Staged`, `Dispatched`, `Cancelled`).
- `DispatchedByEmployeeId` (BIGINT, NOT NULL, FK -> `organization.Employees.Id`): Warehouse supervisor dispatching.
- `DispatchedAt` (DATETIME2, NULL): Actual gate departure time.
- `VehicleNumber` (VARCHAR(50), NULL): Truck license plate.
- `Notes` (NVARCHAR(500), NULL): Manifest notes.
- `CreatedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Creation timestamp.
- `CreatedBy` (NVARCHAR(100), NULL): Creator user.
- `UpdatedAt` (DATETIME2, NULL): Update timestamp.
- `UpdatedBy` (NVARCHAR(100), NULL): Updater user.
- `RowVersion` (ROWVERSION, NOT NULL): Concurrency token.

#### 58. `warehouse.WarehouseDispatchItems`
Inventory items picked and loaded into an outbound dispatch.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `WarehouseDispatchId` (BIGINT, NOT NULL, FK -> `warehouse.WarehouseDispatches.Id`): Parent dispatch.
- `ProductId` (BIGINT, NOT NULL, FK -> `catalog.Products.Id`): Picked product.
- `RequestedQuantity` (INT, NOT NULL): Requisitioned count.
- `PickedQuantity` (INT, NOT NULL): Actual picked count.
- `UnitOfMeasureId` (BIGINT, NOT NULL, FK -> `configuration.UnitsOfMeasure.Id`): UOM.
- `SourceLocationId` (BIGINT, NOT NULL, FK -> `warehouse.WarehouseLocations.Id`): Location from which goods were picked.
- `LotNumber` (VARCHAR(100), NULL): Lot picked.
- `Notes` (NVARCHAR(500), NULL): Remarks.

#### 59. `warehouse.WarehouseDispatchPackages`
Shipment parcels (transit packages) loaded onto a dispatch vehicle/container.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `WarehouseDispatchId` (BIGINT, NOT NULL, FK -> `warehouse.WarehouseDispatches.Id`): Parent dispatch manifest.
- `ShipmentPackageId` (BIGINT, NOT NULL, FK -> `shipping.ShipmentPackages.Id`): Loaded package.
- `Barcode` (VARCHAR(100), NOT NULL): Scanned barcode verification.
- `ScannedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Scan timestamp.
- `ScannedByEmployeeId` (BIGINT, NOT NULL, FK -> `organization.Employees.Id`): Scanning employee.
- `TargetContainerCode` (VARCHAR(50), NULL): Bulk bag / roll cage / pallet code.
- *Unique Constraint*: `(WarehouseDispatchId, ShipmentPackageId)`.

#### 60. `warehouse.ParcelHandlingEvents`
High-volume operational scan log for parcel hub operations. Completely separate from inventory balances.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `ShipmentPackageId` (BIGINT, NOT NULL, FK -> `shipping.ShipmentPackages.Id`): Scanned parcel.
- `ShipmentId` (BIGINT, NOT NULL, FK -> `shipping.Shipments.Id`): Associated master shipment.
- `WarehouseId` (BIGINT, NOT NULL, FK -> `organization.Warehouses.Id`): Physical sorting hub / facility.
- `EventType` (VARCHAR(50), NOT NULL): Operational event (`Unloaded`, `ScannedAtHub`, `Sorted`, `Bagged`, `Staged`, `LoadedToVehicle`, `Dispatched`, `ReceivedAtDeliveryBranch`).
- `BagNumber` (VARCHAR(50), NULL): Master consolidation bag / container barcode.
- `NextDestinationWarehouseId` (BIGINT, NULL, FK -> `organization.Warehouses.Id`): Intended next transit destination.
- `EmployeeId` (BIGINT, NOT NULL, FK -> `organization.Employees.Id`): Scanner operator.
- `ScannedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): High-precision event timestamp.
- `DeviceId` (VARCHAR(100), NULL): Handheld scanner / conveyor camera identifier.
- `Notes` (NVARCHAR(500), NULL): Scanner diagnostics or exception notes.

---

### 4.10 Inventory Module (`inventory` schema)

#### 61. `inventory.InventoryItems`
Configures replenishment and stocking thresholds for a product at a specific inventory warehouse.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `PublicId` (UNIQUEIDENTIFIER, UNIQUE, NOT NULL, DEFAULT NEWSEQUENTIALID()): External reference.
- `ProductId` (BIGINT, NOT NULL, FK -> `catalog.Products.Id`): Catalog product.
- `WarehouseId` (BIGINT, NOT NULL, FK -> `organization.Warehouses.Id`): Facility stocking this product.
- `ReorderPoint` (INT, NOT NULL, DEFAULT 10): Minimum stock triggering reorder.
- `SafetyStock` (INT, NOT NULL, DEFAULT 5): Buffer reserve.
- `TargetStock` (INT, NOT NULL, DEFAULT 100): Optimal stocking capacity.
- `MinimumOrderQuantity` (INT, NOT NULL, DEFAULT 1): Minimum vendor replenishment purchase.
- `MaximumStock` (INT, NOT NULL, DEFAULT 500): Warehouse physical capacity limit for this SKU.
- `IsActive` (BIT, NOT NULL, DEFAULT 1): Active replenishment flag.
- `CreatedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Creation timestamp.
- `CreatedBy` (NVARCHAR(100), NULL): Creator user.
- `UpdatedAt` (DATETIME2, NULL): Update timestamp.
- `UpdatedBy` (NVARCHAR(100), NULL): Updater user.
- *Unique Constraint*: `(ProductId, WarehouseId)`.

#### 62. `inventory.InventoryLots`
Batch and lot control for expiration tracking, perishable goods, and supplier batches.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `PublicId` (UNIQUEIDENTIFIER, UNIQUE, NOT NULL, DEFAULT NEWSEQUENTIALID()): External reference.
- `ProductId` (BIGINT, NOT NULL, FK -> `catalog.Products.Id`): Associated product.
- `LotNumber` (VARCHAR(100), NOT NULL): Vendor or internal lot code.
- `ManufactureDate` (DATE, NULL): Production date.
- `ExpiryDate` (DATE, NULL): Shelf life expiration date.
- `VendorBatchCode` (VARCHAR(100), NULL): Supplier manufacturing code.
- `Notes` (NVARCHAR(500), NULL): Quality remarks.
- `CreatedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Creation timestamp.
- *Unique Constraint*: `(ProductId, LotNumber)`.

#### 63. `inventory.InventorySerialNumbers`
Individual item serial number tracking for high-value cargo and electronics.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `ProductId` (BIGINT, NOT NULL, FK -> `catalog.Products.Id`): Product.
- `SerialNumber` (VARCHAR(100), NOT NULL, UNIQUE): Unique physical serial tag.
- `WarehouseId` (BIGINT, NOT NULL, FK -> `organization.Warehouses.Id`): Current location warehouse.
- `LocationId` (BIGINT, NULL, FK -> `warehouse.WarehouseLocations.Id`): Current bin location.
- `Status` (VARCHAR(30), NOT NULL, DEFAULT 'Available'): State (`Available`, `Reserved`, `Dispatched`, `Defective`).
- `ReceivedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Receipt time.
- `DispatchedAt` (DATETIME2, NULL): Dispatch time.

#### 64. `inventory.InventoryBalances`
Live operational stock balance. Reconciled against `InventoryTransactions`.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `WarehouseId` (BIGINT, NOT NULL, FK -> `organization.Warehouses.Id`): Facility.
- `WarehouseLocationId` (BIGINT, NOT NULL, FK -> `warehouse.WarehouseLocations.Id`): Bin location.
- `ProductId` (BIGINT, NOT NULL, FK -> `catalog.Products.Id`): Product.
- `LotId` (BIGINT, NULL, FK -> `inventory.InventoryLots.Id`): Lot (if tracked).
- `QuantityOnHand` (INT, NOT NULL, DEFAULT 0): Physical units present.
- `QuantityAllocated` (INT, NOT NULL, DEFAULT 0): Units reserved for active orders.
- `QuantityAvailable` (AS (`QuantityOnHand` - `QuantityAllocated`)): Computed available units.
- `QuantityDamaged` (INT, NOT NULL, DEFAULT 0): Units quarantined as defective.
- `LastCountDate` (DATETIME2, NULL): Last cycle count physical audit timestamp.
- `RowVersion` (ROWVERSION, NOT NULL): Concurrency token.
- *Check Constraint*: `QuantityOnHand >= 0`, `QuantityAllocated >= 0`, `QuantityDamaged >= 0`.
- *Unique Constraint*: `(WarehouseId, WarehouseLocationId, ProductId, LotId)`.

#### 65. `inventory.InventoryTransactions`
Append-only master header for all stock balance changes (Receipts, Dispatches, Transfers, Adjustments).
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `PublicId` (UNIQUEIDENTIFIER, UNIQUE, NOT NULL, DEFAULT NEWSEQUENTIALID()): External reference.
- `TransactionNumber` (VARCHAR(50), NOT NULL, UNIQUE): Unique transaction sequence code (e.g., `TXN-202611-00109`).
- `WarehouseId` (BIGINT, NOT NULL, FK -> `organization.Warehouses.Id`): Facility where change occurred.
- `TransactionType` (VARCHAR(50), NOT NULL): Movement type (`Receipt`, `Dispatch`, `TransferIn`, `TransferOut`, `PositiveAdjustment`, `NegativeAdjustment`, `CycleCount`).
- `ReferenceType` (VARCHAR(50), NOT NULL): Business source (`WarehouseReceipt`, `WarehouseDispatch`, `StockTransfer`, `StockAdjustment`).
- `ReferenceId` (BIGINT, NOT NULL): Primary key of source document.
- `ReferenceNumber` (VARCHAR(100), NOT NULL): Human-readable code of source document.
- `PerformedByEmployeeId` (BIGINT, NOT NULL, FK -> `organization.Employees.Id`): Staff member responsible.
- `TransactionDate` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Transaction timestamp.
- `Notes` (NVARCHAR(500), NULL): Transaction remarks.
- `CreatedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Creation timestamp.

#### 66. `inventory.InventoryTransactionItems`
Line-item detail of stock movements, preserving pre- and post-balances for financial and inventory reconciliation.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `InventoryTransactionId` (BIGINT, NOT NULL, FK -> `inventory.InventoryTransactions.Id`): Parent header.
- `ProductId` (BIGINT, NOT NULL, FK -> `catalog.Products.Id`): Transacted product.
- `LocationId` (BIGINT, NOT NULL, FK -> `warehouse.WarehouseLocations.Id`): Bin location impacted.
- `LotId` (BIGINT, NULL, FK -> `inventory.InventoryLots.Id`): Lot impacted.
- `Quantity` (INT, NOT NULL): Delta quantity (+ for incoming, - for outgoing, != 0).
- `UnitCost` (DECIMAL(19,4), NOT NULL, DEFAULT 0): Inventory valuation unit cost.
- `TotalCost` (DECIMAL(19,4), NOT NULL, DEFAULT 0): Total line valuation (`Quantity * UnitCost`).
- `BalanceBefore` (INT, NOT NULL): On-hand balance immediately prior to this transaction.
- `BalanceAfter` (INT, NOT NULL): On-hand balance immediately following this transaction.

#### 67. `inventory.StockAdjustments`
Discrepancy write-offs or write-ins following physical cycle counting or damage.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `PublicId` (UNIQUEIDENTIFIER, UNIQUE, NOT NULL, DEFAULT NEWSEQUENTIALID()): External reference.
- `AdjustmentNumber` (VARCHAR(50), NOT NULL, UNIQUE): Unique adjustment code (e.g., `ADJ-202611-00012`).
- `WarehouseId` (BIGINT, NOT NULL, FK -> `organization.Warehouses.Id`): Impacted facility.
- `ReasonCode` (VARCHAR(50), NOT NULL): Standard reason (`Damage`, `Shrinkage`, `CountingVariance`, `Expired`, `FoundGoods`).
- `ReasonDescription` (NVARCHAR(500), NOT NULL): Justification.
- `Status` (VARCHAR(30), NOT NULL, DEFAULT 'Draft'): Workflow (`Draft`, `Approved`, `Rejected`, `Applied`).
- `ApprovedByEmployeeId` (BIGINT, NULL, FK -> `organization.Employees.Id`): Management approval.
- `ApprovedAt` (DATETIME2, NULL): Approval timestamp.
- `CreatedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Creation timestamp.
- `CreatedBy` (NVARCHAR(100), NULL): Creator user.
- `UpdatedAt` (DATETIME2, NULL): Update timestamp.
- `UpdatedBy` (NVARCHAR(100), NULL): Updater user.
- `RowVersion` (ROWVERSION, NOT NULL): Concurrency token.

#### 68. `inventory.StockTransfers`
Inter-warehouse inventory stock movements with dual-sided dispatch and receiving verification.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `PublicId` (UNIQUEIDENTIFIER, UNIQUE, NOT NULL, DEFAULT NEWSEQUENTIALID()): External reference.
- `TransferNumber` (VARCHAR(50), NOT NULL, UNIQUE): Unique transfer code (e.g., `TRF-202611-0008`).
- `SourceWarehouseId` (BIGINT, NOT NULL, FK -> `organization.Warehouses.Id`): Sending facility.
- `DestinationWarehouseId` (BIGINT, NOT NULL, FK -> `organization.Warehouses.Id`): Receiving facility.
- `Status` (VARCHAR(30), NOT NULL, DEFAULT 'Draft'): Progress (`Draft`, `Approved`, `InTransit`, `Received`, `Cancelled`).
- `RequestedByEmployeeId` (BIGINT, NOT NULL, FK -> `organization.Employees.Id`): Requesting employee.
- `ApprovedByEmployeeId` (BIGINT, NULL, FK -> `organization.Employees.Id`): Approving manager.
- `ShippedAt` (DATETIME2, NULL): Departure time from source.
- `ReceivedAt` (DATETIME2, NULL): Arrival and put-away time at destination.
- `Notes` (NVARCHAR(500), NULL): Transfer justification.
- `CreatedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Creation timestamp.
- `CreatedBy` (NVARCHAR(100), NULL): Creator user.
- `UpdatedAt` (DATETIME2, NULL): Update timestamp.
- `UpdatedBy` (NVARCHAR(100), NULL): Updater user.
- `RowVersion` (ROWVERSION, NOT NULL): Concurrency token.
- *Check Constraint*: `SourceWarehouseId <> DestinationWarehouseId`.

#### 69. `inventory.InventoryReservations`
Soft locks placed on inventory when sales orders are accepted, preventing stock overselling.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `PublicId` (UNIQUEIDENTIFIER, UNIQUE, NOT NULL, DEFAULT NEWSEQUENTIALID()): External reference.
- `ProductId` (BIGINT, NOT NULL, FK -> `catalog.Products.Id`): Product.
- `WarehouseId` (BIGINT, NOT NULL, FK -> `organization.Warehouses.Id`): Warehouse.
- `LocationId` (BIGINT, NOT NULL, FK -> `warehouse.WarehouseLocations.Id`): Specific bin reserved.
- `LotId` (BIGINT, NULL, FK -> `inventory.InventoryLots.Id`): Specific lot reserved.
- `OrderId` (BIGINT, NOT NULL, FK -> `sales.Orders.Id`): Order requiring stock.
- `ReservedQuantity` (INT, NOT NULL): Number of units reserved (> 0).
- `Status` (VARCHAR(30), NOT NULL, DEFAULT 'Active'): State (`Active`, `Released`, `Fulfilled`, `Expired`).
- `ReservedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Reservation time.
- `ExpiresAt` (DATETIME2, NOT NULL): Auto-release deadline if order remains unpaid/unfulfilled.
- `FulfilledAt` (DATETIME2, NULL): Time when picked for dispatch.

---

### 4.11 Finance Module (`finance` schema)

#### 70. `finance.ShippingRateCards`
Baseline price schedules containing tariff matrices.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `PublicId` (UNIQUEIDENTIFIER, UNIQUE, NOT NULL, DEFAULT NEWSEQUENTIALID()): External reference.
- `Code` (VARCHAR(50), NOT NULL, UNIQUE): Tariff code (e.g., `RATE-STANDARD-2026`).
- `Name` (NVARCHAR(150), NOT NULL): Tariff title.
- `Description` (NVARCHAR(500), NULL): Tariff coverage.
- `BaseCurrencyId` (BIGINT, NOT NULL, FK -> `configuration.Currencies.Id`): Pricing currency.
- `EffectiveFrom` (DATETIME2, NOT NULL): Start date/time.
- `EffectiveTo` (DATETIME2, NULL): Expiration date/time.
- `IsDefault` (BIT, NOT NULL, DEFAULT 0): Standard tariff flag for customers with no custom rates.
- `IsActive` (BIT, NOT NULL, DEFAULT 1): Active flag.
- `CreatedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Creation timestamp.
- `CreatedBy` (NVARCHAR(100), NULL): Creator user.
- `UpdatedAt` (DATETIME2, NULL): Update timestamp.
- `UpdatedBy` (NVARCHAR(100), NULL): Updater user.
- `RowVersion` (ROWVERSION, NOT NULL): Concurrency token.

#### 71. `finance.ShippingRateRules`
Granular pricing entries mapping origin/destination zones, weight tiers, base rates, and excess kg rates.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `ShippingRateCardId` (BIGINT, NOT NULL, FK -> `finance.ShippingRateCards.Id`): Parent rate card.
- `ServiceType` (VARCHAR(50), NOT NULL): Service tier (`SameDay`, `NextDayExpress`, `StandardGround`, `Economical`).
- `OriginProvinceId` (BIGINT, NULL, FK -> `configuration.Provinces.Id`): Origin province (NULL = all).
- `DestinationProvinceId` (BIGINT, NULL, FK -> `configuration.Provinces.Id`): Destination province (NULL = all).
- `FromWeightKg` (DECIMAL(12,3), NOT NULL, DEFAULT 0): Weight tier start.
- `ToWeightKg` (DECIMAL(12,3), NOT NULL): Weight tier end.
- `BaseRate` (DECIMAL(19,4), NOT NULL): Flat fee for base weight bracket.
- `AdditionalPerKgRate` (DECIMAL(19,4), NOT NULL, DEFAULT 0): Fee per incremental kg beyond `ToWeightKg`.
- `SurchargePercent` (DECIMAL(5,2), NOT NULL, DEFAULT 0): Fuel/remote markup.
- `MinimumFee` (DECIMAL(19,4), NOT NULL, DEFAULT 0): Floor price for this route.
- `EstimatedDays` (INT, NOT NULL, DEFAULT 2): SLA transit expectation in business days.

#### 72. `finance.ShipmentCharges`
Itemized fee ledger for a shipment (Base Freight, COD Fee, Insurance, Fuel Surcharge, Return Fee).
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `ShipmentId` (BIGINT, NOT NULL, FK -> `shipping.Shipments.Id`): Target shipment.
- `ChargeType` (VARCHAR(50), NOT NULL): Fee classification (`BaseFreight`, `CodFee`, `Insurance`, `FuelSurcharge`, `RemoteAreaFee`, `ReturnFee`, `RedeliveryFee`, `Other`).
- `Amount` (DECIMAL(19,4), NOT NULL): Fee amount.
- `CurrencyId` (BIGINT, NOT NULL, FK -> `configuration.Currencies.Id`): Currency.
- `Description` (NVARCHAR(200), NOT NULL): Charge explanation.
- `IsInvoiced` (BIT, NOT NULL, DEFAULT 0): Billing indicator.
- `InvoiceId` (BIGINT, NULL, FK -> `finance.Invoices.Id`): Invoice where this charge was billed.
- `CreatedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Creation timestamp.

#### 73. `finance.Invoices`
Commercial billing statements issued to customers for shipping services rendered over a cycle.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `PublicId` (UNIQUEIDENTIFIER, UNIQUE, NOT NULL, DEFAULT NEWSEQUENTIALID()): External reference.
- `InvoiceNumber` (VARCHAR(50), NOT NULL, UNIQUE): Unique legal billing code (e.g., `INV-202611-00052`).
- `CustomerId` (BIGINT, NOT NULL, FK -> `crm.Customers.Id`): Billed customer.
- `InvoiceDate` (DATE, NOT NULL): Issuance date.
- `DueDate` (DATE, NOT NULL): Payment deadline.
- `PeriodStartDate` (DATE, NULL): Billing cycle start.
- `PeriodEndDate` (DATE, NULL): Billing cycle end.
- `SubTotal` (DECIMAL(19,4), NOT NULL): Net charges before tax.
- `TaxRate` (DECIMAL(5,2), NOT NULL, DEFAULT 8.0): VAT percentage.
- `TaxAmount` (DECIMAL(19,4), NOT NULL): Computed VAT.
- `TotalAmount` (DECIMAL(19,4), NOT NULL): Gross amount due (`SubTotal + TaxAmount`).
- `PaidAmount` (DECIMAL(19,4), NOT NULL, DEFAULT 0): Total payments allocated to this invoice.
- `OutstandingAmount` (AS (`TotalAmount` - `PaidAmount`)): Remaining balance due.
- `Status` (VARCHAR(30), NOT NULL, DEFAULT 'Draft'): State (`Draft`, `Issued`, `PartiallyPaid`, `Paid`, `Overdue`, `Cancelled`).
- `PaymentTermsDays` (INT, NOT NULL, DEFAULT 30): Credit period.
- `Notes` (NVARCHAR(500), NULL): Billing notes.
- `CreatedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Creation timestamp.
- `CreatedBy` (NVARCHAR(100), NULL): Creator user.
- `UpdatedAt` (DATETIME2, NULL): Update timestamp.
- `UpdatedBy` (NVARCHAR(100), NULL): Updater user.
- `RowVersion` (ROWVERSION, NOT NULL): Concurrency token.

#### 74. `finance.InvoiceItems`
Line items detailed on an invoice, linked directly to shipment charges.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `InvoiceId` (BIGINT, NOT NULL, FK -> `finance.Invoices.Id`): Parent invoice.
- `ShipmentId` (BIGINT, NULL, FK -> `shipping.Shipments.Id`): Billed shipment.
- `ShipmentChargeId` (BIGINT, NULL, FK -> `finance.ShipmentCharges.Id`): Underlying charge line.
- `Description` (NVARCHAR(300), NOT NULL): Invoice line narrative.
- `Quantity` (INT, NOT NULL, DEFAULT 1): Quantity.
- `UnitPrice` (DECIMAL(19,4), NOT NULL): Unit price.
- `Amount` (DECIMAL(19,4), NOT NULL): Line subtotal.
- `TaxAmount` (DECIMAL(19,4), NOT NULL, DEFAULT 0): Line VAT.
- `TotalAmount` (DECIMAL(19,4), NOT NULL): Total line charge.

#### 75. `finance.Payments`
Cash, bank transfer, or credit card remittances received from clients.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `PublicId` (UNIQUEIDENTIFIER, UNIQUE, NOT NULL, DEFAULT NEWSEQUENTIALID()): External reference.
- `PaymentNumber` (VARCHAR(50), NOT NULL, UNIQUE): Unique receipt code (e.g., `PAY-202611-00031`).
- `CustomerId` (BIGINT, NOT NULL, FK -> `crm.Customers.Id`): Remitting customer.
- `PaymentDate` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Payment receipt time.
- `PaymentMethod` (VARCHAR(30), NOT NULL): Medium (`Cash`, `BankTransfer`, `CreditCard`, `InternalCredit`, `CodDeduction`).
- `Amount` (DECIMAL(19,4), NOT NULL): Remitted monetary amount (> 0).
- `CurrencyId` (BIGINT, NOT NULL, FK -> `configuration.Currencies.Id`): Payment currency.
- `TransactionReference` (VARCHAR(150), NULL): Bank transaction / wire reference ID.
- `BankName` (NVARCHAR(150), NULL): Receiving / remitting financial institution.
- `BankAccount` (VARCHAR(50), NULL): Bank account number.
- `Notes` (NVARCHAR(500), NULL): Remarks.
- `Status` (VARCHAR(30), NOT NULL, DEFAULT 'Confirmed'): Status (`Pending`, `Confirmed`, `Cancelled`).
- `ConfirmedByUserId` (BIGINT, NOT NULL, FK -> `identity.UserAccounts.Id`): Finance staff verifying payment.
- `ConfirmedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Verification timestamp.
- `CreatedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Creation timestamp.
- `CreatedBy` (NVARCHAR(100), NULL): Creator user.
- `RowVersion` (ROWVERSION, NOT NULL): Concurrency token.

#### 76. `finance.PaymentAllocations`
Apportions received payments across one or more outstanding invoices. Prevents over-allocation.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `PaymentId` (BIGINT, NOT NULL, FK -> `finance.Payments.Id`): Source payment.
- `InvoiceId` (BIGINT, NOT NULL, FK -> `finance.Invoices.Id`): Target invoice.
- `AllocatedAmount` (DECIMAL(19,4), NOT NULL): Amount applied to invoice (> 0).
- `AllocatedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Allocation time.
- `AllocatedByUserId` (BIGINT, NOT NULL, FK -> `identity.UserAccounts.Id`): Accountant user.
- *Unique Constraint*: `(PaymentId, InvoiceId)`.

#### 77. `finance.CODCollections`
Records Cash on Delivery physically collected by couriers from consignees upon delivery handoff.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `PublicId` (UNIQUEIDENTIFIER, UNIQUE, NOT NULL, DEFAULT NEWSEQUENTIALID()): External reference.
- `ShipmentId` (BIGINT, NOT NULL, FK -> `shipping.Shipments.Id`, UNIQUE): Master shipment.
- `CollectedAmount` (DECIMAL(19,4), NOT NULL): Physical cash collected (> 0).
- `CurrencyId` (BIGINT, NOT NULL, FK -> `configuration.Currencies.Id`): Currency.
- `CollectionDate` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Timestamp of collection.
- `CollectedByEmployeeId` (BIGINT, NOT NULL, FK -> `organization.Employees.Id`): Courier who collected cash.
- `Status` (VARCHAR(30), NOT NULL, DEFAULT 'CollectedByDriver'): Lifecycle (`CollectedByDriver`, `DepositedToBranch`, `VerifiedByCashier`, `RemittedToCustomer`).
- `DriverReceiptNumber` (VARCHAR(50), NULL): Paper receipt given to consignee.
- `VerifiedByUserId` (BIGINT, NULL, FK -> `identity.UserAccounts.Id`): Branch cashier who verified cash envelope.
- `VerifiedAt` (DATETIME2, NULL): Cashier verification timestamp.
- `Notes` (NVARCHAR(500), NULL): Remarks.
- `CreatedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Creation timestamp.
- `RowVersion` (ROWVERSION, NOT NULL): Concurrency token.

#### 78. `finance.CODRemittances`
Consolidated disbursements of collected COD funds back to the selling customer (minus service fees).
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `PublicId` (UNIQUEIDENTIFIER, UNIQUE, NOT NULL, DEFAULT NEWSEQUENTIALID()): External reference.
- `RemittanceNumber` (VARCHAR(50), NOT NULL, UNIQUE): Unique remittance code (e.g., `COD-REMIT-202611-00019`).
- `CustomerId` (BIGINT, NOT NULL, FK -> `crm.Customers.Id`): Shipper receiving payout.
- `RemittanceDate` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Disbursement timestamp.
- `TotalCodAmount` (DECIMAL(19,4), NOT NULL): Gross COD collected.
- `TotalServiceFeesDeducted` (DECIMAL(19,4), NOT NULL, DEFAULT 0): Freight fees deducted at source.
- `NetRemittanceAmount` (DECIMAL(19,4), NOT NULL): Net wire amount paid to customer.
- `CurrencyId` (BIGINT, NOT NULL, FK -> `configuration.Currencies.Id`): Currency.
- `PaymentMethod` (VARCHAR(30), NOT NULL, DEFAULT 'BankTransfer'): Payout method.
- `PaymentReference` (VARCHAR(150), NULL): Electronic bank wire reference.
- `Status` (VARCHAR(30), NOT NULL, DEFAULT 'Draft'): Progress (`Draft`, `Approved`, `Processing`, `Completed`, `Cancelled`).
- `BankName` (NVARCHAR(150), NOT NULL): Customer's beneficiary bank.
- `BankAccount` (VARCHAR(50), NOT NULL): Customer's bank account number.
- `AccountHolderName` (NVARCHAR(150), NOT NULL): Registered account holder name.
- `RemittedByUserId` (BIGINT, NOT NULL, FK -> `identity.UserAccounts.Id`): Finance officer executing payout.
- `RemittedAt` (DATETIME2, NULL): Bank execution timestamp.
- `Notes` (NVARCHAR(500), NULL): Settlement remarks.
- `CreatedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Creation timestamp.
- `CreatedBy` (NVARCHAR(100), NULL): Creator user.
- `RowVersion` (ROWVERSION, NOT NULL): Concurrency token.

#### 79. `finance.CODRemittanceItems`
Junction mapping specific COD collections to a remittance settlement batch.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `CODRemittanceId` (BIGINT, NOT NULL, FK -> `finance.CODRemittances.Id`): Parent remittance batch.
- `CODCollectionId` (BIGINT, NOT NULL, FK -> `finance.CODCollections.Id`, UNIQUE): Remitted collection.
- `ShipmentId` (BIGINT, NOT NULL, FK -> `shipping.Shipments.Id`): Settled shipment.
- `CodAmount` (DECIMAL(19,4), NOT NULL): Gross COD settled.
- `DeductedShippingFee` (DECIMAL(19,4), NOT NULL, DEFAULT 0): Deducted freight fee.
- `NetAmount` (DECIMAL(19,4), NOT NULL): Net payout for this line (`CodAmount - DeductedShippingFee`).

#### 80. `finance.CustomerBalances`
Aggregated operational credit and COD ledger snapshot per customer. Reconciled against ledger entries.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `CustomerId` (BIGINT, NOT NULL, FK -> `crm.Customers.Id`, UNIQUE): Beneficiary customer.
- `CurrentOutstandingBalance` (DECIMAL(19,4), NOT NULL, DEFAULT 0): Unpaid invoiced amounts.
- `UnbilledChargesAmount` (DECIMAL(19,4), NOT NULL, DEFAULT 0): Delivered shipments not yet invoiced.
- `TotalCodHoldingAmount` (DECIMAL(19,4), NOT NULL, DEFAULT 0): COD cash held by company pending remittance.
- `LastUpdatedDate` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Timestamp of balance sync.
- `RowVersion` (ROWVERSION, NOT NULL): Concurrency token.

#### 81. `finance.FinancialLedgerEntries`
Immutable double-entry financial transaction log preserving all customer monetary debits and credits.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `PublicId` (UNIQUEIDENTIFIER, UNIQUE, NOT NULL, DEFAULT NEWSEQUENTIALID()): External reference.
- `EntryNumber` (VARCHAR(50), NOT NULL, UNIQUE): Unique ledger entry code (e.g., `LED-202611-000412`).
- `CustomerId` (BIGINT, NOT NULL, FK -> `crm.Customers.Id`): Impacted customer.
- `EntryType` (VARCHAR(50), NOT NULL): Transaction nature (`InvoiceCharge`, `PaymentReceipt`, `CodCollection`, `CodPayout`, `FeeAdjustment`, `CreditNote`).
- `ReferenceType` (VARCHAR(50), NOT NULL): Source document (`Invoice`, `Payment`, `CodRemittance`, `Adjustment`).
- `ReferenceId` (BIGINT, NOT NULL): Source document ID.
- `ReferenceNumber` (VARCHAR(100), NOT NULL): Source document code.
- `DebitAmount` (DECIMAL(19,4), NOT NULL, DEFAULT 0): Customer debit amount (increases customer liability).
- `CreditAmount` (DECIMAL(19,4), NOT NULL, DEFAULT 0): Customer credit amount (decreases customer liability).
- `RunningBalance` (DECIMAL(19,4), NOT NULL): Customer account balance immediately following entry.
- `CurrencyId` (BIGINT, NOT NULL, FK -> `configuration.Currencies.Id`): Transaction currency.
- `EntryDate` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Accounting timestamp.
- `Description` (NVARCHAR(500), NOT NULL): Transaction description.
- `CreatedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): System recording timestamp.
- `CreatedBy` (NVARCHAR(100), NULL): User or system worker executing entry.

---

### 4.12 Notifications and Reporting Module (`notification` schema)

#### 82. `notification.NotificationTemplates`
Parameterized message layouts for email, SMS, push notifications, and in-app alerts.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `Code` (VARCHAR(50), NOT NULL, UNIQUE): Template code (e.g., `SHIPMENT_DISPATCHED`, `OUT_FOR_DELIVERY`, `INVOICE_ISSUED`).
- `Name` (NVARCHAR(150), NOT NULL): Template name.
- `Channel` (VARCHAR(30), NOT NULL): Medium (`InApp`, `Email`, `SMS`, `Webhook`).
- `SubjectTemplate` (NVARCHAR(300), NULL): Subject line with `{VARIABLES}`.
- `BodyTemplate` (NVARCHAR(MAX), NOT NULL): Message body layout with `{VARIABLES}`.
- `ParametersJson` (NVARCHAR(MAX), NULL): JSON schema of expected parameter keys.
- `IsActive` (BIT, NOT NULL, DEFAULT 1): Active flag.
- `CreatedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Creation timestamp.
- `CreatedBy` (NVARCHAR(100), NULL): Creator user.
- `UpdatedAt` (DATETIME2, NULL): Update timestamp.
- `UpdatedBy` (NVARCHAR(100), NULL): Updater user.

#### 83. `notification.Notifications`
Individual outbound notifications targeted to users, drivers, or customer contacts.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `PublicId` (UNIQUEIDENTIFIER, UNIQUE, NOT NULL, DEFAULT NEWSEQUENTIALID()): External reference.
- `TemplateId` (BIGINT, NULL, FK -> `notification.NotificationTemplates.Id`): Originating template.
- `RecipientUserId` (BIGINT, NULL, FK -> `identity.UserAccounts.Id`): Target user (for in-app alerts).
- `RecipientEmail` (VARCHAR(150), NULL): Target email.
- `RecipientPhone` (VARCHAR(50), NULL): Target phone.
- `Title` (NVARCHAR(200), NOT NULL): Rendered subject / heading.
- `Content` (NVARCHAR(MAX), NOT NULL): Rendered body message.
- `Channel` (VARCHAR(30), NOT NULL): Channel (`InApp`, `Email`, `SMS`, `Webhook`).
- `Priority` (VARCHAR(20), NOT NULL, DEFAULT 'Normal'): Urgency (`Low`, `Normal`, `High`, `Urgent`).
- `Status` (VARCHAR(30), NOT NULL, DEFAULT 'Pending'): Delivery lifecycle (`Pending`, `Queued`, `Sent`, `Delivered`, `Failed`).
- `ReferenceType` (VARCHAR(50), NULL): Associated business entity (`Shipment`, `Order`, `Invoice`).
- `ReferenceId` (BIGINT, NULL): Entity ID.
- `CreatedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Creation timestamp.

#### 84. `notification.NotificationDeliveries`
Append-only log of physical delivery attempts to notification gateways (SMTP, SMS Twilio/Brandname, FCM).
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `NotificationId` (BIGINT, NOT NULL, FK -> `notification.Notifications.Id`): Parent notification.
- `Channel` (VARCHAR(30), NOT NULL): Medium attempted.
- `AttemptNumber` (INT, NOT NULL, DEFAULT 1): Attempt iteration.
- `AttemptedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Gateway dispatch time.
- `Status` (VARCHAR(30), NOT NULL): Outcome (`Success`, `Failed`).
- `GatewayResponse` (NVARCHAR(MAX), NULL): Raw gateway response code / message ID.
- `ErrorDetails` (NVARCHAR(MAX), NULL): Diagnostic error message if failed.

#### 85. `notification.ReportDefinitions`
Metadata and query configurations for operational and financial analytical reports.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `Code` (VARCHAR(50), NOT NULL, UNIQUE): Unique report key (e.g., `RPT_DAILY_DISPATCH`, `RPT_COD_RECONCILIATION`).
- `Name` (NVARCHAR(150), NOT NULL): Report title.
- `Category` (VARCHAR(50), NOT NULL): Category (`Operations`, `Fleet`, `Warehouse`, `Finance`, `Management`).
- `Description` (NVARCHAR(500), NULL): Report purpose.
- `QueryDefinition` (NVARCHAR(MAX), NOT NULL): Stored procedure or parameterized SQL view definition.
- `ParametersSchemaJson` (NVARCHAR(MAX), NULL): JSON schema for report filters.
- `IsActive` (BIT, NOT NULL, DEFAULT 1): Active flag.
- `CreatedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Creation timestamp.
- `CreatedBy` (NVARCHAR(100), NULL): Creator user.

#### 86. `notification.ScheduledReports`
Recurring schedules for automated generation and distribution of reports via email.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `PublicId` (UNIQUEIDENTIFIER, UNIQUE, NOT NULL, DEFAULT NEWSEQUENTIALID()): External reference.
- `ReportDefinitionId` (BIGINT, NOT NULL, FK -> `notification.ReportDefinitions.Id`): Target report.
- `ScheduleCron` (VARCHAR(100), NOT NULL): Standard 5-part cron expression (e.g., `0 6 * * 1-5`).
- `OutputFormat` (VARCHAR(20), NOT NULL, DEFAULT 'Pdf'): File format (`Pdf`, `Excel`, `Csv`).
- `RecipientEmails` (NVARCHAR(MAX), NOT NULL): Semicolon-delimited email distribution list.
- `LastRunAt` (DATETIME2, NULL): Timestamp of prior execution.
- `NextRunAt` (DATETIME2, NOT NULL): Calculated upcoming execution time.
- `IsActive` (BIT, NOT NULL, DEFAULT 1): Schedule active flag.
- `CreatedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Creation timestamp.
- `CreatedBy` (NVARCHAR(100), NULL): Creator user.

---

### 4.13 Integration and Background Processing Module (`integration` schema)

#### 87. `integration.BackgroundJobLogs`
Telemetry and execution history for background services (Worker jobs, polling, cleanup, cache refreshes).
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `JobName` (VARCHAR(100), NOT NULL): Background worker task name.
- `ExecutionId` (VARCHAR(100), NOT NULL): Unique execution correlation GUID.
- `TriggerType` (VARCHAR(30), NOT NULL, DEFAULT 'Scheduled'): Cause (`Scheduled`, `Manual`, `Event`).
- `StartedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Run start time.
- `CompletedAt` (DATETIME2, NULL): Run end time.
- `Status` (VARCHAR(30), NOT NULL, DEFAULT 'Running'): Execution state (`Running`, `Success`, `Warning`, `Failed`).
- `RecordsProcessed` (INT, NOT NULL, DEFAULT 0): Count of entities processed.
- `RecordsSucceeded` (INT, NOT NULL, DEFAULT 0): Successful operations.
- `RecordsFailed` (INT, NOT NULL, DEFAULT 0): Errored operations.
- `LogDetails` (NVARCHAR(MAX), NULL): Structured execution log text.
- `ExceptionDetails` (NVARCHAR(MAX), NULL): Stack trace if failed.

#### 88. `integration.OutboxMessages`
Implements the transactional Outbox Pattern, guaranteeing reliable domain event dispatch without two-phase commit.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `EventType` (VARCHAR(150), NOT NULL): Name of domain event (e.g., `ShipmentDeliveredEvent`, `OrderConfirmedEvent`).
- `AggregateType` (VARCHAR(100), NOT NULL): Originating domain aggregate (`Shipment`, `Order`, `Invoice`).
- `AggregateId` (VARCHAR(100), NOT NULL): Identity of aggregate.
- `PayloadJson` (NVARCHAR(MAX), NOT NULL): Serialized domain event payload in JSON.
- `CreatedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Event recording timestamp.
- `ProcessedAt` (DATETIME2, NULL): Timestamp when publisher dispatched message to broker/queue.
- `Status` (VARCHAR(30), NOT NULL, DEFAULT 'Pending'): Delivery status (`Pending`, `InProgress`, `Published`, `Failed`).
- `RetryCount` (INT, NOT NULL, DEFAULT 0): Dispatch retry counter.
- `ErrorMessage` (NVARCHAR(MAX), NULL): Publication error details if failed.

---

### 4.14 Audit and Files Module (`audit` schema)

#### 89. `audit.AuditLogs`
Immutable security and business change audit trail tracking data mutations across all operational entities.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `PublicId` (UNIQUEIDENTIFIER, UNIQUE, NOT NULL, DEFAULT NEWSEQUENTIALID()): External reference.
- `UserId` (BIGINT, NULL, FK -> `identity.UserAccounts.Id`): Authenticated user who performed mutation.
- `Username` (VARCHAR(100), NULL): Username recorded at time of action.
- `UserRole` (VARCHAR(100), NULL): User's primary role at time of action.
- `Action` (VARCHAR(50), NOT NULL): Action (`Create`, `Update`, `Delete`, `StatusChange`, `Login`, `Logout`).
- `TableName` (VARCHAR(100), NOT NULL): Database table mutated.
- `SchemaName` (VARCHAR(50), NOT NULL): Database schema.
- `PrimaryKey` (VARCHAR(100), NOT NULL): Primary key value of mutated record.
- `OldValuesJson` (NVARCHAR(MAX), NULL): Snapshot of values prior to update (JSON).
- `NewValuesJson` (NVARCHAR(MAX), NULL): Snapshot of values following update (JSON).
- `AffectedColumns` (NVARCHAR(MAX), NULL): Comma-delimited list of altered column names.
- `IpAddress` (VARCHAR(50), NULL): Client IP address.
- `UserAgent` (NVARCHAR(300), NULL): Workstation or mobile app user agent.
- `CorrelationId` (VARCHAR(100), NULL): Request correlation ID tracing across distributed logs.
- `Timestamp` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): UTC event time.

#### 90. `audit.FileAttachments`
Centralized metadata for binary documents, signatures, parcel photos, and scans stored on secure cloud/disk storage.
- `Id` (BIGINT, PK, IDENTITY(1,1)): Primary key.
- `PublicId` (UNIQUEIDENTIFIER, UNIQUE, NOT NULL, DEFAULT NEWSEQUENTIALID()): External reference.
- `EntityType` (VARCHAR(50), NOT NULL): Associated domain entity (`ProofOfDelivery`, `CustomerContract`, `CarrierLabel`, `Invoice`, `Receipt`, `DamageReport`).
- `EntityId` (BIGINT, NULL): Primary key of related business record.
- `FileName` (NVARCHAR(250), NOT NULL): Original client file name (e.g., `pod_signature_20261118.png`).
- `StoredFileName` (VARCHAR(200), NOT NULL, UNIQUE): Unique storage filename (e.g., `d8e30b...21.png`).
- `StoragePath` (NVARCHAR(500), NOT NULL): Blob container or disk folder path.
- `ContentType` (VARCHAR(100), NOT NULL): MIME type (`image/png`, `application/pdf`, `image/jpeg`).
- `FileSizeBytes` (BIGINT, NOT NULL): File size in bytes.
- `FileHashSha256` (VARCHAR(64), NOT NULL): SHA-256 cryptographic hash for tamper verification.
- `UploadedByUserId` (BIGINT, NULL, FK -> `identity.UserAccounts.Id`): Uploader user ID.
- `UploadedAt` (DATETIME2, NOT NULL, DEFAULT SYSUTCDATETIME()): Upload timestamp.
- `IsDeleted` (BIT, NOT NULL, DEFAULT 0): Soft deletion flag.

---

## 5. Master Schema Cross-Reference Summary

| # | Schema | Table Name | PK Type | FK References | Unique Keys |
|:---|:---|:---|:---|:---|:---|
| 1 | `organization` | `Companies` | BIGINT | None | `PublicId`, `Code`, `TaxNumber` |
| 2 | `organization` | `Branches` | BIGINT | `CompanyId`, `AddressId` | `PublicId`, `Code` |
| 3 | `organization` | `Departments` | BIGINT | `CompanyId`, `BranchId`, `ManagerEmployeeId` | `PublicId`, `(BranchId, Code)` |
| 4 | `organization` | `Employees` | BIGINT | `BranchId`, `DepartmentId`, `AddressId` | `PublicId`, `Code`, `Email` |
| 5 | `organization` | `Warehouses` | BIGINT | `BranchId`, `AddressId`, `ManagerEmployeeId` | `PublicId`, `Code` |
| 6 | `identity` | `UserAccounts` | BIGINT | `EmployeeId` | `PublicId`, `Username`, `Email`, `EmployeeId` |
| 7 | `identity` | `Roles` | BIGINT | None | `PublicId`, `Code` |
| 8 | `identity` | `Permissions` | BIGINT | None | `PublicId`, `Code` |
| 9 | `identity` | `UserRoles` | BIGINT | `UserId`, `RoleId` | `(UserId, RoleId)` |
| 10 | `identity` | `RolePermissions` | BIGINT | `RoleId`, `PermissionId` | `(RoleId, PermissionId)` |
| 11 | `identity` | `UserBranchAccess` | BIGINT | `UserId`, `BranchId` | `(UserId, BranchId)` |
| 12 | `identity` | `RefreshTokens` | BIGINT | `UserId` | `TokenHash` |
| 13 | `identity` | `UserSessions` | BIGINT | `UserId` | `SessionTokenHash` |
| 14 | `identity` | `LoginHistories` | BIGINT | `UserId` | None |
| 15 | `configuration` | `Countries` | BIGINT | None | `Code`, `Alpha3Code` |
| 16 | `configuration` | `Provinces` | BIGINT | `CountryId` | `Code` |
| 17 | `configuration` | `Districts` | BIGINT | `ProvinceId` | `Code` |
| 18 | `configuration` | `Wards` | BIGINT | `DistrictId` | `Code` |
| 19 | `configuration` | `Currencies` | BIGINT | None | `Code` |
| 20 | `configuration` | `UnitsOfMeasure` | BIGINT | None | `Code` |
| 21 | `configuration` | `SystemSettings` | BIGINT | None | `Key` |
| 22 | `configuration` | `NumberSequences` | BIGINT | None | `Scope` |
| 23 | `crm` | `Addresses` | BIGINT | `CountryId`, `ProvinceId`, `DistrictId`, `WardId` | `PublicId` |
| 24 | `crm` | `Customers` | BIGINT | `BranchId`, `AccountManagerEmployeeId` | `PublicId`, `CustomerCode` |
| 25 | `crm` | `CustomerContacts` | BIGINT | `CustomerId` | `PublicId` |
| 26 | `crm` | `CustomerAddresses` | BIGINT | `CustomerId`, `AddressId` | `PublicId` |
| 27 | `crm` | `CustomerContracts` | BIGINT | `CustomerId`, `FileAttachmentId` | `PublicId`, `ContractNumber` |
| 28 | `crm` | `CustomerPricingRules`| BIGINT | `CustomerId`, `CustomerContractId`, `ShippingRateCardId` | `PublicId` |
| 29 | `catalog` | `ProductCategories` | BIGINT | `ParentCategoryId` | `PublicId`, `Code` |
| 30 | `catalog` | `Products` | BIGINT | `CategoryId`, `UnitOfMeasureId`, `CurrencyId` | `PublicId`, `Sku` |
| 31 | `sales` | `Orders` | BIGINT | `CustomerId`, `BranchId`, `CurrencyId` | `PublicId`, `OrderNumber` |
| 32 | `sales` | `OrderItems` | BIGINT | `OrderId`, `ProductId`, `UnitOfMeasureId` | `PublicId` |
| 33 | `sales` | `OrderAddresses` | BIGINT | `OrderId`, `CountryId`, `ProvinceId`, `DistrictId`, `WardId` | `(OrderId, AddressType)` |
| 34 | `sales` | `OrderPackages` | BIGINT | `OrderId` | `PublicId` |
| 35 | `shipping` | `Shipments` | BIGINT | `OrderId`, `CustomerId`, `OriginBranchId`, `DestinationBranchId`, `AssignedCarrierId`, `CarrierServiceId`, `CurrencyId` | `PublicId`, `TrackingNumber` |
| 36 | `shipping` | `ShipmentPackages` | BIGINT | `ShipmentId`, `OrderPackageId` | `PublicId`, `Barcode` |
| 37 | `shipping` | `ShipmentAddresses` | BIGINT | `ShipmentId`, `CountryId`, `ProvinceId`, `DistrictId`, `WardId` | `(ShipmentId, AddressType)` |
| 38 | `shipping` | `ShipmentStatusHistories`| BIGINT | `ShipmentId`, `LocationWarehouseId`, `LocationBranchId`, `ChangedByUserId`, `ChangedByEmployeeId` | None |
| 39 | `shipping` | `ShipmentAssignments`| BIGINT | `ShipmentId`, `AssignedToEmployeeId`, `AssignedBranchId`, `AssignedByUserId` | None |
| 40 | `shipping` | `PickupRequests` | BIGINT | `CustomerId`, `RequestedAddressId`, `AssignedEmployeeId` | `PublicId`, `PickupCode` |
| 41 | `shipping` | `DeliveryAttempts` | BIGINT | `ShipmentId`, `AttemptedByEmployeeId` | `(ShipmentId, AttemptNumber)` |
| 42 | `shipping` | `ProofOfDeliveries` | BIGINT | `ShipmentId`, `DeliveryAttemptId`, `SignatureAttachmentId`, `PhotoAttachmentId`, `VerifiedByUserId` | `ShipmentId` |
| 43 | `shipping` | `ReturnRequests` | BIGINT | `OriginalShipmentId`, `NewReturnShipmentId`, `ApprovedByUserId` | `PublicId`, `ReturnCode` |
| 44 | `shipping` | `ShipmentRoutes` | BIGINT | `ShipmentId`, `FromWarehouseId`, `ToWarehouseId`, `CarrierId` | `(ShipmentId, SequenceOrder)` |
| 45 | `carrier` | `Carriers` | BIGINT | None | `PublicId`, `Code` |
| 46 | `carrier` | `CarrierServices` | BIGINT | `CarrierId` | `PublicId`, `(CarrierId, ServiceCode)` |
| 47 | `carrier` | `CarrierConfigurations`| BIGINT | `CarrierId` | `(CarrierId, Environment)` |
| 48 | `carrier` | `CarrierTrackingMappings`| BIGINT | `CarrierId` | `(CarrierId, ExternalStatusCode)` |
| 49 | `carrier` | `CarrierTrackingEvents`| BIGINT | `CarrierId`, `ProcessedShipmentId` | `(CarrierId, ExternalEventId)` |
| 50 | `carrier` | `CarrierWebhookLogs` | BIGINT | `CarrierId` | None |
| 51 | `carrier` | `CarrierApiLogs` | BIGINT | `CarrierId` | None |
| 52 | `carrier` | `CarrierSyncJobs` | BIGINT | `CarrierId` | None |
| 53 | `warehouse` | `WarehouseZones` | BIGINT | `WarehouseId` | `PublicId`, `(WarehouseId, Code)` |
| 54 | `warehouse` | `WarehouseLocations` | BIGINT | `WarehouseZoneId` | `PublicId`, `Barcode` |
| 55 | `warehouse` | `WarehouseReceipts` | BIGINT | `WarehouseId`, `SourceBranchId`, `ReceivedByEmployeeId` | `PublicId`, `ReceiptNumber` |
| 56 | `warehouse` | `WarehouseReceiptItems`| BIGINT | `WarehouseReceiptId`, `ProductId`, `UnitOfMeasureId`, `TargetLocationId` | None |
| 57 | `warehouse` | `WarehouseDispatches` | BIGINT | `WarehouseId`, `DestinationWarehouseId`, `DestinationBranchId`, `AssignedCarrierId`, `DispatchedByEmployeeId` | `PublicId`, `DispatchNumber` |
| 58 | `warehouse` | `WarehouseDispatchItems`| BIGINT | `WarehouseDispatchId`, `ProductId`, `UnitOfMeasureId`, `SourceLocationId` | None |
| 59 | `warehouse` | `WarehouseDispatchPackages`| BIGINT | `WarehouseDispatchId`, `ShipmentPackageId`, `ScannedByEmployeeId` | `(WarehouseDispatchId, ShipmentPackageId)` |
| 60 | `warehouse` | `ParcelHandlingEvents`| BIGINT | `ShipmentPackageId`, `ShipmentId`, `WarehouseId`, `NextDestinationWarehouseId`, `EmployeeId` | None |
| 61 | `inventory` | `InventoryItems` | BIGINT | `ProductId`, `WarehouseId` | `PublicId`, `(ProductId, WarehouseId)` |
| 62 | `inventory` | `InventoryLots` | BIGINT | `ProductId` | `PublicId`, `(ProductId, LotNumber)` |
| 63 | `inventory` | `InventorySerialNumbers`| BIGINT | `ProductId`, `WarehouseId`, `LocationId` | `SerialNumber` |
| 64 | `inventory` | `InventoryBalances` | BIGINT | `WarehouseId`, `WarehouseLocationId`, `ProductId`, `LotId` | `(WarehouseId, WarehouseLocationId, ProductId, LotId)` |
| 65 | `inventory` | `InventoryTransactions`| BIGINT | `WarehouseId`, `PerformedByEmployeeId` | `PublicId`, `TransactionNumber` |
| 66 | `inventory` | `InventoryTransactionItems`| BIGINT | `InventoryTransactionId`, `ProductId`, `LocationId`, `LotId` | None |
| 67 | `inventory` | `StockAdjustments` | BIGINT | `WarehouseId`, `ApprovedByEmployeeId` | `PublicId`, `AdjustmentNumber` |
| 68 | `inventory` | `StockTransfers` | BIGINT | `SourceWarehouseId`, `DestinationWarehouseId`, `RequestedByEmployeeId`, `ApprovedByEmployeeId` | `PublicId`, `TransferNumber` |
| 69 | `inventory` | `InventoryReservations`| BIGINT | `ProductId`, `WarehouseId`, `LocationId`, `LotId`, `OrderId` | `PublicId` |
| 70 | `finance` | `ShippingRateCards` | BIGINT | `BaseCurrencyId` | `PublicId`, `Code` |
| 71 | `finance` | `ShippingRateRules` | BIGINT | `ShippingRateCardId`, `OriginProvinceId`, `DestinationProvinceId` | None |
| 72 | `finance` | `ShipmentCharges` | BIGINT | `ShipmentId`, `CurrencyId`, `InvoiceId` | None |
| 73 | `finance` | `Invoices` | BIGINT | `CustomerId` | `PublicId`, `InvoiceNumber` |
| 74 | `finance` | `InvoiceItems` | BIGINT | `InvoiceId`, `ShipmentId`, `ShipmentChargeId` | None |
| 75 | `finance` | `Payments` | BIGINT | `CustomerId`, `CurrencyId`, `ConfirmedByUserId` | `PublicId`, `PaymentNumber` |
| 76 | `finance` | `PaymentAllocations` | BIGINT | `PaymentId`, `InvoiceId`, `AllocatedByUserId` | `(PaymentId, InvoiceId)` |
| 77 | `finance` | `CODCollections` | BIGINT | `ShipmentId`, `CurrencyId`, `CollectedByEmployeeId`, `VerifiedByUserId` | `PublicId`, `ShipmentId` |
| 78 | `finance` | `CODRemittances` | BIGINT | `CustomerId`, `CurrencyId`, `RemittedByUserId` | `PublicId`, `RemittanceNumber` |
| 79 | `finance` | `CODRemittanceItems` | BIGINT | `CODRemittanceId`, `CODCollectionId`, `ShipmentId` | `CODCollectionId` |
| 80 | `finance` | `CustomerBalances` | BIGINT | `CustomerId` | `CustomerId` |
| 81 | `finance` | `FinancialLedgerEntries`| BIGINT | `CustomerId`, `CurrencyId` | `PublicId`, `EntryNumber` |
| 82 | `notification` | `NotificationTemplates`| BIGINT | None | `Code` |
| 83 | `notification` | `Notifications` | BIGINT | `TemplateId`, `RecipientUserId` | `PublicId` |
| 84 | `notification` | `NotificationDeliveries`| BIGINT | `NotificationId` | None |
| 85 | `notification` | `ReportDefinitions` | BIGINT | None | `Code` |
| 86 | `notification` | `ScheduledReports` | BIGINT | `ReportDefinitionId` | `PublicId` |
| 87 | `integration` | `BackgroundJobLogs` | BIGINT | None | None |
| 88 | `integration` | `OutboxMessages` | BIGINT | None | None |
| 89 | `audit` | `AuditLogs` | BIGINT | `UserId` | `PublicId` |
| 90 | `audit` | `FileAttachments` | BIGINT | `UploadedByUserId` | `PublicId`, `StoredFileName` |
