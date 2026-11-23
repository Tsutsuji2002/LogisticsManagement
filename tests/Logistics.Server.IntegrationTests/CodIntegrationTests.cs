using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Logistics.Server.Application.Common.Interfaces;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Cod.DTOs;
using Logistics.Server.Application.Modules.Identity.DTOs;
using Logistics.Server.Domain.Entities.Configuration;
using Logistics.Server.Domain.Entities.Crm;
using Logistics.Server.Domain.Entities.Finance;
using Logistics.Server.Domain.Entities.Identity;
using Logistics.Server.Domain.Entities.Organization;
using Logistics.Server.Domain.Entities.Shipping;
using Logistics.Server.Infrastructure.Persistence;
using Logistics.Shared.Contracts;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Logistics.Server.IntegrationTests;

public class CodIntegrationTests : IClassFixture<WebApplicationFactory<Logistics.Server.API.Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Logistics.Server.API.Program> _factory;
    private readonly string _dbName;

    public CodIntegrationTests(WebApplicationFactory<Logistics.Server.API.Program> factory)
    {
        _dbName = "CodTestDb_" + Guid.NewGuid().ToString("N");

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
                Code = "LOGICOD",
                LegalName = "Logistics COD Corporation",
                TradingName = "LogiCOD",
                TaxNumber = "0109998887",
                IsActive = true
            };
            await context.Companies.AddAsync(company);
            await context.SaveChangesAsync();

            var branch1 = new Branch
            {
                CompanyId = company.Id,
                Code = "SGN",
                Name = "Saigon Hub",
                BranchType = "Hub",
                Phone = "0281112223",
                IsActive = true
            };
            var branch2 = new Branch
            {
                CompanyId = company.Id,
                Code = "HAN",
                Name = "Hanoi Hub",
                BranchType = "Hub",
                Phone = "0241112223",
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

            var customer = new Customer
            {
                BranchId = branch1.Id,
                CustomerCode = "CUST-COD-001",
                CustomerType = "Corporate",
                Name = "Tech Retailer Vietnam",
                Email = "cod@techretailer.vn",
                PhoneNumber = "0912345678",
                CreditLimit = 100000000m,
                IsActive = true
            };
            await context.Customers.AddAsync(customer);
            await context.SaveChangesAsync();

            var balance = new CustomerBalance
            {
                CustomerId = customer.Id,
                CurrentOutstandingBalance = 0m,
                TotalCodHoldingAmount = 0m,
                UnbilledChargesAmount = 0m,
                LastUpdatedDate = DateTime.UtcNow
            };
            await context.CustomerBalances.AddAsync(balance);

            var driverEmp = new Employee
            {
                BranchId = branch1.Id,
                Code = "EMP-DRV-001",
                FirstName = "Van",
                LastName = "Driver",
                FullName = "Driver Van",
                Email = "driver.van@logistics.test",
                PhoneNumber = "0901234567",
                JobTitle = "Delivery Driver",
                HireDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
                Status = "Active"
            };
            var cashierEmp = new Employee
            {
                BranchId = branch1.Id,
                Code = "EMP-CSH-001",
                FirstName = "Thi",
                LastName = "Cashier",
                FullName = "Cashier Thi",
                Email = "cashier.thi@logistics.test",
                PhoneNumber = "0909876543",
                JobTitle = "Branch Cashier",
                HireDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-2)),
                Status = "Active"
            };
            await context.Employees.AddRangeAsync(driverEmp, cashierEmp);
            await context.SaveChangesAsync();

            var shipment1 = new Shipment
            {
                TrackingNumber = "VN-COD-10001",
                CustomerId = customer.Id,
                OriginBranchId = branch1.Id,
                DestinationBranchId = branch1.Id,
                ShipmentStatus = "OutForDelivery",
                ServiceType = "StandardGround",
                CodAmount = 450000m,
                ShippingFee = 35000m,
                TotalAmount = 485000m,
                CurrencyId = currency.Id
            };
            var shipment2 = new Shipment
            {
                TrackingNumber = "VN-COD-10002",
                CustomerId = customer.Id,
                OriginBranchId = branch1.Id,
                DestinationBranchId = branch1.Id,
                ShipmentStatus = "OutForDelivery",
                ServiceType = "StandardGround",
                CodAmount = 850000m,
                ShippingFee = 45000m,
                TotalAmount = 895000m,
                CurrencyId = currency.Id
            };
            await context.Shipments.AddRangeAsync(shipment1, shipment2);
            await context.SaveChangesAsync();
        }
    }

    private async Task<string> GetTokenAsync(string username, string roleCode, long? employeeId = null, long? restrictBranchId = null)
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
                EmployeeId = employeeId,
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

            if (restrictBranchId.HasValue)
            {
                user.BranchAccesses.Add(new UserBranchAccess
                {
                    BranchId = restrictBranchId.Value,
                    IsDefault = true,
                    GrantedBy = "TestSetup",
                    GrantedAt = DateTime.UtcNow
                });
            }
            else
            {
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
            }

            await context.UserAccounts.AddAsync(user);
            await context.SaveChangesAsync();
        }

        var loginRes = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(username, password));

        loginRes.EnsureSuccessStatusCode();
        var authResult = await loginRes.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        return authResult!.Data!.AccessToken;
    }

    // ═══════════════════════════════════════════════════════════════
    //  1. COD Collection Create & Get Tests
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task CodCollection_Create_And_GetById_Succeeds()
    {
        var token = await GetTokenAsync("admin_user_cod", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var shipment = await context.Shipments.FirstAsync(s => s.TrackingNumber == "VN-COD-10001");
        var driver = await context.Employees.FirstAsync(e => e.Code == "EMP-DRV-001");

        var createReq = new CreateCodCollectionRequest
        {
            ShipmentId = shipment.Id,
            CollectedAmount = 450000m,
            CollectedByEmployeeId = driver.Id,
            DriverReceiptNumber = "REC-DRV-001",
            Notes = "Full cash collected upon delivery"
        };

        var postRes = await _client.PostAsJsonAsync("/api/v1/cod/collections", createReq);
        postRes.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = (await postRes.Content.ReadFromJsonAsync<ApiResponse<CodCollectionDto>>())!.Data!;
        created.ShipmentId.Should().Be(shipment.Id);
        created.CollectedAmount.Should().Be(450000m);
        created.Status.Should().Be("CollectedByDriver");

        // Get by ID
        var getRes = await _client.GetAsync($"/api/v1/cod/collections/{created.Id}");
        getRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = (await getRes.Content.ReadFromJsonAsync<ApiResponse<CodCollectionDto>>())!.Data!;
        fetched.Id.Should().Be(created.Id);

        // Get by Shipment ID
        var getByShipmentRes = await _client.GetAsync($"/api/v1/cod/collections/shipments/{shipment.Id}");
        getByShipmentRes.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ═══════════════════════════════════════════════════════════════
    //  2. COD Collection Lifecycle (Collect -> Deposit -> Verify)
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task CodCollection_Lifecycle_Collect_Deposit_Verify_Succeeds()
    {
        var token = await GetTokenAsync("admin_lifecycle_cod", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var currency = await context.Currencies.FirstAsync();
            var customer = await context.Customers.FirstAsync();
            var branch = await context.Branches.FirstAsync();
            var driver = await context.Employees.FirstAsync(e => e.Code == "EMP-DRV-001");

            var shipment = new Shipment
            {
                TrackingNumber = "VN-COD-LIFECYCLE-1",
                CustomerId = customer.Id,
                OriginBranchId = branch.Id,
                DestinationBranchId = branch.Id,
                ShipmentStatus = "Delivered",
                ServiceType = "StandardGround",
                CodAmount = 600000m,
                ShippingFee = 40000m,
                TotalAmount = 640000m,
                CurrencyId = currency.Id
            };
            await context.Shipments.AddAsync(shipment);
            await context.SaveChangesAsync();

            var collection = new CODCollection
            {
                ShipmentId = shipment.Id,
                CollectedAmount = 600000m,
                CurrencyId = currency.Id,
                CollectionDate = DateTime.UtcNow,
                CollectedByEmployeeId = driver.Id,
                Status = "CollectedByDriver"
            };
            await context.CODCollections.AddAsync(collection);
            await context.SaveChangesAsync();
        }

        // Fetch ID
        long colId;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            colId = (await context.CODCollections.FirstAsync(c => c.Shipment.TrackingNumber == "VN-COD-LIFECYCLE-1")).Id;
        }

        // 1. Deposit to branch
        var depRes = await _client.PostAsJsonAsync($"/api/v1/cod/collections/{colId}/deposit", new { Notes = "Deposited at Saigon hub counter" });
        depRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var depData = (await depRes.Content.ReadFromJsonAsync<ApiResponse<CodCollectionDto>>())!.Data!;
        depData.Status.Should().Be("DepositedToBranch");

        // 2. Cashier verifies
        var verRes = await _client.PostAsJsonAsync($"/api/v1/cod/collections/{colId}/verify", new VerifyCodCollectionRequest
        {
            Notes = "Verified cash in cashbox"
        });
        verRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var verData = (await verRes.Content.ReadFromJsonAsync<ApiResponse<CodCollectionDto>>())!.Data!;
        verData.Status.Should().Be("VerifiedByCashier");
        verData.VerifiedByUserId.Should().NotBeNull();

        // 3. Verify CustomerBalance TotalCodHoldingAmount was updated
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var customer = await context.Customers.FirstAsync();
            var bal = await context.CustomerBalances.FirstOrDefaultAsync(b => b.CustomerId == customer.Id);
            bal.Should().NotBeNull();
            bal!.TotalCodHoldingAmount.Should().BeGreaterOrEqualTo(600000m);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  3. Record COD Collection Failure
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task CodCollection_RecordFailure_Succeeds()
    {
        var token = await GetTokenAsync("admin_failure_cod", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        long colId;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var currency = await context.Currencies.FirstAsync();
            var customer = await context.Customers.FirstAsync();
            var branch = await context.Branches.FirstAsync();
            var driver = await context.Employees.FirstAsync(e => e.Code == "EMP-DRV-001");

            var shipment = new Shipment
            {
                TrackingNumber = "VN-COD-FAIL-1",
                CustomerId = customer.Id,
                OriginBranchId = branch.Id,
                DestinationBranchId = branch.Id,
                ShipmentStatus = "OutForDelivery",
                ServiceType = "StandardGround",
                CodAmount = 300000m,
                ShippingFee = 30000m,
                TotalAmount = 330000m,
                CurrencyId = currency.Id
            };
            await context.Shipments.AddAsync(shipment);
            await context.SaveChangesAsync();

            var collection = new CODCollection
            {
                ShipmentId = shipment.Id,
                CollectedAmount = 300000m,
                CurrencyId = currency.Id,
                CollectionDate = DateTime.UtcNow,
                CollectedByEmployeeId = driver.Id,
                Status = "CollectedByDriver"
            };
            await context.CODCollections.AddAsync(collection);
            await context.SaveChangesAsync();
            colId = collection.Id;
        }

        var failReq = new RecordCodFailureRequest
        {
            ReasonCode = "CustomerRefusedPayment",
            Notes = "Consignee claimed they cancelled the purchase order"
        };
        var res = await _client.PostAsJsonAsync($"/api/v1/cod/collections/{colId}/record-failure", failReq);
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ═══════════════════════════════════════════════════════════════
    //  4. COD Remittance Full Flow (Create, Add Item, Confirm)
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task CodRemittance_FullLifecycle_Create_Add_Confirm_Succeeds()
    {
        var token = await GetTokenAsync("admin_remit_cod", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        long customerId;
        long currencyId;
        long col1Id;
        long col2Id;

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var currency = await context.Currencies.FirstAsync();
            var customer = await context.Customers.FirstAsync();
            var branch = await context.Branches.FirstAsync();
            var driver = await context.Employees.FirstAsync(e => e.Code == "EMP-DRV-001");
            var adminUser = await context.UserAccounts.FirstAsync();

            customerId = customer.Id;
            currencyId = currency.Id;

            var shipmentA = new Shipment
            {
                TrackingNumber = "VN-COD-REMIT-A",
                CustomerId = customer.Id,
                OriginBranchId = branch.Id,
                DestinationBranchId = branch.Id,
                ShipmentStatus = "Delivered",
                ServiceType = "StandardGround",
                CodAmount = 500000m,
                CurrencyId = currency.Id
            };
            var shipmentB = new Shipment
            {
                TrackingNumber = "VN-COD-REMIT-B",
                CustomerId = customer.Id,
                OriginBranchId = branch.Id,
                DestinationBranchId = branch.Id,
                ShipmentStatus = "Delivered",
                ServiceType = "StandardGround",
                CodAmount = 700000m,
                CurrencyId = currency.Id
            };
            await context.Shipments.AddRangeAsync(shipmentA, shipmentB);
            await context.SaveChangesAsync();

            var colA = new CODCollection
            {
                ShipmentId = shipmentA.Id,
                CollectedAmount = 500000m,
                CurrencyId = currency.Id,
                CollectionDate = DateTime.UtcNow,
                CollectedByEmployeeId = driver.Id,
                Status = "VerifiedByCashier",
                VerifiedByUserId = adminUser.Id,
                VerifiedAt = DateTime.UtcNow
            };
            var colB = new CODCollection
            {
                ShipmentId = shipmentB.Id,
                CollectedAmount = 700000m,
                CurrencyId = currency.Id,
                CollectionDate = DateTime.UtcNow,
                CollectedByEmployeeId = driver.Id,
                Status = "VerifiedByCashier",
                VerifiedByUserId = adminUser.Id,
                VerifiedAt = DateTime.UtcNow
            };
            await context.CODCollections.AddRangeAsync(colA, colB);

            // Add holding to customer balance
            var balance = await context.CustomerBalances.FirstAsync(b => b.CustomerId == customer.Id);
            balance.TotalCodHoldingAmount += 1200000m;

            await context.SaveChangesAsync();
            col1Id = colA.Id;
            col2Id = colB.Id;
        }

        // 1. Create Remittance with col1
        var createReq = new CreateCodRemittanceRequest
        {
            CustomerId = customerId,
            CurrencyId = currencyId,
            PaymentMethod = "BankTransfer",
            BankName = "Vietcombank",
            BankAccount = "9876543210",
            AccountHolderName = "Tech Retailer Vietnam",
            CollectionIds = [col1Id],
            ServiceFeesDeducted = 25000m,
            Notes = "Weekly COD Settlement Batch"
        };

        var postRes = await _client.PostAsJsonAsync("/api/v1/cod/remittances", createReq);
        postRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var remit = (await postRes.Content.ReadFromJsonAsync<ApiResponse<CodRemittanceDto>>())!.Data!;
        remit.Status.Should().Be("Draft");
        remit.TotalCodAmount.Should().Be(500000m);
        remit.NetRemittanceAmount.Should().Be(475000m);
        remit.ItemsCount.Should().Be(1);

        // 2. Add col2 item
        var addItemReq = new AddCodRemittanceItemRequest
        {
            CODCollectionId = col2Id,
            DeductedShippingFee = 35000m
        };
        var addRes = await _client.PostAsJsonAsync($"/api/v1/cod/remittances/{remit.Id}/items", addItemReq);
        addRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = (await addRes.Content.ReadFromJsonAsync<ApiResponse<CodRemittanceDto>>())!.Data!;
        updated.ItemsCount.Should().Be(2);
        updated.TotalCodAmount.Should().Be(1200000m);
        updated.TotalServiceFeesDeducted.Should().Be(60000m);
        updated.NetRemittanceAmount.Should().Be(1140000m);

        // 3. Confirm Remittance
        var confReq = new ConfirmCodRemittanceRequest
        {
            PaymentReference = "CITI-TRF-20261123-001",
            ActualRemittedAmount = 1140000m,
            Notes = "Wire transfer successful"
        };
        var confRes = await _client.PostAsJsonAsync($"/api/v1/cod/remittances/{remit.Id}/confirm", confReq);
        confRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var completed = (await confRes.Content.ReadFromJsonAsync<ApiResponse<CodRemittanceDto>>())!.Data!;
        completed.Status.Should().Be("Completed");

        // 4. Check DB: Collections should now have status RemittedToCustomer
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var colA = await context.CODCollections.FindAsync(col1Id);
            var colB = await context.CODCollections.FindAsync(col2Id);
            colA!.Status.Should().Be("RemittedToCustomer");
            colB!.Status.Should().Be("RemittedToCustomer");
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  5. Cancel Remittance Restores Collections
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task CodRemittance_Cancel_RestoresCollections_Succeeds()
    {
        var token = await GetTokenAsync("admin_cancel_cod", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        long remitId;
        long colId;

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var currency = await context.Currencies.FirstAsync();
            var customer = await context.Customers.FirstAsync();
            var branch = await context.Branches.FirstAsync();
            var driver = await context.Employees.FirstAsync(e => e.Code == "EMP-DRV-001");
            var adminUser = await context.UserAccounts.FirstAsync();

            var shipment = new Shipment
            {
                TrackingNumber = "VN-COD-CANCEL-1",
                CustomerId = customer.Id,
                OriginBranchId = branch.Id,
                DestinationBranchId = branch.Id,
                ShipmentStatus = "Delivered",
                ServiceType = "StandardGround",
                CodAmount = 400000m,
                CurrencyId = currency.Id
            };
            await context.Shipments.AddAsync(shipment);
            await context.SaveChangesAsync();

            var col = new CODCollection
            {
                ShipmentId = shipment.Id,
                CollectedAmount = 400000m,
                CurrencyId = currency.Id,
                CollectionDate = DateTime.UtcNow,
                CollectedByEmployeeId = driver.Id,
                Status = "VerifiedByCashier",
                VerifiedByUserId = adminUser.Id,
                VerifiedAt = DateTime.UtcNow
            };
            await context.CODCollections.AddAsync(col);
            await context.SaveChangesAsync();
            colId = col.Id;

            var remittance = new CODRemittance
            {
                RemittanceNumber = "REM-TEST-CANCEL",
                CustomerId = customer.Id,
                CurrencyId = currency.Id,
                TotalCodAmount = 400000m,
                TotalServiceFeesDeducted = 0m,
                NetRemittanceAmount = 400000m,
                PaymentMethod = "BankTransfer",
                Status = "Draft",
                RemittedByUserId = adminUser.Id
            };
            await context.CODRemittances.AddAsync(remittance);
            await context.SaveChangesAsync();

            var item = new CODRemittanceItem
            {
                CODRemittanceId = remittance.Id,
                CODCollectionId = col.Id,
                CodAmount = 400000m,
                DeductedShippingFee = 0m,
                NetAmount = 400000m
            };
            await context.CODRemittanceItems.AddAsync(item);
            col.Status = "RemittedToCustomer";
            await context.SaveChangesAsync();
            remitId = remittance.Id;
        }

        // Cancel the remittance
        var cancelReq = new CancelCodRemittanceRequest { Reason = "Mistaken customer profile selected" };
        var cancelRes = await _client.PostAsJsonAsync($"/api/v1/cod/remittances/{remitId}/cancel", cancelReq);
        cancelRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify collection returned to VerifiedByCashier
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var col = await context.CODCollections.FindAsync(colId);
            col!.Status.Should().Be("VerifiedByCashier");

            var r = await context.CODRemittances.FindAsync(remitId);
            r!.Status.Should().Be("Cancelled");
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  6. Driver Operations & Summary
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task CodOperations_Driver_MyCollections_And_Summary_Succeeds()
    {
        await SeedBaseDataAsync();

        long driverEmpId;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var driver = await context.Employees.FirstAsync(e => e.Code == "EMP-DRV-001");
            driverEmpId = driver.Id;
        }

        var driverToken = await GetTokenAsync("test_driver_cod", AppRoles.WarehouseStaff, employeeId: driverEmpId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", driverToken);

        // 1. Get my collections
        var myColRes = await _client.GetAsync("/api/v1/cod/operations/my-collections");
        myColRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 2. Get my summary
        var mySumRes = await _client.GetAsync("/api/v1/cod/operations/my-summary");
        mySumRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var sum = (await mySumRes.Content.ReadFromJsonAsync<ApiResponse<EmployeeCodSummaryDto>>())!.Data!;
        sum.EmployeeId.Should().Be(driverEmpId);
    }

    // ═══════════════════════════════════════════════════════════════
    //  7. COD Reconciliation Overview & Employee Reconciliation
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task CodReconciliation_Summary_And_Employee_Reconciliation_Succeeds()
    {
        var token = await GetTokenAsync("admin_reconcile_cod", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        long driverEmpId;
        long branchId;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var driver = await context.Employees.FirstAsync(e => e.Code == "EMP-DRV-001");
            var branch = await context.Branches.FirstAsync(b => b.Code == "SGN");
            driverEmpId = driver.Id;
            branchId = branch.Id;
        }

        // Summary
        var sumRes = await _client.GetAsync("/api/v1/cod/reconciliation/summary");
        sumRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var summary = (await sumRes.Content.ReadFromJsonAsync<ApiResponse<CodReconciliationSummaryDto>>())!.Data!;
        summary.Should().NotBeNull();

        // Employee reconciliation
        var empRes = await _client.GetAsync($"/api/v1/cod/reconciliation/employees/{driverEmpId}");
        empRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var empRec = (await empRes.Content.ReadFromJsonAsync<ApiResponse<EmployeeCodReconciliationDto>>())!.Data!;
        empRec.EmployeeId.Should().Be(driverEmpId);

        // Branch reconciliation
        var branchRes = await _client.GetAsync($"/api/v1/cod/reconciliation/branches/{branchId}");
        branchRes.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ═══════════════════════════════════════════════════════════════
    //  8. Cross-Branch Security Enforcement
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public async Task Cod_BranchAccess_Security_Enforces_CrossBranch_Restriction()
    {
        await SeedBaseDataAsync();

        long hanBranchId;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var hanBranch = await context.Branches.FirstAsync(b => b.Code == "HAN");
            hanBranchId = hanBranch.Id;
        }

        // Token restricted to HAN branch only
        var branchToken = await GetTokenAsync("han_only_manager", AppRoles.BranchManager, restrictBranchId: hanBranchId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", branchToken);

        // Attempting to reconcile SGN branch should return 403 Forbidden
        long sgnBranchId;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var sgnBranch = await context.Branches.FirstAsync(b => b.Code == "SGN");
            sgnBranchId = sgnBranch.Id;
        }

        var res = await _client.GetAsync($"/api/v1/cod/reconciliation/branches/{sgnBranchId}");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
