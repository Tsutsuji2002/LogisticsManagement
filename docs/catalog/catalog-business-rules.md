# Catalog — Business Rules & Invariants

## 1. Product Category Rules

1. **Category Code Uniqueness**:
   - `Code` is case-insensitively unique across the organization.
   - Must contain only alphanumeric characters, dashes, and underscores (`^[a-zA-Z0-9_-]+$`).

2. **Hierarchical Relationships**:
   - Categories support arbitrary levels of nesting through `ParentCategoryId`.
   - **Self-Parent Prohibition**: A category cannot have its own ID as its `ParentCategoryId` ($ParentCategoryId \ne Id$).
   - **Circular Hierarchy Prohibition**: A category's parent cannot be one of its own descendants. Ancestor chains are recursively validated upon creation and update.

3. **Safe Deactivation Safeguard**:
   - A category cannot be deactivated if it or any of its subcategories contains one or more active products.
   - Deactivating such a category throws an `InvalidOperationException`. All dependent products must be deactivated or moved first.

4. **Deletion Integrity**:
   - Physical deletion is rejected if referenced by any products or if subcategories exist under it.

---

## 2. Product Master Rules

1. **SKU Uniqueness & Format**:
   - `Sku` is unique across the system.
   - Alphanumeric, dot, dash, and underscore allowed (`^[a-zA-Z0-9_.-]+$`).

2. **Category & Measurement Reference Integrity**:
   - When assigning `CategoryId`, the category must exist and must be currently active. Products cannot be created under inactive categories.
   - When assigning `UnitOfMeasureId`, the unit must exist and be currently active.
   - When assigning `CurrencyId`, the currency must exist and be active; if omitted, the system resolves the default operational currency (`VND`).

3. **Physical Dimensions & Metric Invariants**:
   - Scale weight (`WeightKg`) and dimensions (`LengthCm`, `WidthCm`, `HeightCm`) must be non-negative ($\ge 0$).
   - Volume in cubic meters ($m^3$) is calculated deterministically via DTO projection:
     $$\text{VolumeCbm} = \frac{\text{LengthCm} \times \text{WidthCm} \times \text{HeightCm}}{1,000,000}$$

4. **Hazardous & Perishable Flags**:
   - `IsPerishable` flags products requiring refrigerated, cold-chain, or expedited transit.
   - `IsDangerous` flags hazardous materials (HAZMAT) requiring documentation and transit restrictions.

5. **Historical Transaction Preservation**:
   - Physical deletion of products is strictly prohibited if the product is referenced in historical sales order lines (`sales.OrderItems`) or has stock balances in warehouses (`inventory.InventoryBalances`).
   - Products must be deactivated instead to preserve historical auditability and reporting integrity.

---

## 3. Unit of Measure Rules

1. **Code Uniqueness**:
   - Unit codes (e.g., `KG`, `G`, `CM`, `M`, `CBM`, `PCS`) must be unique.

2. **Categorical Types & Conversions**:
   - Units belong to standard measurement families: `Weight`, `Dimension`, `Volume`, `Count`, `Area`, `Time`.
   - Conversions are strictly defined relative to the base unit (`IsBaseUnit = true`) of the same family.
   - Conversion across disparate types (e.g., converting `Weight` to `Dimension`) is prohibited without explicit product density mappings.

3. **Referential Integrity on Deletion**:
   - Deleting a unit of measure is rejected if any product references it.
   - Deactivating a unit of measure is rejected if active products currently use it.

---

## 4. Logistics Service Catalog Rules

1. **Standard Speed Tiers**:
   - Internal logistics operations offer standard delivery tiers: `SAMEDAY` (6h), `NEXTDAY` (24h), `STANDARD` (72h), `ECONOMICAL` (120h).
   - Tiers define maximum parcel weight and dimensions, COD support, and transit SLA windows.

2. **Carrier Service Integration**:
   - Integrated carrier services from `carrier.CarrierServices` are surfaced alongside internal service levels.
   - Inactive services or carrier services with inactive parent carriers are filtered from active booking transactions.
