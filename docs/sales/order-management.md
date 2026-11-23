# Sales Module — Order Management

## Overview

The Sales — Order Management module provides the core order lifecycle management for the Logistics Management System. It handles the creation, tracking, and cancellation of customer shipment orders, coordinating between customers, branch offices, products, and the downstream shipment execution layer.

## Scope

This module covers:

- **Order creation** with inline addresses, items, and packages
- **Order status lifecycle** (Draft → Confirmed → Processing → Shipped/Delivered/Cancelled)
- **Order item management** (products/services on the order)
- **Order package management** (physical parcel dimensions, weights, declared values)
- **Order address management** (sender/pickup and recipient/delivery contact information)
- **Automatic aggregate recalculation** (totals, weight, volume, package count)
- **Number sequence generation** (unique order numbers: `ORD-YYYYMM-NNNN`)
- **Branch-level access control** enforcement on all endpoints
- **Audit logging** for all state transitions and mutations

## Out of Scope (Handled by Other Modules)

| Capability               | Module            |
|--------------------------|-------------------|
| Shipment creation        | Shipments Module  |
| Carrier assignment       | Carrier Module    |
| Route planning           | Route Module      |
| Invoice generation       | Billing Module    |
| Payment collection (COD) | Billing Module    |
| Inventory reservation    | Inventory Module  |

## Architecture

```
┌─────────────────────────────────────────┐
│          API Layer (HTTP Controllers)    │
│  OrdersController                        │
│  OrderItemsController                    │
│  OrderPackagesController                 │
│  OrderAddressesController                │
└─────────────────┬───────────────────────┘
                  │ Calls
┌─────────────────▼───────────────────────┐
│       Application Layer (Interfaces)    │
│  IOrderService                          │
│  IOrderItemService                      │
│  IOrderPackageService                   │
│  IOrderAddressService                   │
│  INumberSequenceService                 │
└─────────────────┬───────────────────────┘
                  │ Implements
┌─────────────────▼───────────────────────┐
│    Infrastructure Layer (Services)      │
│  OrderService                           │
│  OrderItemService                       │
│  OrderPackageService                    │
│  OrderAddressService                    │
│  NumberSequenceService                  │
└─────────────────┬───────────────────────┘
                  │ Uses
┌─────────────────▼───────────────────────┐
│          Domain Layer (Entities)        │
│  Order (sales.Orders)                   │
│  OrderItem (sales.OrderItems)           │
│  OrderPackage (sales.OrderPackages)     │
│  OrderAddress (sales.OrderAddresses)    │
│  NumberSequence (config.NumberSequences)│
└─────────────────────────────────────────┘
```

## Key Services

### `IOrderService` / `OrderService`
Handles order CRUD and state transitions:
- `GetOrdersAsync` — Paginated list with branch filtering
- `GetOrderByIdAsync` — Fetch order with all nested data
- `CreateOrderAsync` — Atomic creation with addresses, items, packages, and sequence generation
- `UpdateOrderAsync` — Update header fields (Draft only)
- `ConfirmOrderAsync` — Transition Draft → Confirmed
- `CancelOrderAsync` — Cancel with reason, block if shipments in transit
- `DeleteOrderAsync` — Soft or hard delete (Draft only)

### `IOrderItemService` / `OrderItemService`
Manages line items on an order (Draft-only mutations):
- `GetItemsAsync`, `AddItemAsync`, `UpdateItemAsync`, `DeleteItemAsync`
- Triggers `RecalculateOrderTotalsAsync` after each mutation

### `IOrderPackageService` / `OrderPackageService`
Manages physical packages on an order (Draft + Confirmed mutations):
- `GetPackagesAsync`, `AddPackageAsync`, `UpdatePackageAsync`, `DeletePackageAsync`
- Calculates `VolumetricWeightKg = (L × W × H) / 5000`
- Triggers `RecalculateOrderTotalsAsync` after each mutation

### `IOrderAddressService` / `OrderAddressService`
Manages sender and recipient addresses (Draft + Confirmed):
- `GetAddressesAsync`, `GetAddressByTypeAsync`, `SetAddressAsync` (upsert)
- Supports both `Sender` and `Recipient` address types

### `INumberSequenceService` / `NumberSequenceService`
Thread-safe sequence generation for order numbers:
- Scope: `"ORDER"`, Prefix: `"ORD"`, Reset: Monthly
- Format: `ORD-{YYYYMM}-{NUMBER:0000}`
- Uses optimistic concurrency with retry on conflict

## Database Tables Used

| Table                        | Schema         | Purpose                               |
|------------------------------|----------------|---------------------------------------|
| `Orders`                     | `sales`        | Order header and status               |
| `OrderItems`                 | `sales`        | Line items on the order               |
| `OrderPackages`              | `sales`        | Physical packages                     |
| `OrderAddresses`             | `sales`        | Sender and recipient addresses        |
| `NumberSequences`            | `configuration`| Order number counter                  |
| `Shipments`                  | `logistics`    | Read-only: checked during cancellation|
| `Customers`                  | `crm`          | Customer lookup                       |
| `Branches`                   | `organization` | Branch lookup and access control      |

## Permissions

| Permission Code          | Description                        |
|--------------------------|------------------------------------|
| `Sales.OrdersRead`       | View orders and all sub-resources  |
| `Sales.OrdersCreate`     | Create new orders                  |
| `Sales.OrdersUpdate`     | Update order header, items, packages, addresses; confirm |
| `Sales.OrdersCancel`     | Cancel orders                      |

### Role → Permission Mapping

| Role                | Read | Create | Update | Cancel |
|---------------------|------|--------|--------|--------|
| SuperAdmin          | ✅   | ✅     | ✅     | ✅     |
| BranchManager       | ✅   | ✅     | ✅     | ✅     |
| CustomerServiceRep  | ✅   | ✅     | ✅     | ✅     |
| WarehouseStaff      | ✅   | ❌     | ❌     | ❌     |
| Viewer              | ✅   | ❌     | ❌     | ❌     |

## Validation

All request DTOs are validated with FluentValidation:

| Validator                         | Rules                                                       |
|-----------------------------------|-------------------------------------------------------------|
| `CreateOrderRequestValidator`     | `CustomerId > 0`, `CodAmount >= 0`, delivery >= pickup date |
| `UpdateOrderRequestValidator`     | Same as create                                              |
| `CancelOrderRequestValidator`     | `Reason` required, min 5 chars                             |
| `CreateOrderItemRequestValidator` | `ProductName` or `ProductId` required, `Quantity > 0`, `UnitPrice >= 0` |
| `UpdateOrderItemRequestValidator` | Same as create item                                         |
| `CreateOrderPackageRequestValidator`| `WeightKg > 0`, dimensions >= 0, `DeclaredValue >= 0`   |
| `UpdateOrderPackageRequestValidator`| Same as create package                                    |
| `OrderAddressRequestValidator`    | `ContactName`, `PhoneNumber`, `AddressLine1` required; `Email` format valid if provided |

## Error Handling

| Scenario                                 | HTTP Status | Exception                |
|------------------------------------------|-------------|--------------------------|
| Validation failure                       | 400         | `ValidationException`    |
| Order not found                          | 404         | `NotFoundException`      |
| State violation (e.g., add item to Confirmed order) | 409 | `ConflictException` |
| Cancellation blocked (shipment in transit)| 409        | `ConflictException`      |
| Duplicate operation                      | 409         | `ConflictException`      |
| Insufficient permission                  | 403         | Authorization middleware |
| No branch access                         | 403         | Authorization middleware |

## Audit Logging

All create, update, and delete operations emit an audit record via `IAuditService.RecordAsync(...)`:

| Operation         | Action       | Schema  | Table          |
|-------------------|--------------|---------|----------------|
| Order created     | `Create`     | `sales` | `Orders`       |
| Order updated     | `Update`     | `sales` | `Orders`       |
| Order confirmed   | `StatusChange`| `sales`| `Orders`       |
| Order cancelled   | `StatusChange`| `sales`| `Orders`       |
| Item added        | `Create`     | `sales` | `OrderItems`   |
| Item updated      | `Update`     | `sales` | `OrderItems`   |
| Item deleted      | `Delete`     | `sales` | `OrderItems`   |
| Package added     | `Create`     | `sales` | `OrderPackages`|
| Package updated   | `Update`     | `sales` | `OrderPackages`|
| Package deleted   | `Delete`     | `sales` | `OrderPackages`|
| Address set       | `Upsert`     | `sales` | `OrderAddresses`|
