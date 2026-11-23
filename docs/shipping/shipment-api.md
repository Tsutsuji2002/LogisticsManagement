# Shipment Management API Specification

Base URL: `/api/v1`  
Authentication: `Bearer <jwt_token>`  
Standard Envelope: `ApiResponse<T>` or `ApiResponse<PagedResponse<T>>`

---

## 1. Shipments (`/api/v1/shipments`)

| Method | Endpoint | Permission | Description |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/v1/shipments` | `shipping.shipments.read` | Paginated search & filter across accessible branches |
| `GET` | `/api/v1/shipments/{id}` | `shipping.shipments.read` | Get shipment details by ID, GUID, or TrackingNumber |
| `GET` | `/api/v1/shipments/number/{shipmentNumber}` | `shipping.shipments.read` | Lookup shipment by TrackingNumber |
| `GET` | `/api/v1/shipments/by-order/{orderId}` | `shipping.shipments.read` | List shipments created from a specific order |
| `POST` | `/api/v1/shipments/from-order` | `shipping.shipments.create` | Create shipment from confirmed sales order |
| `PUT` | `/api/v1/shipments/{id}` | `shipping.shipments.update` | Update editable fields of draft / ready shipment |
| `POST` | `/api/v1/shipments/{id}/cancel` | `shipping.shipments.cancel` | Cancel shipment |
| `POST` | `/api/v1/shipments/{id}/transitions` | `shipping.shipments.manage-status` | Execute state machine status transition |
| `GET` | `/api/v1/shipments/{id}/history` | `shipping.tracking.read` | Retrieve immutable status history |

---

## 2. Shipment Packages (`/api/v1/shipments/{shipmentId}/packages`)

| Method | Endpoint | Permission | Description |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/v1/shipments/{shipmentId}/packages` | `shipping.shipments.read` | List all packages in shipment |
| `GET` | `/api/v1/shipments/{shipmentId}/packages/{packageId}` | `shipping.shipments.read` | Get package by ID or code |
| `POST` | `/api/v1/shipments/{shipmentId}/packages` | `shipping.shipments.update` | Add new package |
| `PUT` | `/api/v1/shipments/{shipmentId}/packages/{packageId}` | `shipping.shipments.update` | Update package dimensions / status |
| `DELETE` | `/api/v1/shipments/{shipmentId}/packages/{packageId}` | `shipping.shipments.update` | Remove package |

---

## 3. Shipment Addresses (`/api/v1/shipments/{shipmentId}/addresses`)

| Method | Endpoint | Permission | Description |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/v1/shipments/{shipmentId}/addresses` | `shipping.shipments.read` | List snapshot addresses |
| `GET` | `/api/v1/shipments/{shipmentId}/addresses/{addressType}` | `shipping.shipments.read` | Get address by type (Sender/Recipient) |
| `PUT` | `/api/v1/shipments/{shipmentId}/addresses/{addressType}` | `shipping.shipments.update` | Set or update snapshot address |

---

## 4. Pickup Requests (`/api/v1/pickup-requests`)

| Method | Endpoint | Permission | Description |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/v1/pickup-requests` | `shipping.pickups.read` | Search and list pickup requests |
| `GET` | `/api/v1/pickup-requests/{id}` | `shipping.pickups.read` | Get pickup request details |
| `POST` | `/api/v1/pickup-requests` | `shipping.pickups.create` | Create new pickup request |
| `PUT` | `/api/v1/pickup-requests/{id}` | `shipping.pickups.manage` | Update pickup scheduling |
| `POST` | `/api/v1/pickup-requests/{id}/assign/{employeeId}` | `shipping.pickups.manage` | Assign driver to pickup |
| `POST` | `/api/v1/pickup-requests/{id}/confirm` | `shipping.pickups.manage` | Mark driver arrived |
| `POST` | `/api/v1/pickup-requests/{id}/complete` | `shipping.pickups.manage` | Complete pickup with parcel count |
| `POST` | `/api/v1/pickup-requests/{id}/cancel` | `shipping.pickups.manage` | Cancel pickup request |

---

## 5. Shipment Assignments (`/api/v1/shipments/{shipmentId}/assignments`)

| Method | Endpoint | Permission | Description |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/v1/shipments/{shipmentId}/assignments` | `shipping.shipments.read` | List assignments |
| `GET` | `/api/v1/shipments/{shipmentId}/assignments/{assignmentId}` | `shipping.shipments.read` | Get assignment by ID |
| `POST` | `/api/v1/shipments/{shipmentId}/assignments` | `shipping.shipments.assign` | Create assignment |
| `PUT` | `/api/v1/shipments/{shipmentId}/assignments/{assignmentId}` | `shipping.shipments.assign` | Update assignment status |

---

## 6. Delivery Attempts (`/api/v1/shipments/{shipmentId}/delivery-attempts`)

| Method | Endpoint | Permission | Description |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/v1/shipments/{shipmentId}/delivery-attempts` | `shipping.shipments.read` | List attempts |
| `GET` | `/api/v1/shipments/{shipmentId}/delivery-attempts/{attemptId}` | `shipping.shipments.read` | Get attempt details |
| `POST` | `/api/v1/shipments/{shipmentId}/delivery-attempts` | `shipping.shipments.deliver` | Record delivery attempt |

---

## 7. Proof of Delivery (`/api/v1/shipments/{shipmentId}/proof-of-delivery`)

| Method | Endpoint | Permission | Description |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/v1/shipments/{shipmentId}/proof-of-delivery` | `shipping.shipments.read` | Retrieve POD |
| `POST` | `/api/v1/shipments/{shipmentId}/proof-of-delivery?attemptId={id}` | `shipping.shipments.deliver` | Record POD and complete delivery |

---

## 8. Returns (`/api/v1/returns`)

| Method | Endpoint | Permission | Description |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/v1/returns` | `shipping.returns.read` | List return requests |
| `GET` | `/api/v1/returns/{id}` | `shipping.returns.read` | Get return request |
| `POST` | `/api/v1/returns` | `shipping.returns.manage` | Request return for shipment |
| `POST` | `/api/v1/returns/{id}/process` | `shipping.returns.manage` | Approve or reject return |

---

## 9. Shipment Routes (`/api/v1/shipments/{shipmentId}/routes`)

| Method | Endpoint | Permission | Description |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/v1/shipments/{shipmentId}/routes` | `shipping.routes.read` | List route legs |
| `GET` | `/api/v1/shipments/{shipmentId}/routes/{routeId}` | `shipping.routes.read` | Get route leg |
| `POST` | `/api/v1/shipments/{shipmentId}/routes` | `shipping.routes.manage` | Add route leg |
| `PUT` | `/api/v1/shipments/{shipmentId}/routes/{routeId}` | `shipping.routes.manage` | Update leg status / timing |
| `DELETE` | `/api/v1/shipments/{shipmentId}/routes/{routeId}` | `shipping.routes.manage` | Delete route leg |
