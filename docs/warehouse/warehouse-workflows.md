# Warehouse & Inventory Workflows

## 1. Inventory Inbound Receipt Workflow

```mermaid
sequenceDiagram
    autonumber
    actor Supplier as Supplier / Truck
    actor Receiver as Warehouse Staff
    participant API as Web API (/inventory/receipts)
    participant Svc as InventoryService
    participant DB as SQL Server (EF Core)

    Supplier->>Receiver: Deliver goods at receiving dock
    Receiver->>API: POST /receipts (Create draft receipt)
    API->>Svc: CreateReceiptAsync()
    Svc->>DB: Save draft WarehouseReceipt & ReceiptItems
    DB-->>API: Receipt created (Status: Pending)

    Receiver->>Receiver: Inspect quantities, verify quality & damage
    Receiver->>API: POST /receipts/{id}/confirm
    API->>Svc: ConfirmReceiptAsync()
    Svc->>DB: Fetch receipt & target bin locations
    Note over Svc,DB: Check location not blocked & valid
    Svc->>DB: Increment InventoryBalance.QuantityOnHand
    Svc->>DB: Insert InventoryTransaction (Type: Receipt)
    Svc->>DB: Set Receipt.Status = 'Completed'
    DB-->>API: 200 OK (Receipt confirmed)
    API-->>Receiver: Inbound stock updated & ledger recorded
```

---

## 2. Inventory Reservation & Allocation Workflow

```mermaid
sequenceDiagram
    autonumber
    participant OrderModule as Sales Order System
    participant API as Web API (/inventory/reservations)
    participant Svc as InventoryService
    participant DB as SQL Server

    OrderModule->>API: POST /reservations (ProductId, WarehouseId, Quantity)
    API->>Svc: CreateReservationAsync()
    Svc->>DB: Check InventoryBalance (QuantityOnHand - QuantityAllocated)
    alt Available Stock < Requested Quantity
        Svc-->>API: 409 ConflictException (Insufficient stock)
        API-->>OrderModule: Cannot reserve: Insufficient stock
    else Available Stock >= Requested Quantity
        Svc->>DB: InventoryBalance.QuantityAllocated += Requested
        Svc->>DB: Insert InventoryReservation (Status: Active)
        DB-->>API: 201 Created (Reservation active)
        API-->>OrderModule: Reservation confirmed (Reserved stock protected)
    end
```

---

## 3. Parcel Sorting Hub Workflow

```mermaid
flowchart TD
    A([Arriving Line-haul Truck]) --> B[Scan Package Barcode at Dock]
    B --> C{Scan Valid?}
    C -- No --> D[Log Exception: Unreadable Barcode]
    C -- Yes --> E[POST /parcel-handling/receive]
    E --> F[Record Unloaded Event]
    F --> G[Transition Shipment to InSortingHub]
    G --> H[Sort by Destination Hub]
    H --> I[POST /parcel-handling/sort: SortedToLane / Bagged]
    I --> J[Move to Outbound Staging Bay]
    J --> K[POST /parcel-handling/stage: StagedInBay]
    K --> L[Assign to Outbound Dispatch Manifest]
    L --> M[POST /warehouse-dispatches/{id}/confirm]
    M --> N[Transition Shipment to InTransit]
    N --> O([Departing Line-haul to Next Hub])
```

---

## 4. Stock Transfer Between Warehouses

```mermaid
flowchart LR
    subgraph Source ["Source Warehouse"]
        S1[Create Stock Transfer] --> S2[Approve Transfer]
        S2 --> S3[Ship Transfer]
        S3 --> S4[Deduct QuantityOnHand<br/>Log TransferOut Transaction]
    end

    subgraph Transit ["In-Transit"]
        S4 --> T1[Status: InTransit]
    end

    subgraph Destination ["Destination Warehouse"]
        T1 --> D1[Receive Goods at Destination]
        D1 --> D2[Add QuantityOnHand<br/>Log TransferIn Transaction]
        D2 --> D3[Status: Received]
    end
```
