# Carrier Management & Integration API Reference

All carrier endpoints follow the standard REST conventions with `/api/v1/` prefix, requiring JWT bearer authentication and role permissions (except unauthenticated webhook callbacks).

---

## 1. Carrier Endpoints (`/api/v1/carriers`)

### `GET /api/v1/carriers`
- **Permission**: `carrier.carriers.read`
- **Query Params**: `search`, `isActive`, `page` (default: 1), `pageSize` (default: 20)
- **Response**: `ApiResponse<PagedResponse<CarrierDto>>`

### `GET /api/v1/carriers/{identifier}`
- **Permission**: `carrier.carriers.read`
- **Path Params**: `identifier` (Carrier ID, PublicId, or Code like `GHN`)
- **Response**: `ApiResponse<CarrierDetailDto>` including child `Services` and `TrackingMappings`

### `POST /api/v1/carriers`
- **Permission**: `carrier.carriers.manage`
- **Request Body**:
```json
{
  "code": "GHN",
  "name": "Giao Hang Nhanh",
  "contactPhone": "19001206",
  "contactEmail": "contact@ghn.vn",
  "websiteUrl": "https://ghn.vn",
  "apiBaseUrl": "https://api.ghn.vn/v2",
  "trackingUrlTemplate": "https://donhang.ghn.vn/?order_code={0}"
}
```
- **Response**: 201 Created with `ApiResponse<CarrierDetailDto>`

### `PUT /api/v1/carriers/{identifier}`
- **Permission**: `carrier.carriers.manage`
- **Request Body**: `UpdateCarrierRequest` (`name`, `contactPhone`, `contactEmail`, `websiteUrl`, `apiBaseUrl`, `trackingUrlTemplate`)
- **Response**: 200 OK with `ApiResponse<CarrierDetailDto>`

### `PATCH /api/v1/carriers/{identifier}/status?isActive={bool}`
- **Permission**: `carrier.carriers.manage`
- **Response**: 200 OK with `ApiResponse<CarrierDetailDto>`

---

## 2. Carrier Services Endpoints (`/api/v1/carriers/{carrierIdentifier}/services`)

### `GET /api/v1/carriers/{carrierIdentifier}/services`
- **Permission**: `carrier.carriers.read`
- **Response**: `ApiResponse<IReadOnlyList<CarrierServiceDto>>`

### `POST /api/v1/carriers/{carrierIdentifier}/services`
- **Permission**: `carrier.carriers.manage`
- **Request Body**:
```json
{
  "serviceCode": "FAST",
  "serviceName": "GHTK Fast Delivery",
  "description": "Standard ground express",
  "estimatedTransitHours": 24,
  "maxWeightKg": 25.0,
  "maxLengthCm": 120.0,
  "isCodSupported": true,
  "isInsuranceSupported": true
}
```
- **Response**: 201 Created with `ApiResponse<CarrierServiceDto>`

### `PUT /api/v1/carriers/{carrierIdentifier}/services/{serviceId}`
- **Permission**: `carrier.carriers.manage`
- **Response**: 200 OK with `ApiResponse<CarrierServiceDto>`

### `PATCH /api/v1/carriers/{carrierIdentifier}/services/{serviceId}/status?isActive={bool}`
- **Permission**: `carrier.carriers.manage`
- **Response**: 200 OK with `ApiResponse<CarrierServiceDto>`

---

## 3. Carrier Configuration Endpoints (`/api/v1/carriers/{carrierIdentifier}/configurations`)

### `GET /api/v1/carriers/{carrierIdentifier}/configurations`
- **Permission**: `carrier.carriers.manage`
- **Response**: `ApiResponse<IReadOnlyList<CarrierConfigurationDto>>`
- **Security Note**: Sensitive passwords and secrets are masked as boolean flags (`hasApiKey`, `hasPassword`, `hasWebhookSecret`).

### `PUT /api/v1/carriers/{carrierIdentifier}/configurations`
- **Permission**: `carrier.carriers.manage`
- **Request Body**:
```json
{
  "environment": "Sandbox",
  "apiKey": "carrier-api-key",
  "clientId": "client-id",
  "username": "api-user",
  "password": "api-password",
  "webhookSecret": "webhook-secret-token"
}
```
- **Response**: 200 OK with `ApiResponse<CarrierConfigurationDto>`

---

## 4. Carrier Integration Operations (`/api/v1/carrier-integration`)

### `POST /api/v1/carrier-integration/book`
- **Permission**: `carrier.carriers.manage`
- **Request Body**:
```json
{
  "shipmentIdentifier": "SHIP-2026-0001",
  "carrierServiceCode": "STANDARD"
}
```
- **Response**:
```json
{
  "success": true,
  "data": {
    "success": true,
    "carrierTrackingNumber": "GHN20261123153012984",
    "carrierOrderCode": "ORD_GHN_491823",
    "totalFee": 35000,
    "labelUrl": "https://api.ghn.vn/labels/GHN20261123153012984.pdf"
  }
}
```

### `POST /api/v1/carrier-integration/cancel`
- **Permission**: `carrier.carriers.manage`
- **Request Body**: `{ "shipmentIdentifier": "SHIP-2026-0001", "reason": "Customer cancellation" }`

### `POST /api/v1/carrier-integration/rates`
- **Permission**: `carrier.carriers.read`
- **Request Body**: `{ "carrierId": 1, "weightKg": 2.5, "lengthCm": 20, "widthCm": 15, "heightCm": 10 }`

### `GET /api/v1/carrier-integration/tracking/{carrierCode}/{trackingNumber}`
- **Permission**: `carrier.carriers.read`
- **Response**: `ApiResponse<CarrierTrackingQueryDto>`
