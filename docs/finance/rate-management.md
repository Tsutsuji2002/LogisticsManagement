# Shipping Rate Management

## 1. Overview

Rate Management provides centralized control over logistics pricing structures, standard tariffs, weight brackets, geographic surcharge rules, and seasonal rate adjustments.

Rate cards represent base pricing catalogues, while rate rules define discrete pricing formulas applicable to specific geographic pairs, weight tiers, and service levels.

---

## 2. Rate Card Model & Attributes

Each `ShippingRateCard` encapsulates:
- **`Code`**: Unique uppercase identifier (e.g. `RC-DOM-STD-2026`).
- **`Name`**: Human-readable label (e.g. `National Domestic Standard Express 2026`).
- **`BaseCurrencyId`**: Foreign key to `configuration.Currencies`.
- **`EffectiveFrom` & `EffectiveTo`**: Validity period enabling forward-scheduled price revisions.
- **`IsDefault`**: Designates the fallback rate card when no customer-specific contract exists.
- **`IsActive`**: Soft-switch enabling/disabling tariff application without destroying historical references.

---

## 3. Rate Rule Engine & Resolution Matrix

Rate rules are matched using a hierarchical specificity matrix:

1. **Origin & Destination Province Match**:
   - `OriginProvinceId = Shipment.OriginProvinceId` AND `DestinationProvinceId = Shipment.DestinationProvinceId`
   - Weight range: `FromWeightKg <= ChargeableWeight <= ToWeightKg`
2. **Origin Province Match (All Destinations)**:
   - `OriginProvinceId = Shipment.OriginProvinceId` AND `DestinationProvinceId IS NULL`
   - Weight range: `FromWeightKg <= ChargeableWeight <= ToWeightKg`
3. **Destination Province Match (All Origins)**:
   - `OriginProvinceId IS NULL` AND `DestinationProvinceId = Shipment.DestinationProvinceId`
   - Weight range: `FromWeightKg <= ChargeableWeight <= ToWeightKg`
4. **Universal Default Rule**:
   - `OriginProvinceId IS NULL` AND `DestinationProvinceId IS NULL`
   - Weight range: `FromWeightKg <= ChargeableWeight <= ToWeightKg`

---

## 4. Rate Rule Calculation Parameters

Each `ShippingRateRule` specifies:
- **`BaseRate`**: Fixed fee covering the base bracket weight.
- **`AdditionalPerKgRate`**: Variable incremental charge per kilogram beyond `FromWeightKg`.
- **`SurchargePercent`**: Percentage markup (e.g., fuel or peak season surcharge).
- **`MinimumFee`**: Floor limit below which the calculated fee will not drop.
- **`EstimatedDays`**: Expected SLA delivery duration in business days.

---

## 5. Administrative Workflows

- **Rate Card Activation / Deactivation**: Status transitions are logged to the audit system. Only active cards within their effective date window participate in calculations.
- **Default Card Designation**: Setting `IsDefault = true` on a card automatically resets the flag on previously designated default cards for the same currency.
