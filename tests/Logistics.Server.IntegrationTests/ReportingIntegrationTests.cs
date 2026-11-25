using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.BackgroundProcessing.DTOs;
using Logistics.Server.Application.Modules.Identity.DTOs;
using Logistics.Server.Application.Modules.Reporting.DTOs;
using Logistics.Server.Domain.Entities.Carrier;
using Logistics.Server.Domain.Entities.Catalog;
using Logistics.Server.Domain.Entities.Configuration;
using Logistics.Server.Domain.Entities.Crm;
using Logistics.Server.Domain.Entities.Finance;
using Logistics.Server.Domain.Entities.Identity;
using Logistics.Server.Domain.Entities.Inventory;
using Logistics.Server.Domain.Entities.Notification;
using Logistics.Server.Domain.Entities.Organization;
using Logistics.Server.Domain.Entities.Sales;
using Logistics.Server.Domain.Entities.Shipping;
using Logistics.Server.Domain.Entities.Warehouse;
using Logistics.Server.Infrastructure.Persistence;
using Logistics.Shared.Contracts;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Logistics.Server.IntegrationTests;

public class ReportingIntegrationTests : IClassFixture<WebApplicationFactory<Logistics.Server.API.Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Logistics.Server.API.Program> _factory;
    private readonly string _dbName;

    public ReportingIntegrationTests(WebApplicationFactory<Logistics.Server.API.Program> factory)
    {
        _dbName = "ReportingTestDb_" + Guid.NewGuid().ToString("N");

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptors = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    d.ServiceType.Name.Contains("DbContextOptions") ||
                    d.ServiceType == typeof(ApplicationDbContext)).ToList();

                foreach (var d in descriptors) services.Remove(d);

                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase(_dbName));
            });
        });

        _client = _factory.CreateClient();
    }

    private sealed record SeedIds(long SgnBranchId, long HanBranchId, long SgnWhId, long HanHubId, long SgnCustomerId, long HanCustomerId, long SgnEmployeeId, long DefId);

    private async Task<SeedIds> SeedScenarioAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var seeder = scope.ServiceProvider.GetRequiredService<Logistics.Server.Application.Modules.Identity.Services.IIdentityDataSeeder>();
        await seeder.SeedAsync();

        if (!await context.Companies.AnyAsync())
        {
            var now = DateTime.UtcNow;
            var company = new Company { Code = "LOGIRPT", LegalName = "Logi Report Corp", TradingName = "LogiRpt", TaxNumber = "0102030999", IsActive = true };
            context.Companies.Add(company);
            await context.SaveChangesAsync();

            var bSgn = new Branch { CompanyId = company.Id, Code = "SGN", Name = "Saigon Branch", BranchType = "Hub", Phone = "028111", IsActive = true };
            var bHan = new Branch { CompanyId = company.Id, Code = "HAN", Name = "Hanoi Branch", BranchType = "Hub", Phone = "024222", IsActive = true };
            context.Branches.AddRange(bSgn, bHan);
            await context.SaveChangesAsync();

            var currency = new Currency { Code = "VND", Name = "Dong", Symbol = "₫", DecimalPlaces = 0, ExchangeRateToDefault = 1m, IsDefault = true, IsActive = true };
            var uom = new UnitOfMeasure { Code = "PCS", Name = "Piece", Type = "Count", ConversionFactor = 1m, IsBaseUnit = true, IsActive = true };
            var category = new ProductCategory { Code = "CAT", Name = "General", IsActive = true };
            context.Currencies.Add(currency);
            context.UnitsOfMeasure.Add(uom);
            context.ProductCategories.Add(category);
            await context.SaveChangesAsync();

            var product = new Product { CategoryId = category.Id, UnitOfMeasureId = uom.Id, CurrencyId = currency.Id, Sku = "SKU-RPT-1", Name = "Gadget Box", DeclaredValue = 100000m, IsActive = true };
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var empSgn = new Employee { BranchId = bSgn.Id, Code = "EMP-SGN", FirstName = "Minh", LastName = "Le", FullName = "Le Minh", Email = "minh@t.vn", PhoneNumber = "0901", JobTitle = "Driver", HireDate = DateOnly.FromDateTime(now.AddYears(-1)), Status = "Active" };
            var empHan = new Employee { BranchId = bHan.Id, Code = "EMP-HAN", FirstName = "Lan", LastName = "Pham", FullName = "Pham Lan", Email = "lan@t.vn", PhoneNumber = "0902", JobTitle = "Sorter", HireDate = DateOnly.FromDateTime(now.AddYears(-1)), Status = "Active" };
            context.Employees.AddRange(empSgn, empHan);
            await context.SaveChangesAsync();

            var systemUser = new UserAccount { Username = "rpt_cashier", Email = "cashier@t.vn", PasswordHash = "hash", SecurityStamp = "s1", IsActive = true };
            context.UserAccounts.Add(systemUser);
            await context.SaveChangesAsync();

            // Formula-injection test in Customer.Name is intentional for CSV export safety test!
            var custSgn = new Customer { BranchId = bSgn.Id, CustomerCode = "C-SGN", CustomerType = "Corporate", Name = "=EVIL() Merchant SGN", Email = "cs@t.vn", PhoneNumber = "0903", CreditLimit = 5000000m, IsActive = true };
            var custHan = new Customer { BranchId = bHan.Id, CustomerCode = "C-HAN", CustomerType = "Corporate", Name = "Merchant HAN", Email = "ch@t.vn", PhoneNumber = "0904", CreditLimit = 2000000m, IsActive = true };
            context.Customers.AddRange(custSgn, custHan);
            await context.SaveChangesAsync();

            var whSgn = new Warehouse { BranchId = bSgn.Id, Code = "WH-SGN", Name = "SGN Inventory WH", Type = "InventoryWarehouse", TotalAreaSqM = 1000m, IsActive = true };
            var hubHan = new Warehouse { BranchId = bHan.Id, Code = "HUB-HAN", Name = "HAN Sorting Hub", Type = "ParcelHub", TotalAreaSqM = 800m, IsActive = true };
            context.Warehouses.AddRange(whSgn, hubHan);
            await context.SaveChangesAsync();

            var zoneSgn = new WarehouseZone { WarehouseId = whSgn.Id, Code = "Z1", Name = "Storage", ZoneType = "BulkStorage", IsActive = true };
            context.WarehouseZones.Add(zoneSgn);
            await context.SaveChangesAsync();

            var locSgn = new WarehouseLocation { WarehouseZoneId = zoneSgn.Id, Code = "A-01", Barcode = "LOC-A01" };
            context.WarehouseLocations.Add(locSgn);
            await context.SaveChangesAsync();

            // Inventory: OnHand=10, Allocated=6 => Available=4 <= ReorderPoint(5) -> LowStock!
            context.InventoryItems.Add(new InventoryItem { ProductId = product.Id, WarehouseId = whSgn.Id, ReorderPoint = 5, SafetyStock = 2, IsActive = true });
            context.InventoryBalances.Add(new InventoryBalance { WarehouseId = whSgn.Id, WarehouseLocationId = locSgn.Id, ProductId = product.Id, QuantityOnHand = 10, QuantityAllocated = 6, QuantityDamaged = 1 });

            var carrier = new Carrier { Code = "GHN", Name = "Giao Hang Nhanh", ApiBaseUrl = "https://ghn.test", IsActive = true };
            context.Carriers.Add(carrier);
            await context.SaveChangesAsync();

            var svc = new CarrierService { CarrierId = carrier.Id, ServiceCode = "STD", ServiceName = "Standard", IsActive = true };
            context.CarrierServices.Add(svc);
            await context.SaveChangesAsync();

            // 1 Order SGN -> split into 2 Shipments; plus 1 HAN Order
            var ordSgn = new Order
            {
                OrderNumber = "ORD-RPT-SGN",
                CustomerId = custSgn.Id,
                BranchId = bSgn.Id,
                CurrencyId = currency.Id,
                OrderDate = now.AddHours(-12),
                OrderStatus = "PartiallyShipped",
                PaymentStatus = "Pending",
                TotalWeightKg = 5m,
                DeclaredValue = 1000000m,
                CodAmount = 300000m
            };
            ordSgn.OrderItems.Add(new OrderItem { ProductId = product.Id, ProductName = product.Name, Sku = product.Sku, Quantity = 2, UnitOfMeasureId = uom.Id, UnitPrice = 100000m, TotalPrice = 200000m, DeclaredValue = 200000m, WeightKg = 2m });
            ordSgn.OrderPackages.Add(new OrderPackage { PackageCode = "OP-1", WeightKg = 2m, LengthCm = 10m, WidthCm = 10m, HeightCm = 10m, VolumetricWeightKg = 0.2m, DeclaredValue = 200000m });
            ordSgn.OrderPackages.Add(new OrderPackage { PackageCode = "OP-2", WeightKg = 3m, LengthCm = 10m, WidthCm = 10m, HeightCm = 10m, VolumetricWeightKg = 0.2m, DeclaredValue = 300000m });

            var ordHan = new Order
            {
                OrderNumber = "ORD-RPT-HAN",
                CustomerId = custHan.Id,
                BranchId = bHan.Id,
                CurrencyId = currency.Id,
                OrderDate = now.AddHours(-10),
                OrderStatus = "Confirmed",
                PaymentStatus = "Pending",
                TotalWeightKg = 2m,
                DeclaredValue = 500000m,
                CodAmount = 0m
            };
            context.Orders.AddRange(ordSgn, ordHan);
            await context.SaveChangesAsync();

            // Shipments:
            // S1: SGN->SGN Delivered ON TIME, ExpectedCOD=200k
            // S2: SGN->SGN Delivered LATE, ExpectedCOD=100k
            // S3: HAN->HAN InTransit (excluded from terminal delivery success denominator!)
            // S4: SGN->SGN Returned WITHOUT SLA, ExpectedCOD=0
            var s1 = new Shipment
            {
                TrackingNumber = "TRK-SGN-1",
                OrderId = ordSgn.Id,
                CustomerId = custSgn.Id,
                OriginBranchId = bSgn.Id,
                DestinationBranchId = bSgn.Id,
                CurrencyId = currency.Id,
                AssignedCarrierId = carrier.Id,
                CarrierServiceId = svc.Id,
                ShipmentStatus = "Delivered",
                ServiceType = "Standard",
                ChargeableWeightKg = 2m,
                CodAmount = 200000m,
                TotalAmount = 30000m,
                CreatedAt = now.AddHours(-10),
                PickupDate = now.AddHours(-9),
                EstimatedDeliveryDate = now.AddHours(-2),
                ActualDeliveryDate = now.AddHours(-4) // On time
            };
            var s2 = new Shipment
            {
                TrackingNumber = "TRK-SGN-2",
                OrderId = ordSgn.Id,
                CustomerId = custSgn.Id,
                OriginBranchId = bSgn.Id,
                DestinationBranchId = bSgn.Id,
                CurrencyId = currency.Id,
                AssignedCarrierId = carrier.Id,
                CarrierServiceId = svc.Id,
                ShipmentStatus = "Delivered",
                ServiceType = "Express",
                ChargeableWeightKg = 3m,
                CodAmount = 100000m,
                TotalAmount = 50000m,
                CreatedAt = now.AddHours(-10),
                PickupDate = now.AddHours(-9),
                EstimatedDeliveryDate = now.AddHours(-6),
                ActualDeliveryDate = now.AddHours(-3) // Late
            };
            var s3 = new Shipment
            {
                TrackingNumber = "TRK-HAN-3",
                OrderId = ordHan.Id,
                CustomerId = custHan.Id,
                OriginBranchId = bHan.Id,
                DestinationBranchId = bHan.Id,
                CurrencyId = currency.Id,
                ShipmentStatus = "InTransit",
                ServiceType = "Standard",
                ChargeableWeightKg = 2m,
                CodAmount = 50000m,
                TotalAmount = 25000m,
                CreatedAt = now.AddHours(-8)
            };
            var s4 = new Shipment
            {
                TrackingNumber = "TRK-SGN-4",
                CustomerId = custSgn.Id,
                OriginBranchId = bSgn.Id,
                DestinationBranchId = bSgn.Id,
                CurrencyId = currency.Id,
                ShipmentStatus = "Returned",
                ServiceType = "Standard",
                ChargeableWeightKg = 1m,
                CodAmount = 0m,
                TotalAmount = 15000m,
                CreatedAt = now.AddHours(-12),
                EstimatedDeliveryDate = null // No SLA
            };
            context.Shipments.AddRange(s1, s2, s3, s4);
            await context.SaveChangesAsync();

            var pkg1 = new ShipmentPackage { ShipmentId = s1.Id, PackageCode = "SP-1", Barcode = "PKG-001", WeightKg = 2m };
            context.ShipmentPackages.Add(pkg1);
            await context.SaveChangesAsync();

            // Delivery Attempts: s1 had 1 Failed + 1 Delivered
            context.DeliveryAttempts.AddRange(
                new DeliveryAttempt { ShipmentId = s1.Id, AttemptNumber = 1, AttemptedByEmployeeId = empSgn.Id, AttemptedAt = now.AddHours(-5), Status = "Failed", FailureReasonCode = "NO_ANSWER" },
                new DeliveryAttempt { ShipmentId = s1.Id, AttemptNumber = 2, AttemptedByEmployeeId = empSgn.Id, AttemptedAt = now.AddHours(-4), Status = "Delivered" },
                new DeliveryAttempt { ShipmentId = s2.Id, AttemptNumber = 1, AttemptedByEmployeeId = empSgn.Id, AttemptedAt = now.AddHours(-3), Status = "Delivered" });

            // Warehouse & Hub & Inventory Transactions
            var rec = new WarehouseReceipt { ReceiptNumber = "REC-1", WarehouseId = whSgn.Id, ReceiptType = "InventoryInbound", Status = "Completed", ActualArrivalDate = now.AddHours(-5), ReceivedByEmployeeId = empSgn.Id };
            rec.ReceiptItems.Add(new WarehouseReceiptItem { ProductId = product.Id, ExpectedQuantity = 10, ReceivedQuantity = 10, DamagedQuantity = 1, UnitOfMeasureId = uom.Id, TargetLocationId = locSgn.Id });
            var disp = new WarehouseDispatch { DispatchNumber = "DSP-1", WarehouseId = hubHan.Id, DispatchType = "HubTransfer", Status = "Dispatched", DispatchedByEmployeeId = empHan.Id, DispatchedAt = now.AddHours(-2) };
            context.WarehouseReceipts.Add(rec);
            context.WarehouseDispatches.Add(disp);
            context.ParcelHandlingEvents.Add(new ParcelHandlingEvent { ShipmentId = s1.Id, ShipmentPackageId = pkg1.Id, WarehouseId = whSgn.Id, EventType = "Unloaded", EmployeeId = empSgn.Id, ScannedAt = now.AddHours(-6) });

            var invTx = new InventoryTransaction { TransactionNumber = "INVTX-1", WarehouseId = whSgn.Id, TransactionType = "Receipt", ReferenceType = "WarehouseReceipt", ReferenceId = 1, ReferenceNumber = "REC-1", PerformedByEmployeeId = empSgn.Id, TransactionDate = now.AddHours(-5) };
            invTx.TransactionItems.Add(new InventoryTransactionItem { ProductId = product.Id, LocationId = locSgn.Id, Quantity = 10, UnitCost = 100m, TotalCost = 1000m, BalanceBefore = 0, BalanceAfter = 10 });
            context.InventoryTransactions.Add(invTx);

            // Carrier Telemetry
            context.CarrierApiLogs.Add(new CarrierApiLog { CarrierId = carrier.Id, Endpoint = "/order", HttpMethod = "POST", ResponseStatusCode = 200, Timestamp = now.AddHours(-10), IsSuccess = true });
            context.CarrierWebhookLogs.Add(new CarrierWebhookLog { CarrierId = carrier.Id, WebhookUrl = "/wh", PayloadJson = "{}", ResponseStatusCode = 200, ClientIpAddress = "127.0.0.1", ProcessedAt = now.AddHours(-4), ProcessedSuccessfully = true });
            context.CarrierSyncJobs.Add(new CarrierSyncJob { CarrierId = carrier.Id, JobType = "TrackingPoll", StartTime = now.AddHours(-5), Status = "Completed" });
            context.CarrierTrackingEvents.Add(new CarrierTrackingEvent { ProcessedShipmentId = s1.Id, CarrierId = carrier.Id, CarrierTrackingNumber = "EXT-1", ExternalEventId = "EV-1", EventCode = "delivered", RawPayloadJson = "{}", EventTimestamp = now.AddHours(-4), ProcessedStatus = "Processed" });

            // Finance: Draft Invoice (excl from Issued) + Issued Invoice + Partial Payment with Unallocated Credit + Charges
            var invDraft = new Invoice { InvoiceNumber = "INV-DRAFT", CustomerId = custSgn.Id, InvoiceDate = DateOnly.FromDateTime(now), DueDate = DateOnly.FromDateTime(now.AddDays(30)), SubTotal = 10000m, TaxAmount = 0m, TotalAmount = 10000m, PaidAmount = 0m, Status = "Draft" };
            var invIssued = new Invoice { InvoiceNumber = "INV-ISS-1", CustomerId = custSgn.Id, InvoiceDate = DateOnly.FromDateTime(now), DueDate = DateOnly.FromDateTime(now.AddDays(30)), SubTotal = 30000m, TaxAmount = 0m, TotalAmount = 30000m, PaidAmount = 20000m, Status = "PartiallyPaid" };
            context.Invoices.AddRange(invDraft, invIssued);
            await context.SaveChangesAsync();

            context.ShipmentCharges.AddRange(
                new ShipmentCharge { ShipmentId = s1.Id, ChargeType = "BaseFreight", Amount = 30000m, CurrencyId = currency.Id, IsInvoiced = true, InvoiceId = invIssued.Id, CreatedAt = now.AddHours(-8) },
                new ShipmentCharge { ShipmentId = s2.Id, ChargeType = "BaseFreight", Amount = 50000m, CurrencyId = currency.Id, IsInvoiced = false, CreatedAt = now.AddHours(-8) });

            var pay = new Payment { PaymentNumber = "PAY-1", CustomerId = custSgn.Id, PaymentDate = now.AddHours(-2), PaymentMethod = "BankTransfer", Amount = 25000m, CurrencyId = currency.Id, Status = "Confirmed", ConfirmedByUserId = systemUser.Id, ConfirmedAt = now.AddHours(-2) };
            pay.Allocations.Add(new PaymentAllocation { InvoiceId = invIssued.Id, AllocatedAmount = 20000m, AllocatedAt = now.AddHours(-2) }); // 5,000 unallocated!
            context.Payments.Add(pay);

            context.CustomerBalances.AddRange(
                new CustomerBalance { CustomerId = custSgn.Id, CurrentOutstandingBalance = 10000m, UnbilledChargesAmount = 50000m, TotalCodHoldingAmount = 100000m, LastUpdatedDate = now },
                new CustomerBalance { CustomerId = custHan.Id, CurrentOutstandingBalance = 0m, UnbilledChargesAmount = 25000m, TotalCodHoldingAmount = 0m, LastUpdatedDate = now });

            // COD: S1 collected 200k Verified; S2 collected 80k CollectedByDriver (variance 20k vs 100k!)
            var cod1 = new CODCollection { ShipmentId = s1.Id, CollectedAmount = 200000m, CurrencyId = currency.Id, CollectionDate = now.AddHours(-4), CollectedByEmployeeId = empSgn.Id, Status = "VerifiedByCashier", VerifiedAt = now.AddHours(-3) };
            var cod2 = new CODCollection { ShipmentId = s2.Id, CollectedAmount = 80000m, CurrencyId = currency.Id, CollectionDate = now.AddHours(-3), CollectedByEmployeeId = empSgn.Id, Status = "CollectedByDriver" };
            context.CODCollections.AddRange(cod1, cod2);
            await context.SaveChangesAsync();

            var rem = new CODRemittance
            {
                RemittanceNumber = "REM-1",
                CustomerId = custSgn.Id,
                RemittanceDate = now.AddHours(-1),
                TotalCodAmount = 200000m,
                TotalServiceFeesDeducted = 5000m,
                NetRemittanceAmount = 195000m,
                CurrencyId = currency.Id,
                Status = "Completed",
                BankName = "VCB",
                BankAccount = "123",
                AccountHolderName = "Merchant",
                RemittedByUserId = systemUser.Id,
                RemittedAt = now.AddHours(-1)
            };
            rem.RemittanceItems.Add(new CODRemittanceItem { CODCollectionId = cod1.Id, ShipmentId = s1.Id, CodAmount = 200000m, DeductedShippingFee = 5000m, NetAmount = 195000m });
            context.CODRemittances.Add(rem);

            var rptDef = new ReportDefinition
            {
                Code = "RPT_ORDERS",
                Name = "Orders Summary CSV",
                Category = "Sales",
                QueryDefinition = "SELECT * FROM sales.Orders",
                IsActive = true
            };
            context.ReportDefinitions.Add(rptDef);
            await context.SaveChangesAsync();
        }

        var branches = await context.Branches.OrderBy(b => b.Id).ToListAsync();
        var sgnWh = await context.Warehouses.FirstAsync(w => w.Code == "WH-SGN");
        var hanHub = await context.Warehouses.FirstAsync(w => w.Code == "HUB-HAN");
        var cSgn = await context.Customers.FirstAsync(c => c.CustomerCode == "C-SGN");
        var cHan = await context.Customers.FirstAsync(c => c.CustomerCode == "C-HAN");
        var eSgn = await context.Employees.FirstAsync(e => e.Code == "EMP-SGN");
        var def = await context.ReportDefinitions.FirstAsync();
        return new SeedIds(branches[0].Id, branches[1].Id, sgnWh.Id, hanHub.Id, cSgn.Id, cHan.Id, eSgn.Id, def.Id);
    }

    private async Task<string> GetTokenAsync(string username, string roleCode, long? restrictBranchId = null)
    {
        await SeedScenarioAsync();

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<Logistics.Server.Application.Common.Interfaces.IPasswordHasherService>();

        var password = "StrongPassword2026!";
        var user = await context.UserAccounts.Include(u => u.UserRoles).Include(u => u.BranchAccesses)
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
        {
            var role = await context.Roles.FirstAsync(r => r.Code == roleCode);
            user = new UserAccount
            {
                Username = username,
                Email = $"{username}@test.vn",
                PasswordHash = hasher.HashPassword(password),
                SecurityStamp = Guid.NewGuid().ToString("N"),
                IsActive = true
            };
            user.UserRoles.Add(new UserRole { RoleId = role.Id, AssignedBy = "TestSetup", AssignedAt = DateTime.UtcNow });

            if (restrictBranchId.HasValue)
            {
                user.BranchAccesses.Add(new UserBranchAccess { BranchId = restrictBranchId.Value, IsDefault = true, GrantedBy = "TestSetup", GrantedAt = DateTime.UtcNow });
            }
            else
            {
                foreach (var b in await context.Branches.ToListAsync())
                {
                    user.BranchAccesses.Add(new UserBranchAccess { BranchId = b.Id, IsDefault = b.Code == "SGN", GrantedBy = "TestSetup", GrantedAt = DateTime.UtcNow });
                }
            }

            context.UserAccounts.Add(user);
            await context.SaveChangesAsync();
        }

        var loginResp = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(username, password));
        loginResp.EnsureSuccessStatusCode();
        var auth = await loginResp.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        return auth!.Data!.AccessToken;
    }

    [Fact]
    public async Task Dashboards_VerifyAccurateMetricDistinctions_AcrossAllTenEndpoints()
    {
        var ids = await SeedScenarioAsync();
        var adminToken = await GetTokenAsync("rpt_superadmin", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var overview = (await _client.GetFromJsonAsync<ApiResponse<CompanyOverviewDashboardDto>>("/api/v1/dashboard/overview"))!.Data!;
        overview.TotalActiveBranches.Should().Be(2);
        overview.TotalOrders.Should().Be(2);
        overview.TotalShipments.Should().Be(4); // Orders != Shipments!
        overview.DeliveredShipments.Should().Be(2);
        overview.ReturnedShipments.Should().Be(1);
        overview.ShipmentsCurrentlyInTransit.Should().Be(1);
        overview.InventorySummary.TotalQuantityOnHand.Should().Be(10);
        overview.InventorySummary.TotalQuantityAllocated.Should().Be(6);
        overview.InventorySummary.TotalQuantityAvailable.Should().Be(4);
        overview.FinancialSummary.TotalIssuedInvoiceAmount.Should().Be(30000m); // Draft 10k excluded!
        overview.FinancialSummary.TotalPaymentsConfirmed.Should().Be(25000m);
        overview.FinancialSummary.TotalPaymentsAllocated.Should().Be(20000m);
        overview.CodSummary.TotalExpectedCodOnShipments.Should().Be(350000m); // S1(200k)+S2(100k)+S3(50k)
        overview.CodSummary.TotalCollectedByDrivers.Should().Be(280000m); // 200k+80k
        overview.CodSummary.OutstandingDriverCashInHand.Should().Be(80000m);

        var sgnBranchDash = (await _client.GetFromJsonAsync<ApiResponse<BranchDashboardDto>>($"/api/v1/dashboard/branches/{ids.SgnBranchId}"))!.Data!;
        sgnBranchDash.BranchCode.Should().Be("SGN");
        sgnBranchDash.FinancialSummary.Should().NotBeNull(); // SuperAdmin sees Finance

        var opsDash = (await _client.GetFromJsonAsync<ApiResponse<OperationsDashboardDto>>("/api/v1/dashboard/operations"))!.Data!;
        opsDash.ShipmentsInTransit.Should().Be(1);

        var shpDash = (await _client.GetFromJsonAsync<ApiResponse<ShipmentDashboardDto>>("/api/v1/dashboard/shipments"))!.Data!;
        shpDash.DistinctOrdersCovered.Should().Be(2);
        shpDash.TotalShipments.Should().Be(4);

        var delivDash = (await _client.GetFromJsonAsync<ApiResponse<DeliveryPerformanceDashboardDto>>("/api/v1/dashboard/delivery-performance"))!.Data!;
        delivDash.CompletedTerminalShipmentsCount.Should().Be(3); // S1, S2 Delivered + S4 Returned (S3 InTransit excluded)
        delivDash.DeliveredShipmentsCount.Should().Be(2);
        delivDash.InTransitActiveExcludedCount.Should().Be(1);
        delivDash.DeliveredWithSlaCount.Should().Be(2); // S1 + S2
        delivDash.OnTimeDeliveredCount.Should().Be(1); // S1
        delivDash.LateDeliveredCount.Should().Be(1); // S2
        delivDash.OnTimeDeliveryRatePercent.Should().Be(50.00m);

        var ordersDash = (await _client.GetFromJsonAsync<ApiResponse<OrdersDashboardDto>>("/api/v1/dashboard/orders"))!.Data!;
        ordersDash.TotalOrders.Should().Be(2);
        ordersDash.TotalOrderItems.Should().Be(1);
        ordersDash.TotalOrderPackages.Should().Be(2);
        ordersDash.ServiceBreakdownNote.Should().Contain("Shipments");

        var whDash = (await _client.GetFromJsonAsync<ApiResponse<WarehouseDashboardDto>>("/api/v1/dashboard/warehouse"))!.Data!;
        whDash.CompletedReceipts.Should().Be(1);
        whDash.CompletedDispatches.Should().Be(1);
        whDash.ParcelsUnloadedAtHub.Should().Be(1);

        var invDash = (await _client.GetFromJsonAsync<ApiResponse<InventoryDashboardDto>>("/api/v1/dashboard/inventory"))!.Data!;
        invDash.LowStockProductWarehouseCount.Should().Be(1);
        invDash.TransactionsCountInPeriod.Should().Be(1);

        var finDash = (await _client.GetFromJsonAsync<ApiResponse<FinanceDashboardDto>>("/api/v1/dashboard/finance"))!.Data!;
        finDash.TotalShipmentChargesAmount.Should().Be(80000m);
        finDash.UnbilledShipmentChargesAmount.Should().Be(50000m);
        finDash.DraftInvoicesTotal.Should().Be(10000m);
        finDash.IssuedInvoicesTotal.Should().Be(30000m);
        finDash.TotalUnallocatedPaymentCredit.Should().Be(5000m);
        finDash.AuthoritativeCustomerReceivables.Should().Be(10000m);

        var codDash = (await _client.GetFromJsonAsync<ApiResponse<CodDashboardDto>>("/api/v1/dashboard/cod"))!.Data!;
        codDash.ExpectedCodOnShipments.Should().Be(350000m);
        codDash.CollectedByDrivers.Should().Be(280000m);
        codDash.ExpectedVsCollectedDiscrepancy.Should().Be(70000m);
        codDash.RemittedToCustomersNet.Should().Be(195000m);
    }

    [Fact]
    public async Task DetailedReports_And_RoleBranchIsolation_EnforcedStrictly()
    {
        var ids = await SeedScenarioAsync();

        // WarehouseStaff: allowed warehouse/inventory, denied Finance/COD/Hanoi branch
        var whToken = await GetTokenAsync("rpt_wh_sgn", AppRoles.WarehouseStaff, ids.SgnBranchId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", whToken);

        (await _client.GetAsync("/api/v1/dashboard/finance")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await _client.GetAsync("/api/v1/dashboard/cod")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await _client.GetAsync($"/api/v1/dashboard/branches/{ids.HanBranchId}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await _client.GetAsync("/api/v1/reports/export/invoices")).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var whLowStock = await _client.GetFromJsonAsync<ApiResponse<PagedResponse<InventoryBalanceReportRowDto>>>("/api/v1/reports/inventory-balances?onlyLowStock=true");
        whLowStock!.Data!.Items.Should().HaveCount(1);
        whLowStock.Data.Items[0].IsBelowReorderPoint.Should().BeTrue();
        whLowStock.Data.Items[0].QuantityAvailable.Should().Be(4);

        // SGN BranchManager cannot see Finance on branch dashboard unless granted FinanceRead, AND unscoped lists exclude HAN!
        var sgnMgrToken = await GetTokenAsync("rpt_mgr_sgn", AppRoles.BranchManager, ids.SgnBranchId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sgnMgrToken);

        var sgnOrders = await _client.GetFromJsonAsync<ApiResponse<PagedResponse<OrderReportRowDto>>>("/api/v1/reports/orders");
        sgnOrders!.Data!.Items.Should().OnlyContain(o => o.BranchId == ids.SgnBranchId);
        sgnOrders.Data.Items.Should().NotContain(o => o.OrderNumber == "ORD-RPT-HAN");

        (await _client.GetAsync($"/api/v1/reports/orders?BranchId={ids.HanBranchId}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var sgnBranchSelf = (await _client.GetFromJsonAsync<ApiResponse<BranchDashboardDto>>($"/api/v1/dashboard/branches/{ids.SgnBranchId}"))!.Data!;
        sgnBranchSelf.FinancialSummary.Should().BeNull(); // BranchManager lacks Reports.FinanceRead!

        // FinanceStaff: denied Warehouse, allowed Invoices + Receivables + COD Reconciliation
        var finToken = await GetTokenAsync("rpt_fin", AppRoles.FinanceStaff);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", finToken);
        (await _client.GetAsync("/api/v1/dashboard/warehouse")).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var chargesRes = await _client.GetFromJsonAsync<ApiResponse<PagedResponse<ShippingChargeReportRowDto>>>("/api/v1/reports/shipping-charges?isInvoiced=false");
        chargesRes!.Data!.Items.Should().HaveCount(1);

        var invRes = await _client.GetFromJsonAsync<ApiResponse<PagedResponse<InvoiceReportRowDto>>>("/api/v1/reports/invoices");
        invRes!.Data!.Items.First(i => i.InvoiceNumber == "INV-ISS-1").OutstandingAmount.Should().Be(10000m);

        var codRecon = await _client.GetFromJsonAsync<ApiResponse<PagedResponse<CodReconciliationReportRowDto>>>("/api/v1/reports/cod-reconciliation");
        var sgnCod = codRecon!.Data!.Items.First(b => b.BranchId == ids.SgnBranchId);
        sgnCod.ExpectedCod.Should().Be(300000m);
        sgnCod.CollectedByDrivers.Should().Be(280000m);
        sgnCod.DiscrepancyAmount.Should().Be(20000m);
    }

    [Fact]
    public async Task Export_ScheduledReports_Validation_And_Swagger_WorkEndToEnd()
    {
        var ids = await SeedScenarioAsync();
        var adminToken = await GetTokenAsync("rpt_admin2", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Invalid date range rejected cleanly
        (await _client.GetAsync("/api/v1/reports/orders?StartDate=2026-06-10&EndDate=2026-06-01")).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Unsupported export format rejected honestly
        (await _client.GetAsync("/api/v1/reports/export/orders?format=pdf")).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Real CSV export with formula neutralization & audit log
        var csvRes = await _client.GetAsync("/api/v1/reports/export/orders?format=csv");
        csvRes.StatusCode.Should().Be(HttpStatusCode.OK);
        csvRes.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
        var bytes = await csvRes.Content.ReadAsByteArrayAsync();
        var csvBody = Encoding.UTF8.GetString(bytes.Skip(3).ToArray());
        csvBody.Should().Contain("ORD-RPT-SGN");
        csvBody.Should().Contain("'=EVIL() Merchant SGN"); // Neutralized formula!

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            (await db.AuditLogs.AnyAsync(a => a.Action == "Export" && a.SchemaName == "report")).Should().BeTrue();
        }

        // Scheduled report lifecycle under /api/v1/reports/scheduled
        var createRes = await _client.PostAsJsonAsync("/api/v1/reports/scheduled", new CreateScheduledReportRequest
        {
            ReportDefinitionId = ids.DefId,
            BranchId = ids.SgnBranchId,
            ScheduleCron = "0 7 * * *",
            TimeZoneId = "SE Asia Standard Time",
            OutputFormat = "Csv",
            RecipientEmails = "ops@test.vn"
        });
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = (await createRes.Content.ReadFromJsonAsync<ApiResponse<ScheduledReportDto>>())!.Data!;

        var delRes = await _client.DeleteAsync($"/api/v1/reports/scheduled/{created.Id}");
        delRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var deactivated = (await delRes.Content.ReadFromJsonAsync<ApiResponse<ScheduledReportDto>>())!.Data!;
        deactivated.IsActive.Should().BeFalse();

        // Swagger check
        var swaggerRes = await _client.GetAsync("/swagger/v1/swagger.json");
        var swaggerJson = await swaggerRes.Content.ReadAsStringAsync();
        swaggerRes.StatusCode.Should().Be(HttpStatusCode.OK, because: swaggerJson);
        swaggerJson.Should().Contain("/api/v1/dashboard/overview");
        swaggerJson.Should().Contain("/api/v1/reports/export/{reportType}");
        swaggerJson.Should().Contain("/api/v1/reports/cod-reconciliation");
    }
}
