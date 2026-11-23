using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Logistics.Server.Application.Common.Interfaces;
using Logistics.Server.Application.Modules.Organization.DTOs;
using Logistics.Server.Application.Modules.Organization.Services;
using Logistics.Server.Infrastructure.Persistence;
using Logistics.Server.Integrations.Carriers;
using Logistics.Shared.Contracts;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Logistics.Server.IntegrationTests;

public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Logistics.Server.API.Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Logistics.Server.API.Program> _factory;

    public ApiIntegrationTests(WebApplicationFactory<Logistics.Server.API.Program> factory)
    {
        var dbName = "TestIntegrationDb_" + Guid.NewGuid().ToString("N");

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
                    options.UseInMemoryDatabase(dbName));
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task CorrelationIdMiddleware_ShouldIncludeCorrelationIdInResponseHeaders()
    {
        // Act
        var response = await _client.GetAsync("/api/health");

        // Assert
        response.Headers.Contains("X-Correlation-Id").Should().BeTrue();
        var correlationId = response.Headers.GetValues("X-Correlation-Id").FirstOrDefault();
        correlationId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CorrelationIdMiddleware_ShouldPreserveSuppliedCorrelationId()
    {
        // Arrange
        var customId = "custom-test-correlation-id-12345";
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/health");
        request.Headers.Add("X-Correlation-Id", customId);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.Headers.Contains("X-Correlation-Id").Should().BeTrue();
        var returnedId = response.Headers.GetValues("X-Correlation-Id").FirstOrDefault();
        returnedId.Should().Be(customId);
    }

    [Fact]
    public async Task CreateCompany_WithInvalidPayload_ShouldReturn400WithValidationErrors()
    {
        // Arrange - empty payload failing FluentValidation rules
        var invalidRequest = new CreateCompanyRequest(
            Code: "",
            LegalName: "",
            TradingName: "",
            TaxNumber: "",
            Email: "not-an-email",
            Phone: ""
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/companies", invalidRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse>();
        body.Should().NotBeNull();
        body!.Success.Should().BeFalse();
        body.ErrorCode.Should().Be("VALIDATION_FAILED");
        body.ValidationErrors.Should().NotBeNull();
        body.ValidationErrors.Should().ContainKey("Code");
        body.ValidationErrors.Should().ContainKey("LegalName");
        body.ValidationErrors.Should().ContainKey("Email");
    }

    [Fact]
    public async Task CreateAndGetCompany_ShouldReturnCreatedCompany()
    {
        // Arrange
        var request = new CreateCompanyRequest(
            Code: "COMP-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
            LegalName: "Integration Test Company Ltd",
            TradingName: "IT Express",
            TaxNumber: "TAX-" + Guid.NewGuid().ToString("N")[..8],
            Email: "contact@itcompany.vn",
            Phone: "+84-90-1234567"
        );

        // Act - Create
        var createResponse = await _client.PostAsJsonAsync("/api/v1/companies", request);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdBody = await createResponse.Content.ReadFromJsonAsync<ApiResponse<CompanyDto>>();
        createdBody.Should().NotBeNull();
        createdBody!.Success.Should().BeTrue();
        createdBody.Data.Should().NotBeNull();
        createdBody.Data!.Code.Should().Be(request.Code);
        createdBody.Data.PublicId.Should().NotBeEmpty();

        // Act - Get by PublicId
        var getResponse = await _client.GetAsync($"/api/v1/companies/{createdBody.Data.PublicId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var fetchedBody = await getResponse.Content.ReadFromJsonAsync<ApiResponse<CompanyDto>>();
        fetchedBody.Should().NotBeNull();
        fetchedBody!.Data!.LegalName.Should().Be(request.LegalName);
    }

    [Fact]
    public async Task GetCompanies_Paged_ShouldReturnPagedResponse()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/companies?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<CompanyDto>>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
        body.Data!.Page.Should().Be(1);
        body.Data.PageSize.Should().Be(10);
    }

    [Fact]
    public void DependencyInjection_ShouldResolveCoreLayerServices()
    {
        using var scope = _factory.Services.CreateScope();
        var sp = scope.ServiceProvider;

        // Infrastructure persistence & user context
        sp.GetRequiredService<ApplicationDbContext>().Should().NotBeNull();
        sp.GetRequiredService<ICurrentUserService>().Should().NotBeNull();
        sp.GetRequiredService<ICurrentBranchContext>().Should().NotBeNull();
        sp.GetRequiredService<IDateTimeProvider>().Should().NotBeNull();
        sp.GetRequiredService<ICacheService>().Should().NotBeNull();

        // Application module services
        sp.GetRequiredService<ICompanyService>().Should().NotBeNull();
        sp.GetRequiredService<IBranchService>().Should().NotBeNull();

        // Integrations services
        sp.GetRequiredService<ICarrierAdapterFactory>().Should().NotBeNull();
        var carrierAdapters = sp.GetServices<ICarrierAdapter>().ToList();
        carrierAdapters.Should().HaveCount(3);
    }
}
