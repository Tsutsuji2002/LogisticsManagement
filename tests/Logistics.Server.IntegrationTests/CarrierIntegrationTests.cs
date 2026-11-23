using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Logistics.Server.Application.Common.Interfaces;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Carrier.DTOs;
using Logistics.Server.Application.Modules.Identity.DTOs;
using Logistics.Server.Domain.Entities.Carrier;
using Logistics.Server.Domain.Entities.Configuration;
using Logistics.Server.Domain.Entities.Crm;
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

public class CarrierIntegrationTests : IClassFixture<WebApplicationFactory<Logistics.Server.API.Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Logistics.Server.API.Program> _factory;
    private readonly string _dbName;

    public CarrierIntegrationTests(WebApplicationFactory<Logistics.Server.API.Program> factory)
    {
        _dbName = "CarrierTestDb_" + Guid.NewGuid().ToString("N");

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

            var customer = new Customer
            {
                BranchId = branch1.Id,
                CustomerCode = "CUST-CARRIER-001",
                CustomerType = "Corporate",
                Name = "Carrier Test Customer",
                Email = "test@customer.com",
                PhoneNumber = "0987654321",
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

            var branch = await context.Branches.FirstOrDefaultAsync();
            if (branch != null)
            {
                user.BranchAccesses.Add(new UserBranchAccess
                {
                    BranchId = branch.Id,
                    IsDefault = true,
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
    public async Task Carrier_Lifecycle_Create_Read_Update_Toggle_Succeeds()
    {
        var token = await GetTokenAsync("carrier_admin", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // 1. Create Carrier
        var createReq = new CreateCarrierRequest
        {
            Code = "TEST_GHN",
            Name = "Giao Hang Nhanh Test",
            ApiBaseUrl = "https://api.ghn.vn/v2",
            ContactPhone = "19001206",
            ContactEmail = "contact@ghn.vn",
            WebsiteUrl = "https://ghn.vn",
            TrackingUrlTemplate = "https://donhang.ghn.vn/?order_code={0}"
        };

        var createRes = await _client.PostAsJsonAsync("/api/v1/carriers", createReq);
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdCarrier = await createRes.Content.ReadFromJsonAsync<ApiResponse<CarrierDetailDto>>();
        createdCarrier!.Success.Should().BeTrue();
        createdCarrier.Data!.Code.Should().Be("TEST_GHN");
        createdCarrier.Data.IsActive.Should().BeTrue();

        var carrierId = createdCarrier.Data.Id;

        // 2. Read Carrier by Code
        var getRes = await _client.GetAsync("/api/v1/carriers/TEST_GHN");
        getRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await getRes.Content.ReadFromJsonAsync<ApiResponse<CarrierDetailDto>>();
        fetched!.Data!.Id.Should().Be(carrierId);

        // 3. Update Carrier
        var updateReq = new UpdateCarrierRequest
        {
            Name = "Giao Hang Nhanh Updated",
            ApiBaseUrl = "https://api.ghn.vn/v3",
            ContactPhone = "19001207"
        };
        var updateRes = await _client.PutAsJsonAsync($"/api/v1/carriers/{carrierId}", updateReq);
        updateRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateRes.Content.ReadFromJsonAsync<ApiResponse<CarrierDetailDto>>();
        updated!.Data!.Name.Should().Be("Giao Hang Nhanh Updated");

        // 4. Toggle Status
        var toggleRes = await _client.PatchAsync($"/api/v1/carriers/{carrierId}/status?isActive=false", null);
        toggleRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var toggled = await toggleRes.Content.ReadFromJsonAsync<ApiResponse<CarrierDetailDto>>();
        toggled!.Data!.IsActive.Should().BeFalse();

        // 5. Query Carriers with search & filter
        var listRes = await _client.GetAsync("/api/v1/carriers?search=GHN&page=1&pageSize=10");
        listRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await listRes.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<CarrierDto>>>();
        list!.Data!.Items.Should().Contain(c => c.Code == "TEST_GHN");
    }

    [Fact]
    public async Task CarrierServices_Lifecycle_Create_Read_Update_Toggle_Succeeds()
    {
        var token = await GetTokenAsync("carrier_svc_admin", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create parent carrier
        var createCarrierRes = await _client.PostAsJsonAsync("/api/v1/carriers", new CreateCarrierRequest
        {
            Code = "TEST_GHTK",
            Name = "GHTK Test",
            ApiBaseUrl = "https://api.ghtk.vn"
        });
        createCarrierRes.EnsureSuccessStatusCode();

        // 1. Create Service
        var createServiceReq = new CreateCarrierServiceRequest
        {
            ServiceCode = "FAST",
            ServiceName = "GHTK Fast Delivery",
            Description = "Next-day interprovincial delivery",
            EstimatedTransitHours = 24,
            MaxWeightKg = 20m,
            MaxLengthCm = 100m,
            IsCodSupported = true,
            IsInsuranceSupported = true
        };

        var createSvcRes = await _client.PostAsJsonAsync("/api/v1/carriers/TEST_GHTK/services", createServiceReq);
        createSvcRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdSvc = await createSvcRes.Content.ReadFromJsonAsync<ApiResponse<CarrierServiceDto>>();
        createdSvc!.Data!.ServiceCode.Should().Be("FAST");
        var serviceId = createdSvc.Data.Id;

        // 2. Get Services
        var listSvcRes = await _client.GetAsync("/api/v1/carriers/TEST_GHTK/services");
        listSvcRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var services = await listSvcRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<CarrierServiceDto>>>();
        services!.Data.Should().Contain(s => s.ServiceCode == "FAST");

        // 3. Update Service
        var updateSvcRes = await _client.PutAsJsonAsync($"/api/v1/carriers/TEST_GHTK/services/{serviceId}", new UpdateCarrierServiceRequest
        {
            ServiceName = "GHTK Fast Delivery Updated",
            EstimatedTransitHours = 36,
            IsCodSupported = false
        });
        updateSvcRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedSvc = await updateSvcRes.Content.ReadFromJsonAsync<ApiResponse<CarrierServiceDto>>();
        updatedSvc!.Data!.ServiceName.Should().Be("GHTK Fast Delivery Updated");
        updatedSvc.Data.IsCodSupported.Should().BeFalse();

        // 4. Toggle Service Active
        var toggleRes = await _client.PatchAsync($"/api/v1/carriers/TEST_GHTK/services/{serviceId}/status?isActive=false", null);
        toggleRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var toggledSvc = await toggleRes.Content.ReadFromJsonAsync<ApiResponse<CarrierServiceDto>>();
        toggledSvc!.Data!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task CarrierConfigurations_And_TrackingMappings_Lifecycle_Succeeds()
    {
        var token = await GetTokenAsync("carrier_cfg_admin", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create Carrier
        var createCarrierRes = await _client.PostAsJsonAsync("/api/v1/carriers", new CreateCarrierRequest
        {
            Code = "TEST_VTP",
            Name = "Viettel Post Test",
            ApiBaseUrl = "https://api.viettelpost.vn"
        });
        createCarrierRes.EnsureSuccessStatusCode();

        // 1. Upsert Configuration
        var configReq = new UpsertCarrierConfigurationRequest
        {
            Environment = "Sandbox",
            ApiKey = "vtp-sandbox-token-12345",
            Username = "vtp_user",
            Password = "vtp_secret_password",
            WebhookSecret = "vtp-wh-secret"
        };
        var configRes = await _client.PutAsJsonAsync("/api/v1/carriers/TEST_VTP/configurations", configReq);
        configRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var config = await configRes.Content.ReadFromJsonAsync<ApiResponse<CarrierConfigurationDto>>();
        config!.Data!.HasApiKey.Should().BeTrue();
        config.Data.HasPassword.Should().BeTrue();
        config.Data.HasWebhookSecret.Should().BeTrue();
        config.Data.Environment.Should().Be("Sandbox");

        // 2. Get Configurations
        var getCfgRes = await _client.GetAsync("/api/v1/carriers/TEST_VTP/configurations");
        getCfgRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var configs = await getCfgRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<CarrierConfigurationDto>>>();
        configs!.Data.Should().Contain(c => c.Environment == "Sandbox");

        // 3. Create Tracking Mapping
        var mappingReq = new CreateTrackingMappingRequest
        {
            ExternalStatusCode = "VTP_105",
            ExternalStatusName = "Delivered to recipient",
            InternalShipmentStatus = "Delivered",
            Description = "Successful last mile delivery"
        };
        var mappingRes = await _client.PostAsJsonAsync("/api/v1/carriers/TEST_VTP/tracking-mappings", mappingReq);
        mappingRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var mapping = await mappingRes.Content.ReadFromJsonAsync<ApiResponse<CarrierTrackingMappingDto>>();
        mapping!.Data!.ExternalStatusCode.Should().Be("VTP_105");
        mapping.Data.InternalShipmentStatus.Should().Be("Delivered");
        var mappingId = mapping.Data.Id;

        // 4. Update Mapping
        var updateMappingRes = await _client.PutAsJsonAsync($"/api/v1/carriers/TEST_VTP/tracking-mappings/{mappingId}", new UpdateTrackingMappingRequest
        {
            ExternalStatusName = "Delivered successfully",
            InternalShipmentStatus = "Delivered",
            Description = "Updated description"
        });
        updateMappingRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 5. Delete Mapping
        var deleteMappingRes = await _client.DeleteAsync($"/api/v1/carriers/TEST_VTP/tracking-mappings/{mappingId}");
        deleteMappingRes.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task CarrierIntegration_Book_Tracking_Rates_Webhook_Lifecycle_Succeeds()
    {
        var token = await GetTokenAsync("carrier_ops_admin", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // 1. Create Carrier with code GHN
        var createCarrierRes = await _client.PostAsJsonAsync("/api/v1/carriers", new CreateCarrierRequest
        {
            Code = "GHN",
            Name = "Giao Hang Nhanh Official",
            ApiBaseUrl = "https://api.ghn.vn"
        });
        createCarrierRes.EnsureSuccessStatusCode();
        var carrier = (await createCarrierRes.Content.ReadFromJsonAsync<ApiResponse<CarrierDetailDto>>())!.Data!;

        // 2. Query Rates
        var rateReq = new CarrierRateQueryRequest
        {
            CarrierId = carrier.Id,
            WeightKg = 2.0m,
            LengthCm = 20m,
            WidthCm = 15m,
            HeightCm = 10m,
            DeclaredValue = 200000m
        };
        var rateRes = await _client.PostAsJsonAsync("/api/v1/carrier-integration/rates", rateReq);
        rateRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var rates = await rateRes.Content.ReadFromJsonAsync<ApiResponse<CarrierRateResultDto>>();
        rates!.Data!.Success.Should().BeTrue();
        rates.Data.Options.Should().NotBeEmpty();

        // 3. Create a Shipment in database assigned to this carrier
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var branch = await db.Branches.FirstAsync();
            var customer = await db.Customers.FirstAsync();
            var currency = await db.Currencies.FirstAsync();

            var shipment = new Shipment
            {
                TrackingNumber = "SHIP-GHN-TEST-001",
                CustomerId = customer.Id,
                OriginBranchId = branch.Id,
                DestinationBranchId = branch.Id,
                AssignedCarrierId = carrier.Id,
                ShipmentStatus = "Draft",
                ServiceType = "StandardGround",
                ActualWeightKg = 1.5m,
                ChargeableWeightKg = 1.5m,
                CurrencyId = currency.Id,
                Addresses = new List<ShipmentAddress>
                {
                    new()
                    {
                        AddressType = "Sender",
                        ContactName = "Sender Corp",
                        PhoneNumber = "0901111111",
                        FormattedAddress = "123 Le Loi, D1, HCMC"
                    },
                    new()
                    {
                        AddressType = "Recipient",
                        ContactName = "Recipient Customer",
                        PhoneNumber = "0902222222",
                        FormattedAddress = "456 Tran Phu, Ba Dinh, Hanoi"
                    }
                }
            };
            await db.Shipments.AddAsync(shipment);
            await db.SaveChangesAsync();
        }

        // 4. Book Shipment with Carrier
        var bookReq = new CarrierBookShipmentRequest
        {
            ShipmentIdentifier = "SHIP-GHN-TEST-001"
        };
        var bookRes = await _client.PostAsJsonAsync("/api/v1/carrier-integration/book", bookReq);
        bookRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var booking = await bookRes.Content.ReadFromJsonAsync<ApiResponse<CarrierBookingResultDto>>();
        booking!.Data!.Success.Should().BeTrue();
        booking.Data.CarrierTrackingNumber.Should().NotBeNullOrEmpty();

        var carrierTrackingNum = booking.Data.CarrierTrackingNumber!;

        // 5. Query Tracking
        var trackingRes = await _client.GetAsync($"/api/v1/carrier-integration/tracking/GHN/{carrierTrackingNum}");
        trackingRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var tracking = await trackingRes.Content.ReadFromJsonAsync<ApiResponse<CarrierTrackingQueryDto>>();
        tracking!.Data!.CarrierCode.Should().Be("GHN");
        tracking.Data.Events.Should().NotBeEmpty();

        // 6. Test Webhook Ingestion (Unauthenticated endpoint)
        _client.DefaultRequestHeaders.Authorization = null; // No JWT needed for webhook

        var webhookPayload = "{\"tracking_number\":\"" + carrierTrackingNum + "\",\"status\":\"DELIVERED\"}";
        var webhookRes = await _client.PostAsync("/api/v1/carrier-webhooks/GHN",
            new StringContent(webhookPayload, Encoding.UTF8, "application/json"));
        webhookRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var webhookResult = await webhookRes.Content.ReadFromJsonAsync<ApiResponse<CarrierWebhookLogDto>>();
        webhookResult!.Data!.ProcessedSuccessfully.Should().BeTrue();

        // 7. Test Monitoring endpoints with token
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var apiLogsRes = await _client.GetAsync("/api/v1/carrier-monitoring/api-logs");
        apiLogsRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var apiLogs = await apiLogsRes.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<CarrierApiLogDto>>>();
        apiLogs!.Data!.Items.Should().NotBeEmpty();

        var whLogsRes = await _client.GetAsync("/api/v1/carrier-monitoring/webhook-logs");
        whLogsRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var whLogs = await whLogsRes.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<CarrierWebhookLogDto>>>();
        whLogs!.Data!.Items.Should().NotBeEmpty();

        // 8. Cancel Carrier Booking
        var cancelRes = await _client.PostAsJsonAsync("/api/v1/carrier-integration/cancel", new CarrierCancelShipmentRequest
        {
            ShipmentIdentifier = "SHIP-GHN-TEST-001",
            Reason = "Customer requested cancellation"
        });
        cancelRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var cancelResult = await cancelRes.Content.ReadFromJsonAsync<ApiResponse<CarrierCancelResultDto>>();
        cancelResult!.Data!.Success.Should().BeTrue();
    }
}
