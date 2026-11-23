# CRM — Customer Management API Specification

## 1. Overview & Conventions

All endpoints adhere to standard RESTful conventions under the prefix `/api/v1/customers`.
Responses use the standardized `ApiResponse<T>` envelope. Paged collections return `ApiResponse<PagedResponse<T>>`.

---

## 2. Endpoints Summary

### 2.1 Customer Master

| Method | Route | Permission | Description |
|---|---|---|---|
| `GET` | `/api/v1/customers` | `crm.customers.read` | Filtered, paged list of customers across authorized branches |
| `GET` | `/api/v1/customers/overview` | `crm.customers.overview.read` | High-level CRM metrics overview |
| `GET` | `/api/v1/customers/{id}` | `crm.customers.read` | Retrieve single customer (`?includeDetails=true` for full graph) |
| `POST` | `/api/v1/customers` | `crm.customers.create` | Create a new customer record |
| `PUT` | `/api/v1/customers/{id}` | `crm.customers.update` | Update customer demographic and credit profile |
| `PATCH` | `/api/v1/customers/{id}/status` | `crm.customers.status.manage` | Activate or deactivate customer |
| `DELETE` | `/api/v1/customers/{id}` | `crm.customers.delete` | Deactivate customer (with business history safeguard) |

### 2.2 Customer Contacts

| Method | Route | Permission | Description |
|---|---|---|---|
| `GET` | `/api/v1/customers/{customerId}/contacts` | `crm.contacts.read` | List contacts for customer |
| `GET` | `/api/v1/customers/{customerId}/contacts/{contactId}` | `crm.contacts.read` | Get single contact details |
| `POST` | `/api/v1/customers/{customerId}/contacts` | `crm.contacts.create` | Add contact person to customer |
| `PUT` | `/api/v1/customers/{customerId}/contacts/{contactId}` | `crm.contacts.update` | Update existing contact details |
| `PATCH` | `/api/v1/customers/{customerId}/contacts/{contactId}/primary` | `crm.contacts.manage` | Set contact as primary liaison |
| `PATCH` | `/api/v1/customers/{customerId}/contacts/{contactId}/status` | `crm.contacts.manage` | Update contact status |
| `DELETE` | `/api/v1/customers/{customerId}/contacts/{contactId}` | `crm.contacts.manage` | Delete contact person |

### 2.3 Customer Addresses

| Method | Route | Permission | Description |
|---|---|---|---|
| `GET` | `/api/v1/customers/{customerId}/addresses` | `crm.addresses.read` | List customer addresses |
| `GET` | `/api/v1/customers/{customerId}/addresses/{addressId}` | `crm.addresses.read` | Get address details |
| `POST` | `/api/v1/customers/{customerId}/addresses` | `crm.addresses.create` | Link new address to customer |
| `PUT` | `/api/v1/customers/{customerId}/addresses/{addressId}` | `crm.addresses.update` | Update address details |
| `PATCH` | `/api/v1/customers/{customerId}/addresses/{addressId}/default` | `crm.addresses.manage` | Set address as default for its type |
| `PATCH` | `/api/v1/customers/{customerId}/addresses/{addressId}/status` | `crm.addresses.manage` | Update address status |
| `DELETE` | `/api/v1/customers/{customerId}/addresses/{addressId}` | `crm.addresses.manage` | Unlink address |

### 2.4 Customer Contracts

| Method | Route | Permission | Description |
|---|---|---|---|
| `GET` | `/api/v1/customers/{customerId}/contracts` | `crm.contracts.read` | List contracts for customer |
| `GET` | `/api/v1/customers/{customerId}/contracts/{contractId}` | `crm.contracts.read` | Get contract details |
| `POST` | `/api/v1/customers/{customerId}/contracts` | `crm.contracts.create` | Create commercial contract |
| `PUT` | `/api/v1/customers/{customerId}/contracts/{contractId}` | `crm.contracts.update` | Update contract terms and dates |
| `PATCH` | `/api/v1/customers/{customerId}/contracts/{contractId}/status` | `crm.contracts.manage` | Transition contract status |

### 2.5 Customer Pricing Rules

| Method | Route | Permission | Description |
|---|---|---|---|
| `GET` | `/api/v1/customers/{customerId}/pricing-rules` | `crm.pricing-rules.read` | List pricing rules |
| `GET` | `/api/v1/customers/{customerId}/pricing-rules/applicable` | `crm.pricing-rules.read` | Retrieve applicable rules ordered by priority |
| `GET` | `/api/v1/customers/{customerId}/pricing-rules/{ruleId}` | `crm.pricing-rules.read` | Get pricing rule details |
| `POST` | `/api/v1/customers/{customerId}/pricing-rules` | `crm.pricing-rules.create` | Create customer pricing rule |
| `PUT` | `/api/v1/customers/{customerId}/pricing-rules/{ruleId}` | `crm.pricing-rules.update` | Update pricing rule |
| `PATCH` | `/api/v1/customers/{customerId}/pricing-rules/{ruleId}/status` | `crm.pricing-rules.manage` | Activate/deactivate pricing rule |
| `DELETE` | `/api/v1/customers/{customerId}/pricing-rules/{ruleId}` | `crm.pricing-rules.manage` | Delete pricing rule |

---

## 3. Sample Requests & Responses

### Create Customer

**Request**: `POST /api/v1/customers`
```json
{
  "branchId": 1,
  "customerCode": "CUST-HN-001",
  "customerType": "Corporate",
  "name": "Alpha Trading Logistics JSC",
  "tradeName": "Alpha Express",
  "taxCode": "0109988776",
  "email": "contact@alphatrading.vn",
  "phoneNumber": "+84901234567",
  "creditLimit": 100000000.0,
  "creditTermDays": 30,
  "paymentType": "PostpaidMonthly"
}
```

**Response**: `201 Created`
```json
{
  "success": true,
  "data": {
    "id": 1,
    "publicId": "e5b8d274-1234-4567-89ab-cdef01234567",
    "branchId": 1,
    "branchCode": "BR-HAN-01",
    "branchName": "Hanoi Central Hub",
    "customerCode": "CUST-HN-001",
    "customerType": "Corporate",
    "name": "Alpha Trading Logistics JSC",
    "tradeName": "Alpha Express",
    "taxCode": "0109988776",
    "email": "contact@alphatrading.vn",
    "phoneNumber": "+84901234567",
    "creditLimit": 100000000.0,
    "creditTermDays": 30,
    "paymentType": "PostpaidMonthly",
    "accountManagerEmployeeId": null,
    "accountManagerName": null,
    "isActive": true,
    "createdAt": "2026-11-20T14:30:00Z",
    "updatedAt": null
  },
  "message": "Customer created successfully.",
  "errorCode": null,
  "traceId": "7c1851e3-2fa6-40e9-b508-3e4e9415494f"
}
```
