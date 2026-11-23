using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Logistics.Server.Application.Common.Interfaces;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Identity.DTOs;
using Logistics.Server.Application.Modules.Shipping.DTOs;
using Logistics.Server.Domain.Entities.Configuration;
using Logistics.Server.Domain.Entities.Crm;
using Logistics.Server.Domain.Entities.Identity;
using Logistics.Server.Domain.Entities.Organization;
using Logistics.Server.Domain.Entities.Sales;
using Logistics.Server.Domain.Entities.Shipping;
using Logistics.Server.Infrastructure.Persistence;
using Logistics.Shared.Contracts;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Logistics.Server.IntegrationTests;

public class ShippingIntegrationTests : IClassFixture<WebApplicationFactory<Logistics.Server.API.Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Logistics.Server.API.Program> _factory;
    private readonly string _dbName;

    public ShippingIntegrationTests(WebApplicationFactory<Logistics.Server.API.Program> factory)
    {
        _dbName = "ShippingTestDb_" + Guid.NewGuid().ToString("N");

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

            var address = new Address
            {
                ContactName = "Vinamilk Hub",
                PhoneNumber = "0287654321",
                AddressLine1 = "10 Tan Trao, District 7",
                CountryId = country.Id,
                FormattedAddress = "10 Tan Trao, District 7, Ho Chi Minh City"
            };
            await context.Addresses.AddAsync(address);
            await context.SaveChangesAsync();

            var customer = new Customer
            {
                BranchId = branch1.Id,
                CustomerCode = "CUST-SGN-001",
                CustomerType = "Corporate",
                Name = "Vinamilk Logistics Hub",
                Email = "logistics@vinamilk.vn",
                PhoneNumber = "0287654321",
                IsActive = true
            };
            await context.Customers.AddAsync(customer);
            await context.SaveChangesAsync();

            var employee = new Employee
            {
                BranchId = branch1.Id,
                Code = "EMP001",
                FirstName = "Van",
                LastName = "Nguyen",
                FullName = "Nguyen Van Driver",
                Email = "driver@logistics.vn",
                PhoneNumber = "0901234567",
                JobTitle = "Delivery Driver",
                HireDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
                Status = "Active"
            };
            await context.Employees.AddAsync(employee);
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

    private async Task<Order> CreateConfirmedOrderAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var customer = await context.Customers.FirstAsync();
        var branch = await context.Branches.FirstAsync();
        var currency = await context.Currencies.FirstAsync();
        var country = await context.Countries.FirstAsync();

        var order = new Order
        {
            OrderNumber = "ORD-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
            CustomerId = customer.Id,
            BranchId = branch.Id,
            CurrencyId = currency.Id,
            OrderStatus = "Confirmed",
            PaymentStatus = "Pending",
            TotalWeightKg = 5.0m,
            DeclaredValue = 500000m,
            CodAmount = 200000m
        };

        order.OrderPackages.Add(new OrderPackage
        {
            PackageCode = order.OrderNumber + "-PKG01",
            Barcode = "BC001",
            WeightKg = 5.0m,
            LengthCm = 30,
            WidthCm = 20,
            HeightCm = 15,
            VolumetricWeightKg = 1.8m,
            DeclaredValue = 500000m
        });

        order.OrderAddresses.Add(new OrderAddress
        {
            AddressType = "Sender",
            ContactName = "Sender Warehouse",
            PhoneNumber = "0901112222",
            AddressLine1 = "100 Nguyen Van Linh",
            CountryId = country.Id,
            FormattedAddress = "100 Nguyen Van Linh, District 7"
        });

        order.OrderAddresses.Add(new OrderAddress
        {
            AddressType = "Recipient",
            ContactName = "Recipient Store",
            PhoneNumber = "0903334444",
            AddressLine1 = "200 Hai Ba Trung",
            CountryId = country.Id,
            FormattedAddress = "200 Hai Ba Trung, District 1"
        });

        await context.Orders.AddAsync(order);
        await context.SaveChangesAsync();

        return order;
    }

    [Fact]
    public async Task CreateShipmentFromOrder_AndRetrieve_Succeeds()
    {
        var token = await GetTokenAsync("shipping_admin", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var order = await CreateConfirmedOrderAsync();

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var destBranch = await context.Branches.Skip(1).FirstAsync();

        var request = new CreateShipmentFromOrderRequest
        {
            OrderId = order.Id,
            OriginBranchId = order.BranchId,
            DestinationBranchId = destBranch.Id,
            ServiceType = "StandardGround",
            DeclaredValue = 500000m,
            CodAmount = 200000m,
            Notes = "Fragile parcel"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/shipments/from-order", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ShipmentDetailDto>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
        body.Data!.ShipmentStatus.Should().Be("ReadyForPickup");
        body.Data.Packages.Should().HaveCount(1);
        body.Data.Addresses.Should().HaveCount(2);

        // Verify retrieval by tracking number
        var getRes = await _client.GetAsync($"/api/v1/shipments/number/{body.Data.TrackingNumber}");
        getRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var getBody = await getRes.Content.ReadFromJsonAsync<ApiResponse<ShipmentDetailDto>>();
        getBody!.Data!.Id.Should().Be(body.Data.Id);
    }

    [Fact]
    public async Task ShipmentLifecycle_StatusTransitions_ValidAndInvalid()
    {
        var token = await GetTokenAsync("lifecycle_admin", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var order = await CreateConfirmedOrderAsync();

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var destBranch = await context.Branches.Skip(1).FirstAsync();

        var createReq = new CreateShipmentFromOrderRequest
        {
            OrderId = order.Id,
            OriginBranchId = order.BranchId,
            DestinationBranchId = destBranch.Id,
            ServiceType = "StandardGround"
        };

        var createRes = await _client.PostAsJsonAsync("/api/v1/shipments/from-order", createReq);
        createRes.EnsureSuccessStatusCode();
        var shipment = (await createRes.Content.ReadFromJsonAsync<ApiResponse<ShipmentDetailDto>>())!.Data!;

        // 1. ReadyForPickup -> PickedUp
        var t1 = await _client.PostAsJsonAsync($"/api/v1/shipments/{shipment.Id}/transitions",
            new ShipmentStatusTransitionRequest { TargetStatus = "PickedUp" });
        t1.StatusCode.Should().Be(HttpStatusCode.OK);
        var s1 = (await t1.Content.ReadFromJsonAsync<ApiResponse<ShipmentDetailDto>>())!.Data!;
        s1.ShipmentStatus.Should().Be("PickedUp");

        // 2. PickedUp -> InTransit
        var t2 = await _client.PostAsJsonAsync($"/api/v1/shipments/{shipment.Id}/transitions",
            new ShipmentStatusTransitionRequest { TargetStatus = "InTransit" });
        t2.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. InTransit -> OutForDelivery
        var t3 = await _client.PostAsJsonAsync($"/api/v1/shipments/{shipment.Id}/transitions",
            new ShipmentStatusTransitionRequest { TargetStatus = "OutForDelivery" });
        t3.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. Invalid transition: OutForDelivery -> ReadyForPickup -> 409 Conflict
        var tInvalid = await _client.PostAsJsonAsync($"/api/v1/shipments/{shipment.Id}/transitions",
            new ShipmentStatusTransitionRequest { TargetStatus = "ReadyForPickup" });
        tInvalid.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // 5. Verify status history
        var histRes = await _client.GetAsync($"/api/v1/shipments/{shipment.Id}/history");
        histRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var histories = (await histRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<ShipmentStatusHistoryDto>>>())!.Data!;
        histories.Should().HaveCountGreaterOrEqualTo(4);
    }

    [Fact]
    public async Task DeliveryAttempt_AndProofOfDelivery_MarksShipmentDelivered()
    {
        var token = await GetTokenAsync("delivery_admin", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var order = await CreateConfirmedOrderAsync();

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var destBranch = await context.Branches.Skip(1).FirstAsync();
        var employee = await context.Employees.FirstAsync();

        var createReq = new CreateShipmentFromOrderRequest
        {
            OrderId = order.Id,
            OriginBranchId = order.BranchId,
            DestinationBranchId = destBranch.Id,
            ServiceType = "StandardGround"
        };
        var createRes = await _client.PostAsJsonAsync("/api/v1/shipments/from-order", createReq);
        createRes.EnsureSuccessStatusCode();
        var shipment = (await createRes.Content.ReadFromJsonAsync<ApiResponse<ShipmentDetailDto>>())!.Data!;

        // Move to OutForDelivery
        await _client.PostAsJsonAsync($"/api/v1/shipments/{shipment.Id}/transitions",
            new ShipmentStatusTransitionRequest { TargetStatus = "PickedUp" });
        await _client.PostAsJsonAsync($"/api/v1/shipments/{shipment.Id}/transitions",
            new ShipmentStatusTransitionRequest { TargetStatus = "InTransit" });
        await _client.PostAsJsonAsync($"/api/v1/shipments/{shipment.Id}/transitions",
            new ShipmentStatusTransitionRequest { TargetStatus = "OutForDelivery" });

        // Record Delivery Attempt
        var attemptReq = new RecordDeliveryAttemptRequest
        {
            AttemptedByEmployeeId = employee.Id,
            AttemptedAt = DateTime.UtcNow,
            Status = "Delivered",
            RecipientContacted = true
        };
        var attemptRes = await _client.PostAsJsonAsync($"/api/v1/shipments/{shipment.Id}/delivery-attempts", attemptReq);
        attemptRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var attempt = (await attemptRes.Content.ReadFromJsonAsync<ApiResponse<DeliveryAttemptDto>>())!.Data!;

        // Record Proof of Delivery
        var podReq = new CreateProofOfDeliveryRequest
        {
            RecipientName = "Tran Thi Recipient",
            RecipientRelation = "Self",
            DeliveredAt = DateTime.UtcNow,
            Notes = "Delivered directly to consignee"
        };
        var podRes = await _client.PostAsJsonAsync($"/api/v1/shipments/{shipment.Id}/proof-of-delivery?attemptId={attempt.Id}", podReq);
        podRes.StatusCode.Should().Be(HttpStatusCode.Created);

        // Verify Shipment is now Delivered
        var getRes = await _client.GetAsync($"/api/v1/shipments/{shipment.Id}");
        var finalShipment = (await getRes.Content.ReadFromJsonAsync<ApiResponse<ShipmentDetailDto>>())!.Data!;
        finalShipment.ShipmentStatus.Should().Be("Delivered");
        finalShipment.ActualDeliveryDate.Should().NotBeNull();
    }

    [Fact]
    public async Task PickupRequest_Lifecycle_PendingToAssignedToCompleted()
    {
        var token = await GetTokenAsync("pickup_admin", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var customer = await context.Customers.FirstAsync();
        var address = await context.Addresses.FirstAsync();
        var employee = await context.Employees.FirstAsync();

        // 1. Create Pickup Request
        var createReq = new CreatePickupRequestRequest
        {
            CustomerId = customer.Id,
            RequestedAddressId = address.Id,
            ContactName = "Warehouse Lead",
            ContactPhone = "0908889999",
            ScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            TimeSlot = "09:00 - 12:00",
            EstimatedParcels = 10,
            Notes = "Call upon arrival"
        };
        var createRes = await _client.PostAsJsonAsync("/api/v1/pickup-requests", createReq);
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var pr = (await createRes.Content.ReadFromJsonAsync<ApiResponse<PickupRequestDto>>())!.Data!;
        pr.Status.Should().Be("Pending");

        // 2. Assign Driver
        var assignRes = await _client.PostAsync($"/api/v1/pickup-requests/{pr.Id}/assign/{employee.Id}", null);
        assignRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var prAssigned = (await assignRes.Content.ReadFromJsonAsync<ApiResponse<PickupRequestDto>>())!.Data!;
        prAssigned.Status.Should().Be("Assigned");

        // 3. Driver Arrived
        var arriveRes = await _client.PostAsync($"/api/v1/pickup-requests/{pr.Id}/confirm", null);
        arriveRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var prArrived = (await arriveRes.Content.ReadFromJsonAsync<ApiResponse<PickupRequestDto>>())!.Data!;
        prArrived.Status.Should().Be("DriverArrived");

        // 4. Complete Pickup
        var completeReq = new CompletePickupRequest { ActualParcelsPickedUp = 10, Notes = "All parcels in good order" };
        var completeRes = await _client.PostAsJsonAsync($"/api/v1/pickup-requests/{pr.Id}/complete", completeReq);
        completeRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var prCompleted = (await completeRes.Content.ReadFromJsonAsync<ApiResponse<PickupRequestDto>>())!.Data!;
        prCompleted.Status.Should().Be("Completed");
        prCompleted.ActualParcelsPickedUp.Should().Be(10);
    }
}
