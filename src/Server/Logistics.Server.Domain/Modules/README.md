# Domain Modules

This folder organizes the domain models, value objects, domain events, and business rules for the modular monolith. Each module is encapsulated in its own folder:

- `Identity/`: User identities, roles, permissions, and credential policies.
- `Organization/`: Enterprise structure, branches, hubs, and operating centers.
- `Customers/`: Customer profiles, consignors, consignees, and contracts.
- `Orders/`: Logistics booking orders, line items, and fulfillment states.
- `Shipments/`: Waybills, packages, tracking milestones, and proof-of-delivery (POD).
- `Carriers/`: Carrier definitions, service levels, and carrier account settings.
- `Warehouses/`: Warehouse layout, storage zones, locations, and facilities.
- `Inventory/`: Stock keeping units (SKUs), lot tracking, and inventory balances.
- `Finance/`: Cash-on-delivery (COD) ledgers, fee calculations, and settlements.
- `Notifications/`: Notification templates, channels, and delivery logs.
- `Reporting/`: Aggregate read models, operational metrics, and reporting contracts.
- `AuditLogging/`: Audit records, system event traces, and compliance entries.
