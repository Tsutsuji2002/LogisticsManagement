using System.Net;
using System.Net.Http.Json;
using Logistics.Server.Infrastructure.Persistence;
using Logistics.Shared.Contracts;
using Logistics.Shared.Contracts.Health;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Logistics.Server.IntegrationTests;

public class HealthEndpointTests : IClassFixture<WebApplicationFactory<Logistics.Server.API.Program>>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(WebApplicationFactory<Logistics.Server.API.Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
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
                    options.UseInMemoryDatabase("TestIntegrationDb"));
            });
        }).CreateClient();
    }

    [Fact]
    public async Task GetHealth_ReturnsOkStatusCode()
    {
        var response = await _client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetHealth_ReturnsHealthyPayload()
    {
        var response = await _client.GetAsync("/api/health");
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<HealthCheckResponse>>();

        Assert.NotNull(body);
        Assert.True(body.Success);
        Assert.NotNull(body.Data);
        Assert.Equal("Healthy", body.Data.Status);
    }
}
