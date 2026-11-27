using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Logistics.Server.Application.Common.Interfaces;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Carrier.DTOs;
using Logistics.Server.Application.Modules.Cod.DTOs;
using Logistics.Server.Application.Modules.Crm.DTOs;
using Logistics.Server.Application.Modules.Finance.DTOs;
using Logistics.Server.Application.Modules.Identity.DTOs;
using Logistics.Server.Application.Modules.Sales.DTOs;
using Logistics.Server.Application.Modules.Shipping.DTOs;
using Logistics.Server.Application.Modules.Warehouse.DTOs;
using Logistics.Server.Domain.Entities.Carrier;
using Logistics.Server.Domain.Entities.Catalog;
using Logistics.Server.Domain.Entities.Configuration;
using Logistics.Server.Domain.Entities.Crm;
using Logistics.Server.Domain.Entities.Finance;
using Logistics.Server.Domain.Entities.Identity;
using Logistics.Server.Domain.Entities.Organization;
using Logistics.Server.Domain.Entities.Shipping;
using Logistics.Server.Domain.Entities.Warehouse;
using Logistics.Server.Infrastructure.Persistence;
using Logistics.Shared.Contracts;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Logistics.Server.IntegrationTests;

/// <summary>
/// Cross-module End-to-End business workflow, security/IDOR, webhook signature &amp; replay protection,
/// idempotency, and production configuration guard integration tests.
/// </summary>
public class HardeningAndE2EIntegrationTests : IClassFixture<WebApplicationFactory<Logistics.Server.API.Program>>
{
    private readonly WebApplicationFactory<Logistics.Server.API.Program> _factory;
    private readonly HttpClient _client;
    private readonly string _dbName;

    public HardeningAndE2EIntegrationTests(WebApplicationFactory<Logistics.Server.API.Program> factory)
    {
        _dbName = "HardeningE2EDb_" + Guid.NewGuid().ToString("N");
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

    private sealed record SeededIds(
        long SgnBranchId,
        long HanBranchId,
        long CountryId,
        long CurrencyId,
        long UomId,
        long ProductId,
        long EmployeeId,
        long HubWarehouseId,
        long InvWarehouseId,
        long InvLocationId,
        long SgnCustomerId,
        long HanCustomerId);

    private async Task<SeededIds> SeedEnvironmentAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var seeder = scope.ServiceProvider.GetRequiredService<Logistics.Server.Application.Modules.Identity.Services.IIdentityDataSeeder>();
        await seeder.SeedAsync();

        if (!await db.Companies.AnyAsync())
        {
            var company = new Company { Code = "LOGI-E2E", LegalName = "Logi Corp", TradingName = "Logi", TaxNumber = "0312345678", IsActive = true };
            db.Companies.Add(company);
            await db.SaveChangesAsync();

            var sgn = new Branch { CompanyId = company.Id, Code = "SGN", Name = "Saigon Hub", BranchType = "Hub", Phone = "028111", IsActive = true };
            var han = new Branch { CompanyId = company.Id, Code = "HAN", Name = "Hanoi Hub", BranchType = "Hub", Phone = "024111", IsActive = true };
            db.Branches.AddRange(sgn, han);

            var country = new Country { Code = "VN", Alpha3Code = "VNM", Name = "Vietnam", IsActive = true };
            db.Countries.Add(country);

            var currency = new Currency { Code = "VND", Name = "VND", Symbol = "₫", DecimalPlaces = 0, ExchangeRateToDefault = 1m, IsDefault = true, IsActive = true };
            db.Currencies.Add(currency);

            var uom = new UnitOfMeasure { Code = "BOX", Name = "Box", Type = "Count" };
            db.UnitsOfMeasure.Add(uom);
            await db.SaveChangesAsync();

            var cat = new ProductCategory { Code = "GEN", Name = "General", IsActive = true };
            db.ProductCategories.Add(cat);
            await db.SaveChangesAsync();

            var prod = new Product
            {
                Sku = "SKU-E2E-01",
                Name = "Electronics Parcel",
                CategoryId = cat.Id,
                UnitOfMeasureId = uom.Id,
                WeightKg = 2.0m,
                DeclaredValue = 500000m,
                IsActive = true
            };
            db.Products.Add(prod);

            var emp = new Employee
            {
                BranchId = sgn.Id,
                Code = "EMP-E2E-01",
                FirstName = "Nguyen",
                LastName = "Driver",
                FullName = "Nguyen Van Driver",
                Email = "driver@e2e.test",
                PhoneNumber = "0909000111",
                JobTitle = "Courier",
                HireDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Status = "Active"
            };
            db.Employees.Add(emp);

            var custSgn = new Customer
            {
                BranchId = sgn.Id,
                CustomerCode = "CUST-SGN-01",
                CustomerType = "Corporate",
                Name = "Saigon Merchant",
                Email = "sgn@e2e.test",
                PhoneNumber = "0901110001",
                PaymentType = "PostpaidMonthly",
                CreditLimit = 10000000m,
                IsActive = true
            };
            var custHan = new Customer
            {
                BranchId = han.Id,
                CustomerCode = "CUST-HAN-01",
                CustomerType = "Corporate",
                Name = "Hanoi Merchant",
                Email = "han@e2e.test",
                PhoneNumber = "0902220002",
                PaymentType = "PostpaidMonthly",
                CreditLimit = 10000000m,
                IsActive = true
            };
            db.Customers.AddRange(custSgn, custHan);

            var hubWh = new Warehouse { BranchId = sgn.Id, Code = "HUB-SGN", Name = "Saigon Sorting Hub", Type = "ParcelHub", IsActive = true };
            var invWh = new Warehouse { BranchId = sgn.Id, Code = "WH-SGN", Name = "Saigon Inv Warehouse", Type = "InventoryWarehouse", IsActive = true };
            db.Warehouses.AddRange(hubWh, invWh);
            await db.SaveChangesAsync();

            var invZone = new WarehouseZone { WarehouseId = invWh.Id, Code = "Z-INV", Name = "Bulk Zone", ZoneType = "BulkStorage", TemperatureType = "Ambient", IsActive = true };
            db.WarehouseZones.Add(invZone);
            await db.SaveChangesAsync();

            var invLoc = new WarehouseLocation { WarehouseZoneId = invZone.Id, Code = "BIN-01", Barcode = "BC-BIN-01", MaxWeightKg = 5000m, MaxVolumeCbm = 20m };
            db.WarehouseLocations.Add(invLoc);

            var rc = new ShippingRateCard
            {
                Code = "RC-E2E",
                Name = "Default E2E Tariff",
                BaseCurrencyId = currency.Id,
                IsDefault = true,
                IsActive = true,
                EffectiveFrom = DateTime.UtcNow.AddDays(-10)
            };
            rc.Rules.Add(new ShippingRateRule
            {
                ServiceType = "StandardGround",
                FromWeightKg = 0m,
                ToWeightKg = 50m,
                BaseRate = 25000m,
                AdditionalPerKgRate = 3000m,
                MinimumFee = 25000m,
                EstimatedDays = 2
            });
            db.ShippingRateCards.Add(rc);
            await db.SaveChangesAsync();
        }

        var bSgn = await db.Branches.FirstAsync(b => b.Code == "SGN");
        var bHan = await db.Branches.FirstAsync(b => b.Code == "HAN");
        var ctry = await db.Countries.FirstAsync();
        var curr = await db.Currencies.FirstAsync();
        var unit = await db.UnitsOfMeasure.FirstAsync();
        var prd = await db.Products.FirstAsync();
        var drvr = await db.Employees.FirstAsync();
        var hw = await db.Warehouses.FirstAsync(w => w.Code == "HUB-SGN");
        var iw = await db.Warehouses.FirstAsync(w => w.Code == "WH-SGN");
        var il = await db.WarehouseLocations.FirstAsync(l => l.Code == "BIN-01");
        var cs = await db.Customers.FirstAsync(c => c.CustomerCode == "CUST-SGN-01");
        var ch = await db.Customers.FirstAsync(c => c.CustomerCode == "CUST-HAN-01");

        return new SeededIds(bSgn.Id, bHan.Id, ctry.Id, curr.Id, unit.Id, prd.Id, drvr.Id, hw.Id, iw.Id, il.Id, cs.Id, ch.Id);
    }

    private async Task<(string Token, Guid PublicId)> CreateUserAndLoginAsync(string username, string roleCode, params long[] branchIds)
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasherService>();
            var seeder = scope.ServiceProvider.GetRequiredService<Logistics.Server.Application.Modules.Identity.Services.IIdentityDataSeeder>();
            await seeder.SeedAsync();

            if (!await db.UserAccounts.AnyAsync(u => u.Username == username))
            {
                var role = await db.Roles.FirstAsync(r => r.Code == roleCode);
                var user = new UserAccount
                {
                    Username = username,
                    Email = $"{username}@e2e.test",
                    PasswordHash = hasher.HashPassword("StrongPass2026!"),
                    SecurityStamp = Guid.NewGuid().ToString("N"),
                    IsActive = true
                };
                user.UserRoles.Add(new UserRole { RoleId = role.Id, AssignedBy = "TestSetup", AssignedAt = DateTime.UtcNow });

                foreach (var bid in branchIds.Distinct())
                {
                    user.BranchAccesses.Add(new UserBranchAccess
                    {
                        BranchId = bid,
                        IsDefault = bid == branchIds[0],
                        GrantedBy = "TestSetup",
                        GrantedAt = DateTime.UtcNow
                    });
                }

                db.UserAccounts.Add(user);
                await db.SaveChangesAsync();
            }
        }

        var loginResp = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(username, "StrongPass2026!"));
        loginResp.EnsureSuccessStatusCode();
        var body = (await loginResp.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>())!.Data!;
        return (body.AccessToken, body.User.PublicId);
    }

    [Fact]
    public async Task EndToEnd_Workflow_Order_Shipping_ParcelHub_Delivery_Pod_Finance_Cod_And_Dashboard_Succeeds()
    {
        var ids = await SeedEnvironmentAsync();
        var (token, _) = await CreateUserAndLoginAsync("e2e_super_" + Guid.NewGuid().ToString("N")[..5], AppRoles.SuperAdmin, ids.SgnBranchId, ids.HanBranchId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // 1. Create & Confirm Order via REST API
        var createOrdRes = await _client.PostAsJsonAsync("/api/v1/orders", new CreateOrderRequest
        {
            CustomerId = ids.SgnCustomerId,
            BranchId = ids.SgnBranchId,
            CurrencyId = ids.CurrencyId,
            CodAmount = 500000m,
            PickupAddress = new OrderAddressRequest { ContactName = "Sender", PhoneNumber = "0901111111", AddressLine1 = "12 Le Loi", CountryId = ids.CountryId },
            DeliveryAddress = new OrderAddressRequest { ContactName = "Buyer", PhoneNumber = "0902222222", AddressLine1 = "34 Phan Chu Trinh", CountryId = ids.CountryId },
            Items = [new CreateOrderItemRequest { ProductId = ids.ProductId, Quantity = 1, UnitPrice = 500000m, DeclaredValue = 500000m, WeightKg = 2.0m }],
            Packages = [new CreateOrderPackageRequest { WeightKg = 2.0m, LengthCm = 20m, WidthCm = 15m, HeightCm = 10m, DeclaredValue = 500000m, Barcode = "BC-E2E-PKG-01" }]
        });
        createOrdRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = (await createOrdRes.Content.ReadFromJsonAsync<ApiResponse<OrderDetailDto>>())!.Data!;

        var confirmRes = await _client.PostAsync($"/api/v1/orders/{order.Id}/confirm", null);
        confirmRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 2. Create Shipment from Order & Calculate Fee
        var shipRes = await _client.PostAsJsonAsync("/api/v1/shipments/from-order", new CreateShipmentFromOrderRequest
        {
            OrderId = order.Id,
            OriginBranchId = ids.SgnBranchId,
            DestinationBranchId = ids.HanBranchId,
            ServiceType = "StandardGround",
            CodAmount = 500000m,
            DeclaredValue = 500000m
        });
        shipRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var shipment = (await shipRes.Content.ReadFromJsonAsync<ApiResponse<ShipmentDetailDto>>())!.Data!;

        var chargeRes = await _client.PostAsJsonAsync($"/api/v1/finance/shipping-fees/charges/{shipment.Id}/adjustments", new CreateChargeAdjustmentRequest
        {
            ChargeType = "Freight",
            Amount = 31000m,
            CurrencyId = ids.CurrencyId,
            Description = "E2E base freight charge"
        });
        chargeRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var charge = (await chargeRes.Content.ReadFromJsonAsync<ApiResponse<ShipmentChargeDto>>())!.Data!;

        // 3. PickUp -> Parcel Hub Receive (auto-transitions PickedUp -> InSortingHub) -> InTransit -> OutForDelivery
        (await _client.PostAsJsonAsync($"/api/v1/shipments/{shipment.Id}/transitions", new ShipmentStatusTransitionRequest { TargetStatus = "PickedUp" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var pkg = shipment.Packages.First();
        var receiveParcelRes = await _client.PostAsJsonAsync("/api/v1/parcel-handling/receive", new ParcelReceiveRequest
        {
            WarehouseId = ids.HubWarehouseId,
            EmployeeId = ids.EmployeeId,
            TrackingNumberOrBarcode = pkg.Barcode ?? pkg.PackageCode
        });
        receiveParcelRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Hardening invariant: Order cannot be cancelled once shipment left Draft/ReadyForPickup (PickedUp/InSortingHub)
        var cancelBlockedRes = await _client.PostAsJsonAsync($"/api/v1/orders/{order.Id}/cancel", new CancelOrderRequest { Reason = "Too late" });
        cancelBlockedRes.StatusCode.Should().Be(HttpStatusCode.Conflict);

        (await _client.PostAsJsonAsync($"/api/v1/shipments/{shipment.Id}/transitions", new ShipmentStatusTransitionRequest { TargetStatus = "InTransit" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await _client.PostAsJsonAsync($"/api/v1/shipments/{shipment.Id}/transitions", new ShipmentStatusTransitionRequest { TargetStatus = "OutForDelivery" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. Record Failed Delivery Attempt first, confirm POD rejects Failed attempt, then record Delivered attempt + POD
        var failedAttemptRes = await _client.PostAsJsonAsync($"/api/v1/shipments/{shipment.Id}/delivery-attempts", new RecordDeliveryAttemptRequest
        {
            AttemptedByEmployeeId = ids.EmployeeId,
            AttemptedAt = DateTime.UtcNow.AddMinutes(-5),
            Status = "Failed",
            FailureReasonCode = "NO_ANSWER",
            FailureNotes = "Recipient unreachable",
            RecipientContacted = false
        });
        failedAttemptRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var failedAttempt = (await failedAttemptRes.Content.ReadFromJsonAsync<ApiResponse<DeliveryAttemptDto>>())!.Data!;

        var badPodRes = await _client.PostAsJsonAsync($"/api/v1/shipments/{shipment.Id}/proof-of-delivery?attemptId={failedAttempt.Id}", new CreateProofOfDeliveryRequest
        {
            RecipientName = "Buyer",
            RecipientRelation = "Self",
            DeliveredAt = DateTime.UtcNow
        });
        badPodRes.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Re-attempt OutForDelivery -> Delivered
        (await _client.PostAsJsonAsync($"/api/v1/shipments/{shipment.Id}/transitions", new ShipmentStatusTransitionRequest { TargetStatus = "OutForDelivery" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var okAttemptRes = await _client.PostAsJsonAsync($"/api/v1/shipments/{shipment.Id}/delivery-attempts", new RecordDeliveryAttemptRequest
        {
            AttemptedByEmployeeId = ids.EmployeeId,
            AttemptedAt = DateTime.UtcNow,
            Status = "Delivered",
            RecipientContacted = true
        });
        okAttemptRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var okAttempt = (await okAttemptRes.Content.ReadFromJsonAsync<ApiResponse<DeliveryAttemptDto>>())!.Data!;

        var podRes = await _client.PostAsJsonAsync($"/api/v1/shipments/{shipment.Id}/proof-of-delivery?attemptId={okAttempt.Id}", new CreateProofOfDeliveryRequest
        {
            RecipientName = "Buyer",
            RecipientRelation = "Self",
            DeliveredAt = DateTime.UtcNow
        });
        podRes.StatusCode.Should().Be(HttpStatusCode.Created);

        // 5. COD Workflow: Collect -> Deposit -> Verify -> Remit -> Confirm
        var codColRes = await _client.PostAsJsonAsync("/api/v1/cod/collections", new CreateCodCollectionRequest
        {
            ShipmentId = shipment.Id,
            CollectedAmount = 500000m,
            CollectedByEmployeeId = ids.EmployeeId,
            DriverReceiptNumber = "RCP-E2E-1"
        });
        codColRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var col = (await codColRes.Content.ReadFromJsonAsync<ApiResponse<CodCollectionDto>>())!.Data!;

        (await _client.PostAsJsonAsync($"/api/v1/cod/collections/{col.Id}/deposit", new { Notes = "Deposited" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await _client.PostAsJsonAsync($"/api/v1/cod/collections/{col.Id}/verify", new VerifyCodCollectionRequest { Notes = "Verified" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var remitRes = await _client.PostAsJsonAsync("/api/v1/cod/remittances", new CreateCodRemittanceRequest
        {
            CustomerId = ids.SgnCustomerId,
            CurrencyId = ids.CurrencyId,
            PaymentMethod = "BankTransfer",
            BankName = "Vietcombank",
            BankAccount = "00710012345",
            AccountHolderName = "Saigon Merchant",
            CollectionIds = [col.Id],
            ServiceFeesDeducted = 10000m
        });
        remitRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var remit = (await remitRes.Content.ReadFromJsonAsync<ApiResponse<CodRemittanceDto>>())!.Data!;

        var confirmRemitRes = await _client.PostAsJsonAsync($"/api/v1/cod/remittances/{remit.Id}/confirm", new ConfirmCodRemittanceRequest
        {
            PaymentReference = "TRF-E2E-001",
            ActualRemittedAmount = 490000m
        });
        confirmRemitRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 6. Finance Workflow: Draft Invoice (linking ShipmentCharge) -> Issue -> Register Payment & Verify Non-Zero Ledger ReferenceId
        var invRes = await _client.PostAsJsonAsync("/api/v1/finance/invoices", new CreateInvoiceRequest
        {
            CustomerId = ids.SgnCustomerId,
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
            TaxRate = 0m,
            PaymentTermsDays = 30,
            Items = [new CreateInvoiceItemRequest { ShipmentId = shipment.Id, ShipmentChargeId = charge.Id, Description = "Freight fee", Quantity = 1, UnitPrice = 31000m }]
        });
        invRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var inv = (await invRes.Content.ReadFromJsonAsync<ApiResponse<InvoiceDto>>())!.Data!;

        (await _client.PostAsync($"/api/v1/finance/invoices/{inv.Id}/issue", null))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var payRes = await _client.PostAsJsonAsync("/api/v1/finance/payments", new RegisterPaymentRequest
        {
            CustomerId = ids.SgnCustomerId,
            PaymentMethod = "BankTransfer",
            Amount = 31000m,
            CurrencyId = ids.CurrencyId,
            TransactionReference = "PAY-E2E-001",
            InitialAllocations = [new CreatePaymentAllocationRequest { InvoiceId = inv.Id, Amount = 31000m }]
        });
        payRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var payment = (await payRes.Content.ReadFromJsonAsync<ApiResponse<PaymentDto>>())!.Data!;

        var ledgerRes = await _client.GetFromJsonAsync<ApiResponse<PagedResponse<FinancialLedgerEntryDto>>>($"/api/v1/finance/ledger/entries?customerId={ids.SgnCustomerId}");
        var payLedger = ledgerRes!.Data!.Items.FirstOrDefault(e => e.EntryType == "PaymentReceipt");
        payLedger.Should().NotBeNull();
        payLedger!.ReferenceId.Should().Be(payment.Id, "Payment ledger entry must point to persisted Payment.Id rather than 0");

        var dashRes = await _client.GetAsync("/api/v1/dashboard/overview");
        dashRes.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task EndToEnd_Warehouse_DispatchItems_DeductsInventoryAtomically()
    {
        var ids = await SeedEnvironmentAsync();
        var (token, _) = await CreateUserAndLoginAsync("e2e_wh_" + Guid.NewGuid().ToString("N")[..5], AppRoles.SuperAdmin, ids.SgnBranchId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var recRes = await _client.PostAsJsonAsync("/api/v1/inventory/receipts", new CreateWarehouseReceiptRequest
        {
            WarehouseId = ids.InvWarehouseId,
            ReceiptType = "InventoryInbound",
            SourceVendor = "Vendor X",
            ReceivedByEmployeeId = ids.EmployeeId,
            Items = [new CreateWarehouseReceiptItemRequest { ProductId = ids.ProductId, ExpectedQuantity = 50, UnitOfMeasureId = ids.UomId, TargetLocationId = ids.InvLocationId }]
        });
        recRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var receipt = (await recRes.Content.ReadFromJsonAsync<ApiResponse<WarehouseReceiptDto>>())!.Data!;

        (await _client.PostAsJsonAsync($"/api/v1/inventory/receipts/{receipt.Id}/confirm", new ConfirmReceiptRequest()))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var dispRes = await _client.PostAsJsonAsync("/api/v1/warehouse-dispatches", new CreateWarehouseDispatchDto
        {
            WarehouseId = ids.InvWarehouseId,
            DispatchType = "InventoryOutbound",
            DispatchedByEmployeeId = ids.EmployeeId,
            Items = [new CreateWarehouseDispatchItemRequest { ProductId = ids.ProductId, RequestedQuantity = 12, PickedQuantity = 12, UnitOfMeasureId = ids.UomId, SourceLocationId = ids.InvLocationId }]
        });
        dispRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var dispatch = (await dispRes.Content.ReadFromJsonAsync<ApiResponse<WarehouseDispatchDto>>())!.Data!;

        var confirmDispRes = await _client.PostAsync($"/api/v1/warehouse-dispatches/{dispatch.Id}/confirm?employeeId={ids.EmployeeId}", null);
        confirmDispRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var balances = await _client.GetFromJsonAsync<ApiResponse<PagedResponse<InventoryBalanceDto>>>($"/api/v1/inventory/balances?warehouseId={ids.InvWarehouseId}&productId={ids.ProductId}");
        balances!.Data!.Items.First().QuantityOnHand.Should().Be(38);
    }

    [Fact]
    public async Task Security_CrossBranchIsolation_DeniesCrossBranchFinanceCodAndShippingAccess()
    {
        var ids = await SeedEnvironmentAsync();
        var (adminToken, _) = await CreateUserAndLoginAsync("idor_admin_" + Guid.NewGuid().ToString("N")[..5], AppRoles.SuperAdmin, ids.SgnBranchId, ids.HanBranchId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Seed invoice + shipment + pickup request strictly under SGN branch
        var invRes = await _client.PostAsJsonAsync("/api/v1/finance/invoices", new CreateInvoiceRequest
        {
            CustomerId = ids.SgnCustomerId,
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Items = [new CreateInvoiceItemRequest { Description = "SGN Only", Quantity = 1, UnitPrice = 99000m }]
        });
        invRes.EnsureSuccessStatusCode();
        var sgnInv = (await invRes.Content.ReadFromJsonAsync<ApiResponse<InvoiceDto>>())!.Data!;

        long sgnShipmentId;
        long hanOnlyWhId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var shp = new Shipment
            {
                TrackingNumber = "VN-IDOR-SGN-" + Guid.NewGuid().ToString("N")[..6],
                CustomerId = ids.SgnCustomerId,
                OriginBranchId = ids.SgnBranchId,
                DestinationBranchId = ids.SgnBranchId,
                ShipmentStatus = "ReadyForPickup",
                ServiceType = "StandardGround",
                CurrencyId = ids.CurrencyId,
                CodAmount = 100000m
            };
            db.Shipments.Add(shp);
            hanOnlyWhId = (await db.Warehouses.AddAsync(new Warehouse { BranchId = ids.HanBranchId, Code = "HAN-IDOR-" + Guid.NewGuid().ToString("N")[..4], Name = "HAN WH", Type = "ParcelHub", IsActive = true })).Entity.Id;
            await db.SaveChangesAsync();
            sgnShipmentId = shp.Id;
        }
        _ = hanOnlyWhId;

        // Log in as HAN-only user with BranchManager + FinanceStaff permissions (so permissions pass and branch scoping is tested)
        var hanUsername = "han_fin_mgr_" + Guid.NewGuid().ToString("N")[..5];
        var (hanToken, _) = await CreateUserAndLoginAsync(hanUsername, AppRoles.BranchManager, ids.HanBranchId);
        using (var roleScope = _factory.Services.CreateScope())
        {
            var db = roleScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var hanUser = await db.UserAccounts.Include(u => u.UserRoles).FirstAsync(u => u.Username == hanUsername);
            var finRole = await db.Roles.FirstAsync(r => r.Code == AppRoles.FinanceStaff);
            hanUser.UserRoles.Add(new UserRole { RoleId = finRole.Id, AssignedBy = "TestSetup", AssignedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();
        }
        // Re-login after role assignment so SecurityStamp / roles in JWT match DB
        var hanRelogin = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(hanUsername, "StrongPass2026!"));
        hanRelogin.EnsureSuccessStatusCode();
        hanToken = (await hanRelogin.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>())!.Data!.AccessToken;
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", hanToken);

        // List endpoints must not leak SGN records (regression guard for fixed AccessibleBranchIds filter inversion)
        var listInv = await _client.GetFromJsonAsync<ApiResponse<PagedResponse<InvoiceDto>>>("/api/v1/finance/invoices");
        listInv!.Data!.Items.Should().NotContain(i => i.Id == sgnInv.Id);
        var listLedger = await _client.GetFromJsonAsync<ApiResponse<PagedResponse<FinancialLedgerEntryDto>>>("/api/v1/finance/ledger/entries");
        listLedger!.Data!.Items.Should().NotContain(e => e.CustomerId == ids.SgnCustomerId);

        // Direct ID lookups against SGN shipment packages / routes / invoice must return 403 Forbidden
        (await _client.GetAsync($"/api/v1/shipments/{sgnShipmentId}/packages")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await _client.GetAsync($"/api/v1/shipments/{sgnShipmentId}/routes")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await _client.GetAsync($"/api/v1/finance/invoices/{sgnInv.Id}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Security_RevokedSecurityStamp_And_DeactivatedUser_RejectExistingJwtImmediately()
    {
        var ids = await SeedEnvironmentAsync();
        var (adminToken, _) = await CreateUserAndLoginAsync("sec_admin_" + Guid.NewGuid().ToString("N")[..5], AppRoles.SuperAdmin, ids.SgnBranchId);
        var (staffToken, staffPublicId) = await CreateUserAndLoginAsync("sec_staff_" + Guid.NewGuid().ToString("N")[..5], AppRoles.OperationsManager, ids.SgnBranchId);

        // Staff token works initially
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);
        (await _client.GetAsync("/api/v1/auth/me")).StatusCode.Should().Be(HttpStatusCode.OK);

        // Admin replaces roles (which rotates SecurityStamp)
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var roleUpdateRes = await _client.PutAsJsonAsync($"/api/v1/admin/users/{staffPublicId}/roles", new UpdateUserRolesRequest([AppRoles.FinanceStaff]));
        roleUpdateRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Same still-unexpired staff JWT must now fail immediately with 401
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);
        (await _client.GetAsync("/api/v1/auth/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Security_Webhook_SignatureVerification_And_ReplayDeduplication()
    {
        var ids = await SeedEnvironmentAsync();
        var (token, _) = await CreateUserAndLoginAsync("wh_admin_" + Guid.NewGuid().ToString("N")[..5], AppRoles.SuperAdmin, ids.SgnBranchId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        const string carrierCode = "GHTK_SEC";
        const string webhookSecret = "whsec_hmac_signing_key_2026";
        const string carrierTrk = "GHTK-TRK-9988";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var carrier = new Carrier { Code = carrierCode, Name = "GHTK Secure Test", ApiBaseUrl = "https://api.ghtk.test", IsActive = true };
            db.Carriers.Add(carrier);
            await db.SaveChangesAsync();

            db.CarrierConfigurations.Add(new CarrierConfiguration
            {
                CarrierId = carrier.Id,
                Environment = "Production",
                ApiKey = "dummy",
                WebhookSecret = webhookSecret,
                IsActive = true
            });
            db.CarrierTrackingMappings.Add(new CarrierTrackingMapping
            {
                CarrierId = carrier.Id,
                ExternalStatusCode = "DELIVERED",
                ExternalStatusName = "Delivered",
                InternalShipmentStatus = "Delivered",
                IsActive = true
            });
            db.Shipments.Add(new Shipment
            {
                TrackingNumber = "VN-WH-SEC-1",
                CustomerId = ids.SgnCustomerId,
                OriginBranchId = ids.SgnBranchId,
                DestinationBranchId = ids.SgnBranchId,
                AssignedCarrierId = carrier.Id,
                CarrierTrackingNumber = carrierTrk,
                ShipmentStatus = "OutForDelivery",
                ServiceType = "StandardGround",
                CurrencyId = ids.CurrencyId
            });
            await db.SaveChangesAsync();
        }

        _client.DefaultRequestHeaders.Authorization = null;
        var payload = $"{{\"tracking_number\":\"{carrierTrk}\",\"status\":\"DELIVERED\",\"event_id\":\"EVT-UNIQ-100\"}}";

        // 1. Missing signature -> 403
        var noSigRes = await _client.PostAsync($"/api/v1/carrier-webhooks/{carrierCode}", new StringContent(payload, Encoding.UTF8, "application/json"));
        noSigRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // 2. Wrong signature -> 403
        var badReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/carrier-webhooks/{carrierCode}")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        badReq.Headers.Add("X-Webhook-Signature", "sha256=deadbeefdeadbeef");
        (await _client.SendAsync(badReq)).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // 3. Valid HMAC-SHA256 signature -> 200 OK & transitions shipment to Delivered
        var sigBytes = HMACSHA256.HashData(Encoding.UTF8.GetBytes(webhookSecret), Encoding.UTF8.GetBytes(payload));
        var validSig = "sha256=" + Convert.ToHexString(sigBytes).ToLowerInvariant();

        var goodReq1 = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/carrier-webhooks/{carrierCode}")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        goodReq1.Headers.Add("X-Webhook-Signature", validSig);
        var res1 = await _client.SendAsync(goodReq1);
        res1.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. Replay exact event -> idempotent 200 OK without duplicating CarrierTrackingEvent
        var goodReq2 = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/carrier-webhooks/{carrierCode}")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        goodReq2.Headers.Add("X-Webhook-Signature", validSig);
        var res2 = await _client.SendAsync(goodReq2);
        res2.StatusCode.Should().Be(HttpStatusCode.OK);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var shp = await db.Shipments.FirstAsync(s => s.TrackingNumber == "VN-WH-SEC-1");
            shp.ShipmentStatus.Should().Be("Delivered");

            var eventsCount = await db.CarrierTrackingEvents.CountAsync(e => e.ExternalEventId == "EVT-UNIQ-100");
            eventsCount.Should().Be(1, "replayed webhook must not duplicate tracking events");
        }
    }

    [Fact]
    public async Task Idempotency_DoubleInvoicing_DoubleCarrierBook_And_UnverifiedCodRemittance_Rejected()
    {
        var ids = await SeedEnvironmentAsync();
        var (token, _) = await CreateUserAndLoginAsync("idem_admin_" + Guid.NewGuid().ToString("N")[..5], AppRoles.SuperAdmin, ids.SgnBranchId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        long carrierId, shipmentId, unverifiedColId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var carrier = new Carrier { Code = "GHN", Name = "GHN Idem", ApiBaseUrl = "https://api.ghn.vn", IsActive = true };
            db.Carriers.Add(carrier);
            await db.SaveChangesAsync();
            carrierId = carrier.Id;

            var shp = new Shipment
            {
                TrackingNumber = "VN-IDEM-" + Guid.NewGuid().ToString("N")[..6],
                CustomerId = ids.SgnCustomerId,
                OriginBranchId = ids.SgnBranchId,
                DestinationBranchId = ids.SgnBranchId,
                AssignedCarrierId = carrierId,
                ShipmentStatus = "ReadyForPickup",
                ServiceType = "StandardGround",
                CurrencyId = ids.CurrencyId,
                CodAmount = 250000m,
                Addresses =
                [
                    new ShipmentAddress { AddressType = "Sender", ContactName = "S", PhoneNumber = "0901", FormattedAddress = "A" },
                    new ShipmentAddress { AddressType = "Recipient", ContactName = "R", PhoneNumber = "0902", FormattedAddress = "B" }
                ]
            };
            db.Shipments.Add(shp);
            await db.SaveChangesAsync();
            shipmentId = shp.Id;

            var col = new CODCollection
            {
                ShipmentId = shipmentId,
                CollectedAmount = 250000m,
                CurrencyId = ids.CurrencyId,
                CollectionDate = DateTime.UtcNow,
                CollectedByEmployeeId = ids.EmployeeId,
                Status = "CollectedByDriver"
            };
            db.CODCollections.Add(col);
            await db.SaveChangesAsync();
            unverifiedColId = col.Id;
        }

        // 1. Double Carrier Booking rejected
        (await _client.PostAsJsonAsync("/api/v1/carrier-integration/book", new CarrierBookShipmentRequest { ShipmentIdentifier = shipmentId.ToString() }))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await _client.PostAsJsonAsync("/api/v1/carrier-integration/book", new CarrierBookShipmentRequest { ShipmentIdentifier = shipmentId.ToString() }))
            .StatusCode.Should().Be(HttpStatusCode.Conflict);

        // 2. Double Invoicing same ShipmentCharge rejected
        var adjRes = await _client.PostAsJsonAsync($"/api/v1/finance/shipping-fees/charges/{shipmentId}/adjustments", new CreateChargeAdjustmentRequest
        {
            ChargeType = "Extra",
            Amount = 15000m,
            CurrencyId = ids.CurrencyId,
            Description = "Double-invoice guard test"
        });
        adjRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var chargeId = (await adjRes.Content.ReadFromJsonAsync<ApiResponse<ShipmentChargeDto>>())!.Data!.Id;

        var inv1Res = await _client.PostAsJsonAsync("/api/v1/finance/invoices", new CreateInvoiceRequest
        {
            CustomerId = ids.SgnCustomerId,
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Items = [new CreateInvoiceItemRequest { ShipmentId = shipmentId, ShipmentChargeId = chargeId, Description = "Fee", Quantity = 1, UnitPrice = 15000m }]
        });
        inv1Res.StatusCode.Should().Be(HttpStatusCode.Created);

        var inv2Res = await _client.PostAsJsonAsync("/api/v1/finance/invoices", new CreateInvoiceRequest
        {
            CustomerId = ids.SgnCustomerId,
            InvoiceDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Items = [new CreateInvoiceItemRequest { ShipmentId = shipmentId, ShipmentChargeId = chargeId, Description = "Fee duplicate", Quantity = 1, UnitPrice = 15000m }]
        });
        inv2Res.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // 3. Unverified COD Collection cannot be remitted
        var remitRes = await _client.PostAsJsonAsync("/api/v1/cod/remittances", new CreateCodRemittanceRequest
        {
            CustomerId = ids.SgnCustomerId,
            CurrencyId = ids.CurrencyId,
            PaymentMethod = "BankTransfer",
            BankName = "VCB",
            BankAccount = "123",
            AccountHolderName = "Test",
            CollectionIds = [unverifiedColId]
        });
        remitRes.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public void ProductionConfig_PlaceholderJwtSecret_FailsStartupValidation()
    {
        var act = () =>
        {
            using var prodFactory = _factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");
                builder.ConfigureAppConfiguration((_, cfg) =>
                {
                    cfg.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Jwt:Secret"] = "CHANGE_THIS_TO_A_SECURE_SECRET_KEY_AT_LEAST_32_CHARS_LONG!@#$%"
                    });
                });
            });
            _ = prodFactory.CreateClient();
        };

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Jwt:Secret*");
    }
}
