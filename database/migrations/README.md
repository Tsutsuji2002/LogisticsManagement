# Database Migrations

This directory documents EF Core migration workflows and stores idempotent SQL script bundles generated via EF Core CLI:

```bash
# Add a new migration
dotnet ef migrations add InitialCreate --project src/Server/Logistics.Server.Infrastructure --startup-project src/Server/Logistics.Server.API

# Update database
dotnet ef database update --project src/Server/Logistics.Server.Infrastructure --startup-project src/Server/Logistics.Server.API

# Generate idempotent SQL script for CI/CD
dotnet ef migrations script --idempotent --output database/migrations/bundle.sql --project src/Server/Logistics.Server.Infrastructure --startup-project src/Server/Logistics.Server.API
```
