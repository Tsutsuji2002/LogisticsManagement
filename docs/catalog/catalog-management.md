# Catalog — Product & Service Catalog Management

## 1. Overview & Architecture

The **Catalog Module** provides centralized management of product classifications, physical and commercial product definitions, standard packaging and measurement units, and logistics delivery service offerings for the Logistics Management System.

Built according to Clean Architecture and Modular Monolith principles, the Catalog module is shared across operational branches:
- Products, categories, and units of measure are globally available master entities.
- Consumed by upstream Sales (order booking), Shipping (consignment waybills and dimensions), Warehouse (parcel handling), and Inventory (stock balances).
- Decoupled from internal persistence implementations via `ICatalogReadService`.

```
               +---------------------------+
               |    WinForms / Web Client  |
               +-------------+-------------+
                             | Bearer JWT
                             v
               +---------------------------+
               |   Catalog API Controllers |
               +-------------+-------------+
                             |
         +-------------------+-------------------+
         |                   |                   |
         v                   v                   v
+-----------------+ +-----------------+ +-----------------+
| CategoryService | | ProductService  | |   UoM Service   |
+--------+--------+ +--------+--------+ +--------+--------+
         |                   |                   |
         +-------------------+-------------------+
                             |
                             v
               +---------------------------+
               |    ApplicationDbContext   |
               +-------------+-------------+
                             |
                             v
             SQL Server ("catalog", "configuration")
```

---

## 2. Domain & Entity Model

The Catalog module reuses the existing normalized database schema without alterations:

### 2.1 Product Categories (`catalog.ProductCategories`)
Hierarchical parcel and goods classification:
- `Id` (BIGINT, PK): Surrogate identifier.
- `PublicId` (UUID): External API identifier.
- `Code` (VARCHAR(50), UNIQUE): Unique category code (e.g., `ELEC`, `APPAREL`, `FOOD`).
- `Name` (NVARCHAR(150)): Category display title.
- `Description` (NVARCHAR(500), NULL): Detailed description.
- `ParentCategoryId` (BIGINT, NULL, FK): Self-referencing link to parent category.
- `IsActive` (BIT): Status flag.
- Audit timestamps: `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`.

### 2.2 Products (`catalog.Products`)
Master definitions of physical goods and standardized parcel items:
- `Id` (BIGINT, PK): Surrogate identifier.
- `PublicId` (UUID): External API identifier.
- `CategoryId` (BIGINT, FK -> `catalog.ProductCategories`): Assigned category.
- `Sku` (VARCHAR(50), UNIQUE): Stock Keeping Unit identifier.
- `Barcode` (VARCHAR(100), NULL, INDEX): Standard barcode (EAN-13, Code 128, QR).
- `Name` (NVARCHAR(200)): Item standard description.
- `Description` (NVARCHAR(1000), NULL): Detailed product specifications.
- `UnitOfMeasureId` (BIGINT, FK -> `configuration.UnitsOfMeasure`): Standard packaging unit.
- `WeightKg` (DECIMAL(12,3)): Physical unit scale weight in kilograms.
- `LengthCm` (DECIMAL(12,2)): Physical length in centimeters.
- `WidthCm` (DECIMAL(12,2)): Physical width in centimeters.
- `HeightCm` (DECIMAL(12,2)): Physical height in centimeters.
- `DeclaredValue` (DECIMAL(19,4)): Default declared value for insurance and customs.
- `CurrencyId` (BIGINT, FK -> `configuration.Currencies`): Declared value currency.
- `IsPerishable` (BIT): Indicates temperature-sensitive cargo.
- `IsDangerous` (BIT): Indicates HAZMAT cargo requiring special handling.
- `IsActive` (BIT): Operational status flag.

### 2.3 Units of Measure (`configuration.UnitsOfMeasure`)
Standard units for trading, dispatch, and physical dimension measurement:
- `Id` (BIGINT, PK): Primary key.
- `Code` (VARCHAR(20), UNIQUE): Standard code (e.g., `KG`, `G`, `CM`, `M`, `CBM`, `PCS`).
- `Name` (NVARCHAR(50)): Unit name.
- `Type` (VARCHAR(30)): Measurement type (`Weight`, `Dimension`, `Volume`, `Count`, `Area`, `Time`).
- `ConversionFactor` (DECIMAL(18,6)): Ratio relative to base unit of the same type.
- `IsBaseUnit` (BIT): True if this is the fundamental reference unit for its type.
- `IsActive` (BIT): Active status flag.

### 2.4 Logistics Service Catalog
Exposes standard delivery speed tiers and integrated 3rd-party carrier service levels:
- `SAMEDAY`: Same-Day Delivery (metropolitan express delivery within 6 hours).
- `NEXTDAY`: Next-Day Express (inter-provincial delivery within 24 hours).
- `STANDARD`: Standard Ground Freight (nationwide surface transit within 72 hours).
- `ECONOMICAL`: Economical Bulk Cargo (heavy and palletized cargo within 120 hours).
- Dynamic external carrier services queried from `carrier.CarrierServices`.

---

## 3. Cross-Module Integration (`ICatalogReadService`)

Downstream modules consume catalog information via the decoupled contract `ICatalogReadService`:
- Fast single and batch lookups: `GetProductByIdAsync`, `GetProductsByIdsAsync`, `GetProductBySkuAsync`.
- Autocomplete search: `SearchAsync`.
- Transaction feasibility validation: `CanProductBeUsedInTransactionAsync` (verifies product, category, and UoM are all simultaneously active).
- Delivery service validity: `CanServiceBeUsedInTransactionAsync`.

---

## 4. Security & Permissions

All Catalog endpoints enforce granular RBAC permissions:

| Permission | Description |
|---|---|
| `catalog.categories.read` | View category lists, details, and hierarchy tree |
| `catalog.categories.create` | Author new product categories |
| `catalog.categories.update` | Edit category details and parent assignment |
| `catalog.categories.manage-status` | Activate or deactivate product categories |
| `catalog.categories.delete` | Delete unreferenced categories |
| `catalog.products.read` | View product catalog and perform searches |
| `catalog.products.create` | Author new products |
| `catalog.products.update` | Edit product attributes, weights, and dimensions |
| `catalog.products.manage-status` | Toggle product activation |
| `catalog.products.delete` | Delete unreferenced products |
| `catalog.units.read` | View units of measure |
| `catalog.units.manage` | Create, edit, and toggle status of units of measure |
| `catalog.services.read` | View standard delivery service tiers |

---

## 5. Audit Trail

All catalog mutations record complete before/after snapshots and actor metadata in `audit.AuditLogs`:
- Actions: `CREATE`, `UPDATE`, `UPDATE_STATUS`, `DELETE`.
- Schema: `catalog` / `configuration`.
- Sensitive data is omitted; snapshots capture DTO representations preventing serialization cycles.
