# Infrastructure Modules

This folder hosts EF Core entity configurations (`IEntityTypeConfiguration<T>`), module-specific persistence mappings, and external service client implementations for each business module:

- `Identity/`: User/Role entity configurations, ASP.NET Core Identity integration.
- `Organization/`: Branch and Hub EF Core configurations.
- `Customers/`: Customer entity configurations, address mappings.
- `Orders/`: Order and OrderItem entity configurations with value object conversion.
- `Shipments/`: Shipment and Waybill configurations, tracking event tables.
- `Carriers/`: Carrier account persistence and configuration.
- `Warehouses/`: Warehouse, Zone, and Location configurations.
- `Inventory/`: Stock item configurations with concurrency tokens.
- `Finance/`: COD transaction ledgers and financial record configurations.
- `Notifications/`: Notification delivery logging and external provider adapters.
- `Reporting/`: Materialized views, read-optimized database query implementations.
- `AuditLogging/`: Audit log table configurations with high-throughput indexing.
