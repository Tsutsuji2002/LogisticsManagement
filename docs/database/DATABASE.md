# Database Standards & Guidelines

## 1. Primary Engine

- **SQL Server 2022+** (or LocalDB for local developer environments)
- **Data Access**: Entity Framework Core 9.0 with Code-First Migrations

## 2. Table Naming & Column Conventions

- **Tables**: Pluralized PascalCase (e.g., `Customers`, `Shipments`, `Orders`).
- **Primary Keys**: `Id` of type `uniqueidentifier` (`Guid`), clustered or non-clustered with sequential GUID generation where appropriate.
- **Foreign Keys**: Named as `<EntityName>Id` (e.g., `CustomerId`, `WarehouseId`).
- **Audit Columns**: Every entity extending `BaseEntity` automatically includes:
  - `CreatedAt` (`datetime2`, UTC, required)
  - `CreatedBy` (`nvarchar(100)`, optional)
  - `UpdatedAt` (`datetime2`, UTC, optional)
  - `UpdatedBy` (`nvarchar(100)`, optional)

## 3. Modular Monolith Schema Segregation

In SQL Server, each module can utilize distinct schemas:
- `identity.Users`, `identity.Roles`
- `org.Branches`, `org.Hubs`
- `crm.Customers`
- `orders.Orders`, `orders.OrderItems`
- `shipments.Shipments`, `shipments.Waybills`
- `warehouses.Facilities`, `warehouses.Bins`
- `inventory.StockItems`
- `finance.CodTransactions`, `finance.Invoices`
- `audit.AuditLogs`

## 4. Concurrency & Performance

- Implement optimistic concurrency using rowversion/timestamp tokens (`xmin` or `byte[] RowVersion`).
- Ensure all queries spanning foreign keys or frequent lookup filters (such as `TrackingCode`, `OrderNumber`) have explicit non-clustered indices.
