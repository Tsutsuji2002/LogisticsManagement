# Shipping — Shipment Management Module

## 1. Overview & Scope

The Shipping module is the core logistics execution engine of the Logistics Management System. It manages the physical conveyance of goods (waybills/consignments) from pickup through sorting, trunk linehaul transit, and last-mile delivery to proof-of-delivery (POD) or return to sender.

### Core Domain Separation
- **Sales Orders (`sales.Orders`)**: Commercial agreements between customer and logistics company.
- **Shipments (`shipping.Shipments`)**: Physical transportation waybills created from confirmed orders or walk-in customers.
- **Packages (`shipping.ShipmentPackages`)**: Individual parcels/boxes inside a shipment.
- **Addresses (`shipping.ShipmentAddresses`)**: Snapshots of sender, recipient, and return addresses.
- **Status Histories (`shipping.ShipmentStatusHistories`)**: Immutable, append-only chronological log of all milestone scans and transitions.
- **Assignments (`shipping.ShipmentAssignments`)**: Courier, driver, and vehicle dispatches for pickup, linehaul, and last-mile delivery.
- **Pickup Requests (`shipping.PickupRequests`)**: Customer requests for fleet collection at warehouse/premises.
- **Delivery Attempts (`shipping.DeliveryAttempts`)**: Detailed chronological logs of each delivery attempt.
- **Proof of Delivery (`shipping.ProofOfDeliveries`)**: Official legal confirmation of delivery with consignee identity, signature, photo, and geolocation.
- **Return Requests (`shipping.ReturnRequests`)**: Requests to return undeliverable or rejected shipments.
- **Shipment Routes (`shipping.ShipmentRoutes`)**: Ordered multi-leg transit segments between warehouses and hubs.

---

## 2. Architecture & Layering

```
LogisticsManagement
├── src/Server/Logistics.Server.Domain/Entities/Shipping/   # Entities (Shipment, ShipmentPackage, etc.)
├── src/Server/Logistics.Server.Application/Modules/Shipping/
│   ├── DTOs/        # Request, Response, and Summary contracts
│   ├── Services/    # IShipmentService, IPickupRequestService, etc.
│   └── Validators/  # FluentValidation validators
├── src/Server/Logistics.Server.Infrastructure/Services/    # ShippingServices.cs implementations
└── src/Server/Logistics.Server.API/Controllers/Shipping/   # ShipmentsController, PickupRequestsController, etc.
```

---

## 3. Registered Services in Dependency Injection

All Shipping services are registered as scoped services in `Logistics.Server.Infrastructure.DependencyInjection`:
- `IShipmentService` → `ShipmentService`
- `IShipmentPackageService` → `ShipmentPackageService`
- `IShipmentAddressService` → `ShipmentAddressService`
- `IPickupRequestService` → `PickupRequestService`
- `IShipmentAssignmentService` → `ShipmentAssignmentService`
- `IDeliveryAttemptService` → `DeliveryAttemptService`
- `IProofOfDeliveryService` → `ProofOfDeliveryService`
- `IReturnRequestService` → `ReturnRequestService`
- `IShipmentRouteService` → `ShipmentRouteService`
