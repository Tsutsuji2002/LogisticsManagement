# Finance & Billing API Reference

## 1. Overview & Base Conventions

All Finance endpoints are hosted under `/api/v1/finance/...`.
All requests require JWT authentication. Endpoints enforce fine-grained permissions via `[HasPermission(...)]` and branch-level data isolation.

---

## 2. Rate Cards Endpoints (`/api/v1/finance/rate-cards`)

| Method | Endpoint | Permission | Description |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/v1/finance/rate-cards` | `finance.rates.read` | Paginated search of shipping rate cards |
| `GET` | `/api/v1/finance/rate-cards/{id}` | `finance.rates.read` | Retrieve rate card by ID |
| `POST` | `/api/v1/finance/rate-cards` | `finance.rates.manage` | Create new shipping rate card |
| `PUT` | `/api/v1/finance/rate-cards/{id}` | `finance.rates.manage` | Update rate card details |
| `PATCH` | `/api/v1/finance/rate-cards/{id}/status` | `finance.rates.manage` | Toggle active/inactive status |
| `GET` | `/api/v1/finance/rate-cards/{id}/rules` | `finance.rates.read` | Retrieve all rules belonging to card |
| `POST` | `/api/v1/finance/rate-cards/{id}/rules` | `finance.rates.manage` | Add rule to shipping rate card |
| `PUT` | `/api/v1/finance/rate-cards/{id}/rules/{ruleId}` | `finance.rates.manage` | Update specific rate rule |
| `DELETE` | `/api/v1/finance/rate-cards/{id}/rules/{ruleId}` | `finance.rates.manage` | Delete rate rule |

---

## 3. Shipping Fees & Charges Endpoints (`/api/v1/finance/shipping-fees`)

| Method | Endpoint | Permission | Description |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/v1/finance/shipping-fees/calculate` | `finance.charges.calculate` | Calculate fee breakdown for shipment parameters |
| `GET` | `/api/v1/finance/shipping-fees/charges` | `finance.charges.read` | Paginated query of shipment charges |
| `GET` | `/api/v1/finance/shipping-fees/charges/{id}` | `finance.charges.read` | Retrieve shipment charge by ID |
| `POST` | `/api/v1/finance/shipping-fees/charges/{shipmentId}/adjustments` | `finance.charges.adjust` | Add manual fee adjustment to shipment |

---

## 4. Invoices Endpoints (`/api/v1/finance/invoices`)

| Method | Endpoint | Permission | Description |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/v1/finance/invoices` | `finance.invoices.read` | Paginated query of customer invoices |
| `GET` | `/api/v1/finance/invoices/{id}` | `finance.invoices.read` | Retrieve invoice details with line items |
| `POST` | `/api/v1/finance/invoices` | `finance.invoices.create` | Draft new customer invoice |
| `POST` | `/api/v1/finance/invoices/{id}/items` | `finance.invoices.create` | Add item to draft invoice |
| `DELETE` | `/api/v1/finance/invoices/{id}/items/{itemId}` | `finance.invoices.create` | Remove item from draft invoice |
| `POST` | `/api/v1/finance/invoices/{id}/issue` | `finance.invoices.issue` | Issue invoice & post ledger entry |
| `POST` | `/api/v1/finance/invoices/{id}/cancel` | `finance.invoices.cancel` | Cancel invoice & post reverse ledger entry |

---

## 5. Payments Endpoints (`/api/v1/finance/payments`)

| Method | Endpoint | Permission | Description |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/v1/finance/payments` | `finance.payments.read` | Paginated search of payments |
| `GET` | `/api/v1/finance/payments/{id}` | `finance.payments.read` | Retrieve payment by ID |
| `POST` | `/api/v1/finance/payments` | `finance.payments.manage` | Register payment receipt & create GL credit |
| `POST` | `/api/v1/finance/payments/{id}/allocations` | `finance.payments.allocate` | Allocate payment against unpaid invoice |
| `POST` | `/api/v1/finance/payments/{id}/allocations/{allocationId}/reverse` | `finance.payments.reverse` | Reverse payment allocation |

---

## 6. Customer Balances & Ledger Endpoints

| Method | Endpoint | Permission | Description |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/v1/finance/customer-balances` | `finance.balances.read` | List customer balances with branch filter |
| `GET` | `/api/v1/finance/customer-balances/{customerId}` | `finance.balances.read` | Retrieve customer exposure and balance |
| `POST` | `/api/v1/finance/customer-balances/{customerId}/reconcile` | `finance.balances.reconcile` | Reconcile recorded balance against journals |
| `GET` | `/api/v1/finance/ledger/entries` | `finance.ledger.read` | Paginated audit trail of GL journal entries |
