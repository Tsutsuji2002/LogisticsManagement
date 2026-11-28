using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Logistics.Server.Application.Common.Interfaces;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Identity.DTOs;
using Logistics.Server.Application.Modules.Identity.Services;
using Logistics.Server.Domain.Entities.Identity;
using Logistics.Server.Infrastructure.Persistence;
using Logistics.Shared.Contracts;
using Logistics.Shared.Contracts.DevData;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Logistics.Server.IntegrationTests;

public class DevTestDataIntegrationTests : IClassFixture<WebApplicationFactory<Logistics.Server.API.Program>>
{
    private readonly WebApplicationFactory<Logistics.Server.API.Program> _baseFactory;

    public DevTestDataIntegrationTests(WebApplicationFactory<Logistics.Server.API.Program> baseFactory)
    {
        _baseFactory = baseFactory;
    }

    private WebApplicationFactory<Logistics.Server.API.Program> CreateFactory(string env, bool devSeedEnabled)
    {
        var dbName = "DevTestDataDb_" + Guid.NewGuid().ToString("N");
        return _baseFactory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(env);
            builder.UseSetting("DevSeed:Enabled", devSeedEnabled ? "true" : "false");
            if (!string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase))
            {
                builder.UseSetting("Jwt:Secret", "TEST_ONLY_PRODUCTION_SECRET_SIGNING_KEY_32CHARS!");
            }
            builder.ConfigureServices(services =>
            {
                var descriptors = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    d.ServiceType.Name.Contains("DbContextOptions") ||
                    d.ServiceType == typeof(ApplicationDbContext)).ToList();

                foreach (var d in descriptors) services.Remove(d);

                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase(dbName));
            });
        });
    }

    private static async Task<string> CreateUserAndGetJwtAsync(
        WebApplicationFactory<Logistics.Server.API.Program> factory,
        HttpClient client,
        string roleCode)
    {
        var username = "devtest_" + Guid.NewGuid().ToString("N")[..8];
        const string password = "TestPassword2026!";

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasherService>();
            var idSeeder = scope.ServiceProvider.GetRequiredService<IIdentityDataSeeder>();
            await idSeeder.SeedAsync();

            var role = await db.Roles.FirstAsync(r => r.Code == roleCode);
            var user = new UserAccount
            {
                Username = username,
                Email = $"{username}@logistics.vn",
                PasswordHash = hasher.HashPassword(password),
                SecurityStamp = Guid.NewGuid().ToString("N"),
                IsActive = true
            };
            user.UserRoles.Add(new UserRole { RoleId = role.Id, AssignedBy = "Test" });
            await db.UserAccounts.AddAsync(user);
            await db.SaveChangesAsync();
        }

        var loginResp = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(username, password));
        loginResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginBody = await loginResp.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        return loginBody!.Data!.AccessToken;
    }

    [Fact]
    public async Task DevTestDataEndpoints_ShouldReturn403_WhenEnvironmentIsProduction()
    {
        using var factory = CreateFactory("Production", devSeedEnabled: true);
        using var client = factory.CreateClient();
        var jwt = await CreateUserAndGetJwtAsync(factory, client, AppRoles.SuperAdmin);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        var statusResp = await client.GetAsync("/api/v1/dev/test-data/status");
        statusResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var execResp = await client.PostAsJsonAsync("/api/v1/dev/test-data/execute", new ExecuteDevSeedRequest());
        execResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DevTestDataEndpoints_ShouldReturn403_WhenDevSeedDisabled()
    {
        using var factory = CreateFactory("Development", devSeedEnabled: false);
        using var client = factory.CreateClient();
        var jwt = await CreateUserAndGetJwtAsync(factory, client, AppRoles.SuperAdmin);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        var statusResp = await client.GetAsync("/api/v1/dev/test-data/status");
        statusResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DevTestDataEndpoints_ShouldReturn403_ForNonAdminUser()
    {
        using var factory = CreateFactory("Development", devSeedEnabled: true);
        using var client = factory.CreateClient();
        var jwt = await CreateUserAndGetJwtAsync(factory, client, AppRoles.WarehouseStaff);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        var statusResp = await client.GetAsync("/api/v1/dev/test-data/status");
        statusResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ResetAndReseed_WithoutConfirmReset_ShouldReturn400()
    {
        using var factory = CreateFactory("Development", devSeedEnabled: true);
        using var client = factory.CreateClient();
        var jwt = await CreateUserAndGetJwtAsync(factory, client, AppRoles.SuperAdmin);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        var resp = await client.PostAsJsonAsync("/api/v1/dev/test-data/execute", new ExecuteDevSeedRequest
        {
            Mode = DevSeedMode.ResetAndReseed,
            ConfirmReset = false
        });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task FullSeed_Incremental_And_ResetReseed_ShouldPopulateAllModulesAndPassIntegrityChecks()
    {
        using var factory = CreateFactory("Development", devSeedEnabled: true);
        using var client = factory.CreateClient();
        var jwt = await CreateUserAndGetJwtAsync(factory, client, AppRoles.SuperAdmin);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        // 1. FullSeed (Standard)
        var fullResp = await client.PostAsJsonAsync("/api/v1/dev/test-data/execute", new ExecuteDevSeedRequest
        {
            Mode = DevSeedMode.FullSeed,
            Scale = DevSeedScale.Standard,
            RandomSeed = 42
        });
        fullResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var fullBody = await fullResp.Content.ReadFromJsonAsync<ApiResponse<ExecuteDevSeedResultDto>>();
        fullBody!.Data!.Succeeded.Should().BeTrue();
        fullBody.Data.RecordsAddedByEntity["Orders"].Should().BeGreaterThanOrEqualTo(500);
        fullBody.Data.RecordsAddedByEntity["Shipments"].Should().BeGreaterThanOrEqualTo(400);
        fullBody.Data.RecordsAddedByEntity["Customers"].Should().BeGreaterThanOrEqualTo(100);
        fullBody.Data.IntegrityReport!.AllPassed.Should().BeTrue();

        // 2. Incremental
        var incResp = await client.PostAsJsonAsync("/api/v1/dev/test-data/execute", new ExecuteDevSeedRequest
        {
            Mode = DevSeedMode.Incremental,
            Scale = DevSeedScale.Standard,
            RandomSeed = 77
        });
        incResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var incBody = await incResp.Content.ReadFromJsonAsync<ApiResponse<ExecuteDevSeedResultDto>>();
        incBody!.Data!.RecordsAddedByEntity["Orders"].Should().BeGreaterThan(0);
        incBody.Data.IntegrityReport!.AllPassed.Should().BeTrue();

        // 3. Verify explicit endpoint
        var verifyResp = await client.PostAsJsonAsync("/api/v1/dev/test-data/verify", new { });
        verifyResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var verifyBody = await verifyResp.Content.ReadFromJsonAsync<ApiResponse<DevTestDataIntegrityReportDto>>();
        verifyBody!.Data!.AllPassed.Should().BeTrue();

        // 4. ResetAndReseed with confirmation
        var resetResp = await client.PostAsJsonAsync("/api/v1/dev/test-data/execute", new ExecuteDevSeedRequest
        {
            Mode = DevSeedMode.ResetAndReseed,
            Scale = DevSeedScale.Standard,
            RandomSeed = 99,
            ConfirmReset = true
        });
        resetResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var resetBody = await resetResp.Content.ReadFromJsonAsync<ApiResponse<ExecuteDevSeedResultDto>>();
        resetBody!.Data!.Succeeded.Should().BeTrue();
        resetBody.Data.IntegrityReport!.AllPassed.Should().BeTrue();
    }
}
