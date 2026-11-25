# API Guidelines & Conventions

## 1. Standard Response Envelope

All API endpoints return data enveloped in `ApiResponse<T>`:

```json
{
  "success": true,
  "data": {
    "status": "Healthy",
    "environment": "Development",
    "timestamp": "2026-11-18T09:30:00Z",
    "version": "1.0.0"
  },
  "message": null,
  "errors": null
}
```

For failed responses:

```json
{
  "success": false,
  "data": null,
  "message": "The requested shipment was not found.",
  "errors": ["Shipment ID 9f91a0c7-... does not exist."]
}
```

## 2. Standard Pagination Envelope

Paginated endpoints accept query parameters extending `PagedRequest`:
- `Page` (default: 1)
- `PageSize` (default: 20)
- `SortBy` (optional field name)
- `SortDescending` (boolean)

And return `ApiResponse<PagedResponse<T>>`:

```json
{
  "success": true,
  "data": {
    "items": [...],
    "totalCount": 150,
    "page": 1,
    "pageSize": 20,
    "totalPages": 8,
    "hasPreviousPage": false,
    "hasNextPage": true
  }
}
```

## 3. Global Exception Handling

Unhandled exceptions are intercepted by `GlobalExceptionMiddleware`:
- `DomainException`: returns HTTP 400 Bad Request
- `KeyNotFoundException`: returns HTTP 404 Not Found
- `UnauthorizedAccessException`: returns HTTP 401 Unauthorized
- Other unexpected exceptions: logged via Serilog and return HTTP 500 Internal Server Error with sanitized error messages

## 4. Module API Documentation Index

- **Notification & Template API**: [`docs/notification/notification-api.md`](../notification/notification-api.md) (`/api/v1/notifications`, `/api/v1/notification-templates`)
- **Background Processing & Scheduled Reports API**: [`docs/background-processing/background-processing-api.md`](../background-processing/background-processing-api.md) (`/api/v1/background-jobs`, `/api/v1/scheduled-reports`)
- **COD Management API**: [`docs/cod/cod-api.md`](../cod/cod-api.md)
- **Finance & Billing API**: [`docs/finance/finance-api.md`](../finance/finance-api.md)
- **Warehouse & Inventory API**: [`docs/warehouse/warehouse-api.md`](../warehouse/warehouse-api.md)
- **Carrier Integration API**: [`docs/carrier-integration/carrier-api.md`](../carrier-integration/carrier-api.md)
- **Shipping API**: [`docs/shipping/shipment-api.md`](../shipping/shipment-api.md)
- **Sales Order API**: [`docs/sales/order-api.md`](../sales/order-api.md)
- **CRM Customer API**: [`docs/crm/customer-api.md`](../crm/customer-api.md)
- **Catalog API**: [`docs/catalog/catalog-api.md`](../catalog/catalog-api.md)
