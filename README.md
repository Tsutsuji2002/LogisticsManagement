# Logistics Management System

A robust, enterprise-grade Logistics Management System built with .NET 9, Windows Forms desktop primary client, ASP.NET Core Web API, and Clean Architecture modular monolith foundation.

---

## 1. Solution Architecture

The solution implements **Clean Architecture** with a **Modular Monolith** domain strategy:

```
LogisticsManagement/
├── LogisticsManagement.sln
│
├── src/
│   ├── Client/
│   │   ├── Logistics.Client.WinForms/        # WinForms desktop shell (MVP pattern)
│   │   ├── Logistics.Client.Application/     # Client use cases, view models & presenters
│   │   └── Logistics.Client.Infrastructure/  # HttpClientFactory API clients & auth handling
│   │
│   ├── Server/
│   │   ├── Logistics.Server.API/             # ASP.NET Core Web API & Swagger entry point
│   │   ├── Logistics.Server.Application/     # Application use cases, commands, queries
│   │   ├── Logistics.Server.Domain/          # Core entities, value objects, domain events
│   │   ├── Logistics.Server.Infrastructure/  # EF Core DbContext, SQL Server, Redis placeholder
│   │   ├── Logistics.Server.Worker/          # Background worker service for scheduled jobs
│   │   └── Logistics.Server.Integrations/    # External carrier integrations (GHN, GHTK, Viettel Post)
│   │
│   └── Shared/
│       └── Logistics.Shared.Contracts/       # Shared API response envelopes & pagination DTOs
│
├── tests/
│   ├── Logistics.Server.UnitTests/           # Domain & Application unit tests (xUnit)
│   ├── Logistics.Server.IntegrationTests/    # API integration tests (WebApplicationFactory)
│   └── Logistics.Client.UnitTests/           # Client presenter & view logic tests (Moq)
│
├── database/
│   ├── scripts/                              # SQL scripts and seed data
│   └── migrations/                           # EF Core migration documentation & bundles
│
├── docs/
│   ├── architecture/                         # Architecture guides and dependency diagrams
│   ├── database/                             # Database schemas and naming conventions
│   └── api/                                  # API contracts and guidelines
│
├── .editorconfig                             # Code style, formatting and naming conventions
├── .gitignore                                # Git ignore rules for .NET & Visual Studio
├── Directory.Build.props                     # Global build settings (C# 13, net9.0, nullable)
├── Directory.Packages.props                  # Central Package Management (CPM) versions
├── global.json                               # Locked .NET SDK version configuration
└── README.md                                 # Solution documentation & developer guide
```

---

## 2. Technology Stack & Prerequisites

- **.NET SDK**: 9.0.304 (or compatible .NET 9+ SDK)
- **Primary Client**: Windows Forms (.NET 9.0-windows) using Model-View-Presenter (MVP)
- **Backend API**: ASP.NET Core Web API with Swagger / OpenAPI
- **Data Access**: Entity Framework Core 9.0 (SQL Server)
- **Testing**: xUnit, FluentAssertions, Moq, Microsoft.AspNetCore.Mvc.Testing
- **Logging**: Serilog structured logging (Console + Rolling file)
- **Package Management**: Central Package Management (`Directory.Packages.props`)

---

## 3. How to Build and Run

### Step 1: Restore and Build

```bash
# From the solution root:
dotnet restore
dotnet build --configuration Release
```

### Step 2: Run Automated Tests

```bash
dotnet test
```

### Step 3: Run the Backend API

```bash
dotnet run --project src/Server/Logistics.Server.API
```
- Health Check: `GET http://localhost:5000/api/health`
- Swagger UI: `http://localhost:5000/swagger`

### Step 4: Run the WinForms Desktop Client

```bash
dotnet run --project src/Client/Logistics.Client.WinForms
```

---

## 4. Key Architectural Invariants

1. **Desktop Client Decoupling**: The WinForms desktop application connects exclusively through the HTTP REST API using `IApiClient`. It **never** connects directly to the SQL database.
2. **Independent Domain**: The Domain project contains no references to EF Core, ASP.NET Core, or third-party infrastructure frameworks.
3. **Modular Monolith Preparation**: Pre-configured structure supporting 12 future business modules:
   - Identity & Access Management
   - Organization & Branch Management
   - Customer Management (CRM)
   - Order Management
   - Shipment Management
   - Carrier Integration (GHN, GHTK, Viettel Post)
   - Warehouse Management
   - Inventory Management
   - Finance & COD Management
   - Notification Management
   - Reporting & Analytics
   - Audit Logging
