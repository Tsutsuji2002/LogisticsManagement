# COD Management REST API Specification

## 1. Overview & Conventions

All endpoints require:
- HTTPS and JWT Bearer authentication: `Authorization: Bearer <token>`.
- Standard API responses wrapped in `ApiResponse<T>`.
- Granular permission checks via `[HasPermission(...)]`.
- Branch-level data isolation based on authenticated user tokens.

---

## 2. COD Collections Endpoints (`/api/v1/cod/collections`)

### 2.1 Get Collections (Paged)
- **Method**: `GET /api/v1/cod/collections`
- **Permission**: `cod.collections.read`
- **Query Parameters**:
  - `branchId` (long, optional)
  - `employeeId` (long, optional)
  - `customerId` (long, optional)
  - `status` (string, optional: `CollectedByDriver`, `DepositedToBranch`, `VerifiedByCashier`, `RemittedToCustomer`)
  - `isRemitted` (bool, optional)
  - `fromDate` (DateOnly, optional)
  - `toDate` (DateOnly, optional)
  - `page` (int, default: 1)
  - `pageSize` (int, default: 20)
- **Response**: `200 OK` with `ApiResponse<PagedResponse<CodCollectionDto>>`

### 2.2 Get Collection by ID
- **Method**: `GET /api/v1/cod/collections/{id:long}`
- **Permission**: `cod.collections.read`
- **Response**: `200 OK` with `ApiResponse<CodCollectionDto>` or `404 Not Found`

### 2.3 Get Collection by Shipment ID
- **Method**: `GET /api/v1/cod/collections/shipments/{shipmentId:long}`
- **Permission**: `cod.collections.read`
- **Response**: `200 OK` with `ApiResponse<CodCollectionDto>` or `404 Not Found`

### 2.4 Get Collection Summary
- **Method**: `GET /api/v1/cod/collections/summary`
- **Permission**: `cod.collections.read`
- **Query Parameters**: `branchId`, `fromDate`, `toDate`
- **Response**: `200 OK` with `ApiResponse<CodCollectionSummaryDto>`

### 2.5 Create Collection
- **Method**: `POST /api/v1/cod/collections`
- **Permission**: `cod.collections.create`
- **Request Body**:
```json
{
  "shipmentId": 105,
  "collectedAmount": 450000.00,
  "collectedByEmployeeId": 12,
  "driverReceiptNumber": "REC-202611-0091",
  "notes": "Collected cash at recipient doorstep"
}
```
- **Response**: `201 Created` with `ApiResponse<CodCollectionDto>`

### 2.6 Record Field Collection
- **Method**: `POST /api/v1/cod/collections/{id:long}/collect`
- **Permission**: `cod.collections.collect`
- **Request Body**:
```json
{
  "collectedAmount": 450000.00,
  "driverReceiptNumber": "REC-202611-0091",
  "notes": "Paid exact cash amount"
}
```
- **Response**: `200 OK` with `ApiResponse<CodCollectionDto>`

### 2.7 Deposit to Branch Counter
- **Method**: `POST /api/v1/cod/collections/{id:long}/deposit`
- **Permission**: `cod.collections.manage`
- **Request Body**:
```json
{
  "notes": "Turned in cash pouch to Hanoi hub cashier desk"
}
```
- **Response**: `200 OK` with `ApiResponse<CodCollectionDto>`

### 2.8 Cashier Verification
- **Method**: `POST /api/v1/cod/collections/{id:long}/verify`
- **Permission**: `cod.collections.verify`
- **Request Body**:
```json
{
  "notes": "Cash counted and verified in safe"
}
```
- **Response**: `200 OK` with `ApiResponse<CodCollectionDto>`

### 2.9 Record Collection Failure
- **Method**: `POST /api/v1/cod/collections/{id:long}/record-failure`
- **Permission**: `cod.collections.manage`
- **Request Body**:
```json
{
  "reasonCode": "CustomerRefusedPayment",
  "notes": "Recipient stated order was duplicate"
}
```
- **Response**: `200 OK` with `ApiResponse<CodCollectionDto>`

---

## 3. COD Remittances Endpoints (`/api/v1/cod/remittances`)

### 3.1 Get Remittances (Paged)
- **Method**: `GET /api/v1/cod/remittances`
- **Permission**: `cod.remittances.read`
- **Query Parameters**: `customerId`, `status`, `fromDate`, `toDate`, `page`, `pageSize`
- **Response**: `200 OK` with `ApiResponse<PagedResponse<CodRemittanceDto>>`

### 3.2 Get Remittance by ID
- **Method**: `GET /api/v1/cod/remittances/{id:long}`
- **Permission**: `cod.remittances.read`
- **Response**: `200 OK` with `ApiResponse<CodRemittanceDto>` or `404 Not Found`

### 3.3 Create Remittance (Draft)
- **Method**: `POST /api/v1/cod/remittances`
- **Permission**: `cod.remittances.create`
- **Request Body**:
```json
{
  "customerId": 8,
  "currencyId": 1,
  "paymentMethod": "BankTransfer",
  "paymentReference": null,
  "bankName": "Vietcombank",
  "bankAccount": "0071001234567",
  "accountHolderName": "Công ty TNHH Bán Lẻ Công Nghệ",
  "notes": "Batch remittance for Week 47",
  "collectionIds": [101, 102, 103],
  "serviceFeesDeducted": 75000.00
}
```
- **Response**: `201 Created` with `ApiResponse<CodRemittanceDto>`

### 3.4 Add Item to Draft Remittance
- **Method**: `POST /api/v1/cod/remittances/{id:long}/items`
- **Permission**: `cod.remittances.manage`
- **Request Body**:
```json
{
  "codCollectionId": 104,
  "deductedShippingFee": 25000.00
}
```
- **Response**: `200 OK` with `ApiResponse<CodRemittanceDto>`

### 3.5 Remove Item from Draft Remittance
- **Method**: `DELETE /api/v1/cod/remittances/{id:long}/items/{itemId:long}`
- **Permission**: `cod.remittances.manage`
- **Response**: `200 OK` with `ApiResponse<CodRemittanceDto>`

### 3.6 Confirm Remittance (Payout Execution)
- **Method**: `POST /api/v1/cod/remittances/{id:long}/confirm`
- **Permission**: `cod.remittances.confirm`
- **Request Body**:
```json
{
  "paymentReference": "VCB-FT-20261123-998811",
  "actualRemittedAmount": 1425000.00,
  "notes": "Bank transfer completed successfully"
}
```
- **Response**: `200 OK` with `ApiResponse<CodRemittanceDto>`

### 3.7 Cancel Remittance
- **Method**: `POST /api/v1/cod/remittances/{id:long}/cancel`
- **Permission**: `cod.remittances.manage`
- **Request Body**:
```json
{
  "reason": "Customer updated receiving bank account"
}
```
- **Response**: `200 OK` with `ApiResponse<CodRemittanceDto>`

---

## 4. COD Operations Endpoints (`/api/v1/cod/operations`)

### 4.1 Get My Collections (Current Authenticated Driver)
- **Method**: `GET /api/v1/cod/operations/my-collections`
- **Permission**: `cod.operations.read-own`
- **Query Parameters**: `status`, `page`, `pageSize`
- **Response**: `200 OK` with `ApiResponse<PagedResponse<CodCollectionDto>>`

### 4.2 Get My Summary (Current Authenticated Driver)
- **Method**: `GET /api/v1/cod/operations/my-summary`
- **Permission**: `cod.operations.read-own`
- **Response**: `200 OK` with `ApiResponse<EmployeeCodSummaryDto>`

### 4.3 Get Employee Collections (Manager View)
- **Method**: `GET /api/v1/cod/operations/employees/{employeeId:long}/collections`
- **Permission**: `cod.operations.read-branch`
- **Response**: `200 OK` with `ApiResponse<PagedResponse<CodCollectionDto>>`

### 4.4 Get Employee Summary (Manager View)
- **Method**: `GET /api/v1/cod/operations/employees/{employeeId:long}/summary`
- **Permission**: `cod.operations.read-branch`
- **Response**: `200 OK` with `ApiResponse<EmployeeCodSummaryDto>`

---

## 5. COD Reconciliation Endpoints (`/api/v1/cod/reconciliation`)

### 5.1 Reconciliation Summary
- **Method**: `GET /api/v1/cod/reconciliation/summary`
- **Permission**: `cod.reconciliation.read`
- **Query Parameters**: `branchId`, `fromDate`, `toDate`
- **Response**: `200 OK` with `ApiResponse<CodReconciliationSummaryDto>`

### 5.2 Reconcile Employee / Driver
- **Method**: `GET /api/v1/cod/reconciliation/employees/{employeeId:long}`
- **Permission**: `cod.reconciliation.read`
- **Response**: `200 OK` with `ApiResponse<EmployeeCodReconciliationDto>`

### 5.3 Reconcile Branch Vault
- **Method**: `GET /api/v1/cod/reconciliation/branches/{branchId:long}`
- **Permission**: `cod.reconciliation.read`
- **Response**: `200 OK` with `ApiResponse<BranchCodReconciliationDto>`

### 5.4 Reconcile Remittance Batch
- **Method**: `GET /api/v1/cod/reconciliation/remittances/{remittanceId:long}`
- **Permission**: `cod.reconciliation.read`
- **Response**: `200 OK` with `ApiResponse<RemittanceReconciliationDto>`
