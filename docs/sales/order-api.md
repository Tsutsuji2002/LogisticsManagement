# Sales Module — Order Management API Reference

Base path: `/api/v1`  
Authentication: JWT Bearer (`Authorization: Bearer <token>`)  
All responses are wrapped in `ApiResponse<T>`.

---

## Orders

### List Orders

```
GET /api/v1/orders
```

**Query Parameters:**

| Parameter       | Type    | Description                                      |
|-----------------|---------|--------------------------------------------------|
| `branchId`      | long?   | Filter by branch (defaults to user's branches)   |
| `customerId`    | long?   | Filter by customer                               |
| `status`        | string? | Filter by order status (Draft, Confirmed, etc.)  |
| `orderNumber`   | string? | Search by order number prefix                    |
| `fromDate`      | date?   | Created from date (UTC)                          |
| `toDate`        | date?   | Created to date (UTC)                            |
| `page`          | int     | Page number (default: 1)                         |
| `pageSize`      | int     | Page size (default: 20, max: 100)                |

**Response:** `ApiResponse<PagedResult<OrderDto>>`

**Required Permission:** `Sales.OrdersRead`

---

### Get Order

```
GET /api/v1/orders/{id}
```

**Response:** `ApiResponse<OrderDetailDto>`

**Required Permission:** `Sales.OrdersRead`

---

### Create Order

```
POST /api/v1/orders
```

**Request Body:**

```json
{
  "customerId": 1,
  "branchId": 1,
  "codAmount": 500000,
  "notes": "Urgent delivery",
  "requestedPickupDate": "2026-11-21T08:00:00Z",
  "requiredDeliveryDate": "2026-11-23T17:00:00Z",
  "pickupAddress": {
    "contactName": "Warehouse Sender",
    "phoneNumber": "0901112222",
    "addressLine1": "100 Nguyen Van Linh",
    "formattedAddress": "100 Nguyen Van Linh, District 7, Ho Chi Minh City"
  },
  "deliveryAddress": {
    "contactName": "Consignee Recipient",
    "phoneNumber": "0903334444",
    "addressLine1": "200 Hai Ba Trung"
  },
  "items": [
    {
      "productId": null,
      "productName": "Carton Box Small",
      "quantity": 5,
      "unitPrice": 20000,
      "weightKg": 0.5,
      "notes": "Fragile"
    }
  ],
  "packages": [
    {
      "weightKg": 2.5,
      "lengthCm": 30,
      "widthCm": 20,
      "heightCm": 15,
      "declaredValue": 100000,
      "description": "Electronics"
    }
  ]
}
```

**Response:** `201 Created` → `ApiResponse<OrderDetailDto>`

**Required Permission:** `Sales.OrdersCreate`

**Notes:**
- `customerId` is required.
- If `branchId` is omitted, the user's default branch is used.
- `pickupAddress` and `deliveryAddress` in the create request are saved as `Sender` and `Recipient` address types respectively.
- Order number is auto-generated (format: `ORD-YYYYMM-NNNN`).
- Initial status is always `Draft`.

---

### Update Order

```
PUT /api/v1/orders/{id}
```

**Request Body:**

```json
{
  "customerId": 1,
  "branchId": 1,
  "codAmount": 750000,
  "notes": "Updated delivery notes",
  "requestedPickupDate": "2026-11-22T09:00:00Z",
  "requiredDeliveryDate": "2026-11-24T17:00:00Z"
}
```

**Response:** `200 OK` → `ApiResponse<OrderDetailDto>`

**Required Permission:** `Sales.OrdersUpdate`

**Restriction:** Only `Draft` orders can be updated. Returns `409 Conflict` otherwise.

---

### Confirm Order

```
POST /api/v1/orders/{id}/confirm
```

**Request Body:** None

**Response:** `200 OK` → `ApiResponse<OrderDetailDto>`

**Required Permission:** `Sales.OrdersUpdate`

**Restriction:** Only `Draft` orders can be confirmed. Returns `409 Conflict` otherwise.

---

### Cancel Order

```
POST /api/v1/orders/{id}/cancel
```

**Request Body:**

```json
{
  "reason": "Customer requested cancellation due to change of plans"
}
```

**Response:** `200 OK` → `ApiResponse<OrderDetailDto>`

**Required Permission:** `Sales.OrdersCancel`

**Restrictions:**
- Returns `409 Conflict` if the order is already `Cancelled` or `Delivered`.
- Returns `409 Conflict` if any linked shipment is `InTransit` or `Delivered`.
- `reason` is required (minimum 5 characters).

---

### Delete Order

```
DELETE /api/v1/orders/{id}
```

**Response:** `200 OK` → `ApiResponse<bool>`

**Required Permission:** `Sales.OrdersUpdate`

**Restriction:** Only `Draft` orders can be deleted. Returns `409 Conflict` otherwise.

---

## Order Items

### List Order Items

```
GET /api/v1/orders/{orderId}/items
```

**Response:** `ApiResponse<IReadOnlyList<OrderItemDto>>`

**Required Permission:** `Sales.OrdersRead`

---

### Add Order Item

```
POST /api/v1/orders/{orderId}/items
```

**Request Body:**

```json
{
  "productId": null,
  "productName": "Custom Pallet",
  "quantity": 3,
  "unitPrice": 150000,
  "weightKg": 2.0,
  "notes": "Handle with care"
}
```

**Response:** `201 Created` → `ApiResponse<OrderItemDto>`

**Required Permission:** `Sales.OrdersUpdate`

**Restriction:** Items can only be added to `Draft` orders. Returns `409 Conflict` otherwise.

---

### Update Order Item

```
PUT /api/v1/orders/{orderId}/items/{itemId}
```

**Request Body:**

```json
{
  "productId": null,
  "productName": "Updated Product Name",
  "quantity": 5,
  "unitPrice": 175000,
  "weightKg": 2.5
}
```

**Response:** `200 OK` → `ApiResponse<OrderItemDto>`

**Required Permission:** `Sales.OrdersUpdate`

**Restriction:** Only in `Draft` status.

---

### Delete Order Item

```
DELETE /api/v1/orders/{orderId}/items/{itemId}
```

**Response:** `200 OK` → `ApiResponse<bool>`

**Required Permission:** `Sales.OrdersUpdate`

**Restriction:** Only in `Draft` status.

---

## Order Packages

### List Order Packages

```
GET /api/v1/orders/{orderId}/packages
```

**Response:** `ApiResponse<IReadOnlyList<OrderPackageDto>>`

**Required Permission:** `Sales.OrdersRead`

---

### Add Order Package

```
POST /api/v1/orders/{orderId}/packages
```

**Request Body:**

```json
{
  "weightKg": 5.0,
  "lengthCm": 40,
  "widthCm": 30,
  "heightCm": 20,
  "declaredValue": 250000,
  "description": "Fragile electronics",
  "trackingNumber": null
}
```

**Response:** `201 Created` → `ApiResponse<OrderPackageDto>`

**Required Permission:** `Sales.OrdersUpdate`

**Restriction:** Allowed in `Draft` or `Confirmed` status only.

**Auto-calculated field:** `VolumetricWeightKg = (L × W × H) / 5000`

---

### Update Order Package

```
PUT /api/v1/orders/{orderId}/packages/{packageId}
```

**Request Body:** Same as Add Package.

**Response:** `200 OK` → `ApiResponse<OrderPackageDto>`

**Required Permission:** `Sales.OrdersUpdate`

---

### Delete Order Package

```
DELETE /api/v1/orders/{orderId}/packages/{packageId}
```

**Response:** `200 OK` → `ApiResponse<bool>`

**Required Permission:** `Sales.OrdersUpdate`

---

## Order Addresses

### List Order Addresses

```
GET /api/v1/orders/{orderId}/addresses
```

**Response:** `ApiResponse<IReadOnlyList<OrderAddressDto>>`

**Required Permission:** `Sales.OrdersRead`

---

### Get Order Address by Type

```
GET /api/v1/orders/{orderId}/addresses/{addressType}
```

**Path Parameter:** `addressType` = `Sender` or `Recipient`

**Response:** `ApiResponse<OrderAddressDto>`

**Required Permission:** `Sales.OrdersRead`

---

### Set Order Address

```
PUT /api/v1/orders/{orderId}/addresses/{addressType}
```

**Path Parameter:** `addressType` = `Sender` or `Recipient`

**Request Body:**

```json
{
  "contactName": "Warehouse Dispatch",
  "phoneNumber": "0988776655",
  "email": "dispatch@company.vn",
  "companyName": "Vinamilk Distribution",
  "addressLine1": "Lot 4, Tan Binh Industrial Park",
  "addressLine2": "Gate 2",
  "ward": "Tan Binh Ward",
  "district": "Tan Binh",
  "city": "Ho Chi Minh City",
  "province": "Ho Chi Minh",
  "postalCode": "70000",
  "countryCode": "VN",
  "latitude": 10.8,
  "longitude": 106.65,
  "formattedAddress": "Lot 4, Tan Binh Industrial Park, Ho Chi Minh City",
  "notes": "Open Monday-Friday 8AM-5PM"
}
```

**Required fields:** `contactName`, `phoneNumber`, `addressLine1`

**Response:** `200 OK` → `ApiResponse<OrderAddressDto>`

**Required Permission:** `Sales.OrdersUpdate`

**Restriction:** Allowed in `Draft` or `Confirmed` status only.

---

## Response Models

### OrderDto

```json
{
  "id": 1,
  "publicId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "orderNumber": "ORD-202611-0001",
  "orderStatus": "Draft",
  "customerId": 1,
  "customerName": "Vinamilk Logistics Hub",
  "branchId": 1,
  "branchCode": "SGN",
  "branchName": "Saigon Central Branch",
  "codAmount": 500000,
  "declaredValue": 100000,
  "totalAmount": 100000,
  "totalWeightKg": 2.5,
  "totalVolumeM3": 0.009,
  "packageCount": 1,
  "itemCount": 1,
  "requestedPickupDate": "2026-11-21T08:00:00Z",
  "requiredDeliveryDate": "2026-11-23T17:00:00Z",
  "notes": "Urgent delivery",
  "createdAt": "2026-11-20T10:00:00Z"
}
```

### OrderDetailDto

Extends `OrderDto` with:
```json
{
  "...(all OrderDto fields)...",
  "confirmedAt": null,
  "cancelledAt": null,
  "cancelledBy": null,
  "cancellationReason": null,
  "pickupAddress": { "(OrderAddressDto)" },
  "deliveryAddress": { "(OrderAddressDto)" },
  "items": [ "(OrderItemDto)" ],
  "packages": [ "(OrderPackageDto)" ],
  "addresses": [ "(OrderAddressDto)" ]
}
```

### OrderItemDto

```json
{
  "id": 1,
  "publicId": "...",
  "orderId": 1,
  "productId": null,
  "productName": "Carton Box Small",
  "quantity": 5,
  "unitPrice": 20000,
  "totalPrice": 100000,
  "weightKg": 0.5,
  "notes": "Fragile"
}
```

### OrderPackageDto

```json
{
  "id": 1,
  "publicId": "...",
  "orderId": 1,
  "packageNumber": 1,
  "weightKg": 2.5,
  "lengthCm": 30,
  "widthCm": 20,
  "heightCm": 15,
  "volumetricWeightKg": 1.8,
  "declaredValue": 100000,
  "description": "Electronics",
  "trackingNumber": null
}
```

### OrderAddressDto

```json
{
  "id": 1,
  "orderId": 1,
  "addressType": "Sender",
  "contactName": "Warehouse Dispatch",
  "phoneNumber": "0988776655",
  "email": "dispatch@company.vn",
  "companyName": "Vinamilk Distribution",
  "addressLine1": "Lot 4, Tan Binh Industrial Park",
  "addressLine2": "Gate 2",
  "ward": "Tan Binh Ward",
  "district": "Tan Binh",
  "city": "Ho Chi Minh City",
  "province": "Ho Chi Minh",
  "postalCode": "70000",
  "countryCode": "VN",
  "latitude": 10.8,
  "longitude": 106.65,
  "formattedAddress": "Lot 4, Tan Binh Industrial Park, Ho Chi Minh City"
}
```

---

## HTTP Status Codes

| Code | Meaning                                           |
|------|---------------------------------------------------|
| 200  | OK — Successful GET, PUT, DELETE, or action       |
| 201  | Created — Successful POST (create)                |
| 400  | Bad Request — Validation failure                  |
| 401  | Unauthorized — Missing or invalid JWT             |
| 403  | Forbidden — Insufficient permissions or branch    |
| 404  | Not Found — Resource does not exist               |
| 409  | Conflict — Business rule violation (state machine)|
| 500  | Internal Server Error                             |
