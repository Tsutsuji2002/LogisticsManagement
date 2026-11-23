using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Logistics.Server.Application.Common.Interfaces;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Catalog.DTOs;
using Logistics.Server.Application.Modules.Catalog.Services;
using Logistics.Server.Application.Modules.Identity.DTOs;
using Logistics.Server.Domain.Entities.Catalog;
using Logistics.Server.Domain.Entities.Configuration;
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

public class CatalogIntegrationTests : IClassFixture<WebApplicationFactory<Logistics.Server.API.Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Logistics.Server.API.Program> _factory;
    private readonly string _dbName;

    public CatalogIntegrationTests(WebApplicationFactory<Logistics.Server.API.Program> factory)
    {
        _dbName = "CatalogTestDb_" + Guid.NewGuid().ToString("N");

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

        if (!await context.UnitsOfMeasure.AnyAsync())
        {
            await context.UnitsOfMeasure.AddRangeAsync(
                new UnitOfMeasure { Code = "KG", Name = "Kilogram", Type = "Weight", ConversionFactor = 1.0m, IsBaseUnit = true, IsActive = true },
                new UnitOfMeasure { Code = "G", Name = "Gram", Type = "Weight", ConversionFactor = 0.001m, IsBaseUnit = false, IsActive = true },
                new UnitOfMeasure { Code = "CM", Name = "Centimeter", Type = "Dimension", ConversionFactor = 0.01m, IsBaseUnit = false, IsActive = true },
                new UnitOfMeasure { Code = "PCS", Name = "Pieces", Type = "Count", ConversionFactor = 1.0m, IsBaseUnit = true, IsActive = true }
            );
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
        }

        await context.SaveChangesAsync();
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

            await context.UserAccounts.AddAsync(user);
            await context.SaveChangesAsync();
        }

        var loginRes = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(username, password));
        var loginBody = await loginRes.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        return loginBody!.Data!.AccessToken;
    }

    // ==========================================
    // Category Integration Tests
    // ==========================================

    [Fact]
    public async Task CreateCategory_AsSuperAdmin_ShouldReturnCreated()
    {
        await SeedBaseDataAsync();
        var token = await GetTokenAsync("admin_cat", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateCategoryRequest
        {
            Code = "CAT-ELEC-01",
            Name = "Consumer Electronics",
            Description = "Electronic gadgets and appliances"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/catalog/categories", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<CategoryDto>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
        body.Data!.Code.Should().Be("CAT-ELEC-01");
        body.Data.Name.Should().Be("Consumer Electronics");
    }

    [Fact]
    public async Task CreateCategory_WithDuplicateCode_ShouldReturnConflict()
    {
        await SeedBaseDataAsync();
        var token = await GetTokenAsync("admin_cat", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateCategoryRequest
        {
            Code = "CAT-DUP-01",
            Name = "Duplicate Category 1"
        };

        var firstResp = await _client.PostAsJsonAsync("/api/v1/catalog/categories", request);
        firstResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var dupResp = await _client.PostAsJsonAsync("/api/v1/catalog/categories", request);
        dupResp.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetCategoryTree_ShouldReturnNestedHierarchy()
    {
        await SeedBaseDataAsync();
        var token = await GetTokenAsync("admin_cat", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create Parent Category
        var parentReq = new CreateCategoryRequest
        {
            Code = "CAT-TREE-ROOT",
            Name = "Fashion & Apparel"
        };
        var parentResp = await _client.PostAsJsonAsync("/api/v1/catalog/categories", parentReq);
        parentResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var parentBody = await parentResp.Content.ReadFromJsonAsync<ApiResponse<CategoryDto>>();
        var parentId = parentBody!.Data!.Id;

        // Create Child Category
        var childReq = new CreateCategoryRequest
        {
            Code = "CAT-TREE-CHILD",
            Name = "Men's Footwear",
            ParentCategoryId = parentId
        };
        var childResp = await _client.PostAsJsonAsync("/api/v1/catalog/categories", childReq);
        childResp.StatusCode.Should().Be(HttpStatusCode.Created);

        // Query Tree
        var treeResp = await _client.GetAsync("/api/v1/catalog/categories/tree");
        treeResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var treeBody = await treeResp.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<CategoryTreeDto>>>();
        treeBody.Should().NotBeNull();
        treeBody!.Data.Should().NotBeNull();

        var rootNode = treeBody.Data!.FirstOrDefault(n => n.Code == "CAT-TREE-ROOT");
        rootNode.Should().NotBeNull();
        rootNode!.Children.Should().ContainSingle(c => c.Code == "CAT-TREE-CHILD");
    }

    [Fact]
    public async Task UpdateCategory_CircularParentReference_ShouldBeRejected()
    {
        await SeedBaseDataAsync();
        var token = await GetTokenAsync("admin_cat", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var catA = await (await _client.PostAsJsonAsync("/api/v1/catalog/categories", new CreateCategoryRequest { Code = "CAT-CIRC-A", Name = "Category A" }))
            .Content.ReadFromJsonAsync<ApiResponse<CategoryDto>>();

        var catB = await (await _client.PostAsJsonAsync("/api/v1/catalog/categories", new CreateCategoryRequest { Code = "CAT-CIRC-B", Name = "Category B", ParentCategoryId = catA!.Data!.Id }))
            .Content.ReadFromJsonAsync<ApiResponse<CategoryDto>>();

        // Attempt to set Cat A's parent to Cat B (circular)
        var updateResp = await _client.PutAsJsonAsync($"/api/v1/catalog/categories/{catA.Data.Id}", new UpdateCategoryRequest
        {
            Name = "Category A",
            ParentCategoryId = catB!.Data!.Id
        });

        // Should fail with 500 / 400 InvalidOperationException
        updateResp.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }

    // ==========================================
    // Product Integration Tests
    // ==========================================

    [Fact]
    public async Task CreateProduct_WithValidData_ShouldReturnCreated()
    {
        await SeedBaseDataAsync();
        var token = await GetTokenAsync("admin_cat", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create Category first
        var catResp = await _client.PostAsJsonAsync("/api/v1/catalog/categories", new CreateCategoryRequest
        {
            Code = "CAT-PRD-01",
            Name = "Hardware Tools"
        });
        var catBody = await catResp.Content.ReadFromJsonAsync<ApiResponse<CategoryDto>>();
        var categoryId = catBody!.Data!.Id;

        var request = new CreateProductRequest
        {
            CategoryId = categoryId,
            Sku = "SKU-BOSCH-DRILL-01",
            Barcode = "8939000112233",
            Name = "Bosch Cordless Drill GSB 120-LI",
            Description = "12V cordless impact drill driver",
            UnitOfMeasureId = 4, // PCS
            WeightKg = 1.100m,
            LengthCm = 20.0m,
            WidthCm = 15.0m,
            HeightCm = 6.0m,
            DeclaredValue = 1850000m,
            IsPerishable = false,
            IsDangerous = false
        };

        var response = await _client.PostAsJsonAsync("/api/v1/catalog/products", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ProductDto>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Data!.Sku.Should().Be("SKU-BOSCH-DRILL-01");
        body.Data.VolumeCbm.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetProductByCode_ShouldReturnProduct()
    {
        await SeedBaseDataAsync();
        var token = await GetTokenAsync("admin_cat", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var catResp = await _client.PostAsJsonAsync("/api/v1/catalog/categories", new CreateCategoryRequest
        {
            Code = "CAT-LOOKUP-01",
            Name = "Books & Stationery"
        });
        var catBody = await catResp.Content.ReadFromJsonAsync<ApiResponse<CategoryDto>>();

        var productReq = new CreateProductRequest
        {
            CategoryId = catBody!.Data!.Id,
            Sku = "SKU-BOOK-999",
            Barcode = "9786045678901",
            Name = "Clean Architecture Textbook",
            UnitOfMeasureId = 4,
            WeightKg = 0.550m,
            LengthCm = 24.0m,
            WidthCm = 16.0m,
            HeightCm = 3.0m,
            DeclaredValue = 250000m
        };

        await _client.PostAsJsonAsync("/api/v1/catalog/products", productReq);

        var response = await _client.GetAsync("/api/v1/catalog/products/code/SKU-BOOK-999");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ProductDto>>();
        body.Should().NotBeNull();
        body!.Data!.Name.Should().Be("Clean Architecture Textbook");
    }

    [Fact]
    public async Task SearchProducts_ShouldReturnProjectedResults()
    {
        await SeedBaseDataAsync();
        var token = await GetTokenAsync("admin_cat", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var catResp = await _client.PostAsJsonAsync("/api/v1/catalog/categories", new CreateCategoryRequest
        {
            Code = "CAT-SRCH-01",
            Name = "General Goods"
        });
        var catBody = await catResp.Content.ReadFromJsonAsync<ApiResponse<CategoryDto>>();

        await _client.PostAsJsonAsync("/api/v1/catalog/products", new CreateProductRequest
        {
            CategoryId = catBody!.Data!.Id,
            Sku = "SKU-LOGITECH-MOUSE",
            Name = "Logitech MX Master 3S Wireless Mouse",
            UnitOfMeasureId = 4,
            WeightKg = 0.141m,
            DeclaredValue = 2490000m
        });

        var response = await _client.GetAsync("/api/v1/catalog/products/search?q=logitech");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<ProductSearchResultDto>>>();
        body.Should().NotBeNull();
        body!.Data.Should().Contain(p => p.Sku == "SKU-LOGITECH-MOUSE");
    }

    [Fact]
    public async Task DeactivateCategory_WithActiveProducts_ShouldBeRejected()
    {
        await SeedBaseDataAsync();
        var token = await GetTokenAsync("admin_cat", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var catResp = await _client.PostAsJsonAsync("/api/v1/catalog/categories", new CreateCategoryRequest
        {
            Code = "CAT-SAFEGUARD",
            Name = "Safeguard Category"
        });
        var catBody = await catResp.Content.ReadFromJsonAsync<ApiResponse<CategoryDto>>();
        var categoryId = catBody!.Data!.Id;

        // Add active product under category
        await _client.PostAsJsonAsync("/api/v1/catalog/products", new CreateProductRequest
        {
            CategoryId = categoryId,
            Sku = "SKU-ACTIVE-ITEM",
            Name = "Active Dependent Item",
            UnitOfMeasureId = 4,
            WeightKg = 1.0m
        });

        // Attempt deactivating the category
        var deactResp = await _client.PatchAsJsonAsync($"/api/v1/catalog/categories/{categoryId}/status", new UpdateCategoryStatusRequest
        {
            IsActive = false,
            Reason = "Attempt deactivation"
        });

        // Must fail because active products exist in category
        deactResp.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }

    // ==========================================
    // Unit of Measure Integration Tests
    // ==========================================

    [Fact]
    public async Task GetUnitsOfMeasure_ShouldReturnConfiguredUnits()
    {
        await SeedBaseDataAsync();
        var token = await GetTokenAsync("admin_cat", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/v1/catalog/units");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<UnitOfMeasureDto>>>();
        body.Should().NotBeNull();
        body!.Data.Should().Contain(u => u.Code == "KG");
    }

    [Fact]
    public async Task CreateUnitOfMeasure_AsAdmin_ShouldSucceed()
    {
        await SeedBaseDataAsync();
        var token = await GetTokenAsync("admin_cat", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateUnitOfMeasureRequest
        {
            Code = "CBM",
            Name = "Cubic Meter",
            Type = "Volume",
            ConversionFactor = 1.0m,
            IsBaseUnit = true
        };

        var response = await _client.PostAsJsonAsync("/api/v1/catalog/units", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<UnitOfMeasureDto>>();
        body.Should().NotBeNull();
        body!.Data!.Code.Should().Be("CBM");
    }

    // ==========================================
    // Logistics Services Integration Tests
    // ==========================================

    [Fact]
    public async Task GetServices_ShouldReturnStandardLogisticsDeliveryServices()
    {
        await SeedBaseDataAsync();
        var token = await GetTokenAsync("admin_cat", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/v1/catalog/services");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<ServiceDto>>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeEmpty();
        body.Data.Should().Contain(s => s.ServiceCode == "SAMEDAY");
        body.Data.Should().Contain(s => s.ServiceCode == "NEXTDAY");
        body.Data.Should().Contain(s => s.ServiceCode == "STANDARD");
        body.Data.Should().Contain(s => s.ServiceCode == "ECONOMICAL");
    }

    [Fact]
    public async Task GetServiceByCode_ShouldReturnServiceDetail()
    {
        await SeedBaseDataAsync();
        var token = await GetTokenAsync("admin_cat", AppRoles.SuperAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/v1/catalog/services/SAMEDAY");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ServiceDto>>();
        body.Should().NotBeNull();
        body!.Data!.ServiceCode.Should().Be("SAMEDAY");
        body.Data.EstimatedTransitHours.Should().Be(6);
    }

    // ==========================================
    // Security & Authorization Tests
    // ==========================================

    [Fact]
    public async Task RequestWithoutToken_ShouldReturnUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync("/api/v1/catalog/categories");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateCategory_AsReadOnlyUser_ShouldReturnForbidden()
    {
        await SeedBaseDataAsync();
        var token = await GetTokenAsync("readonly_cat", AppRoles.ReadOnly);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateCategoryRequest
        {
            Code = "CAT-NO-PERM",
            Name = "Unauthorized Attempt"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/catalog/categories", request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CatalogReadService_CrossModuleQuery_ShouldResolveSuccessfully()
    {
        await SeedBaseDataAsync();
        using var scope = _factory.Services.CreateScope();
        var readService = scope.ServiceProvider.GetRequiredService<ICatalogReadService>();

        var services = await readService.GetServicesAsync();
        services.Should().NotBeEmpty();

        var canUseService = await readService.CanServiceBeUsedInTransactionAsync("NEXTDAY");
        canUseService.Should().BeTrue();

        var unit = await readService.GetUnitOfMeasureByCodeAsync("KG");
        unit.Should().NotBeNull();
        unit!.Code.Should().Be("KG");
    }
}
