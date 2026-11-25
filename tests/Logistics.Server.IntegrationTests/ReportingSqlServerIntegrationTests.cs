using System.Text;
using FluentAssertions;
using Logistics.Server.Application.Common.Interfaces;
using Logistics.Server.Application.Common.Models;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Identity.Services;
using Logistics.Server.Application.Modules.Reporting.DTOs;
using Logistics.Server.Domain.Entities.Catalog;
using Logistics.Server.Domain.Entities.Configuration;
using Logistics.Server.Domain.Entities.Crm;
using Logistics.Server.Domain.Entities.Finance;
using Logistics.Server.Domain.Entities.Identity;
using Logistics.Server.Domain.Entities.Inventory;
using Logistics.Server.Domain.Entities.Organization;
using Logistics.Server.Domain.Entities.Sales;
using Logistics.Server.Domain.Entities.Shipping;
using Logistics.Server.Domain.Entities.Warehouse;
using Logistics.Server.Infrastructure.Persistence;
using Logistics.Server.Infrastructure.Services;
using Logistics.Server.Infrastructure.Services.Reporting;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace Logistics.Server.IntegrationTests;

/// <summary>
/// Real SQL Server LocalDB integration tests validating Reporting LINQ-to-SQL translation,
/// computed columns (InventoryBalance.QuantityAvailable, Invoice.OutstandingAmount),
/// timezone half-open windows, branch isolation, COD reconciliation, and CSV execution engine.
/// </summary>
public class ReportingSqlServerIntegrationTests : IAsyncLifetime
{
    private readonly DbContextOptions<ApplicationDbContext> _dbOptions;

    public ReportingSqlServerIntegrationTests()
    {
        var dbName = $"LogisticsReportingSqlTest_{Guid.NewGuid():N}";
        var cs = $@"Server=(localdb)\mssqllocaldb;Database={dbName};Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";
        _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(cs)
            .Options;
    }

    public async Task InitializeAsync()
    {
        await using var db = new ApplicationDbContext(_dbOptions);
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await using var db = new ApplicationDbContext(_dbOptions);
        await db.Database.EnsureDeletedAsync();
    }

    private sealed class StubCurrentUserService : ICurrentUserService
    {
        public long? UserId { get; init; }
        public string? Username => "sql-tester";
        public string? Email => "sql@test.vn";
        public long? EmployeeId => null;
        public long? DefaultBranchId { get; init; }
        public IReadOnlyList<long> AccessibleBranchIds { get; init; } = [];
        public IReadOnlyList<string> Roles { get; init; } = [];
        public bool IsAuthenticated => true;
        public bool IsInRole(string role) => Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
        public bool HasBranchAccess(long branchId) => IsInRole(AppRoles.SuperAdmin) || AccessibleBranchIds.Contains(branchId);
    }

    private sealed class StubPermissionService(bool allowAll) : IPermissionService
    {
        public Task<IReadOnlySet<string>> GetUserPermissionsAsync(long userId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlySet<string>>(new HashSet<string>());

        public Task<bool> HasPermissionAsync(long userId, string permissionCode, CancellationToken cancellationToken = default)
            => Task.FromResult(allowAll);

        public Task<IReadOnlyList<long>> GetUserBranchIdsAsync(long userId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<long>>([]);
    }

    private sealed class StubCacheService : ICacheService
    {
        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) => Task.FromResult<T?>(default);
        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RemoveAsync(string key, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    [Fact]
    public async Task SqlServer_ReportingQueries_ExecuteCleanly_WithComputedColumns_TimeZones_AndBranchScope()
    {
        await using var db = new ApplicationDbContext(_dbOptions);

        var company = new Company { Code = "SQLCO", LegalName = "SQL Corp", TradingName = "SQL", TaxNumber = "010888", IsActive = true };
        db.Companies.Add(company);
        await db.SaveChangesAsync();

        var bSgn = new Branch { CompanyId = company.Id, Code = "SGN", Name = "Saigon", BranchType = "Hub", Phone = "111", IsActive = true };
        var bHan = new Branch { CompanyId = company.Id, Code = "HAN", Name = "Hanoi", BranchType = "Hub", Phone = "222", IsActive = true };
        db.Branches.AddRange(bSgn, bHan);
        await db.SaveChangesAsync();

        var currency = new Currency { Code = "VND", Name = "Dong", Symbol = "₫", DecimalPlaces = 0, ExchangeRateToDefault = 1m, IsDefault = true, IsActive = true };
        var uom = new UnitOfMeasure { Code = "PCS", Name = "Piece", Type = "Count", ConversionFactor = 1m, IsBaseUnit = true, IsActive = true };
        var category = new ProductCategory { Code = "CAT", Name = "Cat", IsActive = true };
        db.Currencies.Add(currency);
        db.UnitsOfMeasure.Add(uom);
        db.ProductCategories.Add(category);
        await db.SaveChangesAsync();

        var product = new Product { CategoryId = category.Id, UnitOfMeasureId = uom.Id, CurrencyId = currency.Id, Sku = "SQL-SKU-1", Name = "Widget", DeclaredValue = 100m, IsActive = true };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var emp = new Employee { BranchId = bSgn.Id, Code = "EMP-1", FirstName = "An", LastName = "Nguyen", FullName = "Nguyen An", Email = "an@t.vn", PhoneNumber = "1", JobTitle = "Driver", HireDate = new DateOnly(2025, 1, 1), Status = "Active" };
        db.Employees.Add(emp);
        await db.SaveChangesAsync();

        var custSgn = new Customer { BranchId = bSgn.Id, CustomerCode = "C1", Name = "SGN Cust", Email = "c1@t.vn", PhoneNumber = "1", CreditLimit = 1000m, IsActive = true };
        var custHan = new Customer { BranchId = bHan.Id, CustomerCode = "C2", Name = "HAN Cust", Email = "c2@t.vn", PhoneNumber = "2", CreditLimit = 1000m, IsActive = true };
        db.Customers.AddRange(custSgn, custHan);
        await db.SaveChangesAsync();

        var whSgn = new Warehouse { BranchId = bSgn.Id, Code = "WH-1", Name = "SGN WH", Type = "InventoryWarehouse", IsActive = true };
        db.Warehouses.Add(whSgn);
        await db.SaveChangesAsync();

        var zone = new WarehouseZone { WarehouseId = whSgn.Id, Code = "Z1", Name = "Z1", ZoneType = "BulkStorage", IsActive = true };
        db.WarehouseZones.Add(zone);
        await db.SaveChangesAsync();

        var loc = new WarehouseLocation { WarehouseZoneId = zone.Id, Code = "L1", Barcode = "B1" };
        db.WarehouseLocations.Add(loc);
        await db.SaveChangesAsync();

        db.InventoryItems.Add(new InventoryItem { ProductId = product.Id, WarehouseId = whSgn.Id, ReorderPoint = 8, IsActive = true });
        db.InventoryBalances.Add(new InventoryBalance { WarehouseId = whSgn.Id, WarehouseLocationId = loc.Id, ProductId = product.Id, QuantityOnHand = 12, QuantityAllocated = 5, QuantityDamaged = 1 });

        // Order inside 2026-04-02 in UTC+7 (2026-04-01T18:00:00Z = 2026-04-02 01:00+07)
        var ordSgn = new Order { OrderNumber = "ORD-SQL-1", CustomerId = custSgn.Id, BranchId = bSgn.Id, CurrencyId = currency.Id, OrderDate = new DateTime(2026, 4, 1, 18, 0, 0, DateTimeKind.Utc), OrderStatus = "Shipped", PaymentStatus = "Invoiced", DeclaredValue = 500m, CodAmount = 200m };
        var ordHan = new Order { OrderNumber = "ORD-SQL-2", CustomerId = custHan.Id, BranchId = bHan.Id, CurrencyId = currency.Id, OrderDate = new DateTime(2026, 4, 1, 18, 0, 0, DateTimeKind.Utc), OrderStatus = "Confirmed", PaymentStatus = "Pending", DeclaredValue = 100m, CodAmount = 50m };
        db.Orders.AddRange(ordSgn, ordHan);
        await db.SaveChangesAsync();

        var shp = new Shipment
        {
            TrackingNumber = "TRK-SQL-1",
            OrderId = ordSgn.Id,
            CustomerId = custSgn.Id,
            OriginBranchId = bSgn.Id,
            DestinationBranchId = bSgn.Id,
            CurrencyId = currency.Id,
            ShipmentStatus = "Delivered",
            ServiceType = "Standard",
            ChargeableWeightKg = 1m,
            CodAmount = 200m,
            TotalAmount = 30m,
            CreatedAt = new DateTime(2026, 4, 1, 18, 30, 0, DateTimeKind.Utc),
            PickupDate = new DateTime(2026, 4, 1, 19, 0, 0, DateTimeKind.Utc),
            EstimatedDeliveryDate = new DateTime(2026, 4, 2, 10, 0, 0, DateTimeKind.Utc),
            ActualDeliveryDate = new DateTime(2026, 4, 2, 8, 0, 0, DateTimeKind.Utc)
        };
        db.Shipments.Add(shp);
        await db.SaveChangesAsync();
        // ApplicationDbContext stamps CreatedAt = UtcNow on Add; backdate explicitly to validate SQL Server timezone windowing
        await db.Shipments
            .Where(s => s.Id == shp.Id)
            .ExecuteUpdateAsync(u => u.SetProperty(x => x.CreatedAt, new DateTime(2026, 4, 1, 18, 30, 0, DateTimeKind.Utc)));

        db.Invoices.Add(new Invoice
        {
            InvoiceNumber = "INV-SQL-1",
            CustomerId = custSgn.Id,
            InvoiceDate = new DateOnly(2026, 4, 2),
            DueDate = new DateOnly(2026, 5, 2),
            SubTotal = 100m,
            TaxAmount = 0m,
            TotalAmount = 100m,
            PaidAmount = 35m,
            Status = "PartiallyPaid"
        });
        db.CustomerBalances.Add(new CustomerBalance { CustomerId = custSgn.Id, CurrentOutstandingBalance = 65m, UnbilledChargesAmount = 0m, TotalCodHoldingAmount = 150m });
        db.CODCollections.Add(new CODCollection { ShipmentId = shp.Id, CollectedAmount = 150m, CurrencyId = currency.Id, CollectionDate = new DateTime(2026, 4, 2, 8, 0, 0, DateTimeKind.Utc), CollectedByEmployeeId = emp.Id, Status = "VerifiedByCashier" });
        await db.SaveChangesAsync();

        var adminUser = new StubCurrentUserService { UserId = 1, Roles = [AppRoles.SuperAdmin], AccessibleBranchIds = [bSgn.Id, bHan.Id] };
        var dashSvc = new DashboardQueryService(db, adminUser, new StubPermissionService(true), new StubCacheService());
        var opsSvc = new OperationalReportService(db, adminUser);
        var finSvc = new FinancialReportService(db, adminUser);

        var filter = new ReportFilterRequest
        {
            StartDate = new DateOnly(2026, 4, 2),
            EndDate = new DateOnly(2026, 4, 2),
            TimeZoneId = "SE Asia Standard Time"
        };

        var overview = await dashSvc.GetCompanyOverviewAsync(filter);
        overview.TotalOrders.Should().Be(2); // Both UTC 18:00 orders fall inside 2026-04-02 UTC+7!
        overview.InventorySummary.TotalQuantityAvailable.Should().Be(7);

        var invBalances = await opsSvc.GetInventoryBalancesReportAsync(filter, onlyLowStock: true, new PagedRequest { Page = 1, PageSize = 10 });
        invBalances.Items.Should().HaveCount(1);
        invBalances.Items[0].QuantityAvailable.Should().Be(7);

        var invoices = await finSvc.GetInvoicesReportAsync(filter, new PagedRequest { Page = 1, PageSize = 10 });
        invoices.Items.Should().HaveCount(1);
        invoices.Items[0].OutstandingAmount.Should().Be(65m);

        var codRecon = await finSvc.GetCodReconciliationReportAsync(filter, new PagedRequest { Page = 1, PageSize = 10 });
        codRecon.Items.First(x => x.BranchId == bSgn.Id).DiscrepancyAmount.Should().Be(50m);

        // Restricted branch user excludes HAN on SQL Server!
        var sgnUser = new StubCurrentUserService { UserId = 2, Roles = [AppRoles.BranchManager], AccessibleBranchIds = [bSgn.Id] };
        var sgnOpsSvc = new OperationalReportService(db, sgnUser);
        var sgnOrders = await sgnOpsSvc.GetOrderReportAsync(filter, new PagedRequest { Page = 1, PageSize = 10 });
        sgnOrders.Items.Should().ContainSingle(o => o.OrderNumber == "ORD-SQL-1");

        // Execution engine CSV works on SQL Server
        var engine = new ReportingExecutionEngine(db);
        var csv = await engine.GenerateAsync("RPT_FINANCE_INVOICES", "SELECT 1", "Csv", bSgn.Id);
        csv.Succeeded.Should().BeTrue();
        Encoding.UTF8.GetString(csv.FileBytes!.Skip(3).ToArray()).Should().Contain("INV-SQL-1");
    }
}
