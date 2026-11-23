# Parcel Handling & Hub Operations

## 1. Overview
The Parcel Handling subsystem governs physical package movement inside distribution hubs, transshipment centers, and cross-docking facilities.

Unlike physical inventory, parcels are customer property tracked via shipment waybills (`Shipment.TrackingNumber`) and package bar codes (`ShipmentPackage.Barcode` / `PackageCode`).

---

## 2. Event-Driven Hub Scanning

### 2.1 Supported Event Types
1. **Unloaded**: Parcel arrived at the facility bay and was scanned off the incoming truck/van.
2. **SortedToLane**: Operator scanned parcel and placed it on the outbound conveyor or sorting chute towards a destination hub.
3. **Bagged**: Multiple smaller parcels consolidated into a tamper-evident canvas bag (`BagNumber`).
4. **StagedInBay**: Consolidated bag or parcel moved to an outbound dock or staging bay (`WarehouseLocationId`).
5. **Loaded**: Parcel or bag loaded into an outgoing line-haul vehicle.
6. **ExceptionLogged**: Physical damage, misrouting, missing barcode, or customs hold.

### 2.2 Shipment Status Synchronization
Hub handling scans automatically drive end-to-end shipment statuses:
- When a `PickedUp` shipment has a package unloaded at a sorting hub, the parent shipment transitions to `InSortingHub`.
- When an `InSortingHub` shipment is included on a confirmed outbound `WarehouseDispatch`, it transitions to `InTransit`.
- When an exception is logged (e.g. `Damaged`), the package status is flagged and an audit trail is preserved.

---

## 3. High-Throughput Barcode Lookups
The `/api/v1/parcel-handling/scan` endpoint resolves any of the following identifiers in a single fast query:
1. `ShipmentPackage.Barcode` (e.g., `PKG-123456-01`)
2. `ShipmentPackage.PackageCode`
3. `Shipment.TrackingNumber` (e.g., `TRK-202611-000001`)

If a tracking number is scanned, the primary package or all associated packages are returned to the operator terminal with current routing instructions.

---

## 4. Warehouse Dispatches (`WarehouseDispatches`)
Dispatches represent vehicle departures and manifest handovers:
- **Types**: `Transfer` (hub-to-hub linehaul), `CustomerDelivery` (last-mile vehicle run), `ExternalCarrierHandover`.
- Supports bundling both:
  - Inventory items (for stock distribution transfers).
  - Parcel packages (for hub parcel movements).
- Confirming a dispatch validates vehicle details, marks packages as dispatched, and updates shipment tracking milestones.
