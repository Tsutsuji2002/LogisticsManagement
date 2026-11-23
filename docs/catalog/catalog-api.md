# Catalog — REST API Specification

All Catalog endpoints follow standard API conventions:
- Base prefix: `/api/v1/catalog`
- Authentication: Bearer JWT in `Authorization` header
- Envelope: All responses are wrapped in `ApiResponse<T>` or `ApiResponse<PagedResponse<T>>`

---

## 1. Product Categories

### 1.1 List Categories (Paged)
- **Route**: `GET /api/v1/catalog/categories`
- **Permission**: `catalog.categories.read`
- **Query Parameters**:
  - `search` (string, optional): Search by code, name, or description.
  - `isActive` (bool, optional): Filter by active status.
  - `parentCategoryId` (long, optional): Filter by parent category ID.
  - `page` (int, default: 1): Page number.
  - `pageSize` (int, default: 20): Items per page.
- **Response**: `200 OK`
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": 1,
        "publicId": "e1f2a3b4-...",
        "code": "ELEC",
        "name": "Consumer Electronics",
        "description": "Smartphones, laptops, and gadgets",
        "parentCategoryId": null,
        "parentCategoryName": null,
        "isActive": true,
        "createdAt": "2026-11-20T10:00:00Z",
        "createdBy": "admin"
      }
    ],
    "page": 1,
    "pageSize": 20,
    "totalCount": 1,
    "totalPages": 1
  }
}
```

### 1.2 Get Category Hierarchy Tree
- **Route**: `GET /api/v1/catalog/categories/tree`
- **Permission**: `catalog.categories.read`
- **Query Parameters**:
  - `activeOnly` (bool, default: false): Include only active categories.
- **Response**: `200 OK`
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "publicId": "e1f2a3b4-...",
      "code": "ELEC",
      "name": "Consumer Electronics",
      "isActive": true,
      "productCount": 15,
      "children": [
        {
          "id": 2,
          "publicId": "f2a3b4c5-...",
          "code": "SMARTPHONES",
          "name": "Smartphones & Tablets",
          "isActive": true,
          "productCount": 10,
          "children": []
        }
      ]
    }
  ]
}
```

### 1.3 Get Category by ID or Code
- **Route**: `GET /api/v1/catalog/categories/{id}`
- **Permission**: `catalog.categories.read`
- **Response**: `200 OK` (`CategoryDetailDto`)

### 1.4 Create Category
- **Route**: `POST /api/v1/catalog/categories`
- **Permission**: `catalog.categories.create`
- **Request Body**:
```json
{
  "code": "CAT-APPAREL",
  "name": "Fashion & Apparel",
  "description": "Clothing, footwear, and accessories",
  "parentCategoryId": null
}
```
- **Response**: `201 Created`

### 1.5 Update Category
- **Route**: `PUT /api/v1/catalog/categories/{id}`
- **Permission**: `catalog.categories.update`
- **Request Body**:
```json
{
  "name": "Fashion & Premium Apparel",
  "description": "Updated description",
  "parentCategoryId": null
}
```
- **Response**: `200 OK`

### 1.6 Update Category Status
- **Route**: `PATCH /api/v1/catalog/categories/{id}/status`
- **Permission**: `catalog.categories.manage-status`
- **Request Body**:
```json
{
  "isActive": false,
  "reason": "Seasonal category retirement"
}
```
- **Response**: `200 OK`

### 1.7 Delete Category
- **Route**: `DELETE /api/v1/catalog/categories/{id}`
- **Permission**: `catalog.categories.delete`
- **Response**: `200 OK`

---

## 2. Products

### 2.1 List Products (Paged)
- **Route**: `GET /api/v1/catalog/products`
- **Permission**: `catalog.products.read`
- **Query Parameters**:
  - `search`, `categoryId`, `isActive`, `unitOfMeasureId`, `isDangerous`, `isPerishable`, `minWeight`, `maxWeight`, `page`, `pageSize`
- **Response**: `200 OK` (`PagedResponse<ProductDto>`)

### 2.2 Product Autocomplete Search
- **Route**: `GET /api/v1/catalog/products/search`
- **Permission**: `catalog.products.read`
- **Query Parameters**:
  - `q` (string): Search query text.
  - `limit` (int, default: 20): Max results.
- **Response**: `200 OK` (`IReadOnlyList<ProductSearchResultDto>`)

### 2.3 Get Product by SKU or Barcode
- **Route**: `GET /api/v1/catalog/products/code/{code}`
- **Permission**: `catalog.products.read`
- **Response**: `200 OK` (`ProductDto`)

### 2.4 Get Product by ID
- **Route**: `GET /api/v1/catalog/products/{id}`
- **Permission**: `catalog.products.read`
- **Response**: `200 OK` (`ProductDetailDto`)

### 2.5 Create Product
- **Route**: `POST /api/v1/catalog/products`
- **Permission**: `catalog.products.create`
- **Request Body**:
```json
{
  "categoryId": 1,
  "sku": "SKU-IPHONE-15-PRO",
  "barcode": "8938500011223",
  "name": "Apple iPhone 15 Pro Max 256GB",
  "description": "Natural Titanium",
  "unitOfMeasureId": 4,
  "weightKg": 0.221,
  "lengthCm": 15.99,
  "widthCm": 7.67,
  "heightCm": 0.82,
  "declaredValue": 29990000,
  "currencyId": 1,
  "isPerishable": false,
  "isDangerous": false
}
```
- **Response**: `201 Created`

### 2.6 Update Product
- **Route**: `PUT /api/v1/catalog/products/{id}`
- **Permission**: `catalog.products.update`
- **Response**: `200 OK`

### 2.7 Update Product Status
- **Route**: `PATCH /api/v1/catalog/products/{id}/status`
- **Permission**: `catalog.products.manage-status`
- **Response**: `200 OK`

### 2.8 Delete Product
- **Route**: `DELETE /api/v1/catalog/products/{id}`
- **Permission**: `catalog.products.delete`
- **Response**: `200 OK`

---

## 3. Units of Measure

- `GET /api/v1/catalog/units` — `catalog.units.read`
- `GET /api/v1/catalog/units/search` — `catalog.units.read`
- `GET /api/v1/catalog/units/{id}` — `catalog.units.read`
- `POST /api/v1/catalog/units` — `catalog.units.manage`
- `PUT /api/v1/catalog/units/{id}` — `catalog.units.manage`
- `PATCH /api/v1/catalog/units/{id}/status` — `catalog.units.manage`
- `DELETE /api/v1/catalog/units/{id}` — `catalog.units.manage`

---

## 4. Logistics Services

- `GET /api/v1/catalog/services` — `catalog.services.read`
- `GET /api/v1/catalog/services/{code}` — `catalog.services.read`
