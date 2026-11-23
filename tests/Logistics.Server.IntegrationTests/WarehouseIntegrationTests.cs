using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Logistics.Server.Application.Common.Interfaces;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Identity.DTOs;
using Logistics.Server.Application.Modules.Warehouse.DTOs;
using Logistics.Server.Domain.Entities.Catalog;
using Logistics.Server.Domain.Entities.Configuration;
using Logistics.Server.Domain.Entities.Crm;
using Logistics.Server.Domain.Entities.Identity;
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

public class WarehouseIntegrationTests : IClassFixture<WebApplicationFactory<Logistics.Server.API.Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Logistics.Server.API.Program> _factory;
    private readonly string _dbName;

    public WarehouseIntegrationTests(WebApplicationFactory<Logistics.Server.API.Program> factory)
    {
        _dbName = "WarehouseTestDb_" + Guid.NewGuid().ToString("N");

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptors = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    d.ServiceType.Name.Contains("DbContextOptions") ||
                    d.ServiceType == typeof(ApplicationDbContext)).ToList();

                foreach (var descriptor in descriptors)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase(_dbName));
            });
        });

        _client = _factory.CreateClient();
    }

    private async Task SeedBaseDataAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var seeder = scope.ServiceProvider.GetRequiredService<Logistics.Server.Application.Modules.Identity.Services.IIdentityDataSeeder>();
        await seeder.SeedAsync();

        if (!await context.Companies.AnyAsync())
        {
            var company = new Company
            {
                Code = "LOGICORP",
                LegalName = "Logistics Corporation",
                TradingName = "LogiCorp",
                TaxNumber = "0101234567",
                IsActive = true
            };
            await context.Companies.AddAsync(company);
            await context.SaveChangesAsync();

            var branch1 = new Branch
            {
                CompanyId = company.Id,
                Code = "SGN",
                Name = "Saigon Central Hub",
                BranchType = "Hub",
                Phone = "0281234567",
                IsActive = true
            };
            var branch2 = new Branch
            {
                CompanyId = company.Id,
                Code = "HAN",
                Name = "Hanoi Central Hub",
                BranchType = "Hub",
                Phone = "0241234567",
                IsActive = true
            };
            await context.Branches.AddRangeAsync(branch1, branch2);
            await context.SaveChangesAsync();

            var country = new Country
            {
                Code = "VN",
                Alpha3Code = "VNM",
                Name = "Vietnam",
                IsActive = true
            };
            await context.Countries.AddAsync(country);
            await context.SaveChangesAsync();

            var currency = new Currency
            {
                Code = "VND",
                Name = "Vietnamese Dong",
                Symbol = "₫",
                DecimalPlaces = 0,
                ExchangeRateToDefault = 1.0m,
                IsDefault = true,
                IsActive = true
            };
            await context.Currencies.AddAsync(currency);
            await context.SaveChangesAsync();

            var uom = new UnitOfMeasure
            {
                Code = "PCS",
                Name = "Pieces",
                Type = "Count",
                ConversionFactor = 1.0m,
                IsBaseUnit = true,
                IsActive = true
            };
            await context.UnitsOfMeasure.AddAsync(uom);
            await context.SaveChangesAsync();

            var category = new ProductCategory
            {
                Code = "ELEC",
                Name = "Electronics",
                IsActive = true
            };
            await context.ProductCategories.AddAsync(category);
            await context.SaveChangesAsync();

            var product = new Product
            {
                CategoryId = category.Id,
                UnitOfMeasureId = uom.Id,
                CurrencyId = currency.Id,
                Sku = "SKU-SP-001",
                Name = "Smartphone Pro Max",
                DeclaredValue = 15000000m,
                IsActive = true
            };
            await context.Products.AddAsync(product);
            await context.SaveChangesAsync();

            var employee = new Employee
            {
                BranchId = branch1.Id,
                Code = "EMP_WH_01",
                FirstName = "Huy",
                LastName = "Tran",
                FullName = "Tran Huy Warehouse Staff",
                Email = "huy.tran@logistics.vn",
                PhoneNumber = "0909999888",
                JobTitle = "Warehouse Operator",
                HireDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
                Status = "Active"
            };
            await context.Employees.AddAsync(employee);
            await context.SaveChangesAsync();

            var customer = new Customer
            {
                BranchId = branch1.Id,
                CustomerCode = "CUST-WH-001",
                CustomerType = "Corporate",
                Name = "Warehouse Customer",
                Email = "wh-customer@logicorp.vn",
                PhoneNumber = "0901234567",
                IsActive = true
            };
            await context.Customers.AddAsync(customer);
            await context.SaveChangesAsync();
        }
    }

    private async Task<string> GetTokenAsync(string username, string roleCode)
    {
        await SeedBaseDataAsync();

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasherService>();

        var password = "StrongPassword2026!";
        var user = await context.UserAccounts
            .Include(u => u.UserRoles)
            .Include(u => u.BranchAccesses)
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
        {
            var role = await context.Roles.FirstOrDefaultAsync(r => r.Code == roleCode);
            user = new UserAccount
            {
                Username = username,
                Email = $"{username}@logistics.test",
                PasswordHash = hasher.HashPassword(password),
                SecurityStamp = Guid.NewGuid().ToString("N"),
                IsActive = true
            };

            if (role != null)
            {
                user.UserRoles.Add(new UserRole
                {
                    RoleId = role.Id,
                    AssignedBy = "TestSetup",
                    AssignedAt = DateTime.UtcNow
                });
            }

            var branches = await context.Branches.ToListAsync();
            foreach (var b in branches)
            {
                user.BranchAccesses.Add(new UserBranchAccess
                {
                    BranchId = b.Id,
                    IsDefault = b.Code == "SGN",
                    GrantedBy = "TestSetup",
                    GrantedAt = DateTime.UtcNow
                });
            }

            await context.UserAccounts.AddAsync(user);
            await context.SaveChangesAsync();
        }

        var loginResp = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(username, password));
        loginResp.EnsureSuccessStatusCode();
        var authResult = await loginResp.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        return authResult!.Data!.AccessToken;
    }

    [Fact]
    public async Task Warehouse_Zone_Location_Lifecycle_Succeeds()
    {
        var token = await GetTokenAsync("wh_manager", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        long branchId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            branchId = (await db.Branches.FirstAsync(b => b.Code == "SGN")).Id;
        }

        // 1. Create Warehouse
        var createWhReq = new CreateWarehouseRequest
        {
            BranchId = branchId,
            Code = "WH_SGN_CENTRAL",
            Name = "Saigon Central Hub & Warehouse",
            Type = "Hybrid",
            TotalAreaSqM = 12000m
        };

        var createWhRes = await _client.PostAsJsonAsync("/api/v1/warehouses", createWhReq);
        createWhRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdWh = await createWhRes.Content.ReadFromJsonAsync<ApiResponse<WarehouseDetailDto>>();
        createdWh!.Data!.Code.Should().Be("WH_SGN_CENTRAL");
        var warehouseId = createdWh.Data.Id;

        // 2. Read Warehouse
        var getWhRes = await _client.GetAsync($"/api/v1/warehouses/{warehouseId}");
        getWhRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. Update Warehouse
        var updateWhRes = await _client.PutAsJsonAsync($"/api/v1/warehouses/{warehouseId}", new UpdateWarehouseRequest
        {
            Name = "Saigon Central Hub & Warehouse (Updated)",
            Type = "Hybrid",
            TotalAreaSqM = 14000m
        });
        updateWhRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. Create Zone
        var createZoneReq = new CreateWarehouseZoneRequest
        {
            Code = "STORAGE_A",
            Name = "Main Storage Zone A",
            ZoneType = "BulkStorage",
            TemperatureType = "Ambient"
        };
        var createZoneRes = await _client.PostAsJsonAsync($"/api/v1/warehouses/{warehouseId}/zones", createZoneReq);
        createZoneRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdZone = await createZoneRes.Content.ReadFromJsonAsync<ApiResponse<WarehouseZoneDto>>();
        var zoneId = createdZone!.Data!.Id;

        // 5. Create Location
        var createLocReq = new CreateWarehouseLocationRequest
        {
            Code = "LOC-A-01",
            Barcode = "BC-LOC-A-01",
            Aisle = "A",
            Rack = "01",
            Shelf = "01",
            Bin = "01",
            MaxWeightKg = 2000m,
            MaxVolumeCbm = 8m
        };
        var createLocRes = await _client.PostAsJsonAsync($"/api/v1/warehouses/{warehouseId}/zones/{zoneId}/locations", createLocReq);
        createLocRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdLoc = await createLocRes.Content.ReadFromJsonAsync<ApiResponse<WarehouseLocationDto>>();
        var locId = createdLoc!.Data!.Id;

        // 6. Query Locations
        var getLocsRes = await _client.GetAsync($"/api/v1/warehouses/{warehouseId}/locations");
        getLocsRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var locs = await getLocsRes.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<WarehouseLocationDto>>>();
        locs!.Data!.Items.Should().Contain(l => l.Code == "LOC-A-01");

        // 7. Toggle Location Block
        var blockLocRes = await _client.PatchAsync($"/api/v1/warehouses/{warehouseId}/locations/{locId}/block?isBlocked=true", null);
        blockLocRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var blockedLoc = await blockLocRes.Content.ReadFromJsonAsync<ApiResponse<WarehouseLocationDto>>();
        blockedLoc!.Data!.IsBlocked.Should().BeTrue();
    }

    [Fact]
    public async Task Inventory_Receipt_Adjustment_Transfer_Reservation_Lifecycle_Succeeds()
    {
        var token = await GetTokenAsync("inv_manager", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        long warehouseId, productId, uomId, employeeId, locationId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var branch = await db.Branches.FirstAsync(b => b.Code == "SGN");
            var wh = new Domain.Entities.Organization.Warehouse
            {
                BranchId = branch.Id,
                Code = "WH_INV_TEST",
                Name = "Inventory Test Warehouse",
                Type = "InventoryWarehouse",
                IsActive = true
            };
            db.Warehouses.Add(wh);
            await db.SaveChangesAsync();

            var zone = new WarehouseZone
            {
                WarehouseId = wh.Id,
                Code = "ZONE_MAIN",
                Name = "Main Zone",
                ZoneType = "BulkStorage",
                TemperatureType = "Ambient",
                IsActive = true
            };
            db.WarehouseZones.Add(zone);
            await db.SaveChangesAsync();

            var loc = new WarehouseLocation
            {
                WarehouseZoneId = zone.Id,
                Code = "LOC-INV-01",
                Barcode = "BC-LOC-INV-01",
                MaxWeightKg = 1000m,
                MaxVolumeCbm = 5m
            };
            db.WarehouseLocations.Add(loc);
            await db.SaveChangesAsync();

            warehouseId = wh.Id;
            locationId = loc.Id;
            productId = (await db.Products.FirstAsync()).Id;
            uomId = (await db.UnitsOfMeasure.FirstAsync()).Id;
            employeeId = (await db.Employees.FirstAsync()).Id;
        }

        // 1. Create Inventory Item Settings
        var createItemReq = new CreateInventoryItemRequest
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            ReorderPoint = 10,
            SafetyStock = 5,
            TargetStock = 50,
            MinimumOrderQuantity = 5,
            MaximumStock = 200
        };
        var createItemRes = await _client.PostAsJsonAsync("/api/v1/inventory/items", createItemReq);
        createItemRes.StatusCode.Should().Be(HttpStatusCode.Created);

        // 2. Create and Confirm Warehouse Receipt (Inbound Inventory)
        var createReceiptReq = new CreateWarehouseReceiptRequest
        {
            WarehouseId = warehouseId,
            ReceiptType = "InventoryInbound",
            SourceVendor = "Samsung Electronics Vietnam",
            ReceivedByEmployeeId = employeeId,
            Items = new List<CreateWarehouseReceiptItemRequest>
            {
                new()
                {
                    ProductId = productId,
                    ExpectedQuantity = 100,
                    UnitOfMeasureId = uomId,
                    TargetLocationId = locationId,
                    LotNumber = "LOT-2026-A1"
                }
            }
        };

        var createReceiptRes = await _client.PostAsJsonAsync("/api/v1/inventory/receipts", createReceiptReq);
        createReceiptRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var receipt = (await createReceiptRes.Content.ReadFromJsonAsync<ApiResponse<WarehouseReceiptDto>>())!.Data!;
        var receiptId = receipt.Id;

        // Confirm Receipt
        var confirmReceiptRes = await _client.PostAsJsonAsync($"/api/v1/inventory/receipts/{receiptId}/confirm", new ConfirmReceiptRequest());
        confirmReceiptRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var confirmedReceipt = (await confirmReceiptRes.Content.ReadFromJsonAsync<ApiResponse<WarehouseReceiptDto>>())!.Data!;
        confirmedReceipt.Status.Should().Be("Completed");

        // 3. Verify Inventory Balance Increased
        var getBalancesRes = await _client.GetAsync($"/api/v1/inventory/balances?warehouseId={warehouseId}&productId={productId}");
        getBalancesRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var balances = (await getBalancesRes.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<InventoryBalanceDto>>>())?.Data?.Items;
        // Check using standard JSON deserialize
        var rawBalances = await _client.GetFromJsonAsync<ApiResponse<PagedResponse<InventoryBalanceDto>>>($"/api/v1/inventory/balances?warehouseId={warehouseId}&productId={productId}");
        rawBalances!.Data!.Items.Should().NotBeEmpty();
        rawBalances.Data.Items.First().QuantityOnHand.Should().Be(100);
        rawBalances.Data.Items.First().QuantityAvailable.Should().Be(100);

        // 4. Stock Adjustment
        var adjReq = new CreateStockAdjustmentRequest
        {
            WarehouseId = warehouseId,
            ProductId = productId,
            LocationId = locationId,
            ReasonCode = "CountingVariance",
            ReasonDescription = "Variance found during shift change",
            QuantityChange = -5
        };
        var createAdjRes = await _client.PostAsJsonAsync("/api/v1/inventory/adjustments", adjReq);
        createAdjRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var adj = (await createAdjRes.Content.ReadFromJsonAsync<ApiResponse<StockAdjustmentDto>>())!.Data!;

        // Approve Adjustment
        var approveAdjRes = await _client.PostAsync($"/api/v1/inventory/adjustments/{adj.Id}/approve?employeeId={employeeId}", null);
        approveAdjRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 5. Inventory Reservation
        long orderId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var customer = await db.Customers.FirstAsync();
            var currency = await db.Currencies.FirstAsync();
            var order = new Order
            {
                OrderNumber = "ORD-RES-TEST-01",
                CustomerId = customer.Id,
                BranchId = (await db.Branches.FirstAsync()).Id,
                CurrencyId = currency.Id,
                OrderStatus = "Draft"
            };
            db.Orders.Add(order);
            await db.SaveChangesAsync();
            orderId = order.Id;
        }

        var reserveReq = new CreateInventoryReservationRequest
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            LocationId = locationId,
            OrderId = orderId,
            ReservedQuantity = 10,
            DurationMinutes = 120
        };
        var reserveRes = await _client.PostAsJsonAsync("/api/v1/inventory/reservations", reserveReq);
        reserveRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var reservation = (await reserveRes.Content.ReadFromJsonAsync<ApiResponse<InventoryReservationDto>>())!.Data!;
        reservation.Status.Should().Be("Active");

        // Consume Reservation
        var consumeRes = await _client.PostAsync($"/api/v1/inventory/reservations/{reservation.Id}/consume", null);
        consumeRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var consumed = (await consumeRes.Content.ReadFromJsonAsync<ApiResponse<InventoryReservationDto>>())!.Data!;
        consumed.Status.Should().Be("Fulfilled");

        // 6. Lots and Serial Numbers
        var createLotRes = await _client.PostAsJsonAsync("/api/v1/inventory/lots", new CreateInventoryLotRequest
        {
            ProductId = productId,
            LotNumber = "LOT-2026-BATCH-01",
            ManufactureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1)),
            ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1))
        });
        createLotRes.StatusCode.Should().Be(HttpStatusCode.Created);

        var createSnRes = await _client.PostAsJsonAsync("/api/v1/inventory/serial-numbers", new CreateInventorySerialNumberRequest
        {
            ProductId = productId,
            SerialNumber = "SN-SP-2026-9999",
            WarehouseId = warehouseId,
            LocationId = locationId
        });
        createSnRes.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task ParcelHandling_Scan_Receive_Sort_Stage_Dispatch_Lifecycle_Succeeds()
    {
        var token = await GetTokenAsync("hub_operator", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        long warehouseId, zoneId, locationId, employeeId;
        string shipmentTracking, packageBarcode;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var branch = await db.Branches.FirstAsync(b => b.Code == "SGN");
            var customer = await db.Customers.FirstAsync();
            var currency = await db.Currencies.FirstAsync();

            var hub = new Domain.Entities.Organization.Warehouse
            {
                BranchId = branch.Id,
                Code = "HUB_SGN_01",
                Name = "Saigon Sorting Hub 01",
                Type = "ParcelHub",
                IsActive = true
            };
            db.Warehouses.Add(hub);
            await db.SaveChangesAsync();

            var sortZone = new WarehouseZone
            {
                WarehouseId = hub.Id,
                Code = "SORT_HAN",
                Name = "Sorting Lane Hanoi",
                ZoneType = "ParcelSorting",
                TemperatureType = "Ambient",
                IsActive = true
            };
            db.WarehouseZones.Add(sortZone);
            await db.SaveChangesAsync();

            var stageLoc = new WarehouseLocation
            {
                WarehouseZoneId = sortZone.Id,
                Code = "BAY-HAN-01",
                Barcode = "BC-BAY-HAN-01",
                MaxWeightKg = 5000m,
                MaxVolumeCbm = 20m
            };
            db.WarehouseLocations.Add(stageLoc);
            await db.SaveChangesAsync();

            warehouseId = hub.Id;
            zoneId = sortZone.Id;
            locationId = stageLoc.Id;
            employeeId = (await db.Employees.FirstAsync()).Id;

            // Create Shipment and Package
            var shipment = new Shipment
            {
                TrackingNumber = "SHIP-HUB-PARCEL-01",
                CustomerId = customer.Id,
                OriginBranchId = branch.Id,
                DestinationBranchId = branch.Id,
                ShipmentStatus = "PickedUp",
                ServiceType = "StandardGround",
                CurrencyId = currency.Id,
                ActualWeightKg = 2.5m,
                ChargeableWeightKg = 2.5m
            };
            db.Shipments.Add(shipment);
            await db.SaveChangesAsync();

            var pkg = new ShipmentPackage
            {
                ShipmentId = shipment.Id,
                PackageCode = "PKG-HUB-001",
                Barcode = "BC-PARCEL-HUB-001",
                WeightKg = 2.5m,
                LengthCm = 30m,
                WidthCm = 20m,
                HeightCm = 15m,
                PackageStatus = "Created"
            };
            db.ShipmentPackages.Add(pkg);
            await db.SaveChangesAsync();

            shipmentTracking = shipment.TrackingNumber;
            packageBarcode = pkg.Barcode;
        }

        // 1. Scan Package at Inbound Dock
        var scanRes = await _client.PostAsJsonAsync("/api/v1/parcel-handling/scan", new ScanPackageRequest
        {
            TrackingNumberOrBarcode = packageBarcode,
            WarehouseId = warehouseId,
            EmployeeId = employeeId
        });
        scanRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var scanResult = (await scanRes.Content.ReadFromJsonAsync<ApiResponse<ScanPackageResultDto>>())!.Data!;
        scanResult.PermittedNextOperation.Should().Be("Receive");

        // 2. Receive Parcel
        var receiveRes = await _client.PostAsJsonAsync("/api/v1/parcel-handling/receive", new ParcelReceiveRequest
        {
            TrackingNumberOrBarcode = packageBarcode,
            WarehouseId = warehouseId,
            EmployeeId = employeeId,
            Notes = "Arrived via feeder truck 51C-12345"
        });
        receiveRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var receivedEvt = (await receiveRes.Content.ReadFromJsonAsync<ApiResponse<ParcelHandlingEventDto>>())!.Data!;
        receivedEvt.EventType.Should().Be("Unloaded");

        // 3. Sort Parcel into Destination Zone
        var sortRes = await _client.PostAsJsonAsync("/api/v1/parcel-handling/sort", new ParcelSortRequest
        {
            TrackingNumberOrBarcode = packageBarcode,
            WarehouseId = warehouseId,
            ZoneId = zoneId,
            EmployeeId = employeeId,
            TargetContainerCode = "BAG-HAN-001"
        });
        sortRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var sortedEvt = (await sortRes.Content.ReadFromJsonAsync<ApiResponse<ParcelHandlingEventDto>>())!.Data!;
        sortedEvt.EventType.Should().Be("Sorted");

        // 4. Stage Parcel into Staging Bay
        var stageRes = await _client.PostAsJsonAsync("/api/v1/parcel-handling/stage", new ParcelStageRequest
        {
            TrackingNumberOrBarcode = packageBarcode,
            WarehouseId = warehouseId,
            LocationId = locationId,
            EmployeeId = employeeId,
            BagNumber = "BAG-HAN-001"
        });
        stageRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var stagedEvt = (await stageRes.Content.ReadFromJsonAsync<ApiResponse<ParcelHandlingEventDto>>())!.Data!;
        stagedEvt.EventType.Should().Be("Staged");

        // 5. Create & Confirm Warehouse Dispatch
        long pkgId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            pkgId = (await db.ShipmentPackages.FirstAsync(p => p.Barcode == packageBarcode)).Id;
        }

        var createDispatchRes = await _client.PostAsJsonAsync("/api/v1/warehouse-dispatches", new CreateWarehouseDispatchDto
        {
            WarehouseId = warehouseId,
            DispatchType = "HubTransfer",
            DispatchedByEmployeeId = employeeId,
            VehicleNumber = "29B-98765",
            Packages = new List<CreateWarehouseDispatchPackageRequest>
            {
                new() { ShipmentPackageId = pkgId, TargetContainerCode = "BAG-HAN-001" }
            }
        });
        createDispatchRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var dispatch = (await createDispatchRes.Content.ReadFromJsonAsync<ApiResponse<WarehouseDispatchDto>>())!.Data!;

        // Confirm Dispatch
        var confirmDispatchRes = await _client.PostAsync($"/api/v1/warehouse-dispatches/{dispatch.Id}/confirm?employeeId={employeeId}", null);
        confirmDispatchRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var confirmedDispatch = (await confirmDispatchRes.Content.ReadFromJsonAsync<ApiResponse<WarehouseDispatchDto>>())!.Data!;
        confirmedDispatch.Status.Should().Be("Dispatched");

        // 6. Query Events
        var eventsRes = await _client.GetAsync($"/api/v1/parcel-handling/events?warehouseId={warehouseId}");
        eventsRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var events = (await eventsRes.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<ParcelHandlingEventDto>>>())!.Data!;
        events.Items.Should().Contain(e => e.EventType == "Dispatched");
    }
}
