using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Logistics.Server.Application.Common.Interfaces;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Identity.DTOs;
using Logistics.Server.Application.Modules.Sales.DTOs;
using Logistics.Server.Domain.Entities.Catalog;
using Logistics.Server.Domain.Entities.Configuration;
using Logistics.Server.Domain.Entities.Crm;
using Logistics.Server.Domain.Entities.Identity;
using Logistics.Server.Domain.Entities.Organization;
using Logistics.Server.Infrastructure.Persistence;
using Logistics.Shared.Contracts;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Logistics.Server.IntegrationTests;

public class SalesIntegrationTests : IClassFixture<WebApplicationFactory<Logistics.Server.API.Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Logistics.Server.API.Program> _factory;
    private readonly string _dbName;

    public SalesIntegrationTests(WebApplicationFactory<Logistics.Server.API.Program> factory)
    {
        _dbName = "SalesTestDb_" + Guid.NewGuid().ToString("N");

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

            var branch = new Branch
            {
                CompanyId = company.Id,
                Code = "SGN",
                Name = "Saigon Central Branch",
                BranchType = "Hub",
                Phone = "0281234567",
                IsActive = true
            };
            await context.Branches.AddAsync(branch);
            await context.SaveChangesAsync();

            var customer = new Customer
            {
                BranchId = branch.Id,
                CustomerCode = "CUST-SGN-001",
                CustomerType = "Corporate",
                Name = "Vinamilk Logistics Hub",
                Email = "logistics@vinamilk.vn",
                PhoneNumber = "0287654321",
                IsActive = true
            };
            await context.Customers.AddAsync(customer);
            await context.SaveChangesAsync();
        }

        if (!await context.UnitsOfMeasure.AnyAsync())
        {
            await context.UnitsOfMeasure.AddRangeAsync(
                new UnitOfMeasure { Code = "KG", Name = "Kilogram", Type = "Weight", ConversionFactor = 1.0m, IsBaseUnit = true, IsActive = true },
                new UnitOfMeasure { Code = "PCS", Name = "Pieces", Type = "Count", ConversionFactor = 1.0m, IsBaseUnit = true, IsActive = true }
            );
            await context.SaveChangesAsync();
        }

        if (!await context.Currencies.AnyAsync())
        {
            await context.Currencies.AddAsync(new Currency
            {
                Code = "VND",
                Name = "Vietnamese Dong",
                Symbol = "₫",
                DecimalPlaces = 0,
                ExchangeRateToDefault = 1.0m,
                IsDefault = true,
                IsActive = true
            });
            await context.SaveChangesAsync();
        }

        if (!await context.Countries.AnyAsync())
        {
            await context.Countries.AddAsync(new Country
            {
                Code = "VN",
                Name = "Vietnam",
                IsActive = true
            });
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
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
        {
            var role = await context.Roles.FirstOrDefaultAsync(r => r.Code == roleCode);
            user = new UserAccount
            {
                Username = username,
                Email = $"{username}@logistics.vn",
                PasswordHash = hasher.HashPassword(password),
                SecurityStamp = Guid.NewGuid().ToString("N"),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            if (role != null)
            {
                user.UserRoles.Add(new UserRole { RoleId = role.Id, AssignedAt = DateTime.UtcNow, AssignedBy = "Setup" });
            }

            var branch = await context.Branches.FirstOrDefaultAsync();
            if (branch != null)
            {
                user.BranchAccesses.Add(new UserBranchAccess { BranchId = branch.Id, IsDefault = true, GrantedAt = DateTime.UtcNow });
            }

            await context.UserAccounts.AddAsync(user);
            await context.SaveChangesAsync();
        }

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(username, password));
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        return body!.Data!.AccessToken;
    }

    [Fact]
    public async Task CreateOrder_WithAddressesAndItemsAndPackages_Succeeds()
    {
        var token = await GetTokenAsync("sales_admin", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var customer = await context.Customers.FirstAsync();
        var branch = await context.Branches.FirstAsync();

        var request = new CreateOrderRequest
        {
            CustomerId = customer.Id,
            BranchId = branch.Id,
            CodAmount = 500000m,
            Notes = "Test order creation",
            RequestedPickupDate = DateTime.UtcNow.AddDays(1),
            RequiredDeliveryDate = DateTime.UtcNow.AddDays(3),
            PickupAddress = new OrderAddressRequest
            {
                ContactName = "Warehouse Sender",
                PhoneNumber = "0901112222",
                AddressLine1 = "100 Nguyen Van Linh",
                AddressLine2 = "Gate 2",
                FormattedAddress = "100 Nguyen Van Linh, Gate 2, District 7"
            },
            DeliveryAddress = new OrderAddressRequest
            {
                ContactName = "Consignee Recipient",
                PhoneNumber = "0903334444",
                AddressLine1 = "200 Hai Ba Trung",
                FormattedAddress = "200 Hai Ba Trung, District 1"
            },
            Items = new List<CreateOrderItemRequest>
            {
                new CreateOrderItemRequest
                {
                    ProductName = "Carton Box Small",
                    Quantity = 5,
                    UnitPrice = 20000m,
                    WeightKg = 0.5m
                }
            },
            Packages = new List<CreateOrderPackageRequest>
            {
                new CreateOrderPackageRequest
                {
                    WeightKg = 2.5m,
                    LengthCm = 30,
                    WidthCm = 20,
                    HeightCm = 15,
                    DeclaredValue = 100000m,
                    Description = "Electronic Accessories"
                }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/v1/orders", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<OrderDetailDto>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
        body.Data!.OrderNumber.Should().StartWith("ORD-");
        body.Data.OrderStatus.Should().Be("Draft");
        body.Data.TotalWeightKg.Should().Be(2.5m);
        body.Data.Items.Should().HaveCount(1);
        body.Data.Packages.Should().HaveCount(1);
        body.Data.Addresses.Should().HaveCount(2);
        body.Data.PickupAddress.Should().NotBeNull();
        body.Data.DeliveryAddress.Should().NotBeNull();
    }

    [Fact]
    public async Task OrderLifecycle_DraftToConfirmedToCancelled_BehavesCorrectly()
    {
        var token = await GetTokenAsync("sales_manager", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var customer = await context.Customers.FirstAsync();

        // 1. Create order
        var createReq = new CreateOrderRequest
        {
            CustomerId = customer.Id,
            Notes = "Lifecycle Test Order"
        };
        var createRes = await _client.PostAsJsonAsync("/api/v1/orders", createReq);
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = (await createRes.Content.ReadFromJsonAsync<ApiResponse<OrderDetailDto>>())!.Data!;

        // 2. Add an item
        var itemReq = new CreateOrderItemRequest
        {
            ProductName = "Test Product A",
            Quantity = 3,
            UnitPrice = 50000m,
            WeightKg = 1.0m
        };
        var addItemRes = await _client.PostAsJsonAsync($"/api/v1/orders/{order.Id}/items", itemReq);
        addItemRes.StatusCode.Should().Be(HttpStatusCode.Created);

        // 3. Confirm order
        var confirmRes = await _client.PostAsync($"/api/v1/orders/{order.Id}/confirm", null);
        confirmRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var confirmedOrder = (await confirmRes.Content.ReadFromJsonAsync<ApiResponse<OrderDetailDto>>())!.Data!;
        confirmedOrder.OrderStatus.Should().Be("Confirmed");

        // 4. Try adding item to confirmed order -> should return 409 Conflict
        var addAfterConfirmRes = await _client.PostAsJsonAsync($"/api/v1/orders/{order.Id}/items", itemReq);
        addAfterConfirmRes.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // 5. Cancel order
        var cancelReq = new CancelOrderRequest { Reason = "Client requested cancellation" };
        var cancelRes = await _client.PostAsJsonAsync($"/api/v1/orders/{order.Id}/cancel", cancelReq);
        cancelRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var cancelledOrder = (await cancelRes.Content.ReadFromJsonAsync<ApiResponse<OrderDetailDto>>())!.Data!;
        cancelledOrder.OrderStatus.Should().Be("Cancelled");
        cancelledOrder.CancellationReason.Should().Be("Client requested cancellation");

        // 6. Try cancelling again -> should return 409 Conflict
        var cancelAgainRes = await _client.PostAsJsonAsync($"/api/v1/orders/{order.Id}/cancel", cancelReq);
        cancelAgainRes.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task OrderPackages_AddUpdateRemove_RecalculatesTotals()
    {
        var token = await GetTokenAsync("sales_packages_tester", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var customer = await context.Customers.FirstAsync();

        // 1. Create order
        var createReq = new CreateOrderRequest { CustomerId = customer.Id };
        var createRes = await _client.PostAsJsonAsync("/api/v1/orders", createReq);
        var order = (await createRes.Content.ReadFromJsonAsync<ApiResponse<OrderDetailDto>>())!.Data!;

        // 2. Add package
        var pkgReq = new CreateOrderPackageRequest
        {
            WeightKg = 5.0m,
            LengthCm = 40,
            WidthCm = 30,
            HeightCm = 20,
            DeclaredValue = 200000m
        };
        var addPkgRes = await _client.PostAsJsonAsync($"/api/v1/orders/{order.Id}/packages", pkgReq);
        addPkgRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var pkg = (await addPkgRes.Content.ReadFromJsonAsync<ApiResponse<OrderPackageDto>>())!.Data!;
        pkg.VolumetricWeightKg.Should().Be(4.8m); // (40 * 30 * 20) / 5000 = 4.8

        // Verify order totals updated
        var getOrderRes = await _client.GetAsync($"/api/v1/orders/{order.Id}");
        var orderAfterAdd = (await getOrderRes.Content.ReadFromJsonAsync<ApiResponse<OrderDetailDto>>())!.Data!;
        orderAfterAdd.TotalWeightKg.Should().Be(5.0m);
        orderAfterAdd.DeclaredValue.Should().Be(200000m);

        // 3. Update package
        var updatePkgReq = new UpdateOrderPackageRequest
        {
            WeightKg = 8.0m,
            LengthCm = 50,
            WidthCm = 40,
            HeightCm = 30,
            DeclaredValue = 350000m
        };
        var updatePkgRes = await _client.PutAsJsonAsync($"/api/v1/orders/{order.Id}/packages/{pkg.Id}", updatePkgReq);
        updatePkgRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify order totals updated
        getOrderRes = await _client.GetAsync($"/api/v1/orders/{order.Id}");
        var orderAfterUpdate = (await getOrderRes.Content.ReadFromJsonAsync<ApiResponse<OrderDetailDto>>())!.Data!;
        orderAfterUpdate.TotalWeightKg.Should().Be(8.0m);
        orderAfterUpdate.DeclaredValue.Should().Be(350000m);

        // 4. Remove package
        var delPkgRes = await _client.DeleteAsync($"/api/v1/orders/{order.Id}/packages/{pkg.Id}");
        delPkgRes.StatusCode.Should().Be(HttpStatusCode.OK);

        getOrderRes = await _client.GetAsync($"/api/v1/orders/{order.Id}");
        var orderAfterDel = (await getOrderRes.Content.ReadFromJsonAsync<ApiResponse<OrderDetailDto>>())!.Data!;
        orderAfterDel.TotalWeightKg.Should().Be(0m);
        orderAfterDel.DeclaredValue.Should().Be(0m);
    }

    [Fact]
    public async Task OrderAddresses_SetAndRetrieve_WorksCorrectly()
    {
        var token = await GetTokenAsync("sales_address_tester", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var customer = await context.Customers.FirstAsync();

        var createReq = new CreateOrderRequest { CustomerId = customer.Id };
        var createRes = await _client.PostAsJsonAsync("/api/v1/orders", createReq);
        var order = (await createRes.Content.ReadFromJsonAsync<ApiResponse<OrderDetailDto>>())!.Data!;

        // Set Pickup Address
        var pickupReq = new OrderAddressRequest
        {
            ContactName = "Warehouse Dispatch",
            PhoneNumber = "0988776655",
            AddressLine1 = "Lot 4, Tan Binh Industrial Park"
        };
        var setPickupRes = await _client.PutAsJsonAsync($"/api/v1/orders/{order.Id}/addresses/Sender", pickupReq);
        setPickupRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var pickup = (await setPickupRes.Content.ReadFromJsonAsync<ApiResponse<OrderAddressDto>>())!.Data!;
        pickup.ContactName.Should().Be("Warehouse Dispatch");
        pickup.AddressType.Should().Be("Sender");

        // Set Delivery Address
        var deliveryReq = new OrderAddressRequest
        {
            ContactName = "Retail Store 10",
            PhoneNumber = "0911223344",
            AddressLine1 = "50 Dong Khoi, Ben Nghe Ward"
        };
        var setDeliveryRes = await _client.PutAsJsonAsync($"/api/v1/orders/{order.Id}/addresses/Recipient", deliveryReq);
        setDeliveryRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Query addresses list
        var listRes = await _client.GetAsync($"/api/v1/orders/{order.Id}/addresses");
        listRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var addresses = (await listRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<OrderAddressDto>>>())!.Data!;
        addresses.Should().HaveCount(2);
    }
}
