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
