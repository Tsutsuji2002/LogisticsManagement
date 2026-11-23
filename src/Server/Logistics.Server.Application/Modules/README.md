# Application Modules

This folder hosts the application use cases, commands, queries, and validators for each business module:

- `Identity/`: Authentication commands, token generation, user management use cases.
- `Organization/`: Branch setup, hierarchy queries, operational hub assignments.
- `Customers/`: Customer registration, address verification, credit checks.
- `Orders/`: Order creation commands, state transition handlers, order queries.
- `Shipments/`: Shipment dispatching, milestone updates, barcode scanning workflows.
- `Carriers/`: Carrier rate queries, label generation, carrier webhook handlers.
- `Warehouses/`: Inbound receiving, put-away logic, picking and packing use cases.
- `Inventory/`: Stock adjustments, inventory transfers, low-stock alerts.
- `Finance/`: COD reconciliation commands, invoicing, settlement workflows.
- `Notifications/`: Event notification dispatchers (Email, SMS, Webhook).
- `Reporting/`: KPI generation queries, dashboard metric aggregation.
- `AuditLogging/`: Audit log queries, compliance exports.
